# Centesimo

Centesimo is a simple Android expense tracker built with .NET MAUI. Its goal is to make expense entry quick while keeping the expense database on the device; the optional voice entry sends the phrase you speak to OpenAI (see below).

## Features

- Home (`Oggi`): month total, remaining budget, comparison with the previous period, category line, daily spending chart, latest expenses and a split button to add an expense by hand or by voice.
- Add, edit and delete expenses with an amount, date, category, optional tag and optional note. The `Spese` screen lists the expense history by month.
- Create, edit, archive and restore categories with optional monthly budgets, and tags within a category. Archived categories are unavailable for new expenses, while existing expenses keep their category. On first run the app suggests common categories to create in one tap.
- Each category gets a pastel colour from the app (the least-used slot of the palette, not chosen by the user) and an icon from a 32-icon catalog.
- Category detail with tag filters, and `Insight` for any month or year: totals, comparisons between periods and categories, and history-based insights.
- Weekly, monthly or yearly recurring payments with optional end date. Due expenses are created when the app opens. A paused payment skips the occurrences missed while paused when it is resumed. Reminder notifications arrive at 09:00 on the due day.
- Voice entry (`Inserimento vocale`): create an expense draft from a spoken Italian phrase using OpenAI with your own API key, with explicit confirmation in the expense editor.
- Settings are grouped as `Dati` (recurring payments, import from Money Manager), `Inserimento vocale` and `Informazioni` (version, `Licenze`).
- One-time import from a Money Manager `.mmbackup` file, with preview and no duplication.
- Dark and light theme following the Android setting; the app is locked to portrait.
- Application data is stored locally in SQLite. Centesimo does not require an account and has no backend or sync. The only data that leaves the device is the audio or text of a voice-entry phrase, and only when you enable voice entry.

Receipt photos, localization, backups, and configurable budget alerts are not part of the current version. See [FUTURE.md](FUTURE.md) for the roadmap.

## Design

The visual system (colours, Archivo typography, components) is recorded in [DESIGN.md](DESIGN.md). The approved prototype of every screen is [docs/mockups/centesimo-prototype.html](docs/mockups/centesimo-prototype.html), and the owner's mark and Play Store icon are in `docs/brand/`.

## Architecture

The solution follows a pragmatic Clean Architecture approach with MVVM and the Result Pattern:

- `Centesimo.Domain`: entities, value objects, and domain rules.
- `Centesimo.Application`: use cases, repository contracts, and application services.
- `Centesimo.Infrastructure`: EF Core and SQLite persistence.
- `Centesimo.App`: .NET MAUI Android UI and view models.
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

## Voice entry with OpenAI

Voice entry turns a phrase such as "ieri 12 euro di pizza" into an expense draft. It is optional and uses your own OpenAI API key.

- **Setup:** enter the key in Settings, under "Inserimento vocale". A consent dialog must be accepted before the key is saved; the key can be removed at any time.
- **Key handling:** the key is stored in Android SecureStorage and sent only to the OpenAI API. It lives on the client, so it is not a server-held secret: use it for personal use only.
- **Flow:** hold the microphone button (up to 20 seconds). Audio is transcribed with `gpt-transcribe`, and the text is then interpreted by `gpt-6-luna` with a strict JSON schema that only admits your own category and tag keys, and the result is validated deterministically.
- **What is sent to OpenAI:** the recorded audio (kept in memory, not stored by the app), the phrase text, the names of your active categories and tags, and today's date. Existing expenses and the database are never sent.
- **What stays local:** expenses, budgets, and the SQLite database. There is no backend and no sync.
- **Confirmation:** the draft opens in the expense editor and nothing is saved until you save it; saving still requires a category.
- **Availability:** voice entry is unavailable until a key is configured, and reports an error without an Internet connection. Manual entry always works.

## License

Centesimo is licensed under the [GNU General Public License v3.0](LICENSE).
The corresponding source code for distributed APKs is available as described in [SOURCE_OFFER.md](SOURCE_OFFER.md).
