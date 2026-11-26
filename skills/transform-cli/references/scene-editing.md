# Editing the scene tree

Relative paths in this file resolve to the skill directory, the folder above `references/`.

- [Target one part](#target-one-part)
- [Delete and merge](#delete-and-merge)
- [Move, reparent and rename](#move-reparent-and-rename)
- [Flatten the hierarchy](#flatten-the-hierarchy)
- [Hide, show, pivots and instances](#hide-show-pivots-and-instances)
- [Create and duplicate](#create-and-duplicate)
- [Undo](#undo)

Each command below that changes the scene is one undo step, so `scene undo` takes it back. Lookups (`occurrence find`, `metadata find`) and `--dry-run` previews change nothing and add no undo step, so don't count them when you undo. A command that fails adds no undo step either: it takes back what it did before it failed. Each `run file` / `run code`, and each step of a `workflow run`, that changed the scene is one undo step too; one that changed nothing, because it only read or printed, or raised before any change, adds none. Run `unity transform occurrence --help` and `unity transform scene compress --help` for the flags this build takes.

## Target one part

If the user wants to change one part of an assembly, find it first (`occurrence find --name <regex>` or `metadata find --key <k> --value <regex>`), then pass its id as `--occurrence` to the `algo` command, because without it the command works on the whole scene. The SDK runs `metadata find`'s regex, so it refuses (exit 2) a named group `(?P<n>…)`, a lookbehind, a comment `(?#…)`, an inline flag, or repeats that write out more than 10000 characters (`a{20000}`); write `--ignore-case` rather than `(?i)`.

`algo remove-occluded`, `algo tessellate`, `scene create` and `occurrence set-transform` refuse a value outside the range their `--help` gives (a `--resolution`, `--max-angle` below 1, `--subdivisions`, a huge `--scale`), with `INVALID_COMMAND_ARGS` (exit 2) before the session is touched: values past it crashed or stalled the session. Pick a value inside the range rather than retrying. On a large model, `tessellate` also refuses (exit 2) a value whose estimated memory the machine can't spare; `data.largest` is the finest that fits, so retry with it (or with `--occurrence` on part of the scene). `tessellate` at its defaults isn't checked; a finer value is also held to about 8 GB.

UVs come from the `generate-uv` workflow, not an `algo` command: `workflow run generate-uv` projects UVs onto the parts, packs the islands and fits them to 0-1, on channel 0, across the whole scene, or with `--param occurrence=<id>` on that occurrence and everything under it. `--param projection=` picks the shape they are projected onto, fitted to the model: `aabb` (the default: its bounding box), `cubic-aabb` (the same at one scale on each axis), `mbb` (its smallest bounding box, turned to fit), `cylinder` or `sphere`; pick the one closest to the part's shape. `--param channel=` (0-7), `resolution=` (64-4096 px, default 1024) and `padding=` (px, at most 32768 / resolution) set the rest. A padding too large for the resolution, an occurrence that isn't in the scene, or a resolution the machine can't spare the memory for (measured on the occurrence alone, so `occurrence=` on part of the scene is the way out) fails the run (exit 6) before the scene changes, naming what fits. Each of its steps (`project`, `pack`, `normalize`) that changed the scene is one undo step: to take the run back, `scene undo --steps <n>` with `n` the sum of `undoSteps` in `data.steps` (3 when every step ran, 0 when it was refused or found no triangles), never a fixed 3. To unwrap or merge islands first, or to project onto a shape you place, `workflow copy generate-uv --as <name>` and edit the copy.

`algo normals`, `decimate` and `remove-occluded`, and the `generate-uv` workflow, need triangles: on CAD not yet tessellated they change nothing and warn (`There are no triangles to work on`, or from the workflow `No step had any triangles to work on`), so run `algo tessellate` first. Where only some parts have triangles (a STEP file that carries a mesh for some parts, or CAD imported beside a mesh), `normals` and the `generate-uv` workflow work on those, exit 0, and warn (`<n> of <m> parts had no triangles to work on`). `normals` reports `created` true, `data.skipped` (the parts left unchanged, `{ occurrenceId, name }`; `null` when no part had triangles) and `data.partCount` (every part); the workflow lists the parts it left in its output (`data.output`), after `Left unchanged, with no triangles:`. Check `warnings` rather than the flag: run `algo tessellate`, then the same command again. `algo tessellate` gives every part that has CAD surfaces a mesh at the settings you pass, replacing one it already has; when the triangle count doesn't change it warns: there were no CAD surfaces, or those settings give as many triangles as before.

An `.obj` file carries no unit: `scene export` writes millimeters and `scene import` reads `.obj` as centimeters, so an `.obj` round trip comes back 10 times larger (the export warns). Use `.glb` or `.fbx` when the file comes back into a session; if the user must use `.obj`, run `occurrence set-transform <id> --scale 0.1` on the occurrence `scene import` reported: it holds the file's parts at scale 1, and the 10 sits on the parts under it (`--scale` sets the scale, it doesn't multiply it).

## Delete and merge

If the user asks to delete or merge parts, run `occurrence delete` or `occurrence merge` with `--dry-run` first, then again without it, only for the parts the user named, because neither asks before it changes the scene.

To cut draw calls, use `occurrence merge --by material` (or `assembly`, `final-assembly`, `name`, `level --max-level <n>`), because it groups and merges in one call instead of one per group. Its dry run gives the parts in scope, not the groups the SDK will make.

## Move, reparent and rename

If the user asks to move, rotate, scale, reparent or rename a part, use `occurrence set-transform`, `occurrence set-parent` or `occurrence rename` instead of a script, because each reports the value it replaced.

- `set-transform` is local to the parent, in mm and degrees, and rotates about X, then Y, then Z. Any of `--position`, `--rotation` and `--scale` you leave out stays. A mirror reads back as at most one negative scale (two negative axes are a 180° turn, shown in the rotation), so setting the rotation or the scale alone keeps it.
- `set-parent` keeps the part where it is in the world unless you pass `--keep-local`. On a prototype, its instances keep their place too: each gets the local matrix it had as its own, so later transforms of the prototype no longer move them.

## Flatten the hierarchy

If the scene tree is deep after a CAD import, or the user asks to flatten or simplify the hierarchy, run `scene compress --dry-run`, then `scene compress`, because it removes the occurrences that hold no part and have one child or none, and keeps instances shared.

- Add `--rake` only when the user wants every part directly under one parent, because it drops the assembly structure.
- Pass `--occurrence` to limit it to one sub-assembly, and use `data.resultOccurrenceId` afterwards, because an occurrence left with one child is replaced by it.

## Hide, show, pivots and instances

If the user asks to hide or show parts, recentre a pivot, or edit one copy of an instanced part, use `occurrence hide`, `occurrence show`, `occurrence set-pivot --to center|origin` or `occurrence make-unique` instead of a script, because each is one undo step. A hidden part keeps its triangle count in every count and in the `--min-triangles` / `--max-triangles` filters, so hiding a part never makes it look empty. A point cloud or a curve-only part has no triangles but isn't empty: `--max-triangles` never matches it, and deleting one reports its points and lines.

Tell the user when `set-pivot` warns that it made instances unique, because each copy then has its own geometry, which costs memory and file size.

`metadata set` on an instance that shows its prototype's metadata (`source: prototype`) gives the instance a copy of those entries plus the new ones (`source: instance`): later changes to the prototype's metadata no longer reach it.

## Create and duplicate

If the user wants an empty group to move parts into, or a copy of a part, use `occurrence create --name <name>` or `occurrence duplicate <id...>`, because each reports the new ids.

Pass `--instance` only when the copies should share geometry, because editing one instance changes them all.

## Undo

If a step made the scene worse, such as a decimate that went too far, run `scene undo` instead of re-importing, because the steps before it stay as they were. Run `scene history` for the step names, newest first, then `scene undo --to <step>` to make that step the latest one again, or `--steps <n>` to undo the newest `n`. To try another setting on the model as imported, such as a second decimate ratio, run `scene undo --to "scene import"` rather than `session reset` and a fresh import, because importing a large file again is slow. Then look ids up again, because a later command may reuse an id the undo freed. `--to` takes the exact step name; one that isn't in the history is refused with `STEP_NOT_FOUND` (exit 6) and undoes nothing, so pick a name from `data.candidates`; a session an older CLI started refuses `--to` with `SESSION_OUTDATED`, so use `--steps` there.
