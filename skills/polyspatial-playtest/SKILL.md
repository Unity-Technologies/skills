---
name: polyspatial-playtest
description: Use when you must prove that gameplay, UI, or behavior in a Unity project actually works or is actually broken — "verify my change in Play mode", "does the button do X", "play-test this", "why does the cupcake stop spinning", "check the fix", or whenever someone hands you a PolySpatial annotation reference like `MyRecording-2026-9-11-101947#7d29bfdc…`. Records the Play session as a PolySpatial .qrec, drives the game with simulated input, then answers from the recorded per-frame scene state instead of eyeballing a screenshot. Requires a running Editor with com.unity.pipeline and the PolySpatial recording commands (`unity command --query polyspatial`).
allowed-tools:
  - Bash
  - Read
---

# PolySpatial play-test and debug

Screenshots tell you what a frame looked like; a PolySpatial recording tells you what every
entity *was* on every frame — transforms, component properties, lifecycle, audio — and you can
query it after Play mode has ended. This skill is the loop: understand the request (often an
annotation someone left on a recording), change the code, then **play the game for real, record
it, and read the numbers back**. Use `capture_game_view` only to confirm what a frame looks like
once you already know from the data which frame matters.

All commands below are `unity command <name> [--flag value]`. Always pass `--project-path <path>`
when more than one Editor may be open. Read [references/commands.md](references/commands.md) for
every flag and the shape of each result.

## 0. Preconditions (check once per session)

```bash
unity status                                   # an Editor for this project, state "ready"
unity command --query polyspatial --detail compact   # the polyspatial_* commands must be listed
unity command set_autotick --enable true      # an unfocused Editor barely advances frames otherwise
```

If `polyspatial_*` commands are missing, the project lacks the PolySpatial recording packages: say
so, and fall back to `editor_play` plus `capture_game_view`. Do not hand-edit `.unity`/`.prefab`
files while an Editor is reachable (see the `unity-cli` skill).

The game must use the **Input System** for simulated input to reach it; legacy `Input.GetKey` code
cannot be driven. Check `ProjectSettings/ProjectSettings.asset` → `activeInputHandler` (1 or 2), or
just try `simulate_key` in Play mode and see whether the game reacts.

## 1. Start from an annotation reference

A reference looks like `<recording>#<annotation-id>` (people copy it with the `Ref` button in
Window ▸ PolySpatial ▸ Annotations). It may also arrive as a bare id or as a path to the
annotation's `.json`. Resolve it first; never guess what it points at:

```bash
unity command polyspatial_annotation_list                    # every annotation, one JSON line each
unity command polyspatial_annotation_show --ref "<recording>#<id>" --window 30
```

`polyspatial_annotation_show` returns:

- `annotation.text` — what the person said, and `frame`/`time` — when. `kind` is `entity` when they
  right-clicked an object (then `entityPath`, `worldPosition`, `worldBoundsSize`, `hitPoint` are set)
  or `moment` when they annotated the whole frame.
- `state` — the entity's subtree at that frame: world transform, components and their properties.
- `changes` — every property of that subtree that varied within ±`window` frames, with `from`, `to`,
  `firstChangeFrame`, `lastChangeFrame`. For a moment annotation you get `changedEntities` grouped by
  the top two hierarchy levels instead; drill in with `polyspatial_scene_changes --entity`.

Read text and data together. "The cupcake stopped spinning" at frame 400 plus `world.rotation`
changing through frame 430 means it did *not* stop — the person is describing the expected
behavior, or noticed something else. "The guy is looking back" as a moment annotation plus
`changedEntities` naming only `DudeContainer` tells you which object to inspect.

### Delegate the measuring

Raw recording data is large and the arithmetic is mechanical, so keep both out of the main
conversation. In Claude Code, hand every quantitative question to the `recording-analyst` subagent
that ships with this plugin (`Agent` with `subagent_type: recording-analyst`), passing the project
path, the reference or recording, and the question verbatim. It can only run `polyspatial_*` commands
and compute on their output, so what comes back is a measurement with frames and seconds, and you
decide afterwards whether the *why* needs the code. In Codex there is no plugin subagent: copy
[references/codex-recording-analyst.toml](references/codex-recording-analyst.toml) into the project's
`.codex/agents/` and delegate the same way, or follow the rules below yourself.

### Answer from the recording first, explain from the code second

