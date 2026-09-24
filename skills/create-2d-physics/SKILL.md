---
name: create-2d-physics
description: "Creates, fixes and explains Unity 2D physics, in scripts and in the inspector, across both systems: 2D Physics Core (the Unity.U2D.Physics engine API, with the Physics Pose, Physics Area and Physics Constraint components on top) and the older system (Rigidbody 2D, Collider 2D, Joint 2D, Effector 2D and the Physics2D class). Make sure to use this skill for any 2D physics question, including working out which of the two systems a project uses, creating or moving bodies, adding colliders or shapes, joints and constraints, queries and raycasts, collision and trigger events, and diagnosing why a 2D object fails to appear, move or collide, even when the user names neither system. Not for 3D physics, which uses Rigidbody, BoxCollider, OnCollisionEnter and Physics.Raycast without the 2D suffix."
allowed-tools: WebFetch, WebSearch
metadata:
  version: 0.3.0
  support: https://unity.com/support-services
---

# Create and fix Unity 2D physics

You are reading this because the task is 2D physics, so it is never 3D. Do not weigh 3D physics, and do not check whether a 3D module is present. If the evidence says the user actually means 3D, say so and stop; never answer a 3D question in 2D.

Unity has two 2D physics systems. They share no code and never interact. Work out which one the user is in first, because an answer from the wrong one compiles, runs, and does nothing.

Prefer `WebFetch` over `WebSearch` to fetch documentation links - faster and lands on the exact reference. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.3`

## Which system

You must not write physics code until you know two things: which system, and components or script. Do not start work and raise the ambiguity afterwards.

1. Check the request, the scene and existing scripts against the signal table below. If they name the system and the route, take it and move on.
2. If you are still unsure which systems exist, ask the editor with the probe below. Do not reason from `Packages/manifest.json`: a module missing from it can still be active as someone else's dependency, and there may be no `packages-lock.json` either.
3. If the route is still unknown, stop and ask. Offer all three routes and never choose for the user:
   - Legacy components: Rigidbody 2D and Collider 2D.
   - Core components: Physics Pose and Physics Area, which need the `com.unity.2d.physics` package and 6000.7 or later.
   - Core in script: the `Unity.U2D.Physics` API, no package needed.

```
unity command eval --code 'var r = ""; foreach (var n in new[] { "UnityEngine.Rigidbody2D", "Unity.U2D.Physics.PhysicsWorld", "UnityEngine.LowLevelPhysics2D.PhysicsWorld", "Unity.U2D.Physics.PhysicsPose" }) { var f = false; foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) if (a.GetType(n) != null) f = true; r += f + ","; } return r;'
```

That returns legacy, Core as `Unity.U2D.Physics`, Core as `UnityEngine.LowLevelPhysics2D`, then Core components. Core exists if either Core value is true, and which one tells you the namespace. A false rules that route out. Anything else in doubt, stop and ask rather than inferring.

| Signal | System |
| --- | --- |
| Physics Pose, Area, Constraint or Simulation components | Core, 6000.7+ with the package |
| Rigidbody 2D, Collider 2D, Joint 2D, Effector 2D | Legacy |
| `Unity.U2D.Physics` or `UnityEngine.LowLevelPhysics2D` types in script | Core |
| `Rigidbody2D`, `Collider2D`, `Physics2D` types | Legacy |
| Both present | Separate simulations. Say so, treat separately |
| No signal anywhere | Stop and ask, offering the three routes in step 3 |

Components are the reliable signal, so check the scene and existing scripts first. Request wording counts too: "add a collider to this sprite" is component language, "spawn a thousand of these" is script language.

Version sets the namespace, never which system exists; the [Unity 6.5 upgrade guide](https://docs.unity3d.com/<VERSION>/Documentation/Manual/UpgradeGuideUnity65.html) covers the rename. Only the components need 6000.7.

Within Core: components for objects authored in the scene, script for objects created in bulk at runtime. Modules are `com.unity.modules.physics2d` and `com.unity.modules.physicscore2d`, both on by default, so they only matter if one was stripped.

## 2D Physics Core

The engine API is a built-in module, no package needed. The Physics Pose, Area and Constraint components come from `com.unity.2d.physics`, needing 6000.7 or later. Check `Packages/manifest.json` before suggesting a component. If it is absent, say so and offer to add it, using the `unity-package-management` skill to do the install; the Unity CLI does not manage packages. Everything else here works without it.

A component is not the physics object; [Physics Pose and Physics Area components](https://docs.unity3d.com/<VERSION>/Documentation/Manual/2d-physics-api/2d-physics-api-pose-and-area.html) describes what each one owns. Its own API is authoring and configuration only; the dynamics are on the owned object. Read and write that object, apply forces, query it, handle contacts, from a worker thread if you like. Authoring is the only real branch: after creation the code is identical either way.

The components use the public API alone, by firm design rule, so nothing is component-only and a user can write their own. When nothing built-in fits, a plain MonoBehaviour driving the engine API is the default; it can still consume the package's types. `PhysicsArea` can be derived from, as its package API page documents, but it is advanced, so offer that only when asked. `PhysicsPose` is sealed. Never say a case is unsupported. For many shapes or nested areas, Physics Area Composite already exists.

## Configuring a component

Never write serialized fields. `set_serialized_field` and `get_serialized_fields` will fight you, because the serialized names are not the public names, and it is the wrong route regardless. The components expose public properties and methods for everything; drive those through `eval`.

Definition and geometry are separate concerns. A pose's body settings live in its definition. An area's shape is its geometry. Configure them independently and do not expect one to carry the other.

A change to a definition or a geometry needs its apply call before it takes effect. The type's page states which one; read it rather than assuming the assignment was enough.

So before you configure any component, open that component's own API page and use only members it lists. This is a step, not a suggestion: the CLI advertises `set_serialized_field` and you will reach for it otherwise.

Build the URL in two calls, never pinning a version:

1. Fetch `https://docs.unity3d.com/Packages/com.unity.2d.physics@latest/` and read the version out of the redirect it serves.
2. Fetch `https://docs.unity3d.com/Packages/com.unity.2d.physics@<that version>/api/Unity.U2D.Physics.<Type>.html`.

