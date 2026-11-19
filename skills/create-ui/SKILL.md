---
name: create-ui
description: "Creates, edits, and explains Unity game and editor UI in UI Toolkit, uGUI, or IMGUI, matching the framework the project already uses. Use when the user wants a menu, HUD, or editor window built, changed, styled, or explained."
---

# Create and edit Unity UI

Build, change, and explain UI with whichever of Unity's three UI frameworks fits the project, and deliver new UI as a working demo that you have run and reviewed.

This skill works best with the Unity CLI driving an open Editor (the `unity-cli` skill), the workflow it was built and tested with: compiling, reading the Console, entering Play mode, running tests, and capturing the Game view from the command line. Without it, the build and review steps need the user to do those in the Editor.

## Important

- Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:
  - **docs.unity.com**: The Unity version, eg `6000.7`. Read it from `ProjectSettings/ProjectVersion.txt` and keep the major and minor parts only. The guides link the `.md` form of each page, which is shorter to read than the HTML page.
  - **Package docs**: if the project has the package's docs at `Library/PackageCache/<package>@*/Documentation~/`, read those `.md` files instead of fetching, as the uGUI guide does.
- Before you go forward with a workflow or diagnosis, open and read the doc pages that the framework guide says to read for what you're about to build, and name the pages you read in your reply. Fetch other pages only for a feature you use that the guide doesn't cover. This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.
- Create a restore point you can roll back to if your changes fail, because UI changes touch scenes, assets, and scripts at once: copy the scene and any files you'll change into `Temp/restore/` in the project. Unity doesn't import `Temp/`, and nothing outside the project changes.
- Do only what's asked. Don't change unrelated assets or files. Avoid long explanations. A request to create UI asks for the deliverables in Step 4, so build all of them.
- Never leave verification code in the user's script. Never build logging or detection into their scripts that they didn't ask for; the mock HUD test's `[UI check]` log line is part of the test, not of their scripts. To check behaviour in Play mode, read the values you need through `script eval`, as the framework guide's Checks section describes.
- Use the Unity CLI to compile, read the Console, enter Play mode, and capture the Game view. For UI Toolkit, the guide lists the commands and flags a UI build needs, so load the `unity-cli` skill only if one of them fails; for uGUI and IMGUI, use the `unity-cli` skill. If the CLI can't reach the Editor, ask the user to focus the Unity Editor and tell you what the Console shows, because the Editor only imports and compiles new files when it's focused or told to recompile.

## Glossary

| Term | Meaning |
|---|---|
| UI Toolkit (UITK, UIElements) | UI built from UXML files (structure) and USS files (styles), for runtime and Editor UI |
| uGUI (Unity UI, Canvas UI) | UI built from GameObjects with RectTransforms under a Canvas, from the `com.unity.ugui` package |
| IMGUI (immediate mode GUI) | UI drawn from code every frame in `OnGUI()`, used for older Editor tools and debug overlays |
| "legacy UI" | Usually uGUI. If the user points at `OnGUI` code, they mean IMGUI. |
| Panel Renderer, UI Document | Components that show a UXML file in a scene. New UI uses Panel Renderer; UI Document isn't the default for new UI, but existing scenes may use it. |
| Panel Settings | The asset that controls how a UI Toolkit panel renders and scales |
| Theme Style Sheet (TSS) | Default styles for the built-in UI Toolkit controls, assigned on the Panel Settings |
| TextMesh Pro (TMP) | uGUI's text components and font assets. UI Toolkit uses its own TextCore font assets. |
| Controller script | The MonoBehaviour this skill adds to feed live values to the UI and handle its input |

## Step 1: Get the project context

1. Read `ProjectSettings/ProjectVersion.txt`. Its major and minor version is `<VERSION>`. If it's older than 6000.7, tell the user this skill is written for Unity 6000.7 and later, and ask before you continue.
2. Read `Packages/manifest.json`. `com.unity.ugui` provides uGUI and TextMesh Pro, and `com.unity.inputsystem` decides which input module uGUI needs.
3. Read `activeInputHandler` in `ProjectSettings/ProjectSettings.asset`: 0 is the old Input Manager, 1 is the Input System package, and 2 is both.
4. Check whether the Unity CLI reaches an open Editor for this project. If it does, note the `cursor` that `console status` returns, so you can later read only the Console entries logged after you started. Never run `console clear`, because it also deletes the user's own entries, including errors they may want fixed.

## Step 2: Pick the framework

