# Writing `eval` and Pipeline commands by hand

Read this before running any `eval` or Pipeline command that is not one of the skill's scripts.

## Find the command name first

Pipeline command names change between versions — do not guess one. `unity command` lists the groups; `unity command --tag <tag> --detail full --format json` lists a group's commands and parameters. Tags are not group names (package commands: `packages`; build profiles: `build/settings`). If a tag returns nothing, use `unity command --query <word>`. Subgroups are typed with a dot: `unity command build.target list` (the space form fails with "command not found").

## Domain reloads

Package installs, build target switches, Build Profile activation, and sample imports trigger a reload or recompile. During it the Pipeline server is unreachable and commands fail or time out — expected; do not troubleshoot it.

- Wait until the Editor responds again (`source "<SKILL_DIR>/scripts/_eval.sh" && wait_ready`) before the next step.
- If an `eval` times out, wait 10–15 s and retry the same command unchanged.
- Keep each `eval` to one change.
- Poll long operations instead of `--wait true`.
- The first Android switch or profile activation reimports for minutes.

## `eval` code

`eval` compiles a **statement block, not a file**:

- **No `using` directives** — `using UnityEngine;` is read as a disposal statement (`CS0210`).
- **Fully qualify every type** — bare `AssetDatabase` fails (`CS0246` / `CS0103`).

The recipes in this skill are already fully qualified.

## Type names

Look up type names and namespaces at `https://docs.unity3d.com/Packages/<package>@<VERSION>/api/` (`<VERSION>`: the installed package version, major.minor). Don't rely on names you remember, because XR types have moved between namespaces across versions.
