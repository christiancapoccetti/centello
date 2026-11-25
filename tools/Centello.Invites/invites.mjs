#!/usr/bin/env node

import { createPrivateKey, createPublicKey, generateKeyPairSync, randomUUID, sign } from 'node:crypto';
import {
  closeSync, existsSync, fsyncSync, linkSync, mkdirSync, openSync,
  readFileSync, readdirSync, realpathSync, rmdirSync, unlinkSync, writeFileSync
} from 'node:fs';
import { homedir } from 'node:os';
import { basename, dirname, join, resolve } from 'node:path';
import { emitKeypressEvents } from 'node:readline';
import { parseEnv } from 'node:util';

const identifier = /^[a-f0-9]{32}$/;
const usage = 'Usage: node tools/Centello.Invites/invites.mjs init | init-env | issue <installation-id> <label> | list | revoke <invite-id>';
const privateKeyVariable = 'CENTELLO_INVITE_PRIVATE_KEY';
const publicKeyVariable = 'CENTELLO_INVITE_PUBLIC_KEY';

function isIdentifier(value) {
  return value.length === 32 && identifier.test(value);
}

function defaultHome() {
  const userHome = homedir();
  if (process.platform === 'win32')
    return join(process.env.LOCALAPPDATA || join(userHome, 'AppData', 'Local'), 'Centello', 'Invites');

  if (process.platform === 'darwin')
    return join(userHome, 'Library', 'Application Support', 'Centello', 'Invites');

  return join(process.env.XDG_DATA_HOME || join(userHome, '.local', 'share'), 'centello', 'invites');
}

function resolvedDestination(directory) {
  if (existsSync(directory))
    return realpathSync(directory);

  return join(resolvedDestination(dirname(directory)), basename(directory));
}

function checkOutsideRepository(directory) {
  for (let current = directory; ; current = dirname(current)) {
    if (existsSync(join(current, '.git')))
      throw new Error('The issuer directory must be outside Git repositories.');

    if (current === dirname(current))
      return;
  }
}

function inviteHome() {
  const directory = resolve(process.env.CENTELLO_INVITE_HOME || defaultHome());
  const destination = resolvedDestination(directory);
  checkOutsideRepository(directory);
  checkOutsideRepository(destination);
  return destination;
}

function publishFile(path, content) {
  // Stage before publishing so competing processes can only see complete files.
  const staged = join(dirname(path), `.${randomUUID()}.tmp`);
  const descriptor = openSync(staged, 'wx', 0o600);
  try {
    try {
      writeFileSync(descriptor, content, 'utf8');
      fsyncSync(descriptor);
    } finally {
      closeSync(descriptor);
    }
    linkSync(staged, path);
  } finally {
    unlinkSync(staged);
  }
}

function readHidden(prompt) {
  const input = process.stdin;
  if (!input.isTTY)
    throw new Error('Use a terminal to enter the passphrase or set CENTELLO_INVITE_PASSPHRASE.');

  const previousMode = input.isRaw;
  emitKeypressEvents(input);
  input.setRawMode(true);
  process.stderr.write(prompt);
  input.resume();
  return new Promise((resolve, reject) => {
    const characters = [];
    function finish() {
      input.off('keypress', handleKey);
      input.setRawMode(previousMode);
      input.pause();
      process.stderr.write('\n');
    }
    function handleKey(text, key) {
      if (key.name === 'return' || key.name === 'enter') {
        finish();
        resolve(characters.join(''));
      } else if (key.ctrl && (key.name === 'c' || key.name === 'd')) {
        finish();
        reject(new Error('Passphrase entry canceled.'));
      } else if (key.name === 'backspace') {
        characters.pop();
      } else if (text && !key.ctrl && !key.meta && !/[\p{Cc}]/u.test(text)) {
        characters.push(...text);
      }
    }
    input.on('keypress', handleKey);
  });
}

async function getPassphrase(confirm = false) {
  const configured = process.env.CENTELLO_INVITE_PASSPHRASE;
  const passphrase = configured ?? await readHidden('Issuer passphrase: ');
  if (!passphrase)
    throw new Error('An issuer passphrase is required.');

  if (configured === undefined && confirm && passphrase !== await readHidden('Confirm issuer passphrase: '))
    throw new Error('The passphrases differ.');

  return passphrase;
}

