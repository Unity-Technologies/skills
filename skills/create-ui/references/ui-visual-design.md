# Visual Design for Game UI

Read this before styling new UI, and again when reviewing captures. It applies to every framework; the notes in brackets name the UI Toolkit (USS) or uGUI way to do each thing. Paths such as `references/uitk-svg-icons.md` resolve to the skill folder.

## Pick a direction first

Choose a visual direction that fits the game's genre and tone, and state it in one sentence before writing any styles. Keep every element consistent with it.

## Palette

- A panel color family in 2–3 shades, one primary accent, semantic colors where the game needs them (health, mana, success, warning, rarity), and text colors for primary, secondary, and disabled text.
- Define colors once as tokens and reuse them [USS custom properties: `--accent: #F2B21E;` then `var(--accent)`; uGUI: a static color table in C#].
- Contrast: body text at least 4.5:1 against its background, large numbers and headings at least 3:1. HUD text over the 3D world needs a backing panel, an outline, or a shadow [USS `text-shadow`, `-unity-text-outline-width`; TMP outline and underlay].

## Type scale

- 4–5 sizes from a ratio, for example 14 / 18 / 24 / 32 / 48 px at a 1920×1080 reference: caption, body, label, heading, display.
- Big, bold numbers for values players read at a glance (health, ammo, timer, score); small letter-spaced captions above them [USS `letter-spacing`].
- One or two weights: bold for values and headings, regular for descriptions.
- Nothing smaller than the caption size. Secondary text gets a dimmer color, not a smaller size.

## Spacing and layout

- A spacing scale such as 4 / 8 / 12 / 16 / 24 / 32 px, used for padding and margins everywhere. Each panel type gets one consistent inner padding.
- HUD elements sit in safe margins from the screen edges (24–40 px at 1080p) and align to shared edges and baselines.
- Size a full-screen menu (a skill tree, settings, an inventory) to fill most of the screen, about 70–85% of the reference width, so its content uses the type scale instead of shrinking to fit, and fill that space: enough rows or nodes to make it look like a real game's screen, or a side pane with details or a preview, rather than a half-empty panel.
- Group related information and separate groups with space before reaching for lines and borders.
- Visual hierarchy: the information players need most is the largest, brightest, and placed where they look (screen corners for HUDs, center for prompts).

## Shapes and depth

- One corner-radius family, and border widths that match the direction (1–2 px crisp, 4–6 px cartoon).
- Depth from layered panels, a darker bottom edge, inner highlights, and shadows [USS: `filter: drop-shadow(...)` or a nested element offset behind the panel; uGUI: a Shadow or Outline component or a sprite].
- Give each distinct thing (a skill, an item, a menu category) its own icon, because shared icons make different things look interchangeable.
- Icons share one style (filled or stroked) and one size grid (16 / 24 / 32 / 48 px) [USS: SVG icons, see `references/uitk-svg-icons.md`; custom shapes with Painter2D, see `references/uitk-painter2d.md`].

## States and motion

- Every interactive element has visibly different normal, hover, focus (keyboard and gamepad), pressed, and disabled states. Keep one clear selection highlight for controller navigation.
- Transitions of 100–250 ms for hover and press [USS `transition-property`, `transition-duration`, `transition-timing-function`; uGUI: Selectable color tint or animation].
- Feedback for value changes: a pulse when health drops, a count-up for score, a pop when an item is picked up, a fade or slide when a panel opens [USS transitions on `scale`, `translate`, `opacity`, or `element.style` tweens from C#].
- Add a placeholder binding for sound effects on button clicks or selections so users can easily attach sound to their UI.

## Content and context

- Believable sample data: real-sounding names, numbers, items, and quest text, so the screen reads as a real game.
- Show the UI in a context that sells it: a simple scene or backdrop behind a HUD, a title backdrop behind menus, a dimmed overlay behind modal panels.
- A small controller script makes the screenshot honest: values move, timers run, and states can be shown (for example a damaged health bar or a selected tab).

## Reviewing a capture

Capture the Game view in Play Mode at the reference resolution, then check, in order:

1. **Hierarchy** — squint at the capture: does the most important information stand out first?
2. **Readability** — is every piece of text readable against what is behind it, and at least the caption size?
3. **Spacing and alignment** — do edges line up, are gaps consistent, are margins even?
4. **Defects** — anything clipped, overlapping, stretched, squashed, empty, or off-screen?
5. **Direction** — does it look like the chosen direction and genre throughout?
6. **Behavior** — do hover, press, and value changes show up (capture a second state if needed)?
7. **Colors** — does the color theory make sense here? Are they helping the intent of the UI element?

Fix the most visible problem first, capture again, and repeat. Two or three passes usually take a screen from correct to polished.
