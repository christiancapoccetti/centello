# Google Play Console answers

What to enter in Play Console for the first release on the closed testing track. The closed beta uses the name Centesimo; `<NAME>` in the listing texts stands for the final name, still to be chosen, since Centesimo and Quattrino both clash with existing finance apps or marks. Play Console labels change often; check each answer against the current form. GitHub environment and workflow setup is documented in the README's “Build for Google Play” section.

## Account

- **Trader status:** not a trader. The app is a free personal project with no ads, no purchases and no business behind it. A trader account must show a postal address, phone and email on every store listing (EU Digital Services Act); a non-trader account does not, and EU users see a notice that EU consumer law does not apply to contracts with the developer. Switch to trader before any paid version, ads or in-app purchases.
- **Developer contact email:** required and public. An alias that forwards to a private mailbox (for example Firefox Relay or SimpleLogin) keeps the personal address private.

## App details

- Default language: Italian (it-IT). App, not game. Free.
- Category: Finance.
- Privacy policy: <https://christiancapoccetti.github.io/centesimo/privacy-policy.html>
- Website (optional): <https://github.com/christiancapoccetti/centesimo>

## Store listing (Italian)

**Short description** (80 characters at most):

> Segna le spese in pochi secondi, a mano, a voce o da foto. Restano sul telefono.

**Full description:**

> <NAME> ti aiuta a tenere sotto controllo le spese senza fatica: segni una spesa in pochi secondi e vedi subito come va il mese.
>
> **Inserisci come preferisci**
> • A mano, con importo, categoria, tag e nota.
> • A voce: tieni premuto e di' "ieri 12 euro di pizza".
> • Da una foto o da un PDF: fotografa uno scontrino o scegli una fattura.
>
> **Capisci dove vanno i soldi**
> • Budget mensili per categoria e quanto ti resta.
> • Il confronto con il mese precedente e l'andamento giorno per giorno.
> • Insight per mese e per anno: cosa è cambiato e dove.
> • Il riepilogo del mese e dell'anno, a schede da sfogliare.
> • Pagamenti regolari, come affitto e abbonamenti, registrati da soli.
> • Widget per la schermata home e scorciatoie per aggiungere una spesa al volo.
>
> **I tuoi dati restano tuoi**
> • Le spese restano sul telefono: niente account, niente pubblicità, niente tracciamento.
> • Backup in un file ed esportazione CSV quando vuoi.
> • Voce, foto e PDF usano OpenAI con la tua chiave API, solo quando li usi. Senza chiave l'inserimento a mano funziona sempre.

**Graphics:** the 512 px icon, a 1024×500 feature graphic and at least two phone screenshots with placeholder data. All of them wait for the new name and logo.

## App content

- **App access:** no login. Voice, photo and PDF entry need the user's own OpenAI API key, which cannot be shared with reviewers; say so in the instructions and note that manual entry, the core of the app, works without it.
- **Ads:** no ads.
- **Content rating:** utility app; no violence, sexual content, bad language, drugs, gambling or user-to-user interaction; no location sharing; no digital purchases. Expected rating: PEGI 3.
- **Target audience:** 18 and over; the app is not designed to appeal to children.
- **News, government, health, COVID-19 apps:** no.
- **Financial features:** none of the listed features (no banking, loans, payments, crypto or investment). The app only records the user's own expenses on the device.

## Data safety

Data counts as collected when it leaves the device. Only voice, photo and PDF entry send anything, to OpenAI, with the user's own key, and only when the user starts it.

- **Collected:** audio (voice recordings), photos, files and docs (the PDF), and other user-generated content (the transcribed text and the category and tag names).
  - Purpose: app functionality. Optional: yes, the user chooses to use these features. Processed ephemerally: no, since OpenAI may keep API inputs for abuse monitoring.
- **Shared:** no. The transfer goes to a service provider on a request the user starts, which Play does not count as sharing.
- **Encrypted in transit:** yes (HTTPS).
- **Deletion:** the developer holds no user data; expenses are deleted in the app or by uninstalling it.
- Expenses never leave the device except through Android's own backup to the user's Google account, which is a system feature, not the app's collection.

## First release (closed testing)

The automated publisher requires an existing Play app and an existing closed testing track. Complete any initial app or track setup manually if Play Console requires it. Follow the README's “Build for Google Play” section to configure GitHub Actions; it explicitly sets `PLAY_CLOSED_TRACK` to the track's API identifier (for example, `alpha`).

1. Add the testers' Google account emails to the closed test and share the opt-in link.
2. Use the **Rilascia app** link from a successful CI run on `main` to create a GitHub Release for that verified commit. Use a protected `vMAJOR.MINOR.PATCH` or `vMAJOR.MINOR.PATCH-beta.N` tag and publish the release. A draft does not trigger deployment. The tag must still point to the current `main` commit at signing, deployment, and retry.
3. Enter 1–500 Unicode characters of Italian release notes in the release description, for example: "Prima versione di prova." The workflow publishes these notes to the selected closed track. Its version code is `PLAY_VERSION_CODE_BASE` plus the workflow run number and must exceed the largest code already used on any Play track. Changed notes after signing require a deliberate new release with a new tag.
4. If the workflow fails, rerun the same workflow run to reuse its version code and signed bundle. The bundle is retained for 3 days, and a retry after a successful Play commit detects the existing release instead of uploading it again. A paused Play release is not resumed by retry; handle it deliberately with a new release.
