---
title: Informativa sulla privacy di Quattrino
---

# Informativa sulla privacy di Quattrino

Ultimo aggiornamento: 21 novembre 2026

Quattrino è un'app Android per tenere traccia delle spese personali, sviluppata da Christian Capoccetti come progetto personale. Questa informativa spiega quali dati usa l'app, dove vanno e quali scelte hai.

## In breve

- Le tue spese restano sul tuo telefono. L'app non ha un server e lo sviluppatore non riceve nessun tuo dato.
- Solo se usi l'inserimento con la voce, da una foto o da un PDF, quello che detti, fotografi o scegli viene inviato a OpenAI con la tua chiave API.
- Niente pubblicità, niente account, niente strumenti di analisi o di tracciamento.

## Chi sviluppa l'app e come contattarlo

Christian Capoccetti. Per domande o richieste su questa informativa apri una segnalazione su [GitHub Issues](https://github.com/christiancapoccetti/centesimo/issues).

Le segnalazioni su GitHub sono pubbliche: non scrivere dati personali. Se la richiesta li richiede, nella risposta ti indicherò un canale privato.

## Dati che restano sul telefono

- Spese, categorie, tag, budget, pagamenti regolari e impostazioni sono salvati in un database sul telefono.
- La tua chiave API OpenAI, se la inserisci, è salvata nell'archivio protetto di Android e viene usata solo per chiamare OpenAI.

## Cosa viene inviato a OpenAI, e quando

L'invio avviene solo quando usi la voce, una foto o un PDF, dopo che hai inserito la tua chiave API e accettato l'avviso sul consenso. Vengono inviati:

- l'audio che registri, fino a 20 secondi, tenuto in memoria e mai salvato dall'app;
- il testo trascritto dall'audio;
- la foto che scatti o scegli, ridotta e ripulita dai dati EXIF e dalla posizione GPS, oppure il PDF che scegli;
- i nomi delle tue categorie e dei tag attivi e la data di oggi, per interpretare la spesa.

Le altre spese e il database non vengono mai inviati. I dati viaggiano cifrati (HTTPS).

Poiché l'invio usa la tua chiave, rientra nel tuo rapporto con OpenAI e nelle sue condizioni e [informativa sulla privacy](https://openai.com/policies/). OpenAI può conservare per un periodo limitato i dati ricevuti tramite API per controllare gli abusi. Quattrino non li salva.

L'inserimento a mano funziona sempre, anche senza chiave e senza connessione.

## Backup

- **Backup automatico di Android:** se è attivo sul tuo telefono, Android salva una copia del database delle spese nel tuo account Google. La chiave API OpenAI non è inclusa. Il backup è gestito da Google secondo le impostazioni del tuo account.
- **File di backup ed esportazioni CSV:** li crei tu e vengono salvati nella cartella Download del telefono. Restano sotto il tuo controllo.

## Permessi

- **Microfono:** solo mentre detti una spesa.
- **Fotocamera:** solo per fotografare una spesa, come uno scontrino o una fattura.
- **Notifiche:** per il riepilogo del mese e dell'anno. La notifica non mostra importi e puoi disattivarla nelle impostazioni dell'app.
- **Internet e stato della rete:** solo per comunicare con OpenAI.
- **Avvio del telefono:** per riprogrammare la notifica del riepilogo dopo un riavvio.

## Conservazione e cancellazione

I dati restano sul telefono finché non li cancelli. Puoi eliminare singole spese, rimuovere la chiave API nelle impostazioni o disinstallare l'app, che cancella il database dal telefono. Il backup nel tuo account Google si gestisce dalle impostazioni di Google.

## I tuoi diritti

Lo sviluppatore non riceve i tuoi dati, quindi non ha dati da mostrarti, correggere o cancellare: li controlli direttamente sul telefono. Per i dati inviati a OpenAI puoi rivolgerti a OpenAI. Puoi sempre presentare un reclamo al [Garante per la protezione dei dati personali](https://www.garanteprivacy.it/).

## Minori

Quattrino non è destinata ai minori di 14 anni.

## Modifiche

Eventuali modifiche a questa informativa saranno pubblicate su questa pagina con la data aggiornata. La cronologia delle versioni è nel [repository del progetto](https://github.com/christiancapoccetti/centesimo/commits/main/docs/privacy-policy.md).
