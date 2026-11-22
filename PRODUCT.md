# Product

<!-- impeccable:product-schema 1 -->

## Platform

android

## Users

Two real users today: the developer and their partner, each using Centello daily on their own Android phone. The app is published on Google Play. It is built as if it had a wider audience, so every flow, empty state and error must make sense to someone who did not write the app.

The core situation is logging a purchase right after it happens: phone in hand, often on the move, with no patience for forms. A second, calmer situation is reviewing the month or year to see where the money went.

## Product Purpose

Centello is a personal expense tracker. It exists to make recording an expense nearly effortless and to turn the recorded history into a clear picture of how spending changes over time.

Success means expenses actually get logged, because entry is fast enough that skipping it is not tempting, and that a glance at Today or Insight answers "how am I doing this month compared with before?"

## Positioning

- **Fastest entry.** Logging an expense takes seconds. Today is the home screen, and voice entry ("Inserimento vocale") turns a spoken Italian phrase such as "ieri 12 euro di pizza" into a pre-filled draft constrained to the user's own categories and tags. A photo of a receipt or a PDF does the same.
- **Spending awareness, not budget planning.** Centello shows trends, comparisons between periods and categories, and history-based insights. Category budgets exist but stay light; the app must not turn into a planning or alerting tool.

## Operating Context

- Single-handed phone use, often outdoors or in a shop, right after paying.
- Four tabs: `Oggi`, `Insight`, `Categorie`, `Impostazioni`. Navigation sits at the bottom in portrait and in a left rail on phones in landscape.
- Voice entry: hold the microphone for up to 20 seconds; typing a phrase was removed on purpose. The draft always opens in the expense editor, and nothing is saved without explicit confirmation.
- Photo and PDF entry: the camera segment of the home dock opens a menu to take a photo (Android 13 and later), choose one from the gallery or choose a PDF (up to 5 MB). Like voice, it opens a pre-filled draft in the expense editor, never saves on its own, and stores neither the photo nor the PDF.
- Recurring payments create due expenses when the app opens and send Android notifications for upcoming ones.
- Recap: every closed month and year can be replayed as full-screen story cards from Insight, computed on the device. A card appears only when the period has something to say (enough expenses and days, a record day, a real change), and habit cards read only manual expenses. One local notification on the 1st of the month (09:00, no amounts) invites to open it; it can be switched off in Settings, which is the only place that asks the notification permission.

## Capabilities and Constraints

- Categories with optional monthly budgets (create, edit, archive, restore); tags within a category; expenses with amount, date, category, optional tag and note; monthly history with edit and delete; monthly and yearly overviews; home screen widgets and launcher shortcuts to add an expense (by hand, voice or photo) and read the month from the launcher; recurring payments (weekly, monthly, yearly, optional end date); Insight for any month or year; a recap story of every closed month and year.
- Local-first by construction: SQLite on the device, no account, no backend, no sync. Android Auto Backup copies only the database to the user's own Google account (the OpenAI key is excluded); a backup file and a CSV of every expense can be saved in the Download folder, and a backup file can be restored after a preview. Apart from that, the only data that leaves the phone is the voice-entry audio and its transcript, or the photo or PDF chosen for entry, plus active category and tag names and today's date, sent to OpenAI with the user's own API key and only after explicit consent. Manual entry always works offline.
- Stack: .NET 10 MAUI, Android only (min API 24), MVVM, Clean Architecture, Result Pattern.
- User-facing text is Italian and currently hardcoded; amounts are in euros with Italian formatting (`486,20 €`).
- Not in scope yet: storing receipt photos, localization/English, configurable budget alerts, biometric lock.

## Brand Commitments

- Name: **Centello**. The mark (a warm-white C with a coral segment, supplied by the owner) lives in `docs/brand/` as SVG plus the 512 px Play Store icon; the launcher icon is the adaptive icon in `src/Centello.App/Platforms/Android/Resources/`.
- Voice: Italian, informal second person (`tu`), short and plain; privacy is stated factually ("Restano privati sul dispositivo"), never as a marketing claim.

## Evidence on Hand

- Product docs: `README.md`, `DESIGN.md` (visual system) and `docs/mockups/centello-prototype.html` (approved prototype of every screen). The roadmap lives in GitHub issues.
- Use verified sources for reviews, testimonials and download counts.

## Product Principles

1. **Entry speed is the product.** Any change that adds a tap or a field to logging an expense has to justify itself against that cost.
2. **Show, don't plan.** Help people notice how their spending changes. Do not push budgets, thresholds or guilt.
3. **The user confirms.** Automation (voice entry, recurring payments) proposes; nothing ambiguous is saved silently, and a restore shows what the phone would lose before it replaces anything.
4. **Local by default.** Data stays on the phone. Anything that would leave it is opt-in, explained in plain Italian, and reversible.
5. **Built for an audience of strangers.** Even with two users, every screen should be clear to someone who has never seen the app before.

## Accessibility & Inclusion

- Touch targets of at least 48 × 48 dp.
- Icons support text and are never the only way to understand something; charts carry text alternatives (for example, "Andamento giornaliero: picco di 74,50 € il 18 luglio").
- Bottom-navigation labels must stay visible on narrow screens, and the bar respects the bottom safe area.
- Screen-reader descriptions are a known gap.
