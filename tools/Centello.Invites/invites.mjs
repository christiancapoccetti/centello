#!/usr/bin/env node

import { createPrivateKey, generateKeyPairSync, randomUUID, sign } from 'node:crypto';
import {
  closeSync, existsSync, fsyncSync, linkSync, mkdirSync, openSync,
  readFileSync, readdirSync, realpathSync, unlinkSync, writeFileSync
} from 'node:fs';
import { homedir } from 'node:os';
import { basename, dirname, join, resolve } from 'node:path';
import { emitKeypressEvents } from 'node:readline';

const identifier = /^[a-f0-9]{32}$/;
const usage = 'Usage: node tools/Centello.Invites/invites.mjs init | issue <installation-id> <label> | list | revoke <invite-id>';

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

async function initialize(home, keyFile) {
  if (existsSync(keyFile))
    throw new Error('The issuer is already initialized.');

  const passphrase = await getPassphrase(true);
  const { publicKey, privateKey } = generateKeyPairSync('ed25519', {
    privateKeyEncoding: { type: 'pkcs8', format: 'pem', cipher: 'aes-256-cbc', passphrase }
  });
  mkdirSync(home, { recursive: true, mode: 0o700 });
  checkOutsideRepository(realpathSync(home));
  publishFile(keyFile, privateKey);
  const rawKey = Buffer.from(publicKey.export({ format: 'jwk' }).x, 'base64url');
  process.stdout.write(`${rawKey.toString('base64')}\n`);
}

async function issue(keyFile, records, installationId, label) {
  const encryptedKey = readFileSync(keyFile);
  const passphrase = await getPassphrase();
  let privateKey;
  try {
    privateKey = createPrivateKey({ key: encryptedKey, format: 'pem', passphrase });
  } catch {
    throw new Error('Cannot read the issuer key. Check the passphrase and key file.');
  }

  const inviteId = randomUUID().replaceAll('-', '');
  const payload = `cti1.${inviteId}.${installationId}`;
  const signature = sign(null, Buffer.from(payload, 'ascii'), privateKey).toString('base64url');
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
  const records = join(home, 'issued');
  const revocations = join(home, 'revoked');
  switch (command) {
    case 'init':
      if (args.length !== 0)
        throw new Error(usage);
      await initialize(home, keyFile);
      break;
    case 'issue': {
      if (args.length !== 2 || !isIdentifier(args[0]))
        throw new Error('Provide a 32-character lowercase hexadecimal installation ID and a label.');
      const label = args[1].trim();
      if (!label || /[\p{Cc}]/u.test(label))
        throw new Error('Provide a label without control characters.');
      if (!existsSync(keyFile))
        throw new Error('Run init to create an issuer first.');
      await issue(keyFile, records, args[0], label);
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
