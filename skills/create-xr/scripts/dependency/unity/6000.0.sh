#!/usr/bin/env bash
# scripts/dependency/unity/6000.0.sh — C# for Unity core APIs and URP (URP ships locked to the Editor version).
# Sourced through use_dep (scripts/_eval.sh), which picks the highest version file that is not newer than the
# installed version. When a new version breaks a snippet: copy this file to <new major.minor>.sh, fix it there, and keep
# this one for older versions. Data only: variables holding C# for `eval`; the caller replaces the __PLACEHOLDERS__.

# URP renderer feature class matched by name (not public; FR-3, FR-8). Put into the C# below at the end of this file.
URP_SSAO="ScreenSpaceAmbientOcclusion"

# ---- setup step 2: Active Input Handling (serialized field; feature request FR-6)
ACTIVE_INPUT_CS='var ps = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
var so = new UnityEditor.SerializedObject(ps);
var p = so.FindProperty("activeInputHandler");
if (p == null) return "ERROR: activeInputHandler not found in ProjectSettings";
if (p.intValue != 0) return "already " + (p.intValue == 1 ? "Input System" : "Both") + " (unchanged)";
p.intValue = 2;
so.ApplyModifiedProperties();
UnityEditor.AssetDatabase.SaveAssets();
return "Input Manager (old) -> Both. Takes effect after an Editor restart; tell the user.";'

# ---- step 3: activate an existing Build Profile (__GUID__); sync the selected build target group
ACTIVATE_PROFILE_CS='var path = UnityEditor.AssetDatabase.GUIDToAssetPath("__GUID__");
var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Build.Profile.BuildProfile>(path);
if (profile == null) return "ERROR: no build profile for GUID __GUID__";
UnityEditor.Build.Profile.BuildProfile.SetActiveBuildProfile(profile);
return "activation requested: " + path;'
SYNC_TARGET_GROUP_CS='UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
return "active=" + UnityEditor.EditorUserBuildSettings.activeBuildTarget + " selected=" + UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;'

# ---- step 5: Android player settings (__MINSDK__); Run In Background + save the active profile
ANDROID_PS_CS='UnityEditor.PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.ScriptingImplementation.IL2CPP);
UnityEditor.PlayerSettings.Android.targetArchitectures = UnityEditor.AndroidArchitecture.ARM64;
UnityEditor.PlayerSettings.Android.minSdkVersion = (UnityEditor.AndroidSdkVersions)__MINSDK__;
UnityEditor.PlayerSettings.SetUseDefaultGraphicsAPIs(UnityEditor.BuildTarget.Android, false);
UnityEditor.PlayerSettings.SetGraphicsAPIs(UnityEditor.BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
return "backend=" + UnityEditor.PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android) + " arch=" + UnityEditor.PlayerSettings.Android.targetArchitectures + " minSdk=" + UnityEditor.PlayerSettings.Android.minSdkVersion + " graphics=" + string.Join(",", UnityEditor.PlayerSettings.GetGraphicsAPIs(UnityEditor.BuildTarget.Android));'
RUN_IN_BG_SAVE_CS='UnityEditor.PlayerSettings.runInBackground = true;
var profile = UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();
if (profile != null) UnityEditor.EditorUtility.SetDirty(profile);
UnityEditor.AssetDatabase.SaveAssets();
return "runInBackground=" + UnityEditor.PlayerSettings.runInBackground + ", saved" + (profile != null ? " (profile: " + profile.name + ")" : "");'

# ---- setup done-check fragments (__PRIMARY__, __MINSDK__); they add to `todo` and `r`
SETUP_CHECK_CS='var active = UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();
if (active == null || active.name != "__PRIMARY__") todo.Add("active build profile is " + (active == null ? "none" : active.name) + ", expected __PRIMARY__");
r += "profile=" + (active == null ? "none" : active.name) + " target=" + UnityEditor.EditorUserBuildSettings.activeBuildTarget;
if (!UnityEditor.PlayerSettings.runInBackground) todo.Add("Run In Background is off");'
ANDROID_PS_CHECK_CS='var nbt = UnityEditor.Build.NamedBuildTarget.Android;
if (UnityEditor.PlayerSettings.GetScriptingBackend(nbt) != UnityEditor.ScriptingImplementation.IL2CPP) todo.Add("Android scripting backend is not IL2CPP");
if (UnityEditor.PlayerSettings.Android.targetArchitectures != UnityEditor.AndroidArchitecture.ARM64) todo.Add("Android architecture is not ARM64");
if ((int)UnityEditor.PlayerSettings.Android.minSdkVersion < __MINSDK__) todo.Add("Android minimum API level is below __MINSDK__");
var apis = UnityEditor.PlayerSettings.GetGraphicsAPIs(UnityEditor.BuildTarget.Android);
if (apis.Length != 1 || apis[0] != UnityEngine.Rendering.GraphicsDeviceType.Vulkan) todo.Add("Android graphics API is not Vulkan only");'

