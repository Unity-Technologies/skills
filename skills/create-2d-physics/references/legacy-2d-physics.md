# Legacy 2D physics

Relative paths in this file resolve to the skill directory.

## Step 0

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#manual-legacy-2d-physics) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

## Manual: legacy 2D physics

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual` folder. Start at `en/Manual/2d-physics/2d-physics.html` and follow its links, because local file names differ from the online paths (e.g. `2d-physics/rigidbody/rigidbody-2d-landing.html`).

Otherwise use the online pages.

Base: `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/`. Pages are `.md`.

Pages, relative to the base:

- `2d-physics.md` — contents page for legacy 2D physics
- `2d-physics/rigidbody-2d.md` — Rigidbody 2D: body types, simulation and movement
- `2d-physics/collider-2d.md` — Collider 2D types and their shared properties
- `2d-physics/effectors-2d.md` — Effector 2D components, such as area, buoyancy and platform effectors
- `2d-physics/2d-joints.md` — Joint 2D components that connect bodies
- `2d-physics/physics-2d-profiler.md` — profiling the 2D physics simulation
- `2d-physics/constant-force-2d-reference.md` — Constant Force 2D component (single page, no children)
- `2d-physics/physics-material-2d-reference.md` — Physics Material 2D asset (single page, no children)

Each landing page has a folder of the same name listing its children — follow those rather than guessing leaf names.

## Scripting reference

Legacy types live in the main scripting reference as `unityengine/<type>.md`, e.g. `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/rigidbody2d.md`. See the Scripting reference table in `references/2d-physics-core.md` for the full URL pattern, including how it differs from Core types.

## What you are likely to get wrong

| Your likely assumption | Actually |
| --- | --- |
| Your recall of legacy manual URLs is current | Build URLs from the [Manual](#manual-legacy-2d-physics) list, because the pages moved |
| Legacy manual pages are flat, like `class-Rigidbody2D.html` | Use the family folders, such as `2d-physics/rigidbody-2d/` and `2d-physics/collider-2d/` |
| A Core type or member has a legacy equivalent | Confirm on the legacy type's own page |
