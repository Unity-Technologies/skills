# Test UI Toolkit UI with the UI Test Framework

The UI Test Framework package (`com.unity.ui.test-framework`) runs UI Toolkit UI in Test Runner tests and simulates input. The **mock HUD test** (Play Mode) sets a screen to each state through the controller, checks its interactions, asserts its layout, and captures it: the quick visual check in the function phase and the review loop in the theme phase, faster and cheaper than playing the game to each state. Write **Edit Mode tests** only for a reusable custom control.

Skip the framework for a screen with one or two states and no interactions (a compass, a timer, a single gauge): set each state in Play mode with `script eval` and review Game view captures, because the install, assemblies, and runs cost more than they save. If setup still fails after "Avoid getting stuck", verify by reading element sizes through `script eval` in Play mode and a Game view capture, and say so in the final report.

Paths that start with `Assets/`, `Library/`, `Packages/`, or `Temp/` resolve to the Unity project; paths that start with `references/` resolve to the skill folder.

## Contents

- Enable the package
- Set up the test assemblies
- Mock HUD test
- Avoid getting stuck
- Edit Mode tests
- Run the tests
- Capture in Play mode

## Enable the package

If `Packages/manifest.json` doesn't list it, run `unity command package add --name com.unity.ui.test-framework --confirm true` (no version, so Package Manager picks the one that ships with the Editor; no `--wait`, because the domain reload after the install drops the connection), poll `package status` until `completed`, then `script recompile_status`. It ships with the Editor, so adding it downloads nothing. Name the package in the final report so the user can remove it. This file has what a mock HUD test needs; read the package docs only if a step fails, and then read only the `.md` files in `Library/PackageCache/com.unity.ui.test-framework@*/Documentation~/`, nothing else in that package.

| When this step fails | Read in `Documentation~/` |
|---|---|
| Installing or the test assembly setup | `install-and-set-up.md`, `create-your-first-ui-test.md` |
| Choosing a fixture for Play Mode or runtime UI | `test-fixtures/test-ui-in-runtime.md`, `test-fixtures/introduction-to-test-fixtures.md` |
| Edit Mode tests of a custom control | `test-fixtures/test-ui-without-editor-window-dependencies.md` |
| UI not updating between steps | `test-fixtures/trigger-and-update-ui.md`, `simulate/wait-for-next-ui-frame.md` |
| Clicks, hovers and drags | `simulate/click-on-a-visual-element.md`, `simulate/move-mouse-to-a-visual-element.md`, `simulate/drag-an-element-from-one-place-to-another.md` |
| Keyboard and text input | `simulate/press-keys.md`, `simulate/press-tab-or-return.md`, `simulate/type-into-a-text-field.md` |
| Scrolling | `simulate/scroll-in-a-scrollview.md` |
| Yielding across frames | `test-with-coroutines.md` |
| Fixture state leaking between tests | `test-fixtures/debug-ui-test-fixtures.md`, `test-fixtures/ui-test-component/clean-up-objects-after-tests.md` |

## Set up the test assemblies

1. Put the scripts under test (the controller, custom controls) in their own assembly definition, because a test assembly can't reference Assembly-CSharp. That new assembly can't reference Assembly-CSharp either, so move the scripts it depends on into it too, and reference the packages they use (for example `Unity.InputSystem`).
2. Write the Play Mode test assembly's `.asmdef` in a `Tests` folder next to the UI, together with the UI's `.asmdef` and before any test script: references to `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `Unity.UI.TestFramework.Runtime`, and the UI's assembly; `nunit.framework.dll` as a precompiled reference with `overrideReferences` on; and the `UNITY_INCLUDE_TESTS` define constraint.
3. For a reusable control's Edit Mode tests, add a second assembly set to the Editor platform, with the same settings plus `Unity.UI.TestFramework.Editor`.

## Mock HUD test

1. **States:** 3–4 of the default, the extremes (lowest health, maximum values, the longest names), brief feedback (a hit marker), and selected, disabled, or locked states.
2. **Testable controller:** a public method per state, and a `ResetState()` that resets every value, effect, timer, and flag, because effects otherwise carry into the next capture. Demo gameplay and input live in a separate script.
3. **The test**, from the skeleton below, builds the screen with the project's real Panel Settings (so layout and scaling match the game), checks an interaction with simulated input, and per state resets, sets the state, captures to `Temp/UIStates/<state>-<background>.png` (outside `Assets/`), logs one `[UI check]` line of element sizes, and asserts that elements meant to have a size have one (not containers that only position children, such as a crosshair anchor).
4. **Backgrounds:** a HUD over the game gets dark and light, because each exposes text that reads on only one; a screen with its own background (a menu, settings, a skill tree) needs only dark.
5. **Simulated time:** USS transitions and scheduled callbacks advance only with `simulate.FrameUpdate()` (200 ms each by default), not real time, so the skeleton runs five frames per state; after one frame, colors are mid-transition, which isn't a styling bug.
6. **Function phase:** look at the captures only for what's broken, fix it, and move on.
7. **Theme phase:** after each theme change, rerun and review the captures of the states it affects against `references/ui-visual-design.md`: two passes, a third only for a defect.

For an existing screen that still uses a UI Document, call `SetUIContent(document)` in place of `SetPanelRenderer`. Rename the controller, asset paths, states, element names, and the interaction:

```csharp
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

