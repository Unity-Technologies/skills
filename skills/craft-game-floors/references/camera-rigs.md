# Camera Rigs

> Craft reference for the `craft-game-floors` skill. Relative links in this file resolve to the skill directory. **Load:** whenever a gameplay camera is needed — never hand-roll one. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The opinion

The camera is half of game feel and the first thing a hand-rolled implementation gets wrong (a hand-rolled follow camera has shipped pointing at the outside of the house it was meant to film). Builds under these floors use **Cinemachine** (`com.unity.cinemachine` — on the paved package list, this skill's default install set, for any gameplay camera, 2D rigs included; install it if absent) and one of the recipes below. A hard-parented or static camera never ships.

**On new installs, take the current major — never 2.x.** On Unity 6 editors Cinemachine ships as a core package whose major the editor picks; where a version can be pinned, pin the 3.x line or newer (e.g. `com.unity.cinemachine@3.1`). 2.x is a different API surface (`CinemachineVirtualCamera` / `CinemachineCollider` vs the `CinemachineCamera` / `CinemachineDeoccluder` family from 3.x on), and package-install examples elsewhere still pin 2.x versions — every recipe here is written against the 3.x-family types and will not compile on 2.x.

**Existing projects: check the installed major first** (`Packages/manifest.json`), before any package change. A project already on Cinemachine **2.x** keeps it — upgrading the package breaks existing camera scripts across the API break, and a restore point recovers but does not make them compatible. On 2.x, apply this file's *opinions* (framing, damping, dead zones, FOV, shake-via-impulse) through the 2.x equivalents (`CinemachineVirtualCamera`, its Body/Aim stages, `CinemachineImpulseSource`), skip the 3.x-typed recipes and verifies, and name the version gap in the report. Migrating 2.x → 3.x is never a side effect of a craft fix: it happens only as an explicitly scoped ask, agreed with the user, following the [Cinemachine upgrade guide](https://docs.unity3d.com/Packages/com.unity.cinemachine@<VERSION>/manual/CinemachineUpgradeFrom2.html) and verified end-to-end afterward.

## Third-person follow (the 3D default)

[`CinemachineCamera`](https://docs.unity3d.com/Packages/com.unity.cinemachine@<VERSION>/manual/CinemachineCamera.html) with:

- **Third Person Follow** body — camera distance ~4–6 for interiors, shoulder offset (~0.5 right, ~1.2 up) so the character sits at the lower-third intersection, not dead center.
- **Damping** ~0.5–1.0 s on position — the camera *settles*, never teleports.
- **The rig inherits the follow target's yaw.** On a character that turns toward its movement, the camera whips to face whatever the character faces — hedges, walls, the void. Raise rotation damping, or follow a yaw-damped proxy transform instead of the character root.
- **[Deoccluder](https://docs.unity3d.com/Packages/com.unity.cinemachine@<VERSION>/manual/CinemachineDeoccluder.html) — interiors default OFF.** The interior recipe: skip the deoccluder, frame high enough that walls can't eat the lens; add one only for exteriors/orbit cams. Its tuning knobs go by different names across CM3 minor versions — verify the fields on the *installed* version before touching them — and its failure modes are in the table below.
- **FOV 50–60** for interiors (default 40 feels like a telescope indoors; 90 feels like a fisheye).
- Slight **look-ahead** in movement direction when the game is about exploring toward things.

## Orbit / inspect

[`CinemachineOrbitalFollow`](https://docs.unity3d.com/Packages/com.unity.cinemachine@<VERSION>/manual/CinemachineOrbitalFollow.html) with three-ring setup for player-rotatable cameras; recenter after ~2 s idle. Use for creature/vehicle games where the subject is the point.

## 2D follow

`CinemachineCamera` + **Position Composer**: dead zone ~0.1×0.1 (breathing room for small motion), look-ahead 0.5–1 s along velocity, damping asymmetric (loose horizontal, tight vertical for platformers). Pixel-art projects: pair with `2d-pixel-perfect`.

## Feel hooks

Screenshake and impact kicks route through **Cinemachine Impulse** (source on the event, listener on the camera) — the feel pass owns when they fire and how hard. Never move the camera transform directly for shake.

## Failure modes (deoccluder)

| Symptom | Root cause | Fix |
|---|---|---|
| interior camera collapses onto the character (a configured 4.2 m distance measured at 0.7 m) | default deoccluder settings resolving against room geometry | disable the deoccluder indoors; reframe higher |
| Editor stack overflow in Play mode (seen on 6000.4–6000.6) | the deoccluder's obstacle resolution recursing when an obstacle crosses the camera–target line | remove or disable the deoccluder on that camera |
| deoccluder silently does nothing | Group Framing active on the same camera | pick one — they don't compose |
| camera flips on slopes/loops | the deoccluder resolves against world-up while the target's local up differs | disable it for that rig |

## Verify

Capture during movement (via `unity-cli`): player on the lower-third line, no wall in front of the lens, horizon level. Then the measured check, by eval rather than eyeball: read the rig's configured camera distance off the Third Person Follow component and compare it to the live `(camera.transform.position − target.position).magnitude` — if the live value is under ~80% of the configured one, something is fighting the rig; disable the deoccluder first and re-measure. Walk the player into a corner and repeat both the capture and the distance read.

## Expansion scope

State-driven camera sets (gameplay/menu/photo), blend tuning, target groups for multiplayer framing (note: the deoccluder and Group Framing don't compose), split-screen, cutscene dolly tracks, mobile gyro/touch orbit.
