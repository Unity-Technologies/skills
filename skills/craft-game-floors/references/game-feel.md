# Game Feel

> Craft reference for the `craft-game-floors` skill. Relative links in this file resolve to the skill directory. **Load:** in the feel pass after the core loop works, and whenever actions feel flat, snappy-less, or unacknowledged. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The floor rule

**Every player-visible state change gets a visible + audible answer within 100 ms.** A pickup that just disappears, a bar that snaps, a level-up that's only text — each reads as broken, not minimal.

## The utility (author once, no external deps)

One static `Ease` class — three curves plus a coroutine tween helper — is enough that nobody reaches for a package. The tween runs on **unscaled time by default** so pause fades and UI keep moving at `Time.timeScale = 0`; pass `unscaled: false` for gameplay tweens that should freeze under pause or hit-stop.

```csharp
public static class Ease
{
    // The three curves the recipes name — OutCubic, OutBack (classic 1.70158 overshoot) and
    // OutElastic — use the standard formulas from an easing reference such as https://easings.net.

    // The opinionated part: tweens run on UNSCALED time by default, so shell and UI motion
    // survives timeScale 0; gameplay tweens that should freeze under pause pass unscaled: false.
    public static IEnumerator Tween(
        float duration, System.Action<float> apply, System.Func<float, float> ease, bool unscaled = true)
    {
        for (float t = 0f; t < 1f; t += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / duration)
        {
            apply(ease(t));
            yield return null;
        }

        apply(1f);
    }
}
```

## The recipes

- **Pickup**: scale-pop the collector or a proxy (1 → 1.25 → 1, 0.15 s, OutBack), 8–12 palette-colored particles, a pickup blip (the audio floor's synthesized recipe), floating "+XP" text rising ~0.8 units and fading over 0.6 s.
- **Numbers animate, never snap**: XP/health bars tween fill over 0.3 s OutCubic; counters count up. (Set the uGUI `Image` sprite — `Type.Filled` silently no-ops with a null sprite; found live.)
- **Milestone (level-up, win)**: a *moment*, never just text — the worked sequence below is the template.
- **Movement life**: idle breathing bob (sine, ~2 s period), slight lean into movement direction, landing squash (y 0.85 for 0.1 s). See [art-direction](art-direction.md) for why nothing is statuary.
- **Impacts (action games)**: screenshake via Cinemachine Impulse ([camera-rigs](camera-rigs.md)), hit-stop via [`Time.timeScale`](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/time/timescale.md) `= 0.05` for 50–80 ms of *unscaled* time — always through one manager so effects never fight.

## The worked milestone: level-up

Exact values; retime to taste but keep the shape (flash → sting → banner → payoff → hold):

| When | What | Values |
|---|---|---|
| 0 ms | Flash | fullscreen overlay in the palette accent, alpha 0.6 → 0 over 100 ms, OutCubic, unscaled |
| 0 ms | Sting | the audio floor's three-note ascending sting starts with the flash |
| 50 ms | Banner in | "LEVEL 7" scales 0 → 1 over 350 ms OutBack — overshoots to ~1.1 around 250 ms |
| 50 ms | Burst | 30–50 confetti particles, cone up, 2–3 palette colors |
| 400 ms | Payoff | XP bar refills to the new floor over 0.3 s OutCubic; the level counter counts up |
| 1,500 ms | Banner out | scales 1 → 0 over 200 ms OutCubic; overlay and particles are gone by now |

Input stays live throughout — the moment is a layer over play, never a lock. Run every tween in it unscaled so a level-up landing on the pause edge can't freeze mid-flash.

## Tunables stay visible

Expose the numbers (pop scale, tween durations, shake amplitude) on one component or a debug overlay so the iterate-by-looking loop (`unity-cli` capture → eval → capture) can tune them live without recompiles. Caveat: a timed-out `eval` may still have executed — mutations are not idempotent-safe; put anything long through `run_script`.

## Verify

Via `unity-cli` eval in Play mode: trigger one pickup and one milestone, then assert — the popped transform (the collector or its proxy — whichever the pickup recipe animated) returned to its **pre-pop baseline scale** within 0.5 s (record the baseline before triggering; characters often have non-unit root scales, so never assert against literal 1), the XP bar's `fillAmount` sampled twice 0.1 s apart mid-tween gives two different values (animating, not snapping), and the floating text object exists during the moment and is destroyed within ~1 s. Capture the busiest instant with the **screen** source so the feedback is in the frame. On any miss, the fix is wiring, not taste — confirm the gameplay event actually calls the feel layer.

## Expansion scope

Curve-preset library, UI transition system, controller rumble, frame-event-driven animation feedback, per-genre feel profiles (platformer vs. shooter vs. cozy).
