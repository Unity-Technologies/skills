#!/usr/bin/env bash
# scripts/dependency/com.unity.xr.openxr/1.19.sh — C# for the OpenXR Plug-in.
# Sourced through use_dep (scripts/_eval.sh), which picks the highest version file that is not newer than the
# installed version. When a new version breaks a snippet: copy this file to <new major.minor>.sh, fix it there, and keep
# this one for older versions. Data only: variables holding C# for `eval`; the caller replaces the __PLACEHOLDERS__.

# ---- feature and interaction-profile class names, by device (setup step 4, the setup check).
# REQUIRED = must be on (missing -> the step fails); OPTIONAL = enabled when this package version has them.
OXR_QUEST_REQUIRED="MetaQuestFeature OculusTouchControllerProfile"                            # Android tab
OXR_QUEST_OPTIONAL="MetaQuestTouchPlusControllerProfile MetaQuestTouchProControllerProfile"   # Android + Standalone (Link)
OXR_ANDROIDXR_OPTIONAL="OculusTouchControllerProfile"                                         # Android tab
OXR_STANDALONE_REQUIRED="OculusTouchControllerProfile"                                        # Standalone tab (PC VR, Link)
OXR_PCVR_OPTIONAL="ValveIndexControllerProfile HTCViveControllerProfile MicrosoftMotionControllerProfile"
OXR_FOVEATED="FoveatedRenderingFeature"                    # graphics step 2, graphics check
OXR_PERFORMANCE="XrPerformanceSettingsFeature"             # graphics step 2
OXR_LOADER="OpenXRLoader"                                  # loader checks (XR Management C#; callers fill __LOADER__)
OXR_LOADER_TYPE="UnityEngine.XR.OpenXR.OpenXRLoader"       # loader assignment (XR Management C#; callers fill __LOADER_TYPE__)

# ---- setup step 4: enable features on a tab (__GROUP__, __WANT__, __REQUIRED__; class names: FR-8)
FEATURES_CS='var s = UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(UnityEditor.BuildTargetGroup.__GROUP__);
if (s == null) return "ERROR: OpenXR settings for __GROUP__ not found";
var want = new string[] { __WANT__ };
var required = new string[] { __REQUIRED__ };
var on = new System.Collections.Generic.List<string>();
foreach (var f in s.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>())
{
    var n = f.GetType().Name;
    if (System.Array.IndexOf(want, n) >= 0) { f.enabled = true; on.Add(n); }
}
UnityEditor.EditorUtility.SetDirty(s);
UnityEditor.AssetDatabase.SaveAssets();
foreach (var r in required) if (!on.Contains(r)) return "ERROR: " + r + " not found on the __GROUP__ tab (package missing or version changed)";
var missing = new System.Collections.Generic.List<string>();
foreach (var w in want) if (!on.Contains(w)) missing.Add(w);
return "enabled: " + string.Join(", ", on) + (missing.Count > 0 ? " | not available (skipped): " + string.Join(", ", missing) : "");'

# ---- meta-graphics steps 1-2: single-pass instanced; foveated rendering + performance settings
SPI_CS='var s = UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(UnityEditor.BuildTargetGroup.Android);
if (s == null) return "ERROR: OpenXR settings for Android not found. Run the setup procedure (OpenXR) first.";
s.renderMode = UnityEngine.XR.OpenXR.OpenXRSettings.RenderMode.SinglePassInstanced;
UnityEditor.EditorUtility.SetDirty(s);
UnityEditor.AssetDatabase.SaveAssets();
return "renderMode=" + s.renderMode;'
FOVEATED_CS='var s = UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(UnityEditor.BuildTargetGroup.Android);
if (s == null) return "ERROR: OpenXR settings for Android not found. Run the setup procedure (OpenXR) first.";
var enabled = new System.Collections.Generic.List<string>();
foreach (var f in s.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>())
{
    var n = f.GetType().Name;
    if (n == "__FOVEATED__" || n == "__PERFORMANCE__")
    {
        f.enabled = true;
        enabled.Add(n);
    }
}
UnityEditor.EditorUtility.SetDirty(s);
UnityEditor.AssetDatabase.SaveAssets();
if (!enabled.Contains("__FOVEATED__")) return "ERROR: __FOVEATED__ not found among Android OpenXR features (OpenXR package version?)";
return "enabled: " + string.Join(", ", enabled);'

# ---- done-check fragments: required features per tab (__GROUPS__, __REQS__); graphics render mode + foveated
FEATURES_CHECK_CS='var groups = new UnityEditor.BuildTargetGroup[] { __GROUPS__ };
var reqs = new string[] { __REQS__ };
for (int i = 0; i < groups.Length; i++)
{
    var oxs = UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(groups[i]);
    var on = new System.Collections.Generic.List<string>();
    if (oxs != null) foreach (var f in oxs.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>()) if (f.enabled) on.Add(f.GetType().Name);
    foreach (var q in reqs[i].Split(",".ToCharArray())) if (q != "" && !on.Contains(q)) todo.Add(q + " not enabled on the " + groups[i] + " tab");
    r += " | " + groups[i] + ": on=" + string.Join(",", on);
}'
GRAPHICS_CHECK_CS='var s = UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(UnityEditor.BuildTargetGroup.Android);
if (s == null) return "ERROR: OpenXR is not set up for Android";
var fov = false;
foreach (var f in s.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>()) if (f.GetType().Name == "__FOVEATED__") fov = f.enabled;
r += "renderMode=" + s.renderMode + ", foveated=" + fov;
if (s.renderMode != UnityEngine.XR.OpenXR.OpenXRSettings.RenderMode.SinglePassInstanced || !fov) todo.Add("openxr");'

# Put the class names above into this file's C#.
FOVEATED_CS=${FOVEATED_CS//__FOVEATED__/$OXR_FOVEATED}; FOVEATED_CS=${FOVEATED_CS//__PERFORMANCE__/$OXR_PERFORMANCE}
GRAPHICS_CHECK_CS=${GRAPHICS_CHECK_CS//__FOVEATED__/$OXR_FOVEATED}
