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
- Restore data and attachments from a backup.
- Optional encrypted backup files.

## Insights

- Next step: review the Insight section (content, structure and how it relates to the home and the category detail).
- Configurable budget alerts.

## Categories and recurring payments

- Store the archive and pause dates; the UI currently shows only `Archiviata` and `In pausa`.

## Technical follow-ups

- Clear the DbContext change tracker after a failed save in `RepositoryBase`, so rejected entities do not linger in later saves.
