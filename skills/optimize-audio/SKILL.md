---
name: optimize-audio
description: Optimizes Unity 6 audio memory, CPU cost, and playback quality through correct import settings and mixer configuration. Use when the user wants to reduce audio memory usage, choose the right Load Type for short clips versus music versus ambient beds, configure platform-appropriate sample rates and codecs, force 3D audio to mono, or reduce AudioMixer CPU cost from deep group trees or effects running on silent paths.
---
## Critical Rules

- Do not make changes before reporting findings to the user
- Follow steps in strict order; never jump ahead
- STOP at every `WAIT` checkpoint and await the user's response before continuing
- Quality is more important than speed: measure before and after every change
- Always verify results in a device build; Editor audio stats are indicative only

## 0. Set up the execution path

Every C# step below runs inside a live Editor through the Unity CLI. **The `unity-cli` skill owns
getting you there** — installing the CLI, confirming a connected Editor, adding the project's
`com.unity.pipeline` package, telling a genuinely absent Editor apart from one stuck in Safe Mode,
and discovering the Editor's command catalog. Follow it first; don't re-derive any of it here.

Two things it can't know for you:

- **You need `eval` in particular**, not just a reachable Editor. Confirm it appears in the
  catalog. Its presence depends on the Pipeline package version, not on the CLI, so a healthy
  install can still lack it — if it's missing, say so and stop.
- **Do not hand-edit `.meta` files to change import settings.** Importer values only take effect
  through `SaveAndReimport()` in a live Editor, so an unreachable Editor is a stop, not a cue to
  edit metadata directly.

Run C# with `unity command eval --code '<snippet>'`. Discover the parameter shape from
`unity command --format json` rather than assuming one. `unity command` defaults to a 30 second
timeout.

### Passing C# to `eval`

`eval` compiles a **statement block, not a file**. Two consequences, both of which cause a compile
error rather than a warning:

- **No `using` directives.** The compiler reads `using UnityEngine;` as a resource-disposal
  statement and rejects it (`CS0210`).
- **Types must be fully qualified.** A bare `AssetDatabase` or `AudioImporter` does not resolve
  (`CS0246` / `CS0103`), and a bare `Object` is ambiguous with `object` (`CS0104`).

The recipes in [resources/audio-import-api.md](resources/audio-import-api.md) are written
fully qualified so they can be passed to `eval` as-is.

## 1. Pre-Flight: Detect Audio System

Before doing anything else, establish the audio environment:

1. **Detect platform and sample rate:** Use `eval` to read `EditorUserBuildSettings.activeBuildTarget` and `AudioSettings.outputSampleRate`. The output sample rate affects whether overriding clip sample rates will actually save memory.
2. **Detect AudioMixer presence:** Use the mixer-asset query recipe in [resources/audio-import-api.md](resources/audio-import-api.md) to see if a mixer graph exists. If none exists, note that routing and effect costs are not a concern.
3. **Detect AudioListener:** Use the scene-component query recipe in [resources/audio-import-api.md](resources/audio-import-api.md) for `UnityEngine.AudioListener` to confirm exactly one listener is present. Multiple listeners produce incorrect spatialization; zero listeners produce silence.
4. **Proceed** only after platform and listener state are confirmed.

## 2. Assess Current State

Before recommending any change, gather observable data:

1. **Find all AudioSources:** Use the scene-component query recipe in [resources/audio-import-api.md](resources/audio-import-api.md) for `UnityEngine.AudioSource`. For each result, use **one** `eval` call to batch-read properties — see the batch read recipe in [resources/audio-import-api.md](resources/audio-import-api.md).
2. **Inspect mixer topology:** If a mixer was found in Pre-Flight, use `eval` to read the AudioMixer's exposed parameters and group count. A group count above ~8 or effects on the Master group are immediate flags.
3. **Check DSP buffer size:** Use the DSP buffer recipe in [resources/audio-import-api.md](resources/audio-import-api.md) to read buffer size. See DSP Buffer Size Guidelines in [resources/platform-settings.md](resources/platform-settings.md) for recommended values.
4. **Report findings before making changes:** Summarize ALL detected sources, the listener count, and mixer depth to the user. Flag any immediate risks (e.g., stereo clip with `spatialBlend = 1`, Decompress On Load on a clip > 1 MB, reverb on the Master group).

**WAIT for the user to review the assessment before proceeding.**

## 3. Understand Request

Route to the correct section based on what the user needs:

