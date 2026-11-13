# Rig Setup — Eval Recipes

All recipes use fully qualified types (no `using` directives) so they can be passed to `unity command eval --code '...'` as-is. Replace `<...>` placeholders before running.

## Table of Contents

- [Read or set Active Input Handling](#read-or-set-active-input-handling)
- [Import a package sample](#import-a-package-sample)
- [Import TMP Essentials](#import-tmp-essentials)
- [Find the rig prefab](#find-the-rig-prefab)
- [Check the template Main Camera](#check-the-template-main-camera)
- [Set component fields (fallback)](#set-component-fields-fallback)
- [Name interaction layer 31 Teleport](#name-interaction-layer-31-teleport)
- [Turn on the XR Interaction Simulator](#turn-on-the-xr-interaction-simulator)
- [Add example content](#add-example-content)
- [Rig read-back](#rig-read-back)

## Read or set Active Input Handling

`activeInputHandler`: 0 = Input Manager (old), 1 = Input System, 2 = Both. A change takes effect after an Editor restart. To set, replace `<value>`; to read only, remove the two lines that write.

```csharp
var ps = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
var so = new UnityEditor.SerializedObject(ps);
var p = so.FindProperty("activeInputHandler");
if (p == null) return "activeInputHandler not found";
var before = p.intValue;
p.intValue = <value>; so.ApplyModifiedProperties(); UnityEditor.AssetDatabase.SaveAssets();
return "activeInputHandler " + before + " -> " + p.intValue;
```

## Import a package sample

Replace `<package>` (e.g., `com.unity.xr.interaction.toolkit`, `com.unity.xr.hands`) and `<sample>` with the exact display name (`Starter Assets`, `Hands Interaction Demo`, `HandVisualizer`). Import triggers a recompile — wait for the Editor to respond before continuing.

```csharp
var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/<package>");
if (info == null) return "<package> is not installed";
foreach (var s in UnityEditor.PackageManager.UI.Sample.FindByPackage(info.name, info.version)) {
    if (s.displayName != "<sample>") continue;
    if (s.isImported) return "<sample> already imported for " + info.version;
    var ok = s.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.HideImportWindow | UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
    return "<sample> imported=" + ok + " (" + info.version + ")";
}
return "<sample> not found in " + info.name + " " + info.version;
```

`isImported` is only true for the installed package version. If an older version's sample folder exists under `Assets/Samples/<package display name>/`, ask the user before importing — `OverridePreviousImports` replaces it.

## Import TMP Essentials

Starter Assets requires them. Non-interactive (no importer window):

```csharp
if (UnityEditor.AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return "TMP Essentials already present";
TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
return "TMP Essentials import requested";
```

The import finishes asynchronously; check that `Assets/TextMesh Pro` exists before continuing.

## Find the rig prefab

Replace `<prefab>` with `XR Origin (XR Rig)` or `XR Origin Hands (XR Rig)`:

```csharp
var r = "";
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab \"<prefab>\"", new[]{ "Assets/Samples" })) {
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    if (System.IO.Path.GetFileNameWithoutExtension(path) == "<prefab>") r += path + "\n";
}
return r == "" ? "not found" : r;
```

If more than one path is returned, use the one whose folder matches the installed package version.

## Check the template Main Camera

Targets the scene's root `Main Camera`, not the rig's child camera:

```csharp
UnityEngine.GameObject cam = null;
foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
    if (go.name == "Main Camera") cam = go;
if (cam == null) return "no root Main Camera";
var names = "";
foreach (var c in cam.GetComponents<UnityEngine.Component>()) names += c.GetType().Name + ",";
var template = cam.transform.childCount == 0 && names == "Transform,Camera,AudioListener,UniversalAdditionalCameraData,";
return "active=" + cam.activeSelf + " template=" + template + " components=" + names + " children=" + cam.transform.childCount;
```

## Set component fields (fallback)

Use only if the Pipeline catalog has no command for setting component properties. Replace `<hierarchy path>`, `<component type name>`, and the assignments. Records a prefab override, like editing in the Inspector.

```csharp
var go = UnityEngine.GameObject.Find("<hierarchy path>");
if (go == null) return "not found: <hierarchy path>";
UnityEngine.Component comp = null;
foreach (var c in go.GetComponents<UnityEngine.Component>()) if (c != null && c.GetType().Name == "<component type name>") comp = c;
if (comp == null) return "no <component type name> on <hierarchy path>";
var so = new UnityEditor.SerializedObject(comp);
var p = so.FindProperty("m_SmoothMotionEnabled");      // example
if (p == null) return "field not found";
p.boolValue = false;                                   // example; use objectReferenceValue = null to clear a reference
so.ApplyModifiedProperties();
return "set";
```

`GameObject.Find` with a path only finds active objects. Save the scene afterwards.

Use this recipe for nested fields too if the Pipeline command does not accept them: `FindProperty` takes a dotted path, e.g. `so.FindProperty("m_LeftHandMoveInput.m_InputSourceMode")` with `p.intValue = 0;` (Unused).

## Name interaction layer 31 Teleport

The Starter Assets teleport setup expects interaction layer 31 to be named `Teleport`. Only needed when teleport is on. Edits the XR Interaction Toolkit's interaction layer settings asset, as the Project Settings window does:

```csharp
var guids = UnityEditor.AssetDatabase.FindAssets("t:InteractionLayerSettings");
if (guids.Length == 0) return "InteractionLayerSettings asset not found";
var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
var so = new UnityEditor.SerializedObject(asset);
var names = so.FindProperty("m_LayerNames");
if (names == null || names.arraySize < 32) return "layer names not found";
var layer = names.GetArrayElementAtIndex(31);
if (layer.stringValue == "Teleport") return "already Teleport";
if (!string.IsNullOrEmpty(layer.stringValue)) return "layer 31 is already named '" + layer.stringValue + "' - ask the user";
layer.stringValue = "Teleport";
so.ApplyModifiedProperties();
UnityEditor.AssetDatabase.SaveAssets();
return "layer 31 named Teleport";
```

## Turn on the XR Interaction Simulator

Only when the user asks for the simulator, after importing the **XR Interaction Simulator** sample. Sets XRI's simulator settings (Project Settings > XR Plug-in Management > XR Interaction Toolkit). The settings class is internal, so this edits its asset:

```csharp
var s = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset");
if (s == null) return "XRDeviceSimulatorSettings.asset not found (XRI settings not created yet)";
string prefab = null;
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab \"XR Interaction Simulator\"", new[]{ "Assets/Samples" })) {
    var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    if (System.IO.Path.GetFileNameWithoutExtension(p) == "XR Interaction Simulator") prefab = p;
}
if (prefab == null) return "XR Interaction Simulator prefab not found: import the sample first";
var so = new UnityEditor.SerializedObject(s);
var on = so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab");
var editorOnly = so.FindProperty("m_AutomaticallyInstantiateInEditorOnly");
var classic = so.FindProperty("m_UseClassic");
var pf = so.FindProperty("m_SimulatorPrefab");
if (on == null || editorOnly == null || classic == null || pf == null) return "simulator settings fields not found (XRI version changed?)";
on.boolValue = true; editorOnly.boolValue = true; classic.boolValue = false;
pf.objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefab);
so.ApplyModifiedProperties(); UnityEditor.EditorUtility.SetDirty(s); UnityEditor.AssetDatabase.SaveAssets();
return "simulator on in Editor Play mode only, prefab " + prefab;
```

If more than one prefab path exists, use the one whose folder matches the installed XRI version.

## Add example content

Only after placing a rig, and only if the user says yes. First check that the scene has nothing but the template objects and the rig:

```csharp
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var template = new[]{ "Main Camera", "Directional Light", "Global Volume", "EventSystem", "XR Interaction Manager" };
var other = "";
foreach (var g in scene.GetRootGameObjects())
    if (g.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true) == null && System.Array.IndexOf(template, g.name) < 0) other += g.name + "; ";
return other == "" ? "empty" : "has content: " + other;
```

If it returns `empty` and the user agrees, add the Starter Assets demo pieces: a floor to teleport and move on, objects to grab, and a button to poke:

```csharp
var inv = System.Globalization.CultureInfo.InvariantCulture;
var items = new[]{ "Teleport Area|0,-0.125,0|0,0,0|2,1,4", "Interactables Sample|0,0.25,-0.9|0,0,0|1,1,1", "Far Grab Samples|0,0,0|0,180,0|1,1,1", "Push Button|0.15,0.95,0.45|0,0,0|1,1,1" };
var r = "";
foreach (var it in items) {
    var p = it.Split((char)124); string path = null;
    foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab \"" + p[0] + "\"", new[]{ "Assets/Samples" })) {
        var a = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
        if (System.IO.Path.GetFileNameWithoutExtension(a) == p[0] && a.Contains("/Starter Assets/DemoAssets/")) path = a;
    }
    if (path == null) { r += "missing " + p[0] + "; "; continue; }
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path));
    var t = p[1].Split((char)44); var e = p[2].Split((char)44); var s = p[3].Split((char)44);
    o.transform.position = new UnityEngine.Vector3(float.Parse(t[0], inv), float.Parse(t[1], inv), float.Parse(t[2], inv));
    o.transform.eulerAngles = new UnityEngine.Vector3(float.Parse(e[0], inv), float.Parse(e[1], inv), float.Parse(e[2], inv));
    o.transform.localScale = new UnityEngine.Vector3(float.Parse(s[0], inv), float.Parse(s[1], inv), float.Parse(s[2], inv));
    r += "added " + o.name + "; ";
}
var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return r + "saved";
```

`Teleport Area` is on the Teleport interaction layer (31), which the rig's teleport ray uses. Each piece is a linked prefab instance the user can delete.

## Rig read-back

Read-only. Compare the output against the user's answers.

```csharp
Unity.XR.CoreUtils.XROrigin origin = null; int active = 0, inactive = 0;
foreach (var o in UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
    if (o.gameObject.activeInHierarchy) { origin = o; active++; } else inactive++;
if (origin == null) return "No active XR Origin in the open scene" + (inactive > 0 ? " (" + inactive + " inactive)" : "");
if (active > 1) return active + " active XR Origins in the scene; only one rig may be active";
var r = "rig=" + origin.name + " link=" + UnityEditor.PrefabUtility.GetPrefabInstanceStatus(origin.gameObject) + (inactive > 0 ? " (+" + inactive + " inactive)" : "") + "\n";
var cams = 0; foreach (var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsSortMode.None)) if (c.isActiveAndEnabled) cams++;
var ears = 0; foreach (var a in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>(UnityEngine.FindObjectsSortMode.None)) if (a.isActiveAndEnabled) ears++;
r += "activeCameras=" + cams + " activeListeners=" + ears + "\n";
foreach (var c in origin.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) {
    if (c == null) continue;
    var t = c.GetType().Name;
    if (t == "ControllerInputActionManager") {
        var so = new UnityEditor.SerializedObject(c);
        var sm = so.FindProperty("m_SmoothMotionEnabled"); var st = so.FindProperty("m_SmoothTurnEnabled");
        var tm = so.FindProperty("m_TeleportMode"); var tc = so.FindProperty("m_TeleportModeCancel");
        r += c.name + ": smoothMotion=" + (sm == null ? "?" : sm.boolValue.ToString())
           + " smoothTurn=" + (st == null ? "?" : st.boolValue.ToString())
           + " teleportMode=" + (tm == null ? "?" : (tm.objectReferenceValue != null ? "set" : "empty"))
           + " teleportCancel=" + (tc == null ? "?" : (tc.objectReferenceValue != null ? "set" : "empty")) + "\n";
    }
    else if (t == "NearFarInteractor") {
        var so = new UnityEditor.SerializedObject(c);
        var n = so.FindProperty("m_EnableNearCasting"); var f = so.FindProperty("m_EnableFarCasting");
        r += "  NearFar @" + c.transform.parent.name + " active=" + c.gameObject.activeInHierarchy + " near=" + (n == null ? "?" : n.boolValue.ToString()) + " far=" + (f == null ? "?" : f.boolValue.ToString()) + "\n";
    }
    else if (t == "XRPokeInteractor" || t.EndsWith("Provider") && !t.Contains("Affordance"))
        r += "  " + t + " @" + c.name + " " + (c.enabled && c.gameObject.activeInHierarchy ? "on" : "off") + "\n";
}
return r;
```

A `?` means the field was not found — the XRI version may have renamed it; report it rather than guessing.
