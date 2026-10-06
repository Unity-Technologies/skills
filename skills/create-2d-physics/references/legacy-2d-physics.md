# Legacy 2D physics

Relative paths in this file resolve to the skill directory — the folder that holds the `SKILL.md` for this skill.

## Step 0

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#manual-legacy-2d-physics) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

## Manual: legacy 2D physics

Base: `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/`. Pages are `.md`.

`2d-physics.md` is the contents page. Landing pages, relative to the base: `2d-physics/rigidbody-2d.md`, `2d-physics/collider-2d.md`, `2d-physics/effectors-2d.md`, `2d-physics/2d-joints.md`, `2d-physics/physics-2d-profiler.md`, plus flat `2d-physics/constant-force-2d-reference.md` and `2d-physics/physics-material-2d-reference.md`.

Each landing page has a folder of the same name listing its children — follow those rather than guessing leaf names.

## Scripting reference

Legacy types live in the main scripting reference as `unityengine/<type>.md`, e.g. `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/rigidbody2d.md`. See the [Scripting reference](2d-physics-core.md#scripting-reference) table in the Core reference for the full URL pattern, including how it differs from Core types.

## What you are likely to get wrong

Legacy's page locations were reorganised — your recall of URLs is stale. Legacy manual pages are not flat like `class-Rigidbody2D.html`; they're reorganised under family folders: `2d-physics/rigidbody-2d/`, `2d-physics/collider-2d/`. Core collision layers use a 64-bit mask; legacy uses 32.
