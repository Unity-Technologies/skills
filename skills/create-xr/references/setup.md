# Setup procedure

Relative paths in this file resolve to the skill directory.

Configure the project for the target devices: platform modules, packages, build profile, OpenXR, and player settings.

## Docs

- `com.unity.xr.management`: `EndUser.md` (XR Plug-in Management settings and loaders).
- `com.unity.xr.openxr`: `project-configuration.md` (OpenXR features and interaction profiles per platform), `features/metaquest.md`, `project-validation.md`.
- `com.unity.xr.meta-openxr` (Quest): `get-started/project-settings.md`.
- `com.unity.xr.androidxr-openxr` (Android XR): `get-started/project-setup.md`.

## Run the setup script

For Meta Quest, PC VR, and Android XR, one script does everything. List the devices the user named, primary first; add `--hands` if they want hand tracking (controllers and hands, or hands only) — it installs XR Hands and enables the OpenXR hand features, with or without a rig:

```
bash "<SKILL_DIR>/scripts/setup/setup.sh" quest
bash "<SKILL_DIR>/scripts/setup/setup.sh" quest pcvr
bash "<SKILL_DIR>/scripts/setup/setup.sh" --hands quest
```

Runs ~20× longer than `status.sh` and prints progress; set the timeout accordingly and don't pipe its output.

It prints `OK` / `SKIP` / `WARN` / `FAILED` per step, waits for reloads itself, and ends with `=== XR base setup complete ===`. Every step is idempotent and already-done work is skipped, so after fixing a `FAILED` cause, re-run the same command. `FAILED [n/6 …]` names the step: step n is `scripts/setup/n_*.sh`, which takes the same arguments and can be re-run on its own. Do not rewrite the script or replace it with your own install or wait loops.

| `FAILED` message contains | What to do |
|---|---|
| `is not installed for` (modules) | Ask the user before installing the module with the `unity install-modules` command the message names — large download, may need an Editor restart |
| `blocked by a dialog` | Ask the user to answer the named dialog in the Editor — never click it for them — then re-run |
| `no Pipeline command for` (exit 4) | Run the `unity command --query` the message names. If the command exists under a new name, re-run with the `PIPELINE_CMD_<KEY>="<name>"` override it names; otherwise report the line to the user |
| `did not start` (package add) | A parameter changed: check the command's parameters with the `--query` the message names, report it to the user |
| `could not create the profile` | Ask the user to add the profile in **File > Build Profiles**, then re-run |
| `not found on the … tab` | A required OpenXR feature is missing (package version?) — report the line to the user |
| `EVAL_FAILED` with compiler diagnostics, or `EVAL_NOT_FINISHED` | Report the line; for a timeout, wait ~10 s and re-run once |

If the script reports `Input Manager (old) -> Both`, tell the user the Editor must restart before input works.

Apple Vision Pro isn't covered by this skill.
