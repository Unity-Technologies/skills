# UI Toolkit guide

Understand, edit, and generate UI Toolkit UI: UXML layouts, USS styles, the scene component that displays them, and the C# that drives them.

Paths that start with `references/` resolve to the skill folder; paths that start with `Assets/` resolve to the Unity project. Replace `<VERSION>` in documentation URLs with the project's Editor version, for example `6000.7`. Read `references/uitk-testing.md` in a separate call (together they're longer than one shell command prints), and skip it for a screen with one or two states and no interactions (a compass, a timer, a single gauge), which doesn't use the test framework.

## Contents

- Work in few steps
- Docs to read first
- Understanding and editing
- Generation
- Scene setup
- USS
- UXML, events, and custom controls
- Assets and icons
- C#
- Console messages
- Checks

## Work in few steps

Every step re-reads the whole conversation, so the number of steps drives the cost of a run.

- **CLI:** these commands and flags cover a UI build with the `com.unity.pipeline` package 0.10 or later, so don't load the `unity-cli` skill or look up their help unless one fails:

  | Task | Command |
  |---|---|
  | Keep Play mode running unfocused | `editor set_auto_tick` and `editor.playmode set_run_in_background`, before every `editor.playmode start` |
  | Undo that after Play mode | `editor.playmode set_run_in_background --enabled false` and `editor set_auto_tick --enabled false`, after `editor.playmode stop` |
  | Compile | `script recompile`, then poll `script recompile_status` |
  | Run C# | `script eval --file <f>` |
  | Read the Console | `console status` for the `cursor` and counts, then `console get --since <cursor> --level error` for messages |
  | Play mode | `editor status`, `editor.playmode start`, `editor.playmode stop` |
  | Capture | `capture game_view --source screen --width <w> --height <h> --output-path Temp/UIReview/<name>.png` (project-relative; Unity doesn't import `Temp/`) |
  | Capture Editor UI | `capture editor_window --window <title or type> --output-path Temp/UIReview/<name>.png` (not in batch mode) |
  | Tests | `test run --async-tests true`, `test status` |
  | Packages | `package add --name <name> --confirm true`, `package status` |

  - Run each as `unity command <name> ...`. Parameter names use hyphens (`--async-tests`, `--output-path`, `--interval-ms`); underscores are rejected. Don't send its output to `/dev/null` or cut it by line position: the CLI can print a warning line before the header, so grep the output for the word you want (`completed`, `playing`, `error`).
  - Play mode: turn on auto tick and Run In Background before you enter Play mode. Without both, an unfocused Editor stays on frame 1. Run In Background ships in builds, so use the command, which records the project's value: after `editor.playmode stop`, `--enabled false` puts that value back. Turn auto tick off too, because it persists.
  - Game view capture: `--width` and `--height` default to 1280×720, and the capture is scaled to that size, so pass the Game view's own size or a non-16:9 view comes out stretched.
  - Recompile, which also imports new and changed files. Poll the recompile status: it says `triggered` or `compiling` until it's done, then `completed` (followed by any compile errors), `up_to_date` or `idle`.
  - Never run `console clear`: it also deletes the user's own Console entries. Older warnings stay in the Console and look current, so note the `cursor` from `console status` before a step and read only what came after it with `console get --since <cursor>`.
  - Eval (`script eval --file Temp/<name>.cs`) runs C# and prints its `return` value. The file has no `using` lines, so write fully qualified type names and call extension methods as static methods (`UnityEngine.UIElements.UQueryExtensions.Q(root, "name")`). Use `FindAnyObjectByType`; `FindFirstObjectByType` is deprecated.
  - Entering Play mode can report a network error while the domain reloads; poll the editor status until it shows `"playMode":"playing"`.
  - Test status says `running`, then `completed: <passed>/<total> passed`; `--format json` adds each failure's message.
  - After `package add`, poll the package status until it says `completed`; `idle` means the add never started, so check the add's output. Leave out `--wait`: the domain reload after the install drops the connection, and the command reports a network error even when the install works.
- **Review loop:** with the mock HUD test, copy the test script from `references/uitk-testing.md` and run it after each edit; it recompiles, reports compile errors, runs the test, and lists the captures in one step. Don't write your own. Without the test framework, and for the in-context capture, do each pass as one command with the capture script in `references/uitk-testing.md`.
- **Decide quickly:** pick names, layout, palette, and sample content without weighing alternatives at length; the review passes catch what needs to change.
- **Time-box detours:** if a detail (an open dropdown menu, a hover state, a capture artifact) still isn't confirmed after two attempts, stop, mention it in the final report, and move on; the review passes matter more than any one detail.
- **Batching:** write all the files of a step in one tool call, then compile once; run the Editor setup script and the first test run in one command; open all the captures of a pass in one message.
- **Docs:** fetch the Panel Renderer page and the USS properties reference, in one parallel batch; fetch other pages below only for a feature you use that this guide doesn't cover.
- **Edit in place:** in phase 2, change token values and add or edit selectors with targeted edits; rewriting a whole stylesheet or controller costs as much as writing it the first time.
- **Editor setup:** do the Panel Settings, theme, scene objects, and scene save in one `script eval` script. Test Unity objects with `if (!component)`, not `??` or `?.`: in the Editor a missing component is a placeholder object, so `GetComponent<T>() ?? AddComponent<T>()` never adds it.
- **Built-in controls:** read the `UssClassName` fields of all the controls you style in one `script eval`.

## Docs to read first

USS properties and components vary between Editor versions. Before writing a USS file, open the documentation page for the feature in the target `<VERSION>`. Entries that start with `references/` refer to files in this skill, not documentation pages.

| Building | Read |
|---|---|
| Runtime UI in a scene | [Panel Renderer component](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-support-for-runtime-ui/uie-render-runtime-ui/panel-renderer-component.md), [Panel Settings properties](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-support-for-runtime-ui/uie-render-runtime-ui/uie-runtime-panel-settings.md), [Theme Style Sheet](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-uss/uie-tss.md) |
| Styles | [USS properties reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-uss/properties/reference.md), [Custom properties](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-uss/variables/custom-properties.md) |
| Hover, press, and state animation | [USS transitions](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-uss/properties/uie-transitions.md) |
| Backgrounds, 9-slice, gradients | [Set background images](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-uss/properties/uib-styling-ui-backgrounds.md) |
| Shadows, blur, tint effects | [Built-in filters](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-uss/uss-filter/built-in-filters.md) |
| Built-in controls | [Visual elements reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-element-ref.md) |
| Text | [Work with text](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-work-with-text.md) |
| Input and callbacks | [Events](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-events.md), [Manipulators](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-events/uie-manipulators.md) |
| Drag and drop | [Create a drag-and-drop UI](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-events/uie-event-examples/uie-create-drag-and-drop-ui.md), then `references/uitk-pointermanipulator-guide.md` |
| Reusable controls | [Create a custom control](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-structure-ui/uie-custom-controls/uie-create-custom-controls.md), [Customize UXML attributes](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-structure-ui/uie-custom-controls/custom-control-customize-uxml-attributes.md) |
| Values that follow a data object | [Runtime data binding](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-data-binding/uie-runtime-binding.md), [Binding modes](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-data-binding/uie-runtime-binding/mode-update.md) |
| Rings, charts, custom shapes | `references/uitk-painter2d.md` |
| Mock HUD tests and tests for controls and interactions | `references/uitk-testing.md` |
| Icons | `references/uitk-svg-icons.md` |
| Look and feel | `references/ui-visual-design.md` |

## Understanding and editing

Explain UI structure as a tree:

```
[ElementType] name="elementName" class="class1 class2"
├── [ChildType] name="childName"
│   └── [GrandchildType]
└── [ChildType] class="another-class"
```

Edit only the USS selectors and UXML elements covered by the request. If you rename a name, update every C# query that uses it. Preserve existing formatting and references. Rewrite the entire file only when the changes affect most of its content, so users can review the diff.

## Generation

A screen, menu, panel, or HUD is `.uxml` + `.uss` + a controller script (`.cs`) for live values and interactions, in a saved scene; a "static layout" is just the UXML and USS. Read requests generously: "proper buttons" have hover, focus, and pressed states, "working UI" responds to input and shows live values, and an inventory or crafting screen is a slot grid with drag and drop (`references/uitk-pointermanipulator-guide.md`).

Build new UI in two phases, so the UI works before you spend time on its look.

**Phase 1: function.** Reuse the project's USS, UXML, Panel Settings, and theme files (search `Assets/` only; Panel Settings under `Packages/`, such as a render pipeline's debug window, belong to packages). Write the USS and UXML for the structure with plain styles, and define colors, sizes, and spacing once as USS custom properties so phase 2 restyles in one place. Write every file complete before the Editor imports it, because a half-written UXML is a parse error. Then set up the scene, write the controller (C# section), and test it (Checks 1–4).

**Phase 2: theme and quality.** Theme with `references/ui-visual-design.md`: direction, palette, type scale, depth, interactive states, motion, and SVG icons (`references/uitk-svg-icons.md`); Painter2D (`references/uitk-painter2d.md`) for rings and charts, USS transitions for state changes. Style the child parts of composite controls too (a Slider's tracker and dragger, a Toggle's checkmark, a ScrollView scroller's tracker and dragger, and hide its Editor-style arrow buttons), because the default theme styles them separately. Review (Check 5), then check the screen over the real scene (Check 6).

Apply exact values the user gives (hex colors, pixel sizes, spacing) as given, in either phase.

## Scene setup

1. Reuse an existing Panel Settings asset, because panels that share one also share focus navigation and sort order. If there is none, create one in the Editor setup script with `ScriptableObject.CreateInstance<PanelSettings>()` and save it with `AssetDatabase.CreateAsset(settings, "Assets/UI/PanelSettings.asset")`.
2. Give it a theme in `themeStyleSheet`: a `.tss` from the project, or a new one containing `@import url("unity-theme://default");`. Built-in controls render unstyled without a theme.
3. Add a Panel Renderer to a GameObject in the scene and set its Panel Settings and UXML through `SerializedObject` (fields `m_PanelSettings` and `sourceAsset`), because the C# properties set from a script don't always reach the saved scene. UI Document isn't the default for new UI, so use it only when editing a scene that already has one.
4. Save an untitled scene with an explicit path, for example `EditorSceneManager.SaveScene(scene, "Assets/Scenes/HUDDemo.unity")`, because saving it without a path opens a modal Save As dialog that blocks the Editor.
5. Save the scene, and confirm both references in the saved `.unity` file.

Editor UI (EditorWindow, PropertyDrawer) needs no Panel Settings or scene.

## USS

USS is a subset of CSS that changes between Editor versions, so don't write a property or selector from CSS memory. Check it for `<VERSION>` first: properties in the [USS properties reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-uss/properties/reference.md), selectors on the [USS selectors](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-uss/selectors.md) page, and gradients on the Set background images page. The importer drops what it doesn't know with only a warning, so a missing feature shows up as a style that silently does nothing. When a feature isn't listed, use UXML order or nesting for stacking, child margins for spacing, explicit classes for structural selectors, and Painter2D (`references/uitk-painter2d.md`) for gradients.

- **CSS features USS doesn't have:** no `border` shorthand (use `border-width` and `border-color`), `outline`, `pointer-events` (use `picking-mode="Ignore"` in UXML), `box-shadow` (use a nested element behind the panel), or structural pseudo-classes such as `:nth-child` and `:first-child` (use explicit classes).
- **Tint:** `-unity-background-image-tint-color` multiplies the image's colors (alpha included), so it can't brighten an image: for a hover highlight, tint the resting state below white.
- **Wrapping:** a Label grows to fit its text, so long text needs a constrained width and `white-space: normal` to wrap.
- **Theme imports:** write a `.uss` before the `.tss` that imports it. A theme imported before its stylesheet exists drops the import silently, and the custom properties stay unresolved until the `.tss` is reimported (`AssetDatabase.ImportAsset` with `ImportAssetOptions.ForceUpdate`).
- **Images in UXML:** an image attribute (a Tab's `icon-image`, an Image's `image`) needs the full asset reference, `project://database/<path>?fileID=<id>&amp;guid=<guid>&amp;type=3#<name>`, with the IDs from `AssetDatabase.TryGetGUIDAndLocalFileIdentifier`; a bare path doesn't load. USS `url(...)` takes a bare path.
- **Where styles live:** in USS classes, so themes and states can override them; inline `style="..."` only for one-off values. Asset URLs are `url("project://database/Assets/...")`; web URLs don't load.
- **Layout:** prefer `flex-grow` and percentages, with fixed pixel sizes for icons, bars, and fixed panels. Skip default values.
- **Field inputs:** a long label squeezes a built-in field's input (`.unity-base-field__input`) to nothing, and the default theme's flex settings override `width`. Size inputs with `flex-basis`, `flex-grow: 0`, and `flex-shrink: 0`.
- **Debugging:** debug style and layout issues by querying elements and reading their `resolvedStyle` properties through `script eval` in Play mode, because it shows the values after every stylesheet and inline style has applied.

## UXML, events, and custom controls

- Declare `<ui:UXML xmlns:ui="UnityEngine.UIElements">` (Editor UI adds `xmlns:uie="UnityEditor.UIElements"`), link the stylesheet with `<ui:Style src="Screen.uss" />`, and put the screen under one root `VisualElement` with a class, so one selector can theme, hide, or animate the whole screen.
- A custom control's `xmlns` prefix maps to its exact C# namespace, with matching case and no assembly or class name: `xmlns:custom="Game.UI.Custom"`.
- UXML can't attach handlers. Use `button.clicked += OnPlay;` or `RegisterCallback<ClickEvent>` (and other events) in the controller, and a `PointerManipulator` subclass attached with `element.AddManipulator(...)` for reusable interactions such as drag and drop (`references/uitk-pointermanipulator-guide.md`).
- **Custom controls:** `[UxmlElement]` on a `partial` class, `[UxmlAttribute]` on its properties, for elements that appear on several screens.
- **Data binding:** bind when many values follow one data object, such as a character sheet; for a few HUD values, set properties in the controller. Binding paths are relative to the data source (no type-name prefix), and display-only values need the `ToTarget` mode.

## Assets and icons

Reuse existing project icons first, then generate SVG icons, and use an image generator only for icons too detailed for SVG paths. Reference images in USS with `background-image: url("project://database/Assets/UI/Textures/icon.png");`. Import SVGs as `references/uitk-svg-icons.md` describes, because the default import crops each icon to its drawn shape.

## C#

- **Getting elements (Panel Renderer):** register the versioned UI reload callback, unregister it on disable or destroy, and query elements inside it, as the Panel Renderer pages show. The tree is rebuilt on every reload, so references taken elsewhere go stale. Store the callback's `version` and return early when it hasn't changed, because the callback also runs on register and on re-enable, and subscribing to button events again makes each click fire twice.
- **Reaching the UI from scripts:** the Panel Renderer has no root property (UI Document has `rootVisualElement`), so keep the callback's root in a public property on the controller (for example `Root`), for Play-mode `script eval` scripts and tests.
- **Getting elements (UI Document):** query `rootVisualElement` in `OnEnable`.
- **Styling:** `AddToClassList()` for anything a stylesheet should override; `element.style.*` for per-frame animation and computed values, because inline styles beat USS selectors.
- **Other:** fonts are TextCore `FontAsset` and `PanelTextSettings` (TextMesh Pro assets belong to uGUI). Build EditorWindows in `CreateGUI()`, in an `Editor` folder or Editor-only assembly. Place scripts next to their UXML and USS unless the project keeps them elsewhere.

## Console messages

| Console message | Cause | Fix |
|---|---|---|
| Warning `UAL0010` or `UAL0013` on a UI script | The type has a static field, static auto-property, or static event | Use `const` and instance fields, or mark a type that needs static read-only data `[NoAutoStaticsCleanup]` (namespace `Unity.Scripting.LifecycleManagement`) |

## Checks

1. After the files compile, the Console has no UXML import errors or USS warnings about unknown properties or selectors.
2. Enable the UI Test Framework if the project doesn't have it (`references/uitk-testing.md`), except for a screen with one or two states and no interactions.
3. Run the mock HUD test (`references/uitk-testing.md`) and check its captures quickly for missing, clipped, overlapping, or unreadable elements. Write Edit Mode tests only for a reusable control.
4. The saved scene's Panel Renderer (or UI Document) references the Panel Settings and the UXML.
5. Two review passes: in each, make a theme change, rerun the mock HUD test, and review the fresh captures against the checklist in `references/ui-visual-design.md` (phase 1 and in-context captures don't count). A third pass only for a defect a capture shows.
6. Capture the Game view in Play mode once, over the real scene, with the capture script in `references/uitk-testing.md` (one command). If Play mode stays on frame 1 after a second try, skip the capture and say so in the final report instead of debugging Play mode.

Fallback: without the UI Test Framework, enter Play mode and read through `script eval`, for each named element, whether `root.Q(name)` finds it and its `layout` size, instead of adding logging to the controller. Review Game view captures instead of the mock HUD test.

If a check fails, reread the doc page for that feature before changing the files again.
