---
name: transform-cli
description: "Drives the Asset Transformer SDK (Pixyz) with Transform CLI, outside the Unity Editor. Make sure to use this skill when the user asks to convert (e.g. STEP to GLB), optimize, render or export CAD or 3D files, run a pxz script, or test an SDK build."
---

Run Asset Transformer SDK work as a sequence of `unity transform` shell commands against one live scene, without writing a bespoke host.

## Important

- Create a restore point you can roll back to if your changes fail. Never overwrite the source model: write every export to a new path, so the untouched source is the restore point and a fresh session can re-import it.
- Do only what's asked. Don't change unrelated assets or files, and don't close sessions you didn't open, because another job may own them.
- Pass `--json` to every command whose output you read, because the human output is prose and changes between releases.
- Avoid long explanations.

## Glossary

| Term | Meaning |
|---|---|
| Session | A detached Python host that owns one `pxz` runtime and one scene, and outlives the CLI call that made it. |
| Active session | The session a command uses when neither `--session` nor `TRANSFORM_SESSION` names one. Every process of the same user shares it. |
| Named SDK | An SDK build the CLI knows by name: `managed` (downloaded by the CLI) or `linked` (the user's own folder). |

## Step 1: Read the docs

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation](#full-topic-map) that covers what you intend to do or fix, and check if you've missed anything.
2. Name the page(s) or command(s) you read in your response.

This is because your training knowledge about the CLI and the Asset Transformer SDK might be out of date, incorrect, or for a different SDK build. Flags and `pxz` signatures change between builds.

## Full topic map

The CLI and the loaded SDK describe themselves for the exact build in use; prefer them over any other source.

- **`unity transform --help`**, **`<group> --help`** and **`<group> <command> --help`** — the command groups and the flags this build has. Commands are `<group> <command> [args] [--session <id>]`. `--help --json` gives the whole tree.
- **`unity transform doctor --json`** — whether this machine can run sessions, each failed check with its fix.
- **`unity transform sdk guide --json`** — the SDK's coordinate system, units and API patterns.
- **`unity transform sdk api <module.function> [<module.function> ...] --session <id> --json`** — the parameters and return value of each `pxz` call you name, from the docstrings of the SDK your session runs. `sdk api <words>` searches.
- **`unity transform scene formats --session <id> --json`** — the file formats the loaded SDK imports and exports.

Load these reference files only when the task needs them:

- [references/sessions.md](references/sessions.md) — which session a command uses, agents side by side, busy sessions, the UI, starting over and keeping a scene.
- [references/named-sdks.md](references/named-sdks.md) — installing, registering, licensing, switching and comparing SDK builds.
- [references/scene-editing.md](references/scene-editing.md) — targeting one part, editing the scene tree, and undo.
- [references/workflows.md](references/workflows.md) — the built-in workflows, capturing and writing one, and keeping one working across SDK releases.
- [references/exit-codes.md](references/exit-codes.md) — the exit codes, the `--json` envelope, and what to do about each error code.
- [references/troubleshooting.md](references/troubleshooting.md) — every error string, its root cause and its fix, and the spellings older builds used.

## Step 2: Get the context

1. Run `unity transform version --json`. If `unity` isn't found, or has no `transform` plugin, run `transform version --json` and drop the `unity` from every command in this skill, because a standalone install puts only `transform` on PATH. Keep that spelling for the whole job, and name it in your report.
2. Run `unity transform sdk status --json` to learn the default SDK and every installed name. If it reports none, follow [references/named-sdks.md](references/named-sdks.md) first, because every session needs one.
3. Run `unity transform session list --json` to learn which sessions already exist.
4. If anything above failed, run `unity transform doctor --json` and act on the first check with `status: "fail"`, because each names its cause and its fix.

## Step 3: Run the pipeline

If the user names a running session, work in it: pass `--session <id>` on every command, skip steps 1, 2 and 17, and don't reset it, because its scene is theirs. Import into it only when asked. If the task is instead one self-contained script, run it with `run file` or `run code` and `--isolated`, because that session closes after the run and can't touch anyone else's. Otherwise:

1. Create the session: `unity transform session new --name <job> --json`. Pass `--session <job>` on every later command, because another agent or person can change which session is active.
2. Import the source: `unity transform scene import <source> --session <job> --json`.
3. Record the baseline: `unity transform scene info --session <job> --json`.
4. Run `unity transform workflow list --json` to find a workflow that fits the job, because it is a tested recipe whose `data.steps` report each step's counts.
5. If none fits, go to step 12.
6. Run `unity transform workflow show <name> --json` to read its parameters, because their defaults may not suit the model at hand.
7. If its `origin` isn't `built-in` and the user didn't name it, show them its `path` and ask before you run it, because it is Python from the project or their own folder.
8. If they don't agree and no entry in `shadows` has `origin` `built-in`, go to step 12.
9. If they don't agree, run `unity transform workflow show <path> --json` on the `path` of the `shadows` entry whose `origin` is `built-in`, and pass that path for `<name>` in steps 10 and 11, because the name still resolves to their file and a `user` entry in `shadows` is unapproved Python too ([references/workflows.md](references/workflows.md)).
10. Run `unity transform workflow check <name> --session <job> --json`, because it checks every `pxz` call against the loaded SDK before anything changes the scene.
11. Run `unity transform workflow run <name> --param <name=value> --session <job> --json`, then go to step 15.
12. For each processing step, run the `algo` command named after the `pxz.algo` call, with `--session <job> --json`.
13. For UVs, run steps 6 to 11 with `generate-uv` for `<name>`, then come back here, because UVs come from that workflow, not an `algo` command ([references/scene-editing.md](references/scene-editing.md)).
14. For a call no command makes, run it with `run code` or `run file` and `--session <job> --json`.
15. Run `unity transform scene info --session <job> --json` after the workflow or each processing step, and compare it with the baseline.
16. Export to a new path: `unity transform scene export <new-output> --session <job> --json`. The extension picks the format.
17. Close the session: `unity transform session close --session <job> --json`.

## Decisions

- If a step needs `scene screenshot` or `algo remove-occluded`, read the `gpu` check in `unity transform doctor --json` first, because only those two commands need a graphics driver. If it fails, or either reports `NO_GPU`, skip that step and say so in your report, because the rest of the pipeline runs without one.
- If the user wants to change one part, or to delete, merge, move, hide, duplicate or regroup parts, read [references/scene-editing.md](references/scene-editing.md) first and use the command it names instead of a script, because each is one undo step.
- If each result must start from the source, such as LODs, use `workflow run lods`, which builds each level from a copy of the source and prints each level's triangle count and occurrence id in `data.output`, for `scene export --occurrence`. It fails (exit 6) on a scene with several top-level occurrences, as an assembly often has: pass the part with `--param occurrence=<id>`, or group the parts under one first ([references/workflows.md](references/workflows.md)). For any other result that must start from the source, run `scene undo --to <step>` before each one, naming the last step every result shares (`scene import`, or `algo tessellate` when each result needs the CAD tessellated the same way), instead of importing again, because it returns the scene to that point in a moment, while importing a large CAD file again can take minutes, and a scene still holding earlier work skews every count. Run `session reset` and import again only when `scene history` no longer lists the import (after `scene open`, or in a session someone else's commands share).
- If a step made the scene worse, run `scene undo` (or `scene undo --to <step>`, with the step names from `scene history`) instead of re-importing, because each change is one undo step and the steps before it stay as they were ([references/scene-editing.md](references/scene-editing.md)).
- If the user wants to look at the scene or change it by hand, run `ui open`, then `ui close` when they're done, because an open UI costs ~450 MB ([references/sessions.md](references/sessions.md)).
- If the user asks for a newer CLI, tell them to run `unity plugin upgrade transform`, and run it yourself only if they asked you to update, because the Unity CLI owns that install and `unity transform upgrade` refuses with `UPGRADE_UNSUPPORTED`. Only a standalone install runs `transform upgrade` (`--dry-run` checks).

