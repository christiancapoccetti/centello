---
name: Centesimo
description: A personal expense tracker for Android; a month you read in one glance, an expense you add in one tap.
colors:
  graphite-ground: "#141516"
  graphite-raised: "#26282B"
  graphite-selected: "#333435"
  warm-white-ink: "#F4F4F0"
  signal-grey-mute: "#A0A3A9"
  hairline: "#2A2C2F"
  outline: "#3A3D41"
  chart-bar: "#55585E"
  pace-periwinkle: "#8FA8FF"
  accent-coral: "#FF8A7A"
  alert: "#FF8A7A"
  positive-container: "#16302A"
  on-positive-container: "#7FD6A8"
  tile-ink: "#141516"
  scrim: "#80000000"
  paper-ground-light: "#FAFAF8"
  paper-raised-light: "#EFEFEA"
  paper-selected-light: "#E3E3E2"
  ink-light: "#16161A"
  mute-light: "#62626A"
  hairline-light: "#E4E4DF"
  outline-light: "#DCDCD6"
  chart-bar-light: "#C9C9C3"
  pace-light: "#1F4FD8"
  accent-light: "#E0503D"
  alert-light: "#C8261A"
  positive-container-light: "#E3F2EA"
  on-positive-container-light: "#0B5A3D"
  tile-ink-light: "#16161A"
  category-coral: "#FF8A7A"
  category-peri: "#8EA6FF"
  category-butter: "#F5D06B"
  category-mint: "#6ED3A6"
  category-lilac: "#D4A8F0"
  category-sky: "#8FD0F0"
  category-grey: "#8A8D93"
  category-coral-light: "#F47A68"
  category-peri-light: "#7C95F2"
  category-butter-light: "#EDC14F"
  category-mint-light: "#57C493"
  category-lilac-light: "#C597E6"
  category-sky-light: "#79C2E6"
  category-grey-light: "#9A9DA3"
typography:
  display-total:
    fontFamily: "Archivo Condensed ExtraBold, Archivo, sans-serif"
    fontSize: "68px"
    fontWeight: 800
    lineHeight: 1
    fontFeature: "tnum"
  display-amount-entry:
    fontFamily: "Archivo Condensed ExtraBold, Archivo, sans-serif"
    fontSize: "60px"
    fontWeight: 800
    fontFeature: "tnum"
  display-summary:
    fontFamily: "Archivo Condensed ExtraBold, Archivo, sans-serif"
    fontSize: "48px"
    fontWeight: 800
    lineHeight: 1
    fontFeature: "tnum"
  title-large:
    fontFamily: "Archivo, sans-serif"
    fontSize: "20px"
    fontWeight: 700
    lineHeight: 1.25
    fontFeature: "tnum"
  section-title:
    fontFamily: "Archivo, sans-serif"
    fontSize: "18px"
    fontWeight: 700
    fontFeature: "tnum"
  title-medium:
    fontFamily: "Archivo, sans-serif"
    fontSize: "16px"
    fontWeight: 600
    lineHeight: 1.25
    fontFeature: "tnum"
  amount:
    fontFamily: "Archivo, sans-serif"
    fontSize: "16px"
    fontWeight: 700
    fontFeature: "tnum"
  body-large:
    fontFamily: "Archivo, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.4
    fontFeature: "tnum"
  lead:
    fontFamily: "Archivo, sans-serif"
    fontSize: "15px"
    fontWeight: 400
    lineHeight: 1.3
    fontFeature: "tnum"
  body:
    fontFamily: "Archivo, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.4
    fontFeature: "tnum"
  label:
    fontFamily: "Archivo, sans-serif"
    fontSize: "14px"
    fontWeight: 600
    fontFeature: "tnum"
  caption:
    fontFamily: "Archivo, sans-serif"
    fontSize: "13px"
    fontWeight: 400
    fontFeature: "tnum"
  body-small:
    fontFamily: "Archivo, sans-serif"
    fontSize: "12px"
    fontWeight: 400
    lineHeight: 1.3
    fontFeature: "tnum"
  icon:
    fontFamily: "Material Symbols Rounded (Filled)"
    fontSize: "24px"
rounded:
  none: "0px"
  field: "4px"
  chip: "8px"
  container: "12px"
  card: "16px"
  sheet: "28px"
  pill: "9999px"
spacing:
  hairline: "1px"
  gap-chart: "3px"
  xs: "4px"
  sm: "6px"
  md: "8px"
  lg: "12px"
  row-gap: "14px"
  xl: "16px"
  gutter: "24px"
  empty-top: "56px"
