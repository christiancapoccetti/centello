---
title: Centello privacy policy
---

# Centello privacy policy

Last updated 22 November 2026

Centello is an Android app for tracking personal expenses. Christian Capoccetti develops it as a personal project. You can record expenses by hand without an account, an internet connection, or an OpenAI key. Centello has no developer-operated server, advertising, analytics, or tracking.

## Contact

For privacy questions or requests, use the contact email under **App support** on the Centello listing. For general questions, you can also open a public GitHub issue. Keep personal information, receipts, and API keys out of public issues. [Centello on Google Play](https://play.google.com/store/apps/details?id=it.christiancapoccetti.centesimo) · [GitHub Issues](https://github.com/christiancapoccetti/centello/issues)

If you contact the developer, the developer uses your contact details and message to respond to and handle your request.

## Data on your device

Centello stores your expense records, categories, tags, budgets, and recurring payments in a database on your phone. The expense records can include amounts, dates, notes, and purchase history. Centello uses this information to show your expenses, reports, budgets, and widgets. Android stores app settings on your phone.

If you enter an OpenAI API key, Centello saves it in Android's protected storage. Centello uses the key to authenticate requests to OpenAI.

## Optional entry with OpenAI

To enable voice, photo, and PDF entry, enter your own OpenAI API key and accept the notice in **Impostazioni**. When you use one of these features, Centello sends data directly to OpenAI over HTTPS.

| Action | What Centello sends |
| --- | --- |
| Dictate an expense | Up to 20 seconds of audio for transcription, with active category and tag names as hints, limited to 100 names. Centello then sends the transcript, the names of active categories and their active tags, and today's date to prepare an expense draft. |
| Take or pick a photo | The selected image, resized as needed and processed without retaining photo metadata, plus the names of active categories and their active tags and today's date. |
| Pick a PDF | The full PDF, plus the names of active categories and their active tags and today's date. |

Audio, receipts, invoices, and payment screenshots can contain financial or personal information, including amounts, purchases, merchant details, names, and information about other people. Choose what to record or send with this in mind. Your existing expense records and database are outside the OpenAI request.

Centello holds voice recordings in memory. Android's pickers and camera can create temporary copies of photos and PDFs in the app cache. Centello attempts to delete those copies after reading and before the next photo or PDF operation. The original files remain in their original locations.

OpenAI processes the requests under the terms that apply to your API account. Centello asks OpenAI to disable storage of the interpretation response. OpenAI can still retain interpretation inputs and outputs for abuse monitoring, depending on your account's data controls and OpenAI's policies. Check the documentation for the current periods and exceptions. [OpenAI API data controls](https://developers.openai.com/api/docs/guides/your-data) · [OpenAI Services Agreement](https://openai.com/policies/services-agreement/)

## Backup, device transfer, and export

- **Android cloud backup.** If Android backup is enabled and available, Android can back up the Centello database to your Google account and restore it under your device and account settings. [Android backup](https://support.google.com/android/answer/2819582)
- **Android device transfer.** If you choose a supported Android device-to-device migration, Android can transfer the database to the new device. Centello's backup rules include the database and its working journal. The API key and app cache are outside these backup and transfer rules. [Android Auto Backup](https://developer.android.com/identity/data/autobackup)
- **Backup files and CSV exports.** In **Impostazioni → Backup ed export**, you can create a copy of the database or export your expenses as CSV. On Android 10 and later, Centello saves the file to your Download folder. On earlier supported Android versions, Android opens the share sheet for you to choose a recipient or destination. Centello creates a local cache copy and attempts to remove it after saving to Download. A cache copy may remain after sharing or an interrupted save. Manage any exported copies in the places where you save or share them.

## Permissions and notifications

- **Microphone.** Centello uses it to record an expense you dictate.
- **Camera.** Centello uses it when you choose to take a photo of an expense. Android's pickers give Centello access to the photo or PDF you select.
- **Notifications.** Centello posts local recap and recurring payment reminders. Open Centello to view expense and payment details. You can control recap reminders in **Impostazioni** and notification display through Android settings, including what appears on your lock screen.
- **Internet and network state.** Centello uses these to reach OpenAI for the optional entry features. If you open this policy from the app, your browser loads it separately.
- **Run at startup.** Centello uses Android's startup signal to reschedule local reminders after a restart.

## Retention, deletion, and rights

You can view, edit, or delete expenses in Centello and export them as CSV. Clearing the app's data in Android settings or uninstalling Centello removes its local database from that device. Copies in Download, on another device, or in Android cloud backup are managed separately. Use your file manager, destination device, or Google backup settings to manage those copies.

To stop future OpenAI requests from Centello, remove the key in **Impostazioni → Voce e foto → Rimuovi**. To revoke the key itself, use your OpenAI account. Removing the key from Centello leaves past requests subject to OpenAI's retention and deletion policies. Direct requests about your OpenAI API account and its data to OpenAI. [OpenAI Help Center](https://help.openai.com/)

Where applicable, you can request access, correction, deletion, restriction, or portability of personal data and object to its processing. Use the developer contact for personal data the developer holds, such as support correspondence. For data held by Google or OpenAI, use the provider's own privacy controls or contact channel. You can lodge a complaint with your data protection authority. In Italy, that authority is the Garante per la protezione dei dati personali. [Garante guidance on exercising your rights](https://www.garanteprivacy.it/home/diritti/come-agire-per-tutelare-i-tuoi-dati-personali)

## Intended audience

Centello is intended for adults aged 18 and over.

## Changes

Changes take effect when published on this page. [Privacy policy history](https://github.com/christiancapoccetti/centello/commits/main/docs/privacy-policy.md)
