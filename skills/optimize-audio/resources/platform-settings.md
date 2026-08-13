# Audio Platform Settings Reference

## Compression Format Matrix

| Platform | Recommended Format | Notes |
|---|---|---|
| PC / cross-platform | Vorbis, quality 0.5–0.7 | Raise to 0.7–0.85 for dialogue; default 0.5 often adds artifacts |
| iOS | AAC | Hardware decode; cheapest CPU |
| Android | Vorbis | Software decode |
| Xbox | XMA | Use platform override in import settings |
| PlayStation | ATRAC9 | Use platform override in import settings |
| Web | Vorbis | Browser handles decode |

## Sample Rate Recommendations

| Use Case | Recommended Rate |
|---|---|
| PC / console music and voice | 44100 Hz |
| PC / console SFX | 44100 Hz |
| Mobile SFX | 22050 Hz |
| Mobile dialogue | 22050 or 44100 Hz |
| UI clicks / blips | 22050 Hz |

Halving the sample rate halves the PCM memory cost. Always report the estimated saving for each clip changed.

## Load Type Decision Table

| Load Type | Behavior | Use For |
|---|---|---|
| Decompress On Load | PCM in memory at load; zero per-play CPU | Short SFX < 200 KB (uncompressed) |
| Compressed In Memory | Stays compressed; decompresses on play | Medium clips played occasionally |
| Streaming | Streams from disk; minimal RAM, higher disk I/O | Music, long ambience, voice-overs |

### Load Type Mismatch Flags

- **Decompress On Load** on a clip > 1 MB bloats memory.
- **Streaming** on a clip that plays dozens of times simultaneously adds disk pressure.
- Always apply `Load In Background` for any Streaming clip to prevent the main thread stalling on first play.

## DSP Buffer Size Guidelines

| Setting | Buffer Size | Use Case |
|---|---|---|
| Best Latency | 256 | Rhythm games, real-time synthesis |
| Good Latency | 512 | General gameplay |
| Best Performance | 1024 | Ambient/cinematic, battery-saving |

A very small buffer (64 or 128) costs more CPU per frame. If `bufferLength` is < 256, recommend increasing to "Good Latency" or "Best Performance" to trade latency for CPU stability.
