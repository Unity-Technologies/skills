# The registry manifest — declare what exists, before wiring anything

## Contents

- What it is
- When it is written
- The format
- Filling it from the survey
- The manifest gate
- Uploading to Live Content and the Schema Registry
- Dynamic registration
- Updating

## What it is

The registry manifest is the game's declared inventory: its profile, and every screen, mode,
progression node, resource, product, ad placement, flow, entitlement, and custom event, each
with a stable typed id. It serves three purposes at once:

- the **denominator** — "% of content seen", "% of nodes beaten", and "time-to-exhaustion"
  are computable only because the manifest says what exists;
- the **disclosure** — the list of what is collected, authored from the survey, is disclosed to the developer;
- the **record of intent** — it is written from the survey before any event is wired, so it
  states what the instrumentation is supposed to observe independently of the code that
  ended up observing it. Reviewers, later sessions, and ingest read it to tell a wiring bug
  from a design choice.

The spec fixes the verbs; the manifest supplies this game's nouns. How `screen_view` or
`purchase` is measured is identical across games; which screens, products, and nodes exist
is what this file declares. It holds **nouns only** — the verb placements (which call fires
where) live in the wiring plan, and every plan row cites manifest ids.

## When it is written

After the survey and profile, **before any wiring code exists** — Workflow A step 3,
Workflow B step 3. No `Enter*`/`Send*` call, seam, or subscriber is written until the
manifest gate below passes. When the game already calls `Events.*` without a manifest,
"before any wiring" means before you edit or extend those calls. The manifest declares the
ids they already send, then the new work is added on top. On a finished retrofit the
manifest is the file the reconciled plan cites, in whatever format, and the gate applies to
the entries you add to it (Step 1, the maintenance row).

**Migrating an earlier manifest.** This applies to a first pass only: greenfield, a resumed
run, or an unfinished retrofit. There, a project whose registry is in an earlier shape (a
`telemetry_registry.json` under `Assets/Resources/`, a `registry_hash` field, ids without
typed prefixes) counts as having no manifest: write
`Assets/Resources/Telemetry/telemetry_registry.ugtr` first, from the survey plus that
file's content, at the packed path (The format, below), with `$schema` on the first line,
and pass the gate below. Ids this skill produced without their prefix are renamed with
their call sites in one change (Step 1, the resumed-run row; the same rename rule
holds on Workflow B step 3). Leave the old file in place when the game's own legacy
analytics code still reads it (removing it is a gated retrofit item); it keeps its old ids,
so say in the report that two files exist, that only the `.ugtr` carries the prefixed ids,
and which one the server holds. On a finished
retrofit (a reconciled `TELEMETRY-RETROFIT-PLAN.md` exists) none of this runs: the manifest
the plan cites and the plan are the baseline, you add new entries to that file in place
and leave its settled ids alone, and migrating it to `.ugtr` is a gated plan item you raise
for the developer (Step 1, the maintenance row). A game whose manifest is not the
packed `.ugtr` at that path sends nothing, and an id the packed manifest does not declare
goes out flagged as unregistered and fails `telemetry_verify`, an undeclared `x_` name is
dropped (The runtime gate, below): a finished retrofit on an earlier-format file is in that
state, so say so in the report and in the migration item, which waits as gated work. Never
leave an earlier-format file at `Resources/Telemetry/telemetry_registry.<ext>` beside the
packed `.ugtr`: the build warns, and the player may load it instead. Entries added to an
earlier-format file may be read by the game's own legacy analytics code; when it validates
against that file, the additions themselves are gated.

## The format

