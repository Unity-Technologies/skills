---
name: light-scene
description: "Sets up, repairs and restyles the lighting of a Unity scene, from the first lights and bake to a new mood or lighting that changes over time. Make sure to use this skill when a scene needs lighting, or its lighting looks wrong or costs too much."
allowed-tools: WebFetch, WebSearch
required_editor_version: ">=6000.0.0"
metadata:
  version: 1.0.0
  documentation: "https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-configuration-workflow.md"
  support: "https://unity.com/support-services"
---

# Light a scene

Set up, fix, restyle, optimize or animate the lighting of a Unity scene: choose the workflow for the request below, read its file in full, and follow it.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step. Run C# through the connected Editor with the `script eval` command (`unity command script eval --code '<snippet>'`).
- Don't use `using` directives or unqualified types in the `snippet` for `script eval`: write `UnityEditor.AssetDatabase` and `UnityEngine.Rendering.Volume`, not `AssetDatabase` and `Volume`. Check that each API you call still exists in this Unity version, because several long-standing members became errors in Unity 6. The [error messages](#error-messages) table gives the fix for each compile error.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files, because pipeline, quality and Volume assets are shared by every scene in the project.
- Obey any limit the user puts on the work, over anything this skill says. If they say change nothing, change nothing; if they cap your answer's length, stay under it; if they name one component, answer about that one and say what you skipped. Running ten tools on a question that needed one sentence is a failure even when the answer is right. The same holds the other way: if the user asks for a look these defaults would avoid - an overpowering light, a second sun, clipped highlights - do it, and state the cost in one line rather than pushing back. The defaults here are for when the user has not said.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Verify any C# you author compiles BEFORE reporting it done, and say how. A non-compiling script blocks everything else in the project from compiling, so an unchecked script leaves the user worse off than nothing. If you cannot compile it, say so and keep it small enough to review by eye.
- Before concluding that the Editor, the render pipeline or a tool is broken, change ONE variable in a minimal probe and look at the render. Such claims - renderers "dropped" by a tool, post-processing "not running", HDR "clipped on the way in" - usually turn out to be a mistake in the agent's own setup: an object on a culled layer, a Volume parameter with `overrideState` off, a project with no Tonemapping override.
- A state read is not evidence that a change landed. An object can exist, be enabled and report correct bounds and still never draw, because the camera's `cullingMask` excludes its layer; a Volume parameter can display the right value and be ignored, because its `overrideState` is false. Only the render settles it.
- Read the workflow file for the request before you change anything, because the steps, checks and rules for that work live there and acting on this page alone skips them.
- Avoid long explanations.

## Step 0

Before you go forward with a workflow or diagnosis:

