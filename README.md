# Centesimo

The app is shown to users as **Quattrino**, a working name; Centesimo remains the project and technical name (repository, namespaces, package `it.christiancapoccetti.centesimo`).

Centesimo is a simple Android expense tracker built with .NET MAUI, designed and developed by Christian Capoccetti. The product research, the design, the architecture and the structure of the code are the author's own work; AI coding assistants were used as tools to write and review the implementation under the author's direction. Its goal is to make expense entry quick while keeping the expense database on the device; the optional voice, photo and PDF entry sends what you speak, photograph or pick to OpenAI (see below).

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
- Settings are grouped as `Dati` (recurring payments, `Backup ed export`), `Notifiche` (`Riepilogo del mese`), `Voce e foto` and `Informazioni` (version, `Licenze`).
- Backup and export (`Backup ed export`): create a backup file (`centesimo-yyyy-MM-dd.backup`) and export every expense to CSV (`;` separated, UTF-8 with BOM, for Excel or Sheets). Both are saved in the Download folder (on Android 9 and older the share sheet is used instead). `Ripristina backup` picks a backup file, compares what the file and the phone hold, and replaces the phone's data in one transaction after confirmation.
- Dark and light theme following the Android setting; the app is locked to portrait.
- Application data is stored locally in SQLite. Centesimo does not require an account and has no backend or sync. Apart from Android Auto Backup to your own Google account, the only data that leaves the device is what you send with voice, photo or PDF entry (audio and text, a photo, or a PDF), and only when you enable it.

Android Auto Backup saves the database (and only the database) to your own Google account, so a new phone with the same account gets the expenses back. The OpenAI key, the reminder settings and the preferences are not backed up: enter the key again on the new phone, and the reminders are rebuilt at first launch. Storing receipt photos, localization, and configurable budget alerts are not part of the current version. See [FUTURE.md](FUTURE.md) for the roadmap.

## Home screen widgets

Widgets compute nothing: their words and numbers come from the same Application services as the Home (`HomeOverviewService`, `CategoryChoiceService`).

- **Opening the app:** a tap sends a typed `AppEntry` link in the intent data to `MainActivity`, and `AppEntryRouter` opens it once the Shell can navigate. An expense editor opened from a widget returns to the home screen when it is saved or closed.
- **Refresh:** `StoreChangeInterceptor` (EF Core) signals after every committed write and `HomeScreen` redraws every widget, coalescing signals. It also redraws at midnight, on time and time-zone changes and when the app window activates.
- **Look:** widget words are drawn in Archivo as tinted masks, and colours are `@color/theme_*` resources generated from `ThemeTokens`, so the scheme switches without the app running. See [DESIGN.md](DESIGN.md).

## Design

The visual system (colours, Archivo typography, components) is recorded in [DESIGN.md](DESIGN.md). The approved prototype of every screen is [docs/mockups/centesimo-prototype.html](docs/mockups/centesimo-prototype.html), and the owner's mark and Play Store icon are in `docs/brand/`.

## Architecture

The solution follows a pragmatic Clean Architecture approach with MVVM and the Result Pattern:

- `Centesimo.Domain`: entities, value objects, and domain rules.
- `Centesimo.Application`: use cases, repository contracts, and application services. `RecurringPaymentAutomation` runs behind an exclusive gate so a restore cannot overlap with creating due expenses. `Recap/` builds the recap decks (`RecapService`, one file per card under `Recap/Cards`, thresholds in `RecapRules`) and `RecapNoticeAutomation` decides the notification, behind ports.
- `Centesimo.Infrastructure`: EF Core and SQLite persistence. `SqliteDataBackup` is the only code besides EF that touches the database files (backup copy with `VACUUM INTO`, validation and migration on a private copy, replacement in one transaction); it signals the store change listener itself after a restore, since the copy bypasses EF. Editor view models share one window-lifetime `DbContext`, so `RepositoryBase` clears the change tracker after every save.
- `Centesimo.App`: .NET MAUI Android UI and view models. `Platforms/Android/Widgets` holds the widgets and `Platforms/Android/HomeScreen.cs` the one place that redraws them and publishes the launcher shortcuts; `Platforms/Android/Panel` holds the panel over the launcher, which hosts the voice and the photo capture; `RecapPage` plays the story and `AndroidRecapNoticeBoard` with `RecapNoticeReceiver` own the one alarm that posts the notice (re-armed on activation, boot, package update and time or zone change).
- `tests`: domain, application, and infrastructure test projects.

## Prerequisites

- .NET 10 SDK.
- .NET MAUI Android workload: `dotnet workload install maui-android`.
- Android SDK and a compatible JDK.
- An Android device with USB debugging enabled, or an Android emulator.

## Build and run on Android

Restore the solution and run the tests:

```powershell
dotnet restore Centesimo.slnx
dotnet test Centesimo.slnx --no-restore
```

If the Android SDK and JDK are already discoverable by the .NET workload, build and run the app on the connected device with:

