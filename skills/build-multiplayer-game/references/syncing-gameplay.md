# Syncing gameplay

## Contents

- [Decide each value](#decide-each-value)
- [GhostObject state](#ghostobject-state)
- [Direct ECS state](#direct-ecs-state)
- [Input](#input)
- [Prediction and replay](#prediction-and-replay)
- [Remotes and RPCs](#remotes-and-rpcs)
- [Transforms](#transforms)
- [Physics and collision](#physics-and-collision)
- [Animation and presentation](#animation-and-presentation)
- [References and collections](#references-and-collections)
- [Time and smoothing](#time-and-smoothing)
- [Retrofitting a single-process game](#retrofitting-a-single-process-game)
- [Common object plan](#common-object-plan)

Use this reference to decide what each gameplay object sends and how it behaves
between authoritative updates. Verify the API names below against the installed
`com.unity.netcode` package before generating code. Where the
package's manual text and its code disagree on a name, trust `Runtime/` and the
compiled samples in `Tests/Editor/DocCodeSamples/`, because those are what compiles.

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

## Decide each value

Ask in order:

1. Can every peer derive it from existing state, an event, or a shared seed? Keep
   it local.
2. Who is the sole authority? Only that side writes the canonical value.
3. Does a late joiner need it? Use replicated state if yes, a remote/RPC if no.
4. What is the smallest useful representation? Prefer input, enum, ID, bounded
   collection, or quantized value over a full object or transform.
5. Who needs it, and how often? Apply relevance, importance, and send-rate limits.

## GhostObject state

Before writing a `GhostBehaviour`, read the Netcode `ghostobject/ghostobject-replicate`
page (`GhostField<T>`, accepted types, `FieldConfig`), the
`ghostobject/ghostobject-prediction` page (prediction methods, `CanWriteState`,
`TickedDeltaTime`) and the `ghostobject/ghostobject-hybrid` page (`Ghost.Entity`,
`GhostComponentRef<T>`, jobs). Read them again if generated code fails to compile.
Then apply these rules, which the pages don't state or that are easy to get wrong:

- Declare every `GhostBehaviour` in its own file named after the class, because
  Unity binds a MonoBehaviour to its script by file name. A namespace is
  recommended but not required. Never name a namespace segment `Netcode`, because
  `Netcode` then resolves to that namespace in it and in every namespace that
  contains it, so one such file breaks `Netcode.` calls in other files too.
- `CanWriteState` is true when the ghost entity has `PredictedGhost`, which reads
  as "false for a ghost that is never predicted" — it is not. For `GhostModeMask.Interpolated` the
  package strips `PredictedGhost` on clients and **retains it on the server**, so
  `CanWriteState` is true on the server and false on every client,
  `PredictionUpdate` is the correct server write path even for an
  interpolated-only ghost, and clients are structurally incapable of writing it.
  An all-interpolated design is implementable and gets the single-writer
  invariant enforced by the package rather than by discipline. On a single-world
  host `CanWriteState` is true as well, which is why mirror code needs separate
  gating — see *Single-world details* in [architecture.md](architecture.md).
- For a struct `GhostField<T>` or a `GhostComponentRef<T>`, copy the value, mutate
  the copy, and assign it back, because a change to the copy is never applied.
- Compile a minimal `GhostBehaviour` first. Before treating a source-generation
  failure as a package defect, bisect it to the smallest declaration that still
  fails; a failure that disappears is a project bug, not a reason to fall back.

## Direct ECS state

Read the Netcode `ghosts` and `ghost-types-templates` pages for `[GhostField]`,
`[GhostComponent]` and quantization before authoring direct ghost components.
Quantize every replicated value to gameplay precision, because unneeded bits cost
bandwidth on every snapshot.

## Input

Before modelling player input, read the Netcode `command-stream` page for direct
ghosts, or the `ghostobject/ghostobject-prediction` page (`GhostInput<T>`,
`PredictionGatherInput`, reading past input) for a `GhostBehaviour`. Capture
GhostObject input only in `PredictionGatherInput`, because Netcode overwrites the
input struct right before that method in full-ticks-only mode or with forced input
latency.

**`PredictionGatherInput` runs once per rendered frame, not once per simulation tick.**
`GhostInputSystemGroup` sits in `SimulationSystemGroup`, outside the prediction
loop, so at 60 fps with a 20 Hz tick it fires about three times per tick — and
for an ordinary field only the last write for a tick survives. Choose the
representation per field:

| Input shape | Representation | Why |
| --- | --- | --- |
| Continuous axis (stick, mouse look) | plain field | The last value *is* the current value; dropping older samples is correct |
| One-shot action (jump, fire, ability) | `InputEvent` | Netcode counts these rather than overwriting, so a press on any frame of the tick survives |
| Counted increments (discrete aim steps, scroll notches) | plain field, **coalesced per tick** | Neither last-write nor `InputEvent` preserves a sum: accumulate a running total keyed on `NetworkTime.InputTargetTick` and write the total each frame |

This table is for GhostObject `GhostInput`, whose `PrepareToCapture()` starts from
`default`. Direct ECS input components keep the previous frame's values, so
incremental values sum across frames there (Netcode `command-stream` page).

Writing each sample straight into a plain field silently discards most of a
discrete input stream — a responsiveness bug that no test flags and that players
feel without being able to describe. If the sampling function *consumes* state
(an auto-repeat accumulator, a "pressed since last poll" latch), a discarded
sample is gone permanently: coalesce, never overwrite.

**Make a ghost that gathers input through `PredictionGatherInput` `Predicted` or
`OwnerPredicted` when the owner must see the result without a round trip.** The input
callback runs for owned ghosts with `GhostOwner`, `GhostOwnerIsLocal` and
`AutoCommandTarget`, and the code sends that input for interpolated ghosts too, but
the owner then sees its own actions only after the server's snapshot returns. The
Netcode `command-stream` page describes command targets as predicted, so run any
interpolated-only input design in Play mode and confirm the server receives the input
before building on it. The
alternative is to send intent as a **rate-limited remote** and keep the ghost
interpolated; that is a real design, but per-tick remotes are an anti-pattern
where replicated state would do.

## Prediction and replay

The client re-runs the prediction loop for several ticks per rendered frame and
re-presents the same input on each replayed tick. Before each replay netcode
restores **only** `[GhostField]`-serialized data; plain MonoBehaviour fields,
statics, and any component without ghost fields are explicitly *preserved*, not
rewound.

So the contract governs *where you write from*, but replay safety is decided by
*which storage you mutate*. A predicted value must satisfy: the value after
replaying tick T any number of times equals the value after applying tick T once
to the restored value. The required property is **idempotence with respect to
restored state**, not determinism — a read-modify-write on unrestored storage is
perfectly deterministic and still compounds once per replay.

- **Accumulate into the `GhostField` itself.**
  `Field.Value = Clamp(Field.Value + input.delta)` is replay-safe;
  `local += input.delta; Field.Value = local;` drifts, because `local` survives
  the rollback that resets `Field`.
- **Do not gate accumulation with `IsFirstTimeFullyPredictingTick`.** That flag
  is for one-shot side effects — spawns, VFX, audio. Replayed ticks *must*
  re-apply their delta on top of the restored value; suppressing it throws the
  prediction away and trades drifting input for laggy input.
- If the authority and a predicted client genuinely need different accumulators —
  the server is never replayed, so it may safely accumulate on its own object —
  split the two paths explicitly and say why in a comment. One writer per role.

Symptoms: divergence that grows with time, an owner's rendered value running
ahead of the authoritative one, or a predicted value saturating asymmetrically at
a clamp limit. None of these are visible in a single process, because a host is
never replayed.

## Remotes and RPCs

Read the Netcode `ghostobject/ghostobject-rpcs` page (RPC methods, custom messages,
targeting, polling versus automatic handling) before declaring a GhostObject remote,
and the `rpcs` page for direct ECS `IRpcCommand` in systems and jobs or when you
need targeting the GhostObject API doesn't expose. Then:

- On a single-world host a `ServerToClient` remote also dispatches into the
  host's own handler. Gate any handler with a visible or stateful side effect on
  `IsServer`, never on `IsClient` (true on that host). Full rule: *Single-world
  details* in [architecture.md](architecture.md).
- Treat client-to-server calls as requests and validate sender, ownership, rate,
  range, and resources.
- Do not send remotes every tick or use them to initialize persistent state, because
  replicated state already covers both and a late joiner never receives a past remote.

## Transforms

Read the Netcode `ghostobject/ghostobject-prediction` page (*Read the tick-accurate
transform*, *Parent a ghost under another ghost*) and the
`ghostobject/ghostobject-hybrid` page (nested GhostObjects, scale) before writing
transform or attachment code. Then:

- Do not let a normal `Update` script and the prediction loop both write the same
  replicated value, because the two writers fight and the result corrects every
  tick. This applies to any ghost-backed state, not just the transform.
- Replicate relationships for attachments and derive child transforms locally. Make
  each attachment its own ghost with **Parent Replication** enabled on its prefab.
  Set that option in the Inspector only, because the property is read-only from
  code. Otherwise flatten network identity or use direct ECS.
- Enable **Uniform Scale** on a GhostObject that never scales per-axis, because
  GhostObjects replicate per-axis scale by default. Direct ECS ghost prefabs
  replicate non-uniform scale only if they opt into `PostTransformMatrix3DScaleVariant`.

## Physics and collision

- The authority decides collisions, hits, damage, and resulting state. A client
  can predict feedback but cannot report a trusted outcome.
- Collider geometry comes from matching content; replicate enabled/hitbox state,
  not collider shapes. Verify server builds preserve gameplay collision content.
- Predict only player-controlled or directly interacted bodies. Keep cosmetic
  debris, cloth, and ragdolls local; with GhostObjects, put those bodies in a
  separate `LocalPhysicsMode.Physics3D` scene, because Netcode re-simulates the
  whole default physics scene on every replayed tick.
- Stamp actions with network ticks. For lag compensation, retain bounded collision
  history and validate against the client's tick, never an unbounded client time.

For GameObject physics, read the Netcode `ghostobject/ghostobject-physics` page
(Rigidbody Replication, recommended physics settings, guarding physics messages
against replay). Disabling **Rigidbody Replication** doesn't take a dynamic body out
of replay: Netcode still resimulates it without rolling it back, so put bodies that
must not be predicted in the separate physics scene above. Use GameObject predicted
physics only after a focused prototype under latency. For
direct ECS predicted physics, the more mature advanced path, read the Netcode
`physics` page.

## Animation and presentation

Don't look for an animator replication component for GhostObjects:
`GhostAnimationController` serves entity-ghost presentation objects only.
Replicate animation causes:

- movement speed, grounded state, stance, aim angle, and gameplay phase as compact
  state;
- fire, hit reaction, and emote as one-shot remotes/events;
- animator graph, blending, IK, procedural motion, VFX, audio, camera, and screen
  effects locally.

Avoid root motion owning a predicted transform. Gameplay movement should drive
the canonical transform and animation should follow it.

Gate irreversible presentation triggered from prediction with
`IsFirstTimeFullyPredictingTick` (Netcode `ghostobject/ghostobject-prediction` page)
or emit it from a confirmed state transition. Never use that gate on accumulated or
continuous predicted state; see *Prediction and replay* above.

## References and collections

Do not replicate managed object references. Use stable ghost identity or an ECS
entity reference supported by the serializer and handle targets that have not
spawned yet or have despawned. Never cache a resolved instance across despawn and
respawn without revalidation.

Bound collections and prefer per-element deltas. IDs/enums beat strings. Version
custom payloads when client and server builds can differ. Large payloads fragment,
stall, or fail; split them or avoid replication.

"Bound collections" is not a license to reach for a container type: read the
Netcode `ghost-types-templates` page for the types and capacity limits the installed
generator accepts, then compile a minimal `GhostField<T>` declaration of that type on
a `GhostBehaviour` before designing around it, because the GhostObject generator can
reject a type the page lists.

**Entity-per-fact.** For a bounded, append-only collection — craters, placed
structures, scored hits — prefer one small ghost per element over a list on a
singleton ghost:

- it keeps the data as replicated *state*, so late join is correct without
  replaying events;
- each element is immutable after spawn, so static optimization stops resending
  it;
- it sidesteps container-type limits entirely; and
- ordering, where replay order matters, becomes an explicit `Sequence` field
  rather than an implicit list index — which also survives the fact that
  `GhostField` is eventually consistent and may skip intermediate values.

Bound it by policy and state the cap. A few dozen tiny static ghosts sit far
below the threshold where relevance filtering starts to matter.

## Time and smoothing

Drive simulation from network ticks and the ticked delta time (*Access time* on the
Netcode `ghostobject/ghostobject-prediction` page), never wall-clock time or frame
counts, because those differ per peer. Replicate an authoritative end tick, not a
countdown every frame.

Interpolate remote objects; predict owned or directly interactive objects. Smooth
visual corrections without feeding smoothed presentation back into simulation.
Constant correction indicates nondeterminism, mismatched content, or competing
writers, not a smoothing problem.

## Retrofitting a single-process game

A game that already runs locally has a pre-existing input path and pre-existing
writers for every value that is about to become replicated. That is where retrofit
defects live, and the framing in the rest of this file — `IsServer`,
`IsClient`, `CanWriteState` — pushes toward server-versus-client thinking when
the question that matters is *local play versus game session active*.

- **Audit every writer.** For each value that becomes replicated, list every
  existing site that writes it. Legacy sites must be disabled when a game session is
  active, not merely when the local peer is a client.
- **Use the right predicate.** "Am I the authority?" is true for local play *and*
  for the host, so it is exactly the wrong distinction. The predicate that gates
  legacy input is "is there **no** game session". Getting this wrong lets the host's
  keyboard drive another player's avatar — and because the host is the authority,
  that replicates.
- **Keep the local mode on its original path.** A local/hotseat mode that creates
  no worlds is the cheapest regression guard available: the pre-existing test suite
  keeps passing through the whole migration.
- **Watch for double sampling.** If a legacy poll and `PredictionGatherInput` both run, a
  consuming input function is called twice per frame and every hold/repeat timing
  is wrong — see *Input* above.

## Common object plan

| Object | Replicate | Keep local |
| --- | --- | --- |
| Server-authoritative player | Input; authoritative transform/state; compact animation drivers | Animator, IK, VFX/SFX |
| Trusted owner-driven player | Input or owner-predicted state, validated where needed | Presentation |
| Deterministic projectile | Spawn parameters/seed and authoritative hit result | Flight trail and cosmetic flight simulation |
| Gameplay physics prop | Authoritative thresholded/quantized state | Impact presentation |
| Cosmetic debris/ragdoll | Trigger only | Entire simulation |
| Door/container | Open/content state | Motion and audio |
| Static/procedural world | Nothing, or seed/parameters | Geometry/result |
| Score/phase/timer | Authoritative state and end tick | UI |

On a single-world host, "replicate the cause, derive the visual locally" — the
deterministic-projectile row above, and the same advice in
the lifecycle reference — needs one extra guard.
The host receives its own server-to-client spawn event and it already owns the
authoritative object, so gate the local derivation on `IsServer` and let the
mirror run only on peers that lack the authoritative instance. The same applies to
the authoritative hit result: if applying it mutates shared simulation state such
as terrain, applying it twice on the host **desynchronizes the peers** rather than
merely drawing twice.
