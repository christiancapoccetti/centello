# Future Implementations

This document tracks ideas intentionally excluded from the initial release of Centesimo.
Items are not commitments and will be prioritized after the core manual expense workflow is stable.

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
