using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using System.Collections.Generic;

/// <summary>
/// Batch-processes all scenes in the project, attaching LocalizeStringEvent components
/// to every Text element whose content matches a key in the provided mapping.
///
/// Only call LocalizeAll() after confirming with the user — it modifies and saves every scene.
/// </summary>
public static class L10nBatchProcessor
{
    public static void LocalizeAll(Dictionary<string, string> mapping, string table)
    {
        string[] scenes = AssetDatabase.FindAssets("t:Scene");
        foreach (var guid in scenes)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            LocalizeHierarchy(mapping, table);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    static void LocalizeHierarchy(Dictionary<string, string> mapping, string table)
    {
        var allText = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var text in allText)
        {
            foreach (var kvp in mapping)
            {
                if (!text.text.Contains(kvp.Key)) continue;

                var lse = text.gameObject.GetComponent<LocalizeStringEvent>()
                    ?? text.gameObject.AddComponent<LocalizeStringEvent>();
                lse.StringReference = new UnityEngine.Localization.LocalizedString(table, kvp.Value);

                // Wire the listener through the public UnityEventTools API. The persistent-call
                // fields could be written directly through SerializedObject instead, but those
                // are private serialized names with no compatibility guarantee — and it isn't
                // necessary. A delegate to Text's public `text` setter, handed to
                // AddPersistentListener, serializes to exactly the same thing: target = the Text
                // component, method = set_text, mode = EventDefined (dynamic), call state =
                // EditorAndRuntime. Measured on Unity 6000.5.7f1.
                //
                // The setter has no C# method-group name, so the delegate is built by name.
                // That is reflection over a *public* member, which is fine; reaching for the
                // private m_* fields would not be.
                var setText = (UnityAction<string>)System.Delegate.CreateDelegate(
                    typeof(UnityAction<string>), text, "set_text");

                for (int i = lse.OnUpdateString.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(lse.OnUpdateString, i);
                }
                UnityEventTools.AddPersistentListener(lse.OnUpdateString, setText);

                EditorUtility.SetDirty(lse);
                break;
            }
        }
    }
}
