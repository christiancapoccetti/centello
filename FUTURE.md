# Future Implementations

This document tracks ideas intentionally excluded from the initial release of Centesimo.
Items are not commitments and will be prioritized after the core manual expense workflow is stable.

## Pre-release

What must be decided or checked before Centesimo is published on Google Play, even only for family and friends. Play Console rules change often: confirm thresholds and deadlines there before acting.

### Business model

- Define the business model (free, donations, paid app, or optional paid features). It decides whether a Play payments profile and Google Play Billing are needed, and what the store listing promises.
- PolyForm Noncommercial 1.0.0 bars others from commercial use, not the author, who can still sell the app. Contributions come under `CONTRIBUTING.md`, so they can ship in a paid version too. Versions published before the change stay under GPL-3.0, which allows their commercial reuse.

### Logos, name and copyright

- Confirm that the mark, the launcher icon and the Play Store icon in `docs/brand/` are original work owned by the author.
- The app keeps the name `Centesimo` for the closed beta only. Before a public release it needs a new name, chosen last, with a logo that fits it (the current mark is a "C"): `Centesimo` clashes with the UIBM marks "Il Centesimo" of CDS SPA (classes 16, 35-45) and their "il Centesimo" app on Google Play, and `Quattrino` with other finance apps. Before adopting a name, search it on the Play Store and the App Store and in the trademark registers (TMview: UIBM, EUIPO; classes 9, 36, 42), then rename the app, the privacy policy and the store listing.
- Re-check `THIRD_PARTY_NOTICES.md` against the bundled packages (`project.assets.json`) whenever dependencies or the .NET SDK change. Done on 2026-11-20: every bundled library is listed, the .NET Foundation is named as the holder of the .NET components, and the full MIT, BSD 2-Clause, Apache 2.0 and OFL texts plus the .NET runtime notices ship with the app.
- Tag the commit of every published build on GitHub, so every build points at the exact code it was made from.

### Google Play requirements

- Developer account: one-time fee and identity verification. The contact email becomes public, and the package name `it.christiancapoccetti.centesimo` cannot change after the first upload.
- Share with family through the **internal testing** track (up to 100 testers, available within minutes, no full review). A new personal account must run a closed test with at least 12 testers for 14 days before it can publish to production.
- Privacy policy: `docs/privacy-policy.md`, published with GitHub Pages and linked under `Informazioni` in Settings. GitHub Issues is the contact until a public contact email exists. It names the app, so update it with the new name.
- Play Console answers (trader status, store listing texts, app content, data safety, first release steps) are drafted in `docs/play-console.md`. Screenshots, the feature graphic and the 512 px icon wait for the new name and logo.
- Signed bundle and upload key: see "Build for Google Play" in the README. Increase `ApplicationVersion` for every upload.
- Check 16 KB memory page support for the native libraries (SQLite) in the Play Console App Bundle explorer, and keep the target SDK at the level Play requires (36 today).
- Test the Release build end to end before every upload: trimming can break EF Core at runtime.

### Screen sizes and performance

- Check every screen on several Android sizes and densities: a small phone (about 360 dp wide), a large phone, a tablet, the largest system font size and display size, and both themes. Text must not be clipped, the bottom navigation labels must stay visible and the bar must respect the bottom safe area.
- Measure performance on the Release build on a low-end device: cold start time, scrolling in `Spese` and the Insight charts with several years of expenses (a backup file of real data is a realistic data set, restored only on a local test device).
- Review the Android vitals in the Play Console during internal testing (ANR, crashes, slow startup) before moving to a wider track.

### Product gaps for other users

- Voice, photo and PDF entry need the user's own OpenAI API key, which family members are unlikely to have: explain it in the listing and keep manual entry as the main path.

## Data portability

- Export expenses to CSV for a chosen period (today the export covers every expense).
- Include attachments in the backup and restore them, once attachments exist.
- Optional encrypted backup files.

## Home screen widgets

- Optional hiding of amounts in the widgets.
- Align Oggi's microphone consent with the panel's dialog.

## Ask Centesimo

- Spoken questions about spending ("Quanto ho speso al ristorante a ottobre?", "Spendo più in benzina quest'anno o l'anno scorso?"), answered with a large figure, one sentence and a small chart; a tap opens the expenses behind the figure.
- OpenAI only turns the question into a structured query (category, tag, period, kind of calculation) through the same constrained JSON schema as voice entry; the app computes the answer on the local database, so no expense leaves the phone.
- Limited set of calculations: total, comparison between two periods, largest expense, average. Questions outside it get a plain "non so rispondere" instead of a guess.
- The voice flow must tell a question from an expense: a field in the schema or two separate microphone modes.

## Gemini ("Ok Google")

- "Ok Google, aggiungi una spesa di 10 euro in Ristoranti su Centesimo": expose an `addExpense` function (amount, category name, optional tag, note and date) through Android AppFunctions, the API Gemini uses to call app features on Android 16 and later.
- Gemini does the language understanding, so this path needs no OpenAI key. The app matches the category name against its own active categories; anything missing or ambiguous opens the expense editor pre-filled instead of saving, as voice entry does.
- Blocked today: AppFunctions is an experimental Jetpack library (Kotlin, annotation processing), and Gemini calls third-party apps only in a limited preview for trusted testers. Centesimo would also need a small Kotlin library bound into the MAUI app. Re-check when Gemini opens AppFunctions to every app.
- App Actions (built-in intents in `shortcuts.xml`) belong to the retired Google Assistant and are not a path forward.

## AI providers and the API key

- Support providers other than OpenAI (for example Google Gemini or Mistral) for transcription and for reading photos and PDFs, behind the existing interpreter port. The user picks the provider in `Impostazioni` and enters that provider's key; each provider keeps the same consent and the same constrained output.
- A short in-app tutorial under `Inserimento vocale`, in plain Italian: what an API key is, how to create one on the provider's site step by step, that usage is billed by the provider to the user's own account (with an indicative cost per expense), that the key stays in Android SecureStorage and is sent only to that provider, and how to remove it from Centesimo and revoke it on the provider's site.