JSON in a `.ugtr` file at `Assets/Resources/Telemetry/telemetry_registry.ugtr`, in version control, reviewed
like code. It sits under `Resources/` on purpose: the player build carries it, and the runtime
flags anything the manifest does not declare (see *The runtime gate* below). Anyone unpacking the build can read
the inventory, so it holds nouns and notes, never secrets. No other path works: a manifest
anywhere else is not packed, and the build warns about it.
It follows the registry manifest schema `1.0-draft`. Required top-level
fields: `$schema`, `schema_version`, `project`, `profile`. The version of the registry is the
version segment of `$schema` — e.g. `v1` from
`https://ugs-config-schemas.unity3d.com/v1/insights/telemetry-registry.schema.json`.
Every other section is present when the game has the concept and may be omitted when it does not.
The first line is always the `$schema` reference, so editors check the file against the format
as it is written. The JSON Schema itself does not require `$schema` (backend services strip it
before storing a manifest), so `telemetry_validate_registry` will not report it missing: always
write it anyway, and add it to any manifest that lacks it.

```json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/insights/telemetry-registry.schema.json",
  "schema_version": "1.0-draft",
  "project": {
    "unity_project_id": "<cloud project id, or the product name when not cloud-linked>",
    "app_version": "<PlayerSettings.bundleVersion>",
    "audience": "general | children | mixed",
    "identity": { "source": "ugs_authentication | game_center | google_play_games | developer", "note": "" }
  },
  "profile": {
    "progression_topology": "linear_levels | saga_map | chapter_episodic | run_based | prestige_loop | seasonal_ladder | rank_ladder | collection_catalog | open_sandbox | flat_none",
    "session_cadence": "bite_size | match_rounds | long_form",
    "appointment_pressure": false,
    "economy_structure": ["none | entitlement_only | single_soft | dual_hard_soft | energy_gated | gacha_pity | marketplace"],
    "monetization": ["none | ad_supported | premium_unlock | consumable_iap | gacha | battle_pass | subscription"],
    "social_fabric": "solo | async_competitive | sync_pvp | cooperative",
    "content_model": { "authorship": "authored | procedural | ugc", "delivery": "static | ota | live_ops" },
    "skill_expression": "puzzle | execution_twitch | strategic | collection_patience | knowledge_recall",
    "player_role": "symmetric | creator_consumer",
    "consent_surfaces": ["cmp | att | coppa_gate | in_app | none"],
    "declared_by": "agent",
    "confidence_notes": "which axes were ambiguous and what evidence decided them"
  },
  "screens": [ { "id": "scr_title", "name": "Title", "kind": "menu | hud | overlay | gameplay | system",
                 "unity_ref": { "scene": "", "class": "", "object_path": "" }, "nav_edges": ["scr_shop"] } ],
  "modes": [ { "id": "mode_endless", "name": "Endless" } ],
  "progression": { "structure": "linear | graph | endless | seasonal | catalog",
                   "nodes": [ { "id": "node_w1_l1", "name": "1-1", "ordinal": 1, "difficulty": "", "objectives_total": 3 } ] },
  "phases": [ { "id": "phase_boss_enraged", "name": "Boss: Enraged", "kind": "boss | round | checkpoint | tier | state" } ],
  "resources": [ { "id": "res_coins", "name": "Coins", "kind": "soft_currency | hard_currency | energy | material | ticket | xp | other",
                   "sources": ["run_pickup"], "sinks": ["shop_upgrade"] } ],
  "products": [ { "id": "prod_coins_small", "store_product_id": "com.game.coins.small", "kind": "consumable | non_consumable | subscription",
                  "grants": [ { "resource": "res_coins", "amount": 500 } ] } ],
  "ad_placements": [ { "id": "adp_revive", "format": "rewarded | interstitial | banner | app_open | audio", "moment": "revive_offer" } ],
  "flows": [ { "id": "flow_ftue", "kind": "ftue | tutorial | checkout | consent | custom",
               "steps": [ { "id": "fstep_move", "name": "Move" } ] } ],
  "enums": { "outcome_reasons": [], "difficulty_labels": [], "earn_spend_reasons": [] },
  "entitlements": [ { "id": "ent_no_ads", "unlock_rule": "bundled | free | iap | rewarded_ad | easter_egg | subscription" } ],
  "quests": [ { "id": "qst_daily_runs", "name": "Daily Runs", "quest_line": "", "objective": {}, "cycle": {} } ],
  "custom_events": [ { "name": "x_near_miss",
                       "version": "0.1.0",
                       "trigger_note": "player clears a hazard within the near-miss threshold, mid-attempt",
                       "owner_note": "none — no standard event covers near misses" } ],
  "dynamic": { "enabled": false, "entity_types": ["progression.nodes | screens | liveops_events | ugc_content"] }
}
```

