# Troubleshooting

Links to other reference files point to files in this `references/` folder. Any other relative paths resolve to the skill directory.

### Stereo clip on a 3D AudioSource

1. Confirm `audioSource.spatialBlend == 1`.
2. Confirm `audioSource.clip.channels == 2`.
3. Confirm `audioSource.spread` is 0 (the default), because then the source plays all channels from one point and the second channel only costs memory.
4. Enable `forceToMono` in the AudioClip importer and reimport. Unity mixes both channels to mono during import. If clip levels were balanced before import, disable Normalize to preserve relative volumes. Enable Normalize only if the mixed-down result is too quiet.

### Decompress On Load clip causes memory spike

1. Confirm `clip.loadType == AudioClipLoadType.DecompressOnLoad` and `clip.length` is long (> 5 s).
2. Switch to `Streaming` if it is music or ambience, `CompressedInMemory` if played only occasionally.
3. If the clip is short but still large: check `clip.channels` (stereo wastes double the memory) and `clip.frequency` (high sample rate on a mobile target wastes memory). Apply Force To Mono and/or sample rate override.

### AudioMixer CPU spike — DSP thread hot

1. Confirm with the mixer-asset query recipe that the mixer graph exists.
2. List all groups and their attached effects from the `.mixer` file, as described in Section 2 of the skill. Look for the same effect repeated across groups. An effect costs the same wherever it sits in the tree.
3. Find groups under a common parent that share the same **linear** effect, and suggest applying the effect once on the parent. Effects don't declare linearity, so judge from the effect name: equalizers and filters can be reordered among each other without changing the result, but a compressor, gate, limiter or distortion anywhere in the chain makes the result depend on the combined signal, so keep those on their original groups.
4. If the DSP buffer is small (64 or 128 samples), raise it — see DSP Buffer Size Guidelines in [references/platform-settings.md](platform-settings.md).

### Vorbis quality artifacts on dialogue

1. Confirm `defaultSampleSettings.compressionFormat == AudioCompressionFormat.Vorbis`.
2. Read `defaultSampleSettings.quality` (0–1) and the platform override, if one is enabled. You can't hear the clip, so tune with the user: raise quality a step, ask the user to play the clip in the Editor and say whether the crackle or warble is gone, and repeat. Once it's gone, offer to lower quality a step at a time until they hear it return, then go back one step. Quality is not linear, so tune each clip separately.
3. Confirm the source file is lossless (WAV or AIFF). MP3 sources cannot recover quality lost before Unity's re-encode.

### `Load In Background` causes first-play silence

This is expected behavior: the clip has not finished loading when `Play()` is first called. Mitigate with:
1. Enable **Preload Audio Data** in the clip's import settings so it loads with the scene, or call `clip.LoadAudioData()` before the clip is needed.
2. If the clip must play the moment it's triggered and can't be preloaded, disable Load In Background on that clip so it loads synchronously, and warn the user that synchronous loading can stall the main thread.
