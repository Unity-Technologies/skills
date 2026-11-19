---
name: optimize-audio
description: Optimizes audio memory, CPU cost, and playback quality through import settings and mixer configuration. Use when asked to reduce audio memory or Audio Mixer CPU cost, change clip Load Types, sample rates or codecs, or force 3D audio to mono.
---
## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline so you can tell whether a later result is caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for.
- Avoid long explanations.
- Do not make changes before reporting findings to the user, because import and mixer settings apply to every scene that uses the clip or mixer.
- Follow steps in strict order; never jump ahead, because each step uses what the previous one found.
- STOP at every `WAIT` checkpoint and await the user's response before continuing, because the user decides which changes to make.
- Always verify results in a device build; Editor audio stats are indicative only

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Read the [documentation page(s)](#full-topic-map) covering what you intend to do or fix, and check if you've missed anything.
2. Name the file(s) you read in your response.

Your training knowledge about Unity may be out of date, incorrect, or for the wrong version.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com** (the `xrefmap.yml` and `ScriptReference` URLs): The Editor version, for example `6000.0`. This isn't a package version.
- **docs.unity.com**: The product version, eg `7000.0`

Read the Unity version from `ProjectSettings/ProjectVersion.txt`.

If `<editor-install>/Editor/Data/Documentation/en/Manual` exists, read the local html file named below from it. Otherwise fetch the online `.md` page.

- **Audio Clip Import Settings reference** — `class-AudioClip.html`, [online](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/reference/class-audio-clip.md): every clip import setting.
- **Audio file compression** — `AudioFiles-compression.html`, [online](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/files/compression.md): size, CPU and quality cost of each format.
- **Audio settings** — `class-AudioManager.html`, [online](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity-editor/editor-settings-reference/comp-manager-group/class-audio-manager.md): DSP Buffer Size.
- **Audio Mixer** — `AudioMixer.html`, [online](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/mixer.md): mixer groups, effects and snapshots.
- **Audio Mixer inspectors** — `AudioMixerInspectors.html`, [online](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/mixer/inspectors.md): effect CPU display, Auto Mixer Suspend.
- **Audio Source** — `class-AudioSource.html`, [online](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/reference/class-audio-source.md): Spatial Blend, rolloff, Output.
- **Audio Listener** — `class-AudioListener.html`, [online](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/reference/class-audio-listener.md): listener behaviour.
- **Audio Profiler module** — `ProfilerAudio.html`, [online](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/profiler.md): measuring audio memory and DSP CPU.

### API reference

The API is `UnityEditor.AudioImporter` and the `UnityEngine` audio classes, part of the Unity Engine. It is versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If `<editor-install>/Editor/Data/Documentation/en/ScriptReference` exists, grep its html files and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml`. This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEditor.AudioImporter.forceToMono'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.AudioClip.LoadAudioData` => `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/audioclip/loadaudiodata.md`. All overloads are on the same page.
   - Constructors use `ctor`, and indexers use `item`. For operators, fetch the class page and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'AudioImporter.html'` => `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/AudioImporter.html`).

Always use the docs for the project's Unity version rather than relying on training knowledge, since APIs are frequently marked obsolete between versions. Pages this skill uses:

- [AudioImporter](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/audioimporter.md): `forceToMono`, `loadInBackground`.
- [AudioImporterSampleSettings](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/audioimportersamplesettings.md): per-platform sample settings.
- [AssetImporter.SaveAndReimport](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/assetimporter/saveandreimport.md): applying importer changes.
- [AudioSettings.GetDSPBufferSize](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/audiosettings/getdspbuffersize.md): DSP buffer size.

## 0. Set up the execution path

Every C# step below runs inside a live Editor through the Unity CLI. **The `unity-cli` skill owns
getting you there** — installing the CLI, confirming a connected Editor, adding the project's
`com.unity.pipeline` package, telling a genuinely absent Editor apart from one stuck in Safe Mode,
and discovering the Editor's command catalog. Follow it first; don't re-derive any of it here.

Two things it can't know for you:

- **You need `script eval` in particular**, not just a reachable Editor. Confirm it appears in the
  catalog. Its presence depends on the Pipeline package version, not on the CLI, so a healthy
  install can still lack it — if it's missing, say so and stop.
- **Do not hand-edit `.meta` files to change import settings.** Importer values only take effect
  through `SaveAndReimport()` in a live Editor, so an unreachable Editor is a stop, not a cue to
  edit metadata directly.

Run C# with `unity command script eval --code '<snippet>'`. Discover the parameter shape from
`unity command --format json` rather than assuming one. `script eval`'s default timeout depends on the
Pipeline version, so read it from that schema and pass `--timeout <seconds>` for a slow call.

### Passing C# to `script eval`

`script eval` compiles a **statement block, not a file**, so write the code without `using` directives and with fully qualified types. If the code doesn't compile, look up the error here:

| Error | Cause | Fix |
| --- | --- | --- |
| `CS0210` | A `using` directive, which the compiler reads as a resource-disposal statement | Remove the directive and qualify the types |
| `CS0246` / `CS0103` | An unqualified type, such as `AssetDatabase` or `AudioImporter` | Use the full name, such as `UnityEditor.AssetDatabase` or `UnityEditor.AudioImporter` |
| `CS0104` | A bare `Object`, which is ambiguous with `object` | Use `UnityEngine.Object` |

Every recipe named below is in [references/audio-import-api.md](references/audio-import-api.md),
written fully qualified so it can be passed to `script eval` as-is.

## 1. Pre-Flight: Detect Audio System

Before doing anything else, establish the audio environment:

1. **Detect platform and sample rate:** Use `script eval` to read `EditorUserBuildSettings.activeBuildTarget` and `AudioSettings.outputSampleRate`. Note that `AudioSettings.outputSampleRate` reflects the Editor host device's rate, not the target platform's rate — use it as a rough guide only and measure on-device to confirm. Clips above the output sample rate are usually resampled down when they play, so they gain no quality from the extra samples. Such clips should only be flagged for review, because they could be used for slow playback creatively.
2. **Detect AudioMixer presence:** Use the mixer-asset query recipe to see if a mixer graph exists. If none exists, note that routing and effect costs are not a concern.
3. **Detect AudioListener:** Use the scene-component query recipe for `UnityEngine.AudioListener` to confirm exactly one listener is present. The docs say each scene can only have one Audio Listener to work properly, and with none, nothing is heard. Offer to add one to the main camera, or disable extras.
4. **Proceed** only after platform and listener state are confirmed.

## 2. Assess Current State

Before recommending any change, gather observable data:

1. **Find all AudioSources:** Use the scene-component query recipe for `UnityEngine.AudioSource`. For each result, use **one** `script eval` call to batch-read properties — see the batch read recipe.
2. **Inspect mixer topology:** If a mixer was found in Pre-Flight, use `script eval` to count its groups with `AudioMixer.FindMatchingGroups(string.Empty)`. For the tree and effects, read the `.mixer` asset file as text, because the public `AudioMixer` API can't list groups or effects: each `AudioMixerGroupController` lists its `m_Children` and `m_Effects`, and each `AudioMixerEffectController` has an `m_EffectName`. If the file isn't text (Asset Serialization isn't Force Text), ask the user to describe the group tree and effects from the Audio Mixer window. Also read `m_EnableSuspend` and `m_SuspendThreshold` (Auto Mixer Suspend). An effect costs the same at any tree level and depth adds no cost, so look for effects repeated across groups.
3. **Check DSP buffer size:** Use the DSP buffer recipe to read buffer size. See DSP Buffer Size Guidelines in [references/platform-settings.md](references/platform-settings.md) for recommended values.
4. **Report findings before making changes:** Summarize ALL detected sources, the listener count, and mixer effects to the user. Flag any immediate risks (e.g., stereo clip with `spatialBlend = 1`, Decompress On Load on a long clip that will expand to a large PCM footprint, an effect duplicated across sibling groups, Auto Mixer Suspend off).

**WAIT for the user to review the assessment before proceeding.**

## 3. Understand Request

Route to the correct section based on what the user needs:

| User Says | Path |
|-----------|------|
| "audio memory too high" / "memory profiler shows audio" | Section 4 — Import settings audit |
| "load times slow" / "decompression stall" | Section 4 — Load Type review |
| "DSP spike" / "mixer CPU" / "audio CPU high" / "crackling" / "audio popping" / "dropouts" | Section 4B — Mixer audit (audio thread overload) |
| "3D sound wrong" / "only left channel plays" / "stereo in 3D" | Section 4A — Force To Mono + spatial settings |
| "quality sounds compressed" / "voice sounds muffled" | Section 4C — Compression quality tuning |
| "mobile audio battery" / "mobile memory" | Section 4D — Mobile sample rate override |
| "set import settings on all clips" / "batch audio settings" | Section 4 — Bulk import audit |
| "streaming" / "background loading" / "Addressables audio" | Section 4E — Streaming and async load |

If the symptom is ambiguous, ask: "Is the problem audio memory usage, DSP CPU spikes, or audio playback quality?"

## 4. Primary Diagnostic Workflow

Use the findings from Section 2 to determine which sub-section applies. More than one may apply simultaneously.

### 4A. Force To Mono and Spatial Settings

For any AudioSource where `spatialBlend > 0` (3D positioned sound):

1. **Check clip channel count and spread:** Use `script eval` to read `audioSource.clip.channels`, `audioSource.spatialBlend`, and `audioSource.spread`. If `channels == 2` and `spatialBlend == 1` and `spread == 0`, the second channel only costs memory: at Spread 0, a 3D source plays all channels from the same point, which is mono. If Spread > 0 or if `spatialBlend < 1` (partially 3D or fully 2D), the clip should remain stereo.
2. **Recommend Force To Mono:** Use the read importer recipe to inspect current settings. Before recommending Force To Mono, warn the user that it is a per-clip import setting applied to every platform — if the same clip is used as 2D on one platform and 3D on another, forcing it to mono removes stereo everywhere. Then recommend Force To Mono only for stereo clips on fully 3D sources (spatialBlend == 1) with zero Spread.

   **WAIT for the user to approve the changes before applying.**

3. **Apply and reimport:** Apply Force To Mono with the force-to-mono recipe, then report before/after channel counts to the user.
4. **Verify spatial blend:** Use `script eval` to confirm `audioSource.spatialBlend` is `1.0` (full 3D) and `audioSource.rolloffMode` is set to an appropriate curve.

### 4B. AudioMixer Audit

1. **Find duplicated effects:** List effects repeated across sibling groups. One instance on the shared parent is cheaper, but only for **linear** effects (see [references/troubleshooting.md](references/troubleshooting.md)), because a compressor or distortion on the combined signal changes the mix.
2. **Check Auto Mixer Suspend:** With `m_EnableSuspend: 0`, effects keep processing after all sources stop. When enabled, the mixer suspends once no source plays into it and its output is below `m_SuspendThreshold`, so flag sources that keep playing silently (such as a looping source at volume 0).
3. **Measure per-effect cost:** Don't guess which effect is most expensive. Ask the user to enable **Enable FMOD Profiling** (**Preferences** > **Diagnostics** > **Audio**), choose **Toggle CPU usage display** from an AudioGroup Inspector's gear menu, and report each effect's CPU.
4. **Present recommendations to the user:**
   - Apply duplicated linear effects once on their shared parent.
   - Enable Auto Mixer Suspend; a Threshold Volume above −80 dB suspends sooner.
   - Bypass effects the mix doesn't need.
   - Use **snapshots** to switch mix states (combat / explore / pause) rather than toggling effects at runtime. To stop effect processing via snapshot, enable Allow Wet Mixing on the effect and set its wet level to −80 dB in the target snapshot; a snapshot transition alone silences but does not stop processing.

   **WAIT for the user to approve the mixer changes before applying.**

   Make approved mixer changes in the Audio Mixer window, or ask the user to, because there's no public API for groups and effects and hand-editing the `.mixer` file can corrupt it.

5. **Verify DSP buffer size:** If `bufferLength` from Section 2 is very small (< 256), recommend increasing it — see DSP Buffer Size Guidelines in [references/platform-settings.md](references/platform-settings.md).

### 4C. Compression Quality Tuning

1. **Read current compression format:** Use the read importer recipe to read `compressionFormat` and `quality` for the clips reported by the user.
2. **Compare with the platform matrix:** See the Compression Format Matrix in [references/platform-settings.md](references/platform-settings.md) for per-platform recommendations.
3. **Warn about lossy sources:** Use the lossy source check recipe. If the original file is MP3, warn the user that lossy source quality is lost permanently after Unity re-encodes. Recommend WAV or AIFF sources.
4. **Recommend changes:** Report the format and quality you recommend for each clip.

   **WAIT for the user to approve the changes before applying.**

5. **Apply:** Use the compression format and quality recipe.

### 4D. Mobile Sample Rate Override

1. **Identify SFX clips on mobile target:** Use the scene-component query recipe for `UnityEngine.AudioSource` and filter for non-music, non-dialogue clips.
2. **Read current sample rate setting:** Use the read importer recipe to read `sampleRateSetting` and `sampleRateOverride` for each clip, and recommend a rate for each from Sample Rate Recommendations in [references/platform-settings.md](references/platform-settings.md).

   **WAIT for the user to approve the changes before applying.**

3. **Apply mobile override:** Apply the approved rates with the sample rate override recipe.
4. **Report savings:** Halving the sample rate halves the PCM memory cost. Report the estimated saving for each clip changed.

### 4E. Load Type and Streaming

1. **Audit Load Type per clip:** Use `script eval` to read `clip.loadType` for each clip found in Section 2.
2. **Compare with the decision rule:** See Load Type Decision Table in [references/platform-settings.md](references/platform-settings.md).
3. **Flag mismatches:** See Load Type Mismatch Flags in [references/platform-settings.md](references/platform-settings.md). Report both types of mismatches to the user, with the Load Type you recommend for each clip.

   **WAIT for the user to approve the changes before applying.**

4. **Apply:** Set each approved Load Type with the Set Load Type recipe. Use the Load In Background recipe only for Decompress On Load or Compressed In Memory clips whose load stalls the main thread; Streaming clips already load in the background.

## 5. Troubleshooting

Read [references/troubleshooting.md](references/troubleshooting.md) when the user reports one of these symptoms: a stereo clip on a 3D source, a memory spike from Decompress On Load, a DSP CPU spike, Vorbis artifacts on dialogue, or silence on first play with Load In Background.

## See Also

- `route-audio-to-mixer-groups` — creating mixers and routing Audio Sources into groups.

## Final step

After any import setting or mixer change:

1. **Re-read clip stats:** Use `script eval` to re-read `clip.loadType`, `clip.channels`, `AudioSettings.outputSampleRate`, and the importer's `compressionFormat` to confirm the change applied after reimport.
2. **Confirm AudioSource routing:** Use the scene-component query recipe for `UnityEngine.AudioSource` and verify `audioSource.outputAudioMixerGroup` is assigned as expected after any mixer restructure.
3. **Report delta:** State the before and after values for each setting changed. Do not assume the change was effective without reading back the applied importer values.
4. **Iterate limit:** Maximum 3 adjust-and-verify cycles before pausing to ask the user for feedback.

Check for zero console errors using `console status`. If any check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

Unless the user asks for additional proof, continue to the [Final report](#final-report). If you suspect a runtime fault, enter Play mode once and read state through `script eval`. Do not add logging or take screenshots.

## Final report

After finishing the audit or optimization, report to the user:

- Summarize every setting changed with before/after values.
- List any clips or groups that still need attention (e.g., clips that require on-device measurement to confirm savings).
- Disclose any side effects beyond the requested changes, such as reimports triggered or assets touched that the user didn't name, so the user can review them before committing.
- State any follow-up the user needs to do, such as testing on the target device or committing the changed `.meta` and mixer files.
- If the user needs runtime memory measurement, point them at the Memory Profiler package, which reports the largest AudioClips by runtime byte cost.
- If mixer CPU is still high after the audit, point them at the Unity Profiler's Audio module for DSP thread profiling.