`phases` is an Insights addition, not yet in the spec format: `EnterPhase` takes a registry
reference, and the spec defines no phase entity to resolve it against. Declare one entry per
distinct phase the game enters — boss phases, rounds, checkpoint segments, difficulty tiers,
or a bounded standing state whose duration is the signal — with a `kind` naming which.

Native-resident screens additionally carry `residence: native_shell` (see
the native-shell reference); a progression node, resource, or product may carry declared attribute
keys the dynamic channel will fill.

Normative rules:

- **Typed id prefixes**, lowercase snake_case after the prefix: `scr_ / mode_ / node_ /
  phase_ / res_ / prod_ / adp_ / flow_ / fstep_ / ent_ / qst_`. Custom events are `x_`-prefixed. The
  same id is what the code passes — `EnterScreen("scr_title")`, never `"Title"` or a scene
  name.
- **Ids are immutable.** A rename changes `name`, never `id`. Evolution is additive; a
  removal is a tombstone, `"retired": true`, never a deletion.
- **One id per concept.** A surface reached from two places is one screen with two nav
  edges, not two screens.
- **The profile is authored, never inferred at runtime.** `audience` and `identity` are
  declarations the runtime stamps, not facts the client discovers.

## Filling it from the survey

Every entry derives from `telemetry_game_model.json`; nothing is invented and nothing the
game has is silently left out. Nothing is omitted by choice; an entry the survey no longer
finds is `retired`. It is deleted outright only in the session that first writes the manifest,
before that session's first deploy; whether a server holds it is never guessed.

- **Carry the evidence forward.** `unity_ref` on screens records the class (and scene or
  object path where meaningful) the survey cited; it is how a reader finds the code the
  entry describes. Survey `unknowns` that affect an entry go in `profile.confidence_notes`.
- **Profile from structure.** One value per axis from the survey; set-valued axes
  (`economy_structure`, `monetization`) list every axis the game has. The profile decides
  which event families are wired — a family the profile does not declare is not wired.
- **Custom events carry the intent.** Every `x_` event the plan will need is declared here
  first. Its params live in the event's `.ugtsevent` schema (Create > Services > Insights >
  Custom Event Schema), so author that file first; the manifest entry references it by `name`
  and the schema's `version` (strict `MAJOR.MINOR.PATCH`, bumped whenever the schema
  changes), adds a `trigger_note` saying when it fires relative to the context stack
  (mid-attempt, on screen exit, ...), and an `owner_note` saying why no standard event fits.
  A custom twin of a standard event is a mapping bug; check the generated source first.
- **Targeted follow-up reads** are allowed for what the survey doesn't carry — nav edges,
  non-FTUE flows, outcome and difficulty enums, product grants — each recorded with its
  evidence in the survey file too, so the model and the manifest stay consistent.

## The manifest gate

Before writing the wiring plan, all of these hold (on a finished retrofit, for the entries
you add to the plan's manifest, in whatever format it is; check 1 itself, when the plan's
manifest is not the packed `.ugtr`, is the migration item; problems confined to settled
entries are the standing migration item):

1. `Assets/Resources/Telemetry/telemetry_registry.ugtr` exists, parses, and carries the four
   required top-level fields (`$schema`, `schema_version`, `project`, `profile`) with every
   profile axis set.
2. Every id matches its typed prefix; no duplicate ids; every `scr_` nav edge and every
   `grants.resource` resolves to an entry in the file (nav edges may also name non-screen
   destinations such as `gameplay`).
3. Every survey inventory item (screens, modes, progression, resources, products, ad
   placements, flows) maps to an entry, or is listed in the summary as excluded with a reason.
4. Every planned custom event is declared by `name` and its `.ugtsevent` `version`, with
   `trigger_note` and `owner_note`; its params live in the schema file.

