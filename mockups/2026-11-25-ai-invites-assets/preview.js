const scenarios = {
  trial: "Prove esaurite",
  invite: "Attiva un invito",
  share: "Condividi ID",
  active: "Invito attivo",
  invalid: "Codice non valido",
  mismatch: "Altro telefono",
  revoked: "Invito revocato",
  offline: "Verifica non disponibile",
  unavailable: "AI non disponibile"
};

const demoId = "CA7D · 482B · 916F";
const demoCode = "CENTELLO-DEMO-482B";
const sampleCodes = { invalid: "CENTELLO-CODICE-INCOMPLETO", mismatch: "CENTELLO-DEMO-ALTRO", revoked: "CENTELLO-DEMO-REVOCATO", offline: demoCode };
let userInput = null;
let grantActive = false;
const screen = document.querySelector("#screen");
const phone = document.querySelector(".phone");
const nav = document.querySelector("aside nav");
const compact = document.createElement("select");
compact.className = "compact-control";
compact.setAttribute("aria-label", "Schermata del mockup");
document.querySelector("aside").prepend(compact);

for (const [key, label] of Object.entries(scenarios)) {
  const item = document.createElement("button");
  item.textContent = label;
  item.dataset.scene = key;
  item.onclick = () => navigate(key);
  nav.append(item);
  compact.add(new Option(label, key));
}
compact.onchange = () => navigate(compact.value);
const sample = document.createElement("button");
sample.textContent = "Usa codice demo";
sample.className = "sample";
sample.onclick = () => {
  const field = document.querySelector("#code");
  if (!field) return;
  field.value = demoCode;
  field.focus();
};
document.querySelector("aside").append(sample);
document.querySelector("#appearance").onchange = event =>
  phone.classList.toggle("light", event.target.value === "light");

function icon(code) {
  return '<i class="icon" aria-hidden="true">&#x' + code + ';</i>';
}

function button(label, target, style = "outline", glyph = "") {
  return '<button class="' + style + '" data-action="' + target + '">' +
    (glyph ? icon(glyph) : "") + label + '</button>';
}

function navigate(scene) {
  if (location.hash === "#" + scene) render();
  else location.hash = scene;
}

function home() {
  return '<div class="home"><div class="month"><b>Novembre 2026</b>' + icon("e5cf") +
    '</div><div class="total">384,50 €</div><p>Spese del mese</p><div class="keyline"><i></i><i></i><i></i></div>' +
    '<h3>Ultime spese</h3><div class="row"><span class="tile">' + icon("e8cc") +
    '</span>Spesa alimentare<b>32,50 €</b></div><div class="row"><span class="tile">' + icon("e56c") +
    '</span>Pranzo<b>12,00 €</b></div><div class="entry">' +
    button("Aggiungi spesa", "manual", "outline", "e145") + button("Usa AI", grantActive ? "capture" : "trial", "text", "e029") + '</div></div>';
}

function sheet(background, title, content) {
  return '<div inert aria-hidden="true">' + background + '</div><div class="overlay"><section class="sheet" ' +
    'role="dialog" aria-labelledby="dialog-title"><div class="handle"></div>' +
    '<h2 id="dialog-title">' + title + '</h2>' + content + '</section></div>';
}

const back = '<button class="back" aria-label="Indietro" data-action="settings">' + icon("e5c4") + '</button>';
const intro = back + '<h2>Attiva un invito</h2><div class="section"><h3>Condividi il tuo ID</h3>' +
  '<p>Invialo a chi ti offre l’accesso. Riceverai un codice per questo telefono.</p>' +
  '<strong class="device">' + demoId + '</strong>' + button("Condividi ID", "share") + '</div>';
const errors = {
  invalid: "Codice non valido. Copia il codice completo dal messaggio ricevuto.",
  mismatch: "Questo codice è per un altro telefono. Condividi il tuo ID per riceverne uno nuovo.",
  revoked: "Questo invito è stato revocato. Puoi chiedere un nuovo codice.",
  offline: "Non riesco a verificare l’invito. Controlla la connessione e riprova. La verifica non consuma prove."
};

function redeem(scene) {
  const error = errors[scene];
  return intro + '<section class="section"><label for="code"><strong>Codice di invito</strong></label>' +
    '<textarea class="field" id="code" spellcheck="false" autocapitalize="none" placeholder="Incolla il codice ricevuto"' +
    (error ? ' aria-describedby="feedback" aria-invalid="' + (scene === "invalid" || scene === "mismatch") + '"' : '') +
    '></textarea>' +
    (error ? '<p class="feedback" id="feedback" role="alert">' + error + '</p>' : '') +
    '<div class="actions">' + button(scene === "offline" ? "Riprova" : "Attiva invito", "validate", "fill") +
    (error ? button("Inserisci a mano", "manual", "text") : '') + '</div></section>';
}

