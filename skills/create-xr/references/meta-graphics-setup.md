# Meta Graphics Setup — Quest URP & OpenXR

Relative paths in this file resolve to the skill directory.

Single-pass instanced stereo, foveated rendering, and the Quest URP asset. Requires the setup procedure first (OpenXR on the Android tab, Vulkan); the script checks and stops if either is missing.

## Docs

- `com.unity.xr.openxr`: `features/foveatedrendering.md`, `features/performance-settings.md`.
- `com.unity.xr.meta-openxr`: `get-started/graphics-settings.md` (Quest graphics settings).
- `https://docs.unity.com/en-us/engine/<VERSION>/manual/xr/graphics/untethered-device-optimization.md` (6000.3+; skip on older versions).

## Step 1 — Configure Quest graphics (script, no reload)

```
bash "<SKILL_DIR>/scripts/meta-graphics/meta-graphics-setup.sh"
```

Runs five steps, each sent to the Editor with `unity command eval`: single-pass instanced stereo → foveated rendering and performance settings → new Quest URP asset and renderer (no SSAO) → Quest URP settings (incl. GPU Resident Drawer off) → Quest URP asset on the `Meta Quest (Build Profile)` quality level (other levels unchanged; they can be shared with PC).

Ends with `=== Quest graphics setup complete ===`. The script waits for the Editor itself.

### If a step prints `FAILED`

The script stops at the failed step. Every step is idempotent: fix the cause, then re-run the same script. Do not rewrite the script.

| Message contains | Cause | What to do |
|---|---|---|
| `Run the setup procedure (OpenXR) first` | OpenXR not configured for Android | Re-run `references/setup.md`, then re-run this script |
| `Vulkan is not the Android graphics API` | Player settings not applied | Re-run `references/setup.md`, then re-run this script |
| `no active UniversalRenderPipelineAsset` | Not a URP project | Stop this procedure — tell the user URP is required, and continue to the rig |
| `FoveatedRenderingFeature not found` | OpenXR package version without the feature | Stop and report the `FAILED` line to the user |
| `(URP version changed?)` | This URP version stores the renderer list differently | Stop and report the `FAILED` line to the user |
| `EVAL_FAILED` with compiler diagnostics (`CS....`) | A step doesn't compile against this Unity or package version | Stop and report the `FAILED` line to the user |
| `EVAL_NOT_FINISHED` | Editor still busy | Wait ~10 s, then re-run the same script |
| `no Pipeline command for` (exit 4) | This Pipeline version renamed or removed a command | Run the `unity command --query` the message names; if it exists under a new name, re-run with the `PIPELINE_CMD_<KEY>` override it names, otherwise report the line |

## Step 2 — Done check

The script's last step (`verify`) is the done check: `OK [verify]: done: renderMode=SinglePassInstanced, foveated=True, Meta Quest (Build Profile) on Quest_RPAsset=True, URP settings=ok, SSAO=none`. If it reports `SSAO=present`, the user added SSAO to `Quest_Renderer`: don't change it; flag it in the final report: SSAO is not recommended on Quest (performance cost).
