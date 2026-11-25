import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createPublicKey, verify } from 'node:crypto';
import { mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const cli = fileURLToPath(new URL('../../tools/Centello.Invites/invites.mjs', import.meta.url));
const installation = '2'.repeat(32);

test('issue an installation-bound invitation and identify it later for revocation', t => {
  const home = mkdtempSync(join(tmpdir(), 'centello-invites-'));
  t.after(() => rmSync(home, { recursive: true, force: true }));
  const run = (...args) => spawnSync(process.execPath, [cli, ...args], {
    encoding: 'utf8',
    env: { ...process.env, CENTELLO_INVITE_HOME: home, CENTELLO_INVITE_PASSPHRASE: 'temporary test password' }
  });
  const initialized = run('init');
  assert.equal(initialized.status, 0, initialized.stderr);
  const raw = Buffer.from(initialized.stdout.trim(), 'base64');
  assert.equal(raw.length, 32);
  const encryptedKey = readFileSync(join(home, 'issuer.pkcs8.pem'), 'utf8');
  assert.match(encryptedKey, /^-----BEGIN ENCRYPTED PRIVATE KEY-----/);
  const publicKey = createPublicKey({ key: { kty: 'OKP', crv: 'Ed25519', x: raw.toString('base64url') }, format: 'jwk' });
  assert.notEqual(run('init').status, 0, 'repeat init must preserve the existing issuer');
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
    env: { ...process.env, CENTELLO_INVITE_HOME: inRepo, CENTELLO_INVITE_PASSPHRASE: 'temporary test password' }
  });

  assert.notEqual(run.status, 0);
  assert.match(run.stderr, /outside Git repositories/);
});
