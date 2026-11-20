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
- Seven flat pastel category inks, each a dark glyph on a square tile; the app assigns them, the user never picks.
- Charts are hard-edged flat fills with 3dp gaps (2dp between the pace chart's slots), antialiasing off.
- Actions are pills; selection is an ink ring plus a selected fill, never a colour swap.

## Colors

A graphite-and-warm-white neutral world, one coral accent, and a seven-slot pastel palette that only categories may wear. Every colour has a dark and a light value; the dark one is the reference and the `-light` sibling in the frontmatter is its paper-scheme twin. `src/Centesimo.App/Theming/ThemeColors.cs` is the only colour table in the app; `CategoryPalette.cs` is the only palette.

### Primary
- **Signal Coral** (`accent-coral`, light `accent-light`): the add action. The plus of "Aggiungi spesa", the microphone in the split pill, the plus on the extended FAB and tonal button, the on-state of a switch. It marks the one thing the app exists for.

### Secondary
- **Pace Periwinkle** (`pace-periwinkle`, light `pace-light`): the dashed daily-average line of the home spend chart and its label "media per restare nel budget". Used nowhere else: the Insight charts draw their previous-period line in ink.

### Tertiary
- **Alert** (`alert`, light `alert-light`): errors, inline field errors, the destructive outlined button, and an over-budget category in the home key. In dark it shares the coral hue; in light it deepens to a true red (#C8261A) so it never reads as the accent.
- **Positive container / on-positive** (`positive-container`, `on-positive-container`): only for good news: the comparison chip when this month is lower than the last, and import success checks.

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

**The App Assigns the Colour Rule.** A new category gets the chromatic slot the fewest categories hold (first in palette order on a tie). There is no colour picker, and grey is never handed out; it only shows what has no known colour.

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

**The No Small Caps Rule.** No uppercase labels, no letter-spacing, no kickers above titles. A section is named by its title in sentence case.

## Layout

A single portrait column with a 24dp side gutter on every page. The home is a fixed 56dp top row (the period switcher: month in bold, year in regular mute, previous and next arrows as 48dp buttons), a scrolling middle, and a bottom dock holding the split pill with 24dp side and 12dp vertical margins, above the four-tab navigation bar (`Oggi`, `Insight`, `Categorie`, `Impostazioni`).

Lists are rows, not cards: an expense row is 58dp high (36dp tile, 14dp gap, title over caption, amount right-aligned) with a 1dp hairline under it; a settings row is at least 64dp with the same 36/14 rhythm. Empty states are centred stacks with 32dp sides and 56dp of top room under a 40sp mute glyph. Form field labels start 24dp below what is above them.

The spacing rhythm is 4, 6, 8, 12, 14, 16, 24dp, with 3dp gaps inside charts (2dp between the 31 slots of the pace chart). Every touch target is at least 48 by 48dp; where a visual element is smaller (a 32dp chip, a key item, the "Vedi tutte" link) padding makes up the target and negative margins give the room back, so the visual rhythm is not loosened to fit the finger.

**The Rows Not Cards Rule.** Collections are hairline-divided rows on the ground. A bordered card is for an isolated message (an error, a note), not for list items.

## Elevation & Depth

Flat by construction: there is no shadow in the codebase. Depth is tonal and linear: the ground, one raised graphite tone for floating or interactive surfaces, a 1dp outline around those surfaces, and 1dp hairlines between rows. A bottom sheet separates from the page with a 50% scrim, not a shadow.

**The Graphite Ground Rule.** No shadows, no blur, no glow, no gradients. If a surface needs to stand forward, it takes the raised tone and an outline stroke.

## Shapes

Two families that never mix: pills for what you press, and square or barely rounded geometry for what you read.

- **Pills** (radius = half the height): primary button 56dp, filled and outlined buttons 48dp, extended FAB 56dp, split pill 60dp, segmented control 40dp, listening toast 36dp.
- **Square** (0): category tiles, category line segments, chart bars, fraction bars, palette swatches.
- **Field** (4dp): text and date fields, menus.
- **Chip** (8dp): choice chips.
- **Container** (12dp): info cards and category picker cells.
- **Card** (16dp): the bordered surface card.
- **Sheet** (28dp top corners only): bottom sheets.

**The Hard Edge Rule.** Data graphics are drawn with antialiasing off, flat fills and 3dp gaps (2dp between days in a category's bars and between the pace chart's slots). Small values keep a visible minimum (2dp sliver, 4dp future stub) instead of vanishing.

## Components

### Buttons
Confident ink pills, no elevation.
- **Shape:** full pill (radius half the height).
- **Primary:** ink fill, ground text, Archivo Bold 17sp, 56dp high, 24dp sides: the docked action of a full-screen editor.
- **Filled:** the same at 48dp and 16sp: the main action of a form that is not docked.
- **Outlined:** transparent, 1dp mute stroke, ink Bold 16sp, 48dp. **Danger outlined** swaps stroke and text to alert.
- **Tonal:** raised fill, outline stroke, coral plus glyph: the one action of an empty page.
- **Text:** transparent, ink SemiBold, 48dp minimum.
- **Icon:** 48dp square, 24sp glyph in ink; disabled at 30% opacity.
- **Disabled:** a disabled primary or the first-run split pill drops to 38% opacity.

### Split pill (signature)
The home's bottom dock: one 60dp raised pill with an outline stroke, "+ Aggiungi spesa" (coral plus, Bold 17sp ink) filling the left, a 1dp outline divider, and a 76dp right segment with the coral microphone (26sp). Hold the microphone to dictate; while listening, a small outlined raised toast ("Registra… rilascia per inviare") floats 36dp above it.

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
- **Bottom sheet:** ground fill, 28dp top corners, 24dp sides, over the scrim.

### Inputs / Fields
- **Style:** outlined box, 4dp radius, 56dp, transparent fill, 16sp text, 16dp sides; the SemiBold 12sp label sits on the top edge with a ground-coloured backing.
- **Focus:** the stroke and label turn ink and the stroke widens to 2dp; padding compensates so the text does not move.
- **Error:** stroke and label turn alert; an inline error (alert glyph and 13sp alert text) appears under the field and the first error of a form scrolls into view.
- **Read-only:** hairline stroke.
- **Amount field:** no box; the condensed 60sp numeral over a 2dp underline.

### Navigation
Shell tab bar on the ground with no navigation bar above pages; selected tab and title in ink, unselected in mute. The period switcher (month, year, arrows) is the top of the home and Insight.

### Edge-to-edge lists
A pushed list page (Spese, a category's page, Pagamenti regolari, Licenze, Importa) draws under the transparent navigation bar: its list scrolls behind the gesture handle and ends above it, never on a hard line. The page takes only the top and side insets (`EdgeToEdgePage`, root grid `EdgeToEdgeRoot`); the bottom inset is taken by the scrolling content, as `ListScroll` on a scroll view and the `ListEnd` footer on a collection, and by what floats over the list, such as the Nuovo pagamento button.

### Category tile (signature)
The only place a category's icon and colour become pixels: a square (0 radius) tile in the category's palette ink, with a filled Material Symbols Rounded glyph in tile ink at 58% of the tile size. 36dp in rows and the picker, 24dp inside chips. The catalog holds 31 icons plus the "Altro" fallback; unknown keys fall back to "more" and grey.

### Category line (signature)
A full-width 14dp line under the home total: one flat segment per category, as wide as its share of spending, 3dp gaps, a 2dp minimum. Below it, a key on one line, never wrapped: 10dp swatch and 13sp caption per category, each opening its category. Over-budget categories come first (the furthest over first), then the others by spending; the key shows as many whole items as fit the width and drops the rest whole, never clipping one. A screen reader still hears every category in the line's description.

### Over-budget mark
In the home key, an over-budget category gets the filled Error glyph at 14sp in alert before its name, and the name turns alert in SemiBold. In the Categorie list the row gets the same glyph (16sp, alert) before its name, and its accessibility description adds "oltre il budget". On a category's page the budget sentence takes the home wording ("Hai superato il budget di 28,50 € su 150") in SemiBold alert after a 20sp Error glyph. The mark is the glyph plus the colour, not a word.

### Daily spend chart (signature)
104dp of bars plus 22dp of headroom: past days in chart-bar grey, today in ink, future days as 4dp hairline stubs, a 1.5dp dashed periwinkle pace line (4dp dash, 3dp gap) with its SemiBold 12sp label on a ground backing, and 12sp axis labels (start and end in mute, today's label in bold ink, the end labels giving way when it would touch them).

### Pace chart (signature)
"Il passo del mese" ("Il passo dell'anno" for a year), the first chart of Insight. One slot per day (31) or month (12), 148dp of plot plus 22dp of headroom for labels, 2dp between slots. Each past slot is a cumulative bar in chart-bar grey. The automatic part (recurring payments) sits at its foot in mute, under a 2dp ground band, so the two read as separate pieces. Today is an ink bar, future slots are 4dp hairline stubs, and a slot with spending never draws thinner than 2dp.

The previous period is a 2dp dashed ink step line (4dp dash, 3dp gap) on the same scale, with its closing amount in 12sp mute on a ground backing. The budget is a 1dp outline hairline with its 12sp mute label. The gap at today is a 2dp ink marker beside today's slot. It spans the compared total and the spent total, with the euro difference in Bold 13sp ink over a 12sp mute caption. The axis is 12sp: start and end in mute, today in bold ink, the end labels giving way when it would touch them. Below it comes a key of 10dp swatches (a 14dp dashed one for the previous period) and 13sp mute names, each amount in SemiBold ink, then a sentence in lead style.

### Waterfall chart (signature)
"Da ottobre a novembre" (or "Dal 2025 al 2026"): how the to-date total moved between the compared periods, one column per bar with 3dp between columns, 134dp of plot plus 22dp of headroom. The start total is a chart-bar bar and the end total an ink bar, each with a Bold 12sp ink value and its period name in SemiBold 13sp underneath. Between them, each category that changed by at least 1 € floats as a step in its own ink, from the level before to the level after. Its SemiBold 12sp value sits above a rise or below a fall, and a 24dp category tile sits in the foot row under it. The rest folds into one "Altre categorie" step. A 1dp outline connector crosses each 3dp gap at the shared level, and a step never draws thinner than 2dp.

When the levels are close, the scale does not start at zero and both end bars are cut: a body, a 3dp break and a 3dp base at the foot, so a bar never claims to start at zero. The sentence below the chart names the floor.

### Comparison chip
26dp raised chip after the budget sentence: an arrow glyph (east, north-east or south-east) and a SemiBold label. When spending is lower than last month it takes the positive container and on-positive text.

### Picker cells
Category grid: four columns of tile and 12sp name in a 76dp cell with 12dp radius; the selected cell takes a 2dp ink ring and the selected fill. Ring cells (icon picker) reserve 5dp of room (3dp gap and 2dp ring) so choosing never moves the pictures.

## Do's and Don'ts

### Do:
- **Do** take every colour from `ThemeColors` and `CategoryPalette`; give each new token a dark and a light value, with dark as the reference.
- **Do** keep pages on the graphite ground with a 24dp gutter, and separate list rows with 1dp hairlines.
- **Do** show a category as its square tile (dark glyph on its pastel ink) wherever it appears in a list or picker.
- **Do** let the app assign a new category its least-used palette slot.
- **Do** mark selection and focus with a 2dp ink ring and compensating padding, so nothing shifts.
- **Do** keep every touch target at least 48 by 48dp, using padding and negative margins rather than enlarging the visual.
- **Do** pair every glyph with text or an accessibility description; tile and chip glyphs stay out of the accessibility tree and the container carries the label.
- **Do** mark an over-budget category with the alert Error glyph and alert colour, not with a word.

### Don't:
- **Don't** use shadows, blur, glows or gradients; raise a surface with the raised tone and an outline instead.
- **Don't** use palette colours for interface chrome, or coral for anything but the add action.
- **Don't** offer a colour picker for categories, and don't assign grey.
- **Don't** wrap list items in cards, or bring back tonal Material cards and progress bars over category card lists.
- **Don't** use Archivo Condensed ExtraBold for headings, or any face outside Archivo and Material Symbols Rounded.
- **Don't** add uppercase labels, letter-spacing, or kickers above titles.
- **Don't** round chart bars, line segments or category tiles, or draw data graphics with antialiased gaps.
- **Don't** design for landscape; the app is locked to portrait.
- **Don't** redraw or recolour the logo: the mark (a warm-white C ring with a coral segment, `docs/brand/`) is supplied by the owner.
