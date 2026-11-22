import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { appendFileSync, readFileSync, writeFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const normalizeNotes = notes => notes?.replaceAll('\r\n', '\n').trim();

export function confirmRelease(release, form, eventSha, tagSha, mainSha) {
  if (tagSha !== eventSha || mainSha !== eventSha)
    throw new Error('The release tag and current main must both point to the verified commit.');
  if (!form.published_at || form.draft || form.tag_name !== 'v' + release.version || normalizeNotes(form.body) !== release.notes)
    throw new Error('The GitHub release must still be published with the original tag and signed artifact notes.');
}

export function metadata(tag, changelog, base, run) {
  if (!/^v(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)(?:-beta\.[1-9]\d*)?$/.test(tag))
    throw new Error('Use a version tag such as v1.2.0 or v1.2.0-beta.1.');
  const notes = normalizeNotes(changelog);
  if (!notes || [...notes].length > 500)
    throw new Error('Enter 1 to 500 Unicode characters of Italian release notes.');
  const versionCode = Number(base) + Number(run);
  if (!/^\d+$/.test(base) || !/^[1-9]\d*$/.test(run) || versionCode > 2100000000)
    throw new Error('PLAY_VERSION_CODE_BASE plus the run number must be a valid Android version code.');
  return { version: tag.slice(1), versionCode, notes };
}

export function validateTrack(track) {
  const reserved = ['production', 'internal', 'qa', 'beta', 'open'];
  if (!track || track !== track.trim() || /[:/\\\p{C}]/u.test(track) || reserved.includes(track.toLowerCase()))
    throw new Error('Set PLAY_CLOSED_TRACK to the exact identifier of an existing closed phone track.');
  return track;
}

function parseVersion(name) {
  const match = /^v?(\d+)\.(\d+)\.(\d+)(?:-([\w.-]+))?(?:\+[\w.-]+)?$/.exec(name);
  if (!match) return null; // Play also accepts arbitrary human-readable release names.
  return { core: match.slice(1, 4), pre: match[4]?.split('.') };
}

function compareIdentifiers(left, right) {
  if (left === right) return 0;
  const leftNumeric = /^\d+$/.test(left);
  const rightNumeric = /^\d+$/.test(right);
  if (leftNumeric && rightNumeric) return Math.sign(Number(BigInt(left) - BigInt(right)));
  if (leftNumeric !== rightNumeric) return leftNumeric ? -1 : 1;
  return left < right ? -1 : 1;
}

function compareVersions(left, right) {
  for (let index = 0; index < 3; index++) {
    const order = compareIdentifiers(left.core[index], right.core[index]);
    if (order) return order;
  }
  if (!left.pre || !right.pre) return Number(!left.pre) - Number(!right.pre);
  for (let index = 0; index < Math.min(left.pre.length, right.pre.length); index++) {
    const order = compareIdentifiers(left.pre[index], right.pre[index]);
    if (order) return order;
  }
  return left.pre.length - right.pre.length;
}

export async function publishBeta(api, track, release, sha256) {
  validateTrack(track);
  const { id } = await api('POST', '/edits', {});
  const edit = '/edits/' + encodeURIComponent(id);
  try {
    const path = edit + '/tracks/' + encodeURIComponent(track);
    const current = await api('GET', path);
    const code = String(release.versionCode);
    const next = parseVersion(release.version);
    let alreadyPublished = false;
    for (const item of current.releases ?? []) {
      const codes = item.versionCodes ?? [];
      if (codes.some(value => Number(value) > release.versionCode))
        throw new Error('The track already contains a newer version code.');
      if (codes.includes(code)) {
        if (alreadyPublished || item.status !== 'completed' || item.name !== release.version || codes.length !== 1 ||
            item.releaseNotes?.length !== 1 || item.releaseNotes[0].language !== 'it-IT' ||
            item.releaseNotes[0].text !== release.notes || item.countryTargeting || item.inAppUpdatePriority > 0)
          throw new Error('The tracked version was changed in Play Console. Preserve the operator decision.');
        alreadyPublished = true;
      } else {
        const previous = parseVersion(item.name);
        if (previous && compareVersions(previous, next) >= 0)
          throw new Error('The track already contains this semantic version or a newer one.');
      }
    }

    const { bundles = [] } = await api('GET', edit + '/bundles');
    const existing = bundles.find(bundle => Number(bundle.versionCode) === release.versionCode);
    if (alreadyPublished && !existing)
      throw new Error('The original tracked bundle could not be verified.');
    const bundle = existing ?? await api('UPLOAD', edit + '/bundles');
    if (Number(bundle.versionCode) !== release.versionCode || bundle.sha256?.toLowerCase() !== sha256)
      throw new Error('The Play bundle does not match the signed artifact and version code.');

    // A commit may succeed at Google even if the runner never receives its response.
    if (alreadyPublished) {
      await api('DELETE', edit);
      return;
    }
    await api('PUT', path, {
      track,
      releases: [{
        name: release.version, versionCodes: [String(release.versionCode)], status: 'completed',
        releaseNotes: [{ language: 'it-IT', text: release.notes }],
      }],
    });
    await api('POST', edit + ':commit');
  } catch (error) {
    await api('DELETE', edit).catch(() => {});
    throw error;
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const env = process.env;
  if (process.argv[2] === 'prepare') {
    const form = JSON.parse(readFileSync(process.argv[3], 'utf8'));
    if (form.tag_name !== env.GITHUB_REF_NAME || form.draft || !form.published_at)
      throw new Error('The published release no longer matches the event tag.');
    const release = metadata(form.tag_name, form.body, env.PLAY_VERSION_CODE_BASE, env.GITHUB_RUN_NUMBER);
    writeFileSync(process.argv[4], JSON.stringify(release));
    appendFileSync(env.GITHUB_OUTPUT, `version=${release.version}\ncode=${release.versionCode}\n`);
  } else if (process.argv[2] === 'confirm') {
    const repo = 'repos/' + env.GITHUB_REPOSITORY;
    const get = resource => JSON.parse(execFileSync('gh', ['api', repo + resource], { encoding: 'utf8' }));
    const release = JSON.parse(readFileSync(process.argv[3], 'utf8'));
    confirmRelease(release, get('/releases/' + env.RELEASE_ID), env.GITHUB_SHA,
      get('/commits/' + env.GITHUB_REF_NAME).sha, get('/commits/main').sha);
  } else if (process.argv[2] === 'publish') {
    const release = JSON.parse(readFileSync(process.argv[3], 'utf8'));
    const bytes = readFileSync(process.argv[4]);
    const sha256 = createHash('sha256').update(bytes).digest('hex');
    const base = 'https://androidpublisher.googleapis.com';
    const app = '/androidpublisher/v3/applications/' + encodeURIComponent(env.PLAY_PACKAGE);
    const api = async (method, path, data) => {
      const upload = method === 'UPLOAD';
      const url = upload ? base + '/upload' + app + path + '?uploadType=media' : base + app + path;
      const response = await fetch(url, {
        method: upload ? 'POST' : method,
        headers: {
          Authorization: 'Bearer ' + env.PLAY_ACCESS_TOKEN,
          'Content-Type': upload ? 'application/octet-stream' : 'application/json',
        },
        body: upload ? bytes : JSON.stringify(data),
      });
      if (!response.ok)
        throw new Error(`Play API ${method} ${path} failed (${response.status}): ${await response.text()}`);
      const result = await response.text();
      return result ? JSON.parse(result) : {};
    };
    await publishBeta(api, env.PLAY_CLOSED_TRACK, release, sha256);
    console.log(`Published ${release.version} (code ${release.versionCode}) to ${env.PLAY_CLOSED_TRACK}.`);
  } else {
    throw new Error('Expected prepare, confirm or publish.');
  }
}
