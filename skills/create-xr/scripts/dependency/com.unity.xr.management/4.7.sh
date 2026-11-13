#!/usr/bin/env bash
# scripts/dependency/com.unity.xr.management/4.7.sh — C# for XR Plug-in Management.
# Sourced through use_dep (scripts/_eval.sh), which picks the highest version file that is not newer than the
# installed version. When a new version breaks a snippet: copy this file to <new major.minor>.sh, fix it there, and keep
# this one for older versions. Data only: variables holding C# for `eval`; the caller replaces the __PLACEHOLDERS__.

# ---- setup step 4: enable the OpenXR loader on a tab (__GROUP__)
LOADER_CS='var group = UnityEditor.BuildTargetGroup.__GROUP__;
UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget c = null;
UnityEditor.EditorBuildSettings.TryGetConfigObject(UnityEngine.XR.Management.XRGeneralSettings.settingsKey, out c);
if (c == null)
{
    if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/XR")) UnityEditor.AssetDatabase.CreateFolder("Assets", "XR");
    c = UnityEngine.ScriptableObject.CreateInstance<UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget>();
    UnityEditor.AssetDatabase.CreateAsset(c, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
    UnityEditor.EditorBuildSettings.AddConfigObject(UnityEngine.XR.Management.XRGeneralSettings.settingsKey, c, true);
}
var s = c.SettingsForBuildTarget(group);
if (s == null)
{
    s = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.XR.Management.XRGeneralSettings>();
    UnityEditor.AssetDatabase.AddObjectToAsset(s, c);
    c.SetSettingsForBuildTarget(group, s);
}
if (s.Manager == null)
{
    var m = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.XR.Management.XRManagerSettings>();
    UnityEditor.AssetDatabase.AddObjectToAsset(m, c);
    s.Manager = m;
}
UnityEditor.AssetDatabase.SaveAssets();
foreach (var l in s.Manager.activeLoaders)
    if (l != null && l.GetType().Name == "__LOADER__") return "OpenXR loader already enabled";
bool ok = UnityEditor.XR.Management.Metadata.XRPackageMetadataStore.AssignLoader(s.Manager, "__LOADER_TYPE__", group);
UnityEditor.AssetDatabase.SaveAssets();
return ok ? "OpenXR loader enabled" : "ERROR: AssignLoader returned false. Is com.unity.xr.openxr installed?";'

# ---- setup done-check fragment (__GROUPS__)
LOADER_CHECK_CS='UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget c = null;
UnityEditor.EditorBuildSettings.TryGetConfigObject(UnityEngine.XR.Management.XRGeneralSettings.settingsKey, out c);
foreach (var g in new UnityEditor.BuildTargetGroup[] { __GROUPS__ })
{
    var s = c == null ? null : c.SettingsForBuildTarget(g);
    var loader = false;
    if (s != null && s.Manager != null) foreach (var l in s.Manager.activeLoaders) if (l != null && l.GetType().Name == "__LOADER__") loader = true;
    if (!loader) todo.Add("OpenXR loader not enabled on the " + g + " tab");
}'
