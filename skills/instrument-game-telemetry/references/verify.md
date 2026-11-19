# Verify: the full rungs

The skill's Verify section lists the rungs in order. This file holds each rung's full bar: what to
assert, what counts as proof, and what is red. Read it before running Verify.

## Contents

- Rungs 0 to 7

## Rungs 0 to 7

0. Read the pitfalls reference and check your wiring against it before showing anything.
1. **Compile gate (always).** `unity command recompile`, then poll
   `unity command recompile_status` until `completed`: zero errors, no new warnings. Every
   added `.cs` has a `.meta`; `Packages/packages-lock.json` lists the package. If no Editor is
   reachable, run `unity pipeline list` to distinguish Safe Mode from absent; if absent, stop
   here and say so in the report. Manual code review is not a substitute for compilation.
2. **Stream assertion (the exit bar).** With `Collectors.Log()` in the configure chain, enter
   play mode via the pipeline and run the scripted traversal — **built from the surfaces and
   outcomes the game profile actually declares, never from a fixed template**. The full menu
   is: boot → title → start attempt → one of each declared attempt outcome (a run-based
   game has no win/fail — its natural terminal is the completion; traverse what the
   topology defines) → the main monetization surface **if the profile declares one** → a
   test ad **if wired**. A surface or outcome the profile does not declare is recorded in
   the report as *skipped — not in profile*, explicitly, and the bar computes over declared
   items only: a game with no shop and no fail path verifies as done when its declared
   traversal passes, and silently narrowing the traversal without the skipped-list is a
   verification failure. If the project exposes
   declared test intents (public methods or commands built for driving it), use them;
   otherwise drive it by `unity command script eval` on the game's run-state owner (start a
   run, force win and lose, restart): public members only, one statement per call, reading
   the stream between calls. A surface with nothing to drive, or reachable only through
   reflection or private state, is *skipped: not driven*; a run that could drive nothing ends *not verified*. The assertions are yours
   either way, and reading the stream is never "checking manually". From the
   emitted stream assert: every Enter has its matching Exit; exits are innermost-first;
   every record carries a `release_id` only where a Live Release is active (without
   one, an absent `release_id` is expected and reported, not a finding); every id in the stream
   (screens, nodes, phases, resources, products, placements, flow steps) resolves to a manifest
   entry; every custom name is `x_`-prefixed and declared in `custom_events`; no PII-shaped
   params; and the Editor traffic carries `sandbox = true` (the runtime's `Debug.isDebugBuild`
   default, or `WithSandbox` where that builder exists and you wired it per Constraint 4).
   The Log collector's console lines are the human-readable stream; the sealed sandbox
   capture file (the api-surface reference, Data rules) is its
   durable copy, written only in the Editor and development builds, and the conformance tool
   in rung 4 is the only reader.
3. **Schema publication (the ingest bar).** For every custom event the manifest pins that is
   not `retired` (the stream's are among them; an event the game still sends is not retired),
   the Schema Registry holds a PUBLISHED version equal to the
   version the manifest pins. The proof is the event's `.ugtsevent` deploy item in *Deployed*
   or *Up to date* at that version (the registry reference, deploy
   step 3, re-run in this session if no status is on hand: it is idempotent); the stream
   cannot show it. A custom event without that proof is a red verify, stated plainly: production ingest
   drops every custom event whose name and pinned version are not PUBLISHED, while typed and
   context events land, so a green stream over unpublished schemas ships a build whose custom
   events never arrive.
4. **Conformance tool (when present).** If `unity command` lists a telemetry verification
   command (`insights.events telemetry_verify`, which checks the newest sandbox capture
   against the manifest), run it and meet its completeness bar; it supersedes nothing in
   rung 2, it adds registry resolution and type conformance.
5. **Vendor projection (only if an adapter was requested).** One record per moment in the
   vendor's debug view, and no doubles from a surviving legacy writer.
6. **Rulings check.** Read the rulings reference and walk the register
   against the wired game. Rulings 1, 2, 4 and 5 show up in the verify stream; 3 shows up
   in the placement review; 6 and 7 are checked in the configure chain and on the device; 8
   is a property of the runtime, confirmed by reading the package version's gate; 9 is checked
   in the configure chain and `Packages/packages-lock.json`, and when unlocked in the placement
   review and on the device. The final
   report names each ruling and says how it was checked, or states it could not be checked
   and why.
7. On a failed step, re-read the reference and the pitfalls row for that step before retrying.
   Never report done on a red verify; never ask the developer to check manually.