function render() {
  const scene = location.hash.slice(1) || document.body.dataset.start;
  if (scene === "active") grantActive = true;
  if (scene === "revoked" || scene === "trial") grantActive = false;
  const parentScene = { sent: "share", settings: "invite", capture: "active", home: grantActive ? "active" : "trial" };
  const group = scene in scenarios ? scene : parentScene[scene] ?? "trial";
  for (const item of nav.children) item.setAttribute("aria-current", String(item.dataset.scene === group));
  compact.value = group;
  sample.hidden = !(scene === "invite" || scene in errors);
  if (scene === "trial") {
    screen.innerHTML = sheet(home(), "Hai usato le 5 prove gratuite", '<p>Ti piacerebbe continuare a usare voce, foto e PDF? Dicci cosa ne pensi in due domande.</p>' +
      '<div class="actions">' + button("Rispondi al sondaggio", "survey", "fill") + button("Inserisci a mano", "manual") +
      button("Ho un codice invito", "invite", "text") + '</div>');
  } else if (scene === "share") {
    screen.innerHTML = sheet(intro, "Condividi ID", '<p>ID di questa installazione di Centello</p><strong class="device">' +
      demoId + '</strong><div class="actions">' + button("Invia nella chat di esempio", "sent", "fill") + button("Copia ID", "copy") + button("Chiudi", "invite", "text") +
      '</div><small class="note" role="status"></small>');
  } else if (scene === "sent") {
    screen.innerHTML = '<h2>Chat di esempio</h2><section class="section"><h3>Il tuo messaggio</h3><p>Il mio ID Centello è ' + demoId +
      '.</p></section><section class="section"><h3>Risposta ricevuta</h3><p>Ecco il codice per il tuo telefono.</p><strong>' +
      demoCode + '</strong><div class="actions">' + button("Usa il codice in Centello", "useReceived", "fill") + '</div></section>';
  } else if (scene === "active") {
    screen.innerHTML = back + '<h2>Invito AI</h2><section class="section"><span class="success">' + icon("e86c") +
      'Invito attivo</span><p>Puoi usare voce, foto e PDF senza consumare le prove gratuite. L’invito è valido su questo telefono, senza scadenza.</p>' +
      button("Torna a Oggi", "home", "fill") + '</section>';
  } else if (scene === "unavailable") {
    screen.innerHTML = sheet(home(), "L’AI non è disponibile", '<p>Puoi inserire la spesa a mano. Questa richiesta non consuma una prova.</p>' +
      '<div class="actions">' + button("Inserisci a mano", "manual", "fill") + button("Chiudi", "home", "text") + '</div>');
  } else if (scene === "settings") {
    screen.innerHTML = '<h2>Impostazioni</h2><section class="section"><h3>Voce, foto e PDF</h3><p>Inserimento spese con l’AI</p>' +
      button("Attiva un invito", "invite") + '</section>';
  } else if (scene === "survey" || scene === "manual" || scene === "capture") {
    const destinations = {
      survey: ["Sondaggio sull’AI", "Nell’app si apre il modulo Google pubblicato."],
      manual: ["Inserisci a mano", "Nell’app si apre l’editor della spesa."],
      capture: ["Inserimento vocale", "L’invito è attivo. Nell’app puoi dettare la spesa."]
    };
    const [title, description] = destinations[scene];
    screen.innerHTML = '<h2>' + title + '</h2><p>' + description + '</p>' + button("Torna a Oggi", "home");
  } else if (scene === "home") screen.innerHTML = home();
  else screen.innerHTML = redeem(scene);
  const field = document.querySelector("#code");
  if (field) field.value = userInput ?? sampleCodes[scene] ?? "";
  userInput = null;
  screen.scrollTop = 0;
}

screen.addEventListener("click", async event => {
  const action = event.target.closest("[data-action]")?.dataset.action;
  if (!action) return;
  if (action === "useReceived") {
    userInput = demoCode;
    navigate("invite");
    return;
  }
  if (action === "copy") {
    const message = screen.querySelector("[role=status]");
    try {
      await navigator.clipboard.writeText(demoId);
      message.textContent = "ID copiato.";
    } catch {
      message.textContent = "Seleziona e copia l’ID qui sopra.";
    }
    return;
  }
  if (action === "validate") {
    const candidate = document.querySelector("#code").value.trim();
    const next = candidate === demoCode ? "active" : candidate === sampleCodes.mismatch ? "mismatch" :
      candidate === sampleCodes.revoked ? "revoked" : "invalid";
    userInput = candidate;
    navigate(next);
    return;
  }
  navigate(action);
});
window.addEventListener("hashchange", render);
window.addEventListener("keydown", event => {
  if (event.key !== "Escape") return;
  navigate(document.querySelector(".overlay") && location.hash === "#share" ? "invite" : "home");
});
render();