`telemetry_validate_registry` checks 1, 2, 4 (declaration shape including enum values,
required fields and non-blank notes) mechanically by validating against the JSON Schema: the
gate passes when it reports no `Problems`. Check 3 and the substance of 4 are yours.

Run it:

```
unity command insights.events telemetry_validate_registry
```

With `--path` it validates a manifest outside the default location. Fix every reported
problem before going on. The command reports `Version` (e.g. `v1`) derived from the
manifest's `$schema` URL; include this in the summary. A passing run with no `Version` means
`$schema` is missing: add it and run the command again.

Then show the developer a summary: the profile, counts per entity type, every custom event
with its trigger note, and any exclusions. Greenfield proceeds after showing; on a retrofit
the manifest is part of the plan file's additive tier.

From here on, the manifest is the wiring's contract: every plan row cites the manifest ids
it uses, and a moment that needs an entity the manifest lacks goes into the manifest first
(new entry), then into the code — never the other way round.

## Uploading to Live Content and the Schema Registry

Once the gate passes, two uploads: the manifest to Live Content, so the server holds the
inventory, and every `.ugtsevent` schema the manifest pins to the project's Schema Registry,
so ingest accepts the custom events. Upload the manifest again after every edit to it;
publish a schema again whenever its file changes (with its `version` raised). On a retrofit
both are part of the additive tier.

**Prerequisites:** the project is cloud-linked, the developer is signed in to the Editor, and
`com.unity.pipeline` is installed.
`com.unity.insights.events` depends on `com.unity.services.deployment`, which brings the
Deployment API 1.1 the provider needs; a project that pins `com.unity.services.deployment.api`
to 1.3 or later compiles the provider out. `com.unity.insights.events` talks to the Live
Content service itself, so the `com.unity.services.live-content` package is not needed.
`com.unity.insights.events` deploys the manifest with its own command,
`insights.events telemetry_deploy_registry`, which needs no Deployment CLI. The schemas are
published through the Deployment CLI, `services.deployment deploy_items`. The environment
check uses `services.environment` from `com.unity.services.core` 1.19.0 or later (published
Core up to 1.19.0-exp.2 strips it); check `unity command` lists it.

**The Telemetry Registry provider.** `com.unity.insights.events`
lists every `.ugtr` file in the Deployment window under *Telemetry Registry* and deploys it
itself. Create no `.lcf` or `.lcc` for the manifest. The provider uploads the exact bytes as a
Live Content config at the remote path `telemetry_registry.ugtr` (the file name), with metadata
`kind: telemetry_registry`, `deployedBy`, `schema_version`, and the active Live Release's
`release_name` and `release_id`, in the active environment and Live Release. It refuses to
deploy a manifest that has validation problems, and lists the reasons as the item's states.
A `.lcf` left over from an earlier setup whose root holds the manifest would upload it a
second time: delete that `.lcf` after reading its root shows nothing else in it.

**The Custom Event Schema items.** The same package lists every `.ugtsevent` file in the
project under *Insights* in the Deployment window, one item per custom event, and deploys
them itself. Deploy publishes each to the project's Schema Registry: the subject is the event
name, tagged `insights-custom-event`; the version is the file's `version`; the state is
PUBLISHED. The Schema Registry is project-scoped, so one publish covers every environment,
while the manifest upload stays per environment. A published version's content is fixed, and a fix is the next version: the same
version with other content fails and asks for the version to be raised; the same version with
the same content reports *Up to date*; a deprecated version is refused outright, raise it; a
subject of that name the package did not create is left alone and the deploy fails. Production ingest accepts a custom event only when its name
at the version the manifest pins (stamped on the payload as `schema_version`) is PUBLISHED in
the Schema Registry; a custom event without one is dropped at ingest, while typed and context
events land normally. Uploading the manifest publishes no schema.

**Deploy:**

1. `unity command services.environment get_active_name` — the upload goes to the active
   environment, whatever its name; it changes no game behavior and is stored as a Live Content
   config in the developer's own project. Never switch environments to make a deploy work. Without
   the `services.environment` group (Services Core before 1.19.0) the deploy result's
   `Environment` names it; report an empty one as unknown.
