# Audio Import API Recipes

C# code recipes for `unity command eval --code '<snippet>'`. All examples target the Unity 6
AudioImporter API.

`eval` compiles a statement block, so there are no `using` directives and every type is written
fully qualified. Each recipe `return`s its result as a string rather than calling `Debug.Log`, so
the value comes back on the CLI's stdout instead of only reaching the Editor console.

## Read AudioClip Importer Settings

```csharp
var path = UnityEditor.AssetDatabase.GetAssetPath(audioSource.clip);
var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
return $"forceToMono={importer.forceToMono}, loadType={importer.defaultSampleSettings.loadType}, " +
          $"compressionFormat={importer.defaultSampleSettings.compressionFormat}, " +
          $"quality={importer.defaultSampleSettings.quality}, " +
          $"sampleRateSetting={importer.defaultSampleSettings.sampleRateSetting}, " +
          $"sampleRateOverride={importer.defaultSampleSettings.sampleRateOverride}");
```

## Force To Mono and Reimport

```csharp
var path = UnityEditor.AssetDatabase.GetAssetPath(audioSource.clip);
var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.forceToMono = true;
importer.SaveAndReimport();
return $"Reimported {path} — channels now: {audioSource.clip.channels}");
```

## Set Load Type

```csharp
var path = UnityEditor.AssetDatabase.GetAssetPath(audioSource.clip);
var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
var settings = importer.defaultSampleSettings;
settings.loadType = UnityEngine.AudioClipLoadType.Streaming; // or CompressedInMemory, DecompressOnLoad
importer.defaultSampleSettings = settings;
importer.SaveAndReimport();
```

## Enable Load In Background

```csharp
var path = UnityEditor.AssetDatabase.GetAssetPath(audioSource.clip);
var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.loadInBackground = true;
importer.SaveAndReimport();
```

## Set Compression Format and Quality

```csharp
var path = UnityEditor.AssetDatabase.GetAssetPath(audioSource.clip);
var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
var settings = importer.defaultSampleSettings;
settings.compressionFormat = UnityEngine.AudioCompressionFormat.Vorbis;
settings.quality = 0.7f; // 0.0–1.0; raise to 0.7–0.85 for dialogue
importer.defaultSampleSettings = settings;
importer.SaveAndReimport();
```

## Override Sample Rate (Mobile)

```csharp
var path = UnityEditor.AssetDatabase.GetAssetPath(audioSource.clip);
var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
var settings = importer.defaultSampleSettings;
settings.sampleRateSetting = UnityEditor.AudioSampleRateSetting.OverrideSampleRate;
settings.sampleRateOverride = 22050u;
importer.defaultSampleSettings = settings;
importer.SaveAndReimport();
```

## Read AudioSource Properties (Batch)

Read multiple properties in a single `eval` call:

```csharp
var src = audioSource;
return $"clip={src.clip?.name}, loadType={src.clip?.loadType}, " +
          $"channels={src.clip?.channels}, frequency={src.clip?.frequency}, " +
          $"spatialBlend={src.spatialBlend}, rolloff={src.rolloffMode}, " +
          $"mixerGroup={src.outputAudioMixerGroup?.name ?? "None"}, " +
          $"bypassEffects={src.bypassEffects}");
```

## Read DSP Buffer Size

```csharp
UnityEngine.AudioSettings.GetDSPBufferSize(out int bufferLength, out int numBuffers);
return $"DSP buffer: {bufferLength} samples x {numBuffers} buffers");
```

## Check Source File Format (Lossy Warning)

```csharp
var path = UnityEditor.AssetDatabase.GetAssetPath(clip);
if (path.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase))
    return $"'{clip.name}' is MP3 — lossy source quality is lost permanently after Unity re-encodes. Recommend WAV or AIFF sources.");
```

## Resolving `audioSource` / `clip` inside a snippet

The recipes above are written against an `audioSource` or `clip` variable. `eval` runs each
snippet in a fresh scope, so nothing carries over between calls — resolve the object at the top of
the same snippet that uses it.

By scene object:

```csharp
var sources = UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(
    UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
var audioSource = System.Array.Find(sources, s => s.gameObject.name == "TheGameObjectName");
```

By asset path, when you already know the clip:

```csharp
var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/Audio/Foo.wav");
```

## Enumerate scene components

Substitute the component type (`UnityEngine.AudioSource`, `UnityEngine.AudioListener`). Inactive
objects are included deliberately — a disabled second listener still counts against the
one-listener rule.

```csharp
var found = UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(
    UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
var names = System.Linq.Enumerable.Select(found, c => c.gameObject.name);
return $"count={found.Length}: {string.Join(", ", names)}";
```

## Enumerate mixer assets

An `AudioMixer` is a project asset, not a scene object, so it is found through the asset database
rather than a scene query.

```csharp
// Scope to Assets. Unscoped, FindAssets also walks read-only packages and reports mixers the
// user did not author. The second parameter is string[] searchInFolders; there is no SearchMode
// overload. Measured on one project: t:Material returned 81 unscoped against 9 under Assets.
var guids = UnityEditor.AssetDatabase.FindAssets("t:AudioMixer", new[] { "Assets" });
var paths = System.Linq.Enumerable.Select(guids, UnityEditor.AssetDatabase.GUIDToAssetPath);
return $"count={guids.Length}: {string.Join(", ", paths)}";
```
