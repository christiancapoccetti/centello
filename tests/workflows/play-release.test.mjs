import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import test from 'node:test';
import { confirmRelease, metadata, publishBeta, validateTrack } from '../../.github/scripts/play-release.mjs';

const sha256 = createHash('sha256').update('signed bundle').digest('hex');
const release = { versionCode: 22, version: '1.2.0-beta.1', notes: 'Spese veloci.' };
const completed = {
  name: release.version, versionCodes: ['22'], status: 'completed',
  releaseNotes: [{ language: 'it-IT', text: release.notes }],
};

test('release approval expires when the tag, main or the published changelog changes', () => {
  const form = { tag_name: 'v' + release.version, body: release.notes, draft: false, published_at: '2026-11-22T14:00:00Z' };
  confirmRelease(release, form, 'abc', 'abc', 'abc');
  for (const changes of [{ tag_name: 'v1.3.0' }, { body: 'Nuove note.' }, { draft: true }, { published_at: null }])
    assert.throws(() => confirmRelease(release, { ...form, ...changes }, 'abc', 'abc', 'abc'));
  assert.throws(() => confirmRelease(release, form, 'abc', 'def', 'abc'));
  assert.throws(() => confirmRelease(release, form, 'abc', 'abc', 'def'));
});

test('the tag, Unicode changelog and run number determine a repeatable release', () => {
  const result = metadata('v1.2.0-beta.1', '  ' + '🍕'.repeat(500) + '\r\n', '20', '2');
  assert.deepEqual(result, { ...release, notes: '🍕'.repeat(500) });
  assert.throws(() => metadata('v1.2.0', '🍕'.repeat(501), '20', '2'));
  assert.throws(() => metadata('v1.2.0', '  ', '20', '2'));
  assert.throws(() => metadata('refs/heads/main', 'note', '20', '2'));
  for (const base of ['', '-1', '1e2', '2100000000']) {
    assert.throws(() => metadata('v1.2.0', 'note', base, '2'));
  }
});

test('only closed phone tracks can be destinations', () => {
  for (const name of ['', 'production', 'beta', 'internal', 'qa', 'open', 'wear:production']) {
    assert.throws(() => validateTrack(name));
  }
  assert.equal(validateTrack('alpha'), 'alpha');
  assert.equal(validateTrack('Closed testers'), 'Closed testers');
});

function play({ versions = [], releases = [], bundles = [] } = {}) {
  const calls = [];
  const api = async (method, path, body) => {
    calls.push({ method, path, body });
    if (method === 'POST' && path === '/edits') return { id: 'edit' };
    if (method === 'GET' && path.endsWith('/tracks/alpha')) {
      return { track: 'alpha', releases: [...releases, ...versions.map(code => ({ status: 'completed', versionCodes: [String(code)] }))] };
    }
    if (method === 'GET' && path.endsWith('/bundles')) return { bundles };
    if (method === 'UPLOAD') return { versionCode: 22, sha256 };
    return {};
  };
  return { calls, api };
}

test('uploads a signed bundle, publishes its Italian notes and commits the edit', async () => {
  const { calls, api } = play({ releases: [{ name: 'Prima versione di prova', status: 'completed', versionCodes: ['21'] }] });
  await publishBeta(api, 'alpha', release, sha256);
  assert.deepEqual(calls.map(call => call.method), ['POST', 'GET', 'GET', 'UPLOAD', 'PUT', 'POST']);
  assert.deepEqual(calls.at(-2).body.releases[0], {
    name: release.version, versionCodes: ['22'], status: 'completed',
    releaseNotes: [{ language: 'it-IT', text: release.notes }],
  });
  assert.equal(calls.at(-1).path, '/edits/edit:commit');
});

test('a retry after a successful commit does not upload the artifact again', async () => {
  const { calls, api } = play({ releases: [completed], bundles: [{ versionCode: 22, sha256 }] });
  await publishBeta(api, 'alpha', release, sha256);
  assert.deepEqual(calls.map(call => call.method), ['POST', 'GET', 'GET', 'DELETE']);
});

test('a prior release cannot replace a newer one or reuse a different bundle', async () => {
  for (const state of [{ versions: [23] }, { bundles: [{ versionCode: 22, sha256: 'another' }] }]) {
    const { calls, api } = play(state);
    await assert.rejects(publishBeta(api, 'alpha', release, sha256));
    assert.ok(calls.every(call => call.method !== 'PUT' && call.method !== 'UPLOAD'));
  }
});

test('a newer bundle on another track prevents publishing an older code to a legacy closed release', async () => {
  const { calls, api } = play({
    releases: [{ name: 'Prima versione di prova', status: 'completed', versionCodes: ['21'] }],
    bundles: [{ versionCode: 100, sha256: 'bundle from another track' }],
  });
  await assert.rejects(publishBeta(api, 'alpha', release, sha256), /newer version code/);
  assert.deepEqual(calls.map(call => call.method), ['POST', 'GET', 'GET', 'DELETE']);
});

test('a higher run number cannot publish a semantic downgrade or duplicate version', async () => {
  for (const name of ['1.2.0', '1.2.0-beta.12', 'v1.2.0-rc.1', '11.0.0', release.version]) {
    const { calls, api } = play({ releases: [{ ...completed, name, versionCodes: ['21'] }] });
    await assert.rejects(publishBeta(api, 'alpha', release, sha256));
    assert.ok(calls.every(call => !['UPLOAD', 'PUT'].includes(call.method)));
  }
  const { api, calls } = play({ releases: [{ ...completed, name: '1.2.0-beta.9', versionCodes: ['21'] }] });
  await publishBeta(api, 'alpha', { ...release, version: '1.2.0-beta.10' }, sha256);
  assert.ok(calls.some(call => call.method === 'PUT'));
});

test('a retry respects operator changes to a tracked version and requires the original digest', async () => {
  for (const changes of [
    { status: 'halted' }, { status: 'inProgress' }, { status: 'draft' }, { name: 'versione sospesa' },
    { versionCodes: ['21', '22'] }, { releaseNotes: [{ language: 'it-IT', text: 'Modificato.' }] },
  ]) {
    const { calls, api } = play({ releases: [{ ...completed, ...changes }], bundles: [{ versionCode: 22, sha256 }] });
    await assert.rejects(publishBeta(api, 'alpha', release, sha256));
    assert.ok(calls.every(call => !['UPLOAD', 'PUT'].includes(call.method)));
  }
  const { calls, api } = play({ releases: [completed] });
  await assert.rejects(publishBeta(api, 'alpha', release, sha256));
  assert.ok(calls.every(call => !['UPLOAD', 'PUT'].includes(call.method)));
});
