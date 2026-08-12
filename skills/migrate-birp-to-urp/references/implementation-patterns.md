# Built-in to URP Implementation Patterns

> **How to run the C# in this file.** Blocks that declare a `class`, a `static` method, or a
> `[MenuItem]` are project files — save them under `Assets/Editor/`, let Unity compile, then call
> their entry point through a one-line `unity command eval`. Keep their `using` directives; they are
> correct in a file. Blocks that are a bare sequence of statements can go straight to `eval`, but
> only fully qualified and with no `using` lines — `eval` compiles a statement block, so a `using`
> is read as a resource-disposal statement and rejected (`CS0210`). Extension methods are
> unavailable through `eval` for the same reason: write `System.Linq.Enumerable.FirstOrDefault(seq,
> pred)` and `GetComponent<UniversalAdditionalCameraData>()` instead of the extension forms.

Use these patterns when executing a migration, not for planning-only answers. They exist for fragile steps where a generic script often appears to work in tool logs but fails to persist saved project state. Treat these patterns as lower-freedom implementation requirements, not optional inspiration.

## Table of Contents

- [Persistent URP VolumeProfile Pattern](#persistent-urp-volumeprofile-pattern)
- [Pre-Conversion Material Snapshot Pattern](#pre-conversion-material-snapshot-pattern)
- [Safe Manual Material Conversion Pattern](#safe-manual-material-conversion-pattern)
- [Legacy PPv2 Disable Pattern](#legacy-ppv2-disable-pattern)
- [Reflection Probe URP Asset Pattern](#reflection-probe-urp-asset-pattern)
- [Lighting And Probe Claim Rules](#lighting-and-probe-claim-rules)
- [Visual Exposure Balance Pattern](#visual-exposure-balance-pattern)
- [Execution Regression Checklist](#execution-regression-checklist)
- [Final Status Pattern](#final-status-pattern)

## Pre-Conversion Material Snapshot Pattern

Before running Unity's material converter, a bulk material script, or any manual shader assignment, snapshot source material values while the material is still on the Built-in shader. Use this snapshot as the source of truth for `_BaseMap` and `_BaseColor` restoration. Do not try to recover `_MainTex` after shader conversion.

Minimum snapshot fields:

- asset path and GUID
- source shader name
- `_MainTex`, texture scale, and texture offset
- `_Color`
- `_BumpMap`
- `_MetallicGlossMap`, `_SpecGlossMap`, `_Metallic`, `_Glossiness`, and `_SpecColor` where present
- `_EmissionMap` and `_EmissionColor`
- `_Cutoff` and `_Mode`

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

sealed class MaterialSnapshot
{
    public string Path;
    public string Guid;
    public string SourceShader;
    public Texture MainTex;
    public Vector2 MainTexScale = Vector2.one;
    public Vector2 MainTexOffset = Vector2.zero;
    public Color Color = Color.white;
    public Texture Normal;
    public Texture MetallicGloss;
    public Texture SpecGloss;
    public Texture Emission;
    public Color EmissionColor = Color.black;
    public float Metallic;
    public float Glossiness = 0.5f;
    public Color SpecColor = Color.black;
    public float Cutoff = 0.5f;
    public int Mode;
}

static Dictionary<string, MaterialSnapshot> SnapshotProjectMaterials()
{
    var snapshots = new Dictionary<string, MaterialSnapshot>();
    foreach (var guid in AssetDatabase.FindAssets("t:Material"))
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (path.StartsWith("Packages/") || path.StartsWith("Library/PackageCache/"))
            continue;

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
            continue;

        snapshots[path] = new MaterialSnapshot
        {
            Path = path,
            Guid = guid,
            SourceShader = mat.shader != null ? mat.shader.name : "",
            MainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null,
            MainTexScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one,
            MainTexOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero,
            Color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white,
            Normal = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null,
            MetallicGloss = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null,
            SpecGloss = mat.HasProperty("_SpecGlossMap") ? mat.GetTexture("_SpecGlossMap") : null,
            Emission = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null,
            EmissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black,
            Metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f,
            Glossiness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f,
            SpecColor = mat.HasProperty("_SpecColor") ? mat.GetColor("_SpecColor") : Color.black,
            Cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f,
            Mode = mat.HasProperty("_Mode") ? (int)mat.GetFloat("_Mode") : 0
        };
    }
    return snapshots;
}
```

After conversion, restore and verify from the snapshot:

```csharp
static bool RestoreBasePropertiesFromSnapshot(Material mat, MaterialSnapshot snapshot)
{
    if (mat == null || snapshot == null)
        return false;

    if (snapshot.MainTex != null && mat.HasProperty("_BaseMap"))
    {
        mat.SetTexture("_BaseMap", snapshot.MainTex);
        mat.SetTextureScale("_BaseMap", snapshot.MainTexScale);
        mat.SetTextureOffset("_BaseMap", snapshot.MainTexOffset);
    }
    if (mat.HasProperty("_BaseColor"))
        mat.SetColor("_BaseColor", snapshot.Color);
    if (snapshot.Normal != null && mat.HasProperty("_BumpMap"))
    {
        mat.SetTexture("_BumpMap", snapshot.Normal);
        mat.EnableKeyword("_NORMALMAP");
    }
    if (snapshot.MetallicGloss != null && mat.HasProperty("_MetallicGlossMap"))
        mat.SetTexture("_MetallicGlossMap", snapshot.MetallicGloss);
    if (snapshot.Emission != null && mat.HasProperty("_EmissionMap"))
    {
        mat.SetTexture("_EmissionMap", snapshot.Emission);
        mat.EnableKeyword("_EMISSION");
    }
    if (mat.HasProperty("_Cutoff"))
        mat.SetFloat("_Cutoff", snapshot.Cutoff);

    EditorUtility.SetDirty(mat);
    return snapshot.MainTex == null || !mat.HasProperty("_BaseMap") || mat.GetTexture("_BaseMap") != null;
}
```

If no snapshot was captured, do not claim texture preservation. A post-conversion pass such as `if (mat.HasProperty("_MainTex")) mat.SetTexture("_BaseMap", mat.GetTexture("_MainTex"))` is not reliable after the shader has changed, because `_MainTex` may already be null/default.

## Safe Manual Material Conversion Pattern

Prefer Unity's Render Pipeline Converter when available. Use manual `mat.shader = Shader.Find(...)` only as a fallback, and never after losing the source material data. Before changing a shader, snapshot source properties and restore mapped URP values immediately after assignment. Skip immutable package assets.

**Whichever route you take, read the resulting `shader.name` back.** `MaterialUpgrader.FetchAllUpgradersForPipeline` returns the 2D provider set as well as the 3D one, and two upgraders claim `Standard` at equal priority — so on a 3D project the converter can land a `Standard` material on `Universal Render Pipeline/2D/Mesh2D-Lit-Default` rather than `Universal Render Pipeline/Lit`. It throws nothing and the material does not render magenta, so the only way to catch it is to check the name. Measured on 6000.5.8f1.

If it happens: restore the affected materials from the rollback point and either filter the upgrader list to the 3D providers, or convert them with the manual pattern below, which names its target shader explicitly and so cannot be hijacked.

```csharp
// Verify, per material, after any conversion route.
var expected = "Universal Render Pipeline/Lit"; // or the mapped target for this material
if (mat.shader.name != expected)
    Debug.LogWarning($"{path}: landed on '{mat.shader.name}', expected '{expected}'");
```

```csharp
using UnityEditor;
using UnityEngine;

static bool TryConvertStandardMaterialSafely(Material mat, string path)
{
    if (mat == null || path.StartsWith("Packages/") || path.StartsWith("Library/PackageCache/"))
        return false;

    var sourceMain = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
    var sourceColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
    var sourceScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
    var sourceOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;
    var normal = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
    var metallic = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
    var emission = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
    var cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;

    var urpLit = Shader.Find("Universal Render Pipeline/Lit");
    if (urpLit == null)
        return false;

    mat.shader = urpLit;

    if (mat.HasProperty("_BaseMap"))
    {
        mat.SetTexture("_BaseMap", sourceMain);
        mat.SetTextureScale("_BaseMap", sourceScale);
        mat.SetTextureOffset("_BaseMap", sourceOffset);
    }
    if (mat.HasProperty("_BaseColor"))
        mat.SetColor("_BaseColor", sourceColor);
    if (normal != null && mat.HasProperty("_BumpMap"))
    {
        mat.SetTexture("_BumpMap", normal);
        mat.EnableKeyword("_NORMALMAP");
    }
    if (metallic != null && mat.HasProperty("_MetallicGlossMap"))
        mat.SetTexture("_MetallicGlossMap", metallic);
    if (emission != null && mat.HasProperty("_EmissionMap"))
    {
        mat.SetTexture("_EmissionMap", emission);
        mat.EnableKeyword("_EMISSION");
    }
    if (mat.HasProperty("_Cutoff"))
        mat.SetFloat("_Cutoff", cutoff);

    if (sourceMain != null && mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null)
        throw new System.Exception($"Texture was lost while converting {path}");

    EditorUtility.SetDirty(mat);
    return true;
}
```

After conversion, sample representative materials. If source `_MainTex` was non-null, the converted material must have a non-null `_BaseMap` or an intentionally different URP property with the same texture. If source `_Color` was not white, `_BaseColor` should not silently become white. Do not report "fixed texture/color properties" when the copied source values are already null/default because the shader was changed before snapshotting.

## Persistent URP VolumeProfile Pattern

When creating URP Volume overrides by script, `profile.Add<T>()` alone is not enough proof. Persist the override objects as sub-assets, save, reload, and verify the expected non-null components before claiming PPv2 migration. For a generic PPv2 migration, include the common mappable overrides shown below unless inspection proves the source did not use them; if you omit one, report it as omitted/manual instead of implying full PPv2 parity.

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

static VolumeProfile CreateOrRepairProfile(string profilePath)
{
    var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
    if (profile == null)
    {
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, profilePath);
    }

    // If previous attempts left an empty/broken profile, recreate the known mappable overrides.
    AddPersistentOverride<Bloom>(profile);
    AddPersistentOverride<ColorAdjustments>(profile);
    AddPersistentOverride<Tonemapping>(profile);
    AddPersistentOverride<DepthOfField>(profile);
    AddPersistentOverride<Vignette>(profile);

    EditorUtility.SetDirty(profile);
    AssetDatabase.SaveAssets();
    AssetDatabase.ImportAsset(profilePath);

    var reloaded = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
    var validCount = 0;
    if (reloaded != null)
    {
        foreach (var component in reloaded.components)
        {
            if (component != null)
                validCount++;
        }
    }

    var requiredTypes = new[]
    {
        typeof(Bloom),
        typeof(ColorAdjustments),
        typeof(Tonemapping),
        typeof(DepthOfField),
        typeof(Vignette)
    };

    foreach (var requiredType in requiredTypes)
    {
        if (!reloaded.components.Any(component => component != null && component.GetType() == requiredType))
            throw new System.Exception($"URP VolumeProfile is missing persisted {requiredType.Name} override.");
    }

    if (validCount < requiredTypes.Length)
        throw new System.Exception("URP VolumeProfile did not persist all required override components.");

    return reloaded;
}

static T AddPersistentOverride<T>(VolumeProfile profile) where T : VolumeComponent
{
    if (profile.TryGet<T>(out var existing) && existing != null)
        return existing;

    var component = profile.Add<T>(true);
    component.name = typeof(T).Name;
    AssetDatabase.AddObjectToAsset(component, profile);
    EditorUtility.SetDirty(component);
    EditorUtility.SetDirty(profile);
    return component;
}
```

After using this pattern, also verify the representative scene has a `UnityEngine.Rendering.Volume` component whose `sharedProfile` points to the reloaded profile. A scene Volume with an empty profile, missing common mappable overrides, or `{fileID: 0}` component references is not active migrated post-processing.

When reusing the old PPv2 GameObject as the location for URP post-processing, add a URP `Volume` component and set that component's `sharedProfile`. Do not accidentally set the PPv2 `PostProcessVolume.sharedProfile` to a URP profile; that leaves the scene with old PPv2 still active and no URP Volume reference.

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

static void EnsureSceneVolumeReferencesProfile(VolumeProfile profile)
{
    var volume = Object.FindFirstObjectByType<Volume>();
    if (volume == null)
    {
        var go = GameObject.Find("Post-process Volume") ?? new GameObject("URP Global Volume");
        volume = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
    }

    volume.isGlobal = true;
    volume.sharedProfile = profile;
    volume.enabled = true;
    EditorUtility.SetDirty(volume);
    EditorSceneManager.MarkSceneDirty(volume.gameObject.scene);
    EditorSceneManager.SaveScene(volume.gameObject.scene);
}
```

## Legacy PPv2 Disable Pattern

If a URP Volume replacement exists and has persisted non-null components, disable legacy PPv2 during URP validation. Keep the components/assets as rollback/reference unless the user approves deletion.

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static void DisableLegacyPostProcessingForValidation()
{
    foreach (var behaviour in Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None))
    {
        var typeName = behaviour.GetType().FullName;
        if (typeName == "UnityEngine.Rendering.PostProcessing.PostProcessVolume" ||
            typeName == "UnityEngine.Rendering.PostProcessing.PostProcessLayer")
        {
            behaviour.enabled = false;
            EditorUtility.SetDirty(behaviour);
            EditorSceneManager.MarkSceneDirty(behaviour.gameObject.scene);
        }
    }

    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
}
```

After disabling PPv2, save and re-query the scene serialization or component state. Do not claim PPv2 is disabled if the saved `PostProcessVolume` block still has `m_Enabled: 1`. Do not disable PPv2 and then claim migration success if the URP replacement profile is empty. In that case, report PPv2 as partial/incomplete.

## Reflection Probe URP Asset Pattern

When the representative Built-in scene uses reflection probes, verify the URP asset settings that control probe behavior. The Render Settings converter should map these from Built-in tiers, but hand-created URP assets often leave them disabled. If the scene relies on localized probes, enable and save both probe blending and box projection, then re-read the saved URP asset before reporting reflections as configured.

```csharp
using UnityEditor;
using UnityEngine.Rendering.Universal;

// `urpAsset.reflectionProbeBlending` and `.reflectionProbeBoxProjection` are public but
// READ-ONLY (assigning either is CS0200), so writing them means going through
// SerializedObject and the private field names. Those names carry no compatibility
// guarantee, so this must fail loudly rather than silently: if a name stops resolving,
// tell the user to tick the two boxes under Lighting → Reflection Probes on the URP asset
// instead of reporting success. Never leave a setting silently unapplied.
static bool EnableReflectionProbeSettings(UniversalRenderPipelineAsset urpAsset)
{
    var serialized = new SerializedObject(urpAsset);
    // internal-api-ok: the public reflectionProbeBlending / reflectionProbeBoxProjection
    // properties are get-only (assigning either is CS0200), so there is no public write
    // path. Guarded below: a null property reports and defers to the user's inspector
    // rather than claiming success, and the result is read back through the public
    // properties to confirm the write landed.
    var blending = serialized.FindProperty("m_ReflectionProbeBlending");
    var boxProjection = serialized.FindProperty("m_ReflectionProbeBoxProjection");

    if (blending == null || boxProjection == null)
    {
        Debug.LogWarning(
            "Could not set Probe Blending / Box Projection programmatically on " +
            urpAsset.name + ". Ask the user to enable them on the URP asset under " +
            "Lighting → Reflection Probes, then continue.");
        return false;
    }

    blending.boolValue = true;
    boxProjection.boolValue = true;

    serialized.ApplyModifiedProperties();
    // Read back through the public properties to confirm it actually took.
    if (!urpAsset.reflectionProbeBlending || !urpAsset.reflectionProbeBoxProjection)
    {
        Debug.LogWarning("Probe settings did not apply on " + urpAsset.name +
                         "; ask the user to set them in the URP asset inspector.");
        return false;
    }
    return true;
    EditorUtility.SetDirty(urpAsset);
    AssetDatabase.SaveAssets();
}
```

This does not prove reflection probes were re-rendered. It only proves the URP asset is allowed to use blending and box projection. Report probe refresh separately, based on successful render output, changed probe assets, or visual acceptance.

## Lighting And Probe Claim Rules

For baked-lighting scenes, distinguish three states:

- `Preserved reference`: old `LightingData.asset`, lightmaps, and reflection-probe EXRs remain assigned/unchanged.
- `Partial`: a URP-compatible `.lighting` asset exists or is assigned, but old bake/probe outputs remain active or unchanged.
- `Refreshed/rebaked`: there is saved evidence of a completed URP bake or probe refresh, such as changed/generated lighting/probe assets, successful bake/probe-render tool output, and a post-save representative scene validation.

`Lightmapping.Clear()` and `Lightmapping.ClearLightingDataAsset()` are not enough to claim lighting was migrated or cleared in saved state. After clearing, save the scene and re-query serialized scene state. If the scene still contains a non-zero `m_LightingDataAsset` reference, still points at an old Enlighten/realtime-GI `.lighting` asset, or no URP bake/probe refresh has completed, call lighting `Partial`, not `Complete`.

If the final answer includes "Rebake Lighting", "refresh probes", or similar routine lighting work as a next step, the whole generic full migration is partial. Do not pair those next steps with "complete" or "visual parity preserved".

If you start a bake with `Lightmapping.BakeAsync()`, poll `Lightmapping.isRunning` while the tool budget allows. If it is still running when you must stop, report a phase boundary/partial migration and do not say the scene has been rebaked. Before claiming that new lighting settings are active, save the scene and verify the saved scene no longer points at the old `.lighting` GUID or a non-zero old `m_LightingDataAsset`.

Never say "reflection probes refreshed" from intent alone. If the saved EXR files are unchanged and no successful probe-render evidence exists, say they were preserved and still need refresh or visual acceptance.

## Visual Exposure Balance Pattern

Use this after URP asset assignment, supported material conversion, PPv2/URP Volume setup, and initial lighting/probe repair when the representative capture is visibly too dark, too bright, washed out, or much higher contrast than the Built-in reference.

Do not ask the user for a separate "balance exposure" prompt when rollback safety was already confirmed and the current phase is visual validation. Treat this as part of the same migration pass.

Before tuning values, prove the rendering path is actually using the URP post-processing setup:

```csharp
var report = new System.Collections.Generic.List<string>();

var urpAsset = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline
    as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
report.Add($"Graphics URP Asset: {(urpAsset != null ? urpAsset.name : "NULL")}");

if (urpAsset != null)
{
    // `rendererDataList` is public, so no SerializedObject is needed here. The default
    // renderer's index is not exposed publicly — report every renderer instead, which is
    // more useful anyway: a missing PostProcessData on any renderer the project switches
    // to will produce the same symptom.
    var renderers = urpAsset.rendererDataList;
    report.Add($"Renderer Count: {renderers.Length}");
    for (int i = 0; i < renderers.Length; i++)
    {
        var rendererData = renderers[i] as UnityEngine.Rendering.Universal.UniversalRendererData;
        report.Add($"Renderer[{i}]: {(renderers[i] != null ? renderers[i].name : "NULL")}, "
                 + $"PostProcessData: {rendererData != null && rendererData.postProcessData != null}");
    }
}

var cameras = UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(
    UnityEngine.FindObjectsSortMode.None);
var camera = System.Linq.Enumerable.FirstOrDefault(cameras, c => c.name == "MainCamera")
             ?? UnityEngine.Camera.main;
if (camera != null)
{
    // GetUniversalAdditionalCameraData() is an extension method and needs a `using`, which eval
    // rejects — read the component it wraps instead.
    var data = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
    report.Add(data != null
        ? $"Camera {camera.name}: renderPostProcessing={data.renderPostProcessing}, volumeLayerMask={data.volumeLayerMask.value}"
        : $"Camera {camera.name}: no UniversalAdditionalCameraData component");
}

foreach (var volume in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(
             UnityEngine.FindObjectsSortMode.None))
{
    report.Add($"Volume {volume.name}: global={volume.isGlobal}, enabled={volume.enabled}, "
             + $"weight={volume.weight}, priority={volume.priority}, layer={volume.gameObject.layer}, "
             + $"profile={(volume.sharedProfile != null ? volume.sharedProfile.name : "NULL")}, "
             + $"overrides={(volume.sharedProfile != null ? volume.sharedProfile.components.Count : 0)}");
}

report.Add($"Ambient: mode={UnityEngine.RenderSettings.ambientMode}, "
         + $"intensity={UnityEngine.RenderSettings.ambientIntensity}, "
         + $"skyColor={UnityEngine.RenderSettings.ambientSkyColor}");

return string.Join("\n", report);
```

If the wiring is wrong, fix the wiring first. Common fixes are assigning missing `PostProcessData` to the active renderer, enabling `renderPostProcessing` on the representative camera, setting the camera volume layer mask to include the Volume layer, making the Volume global or correctly bounded, disabling legacy PPv2 during URP validation, and recreating an empty/broken VolumeProfile.

Only after the wiring is valid, tune in bounded steps and capture again after each meaningful pass:

- Start with source PPv2 color grading values where available, then adjust around them rather than replacing them with arbitrary extremes.
- Use `ColorAdjustments.postExposure`, contrast, saturation, `Tonemapping`, `LiftGammaGain`, Bloom threshold/intensity, ambient sky color/intensity, URP additional-light count, shadow distance, and cascades as the first tuning surfaces.
- Prefer moderate persistent values for the final saved state. Very high exposure, very high ambient, or very high bloom can be used as diagnostics, but if they are required to see the scene, report missing GI/bake parity as partial.
- If the source scene relied on Enlighten realtime GI or baked bounce light, exposure balancing can approximate the look, but it does not replace a URP bake. Report the lighting state honestly.

After tuning, save assets and the active scene, re-query the VolumeProfile, camera data, ambient settings, and URP asset, then capture again. Do not claim visual parity if the final report still asks the user to "balance exposure", "fix brightness", or "tune lighting" as an ordinary required next step.

## Execution Regression Checklist

Use this checklist during actual migration, repair, or final validation. Treat each row as a positive execution invariant: verify the saved state, repair when feasible, and report partial/manual status when evidence is missing.

| Area | Verify / repair / report |
| --- | --- |
| Phase continuity | Resume from the first incomplete item after package install, compilation, domain reload, or tool interruption. A generic migration can validly finish one phase per turn. |
| Pipeline and Quality | Re-read Graphics settings and each relevant Quality level after saving. Confirm the URP asset and renderer are active before claiming setup completion. |
| Materials | Snapshot source material data before conversion, then restore `_BaseMap`, `_BaseColor`, texture scale/offset, normal, metallic/specular, emission, alpha cutoff, and surface/blend intent after shader assignment. |
| Particles and effects | Preserve transparent/additive behavior for particle, fog, smoke, steam, decal, VFX, additive, and transparent materials. Prefer URP particle/effect shaders where appropriate. |
| Foliage | Validate grass, trees, terrain details, SpeedTree, billboards, leaf cards, alpha clipping, two-sided rendering, normal/detail maps, wind, and billboard behavior before marking material conversion complete. |
| PPv2 to URP Volumes | Save persistent URP `VolumeProfile` override components, wire a scene `Volume.sharedProfile` to the new profile, and disable legacy PPv2 components for URP visual validation unless intentionally preserved for comparison. |
| PPv2 effect parity | Map common effects such as Bloom, Color Adjustments/Tonemapping, Vignette, and Depth Of Field when present. Report PPv2 SSR as unsupported/manual unless a validated URP/custom replacement exists. Handle AO through renderer SSAO where feasible. |
| Lighting and probes | Open the representative scene, verify saved Lighting Settings and Lighting Data references, clear stale Built-in data when needed, rebake or resume baking under URP when feasible, and refresh reflection probes with saved evidence. |
| Reflection settings | For reflection-probe-heavy scenes, verify URP reflection probe blending and box projection settings or report reflections as partial/manual. |
| Renderer and Console | Re-query the URP renderer list/default index and current Console output after assignment. Repair renderer, shader, and render-pipeline errors, or classify unrelated errors explicitly. |
| Exposure balance | When captures are too dark, blown out, or high contrast, first verify URP Volume/camera/renderer wiring, then perform a bounded tuning pass and save/re-query final values. |
| Final wording | Start with `Partial migration` when any material category, PPv2/URP Volume, baked lighting, reflection probe, renderer feature, custom shader, foliage behavior, visual-parity item, or Console issue remains unresolved. |

## Final Status Pattern

Use wording that matches saved-state evidence:

- `Complete`: every success gate passed.
- `Partial migration`: URP setup/material conversion worked, but PPv2, lighting/probes, Console, renderer, or saved-state verification remains incomplete.
- `Manual follow-up`: unsupported features such as PPv2 SSR, complex custom shaders, `GrabPass`, replacement shaders, or package-owned render code remain.

If any required gate is partial, do not start the final answer with "successfully migrated", "complete", or "visual parity preserved". If `Unity.GetConsoleLogs` still returns errors, either repair them or list them under incomplete/unrelated findings; do not say "no errors remain" while errors are present. A final answer that contains required routine next steps such as rebaking lighting, refreshing probes, or validating scene capture is a partial migration report.

Before using `Complete`, run or reason from a saved-state gate that checks the actual serialized files/components, not only tool intentions:

- URP profile contains the mapped source effects, including `DepthOfField` when the old PPv2 profile had it.
- Scene URP `Volume.sharedProfile` references the new profile GUID.
- Legacy PPv2 `PostProcessVolume` and `PostProcessLayer` components are disabled.
- Active scene no longer references the old Lighting Settings GUID or a non-zero old Lighting Data asset, unless lighting is explicitly reported as partial.
- URP asset reflection probe blending and box projection are enabled for reflection-probe-heavy scenes, or reflections are reported as partial/manual.
- SSAO/renderer-feature work is complete or PPv2 Ambient Occlusion is reported as manual/partial. PPv2 Screen Space Reflections are reported as unsupported/manual unless replaced by a validated URP/custom equivalent.

If any saved-state gate fails, the final answer should start with `Partial migration` and list `Complete`, `Incomplete`, and `Manual follow-up` items. Do not say the project is "fully functional on URP" while any gate is false.