public class HUDStatesTest : RuntimeUITestFixture
{
    Camera m_Camera;
    HUDController m_Hud;

    [UnityTest]
    public IEnumerator States()
    {
        m_Camera = new GameObject("Background").AddComponent<Camera>();
        m_Camera.clearFlags = CameraClearFlags.SolidColor;
        var go = new GameObject("HUD");
        go.SetActive(false);
        var panel = go.AddComponent<PanelRenderer>();
#if UNITY_EDITOR
        panel.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
        panel.visualTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/HUD/HUD.uxml");
#endif
        m_Hud = go.AddComponent<HUDController>();
        go.SetActive(true);
        yield return null;
        SetPanelRenderer(panel);
        simulate.FrameUpdate();

        simulate.Click(rootVisualElement.Q<Button>("reload-button"));
        simulate.FrameUpdate();
        Assert.That(m_Hud.Ammo, Is.EqualTo(30));

        yield return Capture("low-health", () => m_Hud.SetHealth(10), "health-bar", "ammo-label");
        Object.Destroy(go);
        Object.Destroy(m_Camera.gameObject);
    }

    IEnumerator Capture(string state, System.Action setState, params string[] sized)
    {
        foreach (var (background, color) in new[] { ("dark", new Color(0.06f, 0.06f, 0.06f)), ("light", new Color(0.96f, 0.96f, 0.96f)) })
        {
            m_Camera.backgroundColor = color;
            m_Hud.ResetState();
            setState();
            for (var frame = 0; frame < 5; frame++)
                simulate.FrameUpdate(); // 200 ms each, so USS transitions finish before the capture
            yield return new WaitForEndOfFrame();
            var log = $"[UI check] {state}:";
            foreach (var name in sized)
            {
                var element = rootVisualElement.Q(name);
                Assert.That(element, Is.Not.Null, name);
                Assert.That(element.layout.width > 0 && element.layout.height > 0, $"{name} has no size");
                log += $" {name} {element.layout.width:0}x{element.layout.height:0}";
            }
            Debug.Log(log);
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            // 1024 px wide shows layout and color at about half the tokens to review.
            var width = Mathf.Min(1024, shot.width);
            var height = shot.height * width / shot.width;
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(shot, target);
            RenderTexture.active = target;
            var small = new Texture2D(width, height, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            Directory.CreateDirectory("Temp/UIStates");
            File.WriteAllBytes($"Temp/UIStates/{state}-{background}.png", small.EncodeToPNG());
            Object.Destroy(shot);
            Object.Destroy(small);
        }
    }
}
```

## Avoid getting stuck

| Symptom | Cause | Fix |
|---|---|---|
| The test assembly "has no scripts associated with it", though its folder has one | The script was imported before the `.asmdef` | Delete the script and write it again |
| The test status says `running` for minutes | Compilation failed, so the run never started | Fix the errors that the recompile status lists after `completed` |
| A "save your changes" dialog blocks the Editor | The open scene has unsaved changes | Save the open scenes before every test run |
| A layout assert fails on an empty bar | A fill is zero wide at an empty value | Assert on the bar's track |
| Brief feedback never shows in a capture | The controller's timer runs on game time | Give the controller a way to hold feedback on screen, and set it in the test |

## Edit Mode tests

For a reusable custom control, use `UITestFixture` (an empty, unrendered panel) or `EditorWindowUITestFixture<TWindow>` in the Editor test assembly. Add the control to `rootVisualElement`, call `simulate.FrameUpdate()` before each query and assert, simulate input (`Click`, `DoubleClick`, `MouseMove`, `DragAndDrop(from, to)` with `worldBound.center` positions, `TypingText` after focusing, `KeyPress`, `ScrollWheel`), and assert on values, `ClassListContains(...)`, `enabledSelf`, or `resolvedStyle`.

## Run the tests

1. Copy this script to `Temp/run-ui-tests.sh` once, and run `bash Temp/run-ui-tests.sh <TestClassName>` after every edit instead of separate compile, test and console checks. Don't write your own or change how it parses output. It saves the open scenes (a "save your changes" dialog would block the run), recompiles, stops on compile errors, runs only the UI's test class (the project's other Play Mode tests would slow the loop and add failures that aren't the UI's), and prints the result, failure messages, and the `[UI check]` lines, Console errors and capture names from this run only. A run takes 15–30 seconds.

```bash
#!/bin/bash
# Usage: bash Temp/run-ui-tests.sh <TestClassName> [playmode|editmode]
cd "$(dirname "$0")/.." || exit 1
u() { unity command "$@" 2>&1; }
printf '%s\n' 'UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();' 'return "scenes saved";' > Temp/save-scenes.cs
u script eval --file Temp/save-scenes.cs | grep -q "scenes saved" || echo "warning: couldn't save the open scenes"
# Read only what this run logs: console clear would also delete the user's own entries.
c=$(u console status | grep -oE '"cursor": *[0-9]+' | grep -oE '[0-9]+$')
log() { if [ -n "$c" ]; then u console get --since "$c" "$@"; else u console get --tail 60 "$@"; fi; }
u script recompile > /dev/null
for i in $(seq 1 60); do s=$(u script recompile_status); echo "$s" | grep -qE 'triggered|compiling' || break; sleep 2; done
if echo "$s" | grep -qE 'error CS|"failed": *true|compilationFailed"?: *true'; then
  echo "COMPILE ERRORS:"; echo "$s" | grep -oE '[^"]*error CS[^"]*' | head -10; exit 1
fi
rm -rf Temp/UIStates
u test run --mode "${2:-playmode}" --filter "$1" --async-tests true > /dev/null
sleep 3
for i in $(seq 1 20); do
  t=$(u test status --format json | tr '\n' ' ')
  echo "$t" | grep -qE '"status": *"(completed|no_tests)"' && break; sleep 3
done
echo "$t" | grep -oE '"status": *"[a-z_]+"|"(total|passed|failed)": *[0-9]+' | tr -d '" ' | paste -sd' ' -
echo "$t" | grep -qE '"status": *"running"' && echo "still running after 60 s: check the Console errors below"
echo "$t" | grep -oE '"message": *"[^"]*' | cut -c1-300 | head -5
log | grep -oE '\[UI check\][^"]*' | cut -c1-300
log --level error | grep -oE '"message": *"[^"]*' | cut -c1-300 | head -5
ls Temp/UIStates 2>/dev/null || echo "no captures in Temp/UIStates"
```

2. Edit Mode tests: `bash Temp/run-ui-tests.sh <ClassName> editmode`.
3. To find out why something looks wrong, read the element's `resolvedStyle` through eval in Play mode rather than adding logs and rerunning.
4. Keep the tests in the project, and list them, their results, and the capture folder in the final report.

## Capture in Play mode

For the in-context capture, and for review passes without the test framework, copy this script to `Temp/capture-in-play.sh` and run it as one command: `bash Temp/capture-in-play.sh ingame [state ...]`. The first name captures the scene as it starts; for each further name, write `Temp/state-<name>.cs` (a C# file for `script eval` that sets that state through the controller and returns a short string) and the script runs it and captures `<name>`. It turns on auto tick and Run In Background (without it an unfocused Editor stays on frame 1) and checks that Play mode got past frame 1. If it's still on frame 1 it prints `FROZEN` and stops. Then skip the capture, mention it in the final report, and don't debug Play mode. When it stops Play mode, it puts the project's Run In Background value back and turns auto tick off. Captures are 1280×720; if the Game view isn't 16:9, set `GAME_W` and `GAME_H` to its size so they aren't stretched.

```bash
#!/bin/bash
# Usage: bash Temp/capture-in-play.sh <first-name> [state-name ...]
cd "$(dirname "$0")/.." || exit 1
u() { unity command "$@" 2>&1; }
CAP() { mkdir -p Temp/UIReview; u capture game_view --source screen --width "${GAME_W:-1280}" --height "${GAME_H:-720}" --output-path "Temp/UIReview/$1.png" | grep -oE 'savedPath"?: *"?[^",}]*'; }
printf '%s\n' 'return "frame=" + UnityEngine.Time.frameCount;' > Temp/frame.cs
frame() { u script eval --file Temp/frame.cs | grep -oE 'frame=[0-9]+' | cut -d= -f2; }
# Stop Play mode, put back the project's Run In Background value, and turn auto tick off: both persist.
finish() { u editor.playmode stop > /dev/null; u editor.playmode set_run_in_background --enabled false > /dev/null; u editor set_auto_tick --enabled false > /dev/null; }
u editor set_auto_tick > /dev/null
# Without Run In Background an unfocused Editor stays on frame 1, even with auto tick.
u editor.playmode set_run_in_background > /dev/null
u editor.playmode start > /dev/null
for i in $(seq 1 30); do u editor status | grep -qE 'playMode"?: *"?playing' && break; sleep 1; done
for i in $(seq 1 8); do sleep 1; f=$(frame); [ "${f:-0}" -gt 1 ] && break; done
if [ "${f:-0}" -le 1 ]; then echo "FROZEN at frame ${f:-?}"; finish; exit 2; fi
sleep 2; echo "playing, frame $(frame)"
CAP "$1"
shift
for name in "$@"; do
  u script eval --file "Temp/state-$name.cs" | grep -E '^result:|"result"' | cut -c1-200
  sleep 1; CAP "$name"
done
u console status | grep -oE '"consoleErrors": *[0-9]+'
finish
```
