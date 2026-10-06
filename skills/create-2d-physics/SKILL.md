---
name: create-2d-physics
description: "Creates and fixes Unity 2D physics in both systems, 2D Physics Core and the legacy Rigidbody 2D system, for any 2D physics question even if neither is named. Not for 3D physics."
allowed-tools: WebFetch, WebSearch
metadata:
  version: 0.3.0
  support: "https://unity.com/support-services"
---

# Create and fix Unity 2D physics

Do not weigh 3D physics options. If evidence shows the user means 3D, say so and stop.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:
- **docs.unity.com**: The Unity version, e.g. `6000.7`. Pages are markdown — URLs end in `.md`.
- **Package on docs.unity3d.com**: The package version, e.g. `@1.1`. Check `Packages/manifest.json` if unsure.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Don't use `using` directives or unqualified types in the `snippet` for `eval`. The compiler reads using `UnityEngine` and a bare AssetDatabase, Volume, or Object causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Avoid long explanations.
- Never leave verification code in the user's script; never build logging or settle-detection they didn't ask for. See [Final step](#final-step).
- Never reflect over types to discover an API — fetch the member page instead. The system probe is the one exception.

## Step 0: Which physics system

Unity has two 2D physics systems. They share no code and never interact. Identify which one before writing any physics code — an answer from the wrong system compiles, runs, and does nothing.

Check the request, scene and existing scripts against the table below. Components are the reliable signal — check scene and existing scripts first. Check the user's request wording too: "add a collider" is component language, "spawn a thousand" is script language.

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

If still unsure which systems are installed, run the probe. If the route is still unknown, stop and ask — offer all three routes and never choose for the user:

Probe:
```
unity command eval --code 'var r = ""; foreach (var n in new[] { "UnityEngine.Rigidbody2D", "Unity.U2D.Physics.PhysicsWorld", "UnityEngine.LowLevelPhysics2D.PhysicsWorld", "Unity.U2D.Physics.PhysicsPose" }) { var f = false; foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) if (a.GetType(n) != null) f = true; r += f + ","; } return r;'
```
The Probe returns: legacy present, Core as `Unity.U2D.Physics`, Core as `UnityEngine.LowLevelPhysics2D`, Core components present. Core exists if either Core value is true; the true one gives the namespace. `false` rules that route out.

## System reference

Once you know the route, read the matching reference before acting:
- [2D Physics Core reference](references/2d-physics-core.md) - components, configuring them, creating bodies in script, ownership, and the full Core topic map.
- [Legacy 2D physics reference](references/legacy-2d-physics.md) - the legacy manual topic map.

## Worked examples

Prefer in order: the C# example on the type or member page you are using; existing project scripts (match their style); the sample project at `https://github.com/Unity-Technologies/PhysicsExamples2D`. Check all three before inventing a pattern.

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console_status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `eval` rather than adding logging. Take no screenshots as they take too much time.

If a check fails, go back and reread the docs pages in the matching reference to find out what you missed.

## Final report

Short checklist: what you changed and why, which physics system you used, anything left for the user to decide or do.
