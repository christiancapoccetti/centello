# Google Play Console answers

Use this guide to prepare the Centello listing and closed test for the existing package `it.christiancapoccetti.centesimo`. Compare these answers with the current form and the build being submitted. The README's “Build for Google Play” section covers the release workflow.

## Account

- **Trader status.** The current project is free, personal, and ad-free. The account owner must confirm the legal classification and complete the current Play declaration. [Play trader status guidance](https://support.google.com/googleplay/android-developer/answer/17594752)
- **Developer and support contact.** Verify the public developer email in Play and confirm that the address listed under **App support** receives privacy requests. [Play developer account contact requirements](https://support.google.com/googleplay/android-developer/answer/13628312)

## App details

- Default language is Italian (`it-IT`). Centello is a free app in the Finance category.
- Use the same public privacy policy URL in the app and the store listing. [Centello privacy policy](https://christiancapoccetti.github.io/centello/privacy-policy.html)
- Optional website. [Centello repository](https://github.com/christiancapoccetti/centello)

## Store listing in Italian

**Short description** (80 characters at most)

> Registra le spese a mano, a voce, da foto o PDF. Segui budget e pagamenti.

**Full description**

> Centello ti aiuta a tenere sotto controllo le spese senza fatica. Segna una spesa in pochi secondi e vedi subito come va il mese.
>
> **Inserisci come preferisci**
> • A mano, con importo, categoria, tag e nota.
> • A voce. Tieni premuto e di' «ieri 12 euro di pizza».
> • Da una foto o da un PDF. Fotografa uno scontrino o scegli una fattura.
>
> **Capisci dove vanno i soldi**
> • Budget mensili per categoria e quanto ti resta.
> • Il confronto con il mese precedente e l'andamento giorno per giorno.
> • Insight per mese e per anno.
> • Il riepilogo del mese e dell'anno, a schede da sfogliare.
> • Registrazione automatica dei pagamenti regolari, come affitto e abbonamenti.
> • Widget e scorciatoie per aggiungere una spesa dalla schermata home.
>
> **Scegli come gestire i dati**
> • L'inserimento a mano funziona offline. Centello salva le spese sul dispositivo.
> • Puoi creare un file di backup ed esportare le spese in CSV. Android gestisce il backup automatico e il trasferimento su un altro dispositivo.
> • Quando usi voce, foto o PDF, Centello invia il contenuto a OpenAI usando la tua chiave API. L'inserimento a mano funziona anche senza chiave.
> • Centello è senza account, pubblicità o tracciamento.

Use the 512 px icon in `docs/brand/icona-512.png`, a 1024 × 500 feature graphic, and at least two phone screenshots with synthetic data.

## App content

- **App access.** Manual entry requires no login. Voice, photo, and PDF entry require an authorized OpenAI API key. Follow the reviewer steps below and resolve access to the live integration before submission. [Play app access requirements](https://support.google.com/googleplay/android-developer/answer/15748846)
- **Ads.** None.
- **Content rating.** Complete the questionnaire for a utility app with optional AI expense entry. Answer any question about third-party transfers using the actual OpenAI payload and the wording of that question. The rating comes from the questionnaire. [Play content rating guidance](https://support.google.com/googleplay/android-developer/answer/9859655)
- **Target audience.** Ages 18 and over, consistent with the privacy policy.
- **News, government, health, and COVID-19.** None of these categories applies.
- **Financial features.** Centello records expenses, tracks budgets, and reminds users of recurring payments. It does not initiate transactions or offer banking, loans, investment, or financial advice. Confirm with Play whether expense tracking and budgeting fall under **Other** in the Financial features declaration. [Play Financial features declaration](https://support.google.com/googleplay/android-developer/answer/13849271)

## App access instructions for reviewers

The app interface is in Italian. Use only made-up expenses and documents for these checks.

### Manual entry

1. Open **Oggi**. On a fresh install, tap **Crea categorie** to create the selected starter categories.
2. Tap **Aggiungi spesa**. Enter `12,50`, select a category, and enter **Spesa di prova** in **Nota**.
3. Tap **Salva spesa**. The expense appears on **Oggi**. Open **Insight** to see it included in the totals.
4. To test export, open **Impostazioni → Backup ed export → Esporta spese in CSV**. Find the export in Download on Android 10 or later. On Android 8 or 9, choose a destination in the Android share sheet.

### Live voice, photo, and PDF entry

The following steps require an internet connection and an individually authorized key for an OpenAI API project with billing, transcription access (`gpt-transcribe`), and Responses API access (`gpt-6-luna`). Use only a key you are authorized to use. [OpenAI API key guidance](https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety)

1. In **Impostazioni → Voce e foto**, enter the key in **Chiave API OpenAI** and tap **Salva chiave**. Review the data sent to OpenAI and tap **Acconsento** to enable the three features.
2. In **Oggi**, hold the microphone button and say “ieri dodici euro e cinquanta di pizza.” Release the button to send the audio. Grant microphone permission when asked, then hold the button again if recording has not started. Check the amount and date in the proposed expense.
3. For a photo or PDF, make a synthetic receipt with today's date and the text `Scontrino di prova. Pizzeria Esempio. Pizza. TOTALE EUR 12,50`. Save it as a gallery image and a PDF on the test device. Tap the camera icon in **Oggi**. Choose **Scegli dalla galleria** for the image and **Scegli un PDF** for the PDF. On Android 13 or later with a working camera, you can also tap **Scatta una foto** and photograph the same synthetic receipt. In each case, check the proposed draft and tap **Salva spesa** if you want to keep it.
4. To disable the integration, open **Impostazioni → Voce e foto → Rimuovi** and confirm.

### Open review dependency

Play requires reviewers to be able to access all app functionality. The live path currently depends on authorization and billing outside Centello. The manual scenario and sample inputs cover only the paths described above. Before submission, confirm an arrangement with Play that lets reviewers use the live features without purchasing API access or sending a personal key. If Play and OpenAI support an authorized test project with individual access, exercise the full procedure from a fresh install using that arrangement. The publisher must verify reviewer availability and compatibility with OpenAI's current API key guidance before submitting. Keep API keys out of the app bundle, store listing, and review instructions. [Play review access requirements](https://support.google.com/googleplay/android-developer/answer/15748846) · [OpenAI key safety](https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety)

## Data safety

Apply the Data safety definitions to every version still distributed under this package. An OpenAI transfer leaves the device even though Centello uses the user's own key and the developer receives no copy. Declare these optional transfers as **collected** for **App functionality**. [Play Data safety definitions](https://support.google.com/googleplay/android-developer/answer/10787469)

| Data type | What leaves the device |
| --- | --- |
| Audio files → Voice or sound recordings | Audio chosen for transcription. |
| Photos and videos → Photos | A receipt, invoice, or payment image the user takes or selects. |
| Files and docs | The PDF the user selects, including its contents. |
| App activity → Other user-generated content | The expense transcript and active category and tag names. The interpretation request also includes today's date. |
| Financial info → Purchase history | Purchases and transactions described in voice input or in receipts, invoices, and payment screenshots. |
| Financial info → Other financial info | Other financial details in the submitted material, such as amounts due on invoices or bills. |

Check typical receipts, invoices, and payment screenshots for additional types the form covers, such as personal names, addresses, or user payment info when documents show financial account details. Centello sends document contents, so the upload format alone does not determine which declarations apply. [Play data types](https://support.google.com/googleplay/android-developer/answer/10787469)

- **Sharing.** OpenAI receives the data under the user's own API account. Confirm whether the specific user action and the prominent in-app disclosure and consent qualify for Google's exception. Use **Shared: No** for those transfers only if the exception applies. Otherwise, declare the relevant types as shared as well. The BYOK arrangement alone does not establish that OpenAI is a service provider acting under the developer's instructions. [Play transfer exceptions](https://support.google.com/googleplay/android-developer/answer/10787469) · [Play User Data policy](https://support.google.com/googleplay/android-developer/answer/10144311)
- **Ephemeral processing.** Treat interpretation inputs as non-ephemeral. Centello sets `store: false` for Responses API calls, and abuse monitoring can still retain data. For voice recordings, verify whether the current transcription endpoint and applicable provider terms meet Play's ephemeral standard. OpenAI's endpoint table lists no abuse monitoring or application-state retention for transcription. The transcript goes through the separate interpretation request. [OpenAI API data controls](https://developers.openai.com/api/docs/guides/your-data)
- **Encryption in transit.** OpenAI calls use HTTPS. Verify the current form's encryption answer across all flows included in the declaration.
- **Cloud backup and migration.** Android transfers the database and its working journal through Google backup or device-to-device migration under the user's system settings. Centello has no app-operated backup endpoint. The app's Android backup rules exclude the key and cache. Verify how Play treats each system-managed transport on the submitted build before finalizing the form. [Android Auto Backup](https://developer.android.com/identity/data/autobackup)
- **Export, cache, and reminders.** Download exports stay on the device unless the user moves or shares them. On Android 8 and 9, an explicit choice in the Android share sheet can send a file to another app. Assess that flow under Play's user-initiated sharing exception. Temporary picker and export cache copies and Android's local recap and payment reminders make no network requests.
- **Deletion.** Users can delete expenses, clear local app data, or uninstall. Copies in Download, cloud backups, and on other devices have separate controls. Removing the key disables new requests from Centello. Send questions about API data retention or deletion to OpenAI. Centello has no app account to delete. Answer any separate deletion-request question based on the services and data in scope. [Centello privacy policy](privacy-policy.md)

## Before submission

- **Play Console checks.** Test the signed build from a clean install. Verify the in-app disclosures, backup and export routes, and both classes of notifications. Confirm the App access arrangement for the live integration, current Data safety answers, financial features, and content rating. Verify the privacy URL from a browser without an account and confirm that the public support address can receive requests. [Play User Data policy](https://support.google.com/googleplay/android-developer/answer/10144311)
- **Legal review.** Confirm the developer's and providers' roles, applicable legal bases, retention of support correspondence, international transfers, a private channel for rights requests, and trader status. Confirm the OpenAI agreement and how its guidance against deploying keys in mobile apps applies to the current BYOK design. Console declarations do not resolve these questions. [Garante data rights guidance](https://www.garanteprivacy.it/Regolamentoue/diritti-degli-interessati) · [OpenAI API key guidance](https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety)

## Test an update (closed testing)

The automated publisher requires an existing Play app and an existing closed testing track. Complete any initial app or track setup manually if Play Console requires it. Follow the README's “Build for Google Play” section to configure GitHub Actions; it explicitly sets `PLAY_CLOSED_TRACK` to the track's API identifier (for example, `alpha`).

1. Add the testers' Google account emails to the closed test and share the opt-in link.
2. Use the **Rilascia app** link from a successful CI run on `main` to create a GitHub Release for that verified commit. Use a protected `vMAJOR.MINOR.PATCH` or `vMAJOR.MINOR.PATCH-beta.N` tag and publish the release. A draft does not trigger deployment. The tag must still point to the current `main` commit at signing, deployment, and retry.
3. Enter 1–500 Unicode characters of Italian release notes in the release description, for example: "Nuovo nome: Centello. Le spese già registrate restano sul dispositivo." The workflow publishes these notes to the selected closed track. Its version code is `PLAY_VERSION_CODE_BASE` plus the workflow run number and must exceed the largest code already used on any Play track. Changed notes after signing require a deliberate new release with a new tag.
4. If the workflow fails, rerun the same workflow run to reuse its version code and signed bundle. The bundle is retained for 3 days, and a retry after a successful Play commit detects the existing release instead of uploading it again. A paused Play release is not resumed by retry; handle it deliberately with a new release.
