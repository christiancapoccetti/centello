---
title: Quattrino privacy policy
---

# Quattrino privacy policy

Last updated: 21 November 2026

Quattrino is an Android app for tracking personal expenses, developed by Christian Capoccetti as a personal project. This policy explains what data the app uses, where it goes and what choices you have.

## In short

- Your expenses stay on your phone. The app has no server and the developer receives none of your data.
- Only if you use voice, photo or PDF entry is what you say, photograph or pick sent to OpenAI, using your own API key.
- No ads, no accounts, no analytics or tracking tools.

## Who develops the app and how to get in touch

Christian Capoccetti. For questions or requests about this policy, open an issue on [GitHub Issues](https://github.com/christiancapoccetti/centesimo/issues).

GitHub issues are public: do not include personal data. If your request needs it, the reply will point you to a private channel.

## Data that stays on your phone

- Expenses, categories, tags, budgets, recurring payments and settings are stored in a database on your phone.
- Your OpenAI API key, if you enter one, is stored in Android's protected storage and used only to call OpenAI.

## What is sent to OpenAI, and when

Data is sent only when you use voice, a photo or a PDF, after you have entered your API key and accepted the consent notice. What is sent:

- the audio you record, up to 20 seconds, kept in memory and never saved by the app;
- the text transcribed from that audio;
- the photo you take or pick, scaled down and stripped of EXIF data and GPS location, or the PDF you pick;
- the names of your active categories and tags and today's date, so the expense can be interpreted.

Your other expenses and the database are never sent. Data is encrypted in transit (HTTPS).

Because the request uses your own key, it falls under your relationship with OpenAI and its terms and [privacy policy](https://openai.com/policies/). OpenAI may keep data received through its API for a limited time to monitor abuse. Quattrino does not save it.

Manual entry always works, without a key and without a connection.

## Backups

- **Android automatic backup:** if it is turned on for your phone, Android saves a copy of the expense database to your Google account. Your OpenAI API key is not included. The backup is handled by Google according to your account settings.
- **Backup files and CSV exports:** you create them, and they are saved to your phone's Download folder. They remain under your control.

## Permissions

- **Microphone:** only while you dictate an expense.
- **Camera:** only to photograph an expense, such as a receipt or an invoice.
- **Notifications:** for the month and year recap. The notification never shows amounts, and you can turn it off in the app's settings.
- **Internet and network state:** only to communicate with OpenAI.
- **Run at startup:** to reschedule the recap notification after the phone restarts.

## Retention and deletion

Your data stays on your phone until you delete it. You can delete single expenses, remove your API key in the settings, or uninstall the app, which deletes the database from your phone. The backup in your Google account is managed from your Google settings.

## Your rights

The developer does not receive your data, so there is no data held by the developer to show, correct or delete: you control it directly on your phone. For data sent to OpenAI, you can contact OpenAI. You can always lodge a complaint with your data protection authority; in Italy, the [Garante per la protezione dei dati personali](https://www.garanteprivacy.it/).

## Children

Quattrino is not intended for children under 14.

## Changes

Any change to this policy will be published on this page with an updated date. The version history is in the [project repository](https://github.com/christiancapoccetti/centesimo/commits/main/docs/privacy-policy.md).