## Results and errors

Read every result from the `--json` envelope, and check `success` before you run the next command, because every later step builds on the scene the last one left. On a failure, act on `errors[0].code`; [references/exit-codes.md](references/exit-codes.md) gives every code. The ones whose right action isn't obvious:

| Code | Cause | Action |
|---|---|---|
| `SCRIPT_FAILED` | The script raised inside the SDK; its traceback is in `data.output`. | Fix the Python. Don't restart the session, because it is healthy. |
| `SESSION_NOT_FOUND` | The session named isn't running. | If you created it, create it again and re-import; otherwise tell the user. Don't switch to one of `data.candidates`, because they may be other jobs'. |
| `NO_LICENSE` | The SDK has no license to run with. | Run `license status --json` and tell the user. Ask before any `license` change, because it changes the machine's license. |
| `SDK_SHARED_MEMORY_LEFT` | Shared memory an earlier SDK process left is in the way; the license may be fine. | Don't run `license activate`. Tell the user to close every session, then run the cleanup the message names (it needs root) or restart. |
| `SDK_INSTANCE_LIMIT` | The SDK already runs as many times as its license allows; the license is fine. | Close a session you started and no longer need, then retry; if none is yours, tell the user. Don't touch the license. |

## Constraints

- Don't write `pxz` code from memory. Read `sdk guide` first, then look up every call you haven't used yet in this job, all in one `sdk api <module.function> <module.function> ...`, because signatures drift between SDK builds and each `sdk api` starts the SDK again. If a call still raises (`SCRIPT_FAILED`), trust the error.
- Never send a command's output to `/dev/null`, `$null` or `Out-Null`, or pipe it where its exit code is lost, because a refused step then looks like one that worked.
- Don't trust exit code `0` alone. Compare `scene info` counts, because some algorithms report success and change nothing.
- Write absolute paths inside `pxz` code, because a session runs in `~/.transform` and a relative path in a script resolves there.
- Pipe code that has quotes or several lines to `run code -`: in PowerShell, run `$OutputEncoding = [System.Text.UTF8Encoding]::new()` once, then `@'...'@ | unity transform run code - --session <job> --json`; in bash, a `<<'EOF'` heredoc. Windows PowerShell drops every `"` from an argument, so `run code 'print("hi")'` fails with `NameError`, and without that encoding it pipes `é` as `?`.
- Don't assume a clean script context, because variables from earlier `run` calls persist in a session.
- Don't expect `sdk use` to change a running session, because a host keeps the SDK it started with. Start a new session with `--sdk <name>`.
- Don't call `view` functions from a script on a machine whose `gpu` check fails, because they can end the session.
- Don't use spellings you remember from older builds (`run info`, `sdk help`), because they were removed; [references/troubleshooting.md](references/troubleshooting.md) maps each to its replacement.
- Don't run `session close --all`, because it closes sessions other jobs own.
- Don't hand-roll a host with `python -c "import pxz"`, because you'd be testing your own bridge instead of the SDK.

