# AudioRandomContainer API Reference

## Table of Contents
- [Architecture](#architecture)
  - [Key Types](#key-types)
  - [Assignment to AudioSource](#assignment-to-audiosource)
  - [Scope Restrictions](#scope-restrictions)
- [Asset Creation](#asset-creation)
  - [Construction](#construction)
  - [Saving](#saving)
- [Getting and Setting Properties](#getting-and-setting-properties)
  - [Access Methods](#access-methods)
  - [Adding Elements (Clips)](#adding-elements-clips)
  - [Saving After Property Changes](#saving-after-property-changes)
  - [Conditional Property Requirements](#conditional-property-requirements)
- [Code Example](#code-example)

## Architecture

### Key Types
- **`AudioRandomContainer`**: Implements `IAudioGenerator` for dynamic audio playback
- **`AudioContainerElement`**: Holds an `AudioClip` reference plus additional properties (not yet accessible via API)

### Assignment to AudioSource
- ✅ **DO**: Use `AudioSource.generator` to assign the container
- ❌ **DON'T**: Use `AudioSource.resource` (will be deprecated)

### Scope Restrictions
- `AudioRandomContainer` is **internal only** - reach it from C# running inside the Editor
- **Never reference** this type in generated project scripts (causes compilation errors)
- **Always import** `UnityEngine.Audio` namespace when working with this type

## Asset Creation

### Construction
- ✅ **DO**: Use standard C# construction: `new AudioRandomContainer()`
- ❌ **DON'T**: Use reflection or `ScriptableObject.CreateInstance`
- Proper initialization is handled in the native backing layer

### Saving
- **Required**: Save to disk immediately after creation using:
  ```csharp
  AssetDatabase.CreateAsset(container, path);
  AssetDatabase.SaveAssetIfDirty(container);
  ```

## Getting and Setting Properties

### Access Methods
- ✅ **DO**: Access all properties through the API
- ❌ **DON'T**: Use reflection or `SerializedObject`/`SerializedProperty`

### Adding Elements (Clips)
- ✅ **DO**: Use `container.AddElements(clipArray)` or `AudioRandomContainerUtilities.AddElements()`
- ❌ **DON'T**: Set the `elements` property directly

### Saving After Property Changes
- ❌ **DON'T**: Save the asset to disk after setting properties
- Leave saving for the user to do manually

### Conditional Property Requirements
These properties only take effect when their corresponding "enabled" flag is true:

| Property | Requires This Flag Set to `true` |
|----------|----------------------------------|
| `volumeRandomizationRange` | `volumeRandomizationEnabled` |
| `pitchRandomizationRange` | `pitchRandomizationEnabled` |
| `automaticTriggerTimeRandomizationRange` | `automaticTriggerTimeRandomizationEnabled` |
| `loopCountRandomizationRange` | `loopCountRandomizationEnabled` | 

## Code Example

```csharp
// ============================================================================
// STEP 1: Create the asset
// ============================================================================
var container = new AudioRandomContainer();

// ============================================================================
// STEP 2: Save to disk (REQUIRED immediately after creation)
// ============================================================================
var path = "Assets/New Audio Random Container.asset";
path = AssetDatabase.GenerateUniqueAssetPath(path);
AssetDatabase.CreateAsset(container, path);
AssetDatabase.SaveAssetIfDirty(container);

// ============================================================================
// STEP 3: Add audio clips
// ============================================================================

// Option A: Add empty elements (no clips assigned yet)
container.AddElements(2);

// Option B: Add elements with clips assigned
const string clip1Path = "Assets/clip1.wav";
const string clip2Path = "Assets/clip2.wav";
var clip1 = AssetDatabase.LoadAssetAtPath<AudioClip>(clip1Path);
var clip2 = AssetDatabase.LoadAssetAtPath<AudioClip>(clip2Path);
container.AddElements(new[] { clip1, clip2 });

// ============================================================================
// STEP 4: Set properties (ONLY if requested by user)
// ============================================================================

// Volume settings
container.volume = -20; // Range: [-80, 0], unit: decibels
container.volumeRandomizationEnabled = true; // Required for range to work
container.volumeRandomizationRange = new Vector2(-10, 10); // Range: [-80, 80], unit: decibels

// Pitch settings
container.pitch = 500; // Range: [-1200, 1200], unit: cents
container.pitchRandomizationEnabled = true; // Required for range to work
container.pitchRandomizationRange = new Vector2(-100, 100); // Range: [-1200, 1200], unit: cents

// Playback settings
container.playbackMode = AudioRandomContainerPlaybackMode.Random;
container.avoidRepeatingLast = 0; // Only for Random mode; must be < number of clips

// Trigger settings
container.triggerMode = AudioRandomContainerTriggerMode.Automatic;
container.automaticTriggerMode = AudioRandomContainerAutomaticTriggerMode.Pulse;
container.automaticTriggerTime = 2f; // Unit: seconds
container.automaticTriggerTimeRandomizationEnabled = true; // Required for range to work
container.automaticTriggerTimeRandomizationRange = new Vector2(1f, 3f); // Range: [-60, 60]

// Loop settings
container.loopMode = AudioRandomContainerLoopMode.Clips;
container.loopCount = 5; // Disabled if loop mode is Infinite
container.loopCountRandomizationEnabled = true; // Required for range to work
container.loopCountRandomizationRange = new Vector2(3, 5); // Range: [-10, 10]

```