2. `unity command insights.events telemetry_deploy_registry` — deploys
   `Assets/Resources/Telemetry/telemetry_registry.ugtr` (`--path` for another manifest) through
   the Telemetry Registry provider, then reads the deployed copy back. In the result:
   - `Deployed: true` means Live Content accepted it; `Detail` says whether it was uploaded or
     was already there. `Deployed: false` is a failed upload: report `Detail` and `States` word
     for word (validation problems are listed as `Error:` states; fix them and deploy again), and
     don't claim the server holds the inventory.
   - `UpToDate: true` confirms the active environment and release hold exactly this manifest;
     put `RemoteCheck` in the report as the proof of upload. `UpToDate: false` after a
     successful deploy means the read-back failed or found another copy: report `RemoteCheck`.
   - `Environment` and `Release` name where it went. A `Warning: No active Live Release`
     state means the deploy overwrote the environment's unreleased copy, so an older release
     keeps no registry of its own: tell the developer.

When `telemetry_deploy_registry` is missing, find out why before going on:

- **`com.unity.pipeline` is not installed**, but the Deployment window lists the manifest under
  *Telemetry Registry*: the agent cannot deploy. Report the upload as not done (the
  no-server-copy statement below) and name the **Deploy** button in the `.ugtr` inspector as
  where it happens later.
- **The provider is compiled out** (the project pins `com.unity.services.deployment.api` 1.3 or
  later): there is no command, no *Telemetry Registry* entry and no **Deploy** button, so nobody
  can upload the manifest from this Editor. Report the upload as blocked, name the pinned
  version, and say the provider needs `com.unity.services.deployment.api` from 1.1 up to, not
  including, 1.3. The schema items under *Insights* are compiled out by the same pin, so step 3
  is blocked too: report the publish as blocked and end the run *not verified: schemas
  unpublished*.

Never write a script or call a REST endpoint to upload the manifest instead.

3. `unity command services.deployment deploy_items --paths <every .ugtsevent the manifest pins>`
   — the paths comma-separated, each one listed explicitly, one per
   `custom_events` entry that is not `retired`, no directory, no glob, no file the manifest does not pin (a
   file's event name is its schema's `title`, or else its file name, in snake_case). Before
   deploying: read each file's `version` (`0.1.0` when the key is absent, which is what the
   Create menu writes) and make it equal the manifest's pin yourself, the manifest copying the
   file, never the reverse; the build check that warns on a mismatch runs only in a player
   build, and a schema published at the wrong version stays listed as noise while the pinned one
   stays unpublished. Then publish; publishing is project-wide and versioned, so the report
   lists every subject and version published. Confirm each item ends in *Deployed* (`Published
   <name> <version> to the Schema Registry.`) or *Up to date*, at the version the manifest
   pins; the count of those items equals the count of files passed. *Failed to deploy* on any item is a failed publish: report the item's message,
   and don't claim ingest accepts that event. A pinned event with no `.ugtsevent` on disk is
   fixed before deploying. A `retired` entry is never passed, whether or not its file still
   exists (a version it already published stays published),
   and the report names each one separately with whether its file is still on disk; an event
   the game still sends is not retired. The step is idempotent: a later session re-runs it to prove
   publication, and *Up to date* at the pinned version is the pass.

When `services.deployment deploy_items` is missing (`com.unity.pipeline` is not installed),
the agent cannot publish the schemas. Report the publish as not done (the no-PUBLISHED-version
statement below), name each pinned item under *Insights* in the Deployment window as where it
happens later, and end the run *not verified: schemas unpublished*.

When the upload can't happen (not cloud-linked, no reachable Editor, no deploy
command, or the deploy failed), the manifest lives only in the repo. The final report says plainly what that means: **the server holds no copy of the inventory, so every
server-side use of it (content denominators, declared-name validation, registry-resolved
dashboards) is unavailable until the manifest is uploaded.** Never invent an upload endpoint,
and never imply the server can dereference a registry it was never sent. When a schema could
not be published, say the same for the Schema Registry: **the Schema Registry holds no
PUBLISHED version of that event at the pinned version, so production ingest drops every one the
build sends under that name until it is published**; typed and context events are unaffected.

