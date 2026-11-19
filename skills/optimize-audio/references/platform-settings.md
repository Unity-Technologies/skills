# Audio Platform Settings Reference

## Compression Format

See [Audio file compression](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/files/compression.md) for the full format-by-platform table and CPU/size/quality trade-offs.

Skill-specific notes:
- Quality is not linear, so it needs tuning per clip by ear. You can't hear the clip, so don't pick a final value yourself: recommend a starting value, ask the user to play the clip in the Editor and report audible artifacts (crackle, a warbling or metallic tone, or blurred transients such as smeared consonants or drum hits), and adjust from their reports. Lower quality until the user hears artifacts, then raise it slightly.
- MP3 pads loops with silence; use lossless WAV or AIFF source assets instead.

## Sample Rate

See [Audio Clip Import Settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/reference/class-audio-clip.md) for sample rate settings.

Halving the sample rate halves the PCM memory cost. Always report the estimated saving for each clip changed.

## Load Type

See [Audio Clip Import Settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/reference/class-audio-clip.md) for Load Type behavior.

### Load Type Mismatch Flags

- **Decompress On Load** on a long clip bloats memory: the full PCM expands in RAM at load time. Estimate the footprint as `clip.samples × clip.channels × 2` bytes (16-bit PCM) and report it.
- **Streaming** on a clip that plays dozens of times simultaneously adds disk pressure.
- **Load In Background** applies to Decompress On Load and Compressed In Memory clips: loading no longer stalls the main thread, but a `Play()` call before loading finishes is delayed. Streaming clips already load in the background, so the setting has no effect on them.
- For clips that must play immediately, enable **Preload Audio Data** in the clip's import settings so the clip loads with the scene, or call `clip.LoadAudioData()` before it's needed.

## DSP Buffer Size

See [Audio settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity-editor/editor-settings-reference/comp-manager-group/class-audio-manager.md) for DSP Buffer Size options.

If `bufferLength` < 256, recommend increasing to Good Latency (512) or Best Performance (1024) to trade latency for CPU stability.
