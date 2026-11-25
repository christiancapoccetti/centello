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
> • Se attivi l’inserimento spese con l’AI, Centello invia a Google il contenuto scelto per preparare una spesa da controllare e salvare. Puoi sempre inserire una spesa a mano.
> • Centello è senza account, pubblicità o tracciamento.

Use the 512 px icon in `docs/brand/icona-512.png`, a 1024 × 500 feature graphic, and at least two phone screenshots with synthetic data.

## App content

- **App access.** Manual entry requires no login. Live voice, photo, and PDF entry require internet access and a Play installation that passes App Check. Verify reviewer access before submission. [Play app access requirements](https://support.google.com/googleplay/android-developer/answer/15748846)
- **Ads.** None.
- **Content rating.** Complete the questionnaire for a utility app with optional AI expense entry. Answer any question about third-party transfers using the actual provider payload and the wording of that question. The rating comes from the questionnaire. [Play content rating guidance](https://support.google.com/googleplay/android-developer/answer/9859655)
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

Use an internet-connected Android device with the Play-distributed build. The Google account used to install and review the build needs access to the selected test track. Assisted entry requires the Play-recognized, licensed installation. The reviewer needs neither a Centello account nor a personal API key. Verify the full reviewer path from a fresh Play install before submitting. [Play app access requirements](https://support.google.com/googleplay/android-developer/answer/15748846)

1. In **Impostazioni → Voce, foto e PDF**, turn on **Inserimento spese con l’AI**, read the disclosure, and tap **Acconsento**. A fresh installation has five trial drafts, enough for these steps and the optional camera check. If you need more on the same installation, follow the invitation procedure below.
2. In **Oggi**, tap the microphone and say «ieri dodici euro e cinquanta di pizza». Grant microphone permission if prompted. Tap the stop button to prepare the draft, or wait for recording to stop automatically after 20 seconds. Check that the draft shows €12.50 and yesterday’s date.
3. Discard the draft and use a test category named **Lavoro**. Say «Inserisci 150 euro su lavoro perché ho acquistato la licenza di fatture in cloud». Check that **Nota** contains a short note such as «Licenza Fatture in cloud». Edit the note and discard the draft.
4. Create a gallery image and PDF of the synthetic receipt in the ledger below. From the camera menu in **Oggi**, use **Scegli dalla galleria** and **Scegli un PDF** to review both drafts. On Android 13 or later, try **Scatta una foto** with a working camera.
5. Close the editor without saving. Confirm that the expense count is unchanged. Turn off **Inserimento spese con l’AI** and verify that manual entry still works.

## Firebase operator checklist

- [ ] Create separate Firebase projects for local development and the billed Play beta. Register `it.christiancapoccetti.centesimo.dev` in dev and `it.christiancapoccetti.centesimo` in beta. Enable Firebase AI Logic with the Gemini Developer API. Use synthetic inputs in dev and confirm the beta uses the paid service terms before enabling tester traffic. Confirm that Centello’s personal expense use case is eligible under the developer-use terms. Limit testing to eligible regions and adults. Google requires paid service for apps offered in the EEA, Switzerland, and the UK. [Firebase setup](https://firebase.google.com/docs/ai-logic/get-started) · [Gemini API terms](https://ai.google.dev/gemini-api/terms)
- [ ] Restrict each Firebase Android API key to its package and relevant signing certificate SHA-1. Allow the Firebase APIs needed by the SDK, including Firebase AI Logic and App Check, and omit the Generative Language API from the bundled key’s API allowlist. Firebase uses its own authorization for the backend. Use each project’s Android configuration through the staging commands in the [README](../README.md). Keep downloaded configs, access keys, and debug tokens out of the repository and build logs. [Firebase API keys](https://firebase.google.com/docs/projects/api-keys) · [Firebase AI Logic FAQ](https://firebase.google.com/docs/ai-logic/faq-and-troubleshooting)
- [ ] For Play Integrity, link the Play app to the beta Cloud project and register the **Play App Signing** certificate SHA-256 in Firebase App Check. Set **PLAY_RECOGNIZED** and **LICENSED** for the Play-only beta. Use the App Check debug provider only in dev and revoke unused debug tokens. [Play Integrity provider](https://firebase.google.com/docs/app-check/android/play-integrity-provider) · [Android debug provider](https://firebase.google.com/docs/app-check/android/debug-provider)
- [ ] Verify baseline App Check enforcement for Firebase AI Logic. Verify a Play-installed, enrolled tester can generate a draft, and an unregistered client is rejected. The Android facade requests limited-use tokens. Start replay protection in **Monitoring only**, review verified requests, reused-token counts, latency and errors, then decide when to enforce. Retest a lawful Play install after enforcement. [Firebase App Check and replay protection](https://firebase.google.com/docs/ai-logic/app-check)
- [ ] Set a low project request limit and a Firebase AI Logic per-user generation quota for the relevant region. Record the effective values privately, then verify the limits and the app’s quota error. Set a project spend cap where the paid Gemini account supports it. Record the cap and its enforcement delay privately. Set a Cloud Billing budget scoped to the beta and send threshold alerts to a verified private recipient. Billing budget alerts do not stop charges. Verify receipt and the operator’s ability to pause requests, for example by disabling the Firebase AI Logic API in the beta project. Confirm assisted entry fails without writes and manual entry still works. Record who can pause and resume. [Firebase quotas](https://firebase.google.com/docs/ai-logic/quotas) · [Gemini project spend caps](https://firebase.google.com/docs/projects/billing/spend-caps) · [Cloud Billing budgets](https://cloud.google.com/billing/docs/how-to/budgets)
- [ ] Keep app and operator records to aggregate counts, token usage, status, latency and costs. If enabling Firebase AI monitoring, first exclude prompt and response logs in the `_Default` sink using `resource.type="firebasevertexai.googleapis.com/Model"` and check other sinks and exports. Use one synthetic request to verify content stays out of Cloud Logging, retained traces and dashboards. Review access and retention before accepting live traffic. [Firebase AI monitoring](https://firebase.google.com/docs/ai-logic/monitoring)
- [ ] Test a clean Play install and an update from the OpenAI build. Check manual entry offline, decline consent, and confirm no assisted request or App Check attestation before consent. Check consent withdrawal, cancellation, blocked input, access denied, quota limits and timeouts. Confirm failures preserve existing data and that no request content or credentials enter app or CI logs. Confirm the published policy covers both versions while both are distributed.

### Issue and revoke AI invitations

1. Run `node tools/Centello.Invites/invites.mjs init-env` once in a private operator environment. It creates `issuer.env` and prints the Base64 Ed25519 public key. Store that public value in the GitHub Actions environment secret `CENTELLO_INVITE_PUBLIC_KEY` under `play-closed-beta`. The signed app embeds the public key. For a passphrase-encrypted PEM, use `init` instead.
2. In the applicable Firebase project, open **Remote Config**. Set `revoked_invite_ids` to a JSON array. Start with `[]`. For `ai_interest_form_url`, use an HTTPS Google Forms link on `forms.gle` or under `docs.google.com/forms/`. Leave the URL blank to hide the form. Publish both parameters before testing redemption.
3. Ask the tester to share the installation ID from **Impostazioni → Voce, foto e PDF → Attiva un invito**. Run `node tools/Centello.Invites/invites.mjs issue <installation-id> "<local-label>"`. Send the printed code to the tester for that installation. The label stays in the local issuer records.
4. Run `node tools/Centello.Invites/invites.mjs list` to find the invite ID. To revoke it, run `node tools/Centello.Invites/invites.mjs revoke <invite-id>`. Append that ID as a string to the existing `revoked_invite_ids` array and publish the update. The command records revocation locally. The published array controls access on devices.

Keep the issuer directory outside Git and CI. The issuer uses `CENTELLO_INVITE_HOME` when set. On Windows, the default is `%LOCALAPPDATA%/Centello/Invites`. The portable `issuer.env` contains the signing key and grants the ability to issue invitations. Restrict read access to the operator and keep a backup in an encrypted volume, together with the `issued` and `revoked` records. For a PEM issuer, back up `issuer.pkcs8.pem` and keep its passphrase separately.

Enable the Firebase Remote Config Realtime API in the same project. If you restrict the Android API key, allow the Firebase Remote Config, Firebase Remote Config Realtime and Firebase Installations APIs. [Firebase API key restrictions](https://firebase.google.com/docs/projects/api-keys)

Centello requires a fresh Remote Config response at redemption and when an invited installation returns to the foreground. If verification reaches 12 hours, the next draft request requires another refresh. The app listens for published updates during invited use. Ordinary Remote Config fetches use a 12-hour minimum interval. If the cached template lacks both access parameters, Centello forces one refresh per app process to pick up a template published after the first install. A form URL explicitly published as blank keeps the form hidden. If verification fails, use manual entry. Once Centello confirms revocation, it clears the invitation and allows any remaining trial drafts.

### Live acceptance ledger

Six synthetic fixtures are planned. Redeem a test invitation on the installation that runs the full set. Status at handoff is **0 real attempts**. Attestation, fixture outcomes, latency, tokens and cost await a live operator run. Synthetic build configuration and automated tests cannot populate this ledger.

| Fixture | Synthetic input and expected result | Live attempts / delivered |
| --- | --- | --- |
| V1 voice | Say «ieri dodici euro e cinquanta di pizza». Verify €12.50 and yesterday in device-local time. Discard on a physical phone with the Play install and verify the expense count is unchanged. | 0 / 0 |
| P1 photo | Render or photograph a receipt for Pizzeria Esempio. Include today's date, subtotal €11.36, IVA €1.14, total €12.50, cash €20.00, and change €7.50. Expect today's date and €12.50. | 0 / 0 |
| D1 two-page PDF | Put line items and subtotals on page 1. On page 2, show the dated final amount due €12.50, IVA €1.14, and a distinct amount already paid or change. Verify the final amount and date from page 2. | 0 / 0 |
| D2 undated PDF | Remove dates from the synthetic document, keeping the distinct totals. Verify €12.50 and review the fallback to today's local date. | 0 / 0 |
| V2 ambiguous category | Create two equally plausible categories, with different tags, and dictate a purchase without naming either. Expect an empty category and tag. Check independently with a test double that a tag belonging to another category is dropped. | 0 / 0 |
| P2 unreadable photo | Select a synthetic image of an obscured receipt. Expect no invented amount or category. Return to manual entry and verify the expense count. | 0 / 0 |

Run the fixtures against `gemini-3.5-flash-lite` in the billed beta. For every attempt, record the fixture ID, modality, build, first attempt or retry, provider result, correctness of each draft field, elapsed time from submission to editor or error, and input and output tokens where available. The editor does not display or store token counts. For cost measurements, run one fixture at a time on a quiet test project and use aggregate provider usage and billing deltas. Record any traffic that prevents attribution. Record provider input token detail by text, audio, image and document when available. Keep PDF tokens under document for the ledger. Mark missing metrics as unavailable. Keep raw media, transcripts, prompts, responses, and credentials out of the ledger. For timeouts, check billed usage before counting a retry as free.

Summarize attempts, failures, delivered drafts, latency, and tokens per modality. Use the paid model rates in effect on the test date and reconcile against the beta project’s billing report. Include billable thinking tokens in output and use the provider’s document rate for PDF inputs. Divide the cost of all attempts, including failed, abandoned and retried attempts, by the number of delivered drafts. Count each displayed draft once and report correctness separately. With zero deliveries, leave cost per delivered draft unavailable. [Gemini Developer API pricing](https://ai.google.dev/gemini-api/docs/pricing)

## Data safety

Apply the Data safety definitions to every version still distributed under this package. Declare optional draft uploads as **collected** for **App functionality**. Assess Firebase and earlier OpenAI builds together while both remain in scope. [Play Data safety definitions](https://support.google.com/googleplay/android-developer/answer/10787469)

| Data type | What leaves the device |
| --- | --- |
| Audio files → Voice or sound recordings | Audio chosen for assisted entry. |
| Photos and videos → Photos | A receipt, invoice, or payment image the user takes or selects. |
| Files and docs | The PDF the user selects, including its contents. |
| App activity → Other user-generated content | Active category and tag names and today's date. Earlier OpenAI builds also send the expense transcript in a separate request. |
| Device or other IDs | Remote Config uses a Firebase installation ID. Assess this identifier and the Firebase App Check and Play Integrity signals using the SDK disclosures and signed-build traffic. The Centello installation ID used for invitations is shared only when the user chooses to send it. |
| Financial info → Purchase history | Purchases and transactions described in voice input or in receipts, invoices, and payment screenshots. |
| Financial info → Other financial info | Other financial details in the submitted material, such as amounts due on invoices or bills. |

Check typical receipts, invoices, and payment screenshots for additional types the form covers, such as personal names, addresses, or user payment info when documents show financial account details. Centello sends document contents, so the upload format alone does not determine which declarations apply. [Play data types](https://support.google.com/googleplay/android-developer/answer/10787469)

- **Sharing.** Confirm whether each provider relationship or transfer meets a Play exception, including the service provider exception and the user-initiated transfer exception. If an exception does not apply, declare the relevant types as shared. Review Firebase and any earlier OpenAI builds still in scope. [Play transfer exceptions](https://support.google.com/googleplay/android-developer/answer/10787469) · [Play User Data policy](https://support.google.com/googleplay/android-developer/answer/10144311)
- **Ephemeral processing.** Treat Gemini inputs as non-ephemeral because the paid terms allow limited abuse monitoring retention. Disabling content logs controls project logging. Assess earlier builds using OpenAI's endpoint-specific retention rules, including abuse monitoring on the separate interpretation request. [Gemini API terms](https://ai.google.dev/gemini-api/terms) · [OpenAI API data controls](https://developers.openai.com/api/docs/guides/your-data)
- **App attestation.** Review the Firebase and Play Integrity disclosures for security signals and any relevant device identifiers. Verify behavior before and after enabling assisted drafts on the signed build. [Firebase Android data disclosure](https://firebase.google.com/docs/android/play-data-disclosure) · [Play Integrity data disclosure](https://developer.android.com/google/play/integrity/terms)
- **Encryption in transit.** The provider calls use HTTPS. Verify the current form’s encryption answer across all flows included in the declaration.
- **Cloud backup and migration.** Android transfers the database and its working journal through Google backup or device-to-device migration under the user's system settings. Centello has no app-operated backup endpoint. The app’s Android backup rules exclude the consent preference, earlier OpenAI key, and cache. Verify how Play treats each system-managed transport on the submitted build before finalizing the form. [Android Auto Backup](https://developer.android.com/identity/data/autobackup)
- **Export, cache, and reminders.** Download exports stay on the device unless the user moves or shares them. On Android 8 and 9, an explicit choice in the Android share sheet can send a file to another app. Assess that flow under Play's user-initiated sharing exception. Temporary picker and export cache copies and Android's local recap and payment reminders make no network requests.
- **Deletion.** Users can delete expenses, clear local app data, or uninstall. Copies in Download, cloud backups, and on other devices have separate controls. Turning off assisted drafts stops new Gemini requests. Firebase Remote Config continues fetching access settings when the app opens. In earlier OpenAI builds, removing the key stops new requests. Follow the privacy policy for request retention and deletion. Centello has no app account to delete. Answer any separate deletion-request question based on the services and data in scope. [Centello privacy policy](privacy-policy.md)

## Before submission

- **Play Console checks.** Test the signed build from a clean install. Verify the in-app disclosures, backup and export routes, and both classes of notifications. Confirm the App access arrangement for the live integration, current Data safety answers, financial features, and content rating. Verify the privacy URL from a browser without an account and confirm that the public support address can receive requests. [Play User Data policy](https://support.google.com/googleplay/android-developer/answer/10144311)
- **Legal review.** Confirm the developer's and providers' roles, applicable legal bases, retention of support correspondence, international transfers, a private channel for rights requests, and trader status. Review the Firebase and paid Gemini terms, including model use and geographic eligibility. Retain the OpenAI terms review for older artifacts still distributed. Console declarations do not resolve these questions. [Garante data rights guidance](https://www.garanteprivacy.it/Regolamentoue/diritti-degli-interessati) · [Gemini API terms](https://ai.google.dev/gemini-api/terms)

## Test an update (closed testing)

The automated publisher requires an existing Play app and an existing closed testing track. Complete any initial app or track setup manually if Play Console requires it. Follow the README's “Build for Google Play” section to configure GitHub Actions; it explicitly sets `PLAY_CLOSED_TRACK` to the track's API identifier (for example, `alpha`).

1. Add the testers' Google account emails to the closed test and share the opt-in link.
2. Use the **Rilascia app** link from a successful CI run on `main` to create a GitHub Release for that verified commit. Use a protected `vMAJOR.MINOR.PATCH` or `vMAJOR.MINOR.PATCH-beta.N` tag and publish the release. A draft does not trigger deployment. The tag must still point to the current `main` commit at signing, deployment, and retry.
3. Enter 1–500 Unicode characters of Italian release notes in the release description, for example: "Nuovo nome: Centello. Le spese già registrate restano sul dispositivo." The workflow publishes these notes to the selected closed track. Its version code is `PLAY_VERSION_CODE_BASE` plus the workflow run number and must exceed the largest code already used on any Play track. Changed notes after signing require a deliberate new release with a new tag.
4. If the workflow fails, rerun the same workflow run to reuse its version code and signed bundle. The bundle is retained for 3 days, and a retry after a successful Play commit detects the existing release instead of uploading it again. A paused Play release is not resumed by retry; handle it deliberately with a new release.