## Example

Request: "Decimate `C:\cad\pump.step` to about 100k triangles and give me a GLB."

```powershell
unity transform session list --json             # -> [] : no other sessions, no conflict
unity transform session new --name pump --json
unity transform scene import C:\cad\pump.step --session pump --json
unity transform algo tessellate --session pump --json
unity transform scene info --session pump --json # -> triangleCount: 1840212 (baseline)
unity transform algo decimate --help            # -> --ratio <percent> | --triangles <n> | --tolerance <mm>
unity transform algo decimate --triangles 100000 --session pump --json
unity transform scene info --session pump --json # -> triangleCount: 98433
unity transform scene export C:\cad\pump_100k.glb --session pump --json
unity transform session close --session pump --json
```

Report: triangles went from 1,840,212 to 98,433, and the source `pump.step` is unchanged.

## Final step

Do the following checks:

- Check every command exited `0` with `success: true`.
- Check `scene info --json` shows the change the user asked for, against the baseline.
- Check each export file exists at its new path and the source file is unchanged.
- Check `session list --json` shows no session you opened and didn't mean to keep.

Stop there unless the user asked for proof, such as a `scene screenshot`, or you suspect a specific fault. There is no Unity console to check with `console status`, because this skill changes no Unity project.

If a check fails, read the session's log with `unity transform session logs --session <id> --all` (it also works after the session closed), because the host's stdout, stderr and tracebacks go there and nowhere else. Then go back and reread the [docs](#full-topic-map) to find out what you missed.

## Final report

Give the user a concise but not dense checklist of what you did and why, with before/after counts from `scene info`. If the user needs to follow up with more steps or decisions, list them. For example:

```
I made the following changes:

[✓] Imported pump.step into session `pump` and tessellated it.
[✓] Decimated 1,840,212 → 98,433 triangles.
[✓] Exported C:\cad\pump_100k.glb. pump.step is unchanged.
[✓] Closed session `pump`.

Follow-up:

1. Occluded-part removal was skipped because this machine has no graphics driver. Re-run on a machine with one to drop hidden parts.
```