components:
  button-primary:
    backgroundColor: "{colors.warm-white-ink}"
    textColor: "{colors.graphite-ground}"
    typography: "{typography.title-medium}"
    rounded: "{rounded.pill}"
    padding: "0 24px"
    height: "56px"
  button-filled:
    backgroundColor: "{colors.warm-white-ink}"
    textColor: "{colors.graphite-ground}"
    typography: "{typography.amount}"
    rounded: "{rounded.pill}"
    padding: "0 24px"
    height: "48px"
  button-outlined:
    backgroundColor: "transparent"
    textColor: "{colors.warm-white-ink}"
    typography: "{typography.amount}"
    rounded: "{rounded.pill}"
    padding: "0 24px"
    height: "48px"
  button-tonal:
    backgroundColor: "{colors.graphite-raised}"
    textColor: "{colors.warm-white-ink}"
    typography: "{typography.amount}"
    rounded: "{rounded.pill}"
    padding: "0 24px"
    height: "48px"
  button-danger-outlined:
    backgroundColor: "transparent"
    textColor: "{colors.alert}"
    rounded: "{rounded.pill}"
    padding: "0 24px"
    height: "48px"
  button-text:
    backgroundColor: "transparent"
    textColor: "{colors.warm-white-ink}"
    typography: "{typography.label}"
    padding: "8px 12px"
    height: "48px"
  fab-extended:
    backgroundColor: "{colors.graphite-raised}"
    textColor: "{colors.warm-white-ink}"
    typography: "{typography.amount}"
    rounded: "{rounded.pill}"
    padding: "0 20px 0 16px"
    height: "56px"
  split-pill:
    backgroundColor: "{colors.graphite-raised}"
    textColor: "{colors.warm-white-ink}"
    rounded: "{rounded.pill}"
    height: "60px"
  chip:
    backgroundColor: "transparent"
    textColor: "{colors.warm-white-ink}"
    typography: "{typography.label}"
    rounded: "{rounded.chip}"
    padding: "0 12px"
    height: "32px"
  chip-selected:
    backgroundColor: "{colors.graphite-selected}"
    textColor: "{colors.warm-white-ink}"
    rounded: "{rounded.chip}"
    height: "32px"
  segmented:
    backgroundColor: "transparent"
    textColor: "{colors.warm-white-ink}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    height: "40px"
  segmented-selected:
    backgroundColor: "{colors.graphite-selected}"
    textColor: "{colors.warm-white-ink}"
  text-field:
    backgroundColor: "transparent"
    textColor: "{colors.warm-white-ink}"
    typography: "{typography.body-large}"
    rounded: "{rounded.field}"
    padding: "0 16px"
    height: "56px"
  category-tile:
    backgroundColor: "{colors.category-coral}"
    textColor: "{colors.tile-ink}"
    rounded: "{rounded.none}"
    size: "36px"
  expense-row:
    textColor: "{colors.warm-white-ink}"
    typography: "{typography.title-medium}"
    height: "58px"
  surface-card:
    backgroundColor: "{colors.graphite-ground}"
    rounded: "{rounded.card}"
    padding: "18px"
  info-card:
    backgroundColor: "{colors.graphite-raised}"
    rounded: "{rounded.container}"
    padding: "16px"
  menu-surface:
    backgroundColor: "{colors.graphite-raised}"
    rounded: "{rounded.field}"
    padding: "8px 0"
    width: "220px"
  bottom-sheet:
    backgroundColor: "{colors.graphite-ground}"
    rounded: "{rounded.sheet}"
    padding: "24px 24px 28px 24px"
  comparison-chip:
    backgroundColor: "{colors.graphite-raised}"
    textColor: "{colors.warm-white-ink}"
    typography: "{typography.label}"
    padding: "0 10px 0 6px"
    height: "26px"
  comparison-chip-lower:
    backgroundColor: "{colors.positive-container}"
    textColor: "{colors.on-positive-container}"
---

# Design System: Centesimo

## Overview

**Creative North Star: "Industrial Graphics on Graphite"**

Centesimo reads like a piece of Italian industrial graphics (the Olivetti and Pintori school) printed on the graphite of a station departure board. The ground is a near-black graphite, type is a warm white, and everything that carries meaning is flat: flat pastel inks for categories, flat bars for days, a flat line for the month. Nothing floats, nothing glows, nothing has a shadow. Depth comes from one raised graphite tone and 1dp hairlines.