1. Work on the scene open in the Editor, because that is the one the user means by "my scene" or "this scene". If several scenes are loaded, ask which one. If the open scene has never been saved, work on it anyway and say that it still needs saving.
2. Read the active render pipeline, as [Render pipeline](#render-pipeline) describes, because it decides which pages and steps apply.
3. Open and read the [doc page(s)](#full-topic-map) that cover the specific thing you intend to do or fix, before you change anything, and check if you've missed anything. If your web-fetch tool has to be loaded before first use, load it.
4. List the documentation pages you read, by URL, in your final report.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

Relative paths in this skill, such as `references/topic-map.md`, resolve to this skill's own folder; do not search the project for them.

If the project's exact Unity version has no published manual, use the nearest published Unity 6 version and say which. If a page is missing for the project's version, read the same page in a later Unity 6 manual, such as `6000.3`, and say which version you read. If a page is unreachable - you fetched it and the fetch failed - say so and fall back to this skill's own guidance plus the installed packages. You MUST NOT proceed on memory alone without saying so, because an unstated guess about a version-specific value is the failure this step exists to prevent.

### Render pipeline

Read the active render pipeline from `UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline`, which is the asset actually in use and honours the quality level's override; `null` means the Built-in Render Pipeline. The workflows are written for URP, the preferred path. On URP every scene also has two default Volumes beneath its own: the project's Default Volume Profile and the active quality level's, per [Understand volumes in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volumes.md). Whenever you read Volumes, read those too, through `UnityEngine.Rendering.VolumeManager.instance.globalDefaultProfile` and `qualityDefaultProfile`, because exposure or a grade set there changes the scene even when it contains no Volume. On Built-in or HDRP, read [references/pipeline-notes.md](references/pipeline-notes.md) first - it says which steps change and which do not - then follow the workflow with those substitutions, and name the pipeline in your report. Do NOT refuse a project because of its pipeline.

### Project conventions

Unity's manual describes how lighting works. It cannot tell you what THIS project has already decided. Each workflow file says what to read in the project before you change a value - its own documentation, a sibling scene, its lighting and bake policies. Do that before your first change. Where a project convention and a workflow disagree, the project wins; name the document you followed. Do this because a setup that is correct in general can still be wrong for a project that has already decided these things - the most common reason this work gets rejected.

### When project sources disagree

Project docs often contradict each other or what the scenes ship. Treat that as a finding: name both values and their sources, ask only if it changes what the user will see, otherwise follow the written direction and report the shipped value as drift. Full procedure: [references/project-conventions.md](references/project-conventions.md).

### Full topic map

Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **Engine on docs.unity.com**: The Unity version, eg `6000.3`
- **Package on docs.unity3d.com**: The package version, eg `@17.0`. Check the package version if unsure.

Use only the first two numbers of the Unity version, because a full version such as `6000.3.2f1` gives a 404. docs.unity.com publishes the versions listed in `https://docs.unity.com/en-us/engine/llms.txt`. An unlisted version redirects to a general index page instead of the page you asked for, so treat it as having no published manual, as [Step 0](#step-0) describes.

Manual pages live under `https://docs.unity.com/en-us/engine/<VERSION>/manual/`. Keep the `.md` suffix, because it returns the page as plain Markdown. Links inside a page start with `/engine/`: add `https://docs.unity.com/en-us` before them. URP's local `Documentation~` holds no pages, so URP pages come from the online manual; some are landing pages - follow them down. HDRP pages live in the `com.unity.render-pipelines.high-definition` package manual on docs.unity3d.com, which has no `.md` version, so fetch those as linked; Built-in pages are in the main manual. The pages for each workflow, grouped by stage, are in [references/topic-map.md](references/topic-map.md): open only the pages for the step you are about to do, and name the ones you read.

## Choose the workflow

Choose the workflow from what the user wants changed. Open its file and read it in full before any change. A request that spans several workflows uses each file, in the order listed.

| The user wants | Read |
|---|---|
| A scene with no lighting, or only a default light, lit from scratch - or one light added to a lit scene, such as a lamp, fire or torch | [references/set-up.md](references/set-up.md) |
| A lit scene that looks wrong: too dark, washed out, flat, blocky or missing shadows, an unwanted flicker, or broken since a change | [references/diagnose.md](references/diagnose.md) |
| A bake run, configured or fixed, or lighting that differs between the Editor and a build | [references/bake.md](references/bake.md) |
| A different mood or time of day on an existing setup | [references/style.md](references/style.md) |
| The sky, environment reflections or fog set up or repaired | [references/environment.md](references/environment.md) |
| Materials that render magenta, or a custom shader that receives no light or shadow | [references/materials.md](references/materials.md) |
| Lighting that costs too much on a platform or quality tier | [references/optimize.md](references/optimize.md) |
| Lighting that changes over time: a day-night cycle, a moving sun, a flicker the user asks for, a blend between lighting states | [references/animate.md](references/animate.md) |
| A themed or dramatic look built from new lights, such as a ring of fires in a dark room | set-up, then style; add environment when the sky or ambient must go dark |
| New lights that also move or flicker | set-up, then animate |

- If the user only asks how Unity lighting works and wants no change, answer from the documentation at the length they asked for, without running a workflow.
- Post-processing effects set up for their own sake - bloom, vignette, depth of field - belong to the `urp-postprocessing` skill. Converting a project from the Built-in Render Pipeline to URP belongs to the `migrate-birp-to-urp` skill.
- Otherwise, if no row fits but the request is about how a scene is lit, start with diagnose, because its first section sends each kind of request to the right workflow.

## Rules for every workflow

### Anti-hallucination constraints

| Rule | Reason |
|---|---|
| NEVER state a menu path, UI label or API name from memory | These move between versions. The setting often called "ambient intensity" is Intensity Multiplier under Environment Lighting in Unity 6, per the [Lighting window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md) |
| Test a claimed mechanism against the render before stating it | "Tonemapping is None, so everything above 1.0 clips to white" is checkable: render and count the clipped pixels. An explanation the image contradicts is a wrong diagnosis in a confident voice |

Each workflow file adds the rows that matter for its own work.

### Error messages

| Message | Cause | Fix |
|---|---|---|
| `CS0210` from a `script eval` snippet | `script eval` compiles a statement block, not a file, so a `using` directive reads as a resource-disposal statement | Delete the `using` lines and fully qualify every type |
| `CS0246` or `CS0103` from a `script eval` snippet | A bare type such as `AssetDatabase` or `Volume` does not resolve in a statement block | Write the full name, such as `UnityEditor.AssetDatabase` or `UnityEngine.Rendering.Volume` |
| `CS0104` naming `Object` | `Object` matches both `UnityEngine.Object` and `System.Object` | Write `UnityEngine.Object` |
| `CS0619`: a member is obsolete | Unity deprecated the member and made calling it a compile error | Look the member up in this version's Scripting API and call the replacement it names |
| "Light Probe Groups are unavailable as Probe Volumes have been enabled", in the Inspector | The pipeline asset enables Adaptive Probe Volumes, so a Light Probe Group does nothing | Light moving objects with the project's Probe Volume instead, per [Use Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/use.md) and the bake workflow |

### Keep the change reversible and contained

You are making a scoped edit to lighting, and the user must be able to undo it and to see exactly what moved. Work to that standard:

1. Make the whole change one Ctrl+Z for the user. `Undo.RecordObject` alone leaves no undo entry when a snippet runs through `script eval`, because its snapshot is deferred to the end of an Editor event, so in each snippet:
   1. Open a group: `UnityEditor.Undo.IncrementCurrentGroup()`, name it with `UnityEditor.Undo.SetCurrentGroupName("<what you changed>")`, and keep `UnityEditor.Undo.GetCurrentGroup()`.
   2. Before you modify an object, call `UnityEditor.Undo.RegisterCompleteObjectUndo` on it, which snapshots immediately. After you create an object, call `UnityEditor.Undo.RegisterCreatedObjectUndo` on it.
   3. End with `UnityEditor.Undo.FlushUndoRecordObjects()` and `UnityEditor.Undo.CollapseUndoOperations(<group>)`, then read `UnityEditor.Undo.GetCurrentGroupName()` back. If it is not your label, say the change cannot be undone with Ctrl+Z.
2. List every object and asset you touched in your report, including any you touched only to read.
3. After each change, check its effect. If it made the problem worse, revert it before trying anything else, and say that you reverted it.
4. If the user asks you to revert, revert everything from that request and confirm what the state is now. Do not part-revert and do not substitute a different fix.

NEVER change a setting you were not asked about because it looked wrong. INSTEAD name it, say what you would change it to, and ask.

NEVER delete and recreate a GameObject or hierarchy to fix it. INSTEAD change the specific property, because destroyed objects break references elsewhere in the scene.

NEVER repeat an action that already failed with a larger value or a different setting. INSTEAD stop and report what you tried, because escalating a failing change is how a slow bake becomes a crash.

NEVER edit an asset shared by other scenes without saying so first. INSTEAD name the asset, name what else uses it, and ask.

## Final step

1. Check the project has zero console errors. Use `console_status` for counts. Find its name in the connected Editor with `unity command --query console`, because the name differs between versions, for example `unity command console status`.
2. Capture the Game view once, in Edit mode, and look at it, even when the scene holds little, because lighting is judged by eye: a scene can report perfect values and still render wrong. Find the capture command with `unity command --query capture`, which searches the connected Editor's commands as the `unity-cli` skill describes; `unity command --help` does not list them.
3. Stop there unless the user asked you to prove it works or you suspect a specific fault, such as an animated lighting cycle that never advances, which an Edit-mode capture can't show. If either holds, do the checks in the workflow file's Verify section, and put back anything you change only to run a check - an object you move, a time-of-day value, the active quality level, a setting you toggle - as soon as you have the reading, whether the check passed or failed, because the user asked for the lighting change and nothing else. Name anything you could not put back in the Final report.
4. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `script eval` rather than adding logging. Take no screenshots in Play mode, as they take too much time.

If a check fails, go back and reread the relevant documentation from the [Full topic map](#full-topic-map) for what you missed.

You MUST NOT run the fix-and-recheck loop more than three times. If it still fails on the third attempt, stop and give the user what you found instead of continuing, because past that point you are guessing rather than diagnosing.

## Final report

Give the user a concise but not dense checklist of what you did and why, including any incidental side effects (for example, settings you toggled to test something, or temporary files you created). If the user needs to follow up with more steps or decisions, list them. Include the items in the workflow file's Report section, and every object and asset you touched. List the documentation pages you read, by URL, and the captures you looked at, with what each showed, because those are the evidence behind what you report.

## References

Files in this skill's folder, by relative path:

- Workflows: [set-up](references/set-up.md), [diagnose](references/diagnose.md), [bake](references/bake.md), [style](references/style.md), [environment](references/environment.md), [materials](references/materials.md), [optimize](references/optimize.md), [animate](references/animate.md)
- [Documentation pages, by workflow and stage](references/topic-map.md)
- [Built-in and HDRP substitutions](references/pipeline-notes.md)
- [When project sources disagree](references/project-conventions.md)
- [Terms this skill uses in a specific sense](references/terms.md)