1. If the request names a framework, a file type, or a component from this table, use that framework, because the user's choice overrides what the project uses elsewhere.

   | Request mentions | Framework |
   |---|---|
   | `.uxml` or `.uss` files, "UI Toolkit", "UITK", "UIElements", `CreateGUI`, Panel Renderer, UI Document | UI Toolkit |
   | "uGUI", "Canvas", "RectTransform", a UI `.prefab`, "legacy UI" (unless it points at `OnGUI` code) | uGUI |
   | "IMGUI", `OnGUI`, `OnInspectorGUI`, "immediate mode" | IMGUI |

2. Otherwise, search the project and use the framework it already uses for the same kind of UI (runtime or Editor), because a second framework for the same kind of UI doubles the styling and input setup.

   | Look for | Indicates |
   |---|---|
   | `.uxml` or `.uss` files outside `Editor` folders, Panel Renderer or UI Document components in scenes | UI Toolkit, runtime |
   | `.uxml` files in `Editor` folders, editor scripts with `CreateGUI()` | UI Toolkit, Editor |
   | `Canvas` components in scenes or prefabs | uGUI |
   | Editor scripts with `OnGUI()` or `OnInspectorGUI()` | IMGUI |

3. If the project has no runtime UI yet and the request names no framework, provide pros and cons for each framework given the current task and let the user decide.
4. If the request is for Editor UI and the project has no Editor UI pattern, use UI Toolkit, because Unity recommends it for new Editor UI. Use IMGUI only when the code you're changing already uses it or the user asks for it.
5. If the request is a comparison question ("Which one should I use for mobile?"), answer it directly without changing files. Use the feature differences on the [UI system comparison](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/ui-system-compare.md) page and relate them to the user's project.
6. If the user shares a Figma link, say this skill can't import Figma designs, and ask for a description or a screenshot of the screen. Then continue with Step 3.

## Step 3: Read the guide

Read the guide for the framework, then the doc pages it lists for what you're building:

| Framework | Guide |
|---|---|
| UI Toolkit | [UI Toolkit guide](references/uitk-skill.md) |
| uGUI | [references/ugui-skill.md](references/ugui-skill.md) |
| IMGUI | [references/imgui-skill.md](references/imgui-skill.md) |

The guides send you to these files when you need them:

- [references/ui-visual-design.md](references/ui-visual-design.md): palette, type, spacing, states, and how to review a capture. Read it for any new UI.
- [references/uitk-pointermanipulator-guide.md](references/uitk-pointermanipulator-guide.md): drag and drop for UI Toolkit inventories and crafting screens.
- [references/uitk-painter2d.md](references/uitk-painter2d.md): rings, charts, and custom shapes in UI Toolkit.
- [references/uitk-svg-icons.md](references/uitk-svg-icons.md): SVG icons for UI Toolkit.
- [references/uitk-testing.md](references/uitk-testing.md): the mock HUD test that drives a UI Toolkit screen through its states, checks its interactions, and captures each state, and Edit Mode tests for reusable controls.
- [references/ugui-scrollview-setup.md](references/ugui-scrollview-setup.md): uGUI scroll views.

## Step 4: Do the work

- **Question about existing UI:** inspect the files, scene, or prefab and answer. Don't change files.
- **Targeted edit:** change what the request covers, and keep the rest of each file as it was.
- **New UI:** build it in two phases, so the UI works before you spend time on its look.

  **Phase 1: function.** Make the UI work, and prove it.
  1. **Files:** create the files the guide lists for a screen, with plain styles. Define colors, sizes, and spacing once (USS custom properties in UI Toolkit), so phase 2 can restyle the screen in one place.
  2. **Scene:** for runtime UI, add the UI to the open scene. If the open scene is untitled, first save it under `Assets/Scenes/` with a name that describes the UI, using an explicit path. Editor UI (windows, Inspectors) needs no scene.
  3. **Controller script:** make values change, interactions respond, and screens switch, unless the user asks for a static layout. Give every state a public method, and keep demo gameplay that changes values in a separate script, so tests can drive the UI on their own.
  4. **Tests and a quick visual check:** for UI Toolkit, follow the guide's phase 1 checks: a mock HUD test that sets each state through the controller, checks the interactions, asserts the layout, and captures each state, over dark and light backgrounds for a HUD drawn over the game and a dark one for a screen that covers it ([references/uitk-testing.md](references/uitk-testing.md)). Write Edit Mode tests only for a reusable custom control. A screen with one or two states and no interactions (a compass, a timer) skips the test framework: set its states in Play mode and capture the Game view. For uGUI, enter Play mode and read the sizes the guide's Checks section lists through `script eval`. Fix anything missing, clipped, overlapping, or unreadable before moving on.

  **Phase 2: theme and quality.** Make it look like it belongs in the game.
  1. **Theme:** apply the tips in `references/ui-visual-design.md`: a direction that fits the game, the palette, type scale, spacing, depth, interactive states, motion, and SVG icons for clarity.
  2. **Review:** do at least two review passes. One pass is a theme change, then a fresh mock HUD test run (UI Toolkit) or a fresh Game view capture in Play mode (uGUI), then the design guide's review checklist on the new captures. Fix the most visible problem in the next pass. Captures from before a change, phase 1's quick check, and the in-context capture below don't count as passes, because only a fresh capture shows whether the change worked. For Editor UI, review a fresh capture of the open window instead (`capture editor_window`).
  3. **In context:** capture the Game view in Play mode once, to see the UI over the real scene. For UI Toolkit, use the one-command capture script in the testing reference; if Play mode doesn't advance past frame 1 after a second try, skip the capture and say so in the final report.
  4. **Before the final step:** for new UI, check that this phase had at least two review passes, each with fresh captures after a change. If it had fewer, do the missing passes now.