| User Says | Path |
|-----------|------|
| "audio memory too high" / "memory profiler shows audio" | Section 4 — Import settings audit |
| "load times slow" / "decompression stall" | Section 4 — Load Type review |
| "DSP spike" / "mixer CPU" / "audio CPU high" | Section 4B — Mixer audit |
| "3D sound wrong" / "only left channel plays" / "stereo in 3D" | Section 4A — Force To Mono + spatial settings |
| "quality artifacts" / "voice sounds bad" / "Vorbis crackling" | Section 4C — Compression quality tuning |
| "mobile audio battery" / "mobile memory" | Section 4D — Mobile sample rate override |
| "set import settings on all clips" / "batch audio settings" | Section 4 — Bulk import audit |
| "streaming" / "background loading" / "Addressables audio" | Section 4E — Streaming and async load |

If the symptom is ambiguous, ask: "Is the problem audio memory usage, DSP CPU spikes, or audio playback quality?"

## 4. Primary Diagnostic Workflow

Use the findings from Section 2 to determine which sub-section applies. More than one may apply simultaneously.

### 4A. Force To Mono and Spatial Settings

For any AudioSource where `spatialBlend > 0` (3D positioned sound):

1. **Check clip channel count:** Use `eval` to read `audioSource.clip.channels`. If `channels == 2` and `spatialBlend == 1`, only the left channel plays — this is a bug, not a feature.
2. **Recommend Force To Mono:** Use the read importer recipe in [resources/audio-import-api.md](resources/audio-import-api.md) to inspect current settings, then apply Force To Mono using the force-to-mono recipe.
3. **Apply and reimport:** Report before/after channel counts to the user.
4. **Verify spatial blend:** Use `eval` to confirm `audioSource.spatialBlend` is `1.0` (full 3D) and `audioSource.rolloffMode` is set to an appropriate curve.

### 4B. AudioMixer Audit

1. **Measure group depth:** Use `eval` to walk the mixer's group tree and count levels. More than 3 levels (Master → SFX / Music / Voice → sub-bus) adds routing overhead every frame, even when children are silent.
2. **Check effects on silent groups:** Use `eval` to query each group's effects list. Effects such as `AudioReverbFilter` run their DSP at full cost even when no AudioSource routes to that group.
3. **Flag SFX Reverb on parent groups:** This is the most expensive built-in effect. If found on the Master or a high-level group, flag it explicitly.
4. **Present recommendations to the user:**
   - Remove or bypass effects on groups that have no active sources.
   - Use **snapshots** to switch mix states (combat / explore / pause) rather than toggling effects at runtime.
   - Flatten unnecessary sub-buses; redirect sources to a shallower ancestor.

   **WAIT for the user to approve the mixer changes before applying.**

5. **Verify DSP buffer size:** If `bufferLength` from Pre-Flight is very small (< 256), recommend increasing it — see DSP Buffer Size Guidelines in [resources/platform-settings.md](resources/platform-settings.md).

### 4C. Compression Quality Tuning

1. **Read current compression format:** Use the read importer recipe in [resources/audio-import-api.md](resources/audio-import-api.md) to read `compressionFormat` and `quality` for the clips reported by the user.
2. **Apply the platform matrix:** See the Compression Format Matrix in [resources/platform-settings.md](resources/platform-settings.md) for per-platform recommendations.
3. **Warn about lossy sources:** Use the lossy source check recipe in [resources/audio-import-api.md](resources/audio-import-api.md). If the original file is MP3, warn the user that lossy source quality is lost permanently after Unity re-encodes. Recommend WAV or AIFF sources.

### 4D. Mobile Sample Rate Override

1. **Identify SFX clips on mobile target:** Use the scene-component query recipe for `UnityEngine.AudioSource` and filter for non-music, non-dialogue clips.
2. **Read current sample rate setting:** Use the read importer recipe in [resources/audio-import-api.md](resources/audio-import-api.md) to read `sampleRateSetting` and `sampleRateOverride` for each clip.
3. **Apply mobile override:** Use the sample rate override recipe in [resources/audio-import-api.md](resources/audio-import-api.md). See Sample Rate Recommendations in [resources/platform-settings.md](resources/platform-settings.md) for per-use-case rates.
4. **Report savings:** Halving the sample rate halves the PCM memory cost. Report the estimated saving for each clip changed.

### 4E. Load Type and Streaming

1. **Audit Load Type per clip:** Use `eval` to read `clip.loadType` for each clip found in Section 2.
2. **Apply the decision rule:** See Load Type Decision Table in [resources/platform-settings.md](resources/platform-settings.md).
3. **Flag mismatches:** See Load Type Mismatch Flags in [resources/platform-settings.md](resources/platform-settings.md). Report both types of mismatches to the user.
4. **Apply `Load In Background`** for any Streaming clip — use the Load In Background recipe in [resources/audio-import-api.md](resources/audio-import-api.md).

