## What this skill does and does not automate

Every `AudioMixer` in the Editor is really an `AudioMixerController`, and every `AudioMixerGroup` is
an `AudioMixerGroupController`. Those two controller types are **not public**, and the authoring
calls — creating a mixer, creating a group, re-parenting a group, changing a group's volume — exist
only on them.

This skill deliberately does not use them. Unity makes no stability commitment for non-public API,
so a skill built on one can break silently between versions: the call fails at runtime rather than
at compile time, and the user cannot tell that from a Unity bug.

What matters is that the split is favorable. Each of those controllers derives from a public runtime
type — `UnityEngine.Audio.AudioMixer` and `UnityEngine.Audio.AudioMixerGroup` respectively — and
that public base is what every read and write below goes through. So **everything this skill needs
in order to inspect a mixer and route audio into it is public API**:

| Operation | Route |
|---|---|
| Find the project's mixers | public — `AssetDatabase` + `UnityEngine.Audio.AudioMixer` |
| List a mixer's groups | public — `AudioMixer.FindMatchingGroups` |
| Read an Audio Source's current group | public — `AudioSource.outputAudioMixerGroup` |
| Assign a group to an Audio Source | public — `AudioSource.outputAudioMixerGroup` |
| **Create a mixer or a group, change a group volume** | **not available** — the user does this in the Audio Mixer window |

So the division of labor is: the skill inventories what exists, proposes the routing, asks the user
to add any missing groups (two clicks in a window they already have open), and then does all the
routing itself. The tedious part — walking dozens of Audio Sources and classifying them — is the
part that was worth automating anyway.

All snippets below are written for `unity command eval --code '<snippet>'`: fully qualified, no
`using` directives, returning their result rather than logging it.

## Inventory the project's mixers and their groups

```csharp
// Scope the search to Assets. Unscoped, FindAssets also walks read-only packages, so the
// inventory fills up with mixers the user did not author and cannot edit. Measured on one
// project: t:Material returned 81 unscoped against 9 under Assets, t:Shader 204 against 22.
// The only overloads are (string) and (string, string[] searchInFolders) — there is no
// SearchMode parameter.
var guids = UnityEditor.AssetDatabase.FindAssets("t:AudioMixer", new[] { "Assets" });
if (guids.Length == 0) { return "no AudioMixer assets under Assets/"; }

var rows = new System.Collections.Generic.List<string>();
foreach (var guid in guids)
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var mixer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(path);
    var groups = mixer.FindMatchingGroups("");
    var names = System.Linq.Enumerable.Select(groups, g => g.name);
    rows.Add($"{path}  ({groups.Length} groups): {string.Join(", ", names)}");
}
return string.Join("\n", rows);
```

`FindMatchingGroups("")` returns every group in the mixer as the public `AudioMixerGroup` type,
which is what routing needs. It returns a flat list — it does not describe the parent/child shape.
If the hierarchy matters, read it off the Audio Mixer window with the user rather than reaching for
the non-public tree API.

## Read what the scene's Audio Sources are currently routed to

```csharp
var sources = UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(
    UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);

var rows = new System.Collections.Generic.List<string>();
foreach (var source in sources)
{
    var group = source.outputAudioMixerGroup;
    rows.Add($"{source.gameObject.name}: clip={(source.clip != null ? source.clip.name : "<none>")}, "
           + $"group={(group != null ? group.name : "<none — routes to Master>")}");
}
return rows.Count == 0 ? "no Audio Sources in the open scene" : string.Join("\n", rows);
```

Inactive objects are included on purpose: a disabled Audio Source still ships with the scene and
still needs routing.

## Assign groups to Audio Sources

This is the one write this skill performs. `AudioSource.outputAudioMixerGroup` is typed as the
public `AudioMixerGroup`, and the objects returned by `FindMatchingGroups` are assignable to it, so
there is no cast and no reflection.

**Key the mapping on whichever identifier you actually classified by.** Step 2 classifies from the
**clip asset name** first and falls back to the GameObject name, so the mapping has to accept either
— keying it on GameObject name alone silently drops every source you classified by its clip.

```csharp
var mixer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(
    "Assets/Audio/TheMixer.mixer");

// Keys may be a clip asset name or a GameObject name — whichever you classified from.
var assignments = new System.Collections.Generic.Dictionary<string, string> {
    { "FootStep4_Sound", "Foley" },   // clip name
    { "MenuMusic",       "Music" },   // GameObject name
};

var sources = UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(
    UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);

var done = new System.Collections.Generic.List<string>();
var noSuchGroup = new System.Collections.Generic.List<string>();
var unassigned = new System.Collections.Generic.List<string>();

UnityEditor.Undo.IncrementCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("Route Audio Sources to mixer groups");

foreach (var source in sources)
{
    var clipName = source.clip != null ? source.clip.name : null;
    string wanted = null;
    if (clipName != null) { assignments.TryGetValue(clipName, out wanted); }
    if (wanted == null) { assignments.TryGetValue(source.gameObject.name, out wanted); }

    if (wanted == null)
    {
        // Never skip silently — an unmatched source is a result the user has to see.
        unassigned.Add($"{source.gameObject.name} (clip={clipName ?? "<none>"})");
        continue;
    }

    var group = System.Array.Find(mixer.FindMatchingGroups(""), g => g.name == wanted);
    if (group == null) { noSuchGroup.Add($"{source.gameObject.name} -> {wanted}"); continue; }

    UnityEditor.Undo.RegisterCompleteObjectUndo(source, "Route Audio Source");
    source.outputAudioMixerGroup = group;
    UnityEditor.EditorUtility.SetDirty(source);
    done.Add($"{source.gameObject.name} -> {group.name}");
}

UnityEditor.Undo.FlushUndoRecordObjects();
UnityEditor.Undo.CollapseUndoOperations(UnityEditor.Undo.GetCurrentGroup());

var report = $"routed {done.Count}: {string.Join(", ", done)}";
if (noSuchGroup.Count > 0) { report += $"\nNO SUCH GROUP (create it first): {string.Join(", ", noSuchGroup)}"; }
if (unassigned.Count > 0) { report += $"\nNOT IN THE MAPPING (unrouted): {string.Join(", ", unassigned)}"; }
return report;
```

Three things to carry through to the user:

- **The scene changed, not the mixer asset.** `outputAudioMixerGroup` lives on the Audio Source, so
  the routing only persists once the scene is saved
  (`UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes()`). Say so rather than assuming.
- **Report the `NO SUCH GROUP` list explicitly.** A group the user hasn't created yet is the normal
  case in this flow, not an error to swallow. Show it and ask them to add those groups.
- **Report `NOT IN THE MAPPING` too.** Those are sources your classification missed. Reporting
  "routed 4" while three sources were quietly skipped is the worst outcome available here, because it
  reads as success.

## Asking the user to add a group

There is no supported programmatic route, so hand over precisely rather than vaguely:

> In the Audio Mixer window (Window → Audio → Audio Mixer), select **TheMixer**, then click the
> **+** next to Groups and name the new group **SFX**. Drag it under Master if it isn't already.
> Tell me when it's there and I'll route the sources.

Then re-run the inventory snippet to confirm the group exists before routing — don't assume the
user did it, and don't assume they spelled it the way you asked.
