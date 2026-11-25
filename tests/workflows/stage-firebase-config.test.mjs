import { spawnSync } from 'node:child_process';
import { existsSync, mkdtempSync, readFileSync, rmSync, statSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import assert from 'node:assert/strict';
import test from 'node:test';

const playPackage = 'it.christiancapoccetti.centesimo';
const applicationId = '1:987654321098:android:abcdef0123456789';
const apiKey = `AIza${'a'.repeat(35)}`;
const devSecret = 'FIREBASE_ANDROID_DEV_CONFIG_BASE64';
const playSecret = 'FIREBASE_ANDROID_PLAY_CONFIG_BASE64';

function config(packageName) {
  return {
    project_info: {
      project_number: '987654321098',
      project_id: 'centello-fixture',
      storage_bucket: 'storage-fixture.example',
    },
    client: [{
      client_info: {
        mobilesdk_app_id: applicationId,
        android_client_info: { package_name: packageName },
      },
      api_key: [{ current_key: apiKey }],
      oauth_client: [{ client_id: 'unneeded-fixture' }],
    }],
  };
}

function run(context, variant, secrets, ...args) {
  const directory = mkdtempSync(join(tmpdir(), 'firebase-config-test-'));
  context.after(() => rmSync(directory, { force: true, recursive: true }));
  const output = join(directory, 'firebase.json');
  const env = { ...process.env, [devSecret]: '', [playSecret]: '', ...secrets };
  const result = spawnSync(process.execPath,
    ['.github/scripts/stage-firebase-config.mjs', variant, output, ...args],
    { env, encoding: 'utf8' });
  if (result.error)
    throw result.error;
  return { ...result, output };
}

function encode(value) {
  return Buffer.from(JSON.stringify(value)).toString('base64');
}

test('stages only the matching Android client and SDK fields', context => {
  const input = config(playPackage);
  input.client.unshift(config(`${playPackage}.dev`).client[0]);
  input.client[0].api_key = [{ current_key: 'unneeded-fixture' }];
  const result = run(context, 'play', { [playSecret]: encode(input) });

  assert.equal(result.status, 0, result.stderr);
  assert.equal(result.stdout, '');
  assert.equal(result.stderr, '');
  assert.deepEqual(JSON.parse(readFileSync(result.output)), {
    applicationId,
    apiKey,
    projectId: 'centello-fixture',
    gcmSenderId: '987654321098',
  });
  // Windows uses ACLs. The Linux release runner enforces the POSIX mode.
  if (process.platform !== 'win32')
    assert.equal(statSync(result.output).mode & 0o777, 0o600);
});

test('stages dev configuration from the separate dev secret', context => {
  const input = config(`${playPackage}.dev`);
  const result = run(context, 'dev', { [devSecret]: encode(input), [playSecret]: 'invalid' });
  assert.equal(result.status, 0, result.stderr);
  assert.equal(JSON.parse(readFileSync(result.output)).applicationId, applicationId);
});

test('synthetic verification needs no secrets and uses unique Firebase projects', context => {
  const dev = run(context, 'dev', {}, '--synthetic');
  const play = run(context, 'play', {}, '--synthetic');
  assert.equal(dev.status, 0, dev.stderr);
  assert.equal(play.status, 0, play.stderr);
  assert.notEqual(JSON.parse(readFileSync(dev.output)).projectId,
    JSON.parse(readFileSync(play.output)).projectId);
});

test('Play staging fails closed on missing or development config', context => {
  const devConfig = encode(config(`${playPackage}.dev`));
  const missing = run(context, 'play', { [devSecret]: devConfig });
  const wrong = run(context, 'play', { [playSecret]: devConfig });

  assert.equal(missing.status, 1);
  assert.match(missing.stderr, /FIREBASE_ANDROID_PLAY_CONFIG_BASE64 is required/);
  assert.equal(existsSync(missing.output), false);
  assert.equal(wrong.status, 1);
  assert.match(wrong.stderr, /exactly one Android client/);
  assert.equal(existsSync(wrong.output), false);
});

test('malformed and ambiguous inputs fail without exposing configuration values', context => {
  const duplicate = config(playPackage);
  duplicate.client.push(duplicate.client[0]);
  const invalidProjectId = config(playPackage);
  invalidProjectId.project_info.project_id = 'SENSITIVE_FIXTURE';
  const invalidProjectNumber = config(playPackage);
  invalidProjectNumber.project_info.project_number = 'SENSITIVE_FIXTURE';
  const mismatchedAppId = config(playPackage);
  mismatchedAppId.client[0].client_info.mobilesdk_app_id = '1:123:android:abc';
  const invalidKey = config(playPackage);
  invalidKey.client[0].api_key[0].current_key = 'SENSITIVE_FIXTURE';

  for (const input of [
    'SENSITIVE_FIXTURE',
    Buffer.from('SENSITIVE_FIXTURE').toString('base64'),
    encode(duplicate),
    encode(invalidProjectId),
    encode(invalidProjectNumber),
    encode(mismatchedAppId),
    encode(invalidKey),
  ]) {
    const result = run(context, 'play', { [playSecret]: input });
    assert.equal(result.status, 1);
    assert.equal(result.stdout, '');
    assert.doesNotMatch(result.stderr, /SENSITIVE_FIXTURE/);
    assert.equal(existsSync(result.output), false);
  }
});
