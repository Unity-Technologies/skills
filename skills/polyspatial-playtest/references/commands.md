# Command reference

Every command is `unity command <name> [--flag value] [--project-path <project>]`. Results come
back in the `result` field; several are NDJSON (one JSON object per line) so they stream and grep.

## Annotations

| Command | Flags | Result |
|---|---|---|
| `polyspatial_annotation_list` | `--recording all\|latest\|<name>` (default all) | One line per annotation: `reference`, `recording`, `recordingPath`, `frame`, `frameEnd`, `time`, `kind` (`entity`\|`moment`), `text`, `created`, `createdBy`, and for entity annotations `entityId`, `entityName`, `entityPath`, `worldPosition`, `worldBoundsCenter`, `worldBoundsSize`, `hitPoint`; `camera` is the Scene view camera when it was written. |
| `polyspatial_annotation_show` | `--ref <recording>#<id> \| <id> \| <path.json>` (required), `--window 30`, `--include-components true`, `--max-changes 300` | `annotation` (as above), `recordingFrames`, `changeWindow {from,to}`, `state` (entity annotations: NDJSON lines of the subtree at the frame, parsed into an array), `changes` (entity) or `changedEntities` (moment), `changeCount`, `changesTruncated`, `entityResolvedByName`, `entityMissing`. |

`kind` is `entity`, `region` (a circle drawn in the Scene view: `entityIds`, `entityPaths`, `entityCount`; show returns `members`, their `state` and only their `changes`), `span` (`frame`–`frameEnd`) or `moment`.

A `changes` entry: `{ entity, component?, property, from, to, firstChangeFrame, lastChangeFrame, keyframes }`.
A `changedEntities` entry: `{ entity, changedProperties, properties[], firstChangeFrame, lastChangeFrame }`.

## Recording and playback (through `eval`)

No `polyspatial_*` command enters Play mode. Call the public `UnityEditor.PolySpatial.Utilities.RecordingPlaybackScene` API through `unity command eval --code "..."`:

| Call | Result |
|---|---|
| `return R.StartRecording();` | The new `.qrec` path, or `Error: ...` (already in Play mode, untitled scene). Enters Play mode. |
| `return $"{R.IsLiveSession} {R.LiveFrame}";` | `True <frame>` once the recorder runs; `LiveFrame` is the recording frame counter. |
| `unity command editor_stop` | Leaves Play mode; the file finalizes. Poll `polyspatial_recording_metadata` for it. |
| `return R.StartPlaybackAt("<path>", <frame>, true);` | Rebuilds `<path>` on a timeline inside the open scene, parked on `<frame>`; `null` on success. Never enters Play mode; the scene's own objects are deactivated until `StopPlayback`. |
| `R.SeekTo(<frame>, true); return R.CurrentFrame;` | Rebuilds that frame directly, in either direction. |
| `R.IsPaused = false;` / `R.IsPaused = true;` | Plays in real time / pauses. |
| `return $"{R.IsPlayingBack} {R.CurrentFrame} {R.PlaybackEnded}";` | Replay status. |
| `R.StopPlayback();` | Tears the timeline down and reactivates the scene's objects. |

`R` stands for the full `UnityEditor.PolySpatial.Utilities.RecordingPlaybackScene`; `eval` has no `using`, so spell it out.

| Command | Flags | Result |
|---|---|---|
| `polyspatial_recording_list` | | One line per `.qrec`: `path`, `name`, `sizeKB`, `lastWriteUtc`. |
| `polyspatial_recording_metadata` | `--recording` | `{ path, name, version, frameCount, recordingType, commandCount }`. |

## Recording queries

| Command | Flags | Result |
|---|---|---|
| `polyspatial_scene_state` | `--recording`, `--entity <name or path suffix>`, `--start-frame`, `--end-frame`, `--summarize false`, `--include-components true`, `--transform-digits`, `--properties a,b`, `--exclude-properties c`, `--include-assets false`, `--include-inputs false` | NDJSON: a header line with counts, then one line per entity with `path`, `lifecycle`, `world.position/rotation/scale`, `components[]` and `Component.property` values; a property that varies inside the range becomes `[[frame,value],...]`. `--properties` keeps only the named properties (entity headers stay), which is the cheap way to ask "how many times did Image.color change": count that array. |
| `polyspatial_scene_export` | `--out <file>` plus every `polyspatial_scene_state` flag | Writes the same NDJSON to a file and returns `{ path, lines, bytes }`. Use it whenever the slice is more than a screen of text, then compute on the file. |
| `polyspatial_scene_changes` | `--start-frame/--end-frame` or `--start-time/--end-time`, `--recording`, `--entity`, `--include-components true`, `--max-changes 500`, `--group-depth 0`, `--properties`, `--exclude-properties` | `{ recording, from, to, changeCount, changes[] }` or, with `--group-depth`, `changedEntities[]`. Values that differ only by float rounding are dropped. |
| `polyspatial_entity_timeline` | `--entity` (required), `--recording`, `--property position\|worldPosition\|rotation\|worldRotation\|scale`, `--start-frame`, `--end-frame`, `--step 0` | A header line then `{ frame, x, y, z[, w] }` samples (rotations are quaternions); `--step 0` targets about 200 samples, `--step 1` every frame. Redirect to a file and compute on it. |

Entity arguments accept a name, or a hierarchy path suffix (`Parent/Child`) when the name is
shared; an ambiguous name returns an error listing the candidate paths.

## Driving the game (com.unity.pipeline)

| Command | Flags | Result |
|---|---|---|
| `simulate_key` | `--key Space`, `--action press\|down\|up` | `{ Success, Detail, Error }`. Input System key names. |
| `simulate_pointer` | `--x --y`, `--action click\|move\|down\|up`, `--button left\|right\|middle` | Same. Screen pixels, origin bottom-left. |
| `simulate_input_script` | `--script '{"steps":[…],"releaseAtEnd":true}'` | `{ Status, ScriptId, StepCount, EventCount, EventsFired, StartFrame, EndFrame, StartTime, ElapsedSeconds, Fired[], Released[], Error }`. Steps: `{at, key, action: down\|up\|press\|hold, duration}` or `{at, x, y, button, action: move\|down\|up\|click\|hold, duration}`. Runs over the following frames; one script at a time. |
| `simulate_input_script_status` / `_cancel` | | The same status object. |
| `click_ui_element` | `--name <GameObject>`, `--button left`, `--index 0`, `--hold 0` | Status object; the first `Fired` line names the resolved path and screen position, and whether it scrolled the element into view. Fails with the position when the element is off screen. |
| `editor_play` / `editor_stop` / `editor_pause` | | Plain Play mode control without recording. |
| `capture_game_view` | `--source screen`, `--save_path <project-relative>` | PNG of the Game view; `screen` includes overlay UI and needs Play mode. |
| `set_autotick` | `--enable true` | Keeps the Editor ticking while unfocused. |