function publicKeyBase64(key) {
  return Buffer.from(key.export({ format: 'jwk' }).x, 'base64url').toString('base64');
}

function withIssuerLock(home, work) {
  mkdirSync(home, { recursive: true, mode: 0o700 });
  checkOutsideRepository(realpathSync(home));
  const lock = join(home, '.issuer-lock');
  try {
    mkdirSync(lock, { mode: 0o700 });
  } catch (error) {
    if (error.code === 'EEXIST')
      throw new Error('Another issuer operation holds the directory lock. Retry after it finishes.');

    throw error;
  }

  try {
    work(basename(lock));
  } finally {
    rmdirSync(lock);
  }
}

async function initialize(home, file, portable) {
  if (process.env[privateKeyVariable] !== undefined)
    throw new Error('An issuer key is already loaded in the environment.');

  if (existsSync(home) && readdirSync(home).length > 0)
    throw new Error('Initialize the issuer in an empty directory.');

  const options = portable ? undefined : {
    privateKeyEncoding: { type: 'pkcs8', format: 'pem', cipher: 'aes-256-cbc', passphrase: await getPassphrase(true) }
  };
  const { publicKey, privateKey } = generateKeyPairSync('ed25519', options);
  const raw = publicKeyBase64(publicKey);
  const content = portable
    ? `${privateKeyVariable}=${privateKey.export({ format: 'der', type: 'pkcs8' }).toString('base64')}\n${publicKeyVariable}=${raw}\n`
    : privateKey;

  withIssuerLock(home, lockName => {
    if (readdirSync(home).some(entry => entry !== lockName))
      throw new Error('Initialize the issuer in an empty directory.');

    publishFile(file, content);
  });
  process.stdout.write(`${raw}\n`);
}

function readPortableKey(encoded, expectedPublicKey) {
  const invalidKey = 'The portable issuer needs an Ed25519 PKCS#8 private key in canonical base64.';
  let privateKey;
  try {
    privateKey = createPrivateKey({ key: Buffer.from(encoded ?? '', 'base64'), format: 'der', type: 'pkcs8' });
  } catch {
    throw new Error(invalidKey);
  }

  if (privateKey.asymmetricKeyType !== 'ed25519' ||
      privateKey.export({ format: 'der', type: 'pkcs8' }).toString('base64') !== encoded)
    throw new Error(invalidKey);

  if (expectedPublicKey === undefined)
    throw new Error('The portable issuer needs its public key.');

  assertPublicKey(privateKey, expectedPublicKey);
  return privateKey;
}

function assertPublicKey(privateKey, expectedPublicKey) {
  if (publicKeyBase64(createPublicKey(privateKey)) !== expectedPublicKey)
    throw new Error('The issuer public key does not match the private key.');
}

async function readEncryptedKey(keyFile) {
  const passphrase = await getPassphrase();
  let privateKey;
  try {
    privateKey = createPrivateKey({ key: readFileSync(keyFile), format: 'pem', passphrase });
  } catch {
    throw new Error('Cannot read the issuer key. Check the passphrase and key file.');
  }

  if (privateKey.asymmetricKeyType !== 'ed25519')
    throw new Error('The issuer key must use Ed25519.');

  return privateKey;
}

async function loadIssuer(keyFile, portableFile) {
  const hasPem = existsSync(keyFile);
  const hasPortableFile = existsSync(portableFile);
  const environmentKey = process.env[privateKeyVariable];
  const environmentPublicKey = process.env[publicKeyVariable];
  if (hasPem && (hasPortableFile || environmentKey !== undefined))
    throw new Error('Conflicting issuer key sources.');

  if (hasPortableFile) {
    checkOutsideRepository(dirname(realpathSync(portableFile)));
    const values = parseEnv(readFileSync(portableFile, 'utf8'));
    if (environmentKey !== undefined && (values[privateKeyVariable] !== environmentKey || environmentPublicKey === undefined))
      throw new Error('Conflicting issuer key sources.');

    const key = readPortableKey(values[privateKeyVariable], values[publicKeyVariable]);
    if (environmentPublicKey !== undefined)
      assertPublicKey(key, environmentPublicKey);

    return { key, source: portableFile };
  }

  if (environmentKey !== undefined)
    return { key: readPortableKey(environmentKey, environmentPublicKey), source: '' };

  if (!hasPem)
    throw new Error('Run init or init-env to create an issuer first.');

  const key = await readEncryptedKey(keyFile);
  if (environmentPublicKey !== undefined)
    assertPublicKey(key, environmentPublicKey);

  return { key, source: keyFile };
}