**Runtime registry identification:** Events carry `release_id` and `release_name` automatically when a Live Release is active (`com.unity.services.core` 1.19.0 or later, with `com.unity.services.authentication` signed in to a release, which provides the release context) and Services Core is initialized — no bootstrap code needed. Events also carry the packed manifest's content hash (`registry_hash`) automatically, and each custom event's payload carries its `custom_events` entry's `version` (the pinned `.ugtsevent` version) as `schema_version`; every event's `schema_version` and `audience` are the manifest's `schema_version` and `project.audience`, so there is no registry builder call. The registry manifest uploaded to Live Content is scoped per release, so the backend resolves registry references by release id. When the project has no active Live Release, no record carries a `release_id` (unset envelope fields are omitted from the wire record) and `schema_version` plus `registry_hash` are the registry identity on the envelope: say so in the report.

## The runtime gate

The runtime loads the packed manifest on the first `Events.Configure()....Apply()` and limits
sending to it:

- **No manifest packed, nothing sent.** Every event is dropped, with one warning per session.
- **Undeclared references are sent, flagged.** An event is still sent, in sandbox and production
  alike, when it names an id the manifest does not declare: the id of a screen, play, attempt,
  phase, flow, or consent-flow context, or a field the event's schema documents as a reference
  into a manifest section (resources, products, ad placements, progression nodes, flows and
  steps, modes). It takes an `event_seq`, lists the value in the envelope's `unregistered_refs`
  (field 67) as `<section>:<value>` (e.g. `progression:node_l557`), and logs one warning per id.
  Its context opens; the enter and exit list the ref and the events inside it do not. A sandbox capture holding one still fails `telemetry_verify`
  (`C2.ref-resolution`), and the failure names the listed ref. The session frame, ad breaks without a placement, cutscenes, and
  references whose entity the spec leaves unstated are not checked.
- **Some events are still dropped, with no `event_seq`.** A custom event whose `x_` name is
  undeclared, and a context enter or custom event that names no id at all. A dropped enter opens
  no context.
- **Dynamic ids join the registry.** `SendRegistryDelta` is always sent; when its entity type is
  listed in `dynamic.entity_types` (with `dynamic.enabled: true`), its id resolves from then on.

At build time the package warns when `Assets/Resources/Telemetry/telemetry_registry.ugtr` is
missing (the player would send nothing), when it has validation problems, and when a `.ugtr`
sits anywhere else in `Assets/`. Treat any of these warnings as a failed verify, with one
exception: on a finished retrofit a validation warning confined to settled entries is the
migration item (Step 1, the maintenance row), reported, not a failed verify.

## Dynamic registration

Content born at runtime — user-generated levels, procedural seeds, live-ops events — cannot
be in the build-time file. Declare the channel (`dynamic.enabled: true` with its
`entity_types`) in the manifest, then register each entity from the stream itself:

```csharp
// attrs is a JSON-serialised object: the wire has no variant type.
Events.SendRegistryDelta(entityType, id, name, attrs: "{\"peak_tier\":3,\"plan\":\"2,2,3\"}");
```

- Register **before** the first event that references the id.
- Registrations may carry declared attributes (serialised to the JSON string) — facts about
  the entity (not about any single event), such as a node's difficulty tier or
  configuration. Attribute keys are declared per entity type in the manifest, same
  discipline as custom events.
- **Dedupe on id plus values, never id alone.** The package sends every delta you call;
  deduping is your helper's job if you add one. If an entity's declared attribute values
  change (a re-generated node, a rebalanced live-ops event), send the delta again — the later
  registration is an update. A helper cache keyed on id alone silently swallows the change.

## Updating

When features, screens, levels, economy, ads, or IAP are added to the game, the manifest is
updated first in the same change: new entries (existing ids untouched, removals
tombstoned), re-upload the manifest and publish any new or re-versioned schema, then the
wiring. The line written to `CLAUDE.md` or `AGENTS.md` at
hand-off exists to make future sessions do exactly this.
