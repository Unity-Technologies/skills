---
name: create-2d-physics
description: "Creates and fixes Unity 2D physics in both systems, 2D Physics Core and the legacy Rigidbody 2D system. Use when the user asks about any 2D physics task, even if neither system is named. Not for 3D physics."
allowed-tools: WebFetch, WebSearch
metadata:
  version: 0.3.0
  support: "https://unity.com/support-services"
---

# Create and fix Unity 2D physics

Do not weigh 3D physics options. If evidence shows the user means 3D, say so and stop.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Avoid long explanations.
- Verify runtime behaviour with temporary `Debug.Log` lines, then remove them, because the user didn't ask for logging and leftover lines clutter their script and console. Don't build settle-detection or permanent logging they didn't ask for. See [Final step](#final-step).
- Never reflect over types to discover an API — fetch the member page instead. The system probe is the one exception.

### Passing C# to `eval`

`eval` compiles a statement block, not a file, so write the code without `using` directives and with fully qualified types. `return` what you want to read; it arrives at `data.result.result`. If the code doesn't compile, look up the error here:

| Error | Cause | Fix |
| --- | --- | --- |
| `CS0210` | A `using` directive, which the compiler reads as a resource-disposal statement | Remove the directive and qualify the types |
| `CS0246` / `CS0103` | An unqualified type, such as `Rigidbody2D` or `PhysicsWorld` | Use the full name, such as `UnityEngine.Rigidbody2D` or `Unity.U2D.Physics.PhysicsWorld` |
| `CS0104` | A bare `Object`, which is ambiguous with `object` | Use `UnityEngine.Object` |

## Step 0: Which physics system

Unity has two 2D physics systems. They share no code and never interact. Identify which one before writing any physics code — an answer from the wrong system compiles, runs, and does nothing.

Check the request, scene and existing scripts against the table below, in this order:

1. Check the scene components and existing scripts first, because components are the reliable signal.
2. Check the user's request wording: "add a collider" is component language, "spawn a thousand" is script language.
3. If still unsure which systems are installed, run the probe below.
4. If the route is still unknown, stop and ask — offer all three routes and never choose for the user.

| Signal | System |
| --- | --- |
| Physics Pose, Area, Constraint or Simulation components | Core components, 6000.7+ with the package |
| Rigidbody 2D, Collider 2D, Joint 2D, Effector 2D | Legacy |
| `Unity.U2D.Physics` or `UnityEngine.LowLevelPhysics2D` types in script | Core in script |
| `Rigidbody2D`, `Collider2D`, `Physics2D` types | Legacy |
| Both present | Separate simulations. Say so, treat separately |
| No signal | Stop and ask, offering the three routes above |

Key:
- Legacy = Rigidbody 2D and Collider 2D components.
- Core components = Physics Pose and Physics Area, requiring `com.unity.2d.physics` and Unity 6000.7+.
- Core in script = `Unity.U2D.Physics` API, no package needed.

Probe:
```
unity command eval --code 'var r = ""; foreach (var n in new[] { "UnityEngine.Rigidbody2D", "Unity.U2D.Physics.PhysicsWorld", "UnityEngine.LowLevelPhysics2D.PhysicsWorld", "Unity.U2D.Physics.PhysicsPose" }) { var f = false; foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) if (a.GetType(n) != null) f = true; r += f + ","; } return r;'
```
The Probe returns: legacy present, Core as `Unity.U2D.Physics`, Core as `UnityEngine.LowLevelPhysics2D`, Core components present. Core exists if either Core value is true; the true one gives the namespace. `false` rules that route out.

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **docs.unity.com**: The product version, eg `7000.0`

For docs.unity.com engine pages, use the Unity version, e.g. `6000.7`. Pages are markdown — URLs end in `.md`. Find the package version in `Packages/packages-lock.json`, because `manifest.json` can hold a range or a git URL instead of the resolved version.

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual` folder. Otherwise use the online `.md` pages listed in the references below.

Once you know the route, read the matching reference before acting:
- [2D Physics Core reference](references/2d-physics-core.md) - components, configuring them, creating bodies in script, ownership, and the full Core topic map.
- [Legacy 2D physics reference](references/legacy-2d-physics.md) - the legacy manual topic map.

### API reference

The Core and legacy APIs (`Unity.U2D.Physics` or `UnityEngine.LowLevelPhysics2D`, and `UnityEngine.Rigidbody2D`, `Collider2D`, `Physics2D`) are part of the Unity Engine. They are versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder, grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the project's Unity version in major.minor form). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Rigidbody2D.AddForce(UnityEngine.Vector2,UnityEngine.ForceMode2D)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Rigidbody2D.AddForce(UnityEngine.Vector2,UnityEngine.ForceMode2D)` => `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/rigidbody2d/addforce.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/vector2/ctor.md`), and indexers use `item` (e.g. `unityengine/vector2/item.md`).
   - For operators, fetch the class page (e.g. `unityengine/vector2.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'Rigidbody2D.AddForce.html'` => `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Rigidbody2D.AddForce.html`).

The Core **components** (Physics Pose, Area, Constraint, Simulation) are in the `Unity.U2D.Physics` namespace of the `com.unity.2d.physics` package. They are versioned by package version, not Unity version, so check the installed package version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the installed package version.
2. Fetch and cache `https://docs.unity3d.com/Packages/com.unity.2d.physics@<version>/xrefmap.yml` (replace `<version>` with the installed package version in major.minor form). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/Unity.U2D.Physics.PhysicsPose.html` => `https://docs.unity3d.com/Packages/com.unity.2d.physics@<version>/api/Unity.U2D.Physics.PhysicsPose.html`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

Always use the docs matching the project's Unity version and installed package version rather than relying on training knowledge, because APIs are often marked obsolete between versions and a member you recall might be obsolete or removed in the project's version.

## Worked examples

Prefer in order: the C# example on the type or member page you are using; existing project scripts (match their style); the sample project at `https://github.com/Unity-Technologies/PhysicsExamples2D`. Check all three before inventing a pattern.

## Final step

Physics changes alter runtime behaviour, so a clean compile isn't enough. Run these checks in order. If any check fails, reread the [docs pages](#full-topic-map) to find out what you missed, fix it, and run the checks again.

1. Use `console_status` to check the project has zero console errors.
2. Read back what you changed, because a change can compile cleanly and still do nothing:
   - For components you added or configured in the scene, use `eval` to check each one exists on its GameObject and its properties hold the values you set, such as a Collider 2D's `isTrigger` or the members listed on a Core component's type page.
   - For a script, read it back and check it uses the system and namespace from [Step 0](#step-0-which-physics-system), and sets each Core body's `type` explicitly.
3. Add temporary `Debug.Log` lines that print the behaviour you changed, such as a body's position or velocity, or a contact or trigger callback firing.
4. Enter Play mode and check the console shows the expected log lines and still has zero errors. You can also read the world through `eval`. Take no screenshots, because they take too much time.
5. Exit Play mode, remove the temporary `Debug.Log` lines, and check the console again has zero errors.

## Final report

Short checklist: what you changed and why, which physics system you used, anything left for the user to decide or do.

