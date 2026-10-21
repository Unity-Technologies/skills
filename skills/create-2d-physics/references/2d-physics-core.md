# 2D Physics Core

## Contents

- [Step 0](#step-0) — when to read before acting
- [Configuring a component](#configuring-a-component)
- [Creating in script](#creating-in-script)
- [Ownership](#ownership)
- [What you are likely to get wrong](#what-you-are-likely-to-get-wrong)
- [Manual: 2D Physics Core](#manual-2d-physics-core)
- [Scripting reference](#scripting-reference)
- [Package documentation](#package-documentation)

The engine API is a built-in module — no package needed. The Physics Pose, Area and Constraint **components** come from `com.unity.2d.physics`, requiring 6000.7+. Check `Packages/manifest.json` before suggesting a component; if absent, offer to add it using the `unity-package-management` skill.

A component is not the physics object. It owns a body, shape or joint — operate on that owned object. Read/write it, apply forces, query it, handle contacts (from a worker thread if needed). See [Physics Pose and Physics Area components](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/2d-physics-api/create-objects/pose-and-area.md).

The components use only the public API, by design, so nothing is component-only — a plain MonoBehaviour driving the engine API always works. Never say a case is unsupported. For many shapes or nested areas, Physics Area Composite already exists.

Within Core: components for scene-authored objects, script for bulk runtime creation. The modules `com.unity.modules.physics2d` and `com.unity.modules.physicscore2d` are on by default; only matter if stripped. The Unity version sets the namespace, not which system exists. Read the [Unity 6.5 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity65.md) (local: `en/Manual/UpgradeGuideUnity65.html`) for the rename.

## Step 0

Before you go forward with a workflow or diagnosis:

1. Open and read the documentation page(s) from the [Manual](#manual-2d-physics-core) or [Scripting reference](#scripting-reference) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

Always use versioned URLs — unversioned links resolve to the current release, not the project's version.

Core script type pages are an exception: the API only changed namespace, so the version only decides whether to write `Unity.U2D.Physics` or `UnityEngine.LowLevelPhysics2D`. Use the project's version when known, otherwise 6000.7.

Most tasks (apply force or impulse, move a body, read velocity, add a component) need no reading — just do it.

Exceptions that always require reading first:
- **Setting component values**: fetch the type page per [Configuring a component](#configuring-a-component).
- **Creating a body in script**: read the two pages in [Creating in script](#creating-in-script).
- **Any member you cannot name with confidence**: read one page. Never read more than two pages for a single task.

## Configuring a component

Follow these rules for configuring a component:

- Never write serialized fields — the serialized names differ from public names. Drive everything through public properties and methods via `eval`.
- Definition and geometry are separate: a pose's body settings live in its definition; an area's shape is its geometry. Configure them independently. A change to either needs its apply call before it takes effect — the type page states which one.
- Before configuring any component, fetch its API page and use only members it lists. Read the installed version from `Packages/packages-lock.json` (major.minor, e.g. `1.1`), then fetch:
`https://docs.unity3d.com/Packages/com.unity.2d.physics@<version>/api/Unity.U2D.Physics.<Type>.html`

One page per type covers all members. If a member is missing, treat it as undocumented, not absent.

## Creating in script

Before creating a Core body in script, read these two pages and follow their examples. Set `type` on the definition explicitly.

- `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/u2d/physics/physicsbodydefinition.md`
- `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/u2d/physics/physicsbody.md`

Use the project's version, or 6000.7 if unknown. On 6000.3–6000.4 the namespace is `UnityEngine.LowLevelPhysics2D`, e.g. `.../script-reference/unityengine/lowlevelphysics2d/physicsbody.md`.

Script types — joints, geometry, queries, and everything else — are engine types in the main scripting reference only. Never look for them in the `com.unity.2d.physics` package docs.

## Ownership

Core only. Before you destroy or change a world, body, shape, chain or joint you didn't create in this task, read the owner rules on these pages and on the member page you're calling, such as [PhysicsBody.Destroy](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/u2d/physics/physicsbody/destroy.md):

- [PhysicsBody.SetOwner](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/u2d/physics/physicsbody/setowner.md)
- [PhysicsWorld.CreateOwnerKey](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/u2d/physics/physicsworld/createownerkey.md)

Follow these rules:

- Create an owner key and own what you create, so later calls can tell your objects from the user's.
- Never destroy an object you only found through a query, because it belongs to the user's code or a component.
- To remove a component-created object, remove its component instead.
- After an owner-gated call, check the console for warnings, because a refused call doesn't throw.

## What you are likely to get wrong

Core's API is young and was renamed, so your recall is unreliable. Nothing carries over between systems unverified. Where a row links a page, read it instead of relying on recall.

| Your likely assumption | What to do instead |
| --- | --- |
| Legacy manual pages are flat, like `class-Rigidbody2D.html` | Build URLs from the folder paths in the Legacy 2D physics reference |
| Your recalled Core namespace is right for the project | Check the project's Unity version, then read the [Unity 6.5 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity65.md) |
| Box2D v2 naming and semantics apply to Core | Ignore older forum answers. Read the Core type page |
| A property uses the unit Box2D or the other system uses | Read the property page for its unit and range |
| Core layer masks are the same width as a `LayerMask` | Read [PhysicsMask](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/u2d/physics/physicsmask.md) |
| Some behaviour is component-only | None is. Drive the engine API from a MonoBehaviour |
| A user's own component needs wiring to work with Unity's | None. All interaction is engine-side |
| Any handle can be destroyed | Only what you own. Follow [Ownership](#ownership) |
| You must create a world first | Read [PhysicsWorld.defaultWorld](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/u2d/physics/physicsworld/defaultworld.md) |
| Debug visuals need gizmos or `Debug.DrawLine` | Use the Core physics renderer from [Scripting reference](#scripting-reference). Never hand-roll it |
| A method exists because the other system has one like it | Confirm on its own page |
| A recalled member such as `bodyType` still sets body type | Read [PhysicsBody.type](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unity/u2d/physics/physicsbody/type.md) |
| A newly created body has a known default type | Don't assume. Set `.type` explicitly: falls, thrown or bounces → Dynamic. Ground, wall or anchor → Static |
| "add a circle/capsule/polygon/segment area" means the dedicated component | Could be that or Primitive with `shapeType` — same shape, two routes. Prefer dedicated when fixed at authoring time; Primitive only if it must switch at runtime. Chain Segment has no dedicated component |

## Manual: 2D Physics Core

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual/2d-physics-api/` folder. Local file names are flat: `2d-physics-api-<last path segment>.html` (e.g. `create-objects/reference-body.md` => `2d-physics-api-reference-body.html`), and section pages end `-landing` (e.g. `connect-objects.md` => `2d-physics-api-connect-objects-landing.html`). Start at `2d-physics-api-landing.html` and follow its links if a name doesn't match.

Otherwise use the online pages.

Base: `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/2d-physics-api/`. Pages are `.md`. The landing page is `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/2d-physics-api.md` — contents.

Paths below are relative to the base:
- `introduction.md` — what it is, component-to-object mapping
- `get-started.md` — first scene with components
- `create-objects.md` — Physics Pose and Physics Area components. Children in `create-objects/`: `pose-and-area.md`, `physics-object.md`, `add-sprite.md`, `debug-drawing.md` (Scene view editing, rendering modes)
- `connect-objects.md` — joints. Children in `connect-objects/`: `joints.md`, `create-constraint.md`, `constraint-events.md`
- `properties.md` — definitions, pinned properties, custom data, global settings. Children in `properties/`: `definitions.md`, `pin-properties.md`, `custom-data.md`, `class-physics-core-settings2d.md`, `preferences-window-reference.md`
- `interactions.md` — collisions, contacts, triggers, filtering (queries are `PhysicsWorld` cast/overlap methods in the scripting reference). Children in `interactions/`: `introduction.md`, `collisions-enable.md`, `collision-handle.md`

Read in full before acting (outside this skill's scope): `worlds.md`, `3d-planes.md`, `multithreading.md`.

Component reference pages follow a pattern — build them:
- Body: `create-objects/reference-body.md`
- Area: `create-objects/reference-area-<kind>.md` (capsule, circle, composite, contour, path, polygon, primitive, segment, sprite)
- Joint: `connect-objects/reference-joint-<kind>.md` (distance, fixed, hinge, relative, slider, wheel)
- Constraint: `connect-objects/reference-constraint-ignore.md`
- Simulation: `worlds/reference-simulation-component.md`
- Assets: `worlds/reference-world.md` (Physics Simulation Definition), `worlds/reference-simulation-asset.md` (Physics Simulation World)

This manual covers components. For joint structs, definitions, geometry, queries, math, events and destruction — use the scripting reference.

## Scripting reference

Build the URL — do not search for member pages. Names are lowercase.

Base: `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/`

| You want | Pattern | Example |
| --- | --- | --- |
| Legacy type in `UnityEngine` | `unityengine/<type>.md` | `unityengine/rigidbody2d.md` |
| Core namespace (lists types) | `unity/u2d/physics.md` | |
| Core type | `unity/u2d/physics/<type>.md` | `unity/u2d/physics/physicsbody.md` |
| Core type, 6000.3–6000.4 | `unityengine/lowlevelphysics2d/<type>.md` | `unityengine/lowlevelphysics2d/physicsbody.md` |
| Method, property, field or nested type | `<type page without .md>/<member>.md` | `unity/u2d/physics/physicsbody/createshape.md`, `unity/u2d/physics/physicsbody/linearvelocity.md` |
| A joint in script | Start at `unity/u2d/physics/physicsjoint.md`, then `physics<kind>joint` and `physics<kind>jointdefinition` | `unity/u2d/physics/physicshingejoint.md` |

A type page lists members; a method page lists every overload. Units, ranges and defaults are on property pages. A 404 means the name is wrong, or that version's docs aren't published — fetch the type page and read its members, or try an adjacent version.

For the Core renderer: start at `unity/u2d/physics/physicsworld/renderingmode.md`, `unity/u2d/physics/physicscoresettings2d.md`, and `physicsworld` draw methods: `drawgeometry`, `drawshapeproxy`, `drawqueryresult`, `drawlinestrip`, `drawshapes`.

## Package documentation

Package API pages are live only — not local files. Fetch using the version from `Packages/packages-lock.json`:
`https://docs.unity3d.com/Packages/com.unity.2d.physics@<major.minor>/api/Unity.U2D.Physics.<Type>.html`

The package manual pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/com.unity.2d.physics@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time. Start with `Documentation~/TableOfContents.md`, or list the `Documentation~/` `.md` files if there is none, to find the page you need.

If that folder has no `.md` files, use the online pages at `https://docs.unity3d.com/Packages/com.unity.2d.physics@<VERSION>/manual/`, because some package versions don't ship their docs.

The `api/index.html` listing fetches as nothing — go straight to a named type page.