# ---- meta-graphics: Vulkan precondition of step 2; Quest URP asset, steps 3-5 (m_RendererDataList: FR-7)
VULKAN_REQUIRED_CS='var apis = UnityEditor.PlayerSettings.GetGraphicsAPIs(UnityEditor.BuildTarget.Android);
if (apis.Length == 0 || apis[0] != UnityEngine.Rendering.GraphicsDeviceType.Vulkan) return "ERROR: Vulkan is not the Android graphics API (" + string.Join(",", apis) + "). Foveated rendering needs it. Run the setup procedure (player settings) first.";'
URP_COPY_CS='string rpDst = "Assets/Settings/Quest_RPAsset.asset";
string rdDst = "Assets/Settings/Quest_Renderer.asset";
// Created fresh, not copied: a copy of the current project asset (the PC one in the URP template) carries desktop
// settings that break Quest builds (SSAO feature -> stripped resources; GPU Resident Drawer -> pipeline creation fails).
var src = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
if (src == null) return "ERROR: no active UniversalRenderPipelineAsset. Is this a URP project?";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Settings")) UnityEditor.AssetDatabase.CreateFolder("Assets", "Settings");
var rd = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(rdDst);
if (rd == null)
{
    rd = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
    // Post-processing resources: reuse those of the project renderer (the default getter is internal).
    var srcList = new UnityEditor.SerializedObject(src).FindProperty("m_RendererDataList");
    var srcRd = (srcList != null && srcList.arraySize > 0 ? srcList.GetArrayElementAtIndex(0).objectReferenceValue : null) as UnityEngine.Rendering.Universal.UniversalRendererData;
    if (srcRd != null) rd.postProcessData = srcRd.postProcessData;
    UnityEditor.AssetDatabase.CreateAsset(rd, rdDst);
}
var rp = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(rpDst);
bool created = rp == null;
if (created)
{
    rp = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.Create(rd);
    UnityEditor.AssetDatabase.CreateAsset(rp, rpDst);
}
else
{
    var so = new UnityEditor.SerializedObject(rp);
    var list = so.FindProperty("m_RendererDataList");
    if (list == null) return "ERROR: renderer list not found on the URP asset (URP version changed?)";
    if (list.arraySize == 0) list.arraySize = 1;
    list.GetArrayElementAtIndex(0).objectReferenceValue = rd;
    so.ApplyModifiedProperties();
}
UnityEditor.AssetDatabase.SaveAssets();
return "Quest_RPAsset " + (created ? "created" : "already present") + ", linked to Quest_Renderer";'
URP_SETTINGS_CS='var rp = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/Quest_RPAsset.asset");
if (rp == null) return "ERROR: Quest_RPAsset not found. Run step 3 first.";
rp.supportsHDR = false;
rp.supportsCameraDepthTexture = false;
rp.supportsCameraOpaqueTexture = false;
rp.msaaSampleCount = 2;
// GPU Resident Drawer fails pipeline creation on Quest.
rp.gpuResidentDrawerMode = UnityEngine.Rendering.GPUResidentDrawerMode.Disabled;
UnityEditor.EditorUtility.SetDirty(rp);
var rd = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/Quest_Renderer.asset");
string depthPriming = "no renderer";
string ssao = "none";
if (rd != null)
{
    rd.depthPrimingMode = UnityEngine.Rendering.Universal.DepthPrimingMode.Disabled;
    rd.renderingMode = UnityEngine.Rendering.Universal.RenderingMode.Forward;
    rd.shadowTransparentReceive = false;
    // Step 3 creates the renderer without features; report SSAO if one is present anyway.
    foreach (var f in rd.rendererFeatures)
        if (f != null && f.GetType().Name == "__SSAO__") ssao = "present";
    UnityEditor.EditorUtility.SetDirty(rd);
    depthPriming = rd.depthPrimingMode.ToString();
}
UnityEditor.AssetDatabase.SaveAssets();
return "HDR=" + rp.supportsHDR + ", DepthTex=" + rp.supportsCameraDepthTexture + ", OpaqueTex=" + rp.supportsCameraOpaqueTexture + ", MSAA=" + rp.msaaSampleCount + "x, GPUResidentDrawer=" + rp.gpuResidentDrawerMode + ", DepthPriming=" + depthPriming + ", SSAO=" + ssao;'
QUALITY_LEVELS_CS='var rp = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/Quest_RPAsset.asset");
if (rp == null) return "ERROR: Quest_RPAsset not found. Run step 3 first.";
// Added by the Meta Quest build profile (Android only). Other levels can be shared with PC / Quest Link / Editor.
int level = System.Array.IndexOf(UnityEngine.QualitySettings.names, "__QUEST_LEVEL__");
if (level < 0) return "ERROR: no __QUEST_LEVEL__ quality level. Create the Meta Quest build profile first (it adds the level).";
int original = UnityEngine.QualitySettings.GetQualityLevel();
UnityEngine.QualitySettings.SetQualityLevel(level, false);
UnityEngine.QualitySettings.renderPipeline = rp;
UnityEngine.QualitySettings.SetQualityLevel(original, false);
// QualitySettings.renderPipeline changes the levels in memory only; SaveAssets skips them unless the
// QualitySettings asset is marked dirty, so they would reach disk only when the Editor quits cleanly.
var qualityAsset = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
if (qualityAsset.Length == 0) return "ERROR: could not load ProjectSettings/QualitySettings.asset to save it";
UnityEditor.EditorUtility.SetDirty(qualityAsset[0]);
UnityEditor.AssetDatabase.SaveAssets();
return "__QUEST_LEVEL__ uses Quest_RPAsset";'