One page per type, with every property and method on it, so one fetch covers the whole component. The `api/index.html` listing renders in the browser and fetches as nothing, so never start there.

`@latest` serves the newest *published* docs, which can lag the installed package. Compare the version from the redirect with the version in `Packages/manifest.json`. If the docs are older, say so once, then treat a member missing from the page as not yet documented, never as proof it does not exist. Do not tell the user a member is wrong on the strength of an older page.

## Ownership

Core only. Applies to worlds, bodies, shapes, chains and joints.

Each owner-gated call states it on its own page, as [PhysicsBody.Destroy](https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Unity.U2D.Physics.PhysicsBody.Destroy.html) does. On those calls the key argument looks optional because it defaults to zero, which matches only an object with no owner; `SetOwner` is different, and there zero means create a new key. A refusal logs a warning rather than throwing, so treat one as owner-gated, not broken.

Ownership is yours to use: create a key and own what you create. It exists because queries return objects you did not create, shapes above all, and deleting one you merely found is the mistake it prevents. Script-created objects are yours to destroy; component-created ones need the component removed; the default world is engine-owned and cannot be removed.

## Important

- Use the Unity CLI (`unity command --format json`) with the `unity-cli` and `unity-pipeline` skills. Run C# in the connected Editor with `unity command eval --code '<snippet>'`.
- An `eval` snippet compiles as a method body. `UnityEngine` and `UnityEditor` are in scope, so `Vector2` and `AssetDatabase` work bare; every other namespace must be written in full, `Unity.U2D.Physics.PhysicsWorld`, not `PhysicsWorld`. A `using` directive fails, because it parses as a using statement.
- `return` what you want to read. It arrives at `data.result.result`.
- Create a restore point you can roll back to if your changes fail. A project already under git needs nothing more.
- Do only what's asked. Don't change unrelated assets or files. Avoid long explanations.
- Never leave verification code in the user's script, and never build settle-detection or reporting machinery they did not ask for. See [Final step](#final-step) for how much proving is expected, which is usually none.
- Never reflect over types in the editor to discover an API. Fetch the member page, which carries full signatures. Reflection is slow and it is the single biggest waste of time on these tasks. The system probe is the one exception, because it only checks that a type exists.

## Step 0

Most tasks need no reading. Adding a component, applying a force or an impulse, moving a body, reading a velocity: just do it — except choosing which Area component, which the table below already answers.

Setting a component's values is the exception, and nothing above excuses it. Follow [Configuring a component](#configuring-a-component): its type page every time, and it does not count against the budget below.