When the question is about what happened ("how many times did it rotate", "did it ever leave the
platform", "how long was the button disabled"), the recording is the source of truth and the
answer is a number you compute from it. Do that **before** opening any scene or script, and report
it as soon as you have it. Only then, if the person asked *why*, read the code that drives the
entity — and say plainly which part of your answer is measured and which is inferred from code.
Do not spend the session grepping `.unity` files while the measured answer sits unreported.

Worked example, "how many times did the cupcake rotate?". The recording gives you samples;
the arithmetic is yours — dump every frame to a file and compute in a short script, exactly as
you would with any dataset:

```bash
unity command polyspatial_annotation_show --ref "<recording>#<id>"     # entityPath: …/Cupcake/Wiggle/MMCupcake/Cup
unity command polyspatial_entity_timeline --recording <recording> --entity "MMCupcake/Cup" --property rotation --step 1 --json > cup.json
unity command polyspatial_entity_timeline --recording <recording> --entity "Wiggle/MMCupcake" --property rotation --step 1 --json > mmcupcake.json
# python: for consecutive quaternions q0,q1 take delta = q1 * inverse(q0), convert to angle-axis,
# accumulate signed angle about the dominant axis; sum / 360 = turns; runs of |delta| > 0 = bursts.
```

Result on the sample recording: `Cup` never turns relative to its parent (one distinct rotation
in 779 frames); `MMCupcake` turns 13.9 times about x in two bursts, frames 336–380 (6.9) and
384–428 (6.9). Answer with those numbers and frames first: "the cupcake spun ~13.9 turns in two
~7-turn bursts at 7.4–8.4 s and 8.5–9.5 s; the rotation is on the parent MMCupcake, the annotated
Cup mesh is rigid." Then, only if asked why: the MMF_Rotation feedback on that object.

The same pattern answers "how many times did the color change" (`polyspatial_scene_state
--entity X --start-frame 1 --end-frame N --properties Image.color` and count that keyframe array),
"how far did the player travel" (sum `worldPosition` deltas), "was it ever inactive" (the
`lifecycle` keyframes). There is no per-question command; there are samples and your computation.
When a slice is more than a screen of text, `polyspatial_scene_export --out Temp/x.ndjson ...`
writes it to a file: compute on the file, print only the result.

Rules for measuring:

- A property that is constant on the annotated entity usually lives on an ancestor: walk up the
  `entityPath` one level at a time. `worldRotation`/`worldPosition` give the composed result.
- Net rotation and total rotation differ: a wiggle travels many degrees and nets zero. Report
  the one the question asks for, and say which.
- `--step 1` gives every frame; the default samples ~200 points, which is fine for a curve but
  not for counting.
- Convert frames to seconds with the `time` fields of `polyspatial_annotation_list` or `--start-time`
  on `polyspatial_scene_changes`; quote both in the answer.

To see the moment, replay and park on the frame, then capture:

```bash
unity command polyspatial_playback --recording <recording>     # enters Play mode; wait for it
unity command polyspatial_playback_status                      # poll until playingBack is true
unity command polyspatial_playback_seek --frame 400 --pause true
unity command capture_game_view --source screen --save_path Temp/annotation-400.png
unity command polyspatial_record_stop                          # leaves Play mode
```

If the task is to change behavior, then go read the code that drives that entity (the hierarchy
path names the GameObjects), fix or implement, and prove it with section 2.

## 2. Verify by playing: record → drive → stop → query

Decide **before** recording what "correct" means as numbers: which entity, which property, which
frames or seconds, which value or change. Then:

```bash
# 1. Open the scene to test (must be a saved scene; recording refuses untitled scenes).
unity command polyspatial_record_start            # arms a .qrec and enters Play mode; returns its path
unity command polyspatial_playback_status         # poll until inPlayMode and recording are true; note "frame"

# 2. Drive the game. Timed sequences run over real frames; poll status until completed.
unity command simulate_input_script --script '{"steps":[{"at":0.5,"key":"W","action":"hold","duration":1.0},{"at":2.0,"x":640,"y":360,"action":"click"}]}'
unity command simulate_input_script_status        # "fired" lists time and Time.frameCount per event
unity command click_ui_element --name "Play Button"   # uGUI by GameObject name; scrolls it into view
unity command polyspatial_playback_status         # note "frame" again: the recording frames you drove

# 3. Stop and wait for the file.
unity command polyspatial_record_stop             # returns the .qrec path
unity command polyspatial_recording_metadata --recording <path>   # poll until it answers; frameCount

# 4. Ask the recording.
unity command polyspatial_scene_state --recording <path> --summarize true                 # what exists
unity command polyspatial_scene_changes --recording <path> --start-frame A --end-frame B --entity "Player"
unity command polyspatial_entity_timeline --recording <path> --entity "Player" --property worldPosition --start-frame A --end-frame B
unity command polyspatial_scene_state --recording <path> --entity "Enemy/HealthBar" --start-frame B --end-frame B
```

Rules of evidence:

- Quote frames and values from the recording in your answer ("frame 1210–1290: `world.position.y`
  rose from 0.00 to 2.31, then fell back by frame 1350"), and name the `.qrec` path so a person can
  replay it. A screenshot alone is not proof.
- Entity names repeat (a UI scene has hundreds of `Text`). When a command says the name is
  ambiguous, pass a hierarchy path suffix such as `Button - Scale/Text`.
- `polyspatial_entity_timeline --property` takes `position`, `worldPosition`, `rotation`,
  `worldRotation`, `scale` (the `world.rotation` spelling from scene_state output also works);
  frames are 1-based.
- Frames from `polyspatial_playback_status` while recording are recording frames; `Time.frameCount`
  in `simulate_input_script_status` is the game's counter. Bracket with status before and after
  driving, or convert with `--start-time/--end-time` on `polyspatial_scene_changes`.
- The Editor throttles when unfocused: expect frame rates that differ from a focused run, and use
  seconds, not frame counts, when timing input.
- Leave Play mode with `polyspatial_record_stop` (or `editor_stop`); the scene that was open
  before is restored. Never leave the Editor in Play mode.

## 3. Inspect an existing recording without an annotation

```bash
unity command polyspatial_recording_list
unity command polyspatial_recording_metadata --recording latest
unity command polyspatial_scene_state --recording latest --summarize true
unity command polyspatial_scene_changes --recording latest --start-time 0 --end-time 5 --group-depth 2 --include-components false
```

Start wide (`--summarize`, `--group-depth 2`) and narrow to one entity and a short frame range;
whole-scene keyframe dumps run to hundreds of kilobytes.

## Gotchas

- Entering or leaving Play mode reloads the domain: `unity command` may fail to connect for a few
  seconds. Retry; do not assume the Editor died.
- `polyspatial_record_start` fails when already in Play mode, when the scene is untitled, or when
  the scene has unsaved changes at playback time. Save first.
- Input screen coordinates are Game view pixels with the origin bottom-left; `capture_game_view`
  reports the size it rendered at.
- Audio needs `UnityEngine.AudioSource` in PolySpatial Settings ▸ Generic Tracking Excluded Types
  to be recorded as play/stop events; without it, audio shows no state changes.
