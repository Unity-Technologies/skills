---
name: setup-audiorandomcontainer
description: Creates AudioRandomContainer assets with randomized audio playback. Use for dynamic sounds from multiple clips with variation of volume, pitch and playback timing.
required_editor_version: ">=6000.3.13"
---
# Set up AudioRandomContainer

## When to use this skill
Use when the user wants to:
- Create dynamic audio that randomizes between multiple clips
- Set up audio with randomized volume, pitch, or timing
- Configure automatic or triggered audio playback with variation

## DO's
- Use the internal API from `UnityEngine.Audio` and `UnityEditor.Audio` namespaces
- Use standard C# construction: `new AudioRandomContainer()`
- Use `AudioRandomContainerUtilities.AddElements()` or `container.AddElements()` to add clips
- Save the asset immediately after creation using `AssetDatabase.SaveAssetIfDirty()`
- Default filename: "New Audio Random Container.asset" (auto-enumerate if exists)

## DON'Ts
- ❌ Never use reflection
- ❌ Never use `ScriptableObject.CreateInstance`
- ❌ Never use `SerializedObject`/`SerializedProperty`
- ❌ Do not save to disk after setting properties (leave for user)
- ❌ Do not auto-play `AudioSource` after assigning container unless explicitly requested 

## Procedure
1. Create the container: `var container = new AudioRandomContainer()`
2. Generate unique path and save to disk with `AssetDatabase.CreateAsset()` and `AssetDatabase.SaveAssetIfDirty()`
3. **Only if requested**: Add audio clips using `container.AddElements(clipArray)`
4. **Only if requested**: Update properties using the API (see references)

## References
See [references/api.md](references/api.md)