function bindIssuer(home, keyFile, portableFile, { key, source }) {
  const binding = join(home, 'issuer.public');
  const publicKey = publicKeyBase64(createPublicKey(key));
  if (!existsSync(binding)) {
    withIssuerLock(home, () => {
      const currentSource = existsSync(keyFile) ? keyFile : existsSync(portableFile) ? portableFile : '';
      if (source !== currentSource)
        throw new Error('The issuer key source changed. Retry the command.');

      if (!existsSync(binding))
        publishFile(binding, `${publicKey}\n`);
    });
  }

  if (readFileSync(binding, 'utf8').trim() !== publicKey)
    throw new Error('This directory is already bound to a different issuer.');
}

async function issue(home, keyFile, portableFile, records, installationId, label) {
  const issuer = await loadIssuer(keyFile, portableFile);
  bindIssuer(home, keyFile, portableFile, issuer);
  const inviteId = randomUUID().replaceAll('-', '');
  const payload = `cti1.${inviteId}.${installationId}`;
  const signature = sign(null, Buffer.from(payload, 'ascii'), issuer.key).toString('base64url');
  mkdirSync(records, { recursive: true, mode: 0o700 });
  checkOutsideRepository(realpathSync(records));
  publishFile(join(records, `${inviteId}.json`), JSON.stringify({ inviteId, installationId, label }));
  process.stdout.write(`${payload}.${signature}\n`);
}

function list(records, revocations) {
  if (!existsSync(records))
    return;

  for (const file of readdirSync(records).sort()) {
    if (!/^[a-f0-9]{32}\.json$/.test(file))
      continue;

    const { inviteId, installationId, label } = JSON.parse(readFileSync(join(records, file), 'utf8'));
    const status = existsSync(join(revocations, inviteId)) ? '\t[revocation recorded locally]' : '';
    process.stdout.write(`${inviteId}\t${installationId}\t${label}${status}\n`);
  }
}

function revoke(records, revocations, inviteId) {
  if (!existsSync(join(records, `${inviteId}.json`)))
    throw new Error('No invitation matches this identifier.');

  mkdirSync(revocations, { recursive: true, mode: 0o700 });
  checkOutsideRepository(realpathSync(revocations));
  try {
    publishFile(join(revocations, inviteId), '');
  } catch (error) {
    if (error.code !== 'EEXIST')
      throw error;
  }
  process.stdout.write(`Append "${inviteId}" to the existing revoked_invite_ids JSON array in Firebase Remote Config and publish it.\n`);
}

async function main([command, ...args]) {
  const home = inviteHome();
  const keyFile = join(home, 'issuer.pkcs8.pem');
  const portableFile = join(home, 'issuer.env');
  const records = join(home, 'issued');
  const revocations = join(home, 'revoked');
  switch (command) {
    case 'init':
    case 'init-env':
      if (args.length !== 0)
        throw new Error(usage);
      await initialize(home, command === 'init-env' ? portableFile : keyFile, command === 'init-env');
      break;
    case 'issue': {
      if (args.length !== 2 || !isIdentifier(args[0]))
        throw new Error('Provide a 32-character lowercase hexadecimal installation ID and a label.');
      const label = args[1].trim();
      if (!label || /[\p{Cc}]/u.test(label))
        throw new Error('Provide a label without control characters.');
      await issue(home, keyFile, portableFile, records, args[0], label);
      break;
    }
    case 'list':
      if (args.length !== 0)
        throw new Error(usage);
      list(records, revocations);
      break;
    case 'revoke':
      if (args.length !== 1 || !isIdentifier(args[0]))
        throw new Error('Provide the 32-character lowercase hexadecimal invite ID from list.');
      revoke(records, revocations, args[0]);
      break;
    default:
      throw new Error(usage);
  }
}

try {
  await main(process.argv.slice(2));
} catch (error) {
  process.stderr.write(`${error.message}\n`);
  process.exitCode = 1;
}