Creating a body in script is the same: read the two pages in [Creating in script](#creating-in-script) first. They do not count against the budget below either.

Otherwise read one page only when you are about to use a member you cannot name with confidence, or the task is in an area this skill does not cover. Never read more than two.

The cost of reading is real. A task that should take seconds must not become minutes of research.

## Creating in script

Before creating a Core body in script, read these two type pages and follow their examples. Set `type` on the definition explicitly.

- `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Unity.U2D.Physics.PhysicsBodyDefinition.html`
- `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Unity.U2D.Physics.PhysicsBody.html`

Use the project's version for `<VERSION>`, or 6000.7 if you cannot find it; the API only changed namespace. On 6000.3 and 6000.4 the code namespace is `UnityEngine.LowLevelPhysics2D`, but the page name drops the `UnityEngine.` prefix, as in `LowLevelPhysics2D.PhysicsBody.html`.

These, joints, geometry and every other script type are engine types, documented only in the main scripting reference. Never look for them in the `com.unity.2d.physics` package docs, which cover the components alone.

## What you are likely to get wrong

The two systems fail you oppositely, and nothing carries over between them unverified. On Core your API recall is wrong, because it is young and was renamed. On legacy your API recall is broadly right but your recall of page locations is stale, because those pages were reorganised.

| Your likely assumption | Actually |
| --- | --- |
| Legacy manual pages are flat, like `class-Rigidbody2D.html` | Reorganised under family folders: `2d-physics/rigidbody/`, `2d-physics/collider/` |
| A Core component holds the state, as Rigidbody 2D does | It owns a body, shape or joint. Operate on the owned object |
| The namespace is `UnityEngine.LowLevelPhysics2D` | 6000.4 and earlier. From 6000.5 it is `Unity.U2D.Physics` |
| Box2D v2 naming and semantics apply to Core | It is Box2D v3. Most older forum answers describe v2 |
| Angles are in radians, as Box2D uses | Read the property page. Hinge angles are documented in degrees |
| Core collision layers are a 32-bit mask | 64 layers in Core, 32 in legacy |
| Some behaviour is component-only | None is |
| A user's own component needs wiring to work with Unity's | None. All interaction is engine-side, one set of rules |
| Any handle can be destroyed | Only what you own. See [Ownership](#ownership) |
| You must create a world first | A valid default world exists in an empty scene. Use `PhysicsWorld.defaultWorld` |
| Debug visuals need gizmos or `Debug.DrawLine` | Core has a physics renderer, automatic per world draw options plus explicit draw calls, scopeable to editor, development player or any player. Never hand-roll it |
| A method exists because the other system has one like it | Confirm it on its own page |
| A body's type is set with `bodyType` and `RigidbodyType2D` | Obsolete, left over from the rename, and not in the docs at all. Use `type` with `PhysicsBody.BodyType` |
| A newly created body has a known default type | Do not assume a specific default — set `.type` explicitly; infer dynamic vs static from the request's words: falls/thrown/bounces → Dynamic, ground/wall/anchor → Static |
| "add a circle/capsule/polygon/segment area" means the dedicated component | Could be that or Primitive with `shapeType` set to match — same shape, two routes. Prefer the dedicated one when fixed at authoring time, Primitive only if it must switch at runtime. Chain Segment has no dedicated component — Primitive is its only route |

## Full topic map

Unversioned links exist in the wild, including on Unity's own pages, and resolve to the current release rather than the project's, so never use or copy that form.

Core script types are the exception. Their API is unchanged apart from the namespace, so the version only decides whether to write `Unity.U2D.Physics` or `UnityEngine.LowLevelPhysics2D`. Read their pages from the project's version when you know it, otherwise from 6000.7; never stop or guess over the version for them.

### Manual: 2D Physics Core

Base: `https://docs.unity3d.com/<VERSION>/Documentation/Manual/2d-physics-api/`

- `2d-physics-api-landing.html` contents
- `2d-physics-api-introduction.html` what it is, component to object mapping
- `2d-physics-api-get-started-landing.html` first scene, with components
- `2d-physics-api-create-objects-landing.html` Physics Pose and Physics Area components
- `2d-physics-api-connect-objects-landing.html` joints
- `2d-physics-api-properties-landing.html` shared definitions, pinned properties, custom data, global settings, preferences
- `2d-physics-api-interactions-landing.html` collisions, contacts, triggers, collision filtering. Queries have no Manual page; they are the `PhysicsWorld` cast and overlap methods in the scripting reference
- `2d-physics-api-debug-drawing.html` Scene view editing, rendering modes

Outside this skill; read in full before acting: `2d-physics-api-worlds-landing.html` multiple worlds, `2d-physics-api-3d-planes.html` non-XY plane, `2d-physics-api-multithreading.html` jobs and locking.

Component references follow a convention, so build the URL: `2d-physics-api-reference-body.html` for Physics Pose, `-reference-area-<kind>.html` (capsule, circle, composite, contour, path, polygon, primitive, segment, sprite), `-reference-joint-<kind>.html` (distance, fixed, hinge, relative, slider, wheel), `-reference-constraint-ignore.html`, `-reference-simulation-component.html`. Two asset references break the pattern: `-reference-world.html` is the Physics Simulation Definition asset, and `-reference-simulation-asset.html` is the Physics Simulation World asset.

This manual is mostly about components. Joint structs, definition structs, geometry, queries, math, events and destruction are barely named in it; use the scripting reference for those.

### Manual: legacy 2D physics

Base: `https://docs.unity3d.com/<VERSION>/Documentation/Manual/2d-physics/`

`2d-physics.html` contents, then `rigidbody/rigidbody-2d-landing.html`, `collider/collider-2d-landing.html`, `effectors/effectors-2d-landing.html`, `joints/2d-joints-landing.html`, `physics-profiler/physics-2d-profiler-landing.html`, plus flat `constant-force-2d-reference.html` and `physics-material-2d-reference.html`.

Each landing lists its own children. Follow those rather than guessing a leaf name.

### Scripting reference

Build the URL; do not search for member pages or guess titles.

Base: `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/`

| You want | Pattern | Example |
| --- | --- | --- |
| Legacy type, in `UnityEngine` | `<Type>.html` | `Rigidbody2D.html` |
| Core type | `Unity.U2D.Physics.<Type>.html` | `Unity.U2D.Physics.PhysicsBody.html` |
| Core type, 6000.3 and 6000.4 | `LowLevelPhysics2D.<Type>.html` | `LowLevelPhysics2D.PhysicsBody.html` |
| Method or nested type | `<type>.<Member>.html` | `Unity.U2D.Physics.PhysicsBody.CreateShape.html` |
| Property or field | `<type>-<member>.html` | `Unity.U2D.Physics.PhysicsBody-linearVelocity.html` |
| A joint in script | Start at `Unity.U2D.Physics.PhysicsJoint.html`, then `Physics<Kind>Joint` and `Physics<Kind>JointDefinition` | `Unity.U2D.Physics.PhysicsHingeJoint.html` |

A type page lists members; a method page lists every overload. Units, ranges and defaults are on property pages. A 404 means the name is wrong: fetch the type page and read its members rather than guessing again.

Type pages carry C# examples, and only some member pages do. Read the one closest to the member you are using, because it matches the version you target.

For the Core renderer start at `Unity.U2D.Physics.PhysicsWorld-renderingMode`, `Unity.U2D.Physics.PhysicsCoreSettings2D`, and the `PhysicsWorld` draw family, `DrawGeometry`, `DrawShapeProxy`, `DrawQueryResult`, `DrawLineStrip` and static `DrawShapes`.

### Package documentation

The main scripting reference covers engine types only, so never build one of those URLs for a package type.

Package docs live at `https://docs.unity3d.com/Packages/<package>@<major.minor>/`, manual under `manual/`, XML-generated API under `api/`. Major and minor only, never the patch. For version agnosticism fetch `@latest/` and read the version from the redirect, then build from that; appending a path to `@latest` fails.

Package API pages are fully qualified, `api/Unity.U2D.Physics.PhysicsPose.html`, one page per type with all members on it, so the `.Member` and `-member` patterns do not apply. The index renders in the browser and fetches as nothing, so go straight to a named type.

## Worked examples

Prefer, in order: the C# example on the type or member page you are using; scripts already in the project, matching their style; then the sample project, `https://github.com/Unity-Technologies/PhysicsExamples2D`. Never invent a pattern before checking all three.

## Final step

1. Check the project compiles with zero console errors. Use `console_status` for counts rather than pulling the console, which can return whole stack traces you do not need.
2. Stop there, unless the user asked you to prove it works or you have a specific reason to think it is broken. Do not enter Play mode to admire your own work, and never measure jump heights, resting positions, sleep states or velocities unless the user asked for those numbers.
3. If you do enter Play mode, do it once, only to chase a fault you already suspect. Read the world through `eval` rather than adding logging to the user's script, and take no screenshots.
4. If a check fails, reread the relevant page from the topic map for what you missed.

## Final report

Give a short checklist of what you changed and why, and say which physics system you worked in. List anything left for the user to decide or do.
