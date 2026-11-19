# Web build troubleshooting

## Contents

- [Build still large after enabling Strip Engine Code](#build-still-large-after-enabling-strip-engine-code)
- [Brotli not working — Decompression Fallback required](#brotli-not-working--decompression-fallback-required)
- [Stutter in Safari but not Chrome](#stutter-in-safari-but-not-chrome)
- [Memory growth slow path triggered](#memory-growth-slow-path-triggered)
- [Frame rate set to 60 but browser runs erratically](#frame-rate-set-to-60-but-browser-runs-erratically)
- [Firefox cache rejecting large files](#firefox-cache-rejecting-large-files)
- [Local dev server setup](#local-dev-server-setup)

## Build still large after enabling Strip Engine Code

1. Is **Managed Stripping Level** set to Medium or Low? → Set to High for release.
2. Are plug-ins using reflection to access engine modules that would otherwise be stripped? → Add a `link.xml` to preserve needed symbols.
3. Is **Exceptions** set to Full? → Full adds the largest code overhead; switch to None or Explicitly Thrown Only.

## Brotli not working — Decompression Fallback required

1. Is the server sending `Content-Encoding: br`? → Without this header the browser won't decompress; the fallback JS decompressor is then needed.
2. Is the build hosted over HTTP (not HTTPS)? → Brotli requires a secure context; degrade to Gzip for HTTP hosting.

## Stutter in Safari but not Chrome

1. Does the project set `Application.targetFrameRate = 60`? → On Safari WebGL this conflicts with browser pacing; set to `-1`.
2. Are there shaders that behave differently on Safari's WebGL implementation? → Test on device; Safari's WebGL/Wasm runtime differs from Chromium — some GLSL constructs are handled differently.

## Memory growth slow path triggered

1. Is **Initial Memory Size** too small for the project's peak? → Wasm memory growth requires a full buffer copy; set Initial Memory Size to a realistic peak estimate.
2. Is **Memory Growth Mode** set to Linear? → Switch to **Geometric** for saner growth curve.

## Frame rate set to 60 but browser runs erratically

1. Is `Application.targetFrameRate = 60` set in code? → On Web this conflicts with `requestAnimationFrame` browser pacing. Set to `-1`.
2. Is `vSyncCount` non-zero? → Set to 0; the browser handles pacing.

## Firefox cache rejecting large files

Firefox limits individual cache entries via `browser.cache.disk.max_entry_size`. If the build exceeds this (default ~50 MB), assets won't cache. Solution: use Addressables to split into bundles < 51 MB, or instruct users to increase the setting in `about:config`.

## Local dev server setup

For testing builds locally with proper MIME types:

```bash
# Python (HTTP)
python -m http.server 55553 -d path/to/build

# Node.js (install serve-handler)
npx serve path/to/build -l 3001
```

For Brotli testing, use HTTPS — Brotli requires a secure context. Generate a self-signed cert with OpenSSL for local testing.
