import { writeFileSync } from 'node:fs';

const playPackage = 'it.christiancapoccetti.centesimo';
const variants = {
  dev: { packageName: `${playPackage}.dev`, secret: 'FIREBASE_ANDROID_DEV_CONFIG_BASE64' },
  play: { packageName: playPackage, secret: 'FIREBASE_ANDROID_PLAY_CONFIG_BASE64' },
};

function parseConfig(encoded, secret) {
  if (!encoded)
    throw new Error(`${secret} is required.`);

  const compact = encoded.replace(/\s/g, '');
  const bytes = Buffer.from(compact, 'base64');
  if (bytes.toString('base64') !== compact)
    throw new Error(`${secret} must contain standard base64.`);

  try {
    return JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes));
  } catch {
    throw new Error(`${secret} must decode to UTF-8 JSON.`);
  }
}

function androidOptions(config, packageName) {
  const clients = Array.isArray(config?.client) ? config.client : [];
  const matches = clients.filter(client =>
    client?.client_info?.android_client_info?.package_name === packageName);
  if (matches.length !== 1)
    throw new Error(`Firebase configuration must contain exactly one Android client for ${packageName}.`);

  const [client] = matches;
  const applicationId = client.client_info.mobilesdk_app_id;
  const apiKey = client.api_key?.[0]?.current_key;
  const projectId = config.project_info?.project_id;
  const gcmSenderId = config.project_info?.project_number;
  if (typeof gcmSenderId !== 'string' || !/^[1-9][0-9]*$/.test(gcmSenderId))
    throw new Error('Firebase project_number is missing or invalid.');
  if (typeof applicationId !== 'string' ||
      !new RegExp(`^1:${gcmSenderId}:android:[0-9a-f]+$`).test(applicationId))
    throw new Error('Firebase mobilesdk_app_id is missing or does not match project_number.');
  if (typeof apiKey !== 'string' || !/^AIza[A-Za-z0-9_-]{35}$/.test(apiKey))
    throw new Error('Firebase current_key is missing or invalid.');
  if (typeof projectId !== 'string' || !/^[a-z][a-z0-9-]{4,28}[a-z0-9]$/.test(projectId))
    throw new Error('Firebase project_id is missing or invalid.');

  return { applicationId, apiKey, projectId, gcmSenderId };
}

function syntheticConfig(variant, packageName) {
  const projectNumber = variant === 'dev' ? '111111111111' : '222222222222';
  return {
    project_info: { project_id: `centello-ci-${variant}`, project_number: projectNumber },
    client: [{
      client_info: {
        mobilesdk_app_id: `1:${projectNumber}:android:0123456789abcdef`,
        android_client_info: { package_name: packageName },
      },
      api_key: [{ current_key: `AIza${'0'.repeat(35)}` }],
    }],
  };
}

try {
  const [variant, output, mode, ...rest] = process.argv.slice(2);
  if (!Object.hasOwn(variants, variant) || !output ||
      (mode !== undefined && mode !== '--synthetic') || rest.length)
    throw new Error('Usage: node .github/scripts/stage-firebase-config.mjs dev|play OUTPUT [--synthetic]');

  const { packageName, secret } = variants[variant];
  const config = mode === '--synthetic'
    ? syntheticConfig(variant, packageName)
    : parseConfig(process.env[secret], secret);
  const options = androidOptions(config, packageName);
  try {
    writeFileSync(output, JSON.stringify(options), { flag: 'wx', mode: 0o600 });
  } catch {
    throw new Error('Could not create the staged Firebase configuration.');
  }
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}
