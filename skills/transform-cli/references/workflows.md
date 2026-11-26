# Workflows

Relative paths in this file resolve to the skill directory, the folder above `references/`.

A workflow is a recipe of several steps, kept as a Python file, that runs on a session's scene. Each step that changes the scene is one undo step (one that changes nothing adds none), and every step reports the scene's polygon and triangle counts before and after. A step that raises stops the run and is named, with the results of the steps before it. It is not rolled back: if it changed the scene before it raised, its `undoSteps` in `data.steps` is `1` and `scene undo` reverts it; if it is `0`, the step changed nothing, so don't undo, because that reverts the step before it. Run `unity transform workflow --help` for the commands this build has, and `unity transform workflow show <name> --json` for one workflow's parameters, types and defaults.

## Contents

- [Built-in workflows](#built-in-workflows)
- [Capturing a session](#capturing-a-session)
- [Where workflows come from](#where-workflows-come-from)
- [Installing a shared workflow](#installing-a-shared-workflow)
- [The file](#the-file)
- [When the SDK changes](#when-the-sdk-changes)
- [Outside the CLI](#outside-the-cli)

## Built-in workflows

| Workflow | Does |
|---|---|
| `optimize` | Repairs and tessellates, then, while the scene is over budget, deletes duplicated parts, small parts and hidden geometry, and decimates to the budget; generates UVs and tangents. |
| `glb-preview` | A lightweight GLB for a web preview: repairs and tessellates CAD, merges by material, compresses geometry and textures, decimates, and exports. |
| `realtime-budget` | Brings an assembly to a triangle budget: deletes small parts and holes, removes geometry that can't be seen from outside, instances repeated parts, simplifies, then shares the budget between part groups by visual weight. `--param triangles=<n>` sets the budget, and `--param "groups=Body=2,Seat=0.5"` weighs parts by name pattern. |
| `generate-uv` | UVs on one channel: projects them onto a shape fitted to the model, packs the islands into one map, and fits it to 0-1. `--param projection=aabb\|cubic-aabb\|mbb\|cylinder\|sphere` picks the shape, `occurrence=<id>` limits it to one occurrence, and `channel`, `resolution` and `padding` set the rest. Parts with no triangles beside meshed ones get a warning and are listed in `data.output`. `algo generate-uv` is now `workflow run generate-uv`. |
| `lods` | Levels of detail: a copy of the source per ratio, beside it, each with geometry of its own and decimated to that percentage of the source's triangles; the source is unchanged. `--param ratios=50,25,12.5` (the default) go from largest to smallest, at most 8. The source is `--param occurrence=<id>`, else the scene's one top-level occurrence: a scene with several fails (exit 6), so pass the part or group the parts under one occurrence first (`occurrence create`, `occurrence set-parent`). Each level is named `<source>_LOD<n>`, and `data.output` has a line per level with its triangle count and occurrence id, for `scene export --occurrence <id>`. A source with no triangles (CAD not yet tessellated: `algo tessellate` first), or levels the machine's memory can't hold, fail before anything is copied; the memory refusal names ratios that fit. A level that kept no triangles (a ratio too small for the model) fails the run after the levels are made: `scene undo` removes them, then retry with larger ratios. |

Every size a built-in uses comes from the model's bounding box, never fixed millimetres. Steps that render (removing hidden geometry) are skipped with a warning on a machine without a GPU; a workflow that declares `gpu = "required"` is refused with `NO_GPU` before any step runs. A run that leaves no triangles, whose steps had none to work on (CAD such as STEP is surfaces until tessellated: `algo tessellate` first, or `--param tessellate=true` on `realtime-budget`), or whose `--output` mesh file holds none still exits 0, with a warning in `warnings`: check it before using the result. A point cloud or a curve-only model has no triangles either; the warning then says so, and `scene import` of one doesn't warn. `optimize` deletes loose points and curves, and `glb-preview` deletes curves, so their `--output` from such a model can hold no geometry, with a warning that says so.

`workflow run <name> --input <file> --output <file>` imports the file into an emptied scene first (as `scene import --clear`) and exports after the last step. Without `--input` it runs on the session's scene; on an empty scene it fails with `WORKFLOW_EMPTY_SCENE` (exit 4): pass `--input`, or `scene import` first. `workflow show`, `run`, `check` and `emit` take the path to a workflow file as well as a name. `run file` refuses a workflow file (exit 2) and names `workflow run <path>` instead. Each item of `data.steps` has `unchanged`: `true` for a step that is nothing but `tx` operations, all of which found nothing to do, and that added no undo step (`tx.remove_occluded` with no triangles), which `workflow capture` leaves out.

## Capturing a session

`unity transform workflow capture <name> [--session <id>] [--project] [--force]` writes what the session did to its scene as a workflow (`~/.transform/workflows/<name>.py`, or the project's with `--project`), from the session's journal (`~/.transform/logs/<id>.journal.jsonl`, one JSON line per request). Prototype a pipeline one command at a time, then capture it and replay it on any model with `workflow run <name> --input <file> --output <file>`, or as a script with `workflow emit`. When you assembled several steps that worked and the user is likely to repeat the job, offer to capture them (or to write one with `workflow new` or `workflow copy`), because `workflow check` then keeps them working across SDK releases. Write it only when the user agrees, because it lands in their `~/.transform` or project.

| Request | In the workflow |
|---|---|
| The first `scene import` | No step: the model comes from `--input` or the script's argument |
| A later `scene import` | A step importing a `str` parameter that defaults to the recorded path |
| `scene export` | No step |
| `algo`, `occurrence`, `material`, `metadata`, `scene create`, `scene compress` commands | A step with the `pxz` code the command runs |
| `run code`, `run file` | A step running the script as sent; scripts share one namespace, as in the session. A hard-coded path stays, with a warning |
| `workflow run` | That workflow's own steps, each named after the workflow and the step (`optimize: prepare`), its file pasted in with the parameters it ran with, so the capture doesn't depend on the original. Undoing the last steps of the run leaves out only those |
| Read-only and internal requests, failed ones, undone ones, ones that changed nothing (`algo remove-occluded` or `algo normals` with no triangles, `metadata remove` of a key nobody has), and workflow steps that changed nothing (`tx.remove_occluded` with no triangles) | Left out (the `workflow run` stays, so its file's code outside the steps still runs) |
| `scene clear`, `session reset`, `scene open`, `scene import --clear` | The capture starts over there |

`--json` gives `data.steps` (each with the request it came from), `data.skipped` (each request left out, or with `step` a step of a kept `workflow run` left out, with `reason`: `internal`, `read-only`, `failed`, `undo`, `undone`, `unchanged`, `model`, `export`, `cleared`, `start-over`, `untranslatable`) and `data.model`; `warnings` names occurrence ids from the session (they match only the scene captured from: each new import, even of the same file in the same session, gives other ids, and the step then changes other parts or none, still succeeding, so rewrite those steps to find parts by name before replaying on a new import) and paths written in scripts. The header sets `requires-sdk` to the session's SDK version, so an older SDK refuses the captured workflow with `SDK_UNSUPPORTED` (exit 4). Nothing to capture fails with `COMMAND_FAILED` (exit 6). A workflow of that name already in the target folder is refused with `INVALID_COMMAND_ARGS` (exit 2) without `--force`; a capture never changes a built-in.

## Where workflows come from

A name found in more than one place runs the first:

1. Project: `.transform/workflows/` in the current folder.
2. User: `~/.transform/workflows/`.
3. Built-in: `workflows/` beside the CLI.

`workflow list --json` and `workflow show <name> --json` give each workflow's `origin` (`project`, `user` or `built-in`) and `path`, and `shadows` the files of the same name it hides. A project or user workflow is Python that runs with the user's permissions, and a project's can come with a repository someone else wrote, so before running one the user didn't name, show them its `path` and run it only once they agree. To use the built-in it hides instead, pass the built-in's `path` from `shadows` to `workflow show`, `check` and `run` in place of the name, because the name still resolves to the project's or user's file.

A file that can't be read still holds its name: `workflow list` shows it as `invalid`, and every command refuses it with `WORKFLOW_INVALID` rather than run a lower one in its place. `workflow copy <name> --as <new>` copies a workflow to change it (`--project` puts the copy in the project); `workflow new <name>` writes a template. `workflow new`, `copy` and `install` warn when the file they write hides a built-in of the same name, or when a file of that name elsewhere runs instead of it; tell the user, because the built-in no longer runs under that name.

## Installing a shared workflow

`unity transform workflow install <file-or-url> [--sha256 <hash>] [--project] [--force] --json` installs one workflow file a team shares, from an `https://` URL or a path, into `~/.transform/workflows/` (or the project's with `--project`). The file is named `<name>.py`, or `<name>@<version>.py` for a variant. `data.status` is `installed`, `updated` or `unchanged`, `data.sha256` the file's hash and `data.source` where it came from; `workflow show <name> --json` gives each file's `installedFrom`.

- A workflow file is Python that runs with the user's own permissions, so install one only from a URL or path the user gave you, and pass `--sha256` when they have the file's hash, because it pins the exact file. Don't look for a workflow online to install.
- Plain `http://`, a bad file name or a bad `--sha256` fail with exit 2. A file that isn't a workflow fails with `WORKFLOW_INVALID`, a hash that differs from `--sha256` with `CHECKSUM_MISMATCH`, an HTTP error with `DOWNLOAD_FAILED` (all exit 6), and an unreachable host with `NETWORK_UNREACHABLE` (exit 7). Nothing is written.
- A file edited since it was installed, or one the CLI didn't install, fails with `INVALID_COMMAND_ARGS` (exit 2), as a name taken does for `workflow new`, `copy`, `capture` and `emit`. Don't pass `--force` unless the user agrees to lose that file. The workflow's other files are kept; `data.outrankedBy` lists its variants that run instead of the installed file on newer SDKs, with a warning.
- Run `workflow check <name>` before running one: an installed workflow is unchecked. The check covers only `pxz` calls, and a workflow can import only the standard library, `pxz` and the packages of the session's Python runtime, such as `numpy` (a session ignores `PYTHONPATH`), so one importing another module passes the check and fails when it runs. Only the workflow file is installed.

## The file

```python
# /// workflow
# description = "Repair, then decimate to a budget"
# requires-sdk = ">=2026.1"   # optional
# requires-cli = ">=0.1"     # optional
# gpu = "none"                # none, optional or required
#
# [params.triangles]
# type = "int"                # int, float, bool or str
# default = 100000
# help = "Triangle budget"
# ///


@step("repair")
def repair(p):
    algo.repairMesh([scene.getRoot()], 0.1)


@step("decimate", when=lambda p: tx.counts()[1] > p.triangles)
def decimate(p):
    tx.decimate_target([scene.getRoot()], ["triangleCount", p.triangles])
```

- The header is TOML in comments, read without a session. A parameter may also have `choices = ["a", "b"]`, and an `int` or `float` one `min` and `max` (inclusive); a value outside them is refused with exit 2. `requires-cli` names the CLI versions that can read the file; an older CLI refuses it with `WORKFLOW_INVALID` and a message to upgrade. A prerelease meets the version it leads to (`0.1.0-beta.1` meets `>=0.1`), unless the condition names a prerelease itself: `0.1.0-beta.1` doesn't meet `>=0.1.0-beta.2`. A nightly comes before every other prerelease of its version (alpha, beta, rc), since its version doesn't say which it includes: `0.1.0-nightly.20261113` doesn't meet `>=0.1.0-alpha.1`, but meets `>=0.1`.
- Steps run in file order, each with the parameters as `p`. `when=` skips a step whose condition is false; `gpu=True` skips one on a machine that can't render.
- A workflow runs after `from pxz import *` in a fresh namespace each run, so a variable from an earlier script can't change it. It can import only the standard library, `pxz` and the packages of the session's Python runtime, such as `numpy`. It also has `SDK_VERSION`, `sdk_at_least("<version>")` and `tx`, the code behind the CLI's own commands:

  | Function | Does |
  |---|---|
  | `tx.counts(occ=None)` | `(polygons, triangles)` under `occ`, or the whole scene; hidden geometry included |
  | `tx.polygons(occs, triangles=False)` | The polygon count under `occs`, or the triangle count with `triangles=True` |
  | `tx.diagonal(occ=None)` | The bounding box diagonal in mm, under `occ` or the whole scene |
  | `tx.remove_occluded(occ, level="polygons", resolution=1024, sphere_count=16, fov=90, adjacency_depth=1)` | As `algo remove-occluded`; `level` is `parts`, `patches` or `polygons`. Renders, so needs a GPU |
  | `tx.decimate_to_tolerance(occ, tolerance)` | As `algo decimate --tolerance` |
  | `tx.decimate_target(occs, strategy, ...)` | As `algo decimate --ratio` / `--triangles`, with `algo.decimateTarget`'s remaining arguments, positional (no keywords), then deletes the decimated CAD parts' patches. Use it rather than `algo.decimateTarget`, whose leftover patches make a later UV, normals or repair step fail |
  | `tx.check_uv_packing(occs, resolution)` | Raises, before the step changes anything, when packing the UVs under `occs` at `resolution` px needs more memory than the machine can spare, naming the largest resolution that fits |
  | `tx.warn(message)` | Adds `message` to the run's `warnings`, as a command's: for parts a step couldn't work on. A session an older CLI started hasn't got it: `getattr(tx, "warn", print)(message)` in a shared file |
- `print` goes to the session log and to the result's `output`.

## When the SDK changes

`workflow check <name> --session <id> --json` binds every `pxz` call against the session's SDK: the call exists, takes the arguments given, and gets every required one. A broken workflow fails with `WORKFLOW_BROKEN` (exit 6), and each problem in `data.results[].problems` gives its line, the call, what is wrong and the signature the SDK has now. Fix each call to that signature, because it is the loaded SDK's own, unless the workflow must keep working on older SDKs too (below). After a new SDK is installed, run `workflow check --all` in a session on it.

To keep one workflow working on several SDK releases:

- **One call:** branch inside the step with `if sdk_at_least("<version>"): … else: …`. `workflow check` skips the branch the session's SDK won't take.
- **A whole file:** add a variant `<name>@<version>.py` next to the base file. A session runs the newest variant its SDK has reached, else the base file.
- **A floor:** `requires-sdk = ">=<version>"` refuses an older SDK up front with `SDK_UNSUPPORTED` (exit 4).

A session started by a CLI from before workflows existed can't run or check one, and one from before `workflow capture` has no journal to capture (`SESSION_OUTDATED`, exit 4): start a new session.

## Outside the CLI

`unity transform workflow emit <name> --output <script>.py [--param <name=value>]` writes a standalone Python script that runs with plain `pxz` and no session: `python <script>.py --input <file> --output <file> [--<param> <value> ...]`. Each workflow parameter is a kebab-case flag (`--max-sag`; a bool takes `--merge` / `--no-merge`; one named `input`, `output` or `help` is `--param-<name>`), defaulting to the `--param` value given to `emit`, and `--help` lists them. `--output`'s folder is created and its extension picks the format. It exits 0 when done, 1 when the run fails, 2 on bad arguments. The older positional `python <script>.py <input> <output>` still works. The same file runs as a Unity Pipeline Automation custom-script action (arguments are the flags) and in a session with `unity transform run file <script>.py -- --input <file> --output <file>`, with absolute paths, because the script runs in the session, whose working directory is not yours. It uses the base file; `--sdk-version <version>` picks that SDK's variant. Use `--output` rather than redirecting with `>`: the CLI writes UTF-8, and Windows PowerShell 5.1's `>` writes UTF-16, which Python can't run. It won't replace an existing file without `--force`. Without `--output` it prints the script. Run inside a Python process where `pxz` is already initialized, the script uses that session instead of starting one. Run with no input on an empty scene, it prints its usage and exits 2.