## Final step

Do the following checks:

1. Check that `console status` reports no compile failure, and that `console get --since <cursor> --level error`, from the cursor you noted in Step 1, returns no errors your changes caused. Errors that were already in the Console before you started are the user's: name them in the final report instead of fixing them.
2. Stop there unless the user asked for proof or a specific fault is suspected, because phase 1's tests and phase 2's review passes already checked the UI.
3. If a specific fault is suspected, enter Play mode only once to chase it. Read the world through `script eval` (an element's `resolvedStyle` or `layout`, a `RectTransform.rect.size`) rather than adding logging, and take no screenshots.

If a check fails, go back and reread the doc pages for that feature, listed in the guide's docs table, to find out what you missed. Then fix the files and run the checks again.

## Final report

A short checklist:

- The framework you used, and why (named by the user, detected, or chosen).
- The files you created or changed, and the scene, with their paths as saved on disk.
- The doc pages you read.
- The tests you wrote, their results, and the capture folder.
- Any package you added, such as the UI Test Framework, so the user can remove it.
- Each phase 2 review pass: what you changed, and what the fresh captures showed.
- What the in-context capture showed, and any fault you chased in the final step.
- Anything left for the user, such as sound hooks to attach or values to tune.

## Worked example

Request: "Create a HUD with a health bar and an ammo counter." The project has no UI.

1. Step 1: `ProjectVersion.txt` says `6000.7.0b3`, so `<VERSION>` is `6000.7`. The project uses the Input System package only. The CLI reaches the Editor, and `console status` returns the cursor to read from later.
2. Step 2: the project has no `.uxml`, `.uss`, or Canvas, and the request names no framework, so give the pros and cons of UI Toolkit and uGUI for this HUD and let the user decide. The user picks UI Toolkit, so note that for the final report.
3. Step 3: read `references/uitk-skill.md`, then the Panel Renderer, USS properties reference, and USS transitions pages for `6000.7`.
4. Step 4, phase 1: create `Assets/UI/HUD/HUD.uxml`, `HUD.uss` (plain styles, colors and sizes as custom properties), and `HUDController.cs`. Reuse `Assets/UI/PanelSettings.asset`, and add a Panel Renderer to a `HUD` GameObject in the open scene, `Assets/Scenes/Main.unity`. Enable the UI Test Framework, and write `Assets/UI/HUD/Tests/HUDStates.cs` for four states (default, low health, empty magazine, kill) over dark and light backgrounds. The quick check shows every element laid out and nothing clipped.
5. Step 4, phase 2: choose a direction for the game, restyle through the custom properties, and draw SVG icons for health and ammo, then run the test again (pass 1). The fresh captures show the ammo count hard to read on the light background, so add a backing panel and run the test again (pass 2). Capture the Game view once over `Main.unity`.
6. Final step: `console status` reports no compile failure, `console get` since the Step 1 cursor shows no errors, and no fault is suspected, so stop.
7. Final report:

   ```
   - Framework: UI Toolkit (project had no UI; the user chose it after the pros and cons)
   - Files: Assets/UI/HUD/HUD.uxml, Assets/UI/HUD/HUD.uss, Assets/UI/HUD/HUDController.cs
   - Scene: Assets/Scenes/Main.unity (HUD GameObject with a Panel Renderer)
   - Docs read: Panel Renderer component, USS properties reference, USS transitions
   - Tests: Assets/UI/HUD/Tests/HUDStates.cs passes; 8 captures in Temp/UIStates/
   - Review pass 1: restyled the panels and added SVG icons; fresh captures showed the ammo count unreadable on the light background
   - Review pass 2: added a backing panel behind the ammo count; fresh captures read on both backgrounds
   - Added: com.unity.ui.test-framework (ships with the Editor; remove it in Package Manager if you don't want it)
   - For you: HUDController.SetHealth(int) and SetAmmo(int, int) take your gameplay values
   ```
