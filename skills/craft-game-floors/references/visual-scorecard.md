# Visual Scorecard

> Craft reference for the `craft-game-floors` skill. **Load:** at the end of every build session and after any visual change worth verifying. A single-floor touch-up owes only that floor's own Verify section; the full board below is the build session's done gate. Capture mechanics live in `unity-cli` (editor_play → capture_game_view → eval). Relative links in this file resolve to the skill directory. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The rule

**The first capture is a draft, never the deliverable.** A build session under these floors (any paved build) ends with scored frames, not a green compile. "It runs" and "it's done" are different claims; this gate is the difference.

## Procedure

1. **Record the Editor's starting state first** — whether Play mode is already running (a user-owned session is never restarted or stopped: score inside it or ask), and the current auto-tick and `runInBackground` values, so both can be restored after the gate. Then prove liveness with this four-call loop before trusting any capture: `editor set_auto_tick on` · script eval [`Application.runInBackground`](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/application/runinbackground.md) `= true` · `editor.playmode start` · a job wait on `Time.frameCount` with op `changed`. The command names here are the installed pipeline package's — confirm them with `unity command` before running. Run the loop every time: capture-on-demand renders frozen games as alive, and an unfocused Editor can freeze play at frame 1–2 while status still reports playing. **After the gate, restore the recorded auto-tick and `runInBackground` values and exit Play if the gate started it.**
2. Render a warm-up frame and discard it before every scored capture — the first render after a rebuild comes back blank (async shader compile).
3. Capture **three frames**: the standard gameplay angle · a character close-up · the busiest UI moment (mid-pickup or milestone, so feedback is *in* the frame). **UI in captures:** game-view capture can skip ScreenSpaceOverlay canvases entirely while the canvas is provably alive — for any frame that must show UI, either use a screen-level capture source where the installed pipeline package offers one (confirm its capture flags — names and `--view` options vary by version) or put the HUD on a ScreenSpaceCamera canvas, which renders in every capture path. Verify UI presence in the frame, not in the hierarchy. **Driving the game for captures:** simulated key events may not reach Input System polling in editor play mode — when a verification drive needs movement, compile a small debug-input hook into the controller rather than fighting the input simulator. **Where captures land:** `capture game_view` writes to `Assets/Captures/` — add `Assets/Captures/` to .gitignore before the first capture.
4. Score each axis 1–5. The palette-discipline instant fail is machine-checkable first: eval-scan every renderer for `Default-Material`, TMP for the stock font **left at default settings** (a deliberately styled stock font passes — placeholder-assets prescribes exactly that), **every ParticleSystem's startColor and every HUD panel Image for untinted white/gray** (the two elements that most often slip the palette while passing the material scan; a white sprite tint that deliberately preserves the sprite's own colors also passes — the scan flags defaults, not choices), before scoring — so axis 1's fail never rests on eyesight alone. **If the request authorized changes**, anything below 3 gets a fix, and spend at least one fix pass even on a passing board; **on an assessment-only ask** ("score it"), report the board and the recommended fixes and change nothing until the user says go. When tuning live via `eval`: a timed-out eval may still have executed — mutations are not idempotent-safe; long work goes through `run_script`.

## The six axes

| Axis | The question | Instant fail |
|---|---|---|
| Palette discipline | Does every color trace to the art bible? | default-gray material, skybox, or UI anywhere |
| Style uniformity | One style across character, environment, sky, UI? | style breaks at the character's edge |
| Lighting depth | Warm/cool separation? Shadow sides alive? | flat white directional look |
| Silhouette | Is the character identifiable filled black? Grade from the gameplay camera angle as well as a side view. | reads as "capsules" |
| UI intentionality | HUD on the palette, aligned, styled text? | default uGUI skin visible |
| Feedback visibility | Does the busy frame *show* the game answering the player? | a pickup frame with nothing happening |

## The 2D anchor set

For a 2D build, grade the same six questions through their 2D equivalents: palette discipline = every sprite tint traces to the bible (no default-white sprites); style uniformity = one consistent pixel density across all sprites, no mixed-resolution art; silhouette = the character reads at gameplay zoom, not just in the source asset; lighting depth = outline discipline instead — either every gameplay-critical sprite carries the outline treatment or none does; UI intentionality and feedback visibility grade unchanged. Where a 2D floor is missing, name the gap in the report rather than grading against a 3D axis the build cannot meet.

## Calibration anchors

- **5** — a promo still from a stylized indie (Monument Valley, A Short Hike): total palette and style discipline.
- **3** — a polished jam entry: simple assets, but deliberate light, consistent style, styled UI. **The paved floor — the minimum any build under these floors ships at.**
- **1** — untextured primitives under one white directional with default-gray UI: the documented starting point this gate exists to abolish.

## Report

Print the board in one fixed grammar — one line per axis, `axis: <score>/5 — <one-line evidence>` — so a later session can parse it, then what the fix pass changed, and attach the final captures. The scored frames are the session's proof-of-done artifact.

## Expansion scope

Baseline captures + visual regression across sessions, resolution/aspect sweeps, colorblind-safety check, bot-driven capture routes, per-genre anchor packs.