## 5. Validation

After any import setting or mixer change:

1. **Re-read clip stats:** Use `eval` to re-read `clip.loadType`, `clip.channels`, `AudioSettings.outputSampleRate`, and the importer's `compressionFormat` to confirm the change applied after reimport.
2. **Confirm AudioSource routing:** Use the scene-component query recipe for `UnityEngine.AudioSource` and verify `audioSource.outputAudioMixerGroup` is assigned as expected after any mixer restructure.
3. **Report delta:** State the before and after values for each setting changed. Do not assume the change was effective without reading back the applied importer values.
4. **Iterate limit:** Maximum 3 adjust-and-verify cycles before pausing to ask the user for feedback.

## 6. Troubleshooting

### Stereo clip on a 3D AudioSource — only left channel audible

1. Confirm `audioSource.spatialBlend == 1`.
2. Confirm `audioSource.clip.channels == 2`.
3. Enable `forceToMono` in the AudioClip importer and reimport. Unity mixes both channels to mono during import, preserving level with `normalize = true` (keep on).
4. If the user does not want to reimport: set `audioSource.panStereo = 0` as a runtime workaround, but warn this does not recover stereo information.

### Decompress On Load clip causes memory spike

1. Confirm `clip.loadType == AudioClipLoadType.DecompressOnLoad` and `clip.length` is long (> 5 s).
2. Switch to `Streaming` if it is music or ambience, `CompressedInMemory` if played only occasionally.
3. If the clip is short but still large: check `clip.channels` (stereo wastes double the memory) and `clip.frequency` (high sample rate on a mobile target wastes memory). Apply Force To Mono and/or sample rate override.

### AudioMixer CPU spike — DSP thread hot

1. Confirm with the mixer-asset query recipe that the mixer graph exists.
2. Use `eval` to list all groups and their attached effects. Look for reverb, chorus, or EQ on high-level groups.
3. Move expensive effects down to leaf groups that are only active when sources are playing.
4. Use snapshots to bypass effect chains during gameplay states where they are not heard (e.g., bypass reverb during a menu).
5. If the DSP buffer is small (64 or 128 samples), raise it — see DSP Buffer Size Guidelines in [resources/platform-settings.md](resources/platform-settings.md).

### Vorbis quality artifacts on dialogue

1. Confirm `defaultSampleSettings.compressionFormat == AudioCompressionFormat.Vorbis`.
2. Confirm `defaultSampleSettings.quality` — default is 0.5, which is often audible on voice. Raise to 0.7–0.85.
3. On iOS: switch to AAC instead of Vorbis (hardware decode, better quality at equivalent bitrate).
4. Confirm the source file is lossless (WAV or AIFF). MP3 sources cannot recover quality lost before Unity's re-encode.

### AudioListener count is not exactly one

- **Zero listeners:** All audio will be silent. Use `eval` to add an `AudioListener` component to the main camera: `UnityEngine.Camera.main.gameObject.AddComponent<UnityEngine.AudioListener>()`.
- **Multiple listeners:** Unity uses the last enabled one, producing unpredictable spatialization. Use the scene-component query recipe for `UnityEngine.AudioListener` and disable all but the intended one.

### `Load In Background` causes first-play silence

This is expected behavior: the clip has not finished loading when `Play()` is first called. Mitigate with:
1. Preload the clip at scene start by calling `clip.LoadAudioData()` before it is needed.
2. Use `AudioSource.PlayScheduled()` with a slight delay to allow async load to complete.
3. For AudioSources that must play immediately: switch to `CompressedInMemory` (synchronous on first play) rather than `Streaming` with background load.

## 7. Completion

After finishing the audit or optimization:

- Summarize every setting changed with before/after values.
- List any clips or groups that still need attention (e.g., clips that require on-device measurement to confirm savings).
- If the user needs runtime memory measurement, point them at the Memory Profiler package, which reports the largest AudioClips by runtime byte cost.
- If mixer CPU is still high after the audit, point them at the Unity Profiler's Audio module for DSP thread profiling.

## Detailed References

- **Platform settings, compression matrix, load types, sample rates:** [resources/platform-settings.md](resources/platform-settings.md)
- **AudioImporter API recipes and code patterns:** [resources/audio-import-api.md](resources/audio-import-api.md)

## See Also

- **Memory Profiler package** — finds the largest AudioClips by runtime byte cost.
- **Unity Profiler, Audio module** — DSP CPU markers and frame-time budget.
- `audio-setup-mixers` — creating mixers and routing Audio Sources into groups.