```powershell
dotnet build src/Centesimo.App/Centesimo.App.csproj -t:Run -f net10.0-android
```

Otherwise, provide their locations explicitly:

```powershell
dotnet build src/Centesimo.App/Centesimo.App.csproj -t:Run -f net10.0-android `
  -p:AndroidSdkDirectory="<ANDROID_SDK_PATH>" `
  -p:JavaSdkDirectory="<JDK_PATH>"
```

Confirm that the target device is available with `adb devices` before running the app. The minimum supported Android API level is 24.

## Build for Google Play

Google Play takes an Android App Bundle (AAB) signed with the upload key; Play App Signing then re-signs the app it delivers. A plain Release build stays an APK signed with the local debug key, so an app installed from Play cannot be updated by a local build, nor the other way round.

Create the upload key once, outside the repository, and keep a backup of the file and its password (Google can reset a lost upload key, but it takes a support request):

```powershell
keytool -genkeypair -v -keystore "$HOME\keys\play-upload.jks" -alias upload -keyalg RSA -keysize 4096 -validity 10000
```

Before every upload, raise `ApplicationVersion` in `src/Centesimo.App/Centesimo.App.csproj`, then build the bundle with the key path and password in environment variables:

```powershell
$env:PLAY_UPLOAD_KEYSTORE = "$HOME\keys\play-upload.jks"
$env:PLAY_UPLOAD_PASSWORD = Read-Host "Upload key password"
dotnet publish src/Centesimo.App/Centesimo.App.csproj -c Release -f net10.0-android -p:PlayRelease=true
```

The signed bundle is `src/Centesimo.App/bin/Release/net10.0-android/publish/it.christiancapoccetti.centesimo-Signed.aab`. Tag the commit of every uploaded build. The privacy policy is published with GitHub Pages at <https://christiancapoccetti.github.io/centesimo/privacy-policy.html>.

## Voice, photo and PDF entry with OpenAI

Voice entry turns a phrase such as "ieri 12 euro di pizza" into an expense draft; photo and PDF entry do the same from a receipt photo or a PDF. It is optional and uses your own OpenAI API key.

- **Setup:** enter the key in Settings, under "Voce e foto". A consent dialog must be accepted before the key is saved; the key can be removed at any time.
- **Key handling:** the key is stored in Android SecureStorage and sent only to the OpenAI API. It lives on the client, so it is not a server-held secret: use it for personal use only.
- **Flow:** hold the microphone button (up to 20 seconds). Audio is transcribed with `gpt-transcribe`, and the text is then interpreted by `gpt-6-luna` with a strict JSON schema that only admits your own category and tag keys, and the result is validated deterministically.
- **Photo and PDF flow:** the camera segment of the home dock opens a menu: "Scatta una foto" (Android 13 and later only), "Scegli dalla galleria" and "Scegli un PDF" (up to 5 MB). A photo is scaled to 1600 px and re-encoded as JPEG without EXIF or GPS data. The photo or PDF goes to `gpt-6-luna` in a single call, with the same strict schema as voice. If the document cannot be read, the editor opens empty with today's date.
- **What is sent to OpenAI:** the recorded audio (kept in memory, not stored by the app), the phrase text, the photo or PDF you chose, the names of your active categories and tags, and today's date. Existing expenses and the database are never sent. OpenAI may keep API inputs for abuse monitoring, so Centesimo can only promise that it does not save them itself.
- **Nothing is stored by Centesimo:** the copy of a photo or PDF placed in the app cache is deleted before and after each pick, and your own files are never modified or deleted. No storage permission is needed; only the camera permission is declared.
- **What stays local:** expenses, budgets, and the SQLite database. There is no backend and no sync; the only copy outside the phone is Android Auto Backup to your own Google account, which does not include the OpenAI key.
- **Entry panel:** the microphone of the `Aggiungi` widget (or the `Spesa a voce` shortcut) opens a panel that listens at once. A second tap sends the phrase and it is sent on its own at 20 seconds; `Annulla`, a tap outside, Back or leaving cancels. The camera (or `Spesa da foto`) opens the camera at once and then reads the photo over the launcher. Oggi's hold-to-talk is unchanged.
- **Confirmation:** the draft opens in the expense editor (with a "Dalla foto · controlla e salva" or "Dal PDF · controlla e salva" banner for documents) and nothing is saved until you save it; saving still requires a category.
- **Availability:** the microphone and camera segments are disabled until a key is configured (both lead to Settings), and reports an error without an Internet connection. Manual entry always works.

## License

Copyright © 2026 Christian Capoccetti.

Centesimo is source-available under the [PolyForm Noncommercial License 1.0.0](LICENSE): you may read, use, change and share the code for noncommercial purposes only. Commercial use is not permitted.

The names "Centesimo" and "Quattrino" and the logo in `docs/brand/` are not licensed.

Versions published up to the commit that adopted this license remain available under the GNU General Public License v3.0.

Contributions are accepted only under the terms in [CONTRIBUTING.md](CONTRIBUTING.md).
