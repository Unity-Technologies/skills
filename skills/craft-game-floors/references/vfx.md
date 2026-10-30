# VFX

> Craft reference for the `craft-game-floors` skill. Relative links in this file resolve to the skill directory. **Load:** during the feel pass and whenever an effect is needed — [game-feel](game-feel.md) says *when* effects fire; this reference says *what* they look like. Colors come from [art-direction](art-direction.md)'s palette; glow interplay from [scene-lighting](scene-lighting.md)'s bloom threshold. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The rules (every effect, no exceptions)

- **Palette only** — start/gradient colors sampled from the art bible; a white default particle breaks style as hard as a gray material.
- **Short-lived** — 0.3–0.8 s lifetimes for event effects; size-over-lifetime shrinks to zero so nothing pops out of existence.
- **Readable at gameplay distance** — size and count tuned against the real camera ([camera-rigs](camera-rigs.md)), verified by capture, not in the inspector.
- **Pooled** — never Instantiate/Destroy per event. Reuse one ParticleSystem via `Stop`/`Clear`/`Play` (or one system with `Emit()`); when effects need real instances, pool them with [`UnityEngine.Pool.ObjectPool<T>`](https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Pool.ObjectPool_1.html) — built into the engine, zero dependency. Construct it with a createFunc plus actionOnGet/actionOnRelease so reset-on-release is baked in; take with `Get()`, return with `Release(item)`.
- **Nothing loops without a job** — a looping effect must be telling the player something (a shrine glows because it's interactive).

## The recipes (URP, built-in ParticleSystem)

- **Pickup burst** — 8–12 particles, sphere emission, radial start speed 2–3, slight gravity, palette accent → transparent gradient. Pair with the pop + blip ([game-feel](game-feel.md)).
- **Trail** — for moving collectibles and guides (the butterfly case): a small emitter with rate-over-distance, 0.5 s lifetime, shrinking. Sells motion better than the mover itself.
- **Ambient motes** — the interior seller: slow-drifting dust in the key light's direction, 10–20 alive, barely-visible palette-light color, large box emitter above the play space. This one effect makes a room feel inhabited.
- **Milestone confetti** — 30–50 particles, cone up, gravity on, 2–3 palette colors, 1.2 s — reserved for the full-screen moments so they stay special.
- **Movement puffs** — tiny 3–4 particle poofs on footfall/landing, ground-colored, 0.3 s.

Material — the paved default: `Sprites/Default` (or URP particle-unlit explicitly set to Transparent) tinted from the palette; the failure table below has the two traps. Additive/glow sparingly and only above the bloom threshold on purpose — one glowing thing per frame is an accent; five is soup.

## 2D delta

In a 2D scene, particles sort through the renderer like every sprite: set Sorting Layer and Order in Layer on the [`ParticleSystemRenderer`](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/particlesystemrenderer.md) (both inherited from `Renderer`) so effects draw above the sprites they comment on and below UI. Sorting Layer beats camera distance — an effect on the wrong layer hides behind the background no matter its Z.

## Failure modes

| Symptom | Root cause | Fix |
|---|---|---|
| particles render fully opaque despite an alpha tint | URP particle-unlit ignores alpha while Surface Type is Opaque | use `Sprites/Default`, or set the material's Surface Type to Transparent (or enable Alpha Clipping) |
| console floods with curve errors while animating velocity over lifetime | the x/y/z curves are in mixed modes (two set, one left default) | set **all three axes** in the same curve mode |

## Verify

Capture mid-effect at the gameplay camera (the done gate's "feedback visibility" axis needs the effect *in* the frame) — and time the capture by eval, not luck: assert the system's `particleCount > 0` at the capture moment. Pooling check: fire the effect ten times and assert the ParticleSystem instance count stayed constant before and after (a growing count means an Instantiate leaked in). Check the ambient motes read in a still — if they only register in motion, raise size slightly, never count.

## Expansion scope

VFX Graph for GPU-scale effects, shader-driven dissolves/impacts, decals, screen-space effects (speed lines, damage vignettes), effect LOD and budgets per platform.
