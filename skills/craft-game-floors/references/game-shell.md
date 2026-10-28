# Game Shell

> Craft reference for the `craft-game-floors` skill. **Load:** once the core loop works, before the feel pass concludes — and in any ad-hoc session, whenever the game boots straight into gameplay: such a build isn't done. UI construction mechanics live in the `ui` skill family; this reference owns which screens exist and how they behave. Relative links in this file resolve to the skill directory. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The opinion

A game that boots directly into gameplay reads as a demo. The shell is formulaic — which is exactly why an agent should never skip it: it's an hour of work that changes what the artifact *is*.

## The minimum shell (four states, one state machine)

One `GameState` enum (`Title, Playing, Paused, Ended`) on one manager; every screen is a palette-styled panel (see [art-direction](art-direction.md) — default uGUI skin never ships; if no art bible or Palette source exists yet, run that reference first — it is the root dependency). On a fresh URP template, import TMP Essential Resources before the first text element ([TMP font assets](https://docs.unity3d.com/Packages/com.unity.ugui@<VERSION>/manual/TextMeshPro/index.html)); the exact failure and fix are in the table below.

- **Title** — the worked treatment below. **Backdrop is the live game scene**, gameplay HUD hidden. Free production value; no separate art, and the style uniformity is automatic.
- **Pause** — `Time.timeScale = 0` + dimmed overlay; Resume / Restart / Quit. Audio ducks −6 dB (mixer snapshot, [game-audio](game-audio.md)). Feel/tween code must run on *unscaled* time while paused ([game-feel](game-feel.md)'s Tween has an unscaled path).
- **Settings** — Music and SFX sliders bound to the mixer parameters MusicVol and SFXVol (the audio floor's exposed names, [game-audio](game-audio.md)). Probe before binding: `mixer.GetFloat("MusicVol", out _)` must return true; if there is no mixer, bind the audio floor's static gain layer instead — and if there is no audio at all, run that floor first. That's the floor; more is expansion scope.
- **Ended** — win/lose banner as a *moment* (milestone recipe from [game-feel](game-feel.md)), the run's numbers (score/level/time), Restart + Title. Restart must produce a clean run — re-enter via scene reload or a real reset path, never leftover state.

## Worked title treatment (imitate this, then vary)

- **Layout**: game name centered at ~60% screen height, the shell's largest text; prompt line centered at ~25% height, small, letter-spaced uppercase. Nothing else on screen — no placeholder menus.
- **Palette roles**: name in the palette's accent with a soft shadow in the palette's darkest value; prompt in the neutral light tone at ~70% alpha; any scrim behind text is the darkest palette value at 60–80% alpha, never pure black.
- **Backdrop camera (3D)**: slow idle orbit of the play space, roughly 4–6°/s, framing the character or centerpiece off-center (rule of thirds); or a locked wide shot if the scene reads better still.
- **2D delta**: the backdrop is a slow parallax pan, not an orbit — pan across the level at ~0.5 units/s with background layers moving at fractional speeds.
- **Prompt text**: "press any key" (or the real bind), alpha pulsing 0.5↔1.0 over ~1.6 s on unscaled time — motion is what marks it interactive.

## Transitions

Every state change crosses a 0.3 s fade (one fullscreen black-from-palette overlay, alpha tween on unscaled time). Hard cuts between screens read as broken.

## Failure modes (verify by capture, not compile)

| Exact error / symptom | Root cause | Fix |
|---|---|---|
| `Value cannot be null (source)` on the first `TextMeshProUGUI` | fresh URP templates ship without TMP Essential Resources, so the default font asset is null | import the essentials first — editor-side `TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false)` — then recreate the element |
| UITK text input renders at zero width despite an authored width | the default theme's flex-basis collapses it | set `flex-basis` explicitly on the input |
| UITK panel renders dark with no warning | an unresolved `var(--token)` in the stylesheet | define or correct the token |
| UITK styles silently absent | a `.tss` that `@import`s a `.uss` written in the same batch can drop it | author the `.uss` in its own pass, or inline the styles |

## Verify

Drive the full loop headlessly via `unity-cli` (eval state changes + capture each screen): boot → Title frame, pause → Paused frame, end → Ended frame, restart → clean second run. "Clean" is asserted, not eyeballed: eval-read the `GameState` enum at each step to confirm the transition landed, re-read the run counters after restart (score/XP/level back at initial values), and read the Editor log for a zero console-error count. Caveat when driving via `eval`: a timed-out eval may still have executed — mutations are not idempotent-safe; long work goes through `run_script`.

## Expansion scope

Save slots and persistence, options that persist (audio, quality, language via `localization`), input rebinding UI, accessibility (text size, contrast, screen-shake toggle), controller/focus navigation, credits and legal screens.
