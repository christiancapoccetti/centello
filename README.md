# Centello

Centello is a simple Android expense tracker built with .NET MAUI, designed and developed by Christian Capoccetti. The product research, the design, the architecture and the structure of the code are the author's own work; AI coding assistants were used as tools to write and review the implementation under the author's direction. Its goal is to make expense entry quick while keeping the expense database on the device; the optional voice, photo and PDF entry sends what you speak, photograph or pick to OpenAI (see below).

## Features

- Home (`Oggi`): month total, remaining budget, comparison with the previous period, category line, daily spending chart, latest expenses and a split button to add an expense by hand, by voice, or from a photo or PDF.
- Add, edit and delete expenses with an amount, date, category, optional tag and optional note. The `Spese` screen lists the expense history by month.
- Create, edit, archive and restore categories with optional monthly budgets, and tags within a category. Archived categories are unavailable for new expenses, while existing expenses keep their category. On first run the app suggests common categories to create in one tap.
- Each category has a pastel colour and an icon from a 32-icon catalog. The editor suggests the least-used of the six palette colours and the user may pick another.
- Category detail with tag filters, and `Insight` for any month or year: the total against the previous period, a pace chart of cumulative spending (automatic and variable) with the budget, a waterfall of what changed per category, and history-based insights.
- Recap (`Il tuo novembre`, `Il tuo 2026`): Wrapped-style full-screen story cards for every closed month and year, computed on the device and opened from the month or year row of `Insight` (a year is reviewable from 30 December). Cards advance by themselves, hold to pause, tap to move. A local notification on the 1st of the month at 09:00 (on 1 January for the year) opens the recap and never shows amounts; the `Riepilogo del mese` switch in `Impostazioni` turns it off.
- Weekly, monthly or yearly recurring payments with optional end date. Due expenses are created when the app opens. A paused payment skips the occurrences missed while paused when it is resumed. Reminder notifications arrive at 09:00 on the due day.
- Voice, photo and PDF entry: create an expense draft from a spoken Italian phrase (`Inserimento vocale`), from a photo of a receipt or from a PDF using OpenAI with your own API key, with explicit confirmation in the expense editor.
- Home screen widgets: `Aggiungi` (add an expense by hand, by voice or from a photo, in four sizes), `Scorciatoie` (your four most used categories plus `Altra`), `Il mese` (total, remaining budget, comparison and category line) and `Il ritmo` (the same with Oggi's daily chart). A widget opens the app on the right screen, and the microphone and camera of `Aggiungi` open a panel over the launcher. The same entries are in the long-press menu of the app icon. Widgets follow the launcher's light and dark scheme and refresh after every saved change, a restored backup included.
- Settings are grouped as `Dati` (recurring payments, `Backup ed export`), `Notifiche` (`Riepilogo del mese`), `Voce e foto` and `Informazioni` (version, `Licenze`, `Informativa sulla privacy`).
- Backup and export (`Backup ed export`): create a backup file (`centello-yyyy-MM-dd.backup`) and export every expense to CSV (`;` separated, UTF-8 with BOM, for Excel or Sheets). Both are saved in the Download folder (on Android 9 and older the share sheet is used instead). `Ripristina backup` picks a backup file, compares what the file and the phone hold, and replaces the phone's data in one transaction after confirmation.
- Dark and light theme following the Android setting, with portrait and phone landscape layouts.
- Application data is stored locally in SQLite. Centello does not require an account and has no backend or sync. Apart from Android Auto Backup to your own Google account, the only data that leaves the device is what you send with voice, photo or PDF entry (audio and text, a photo, or a PDF), and only when you enable it.

Android Auto Backup saves the database (and only the database) to your own Google account, so a new phone with the same account gets the expenses back. The OpenAI key, the reminder settings and the preferences are not backed up: enter the key again on the new phone, and the reminders are rebuilt at first launch. Storing receipt photos, localization, and configurable budget alerts are not part of the current version. The roadmap is tracked in [GitHub issues](https://github.com/christiancapoccetti/centello/issues).

## Home screen widgets

Widgets compute nothing: their words and numbers come from the same Application services as the Home (`HomeOverviewService`, `CategoryChoiceService`).

- **Opening the app:** a tap sends a typed `AppEntry` link in the intent data to `MainActivity`, and `AppEntryRouter` opens it once the Shell can navigate. An expense editor opened from a widget returns to the home screen when it is saved or closed.
- **Refresh:** `StoreChangeInterceptor` (EF Core) signals after every committed write and `HomeScreen` redraws every widget, coalescing signals. It also redraws at midnight, on time and time-zone changes and when the app window activates.
- **Look:** widget words are drawn in Archivo as tinted masks, and colours are `@color/theme_*` resources generated from `ThemeTokens`, so the scheme switches without the app running. See [DESIGN.md](DESIGN.md).

## Design

The visual system (colours, Archivo typography, components) is recorded in [DESIGN.md](DESIGN.md). The approved prototype of every screen is [docs/mockups/centello-prototype.html](docs/mockups/centello-prototype.html), and the owner's mark and Play Store icon are in `docs/brand/`.

## Architecture

The solution follows a pragmatic Clean Architecture approach with MVVM and the Result Pattern:

- `Centello.Domain`: entities, value objects, and domain rules.
- `Centello.Application`: use cases, repository contracts, and application services. `RecurringPaymentAutomation` runs behind an exclusive gate so a restore cannot overlap with creating due expenses. `Recap/` builds the recap decks (`RecapService`, one file per card under `Recap/Cards`, thresholds in `RecapRules`) and `RecapNoticeAutomation` decides the notification, behind ports.
- `Centello.Infrastructure`: EF Core and SQLite persistence. `SqliteDataBackup` is the only code besides EF that touches the database files (backup copy with `VACUUM INTO`, validation and migration on a private copy, replacement in one transaction); it signals the store change listener itself after a restore, since the copy bypasses EF. Editor view models share one window-lifetime `DbContext`, so `RepositoryBase` clears the change tracker after every save.
- `Centello.App`: .NET MAUI Android UI and view models. `Platforms/Android/Widgets` holds the widgets and `Platforms/Android/HomeScreen.cs` the one place that redraws them and publishes the launcher shortcuts; `Platforms/Android/Panel` holds the panel over the launcher, which hosts the voice and the photo capture; `RecapPage` plays the story and `AndroidRecapNoticeBoard` with `RecapNoticeReceiver` own the one alarm that posts the notice (re-armed on activation, boot, package update and time or zone change).
- `tests`: domain, application, and infrastructure test projects.

## Prerequisites

- .NET 10 SDK.
- .NET MAUI Android workload: `dotnet workload install maui-android`.
- Android SDK and a compatible JDK.
- An Android device with USB debugging enabled, or an Android emulator.

## Build and run on Android

Restore the solution and run the tests:

```powershell
dotnet restore Centello.slnx
dotnet test Centello.slnx --no-restore
```

If the Android SDK and JDK are already discoverable by the .NET workload, build and run the app on the connected device with:

```powershell
dotnet build src/Centello.App/Centello.App.csproj -t:Run -f net10.0-android
```

Otherwise, provide their locations explicitly:

```powershell
dotnet build src/Centello.App/Centello.App.csproj -t:Run -f net10.0-android `
  -p:AndroidSdkDirectory="<ANDROID_SDK_PATH>" `
  -p:JavaSdkDirectory="<JDK_PATH>"
```

Confirm that the target device is available with `adb devices` before running the app. The minimum supported Android API level is 24.

Debug builds use the development identity by default: the launcher shows `Centello Dev`, and Android installs it as `it.christiancapoccetti.centesimo.dev`. The long-press shortcuts, widgets and launcher panel use the same development identity. Development data stays on the Android device in the development app's private database and preferences, separate from the original Play app and its data.

To create a Release-configuration development APK, use `DevBuild=true`:

```powershell
dotnet publish src/Centello.App/Centello.App.csproj -c Release -f net10.0-android -p:DevBuild=true
```

The APK is written under `src/Centello.App/bin/Dev/Release/net10.0-android/publish/`. Install it with:

```powershell
adb install -r src/Centello.App/bin/Dev/Release/net10.0-android/publish/it.christiancapoccetti.centesimo.dev-Signed.apk
```

Development APKs use the workstation's Android debug key. The same key is sufficient to install updates, and Debug and Release development builds share the same app ID and device data. Google Play remains the original `it.christiancapoccetti.centesimo` app; build it using the Play instructions below.

## Build for Google Play

Google Play receives a signed Android App Bundle (AAB); Play App Signing re-signs the app delivered to users. The automated workflow expects an existing Play app and closed testing track. Complete any initial app or track setup manually if Play Console requires it. A normal Release build remains a locally debug-signed APK and cannot update a Play installation.

### One-time release setup

1. For an existing Play app, keep using its current upload key and closed testing track. Create a replacement key only if setting up a new app. The workflow targets the API track identifier `alpha` when `PLAY_CLOSED_TRACK` is explicitly set to `alpha`; configure this variable to the exact identifier of an existing closed track.
2. In Google Cloud, enable the Android Publisher API (`androidpublisher.googleapis.com`) and IAM Service Account Credentials API (`iamcredentials.googleapis.com`) for the service account's project. The pinned `google-github-actions/auth` action generates an access token from the JSON key, so grant that service account `roles/iam.serviceAccountTokenCreator` on itself. Add it as a Play Console user with **Release apps to testing tracks** permission and access to this app.
3. Create the GitHub Actions environment before setting environment secrets. From the repository root, run `gh auth login`, then:

   ```powershell
   gh api --method PUT repos/{owner}/{repo}/environments/play-closed-beta
   ```

   Replace `{owner}/{repo}` with the repository slug. On a fresh repository this creates the environment used by the release workflow. In **Settings → Environments → play-closed-beta**, set **Deployment branches and tags** to **Selected branches and tags**, then allow only the `v*` tag pattern.
4. If this is a new app without an upload key, create one outside the repository and keep a secure backup of its file and password. For an existing app, use its existing upload key:

   ```powershell
   keytool -genkeypair -v -keystore "$HOME\keys\play-upload.jks" -alias upload -keyalg RSA -keysize 4096 -validity 10000
   ```

5. Add these two **environment secrets** to `play-closed-beta`: `PLAY_UPLOAD_KEYSTORE_BASE64` (the upload keystore encoded as one-line Base64) and `PLAY_UPLOAD_PASSWORD` (the keystore password). Stream the keystore to `gh`; set the password using its interactive masked prompt:

   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("$HOME\keys\play-upload.jks")) | gh secret set PLAY_UPLOAD_KEYSTORE_BASE64 --env play-closed-beta
   gh secret set PLAY_UPLOAD_PASSWORD --env play-closed-beta
   ```

6. Add the service account's JSON key as environment secret `PLAY_SERVICE_ACCOUNT_JSON`, streamed from its downloaded file:

   ```powershell
   Get-Content -Raw .\play-service-account.json | gh secret set PLAY_SERVICE_ACCOUNT_JSON --env play-closed-beta
   ```

   Store that JSON file securely and remove the local copy when setup is complete.
7. Set these **environment variables**. Set `PLAY_VERSION_CODE_BASE` so its sum with the next GitHub Actions run number is greater than every version code already used on any Play track. For an existing app whose highest code is `2`, set the base to `2`; a first workflow run numbered `1` then publishes code `3`. Set `PLAY_CLOSED_TRACK` explicitly to `alpha` for the Alpha track:

   ```powershell
   gh variable set PLAY_VERSION_CODE_BASE --env play-closed-beta --body 2
   gh variable set PLAY_CLOSED_TRACK --env play-closed-beta --body alpha
   ```

8. Protect release tags matching `v*` with a repository ruleset: restrict tag creation to trusted release maintainers, and prevent updates and deletion. Only a trusted publisher should create and publish releases for verified commits on `main`.

### Publish a closed-testing release

Pull requests and pushes to `main` show test results in the logs and coverage in the run summary. CI on `main` provides a **Rilascia app** link for its verified commit. Create and publish a GitHub Release from that commit using a tag matching `vMAJOR.MINOR.PATCH` or `vMAJOR.MINOR.PATCH-beta.N` (for example, `v1.2.0-beta.1`). The tag must still point to the current `main` commit when signing, deploying, and retrying, and must have a successful push run of CI. Publish the release; drafts do not trigger deployment. The workflow builds an upload-key-signed AAB, checks package identity and artifact integrity, and publishes it as a completed release to `PLAY_CLOSED_TRACK` with Italian release notes. Enter 1–500 Unicode characters in the release description; the workflow trims surrounding whitespace and rejects an empty or overlength description. Do not edit the release notes after signing; changed notes require a deliberate new release with a new tag.

The workflow retains its signed bundle for 3 days and reuses it on retries. If a run fails, rerun that same workflow run so it keeps the version code and bundle. A retry after Play accepted the release checks the existing bundle and completed track release, avoiding a duplicate upload. A paused Play release is not resumed by retry; handle it deliberately with a new release. Do not create a second release with the same tag to retry.

### Manual fallback

For an intentional manual upload, use the local upload key and Play Console. Raise `ApplicationVersion` before each upload. Keep the upload key path and password out of the repository and shell history. Set them in the environment, then publish:

```powershell
Remove-Item -Recurse -Force src\Centello.App\bin\Release, src\Centello.App\obj\Release -ErrorAction SilentlyContinue
$env:PLAY_UPLOAD_KEYSTORE = "$HOME\keys\play-upload.jks"
$env:PLAY_UPLOAD_PASSWORD = Read-Host "Upload key password"
dotnet publish src/Centello.App/Centello.App.csproj -c Release -f net10.0-android -p:PlayRelease=true
```

The signed bundle is `src/Centello.App/bin/Release/net10.0-android/publish/it.christiancapoccetti.centesimo-Signed.aab`; the `.aab` without `-Signed` is unsigned. Check the signer with `keytool -printcert -jarfile <bundle>` before uploading. Before a manual upload, test the Release build end to end on a phone, check Play's current target SDK and 16 KB memory page requirements, and re-check `THIRD_PARTY_NOTICES.md` against bundled packages after dependency or .NET SDK changes. The privacy policy is published at <https://christiancapoccetti.github.io/centello/privacy-policy.html>, and Play Console listing and policy answers are in [docs/play-console.md](docs/play-console.md).

## Voice, photo and PDF entry with OpenAI

Voice entry turns a phrase such as "ieri 12 euro di pizza" into an expense draft; photo and PDF entry do the same from a receipt photo or a PDF. It is optional and uses your own OpenAI API key.

- **Setup:** enter the key in Settings, under "Voce e foto". A consent dialog must be accepted before the key is saved; the key can be removed at any time.
- **Key handling:** the key is stored in Android SecureStorage and sent only to the OpenAI API. It lives on the client, so it is not a server-held secret: use it for personal use only.
- **Flow:** hold the microphone button (up to 20 seconds). Audio is transcribed with `gpt-transcribe`, and the text is then interpreted by `gpt-6-luna` with a strict JSON schema that only admits your own category and tag keys, and the result is validated deterministically.
- **Photo and PDF flow:** the camera segment of the home dock opens a menu: "Scatta una foto" (Android 13 and later only), "Scegli dalla galleria" and "Scegli un PDF" (up to 5 MB). A photo is scaled to 1600 px and re-encoded as JPEG without EXIF or GPS data. The photo or PDF goes to `gpt-6-luna` in a single call, with the same strict schema as voice. If the document cannot be read, the editor opens empty with today's date.
- **What is sent to OpenAI:** the recorded audio (kept in memory, not stored by the app), the phrase text, the photo or PDF you chose, the names of your active categories and tags, and today's date. Existing expenses and the database are never sent. OpenAI may keep API inputs for abuse monitoring, so Centello can only promise that it does not save them itself.
- **Nothing is stored by Centello:** the copy of a photo or PDF placed in the app cache is deleted before and after each pick, and your own files are never modified or deleted. No storage permission is needed; only the camera permission is declared.
- **What stays local:** expenses, budgets, and the SQLite database. There is no backend and no sync; the only copy outside the phone is Android Auto Backup to your own Google account, which does not include the OpenAI key.
- **Entry panel:** the microphone of the `Aggiungi` widget (or the `Spesa a voce` shortcut) opens a panel that listens at once. A second tap sends the phrase and it is sent on its own at 20 seconds; `Annulla`, a tap outside, Back or leaving cancels. The camera (or `Spesa da foto`) opens the camera at once and then reads the photo over the launcher. Oggi's hold-to-talk is unchanged.
- **Confirmation:** the draft opens in the expense editor (with a "Dalla foto · controlla e salva" or "Dal PDF · controlla e salva" banner for documents) and nothing is saved until you save it; saving still requires a category.
- **Availability:** the microphone and camera segments are disabled until a key is configured (both lead to Settings), and reports an error without an Internet connection. Manual entry always works.

## License

Copyright © 2026 Christian Capoccetti.

Centello is source-available under the [PolyForm Noncommercial License 1.0.0](LICENSE): you may read, use, change and share the code for noncommercial purposes only. Commercial use is not permitted.

The name "Centello" and the logo in `docs/brand/` are not licensed.

Versions published up to the commit that adopted this license remain available under the GNU General Public License v3.0.

Contributions are accepted only under the terms in [CONTRIBUTING.md](CONTRIBUTING.md).
