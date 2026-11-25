import assert from 'node:assert/strict';
import { spawn, spawnSync } from 'node:child_process';
import { createPublicKey, verify } from 'node:crypto';
import { once } from 'node:events';
import { existsSync, mkdtempSync, readFileSync, readdirSync, rmSync, statSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseEnv } from 'node:util';
import test from 'node:test';

const cli = fileURLToPath(new URL('../../tools/Centello.Invites/invites.mjs', import.meta.url));
const installation = '2'.repeat(32);
const environment = { ...process.env };
delete environment.CENTELLO_INVITE_PRIVATE_KEY;
delete environment.CENTELLO_INVITE_PUBLIC_KEY;

test('issue an installation-bound invitation and identify it later for revocation', t => {
  const home = mkdtempSync(join(tmpdir(), 'centello-invites-'));
  t.after(() => rmSync(home, { recursive: true, force: true }));
  const run = (...args) => spawnSync(process.execPath, [cli, ...args], {
    encoding: 'utf8',
    env: { ...environment, CENTELLO_INVITE_HOME: home, CENTELLO_INVITE_PASSPHRASE: 'temporary test password' }
  });
  const initialized = run('init');
  assert.equal(initialized.status, 0, initialized.stderr);
  const raw = Buffer.from(initialized.stdout.trim(), 'base64');
  assert.equal(raw.length, 32);
  const encryptedKey = readFileSync(join(home, 'issuer.pkcs8.pem'), 'utf8');
  assert.match(encryptedKey, /^-----BEGIN ENCRYPTED PRIVATE KEY-----/);
  const publicKey = createPublicKey({ key: { kty: 'OKP', crv: 'Ed25519', x: raw.toString('base64url') }, format: 'jwk' });
  assert.notEqual(run('init').status, 0, 'repeat init must preserve the existing issuer');
  assert.notEqual(run('init-env').status, 0);
  assert.equal(readFileSync(join(home, 'issuer.pkcs8.pem'), 'utf8'), encryptedKey);
  const issued = run('issue', installation, 'Demo phone');
  assert.equal(issued.status, 0, issued.stderr);
  const token = issued.stdout.trim();
  const [version, id, target, signature] = token.split('.');
  assert.equal(version, 'cti1');
  assert.match(id, /^[a-f0-9]{32}$/);
  assert.equal(target, installation);
  const payload = Buffer.from([version, id, target].join('.'));
  assert.ok(verify(null, payload, publicKey, Buffer.from(signature, 'base64url')));
  const listed = run('list');
  assert.equal(listed.status, 0, listed.stderr);
  assert.ok(listed.stdout.includes(id) && listed.stdout.includes('Demo phone'));
  const revoked = run('revoke', id);
  assert.equal(revoked.status, 0, revoked.stderr);
  assert.ok(revoked.stdout.includes(id));
  assert.match(revoked.stdout, /existing revoked_invite_ids JSON array/);
  assert.equal(run('revoke', id).status, 0);
  assert.match(run('list').stdout, /revocation recorded locally/);
  assert.notEqual(run('issue', 'invalid-id', 'Demo phone').status, 0);
  assert.notEqual(run('issue', `${installation}\n`, 'Demo phone').status, 0);
});

test('refuse issuer storage inside the checkout', () => {
  const inRepo = fileURLToPath(new URL('../../tools/Centello.Invites', import.meta.url));
  const run = spawnSync(process.execPath, [cli, 'init'], {
    encoding: 'utf8',
    env: { ...environment, CENTELLO_INVITE_HOME: inRepo, CENTELLO_INVITE_PASSPHRASE: 'temporary test password' }
  });

  assert.notEqual(run.status, 0);
  assert.match(run.stderr, /outside Git repositories/);
});

