# Centesimo

Centesimo is a simple Android expense tracker built with .NET MAUI. Its goal is to make expense entry quick while keeping the expense database on the device; the optional quick entry sends the phrase you speak to OpenAI (see below).

## Features

- Create, edit, archive, and restore expense categories with optional monthly budgets. Archived categories are unavailable for new expenses, while existing expenses retain their category.
- Create and archive tags within a category.
- Add expenses with an amount, date, category, optional tag, and optional note.
- Browse monthly expense history, then edit or delete an expense.
- Review monthly or yearly spending and category budget progress from the home dashboard, with category details available for both periods.
- Configure weekly, monthly, or yearly recurring payments, with optional end dates. Due expenses are created when the app is opened, and upcoming payments generate Android notifications.
- Create an expense draft from a spoken Italian phrase using OpenAI with your own API key, plus explicit confirmation in the expense editor.
- Review local spending trends, category comparisons, and history-based insights for any month or year.
- Store application data locally in SQLite. Centesimo does not require an account and has no backend or sync. The only data that leaves the device is the audio or text of a quick-entry phrase, and only when you enable OpenAI quick entry.

Receipt photos, localization, backups, and configurable budget alerts are not part of the current version. See [FUTURE.md](FUTURE.md) for the roadmap.

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
- **Availability:** voice quick entry is unavailable until a key is configured, and report an error without an Internet connection. Manual entry always works.

## License

Centesimo is licensed under the [GNU General Public License v3.0](LICENSE).
The corresponding source code for distributed APKs is available as described in [SOURCE_OFFER.md](SOURCE_OFFER.md).
