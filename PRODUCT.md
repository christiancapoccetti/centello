# Product

## Platform

android

## Users

Centello is for people who record their daily expenses on an Android phone. Its flows, empty states and errors must make sense to someone opening the app for the first time.

The core situation is logging a purchase right after it happens: phone in hand, often on the move, with no patience for forms. A second, calmer situation is reviewing the month or year to see where the money went.

## Product Purpose

Centello is a personal expense tracker. It exists to make recording an expense nearly effortless and to turn the recorded history into a clear picture of how spending changes over time.

Success means people log their expenses, check how much is left in each category on **Oggi**, and see how their spending changed in **Insight**.

## Positioning

- **Fastest entry.** Logging an expense takes seconds. Today is the home screen, and voice entry ("Inserimento vocale") turns a spoken Italian phrase such as "ieri 12 euro di pizza" into a pre-filled draft constrained to the user's own categories and tags. A photo of a receipt or a PDF does the same.
- **Spending awareness.** Centello shows how spending compares across periods and categories. Optional category budgets show how much is left or how far spending exceeded the limit.

## Operating Context

- Single-handed phone use, often outdoors or in a shop, right after paying.
- Four tabs: `Oggi`, `Insight`, `Categorie`, `Impostazioni`. Navigation sits at the bottom in portrait and in a left rail on phones in landscape.
- Voice entry: tap the microphone on Today to start recording. The button shows a stop icon and the hint "In registrazione · Tocca per fermare". Tap it again to prepare a draft, or wait for recording to stop automatically at 20 seconds, as in the launcher panel. The draft opens in the expense editor for review, and nothing is saved without explicit confirmation. Typing a phrase was removed on purpose.
- Photo and PDF entry: the camera segment of the home dock opens a menu to take a photo (Android 13 and later), choose one from the gallery or choose a PDF (up to 5 MB). Like voice, it opens a pre-filled draft in the expense editor, never saves on its own, and stores neither the photo nor the PDF.
- Recurring payments create due expenses when the app opens. When Android allows notifications, Centello schedules a reminder for 09:00 local time on the due date. Android can delay delivery.
- Recap: every closed month and year can be replayed as full-screen story cards from Insight, computed on the device. A card appears only when the period has something to say (enough expenses and days, a record day, a real change), and habit cards read only manual expenses. People can opt in to the recap notification in Settings. Centello schedules it for 09:00 local time on the first of the month, and Android can delay delivery. The notification omits amounts.
- The spending map appears before the final recap card in every monthly or annual recap with spending. Each flow starts from a spending type present in the period and passes through its category. Expenses without an identifiable tag appear under `Senza tag`. The story pauses on the map. Tapping the left half goes back one card and tapping the right half opens the final recap card. In landscape, people can scroll through the full map and zoom in to explore it in both directions. People can preview and save or share a PNG rendered on the device. Each preview starts with percentages only and offers the option to include amounts.

## Capabilities and Constraints

- People can create, edit, archive, and restore categories with optional monthly budgets. Expenses record an amount, date, category, and optional tag and note. People can manage tags within a category and edit or delete expenses from monthly history. Recurring payments can repeat weekly, monthly, or yearly and can have an end date.
- **Oggi** shows category spending and budget progress for the selected month or year. The yearly limit is twelve times the monthly budget. **Budget per categoria** shows the current month on the Android home screen. Other widgets and launcher shortcuts can show a month overview or start manual, voice, or photo entry.
- **Insight** compares spending for a month or year with the previous comparable period. People see category share rows inline, with an option to reveal remaining rows when there are more than three. They can switch separately between cumulative spending and signed euro category changes. Closed months and years have a recap.
- Local-first by construction: SQLite on the device, no account, no backend, no sync. Android Auto Backup copies only the database to the user's own Google account. People can save or share a backup file or CSV export. A backup can be restored after a preview. Assisted entry requires explicit opt-in and sends recorded audio, a photo, or a PDF to Google through Firebase AI Logic, plus active category and tag names and today's date. The editor lets the user review the draft before saving. Manual entry always works offline.
- Stack: .NET 10 MAUI, Android only (min API 24), MVVM, Clean Architecture, Result Pattern.
- User-facing text is Italian and currently hardcoded; amounts are in euros with Italian formatting (`486,20 €`).
- Not in scope yet: storing receipt photos, localization/English, configurable budget alerts, biometric lock.

## Brand Commitments

- Name: **Centello**. The mark (a warm-white C with a coral segment, supplied by the owner) lives in `docs/brand/` as SVG plus the 512 px Play Store icon; the launcher icon is the adaptive icon in `src/Centello.App/Platforms/Android/Resources/`.
- Voice: Italian, informal second person (`tu`), short and plain; privacy is stated factually ("Restano privati sul dispositivo"), never as a marketing claim.

## Evidence on Hand

- The README covers app features, and DESIGN.md describes the visual system. The original prototype lives in `docs/mockups/centello-prototype.html`. The roadmap lives in GitHub issues.
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
- When a screen reader is active, recap cards advance through previous and next buttons. Focus moves to the card title, which includes its position in the story.
