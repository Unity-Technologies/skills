# Playable Loop

> Craft reference for the `craft-game-floors` skill. Relative links in this file resolve to the skill directory. **Load:** once a character moves and before any content pass, and whenever a build "has nothing to do", has no way to win or lose, or can't be restarted without leaving Play mode. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The floor rule (a default, not a mandate)

**By default, a run can be started, played toward a visible goal, won or lost, and restarted — all without leaving Play mode.** A world you can only walk around in reads as a tech demo, however good it looks; closing the loop is what makes it a game, so when the ask is open on shape, close the loop before adding content: one verb, one goal, one pressure, then build outward.

**The user's stated design beats the default.** This floor fills the engine's "nothing to do" default, never the user's choice (the same rule every floor follows, from the mid-flow check): when the ask names a shape without win or lose — a sandbox, a toy, an endless sim, a walking experience — build that shape. Apply only the parts of the loop that still fit (a visible way to engage, a reset or restart that works without leaving Play mode), skip the rest without arguing, and name in the report which parts were set aside because of the user's design. At most one line offering the full loop, never a second.

## The anatomy (five parts of the default loop)

- **Verb** — the one thing the player does (move and collect, aim and shoot, place, dodge). The first loop gets exactly one verb; second verbs and mechanics are content passes after the loop closes.
- **Goal** — a state the verb moves toward, stated on the HUD from the first frame ("Collect 10", "Reach the gate", "Survive 60s"). A goal the player must infer is a missing goal. [game-shell](game-shell.md) owns the HUD's styling; this floor owns that the goal is on it.
- **Pressure** — the thing that can end the run against the player: a timer, a hazard, lives. Without pressure the goal is a checklist, and the win means nothing.
- **End states** — win and lose both reachable in normal play, each a *moment* ([game-feel](game-feel.md)'s milestone template), each landing on the end screen ([game-shell](game-shell.md)).
- **Restart** — one input from either end state into a fresh run. Prefer reloading the scene with [`SceneManager.LoadScene`](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/scenemanagement/scenemanager/loadscene.md) over hand-resetting fields — and name the trap either way: statics, `DontDestroyOnLoad` objects and singletons survive a scene reload, so any run state they hold (score, timers, spawned-object registries) must be reset explicitly or the second run inherits the first run's state.

## The driver (one opinion, one example)

The opinion: **run state has one owner**, and the other floors subscribe to it — the shell's screens, the feel pass's moments, the live floor's score submits and any telemetry hang off its transitions instead of reaching into gameplay code. Expose the goal and pressure numbers (target count, run seconds, lives) on that owner so the eval → capture → eval loop can tune them live.

A project that already has a run-state owner — its own manager, a state machine, an ECS system — keeps it: wire the floors to what exists rather than restructuring, because a floor never replaces a developer's choice. Only when the project has none, the example below is one small shape that works:

```csharp
public enum RunState { Ready, Running, Won, Lost }

// One owner of run state, transitions through these methods, and an event per
// transition so shell/feel/live wire in without reaching into gameplay code.
public class GameLoop : MonoBehaviour
{
    public RunState State { get; private set; } = RunState.Ready;
    public event System.Action<RunState> StateChanged;

    public void StartRun()            { Set(RunState.Running); }
    public void Win()                 { if (State == RunState.Running) Set(RunState.Won); }
    public void Lose()                { if (State == RunState.Running) Set(RunState.Lost); }

    void Set(RunState s) { State = s; StateChanged?.Invoke(s); }
}
```

The shell floor later adds pause to this same owner rather than a second state machine, because two owners drift apart (a menu showing a live run the loop already ended): see [game-shell](game-shell.md).

## Build discipline

1. Close the loop with placeholder everything — primitive pickups, a cube gate, a number for the timer — before any content or second mechanic, because an open loop makes every later pass unverifiable ("is this fun?" has no answer without a run that ends).
2. One system per pass, and the game is playable at the end of every pass — a pass that leaves the loop broken overnight is two passes.
3. The feel pass ([game-feel](game-feel.md)) comes after this floor by design: its pickup, milestone and end-state recipes need real loop events to hang on.

## Verify

Via `unity command script eval` in Play mode, drive the loop end to end — a loop a script can't drive is also a loop no later floor can hook:

1. Assert `Ready`, fire the start input (or `StartRun()`), assert `Running`.
2. Drive the verb once and assert the progress value changed (counter up, timer down, distance closed).
3. Capture one mid-run frame with the goal and progress visible on the HUD — [visual-scorecard](visual-scorecard.md)'s feedback-visibility axis grades this frame, so take it while the run is still live.
4. Force the win by eval (set the progress to the goal), assert `Won` and the end screen active.
5. Fire restart. A scene reload brings the owner back in `Ready`, so assert `Ready` and the progress value at its start, then `StartRun()`, assert `Running`, and drive the verb once to prove the second run is live — this is the pass that catches surviving statics.
6. Force the lose by eval (run the pressure out: timer to zero, lives to zero), assert `Lost` and the end screen active.

On a user-chosen shape without win or lose, verify the parts that exist instead: drive the verb, assert the visible way to engage (the floor rule's term) responds — a placed object persisting, a counter or reaction updating — and prove the reset restores a fresh state.

Caveat: a timed-out `eval` may still have executed — mutations are not idempotent-safe; put anything long through `run_script`.

## The boundary (what this floor is not)

The floor is the loop *closing*, never the game's mechanics. A second verb, a double jump, enemy AI, level design, difficulty curves and content are the user's game code — route those asks to the user's own direction (or the matching catalog skill), not this reference. Score submits and saves on the end states belong to [live-floor](live-floor.md); the screens to [game-shell](game-shell.md); the moments to [game-feel](game-feel.md).

## Expansion scope

Per-genre loop profiles (platformer, shooter, cozy), wave/level progression, difficulty ramps, meta loops (upgrades between runs), tutorialization of the first run.
