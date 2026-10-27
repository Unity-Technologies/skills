# Character Motion

> Craft reference for the `craft-game-floors` skill. Relative links in this file resolve to the skill directory. **Load:** when a character glides instead of walks, freezes instead of idles, or was assembled from primitives with no animation source. [art-direction](art-direction.md) owns silhouette and proportion; this reference makes the character *move* like a character. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The opinion

A character that translates across the floor in a fixed pose reads as a chess piece. Agent-built characters rarely have rigs — so the paved answer (this skill's default, rig or no rig) is **procedural motion**: a handful of sine waves and springs, driven by velocity, layered on the primitive hierarchy. It is cheap, tunable live via `unity-cli` eval, and the single biggest step from "tech demo" to "game".

## The layers (one ~80-line component)

Drive everything from normalized speed (`speed / maxSpeed`) so it scales:

- **Gait bob** — body y-offset: `sin(t · stepFreq · 2π) · bobAmp · speedN`, stepFreq ~2.5 Hz at full speed. Add a half-frequency roll (z-rotation, ~2°) so weight shifts side to side.
- **Limb swing** — legs/arms rotate on x via the same sine, opposite limbs phase-shifted by π. Even cylinders read as walking when they alternate.
- **Lean** — pitch the body into acceleration (~8° at full accel) and bank into turns (~5°). Compute from velocity delta; damp it.
- **Squash & stretch** — jump: stretch y 1.1 on rise; land: squash y 0.85 / xz 1.1 for 0.1 s, ease out with the feel pass's Ease helper.
- **Secondary motion** — tail, ears, antennae follow with delay: each segment lags its parent's rotation by 0.05–0.1 s (a one-line exponential damp is enough). This is where charm lives.
- **Head look** — head yaws toward movement direction or the nearest point of interest, clamped ~60°, damped.
- **Settle, never freeze** — on stop, blend gait amplitude to zero over 0.3 s and hand off to the idle breathe/sway (from [art-direction](art-direction.md)); a hard stop at full pose is the giveaway.

## Physics note (paid for live, docs-confirmed)

The [collider interaction rules](https://docs.unity.com/en-us/engine/<VERSION>/manual/physics-section/physics-overview/collision-section/collider-interactions/create-trigger.md) govern when trigger events fire; the trap this floor pays for: a NavMesh-driven character with its CharacterController disabled has **no collider at all**, so pickups' `OnTriggerEnter` silently never fires. Keep a kinematic Rigidbody + trigger SphereCollider on the character root so interaction survives whichever component is steering. NavMesh surfaces, agents, and links themselves are `initialize-ai-navigation`'s job — this note only keeps the character touchable while an agent drives it.

## Tuning

Expose bobAmp, stepFreq, lean, squash on the component; tune live in Play mode via eval → capture → eval (no recompiles — the proven inner loop). Caveat: a timed-out `eval` may still have executed — mutations are not idempotent-safe; put anything long through `run_script`.

## Verify

Capture three frames during movement at intervals that don't alias the gait — at 2.5 Hz, samples one second apart land after exactly five half-cycles and can repeat the pose, so sample at ~0.7 s and ~1.3 s offsets (or any interval that isn't a multiple of the configured step period). The pose must differ visibly in all three. Run the machine form of the same check alongside: eval-sample the body's `localPosition.y` and one limb's `localRotation` at the three timestamps — all three samples must differ. Then stop the character and sample twice 0.5 s apart: the values must still differ (breathing, not frozen); if they match, the settle blend never handed off to idle — check the amplitude blend-out. Finish with one interaction check: drive the character over a pickup and assert its `OnTriggerEnter` fired (counter or log line) — a silent pickup means the kinematic Rigidbody or trigger collider is missing from the root.

## Expansion scope

Animator blend trees and state machines, humanoid retargeting onto imported/generated rigs, foot IK and slope adaptation, animation events driving feel/audio hooks, per-genre locomotion profiles.