# ---- graphics done-check fragment
URP_CHECK_CS='int level = System.Array.IndexOf(UnityEngine.QualitySettings.names, "__QUEST_LEVEL__");
var lp = level < 0 ? null : UnityEngine.QualitySettings.GetRenderPipelineAssetAt(level);
var levelOk = lp != null && lp.name == "Quest_RPAsset";
// The values URP_SETTINGS_CS writes.
var bad = new System.Collections.Generic.List<string>();
var rp = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/Quest_RPAsset.asset");
if (rp == null) bad.Add("asset missing");
else
{
    if (rp.supportsHDR) bad.Add("HDR");
    if (rp.supportsCameraDepthTexture) bad.Add("DepthTex");
    if (rp.supportsCameraOpaqueTexture) bad.Add("OpaqueTex");
    if (rp.msaaSampleCount != 2) bad.Add("MSAA");
    if (rp.gpuResidentDrawerMode != UnityEngine.Rendering.GPUResidentDrawerMode.Disabled) bad.Add("GPUResidentDrawer");
}
var rd = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/Quest_Renderer.asset");
if (rd == null) bad.Add("renderer missing");
else
{
    if (rd.depthPrimingMode != UnityEngine.Rendering.Universal.DepthPrimingMode.Disabled) bad.Add("DepthPriming");
    if (rd.shadowTransparentReceive) bad.Add("ShadowTransparentReceive");
}
var ssaoOn = false;
// Reported, not a todo: SSAO is not recommended on Quest, but the user may keep it.
if (rd != null) foreach (var x in rd.rendererFeatures) if (x != null && x.GetType().Name == "__SSAO__") ssaoOn = true;
r += ", __QUEST_LEVEL__ on Quest_RPAsset=" + (level < 0 ? "level missing" : levelOk.ToString())
   + ", URP settings=" + (bad.Count == 0 ? "ok" : "not set: " + string.Join(",", bad)) + ", SSAO=" + (ssaoOn ? "present" : "none");
if (!levelOk || bad.Count > 0) todo.Add("urp");'

# ---- status.sh: XR Origin in the open scene (matched by name; no package reference)
RIG_CHECK_CS='var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (string.IsNullOrEmpty(scene.path)) return "not present (no scene open)";
string active = null; int inactive = 0;
foreach (var root in scene.GetRootGameObjects())
    foreach (var c in root.GetComponentsInChildren<UnityEngine.Component>(true))
        if (c != null && c.GetType().Name == "XROrigin") {
            if (c.gameObject.activeInHierarchy && active == null) active = c.gameObject.name + " link=" + UnityEditor.PrefabUtility.GetPrefabInstanceStatus(c.gameObject);
            else if (!c.gameObject.activeInHierarchy) inactive++;
        }
if (active != null) return "present: " + active + (inactive > 0 ? " (+" + inactive + " inactive)" : "") + " scene=" + scene.path;
return (inactive > 0 ? "not present (" + inactive + " inactive XR Origin)" : "not present") + " (scene " + scene.path + ")";'

# Put the class names above into this file's C#.
URP_COPY_CS=${URP_COPY_CS//__SSAO__/$URP_SSAO}; URP_SETTINGS_CS=${URP_SETTINGS_CS//__SSAO__/$URP_SSAO}; URP_CHECK_CS=${URP_CHECK_CS//__SSAO__/$URP_SSAO}
URP_QUEST_LEVEL="Meta Quest (Build Profile)"   # engine constant (MetaAndroidBuildProfileExtension.k_QualityLevelName)
QUALITY_LEVELS_CS=${QUALITY_LEVELS_CS//__QUEST_LEVEL__/$URP_QUEST_LEVEL}; URP_CHECK_CS=${URP_CHECK_CS//__QUEST_LEVEL__/$URP_QUEST_LEVEL}
