#!/usr/bin/env bash
set -euo pipefail

bundle="$1"
version_code="$2"
version="$3"

: "${FirebaseAndroidConfigFile:?Stage the Play Firebase configuration before building the bundle.}"

dotnet build src/Centello.App/Centello.App.csproj -c Release -t:SignAndroidPackage \
  -p:PlayRelease=true "-p:FirebaseAndroidConfigFile=$FirebaseAndroidConfigFile" \
  "-p:ApplicationVersion=$version_code" "-p:ApplicationDisplayVersion=$version"

pwsh -NoProfile -File tests/android/check-variant.ps1 \
  -Manifest src/Centello.App/obj/Release/net10.0-android/android/AndroidManifest.xml -Variant Play
cp "src/Centello.App/bin/Release/net10.0-android/$PLAY_PACKAGE-Signed.aab" "$bundle"
jarsigner -verify -strict -keystore "$PLAY_UPLOAD_KEYSTORE" -storepass:file "$PLAY_UPLOAD_PASSWORD_FILE" "$bundle" upload
if ! unzip -p "$bundle" base/assets/firebase-android.json | cmp -s "$FirebaseAndroidConfigFile" -; then
  echo "::error::The signed Play bundle must contain the staged Firebase options."
  exit 1
fi
