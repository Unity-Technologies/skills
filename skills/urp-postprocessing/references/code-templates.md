## Code Templates

Each template is a snippet to run through the Editor's `eval` command. Hard stops `throw`,
so the eval fails loudly; anything the caller needs to read is `return`ed.

**`eval` compiles a statement block, not a file.** `using` directives are rejected there — the
compiler reads `using UnityEngine;` as a resource-disposal statement and fails. So these
templates fully qualify every type. If you instead save the code as a `.cs` file for the user
to keep, add the usings back and drop the qualification.

### Creating a Global Volume with Effects

```csharp
// Verify HDR is enabled on the URP Asset
var urpAsset = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
if (urpAsset == null) { throw new System.Exception("No UniversalRenderPipelineAsset active."); }
if (!urpAsset.supportsHDR) { throw new System.Exception("HDR is disabled on the URP Asset. Enable it for Bloom/Tonemapping."); }

var profile = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
var assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/Settings/PostProcessProfile.asset");
UnityEditor.AssetDatabase.CreateAsset(profile, assetPath);

// MUST set overrideState = true on each property
var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>();
bloom.threshold.overrideState = true;
bloom.threshold.value = 0.9f;
bloom.intensity.overrideState = true;
bloom.intensity.value = 1f;
bloom.scatter.overrideState = true;
bloom.scatter.value = 0.7f;

var tonemapping = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>();
tonemapping.mode.overrideState = true;
tonemapping.mode.value = UnityEngine.Rendering.Universal.TonemappingMode.ACES;

var volumeObj = new UnityEngine.GameObject("Global Volume");
var volume = volumeObj.AddComponent<UnityEngine.Rendering.Volume>();
volume.isGlobal = true;
volume.profile = profile;

UnityEditor.Undo.RegisterCreatedObjectUndo(volumeObj, "Create Global Volume");
UnityEditor.EditorUtility.SetDirty(profile);
UnityEditor.AssetDatabase.SaveAssets();

return "Created Global Volume with Bloom and ACES Tonemapping.";
```

### Enabling Post-Processing on Camera

```csharp
var cam = UnityEngine.Camera.main;
if (cam == null) { throw new System.Exception("No Main Camera found."); }

if (!cam.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var data)) { throw new System.Exception("Missing UniversalAdditionalCameraData. Is URP active?"); }

UnityEditor.Undo.RecordObject(data, "Enable Post-Processing");
data.renderPostProcessing = true;
UnityEditor.EditorUtility.SetDirty(data);
return $"Post-processing enabled on '{cam.name}'.";
```

### Modifying an Existing Volume Profile

```csharp
var volumeObj = UnityEngine.GameObject.Find("Global Volume");
if (volumeObj != null && volumeObj.TryGetComponent<UnityEngine.Rendering.Volume>(out var volume) && volume.profile != null)
{
    if (volume.profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
    {
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 2f;
    }
    UnityEditor.EditorUtility.SetDirty(volume.profile);
}
```

### Making a change undoable through `eval`

`Undo.RecordObject` alone does **not** produce an undo entry when the snippet runs through the
Editor's `eval` command (measured: nothing appeared on the undo stack). `RecordObject` takes a
*deferred* snapshot that Unity flushes at the end of an Editor event, and a snippet executed by
the Pipeline server is outside that loop.

Use the explicit group + immediate-snapshot + flush sequence instead, which does not rely on
the event loop:

```csharp
UnityEditor.Undo.IncrementCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("Set up post-processing");     // the label the user will see
var group = UnityEditor.Undo.GetCurrentGroup();

UnityEditor.Undo.RegisterCompleteObjectUndo(target, "Set up post-processing");   // immediate, not deferred
// ... make the modification, and for a newly created object:
// UnityEditor.Undo.RegisterCreatedObjectUndo(newObject, "Set up post-processing");
UnityEditor.EditorUtility.SetDirty(target);

UnityEditor.Undo.FlushUndoRecordObjects();         // force the snapshot out now
UnityEditor.Undo.CollapseUndoOperations(group);    // one entry, not several

return UnityEditor.Undo.GetCurrentGroupName();     // report the label back to confirm it landed
```

**Verify it landed rather than assuming.** The returned group name tells you the group exists;
to confirm the entry is actually on the stack, have the user check `Edit > Undo <label>` in the
menu — it should read your label.

**If it still doesn't land, fall back to a scoped revert rather than dropping the guarantee.**
Before modifying, read the current value of the field you're about to change and report it with
a one-line snippet that restores it, so reverting is a single command:

```csharp
// captured before the change
var previous = data.renderPostProcessing;         // report: previous = false
// revert snippet to hand the user:
//   data.renderPostProcessing = false;
```

Scene and prefab files are text-serialized, so version control is the last resort, not the
first answer — offer the scoped revert before pointing at `git`.