test('initialize and use a portable issuer without overwriting or mixing signers', t => {
  const home = mkdtempSync(join(tmpdir(), 'centello-portable-invites-'));
  const recordsHome = mkdtempSync(join(tmpdir(), 'centello-portable-records-'));
  const otherHome = mkdtempSync(join(tmpdir(), 'centello-other-issuer-'));
  t.after(() => {
    for (const directory of [home, recordsHome, otherHome])
      rmSync(directory, { recursive: true, force: true });
  });

  const run = (directory, nodeOptions, ...args) => spawnSync(process.execPath, [...nodeOptions, cli, ...args], {
    encoding: 'utf8',
    env: { ...environment, CENTELLO_INVITE_HOME: directory, CENTELLO_INVITE_PASSPHRASE: 'test password' }
  });

  const initialized = run(home, [], 'init-env');
  assert.equal(initialized.status, 0, initialized.stderr);
  const issuerFile = join(home, 'issuer.env');
  const issuer = readFileSync(issuerFile, 'utf8');
  const values = parseEnv(issuer);
  assert.equal(initialized.stdout.trim(), values.CENTELLO_INVITE_PUBLIC_KEY);
  assert.ok(values.CENTELLO_INVITE_PRIVATE_KEY);
  assert.ok(!initialized.stdout.includes(values.CENTELLO_INVITE_PRIVATE_KEY));
  if (process.platform !== 'win32')
    assert.equal(statSync(issuerFile).mode & 0o777, 0o600);
  assert.notEqual(run(home, [], 'init-env').status, 0);
  assert.notEqual(run(home, [], 'init').status, 0);
  assert.equal(readFileSync(issuerFile, 'utf8'), issuer);
  assert.ok(!existsSync(join(home, 'issuer.pkcs8.pem')));

  const raw = Buffer.from(values.CENTELLO_INVITE_PUBLIC_KEY, 'base64');
  const publicKey = createPublicKey({ key: { kty: 'OKP', crv: 'Ed25519', x: raw.toString('base64url') }, format: 'jwk' });
  for (const issued of [
    run(home, [], 'issue', installation, 'Local file'),
    run(home, [`--env-file=${issuerFile}`], 'issue', installation, 'Matching file and environment'),
    run(recordsHome, [`--env-file=${issuerFile}`], 'issue', installation, 'Portable environment')
  ]) {
    assert.equal(issued.status, 0, issued.stderr);
    const parts = issued.stdout.trim().split('.');
    assert.equal(parts[2], installation);
    assert.ok(verify(null, Buffer.from(parts.slice(0, 3).join('.')), publicKey, Buffer.from(parts[3], 'base64url')));
  }

  assert.equal(run(otherHome, [], 'init-env').status, 0);
  const otherIssuerFile = join(otherHome, 'issuer.env');
  assert.notEqual(run(home, [`--env-file=${otherIssuerFile}`], 'issue', installation, 'Conflict').status, 0);
  assert.notEqual(run(recordsHome, [`--env-file=${otherIssuerFile}`], 'issue', installation, 'Conflict').status, 0);
  const wrongPublicKey = spawnSync(process.execPath, [`--env-file=${issuerFile}`, cli, 'issue', installation, 'Mismatch'], {
    encoding: 'utf8',
    env: { ...environment, CENTELLO_INVITE_HOME: recordsHome, CENTELLO_INVITE_PUBLIC_KEY: Buffer.alloc(32).toString('base64') }
  });
  assert.notEqual(wrongPublicKey.status, 0);
  assert.match(wrongPublicKey.stderr, /public key does not match/);
});

test('allow only one concurrent initializer across issuer formats', async t => {
  const home = mkdtempSync(join(tmpdir(), 'centello-competing-issuers-'));
  t.after(() => rmSync(home, { recursive: true, force: true }));
  const start = command => once(spawn(process.execPath, [cli, command], {
    stdio: 'ignore',
    env: { ...environment, CENTELLO_INVITE_HOME: home, CENTELLO_INVITE_PASSPHRASE: 'temporary test password' }
  }), 'exit');

  const [[pem], [portable]] = await Promise.all([start('init'), start('init-env')]);

  assert.deepEqual([pem, portable].toSorted(), [0, 1]);
  assert.deepEqual(readdirSync(home), [pem === 0 ? 'issuer.pkcs8.pem' : 'issuer.env']);
});
