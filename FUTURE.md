# Future Implementations

This document tracks ideas intentionally excluded from the initial release of Centesimo.
Items are not commitments and will be prioritized after the core manual expense workflow is stable.

## Pre-release

What must be decided or checked before Centesimo is published on Google Play, even only for family and friends. Play Console rules change often: confirm thresholds and deadlines there before acting.

### Business model

- Define the business model (free, donations, paid app, or optional paid features). It decides whether a Play payments profile and Google Play Billing are needed, and what the store listing promises.
- GPL-3.0 allows selling the app, but the source of every published build must stay available (see `SOURCE_OFFER.md`): factor that into any paid option.

### Logos, name and copyright

- Confirm that the mark, the launcher icon and the Play Store icon in `docs/brand/` are original work owned by the author.
- Search the name `Centesimo` on the Play Store and in the trademark registers (UIBM, EUIPO) for conflicting finance apps or marks.
- Re-check `THIRD_PARTY_NOTICES.md` against the bundled packages (`project.assets.json`) whenever dependencies or the .NET SDK change. Done on 2026-11-20: every bundled library is listed, the .NET Foundation is named as the holder of the .NET components, and the full MIT, BSD 2-Clause, Apache 2.0 and OFL texts plus the .NET runtime notices ship with the app.
- Tag the commit of every published build on GitHub, so the source offer points at the exact code.

### Google Play requirements

- Developer account: one-time fee and identity verification. The contact email becomes public, and the package name `it.christiancapoccetti.centesimo` cannot change after the first upload.
- Share with family through the **internal testing** track (up to 100 testers, available within minutes, no full review). A new personal account must run a closed test with at least 12 testers for 14 days before it can publish to production.
- Privacy policy on a public URL (for example GitHub Pages), in Italian. Required because the app records audio and sends it to OpenAI.
- Data safety form: voice audio, its transcript and the category and tag names are sent to OpenAI only when voice entry is enabled, encrypted in transit; expenses never leave the device.
- Financial features declaration: Centesimo tracks expenses and provides no financial services.
- Content rating questionnaire, target audience, and the ads declaration (no ads).
- Store listing: short and full description, at least two screenshots made with placeholder data, a 1024×500 feature graphic, and the 512 px icon already in `docs/brand/`.
- Ship a signed Android App Bundle (AAB) with Play App Signing. Keep the upload key outside the repository with a backup, and add `*.keystore` and `*.jks` to `.gitignore` (today only `*.pfx` is ignored).
- Increase `ApplicationVersion` for every upload.
- Check 16 KB memory page support for the native libraries (SQLite) in the Play Console App Bundle explorer, and keep the target SDK at the level Play requires (36 today).
- Test the Release build end to end before every upload: trimming can break EF Core at runtime.

### Product gaps for other users

- Backup file and restore (see Data portability): Android Auto Backup is on, but there is still no backup file you can keep and no restore screen, so uninstalling or leaving Google's backup loses every expense.
- Voice entry needs the user's own OpenAI API key, which family members are unlikely to have: explain it in the listing and keep manual entry as the main path.

## Accessibility and input

- Voice control for navigating common actions (expense entry by voice already exists as `Inserimento vocale`).
- Improved screen-reader descriptions and accessibility testing.
- Optional biometric protection when opening the app.

## Localization

- Move user-facing text to localization resources.
- Support English in addition to Italian.
- Format currency, dates, and numbers according to the selected locale.

## Attachments

- Capture or select receipt photos.
- Store attachments locally and manage their lifecycle.
- Optional receipt text recognition to prefill an expense.

## Data portability

- Export a local backup of application data.
- Export expenses to CSV (for spreadsheets), for a chosen period.
- Restore data and attachments from a backup.
- Optional encrypted backup files.

## Home screen widgets

- Quick-entry widget: add an expense by hand or by voice without opening the app first.
- Summary widget: month total, remaining budget and the comparison with the previous period.

## Insights

- Configurable budget alerts.
- "Una nuova abitudine" per tag, not only per category.
- Move the home and the category detail onto one shared spending model, and delete `SpendBuckets` and the home's own comparison.

## Categories and recurring payments

- Store the archive and pause dates; the UI currently shows only `Archiviata` and `In pausa`.

## Technical follow-ups

- Clear the DbContext change tracker after a failed save in `RepositoryBase`, so rejected entities do not linger in later saves.
- `CategoryListService` repeats the positive-budget check that `PeriodBudget` owns; use `PeriodBudget` there.
- `InsightsViewModel.EmptyTitle` still builds its wording from the period in the view model; move that wording next to the other period texts in `InsightTexts`.
