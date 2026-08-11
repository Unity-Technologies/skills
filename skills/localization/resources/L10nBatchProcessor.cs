using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
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

                // Use SerializedObject to set dynamic binding mode (Mode 0).
                // UnityEventTools.AddPersistentListener cannot reliably set Mode 0.
                var so = new SerializedObject(lse);
                var calls = so.FindProperty("m_UpdateString.m_PersistentCalls.m_Calls");
                calls.ClearArray();
                calls.InsertArrayElementAtIndex(0);
                var call = calls.GetArrayElementAtIndex(0);
                call.FindPropertyRelative("m_Target").objectReferenceValue = text;
                call.FindPropertyRelative("m_MethodName").stringValue = "set_text";
                call.FindPropertyRelative("m_Mode").enumValueIndex = 0;       // Dynamic
                call.FindPropertyRelative("m_CallState").enumValueIndex = 2;  // EditorAndRuntime
                so.ApplyModifiedProperties();
                break;
            }
        }
    }
}
