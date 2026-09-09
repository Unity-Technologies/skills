using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Batch-processes all scenes in the project, attaching LocalizeStringEvent components
/// to every text element whose content matches a key in the provided mapping.
///
/// Covers BOTH legacy UnityEngine.UI.Text and TextMeshPro (TMP_Text, which is the base of
/// TextMeshProUGUI and TextMeshPro). Walking only one of the two is the usual mistake and it
/// fails silently: measured on a real project, FindObjectsByType&lt;Text&gt; found 1 component
/// while FindObjectsByType&lt;TMP_Text&gt; found 13, so a Text-only pass reports success having
/// localized almost nothing.
///
/// Only call LocalizeAll() after confirming with the user — it modifies and saves every scene.
/// It also reports what it did NOT convert; see the return value of LocalizeHierarchy.
/// </summary>
public static class L10nBatchProcessor
{
    /// <summary>Localizes every scene. Returns the labels that matched no mapping entry.</summary>
    public static List<string> LocalizeAll(Dictionary<string, string> mapping, string table)
    {
        var unmatched = new List<string>();

        // Scope the search to Assets/. An unscoped FindAssets searches the whole project INCLUDING
        // read-only packages, so it returns package test scenes and this loop then tries to open,
        // dirty and save assets it must not touch. Measured on a real project: unscoped found 20
        // scenes where the project has 1, and all 19 extras were inside read-only packages
        // (com.unity.addressables test fixtures).
        string[] scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        foreach (var guid in scenes)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            unmatched.AddRange(LocalizeHierarchy(mapping, table, path));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        return unmatched;
    }

    /// <summary>
    /// Wires the open scene. Returns "scene :: object :: text" for every label that was found
    /// but matched no mapping entry, so partial coverage is reported rather than silent.
    /// </summary>
    static List<string> LocalizeHierarchy(
        Dictionary<string, string> mapping, string table, string scenePath)
    {
        var unmatched = new List<string>();

        // Both component families. TMP_Text is abstract and covers TextMeshProUGUI and TextMeshPro.
        var labels = new List<Component>();
        labels.AddRange(Object.FindObjectsByType<Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None));
        labels.AddRange(Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None));

        foreach (var label in labels)
        {
            // Read the current string from whichever family this component belongs to.
            var current = label is Text legacy ? legacy.text : ((TMP_Text)label).text;
            if (string.IsNullOrEmpty(current)) continue;

            var matched = false;
            foreach (var kvp in mapping)
            {
                if (!current.Contains(kvp.Key)) continue;

                var lse = label.gameObject.GetComponent<LocalizeStringEvent>()
                    ?? label.gameObject.AddComponent<LocalizeStringEvent>();
                lse.StringReference = new UnityEngine.Localization.LocalizedString(table, kvp.Value);

                // Wire the listener through the public UnityEventTools API. The persistent-call
                // fields could be written directly through SerializedObject instead, but those
                // are private serialized names with no compatibility guarantee, and it isn't
                // necessary. A delegate to the component's public `text` setter, handed to
                // AddPersistentListener, produces the same serialized call: target = the
                // component, method = set_text, mode = EventDefined (dynamic).
                //
                // The setter has no C# method-group name, so the delegate is built by name.
                // That is reflection over a *public* member, which is fine; reaching for the
                // private m_* fields would not be. `set_text` is public on both Text and
                // TMP_Text, so the same call works for either target.
                var setText = (UnityAction<string>)System.Delegate.CreateDelegate(
                    typeof(UnityAction<string>), label, "set_text");

                for (int i = lse.OnUpdateString.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(lse.OnUpdateString, i);
                }
                UnityEventTools.AddPersistentListener(lse.OnUpdateString, setText);

                // AddPersistentListener leaves the call at RuntimeOnly, which means the label does
                // NOT update when the locale changes in the Editor: correct in a build, dormant
                // while authoring, and it stays dormant through a save and reload. Set the state
                // explicitly. SetPersistentListenerState is public API on UnityEventBase.
                // Verified on Unity 6000.5.8f1: without this the listener never fires in Edit mode.
                var callIndex = lse.OnUpdateString.GetPersistentEventCount() - 1;
                lse.OnUpdateString.SetPersistentListenerState(
                    callIndex, UnityEventCallState.EditorAndRuntime);

                EditorUtility.SetDirty(lse);
                matched = true;
                break;
            }

            // A label the mapping never covered is a coverage gap the caller has to see.
            // Reporting "localized 12" while silently leaving 9 untouched is the failure mode
            // this return value exists to prevent.
            if (!matched)
            {
                unmatched.Add($"{scenePath} :: {label.gameObject.name} :: \"{current}\"");
            }
        }

        return unmatched;
    }
}