The system is dense but quiet. One condensed extra-bold numeral (the month's total) is the loudest thing on any screen; everything else is Archivo at body sizes in ink or mute grey, with tabular figures so every amount lines up. Colour is reserved: the category palette belongs to categories alone, and a single coral marks the act of adding an expense. Dark is the reference scheme; light is a paper twin with the same roles, and both follow the system setting.

This is a native Android app (.NET MAUI) locked to portrait. All measures below are dp (sizes in sp for text); the frontmatter writes them as px for tool compatibility. It replaced an earlier green Material 3 look: tonal Material cards, progress bars over lists of category cards and Material colour roles are not part of this world.

**Key Characteristics:**
- Graphite ground (dark reference) or warm paper (light), no shadows anywhere.
- Archivo throughout, Archivo Condensed ExtraBold only for money shown at display size, tabular figures everywhere.
- Seven flat pastel category inks, each a dark glyph on a square tile; the app suggests one, the user picks from the six chromatic ones.
- Charts are hard-edged flat fills with 3dp gaps (2dp between the pace chart's slots), antialiasing off.
- Actions are pills; selection is an ink ring plus a selected fill, never a colour swap.

## Colors

A graphite-and-warm-white neutral world, one coral accent, and a seven-slot pastel palette that only categories may wear. Every colour has a dark and a light value; the dark one is the reference and the `-light` sibling in the frontmatter is its paper-scheme twin. `ThemeTokens` (`src/Centesimo.Application/Appearance/ThemeTokens.cs`) is the only colour table in the app: `ThemeColors.cs` reads it for MAUI, and the `@color/theme_*` resources the widgets use are generated from it and held to it by a test. `CategoryPalette.cs` is the only palette.

### Primary
- **Signal Coral** (`accent-coral`, light `accent-light`): the add action. The plus of "Aggiungi spesa", the microphone and camera in the split pill, the plus on the extended FAB and tonal button, the on-state of a switch. It marks the one thing the app exists for.

### Secondary
- **Pace Periwinkle** (`pace-periwinkle`, light `pace-light`): the dashed daily-average line of the home spend chart and its label "media per restare nel budget". Used nowhere else: the Insight charts draw their previous-period line in ink.

### Tertiary
- **Alert** (`alert`, light `alert-light`): errors, inline field errors, the destructive outlined button, and an over-budget category in the home key. In dark it shares the coral hue; in light it deepens to a true red (#C8261A) so it never reads as the accent.
- **Positive container / on-positive** (`positive-container`, `on-positive-container`): only for good news: the comparison chip when this month is lower than the last, the confirmation row of an active voice entry key, and the "Fatto" card of a finished restore.

### Neutral
- **Graphite Ground** (`graphite-ground`, light `paper-ground-light`): every page, the shell, the tab bar, bottom sheets, and the backing of labels that sit over chart bars.
- **Raised Graphite** (`graphite-raised`, light `paper-raised-light`): the one raised tone: split pill, extended FAB, tonal button, menus, info cards, comparison chip, badges.
- **Selected** (`graphite-selected`, light `paper-selected-light`): the fill under a selected chip, segment or picker cell.
- **Warm White Ink** (`warm-white-ink`, light `ink-light`): primary text, icons, filled buttons, today's bar, focus and selection rings, and in the Insight charts the previous-period line, the gap marker and the waterfall's end bar.
- **Mute** (`signal-grey-mute`, light `mute-light`): secondary text, captions, helper text, unselected tabs, field outlines at rest, empty-state glyphs, and the automatic part of the pace chart's bars.
- **Hairline** (`hairline`, light `hairline-light`): 1dp row dividers, card strokes, future-day stubs, read-only field outlines.
- **Outline** (`outline`, light `outline-light`): strokes of raised surfaces (pill, FAB, menu, unselected chip), the split pill's divider, the pace chart's budget hairline and the waterfall's connectors.
- **Chart Bar** (`chart-bar`, light `chart-bar-light`): past days in the spend chart, the pace chart's bars and the waterfall's start bar.
- **Tile Ink** (`tile-ink`, light `tile-ink-light`): the dark glyph on a category tile, in both schemes.
- **Scrim** (`scrim`): 50% black behind a bottom sheet.

### Category palette
Slots, stored by key in `Categories.Color`: coral (Corallo), peri (Pervinca), butter (Burro), mint (Menta), lilac (Lilla), sky (Celeste), and grey (Grigio). The six chromatic slots are a little deeper in the light scheme; grey is the fallback for a missing or unknown key.

**The Category Owns Its Ink Rule.** Palette colours appear only where a category is shown: its tile, its swatch in a key, its segment of the category line, its bars, its fraction bars and its step in the waterfall. Interface chrome never borrows a palette colour, and a palette colour never carries interface meaning.

**The App Suggests the Colour Rule.** A new category opens with the chromatic slot the fewest active categories hold (first in palette order on a tie) already selected, and the user may pick any of the six chromatic slots instead. Grey is never offered; it only shows a missing or legacy colour, which an edit keeps until the user picks a palette colour.

**The One Coral Rule.** Coral as an interface colour means "add an expense". Do not use it for emphasis, links or decoration.

## Typography

**Display Font:** Archivo Condensed ExtraBold (with Archivo)
**Body Font:** Archivo Regular, SemiBold and Bold
**Icon Font:** Material Symbols Rounded, filled

**Character:** A grotesque with industrial bones: the condensed extra-bold numeral has the weight of printed signage, and the regular cut stays plain and legible at 12 to 16sp. Hierarchy comes from size and weight, never from case or tracking.

### Hierarchy
- **Display total** (800, 68sp, line height 1): the month's total on the home, with the euro sign at 38sp after an 18sp space. Negative margins take back Android's font padding.
- **Display amount entry** (800, 60sp, euro sign at 34sp): the amount field of the expense editor, over a 2dp underline.
- **Display summary** (800, 48sp, line height 1): the total of an Insight period or a category detail.
- **Title large** (700, 20sp, 1.25): every page title in its top row (tab pages at the 24dp gutter, pushed pages after the back button) and bottom-sheet titles. There is no larger heading level.
- **Section title** (700, 18sp): "Ultime spese" and other section heads.
- **Title medium** (600, 16sp, 1.25): row titles (an expense row's category name).
- **Amount** (700, 16sp): the amount at the end of a row; also the face of 48dp buttons.
- **Body large** (400, 16sp, 1.4, mute): the budget sentence under the total, with its key figure in bold ink.
- **Lead** (400, 15sp, 1.3, mute): introductory sentences.
- **Body** (400, 14sp, 1.4): default text.
- **Label** (600, 14sp): chips, segments, field labels, the comparison chip.
- **Caption** (400, 13sp, mute): row details, the category key.
- **Body small** (400, 12sp, 1.3, mute): helper text, chart axis labels.

**The Tabular Figures Rule.** Every label, entry and button turns on Archivo's `tnum` (set by the Android handlers in `TabularFigures.cs`). Amounts must align in columns; never ship a text surface that bypasses it.

**The One Loud Numeral Rule.** Archivo Condensed ExtraBold is for money at display size only: the total, the summary, the amount being typed. It is never a heading face.

**The Large Text Rule.** Every page must read at the largest system font (200%) on a 360dp phone, and on a 320dp one with the largest display size: no text cut without an ellipsis, no figure lost, no overlap. Heights are minimums (an expense row is 58dp at least, a chip 32dp, a button 48dp), and text wraps or ends in an ellipsis inside the width it is given, never in a stack that measures it with no limit. A row that ends in an amount (`TrailingValueLayout`) moves the amount under its text when the text would keep less than 120dp. Glyphs, the display numerals (the total, a summary, the amount being typed) and the words inside charts keep their drawn size, since a box or a column is built around them; a chart carries its words in its description.

**The No Small Caps Rule.** No uppercase labels, no letter-spacing, no kickers above titles. A section is named by its title in sentence case.

## Layout

A single portrait column with a 24dp side gutter on every page. The home is a fixed 56dp top row (the period switcher: month in bold, year in regular mute, previous and next arrows as 48dp buttons), a scrolling middle, and a bottom dock holding the split pill with 24dp side and 12dp vertical margins, above the four-tab navigation bar (`Oggi`, `Insight`, `Categorie`, `Impostazioni`).

Lists are rows, not cards: an expense row is 58dp at least (36dp tile, 14dp gap, title over caption, amount right-aligned) with a 1dp hairline under it; a settings row is at least 64dp with the same 36/14 rhythm. Empty states are centred stacks with 32dp sides and 56dp of top room under a 40sp mute glyph. Form field labels start 24dp below what is above them.

The spacing rhythm is 4, 6, 8, 12, 14, 16, 24dp, with 3dp gaps inside charts (2dp between the 31 slots of the pace chart). Every touch target is at least 48 by 48dp; where a visual element is smaller (a 32dp chip, a key item, the "Vedi tutte" link) padding makes up the target and negative margins give the room back, so the visual rhythm is not loosened to fit the finger.

**The Rows Not Cards Rule.** Collections are hairline-divided rows on the ground. A bordered card is for an isolated message (an error, a note), not for list items.

## Elevation & Depth

Flat by construction: there is no shadow in the codebase. Depth is tonal and linear: the ground, one raised graphite tone for floating or interactive surfaces, a 1dp outline around those surfaces, and 1dp hairlines between rows. A bottom sheet separates from the page with a 50% scrim, not a shadow.

**The Graphite Ground Rule.** No shadows, no blur, no glow, no gradients. If a surface needs to stand forward, it takes the raised tone and an outline stroke.

## Shapes

Two families that never mix: pills for what you press, and square or barely rounded geometry for what you read.

- **Pills** (radius = half the height): primary button 56dp, filled and outlined buttons 48dp, extended FAB 56dp, split pill 60dp, segmented control 40dp, listening toast 36dp, the Aggiungi widget's pill.
- **Square** (0): category tiles, category line segments, chart bars, fraction bars, palette swatches.
- **Field** (4dp): text and date fields, menus.
- **Chip** (8dp): choice chips.
- **Container** (12dp): info cards and category picker cells.
- **Card** (16dp): the bordered surface card.
- **Sheet** (28dp top corners only): bottom sheets.
- **Widget** (24dp, or the launcher's own corner on Android 12 and later): the surface of Il mese, Scorciatoie and Il ritmo.

**The Hard Edge Rule.** Data graphics are drawn with antialiasing off, flat fills and 3dp gaps (2dp between days in a category's bars and between the pace chart's slots). Small values keep a visible minimum (2dp sliver, 4dp future stub) instead of vanishing.

## Components

### Buttons
Confident ink pills, no elevation.
- **Shape:** full pill (radius half the height).
- **Primary:** ink fill, ground text, Archivo Bold 17sp, 56dp high, 24dp sides: the docked action of a full-screen editor.
- **Filled:** the same at 48dp and 16sp: the main action of a form that is not docked.
- **Outlined:** transparent, 1dp mute stroke, ink Bold 16sp, 48dp. **Danger outlined** swaps stroke and text to alert; it is the confirm button of an irreversible action ("Sostituisci i dati del telefono" on Ripristina backup), paired with a plain outlined escape ("Scegli un altro file").
- **Tonal:** raised fill, outline stroke, coral plus glyph: the one action of an empty page.
- **Text:** transparent, ink SemiBold, 48dp minimum.
- **Icon:** 48dp square, 24sp glyph in ink; disabled at 30% opacity.
- **Disabled:** a disabled primary or the first-run split pill drops to 38% opacity.

### Split pill (signature)
The home's bottom dock: one 60dp raised pill with an outline stroke, "+ Aggiungi spesa" (coral plus, Bold 17sp ink) filling the left, and two 64dp icon segments on the right, each behind a 1dp outline divider: the coral microphone and the coral camera (26sp). Hold the microphone to dictate; while listening, a small outlined raised toast ("Registra… rilascia per inviare") floats 36dp above it. Tapping the camera opens a menu above it (the menu surface below, 48dp rows with a 22sp glyph and a SemiBold 16sp label): "Scatta una foto" (Android 13 and later only), "Scegli dalla galleria", "Scegli un PDF". Both icon segments share one disabled state: with no OpenAI key saved they look disabled and tapping them leads to Settings.

### Home screen widgets (signature)
Four widgets that are the Home on the launcher, in the same flat graphite and the same Archivo.
- **Aggiungi** (resizable in both directions): four shapes chosen by its size and the measured width of its words (`AddWidgetShapes.Of`). 2x1: the coral plus and microphone. 3x1: plus, microphone and camera as icons. 4x1: a raised pill with the coral plus, "Aggiungi spesa" and a divider, then the coral microphone and camera, as Oggi's dock. 4x2: a full-width "Aggiungi spesa" pill over "A voce" and "Da foto".
- **Scorciatoie** (4x1): the four categories used most in the last 30 days (by count) as tiles with their name, then "Altra" for any other; each opens the editor on that category.
- **Il mese** (2x2, resizable to 3x2, 2x3 and 3x3): the month (with the year when it fits beside it), the total in the condensed numeral, the remaining budget, a short comparison chip, the category line and, when there is one, the over-budget category. The size the launcher reports sets its shape (`MonthWidgetShapes`): the tightest padding and the smallest total on a 2x2, a larger total when wide. Then the rows that fit are drawn, in this order of loss: the chip goes first, then the alert shrinks from its full sentence to one line cut with an ellipsis, then the category line goes and, last, the alert (the category line comes back when only one of the two fits); the month, the total and the budget sentence always stay.
- **Il ritmo** (4x2): the full comparison chip, a round coral plus and Oggi's daily chart with its pace line ("27 € al giorno").

Tapping a widget opens the app on the matching screen; an editor opened from a widget returns to the launcher when it is saved or closed. The gallery and the PDF stay inside Oggi.

**The Widget Words Are Pictures Rule.** Launchers ignore the app's font resources, so no widget word is a TextView. Every word is drawn in Archivo with tabular figures as a single-colour mask that the layout tints with its `@color/theme_*` colour, like the glyphs; a text with two colours (the budget sentence with its bold ink amount) is drawn twice, light and dark, and the launcher shows the copy that matches its scheme. Each image carries the same words as its description. Widget text therefore does not follow the system font size.

**The Theme Resources Rule.** Widget layouts take colours only from `@color/theme_*`, generated from `ThemeTokens` in `values/` and `values-night/`, so a widget switches between light and dark with the launcher while the app is not running. Data graphics keep the hard-edge rules of the Home.

### Entry panel (signature)
The sheet opened from the Aggiungi microphone or camera, or from the "Spesa a voce" and "Spesa da foto" shortcuts: translucent, over the launcher, on the scrim. `PanelActivity` hosts voice (`Dictation`) or photo (`PhotoReading`), and `PanelFaces.Of` holds every word it shows.
- **Voice:** starts listening at once. A second tap on the microphone sends; so does reaching 20 seconds. "Annulla", a tap outside, Back or leaving the panel cancels. Oggi's hold-to-talk still refuses a recording that reaches 20 seconds.
- **Photo:** the camera opens at once (the gallery below Android 13), after the same camera consent as Oggi. The panel stays alive behind the camera, then shows the reading face ("Lettura della foto", a progress bar and the privacy line) over the launcher. "Annulla" or backing out of the camera closes it. If Android ended the process behind the camera, the sheet says "La foto non è arrivata a Centesimo. Riprova.".
- **No key:** the sheet gives the missing-key sentence of its source and "Apri impostazioni".

The draft then opens in the expense editor, as it does from Oggi.

### Launcher shortcuts (signature)
The long-press menu of the app icon, all dynamic: "Nuova spesa", "Spesa a voce", "Spesa da foto" and "Spesa in" the category most used in the last 30 days (absent with no category). The ways are drawn graphite with a coral glyph; the category takes its palette colour with its glyph in tile ink, like its tile. `HomeScreen` publishes them on every store change and when the app window activates, also with no widget installed, so they appear after the first launch after install. Each can be pinned. With no category every entry opens Oggi (`HomeScreenData.Admit`).

### Extended FAB
Raised fill, outline stroke, coral plus, Bold 16sp ink, 56dp pill: the action that floats over a list.

### Chips
- **Style:** 32dp high inside a 48dp target, 8dp radius, transparent with a 1dp outline stroke, SemiBold 14sp ink, 12dp sides.
- **Leading:** a 24dp category tile, a 10dp palette dot, its own glyph, or a check when selected.
- **Selected:** 2dp ink ring and the selected fill; padding shrinks by the extra unit so the text never moves.

### Segmented control
40dp pill with a 1dp mute stroke and 1dp mute dividers; the chosen segment takes the selected fill and an 18sp check before its SemiBold label.

### Cards / Containers
- **Surface card:** ground fill, 1dp hairline stroke, 16dp radius, 18dp padding: errors and notes.
- **Info card:** raised fill, no stroke, 12dp radius, 16dp padding.
- **Menu surface:** raised fill, outline stroke, 4dp radius, 8dp vertical padding, 220dp minimum width.
- **Bottom sheet:** ground fill, 28dp top corners, 24dp sides, over the scrim. The reading sheet is shared by voice, photo and PDF ("Inserimento vocale", "Lettura della foto", "Lettura del PDF"): title, a progress bar and one privacy line (for example "Foto inviata a OpenAI, Centesimo non la salva").

### Inputs / Fields
- **Style:** outlined box, 4dp radius, 56dp, transparent fill, 16sp text, 16dp sides; the SemiBold 12sp label sits on the top edge with a ground-coloured backing.
- **Focus:** the stroke and label turn ink and the stroke widens to 2dp; padding compensates so the text does not move.
- **Error:** stroke and label turn alert; an inline error (alert glyph and 13sp alert text) appears under the field and the first error of a form scrolls into view.
- **Read-only:** hairline stroke.
- **Amount field:** no box; the condensed 60sp numeral over a 2dp underline.

### Navigation
Shell tab bar on the ground with no navigation bar above pages; selected tab and title in ink, unselected in mute. The period switcher (month, year, arrows) is the top of the home and Insight.

### Settings rows
`SettingsRow` has a `Kind`: **Info** (not tappable, no arrow), **Action** (tappable, runs in place, no arrow) and **Navigation** (tappable, trailing arrow, "Apri" as the accessibility hint). "Backup ed export" in `Dati` is a Navigation row to a pushed page of three titled sections ("Account Google", "File di backup", "Foglio di calcolo") that mixes all three kinds, with a note under the Info row. A finished save answers with a snackbar ("Salvato in Download: <name>"), not a page.

### Edge-to-edge lists
A pushed list page (Spese, a category's page, Pagamenti regolari, Backup ed export, Ripristina backup, Licenze) draws under the transparent navigation bar: its list scrolls behind the gesture handle and ends above it, never on a hard line. The page takes only the top and side insets (`EdgeToEdgePage`, root grid `EdgeToEdgeRoot`); the bottom inset is taken by the scrolling content, as `ListScroll` on a scroll view and the `ListEnd` footer on a collection, and by what floats over the list, such as the Nuovo pagamento button.

### Category tile (signature)
The only place a category's icon and colour become pixels: a square (0 radius) tile in the category's palette ink, with a filled Material Symbols Rounded glyph in tile ink at 58% of the tile size. 36dp in rows, 48dp in the icon picker, 56dp as the editor's preview, 24dp inside chips. In the icon picker an unselected tile is neutral: the raised tone with a Mute glyph, so it reads as an icon on offer. Only the selected tile wears the category's ink, with the tile-ink glyph, inside its ring. The catalog holds 31 icons plus the "Altro" fallback; unknown keys fall back to "more" and grey.

### Category line (signature)
A full-width 14dp line under the home total: one flat segment per category, as wide as its share of spending, 3dp gaps, a 2dp minimum. Below it, a key on one line, never wrapped: 10dp swatch and 13sp caption per category, each opening its category. Over-budget categories come first (the furthest over first), then the others by spending; the key shows as many whole items as fit the width and drops the rest whole, never clipping one. A screen reader still hears every category in the line's description.

### Over-budget mark
In the home key, an over-budget category gets the filled Error glyph at 14sp in alert before its name, and the name turns alert in SemiBold. In the Categorie list the row gets the same glyph (16sp, alert) before its name, and its accessibility description adds "oltre il budget". On a category's page the budget sentence takes the home wording ("Hai superato il budget di 28,50 € su 150") in SemiBold alert after a 20sp Error glyph. The mark is the glyph plus the colour, not a word.

### Daily spend chart (signature)
104dp of bars plus 22dp of headroom: past days in chart-bar grey, today in ink, future days as 4dp hairline stubs, a 1.5dp dashed periwinkle pace line (4dp dash, 3dp gap) with its SemiBold 12sp label on a ground backing, and 12sp axis labels (start and end in mute, today's label in bold ink, the end labels giving way when it would touch them).

### Pace chart (signature)
"Spesa accumulata" (for a month or a year), the first chart of Insight. One slot per day (31) or month (12), 148dp of plot plus 22dp of headroom for labels, 2dp between slots. Each past slot is a cumulative bar in chart-bar grey. The automatic part (recurring payments) sits at its foot in mute, under a 2dp ground band, so the two read as separate pieces. Today is an ink bar, future slots are 4dp hairline stubs, and a slot with spending never draws thinner than 2dp.

The previous period is a 2dp dashed ink step line (4dp dash, 3dp gap) on the same scale, with its closing amount in 12sp mute on a ground backing. The budget is a 1dp outline hairline with its 12sp mute label. The gap at today is a 2dp ink marker beside today's slot. It spans the compared total and the spent total, with the euro difference in Bold 13sp ink over a 12sp mute caption. The axis is 12sp: start and end in mute, today in bold ink, the end labels giving way when it would touch them. Below it comes a key of 10dp swatches (a 14dp dashed one for the previous period) and 13sp mute names, each amount in SemiBold ink. No sentence follows the chart.

### Waterfall chart (signature)
"Da ottobre a novembre" (or "Dal 2025 al 2026"): how the to-date total moved between the compared periods, one column per bar with 3dp between columns, 134dp of plot plus 22dp of headroom. The start total is a chart-bar bar and the end total an ink bar, each with a Bold 12sp ink value and its period name in SemiBold 13sp underneath. Between them, each category that changed by at least 1 € floats as a step in its own ink, from the level before to the level after. Its SemiBold 12sp value sits above a rise or below a fall, and a 24dp category tile sits in the foot row under it. The rest folds into one "Altre categorie" step. A 1dp outline connector crosses each 3dp gap at the shared level, and a step never draws thinner than 2dp.

When the levels are close, the scale does not start at zero and both end bars are cut: a body, a 3dp break and a 3dp base at the foot, so a bar never claims to start at zero. A 12sp mute label under the foot row, aligned to the chart's left edge, names the floor ("Scala da 1.150 €"), and the screen-reader description says it too. It is empty and hidden when the scale starts at zero. No sentence follows the chart.

### Comparison chip
26dp raised chip after the budget sentence: an arrow glyph (east, north-east or south-east) and a SemiBold label. When spending is lower than last month it takes the positive container and on-positive text.

### Picker cells
Category grid: four columns of tile and 12sp name in a 76dp cell with 12dp radius; the selected cell takes a 2dp ink ring and the selected fill. Ring cells (icon and colour pickers) reserve 5dp of room (3dp gap and 2dp ring) so choosing never moves the pictures.

### Colour picker
The category's colour in the editor, under a "Colore" field label between the name and the icon picker. The six chromatic slots, never grey, as 40dp square swatches (0 radius) in ring cells. They sit in one row with the first on the left gutter, the last on the right gutter and equal gaps between, so the row lines up with the six columns of the icon grid below. The chosen swatch takes the 2dp ink ring. Each cell is described by the colour's Italian name and, when chosen, "Selezionato". While creating a category, a 13sp Mute caption under the row says "Consigliato il colore usato da meno categorie." and the suggestion starts selected; when editing there is no caption. Choosing a colour repaints the preview tile and the selected icon at once. An archived category shows neither picker.

## Do's and Don'ts

### Do:
- **Do** take every colour from `ThemeTokens` (through `ThemeColors`, or `@color/theme_*` on a widget) and `CategoryPalette`; give each new token a dark and a light value, with dark as the reference.
- **Do** keep pages on the graphite ground with a 24dp gutter, and separate list rows with 1dp hairlines.
- **Do** show a category as its square tile (dark glyph on its pastel ink) wherever it appears in a list, and in the editor's icon picker only for the selected icon, the others staying neutral.
- **Do** suggest the least-used chromatic slot for a new category by preselecting it, and let the user pick another of the six.
- **Do** mark selection and focus with a 2dp ink ring and compensating padding, so nothing shifts.
- **Do** keep every touch target at least 48 by 48dp, using padding and negative margins rather than enlarging the visual.
- **Do** pair every glyph with text or an accessibility description; tile and chip glyphs stay out of the accessibility tree and the container carries the label.
- **Do** mark an over-budget category with the alert Error glyph and alert colour, not with a word.

### Don't:
- **Don't** use shadows, blur, glows or gradients; raise a surface with the raised tone and an outline instead.
- **Don't** use palette colours for interface chrome, or coral for anything but the add action.
- **Don't** offer grey as a choice for a category, and don't paint every icon in the picker with the category's colour.
- **Don't** wrap list items in cards, or bring back tonal Material cards and progress bars over category card lists.
- **Don't** use Archivo Condensed ExtraBold for headings, or any face outside Archivo and Material Symbols Rounded.
- **Don't** add uppercase labels, letter-spacing, or kickers above titles.
- **Don't** round chart bars, line segments or category tiles, or draw data graphics with antialiased gaps.
- **Don't** design for landscape; the app is locked to portrait.
- **Don't** redraw or recolour the logo: the mark (a warm-white C ring with a coral segment, `docs/brand/`) is supplied by the owner.
