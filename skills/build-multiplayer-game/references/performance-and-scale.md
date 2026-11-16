# Performance and scale

## Contents

- [Set budgets before writing netcode](#set-budgets-before-writing-netcode)
- [Starting bands to reason against](#starting-bands-to-reason-against)
- [The levers, in order of impact](#the-levers-in-order-of-impact)
- [Prediction cost](#prediction-cost)
- [How to test — the measurement loop](#how-to-test--the-measurement-loop)
- [Verifying prediction](#verifying-prediction)
- [Common causes of slow multiplayer](#common-causes-of-slow-multiplayer)

Choosing the right authoring path buys **headroom**; replication design decides whether
the game actually spends it well. A project on the fastest path still ships a laggy
game if it replicates everything, every tick, to everyone.

Use this file when the game has a scale/performance requirement, when the developer
reports lag, jitter, rubber-banding, or bandwidth problems, or before signing off on an
architecture. **This file also owns how to test** — every other file defers here for
that.

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

## Set budgets before writing netcode

Derive these four numbers (ask only for what the design implies but doesn't state) and
write them down — they turn "make it fast" into a checkable target:

1. **Max players per game session.** Drives per-client bandwidth and authority CPU.
2. **Max networked objects in scope at once** — not total objects in the world, but how
   many one client must know about. This, more than player count, is what saturates
   bandwidth.
3. **Tick / send rate.** The authority's fixed simulation tick, and how often state goes
   out. These can differ: simulate at a high tick and send less often. For Netcode for
   Entities, read the Netcode `client-server-worlds` page (`ClientServerTickRate`)
   before setting them. Netcode creates that singleton with a server or host world, so
   code that installs one only "if absent" silently does nothing and leaves the package
   default while reading as though the budget applied — overwrite the existing
   singleton on the server (or set the `NetcodeConfig` asset), and assert the value in
   a test. An unasserted budget is a comment.
4. **Bandwidth ceiling per client.** Set by the weakest supported target/network.

Don't ask the developer to supply the tick rate or bandwidth ceiling — those are what
they're asking *you* for. Propose values from the bands below, tie each to the game's
requirements, and let the developer correct them.

State the budgets back with the recommendation, so the architecture is justified by
numbers rather than genre. If a requirement and a budget are incompatible (e.g.
hundreds of predicted physics bodies at a high tick on mobile), say so plainly and
propose which requirement to relax — see *Requirement conflicts* in
[architecture.md](architecture.md).

## Starting bands to reason against

**Order-of-magnitude starting points for a first proposal — not guarantees, limits, or
Unity-published figures.** Their job is to make the budget comparison executable and to
flag "this design is off by an order of magnitude" early. Every one must be replaced by
measurements from the actual game.

| Dimension | Starting proposal | Notes |
| --- | --- | --- |
| **Tick rate — competitive action** (FPS, fighting, twitch) | **30–60 Hz** | Responsiveness and fair hit registration dominate. Above 60 is specialist territory and multiplies prediction and bandwidth cost |
| **Tick rate — co-op / casual / sandbox** | **15–30 Hz** | Usually indistinguishable to players; large CPU and bandwidth savings |
| **Tick rate — turn-based / async** | n/a | Use request/response and a backend, not a fixed netcode tick |
| **Send rate** | **at or below the tick, often 20–30 Hz** | Sending less often than you simulate is the cheapest early win; not every object needs per-tick updates |
| **Per-client bandwidth - desktop/console broadband** | **~50-150 KB/s** (about 0.4-1.2 Mbps) | Still finite: the ceiling is the worst-connected player you intend to support |
| **Per-client bandwidth - mobile cellular** | **~10-30 KB/s** (about 80-240 kbps) | Markedly tighter and metered. Also watch battery/radio wake-ups from high send rates |
| **Objects in scope where relevance filtering stops being optional** | **~100–300** | Below that, naive replication often survives; above it, interest management is the difference between shipping and not |
| **Predicted objects per client** | **~1–10** — what the player controls and directly touches | Cost is per predicted object *and* multiplied by re-simulation (below). The floor is not zero — see below |

**Predict what the player controls when responsiveness matters.** Budget at least one
predicted ghost per player when the owner must see its own input take effect without
a round trip (why, and the remote-based alternative: *Input* in
[syncing-gameplay.md](syncing-gameplay.md)).

How to use them: pick a band from the game's requirements, state the number you're
proposing and why, then treat the first profiling run as the thing that decides. If the
developer's target sits outside a band, that's a signal to check the design or the
requirement — not automatically a blocker.

Verify platform-specific ceilings against current platform/certification documentation
rather than these bands, especially for console submission.

## The levers, in order of impact

Apply these in order, because the early ones win far more than the late ones and
micro-optimizing before relevance filtering is wasted effort. For Unity Netcode,
read each lever's page before applying it, and again if the measurement doesn't move.

1. **Don't replicate it at all.** The cheapest object is never networked: derive
   deterministic content from a shared seed, keep cosmetic objects local, and replicate
   outcomes rather than intermediate steps.
2. **Relevance.** Usually the biggest saving, and an information boundary, because a
   client cannot inspect state it never receives. Netcode `optimization/optimize-ghosts`
   page (*Ghost relevancy*). The GhostObject bridge has no NGO-style `NetworkShow`
   facade; reach relevance through the linked entity/world with an ECS relevance system.
3. **Importance.** Decides what degrades first when bandwidth saturates. Netcode
   `optimization/optimize-ghosts` page (*Importance scaling*).
4. **Static optimization and send rate.** Stop resending what doesn't change and send
   low-fidelity objects less often. Netcode `optimization/optimize-ghosts` page
   (*Optimization Mode*) and `optimization/limit-snapshot-size` page. Lower
   predicted-ghost send rates cautiously, because they trade client CPU for
   misprediction (Netcode `optimization/reduce-prediction-overhead` page).
5. **Shrink the payload.** Quantize to the precision the game needs and don't replicate
   values a client can derive. Netcode `optimization/compression` page.
6. **Narrow prediction scope.** Prediction is the most expensive per-object feature, so
   predict what the player controls and touches and interpolate the rest. Netcode
   `optimization/reduce-prediction-overhead` and `prediction-switching` pages.
   Prediction switching is not available on a single-world host.
7. **Split by audience.** Send data only to the clients that use it (physics velocity
   only to clients predicting that object, owner-only state only to its owner).

A GhostObject sets some of these levers on its GhostObject component (Netcode
`ghostobject/ghostobject-replicate` page, *GhostObject settings*); for the rest, use
`Ghost.Entity`/`Ghost.World` or `GhostComponentRef<T>`. See
[netcode-for-entities.md](netcode-for-entities.md).

## Prediction cost

Read the Netcode `prediction-n4e` page for rollback and re-simulation before budgeting
predicted logic. Budget **re-simulation as RTT x tick rate**, and scale the page's
example figure to the game's own tick rate rather than reusing it.

Consequences to design around:

- Keep predicted logic lean; heavy work does not belong in the prediction loop.
- Predicted physics multiplies this cost — budget it explicitly for physics-heavy
  competitive games (Netcode `optimization/reduce-prediction-overhead` page).
- Predicted logic must be **idempotent with respect to restored state** across repeated
  ticks, or it produces jitter and mispredictions on its own. Determinism is not
  sufficient: rollback restores only `[GhostField]`-serialized data, so a
  read-modify-write on storage it does not restore is perfectly deterministic and still
  compounds once per replayed tick. See *Prediction and replay* in
  [syncing-gameplay.md](syncing-gameplay.md).
- Cost scales with RTT, so a build that feels fine on LAN can fall apart at real latency.
- A single-world host is never replayed, so host-side profiling understates prediction
  cost — and host-side testing cannot see prediction defects at all. Measure on a real
  client process.
- Partial snapshots can roll back interacting predicted ghosts to different ticks.
  Read the Netcode `prediction-details` page for the options before choosing one, and
  measure the added CPU and prediction-history memory cost of the fix.

## How to test — the measurement loop

**This is the canonical testing guidance for the whole skill.** Recommend this loop
rather than asserting a design is fast:

1. **Profile the network**, not just the frame time. Use Unity's multiplayer profiling
   tools to see per-object bandwidth and message volume, and identify which objects
   dominate the budget.
2. **Simulate real conditions, from the start.** Add latency, jitter, and packet loss
   with the in-Editor emulator in the Multiplayer Play Mode clones, never on a clean
   LAN alone, which hides the problems prediction exists to solve; a buffer or timeout
   tuned on LAN will be wrong in the field. Set them on each clone with
   `netcode.simulator set` (Unity Netcode) or `netcode.gameobjects.simulator set`
   (NGO); see *Network conditions in clones* in
   [multiplayer-play-mode.md](multiplayer-play-mode.md), and read the emulator page
   it names before configuring it. Confirm the measured RTT rose before trusting the run.
   In final testing, also run over the actual transport/Relay path and on target
   devices. The Netcode emulator works only in development builds; a release build
   silently ignores its settings and `--loadNetworkSimulatorJsonFile`, so use OS-level
   tools there as the NGO page describes.
3. **Compare against the budgets** above. Name the top consumer.
4. **Apply the highest lever that addresses it** and re-measure.
5. **Stop after about three iterations** without meaningful improvement and tell the developer
   what's structurally limiting - an architecture problem (wrong topology, too many
   predicted bodies, no relevance filtering) will not yield to further tuning.

Use Multiplayer Play Mode, with the emulator on, as the working loop for function and
for behaviour under latency. Editor profiling numbers are indicative; take the final
performance numbers from development builds on target hardware in final testing (see the
verification gate's *Final testing* section). Connecting
successfully is not "done" — also run the unhappy-path checklist in
[netcode-lifecycle.md](netcode-lifecycle.md) under these same adverse conditions.

## Verifying prediction

If the project predicts anything, verify prediction **in two processes**:

- a second process on a non-loopback address — a Multiplayer Play Mode clone joining
  the main Editor by default, with the emulator adding latency;
- input driven on the clone by `script eval` calls to the game's own gameplay
  methods (no command injects input into a clone), so no input-injection code goes
  into the game;
- each peer's **rendered** value compared against its **replicated** value, read
  with `netcode.ghost get` on each instance several times over enough ticks for drift
  to show, not through added logging.

When the comparison diverges, find the first system that introduced it with the tool
on the Netcode `testing/prediction-tracing-tool` page. Read its requirements first:
it needs separate client and server worlds in one process, so it cannot trace a
single-world host.

A single-world host (the default) is never replayed, so single-process tests only
ever take the authority path — see *Single-world details* in [architecture.md](architecture.md).
Divergence that grows with time, or a predicted value saturating asymmetrically
at a clamp limit, means accumulation into unrestored storage — see *Prediction
and replay* in [syncing-gameplay.md](syncing-gameplay.md).

## Common causes of slow multiplayer

Check these before anything exotic:

- **No relevance filtering** - every client receives the whole world.
- **Everything predicted** - prediction applied to objects the player never touches.
- **Replicating full transforms every tick** for objects that barely move, or that could
  be driven locally.
- **Replicating derived or cosmetic state** (animation/VFX detail) instead of the event
  that caused it.
- **Per-tick RPC/event spam** where replicated state (or a batched event) would do.
- **Unquantized, unbounded payloads** - large structs, strings, or collections
  replicated wholesale.
- **Tick/send rate set higher than the game needs**, applied globally.
- **Only tested on LAN**, so latency-dependent cost was never observed.
- **Networking objects that could be deterministic** from a seed or shared config.
