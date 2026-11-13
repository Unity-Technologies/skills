---
name: create-xr
description: >-
  Sets up a Unity project for XR. Use when user asks for XR, OpenXR, Quest
  graphics or XR rig.
required_editor_version: ">=6000.1"
---

Set up a Unity project for XR: configure it for the target devices, optimize graphics for Quest, then add a configured XR rig.

**Goal: the project builds for the target device(s) with XR enabled, and the user can enter the scene on their headset with working controllers (or hands) and the movement they chose — without building it.**

**`<SKILL_DIR>`** = the "Base directory for this skill" shown at the top of this document.
**`<PROJECT_PATH>`** = absolute path to the Unity project on disk.

## Critical Rules

- Do only what's asked. Don't change unrelated assets or files. Avoid long explanations. Unrequested changes are hard for the user to spot and undo.
- Create a restore point you can roll back to if your changes fail: check the project's version control has no uncommitted changes, or otherwise copy `Packages/` and `ProjectSettings/` to a folder outside the project.
- Don't add verification code or logging to the project; read state back through the scripts or `eval`. Anything you add stays in the user's project and console.
- Do NOT specify package version numbers — let the Package Manager resolve the latest compatible version, because a pinned version may not support the project's Unity version.
- Do NOT edit `Packages/manifest.json`, `.unity`, `.prefab`, `.asset`, or `.meta` files directly, because the open Editor doesn't validate hand edits and can overwrite them or break on a malformed file.
- Do NOT write C# files into the project — C# runs only through `eval`, because a script file forces a recompile and stays in the user's project.
- Do NOT dismiss Editor dialogs with UI automation, simulated clicks, or key presses, because a dialog asks for a decision that belongs to the user (such as restarting the Editor). If the Editor is blocked by a dialog, tell the user what it says and ask them to answer it.
- Do NOT write your own package-install or wait-for-Editor loops — use the skill's scripts and the `install_package` / `wait_ready` helpers in `scripts/_eval.sh`, because they already handle reloads, timeouts, and blocking dialogs.
- Do NOT use reflection or non-public APIs, because they change between Unity versions without notice. If no public API or Pipeline command covers a step, ask the user to do it in the Editor.
- Use a Pipeline command when one covers the step; `eval` only when none does, because commands are validated and don't compile C# on every call.
- Read back the result of every change before the next step, because a call can report success while the Editor is reloading and the value didn't change.

## Docs

Before you start a procedure or diagnose a failure:

1. Open and read the doc pages listed in that procedure's file.
2. Name the files you read in your response.

This is because your training knowledge about Unity and its XR packages may be outdated, incorrect, or for the wrong version.

The pages are markdown files in `<PROJECT_PATH>/Library/PackageCache/<package>@*/Documentation~/` (installed packages only). Read only the `.md` files in that single subfolder. Don't open non-.md files or other Library or PackageCache folders, because that takes too long. If a page is missing, find it in that folder's `TableOfContents.md`; if the folder has no `.md` files, use the online docs. The decisions in this skill still apply.

Prefer `WebFetch` over `WebSearch`, because it's faster and lands on the exact page. Replace `<VERSION>` before fetching:

- **Packages on docs.unity3d.com** (`https://docs.unity3d.com/Packages/<package>@<VERSION>/manual/`): the installed package version, major.minor (e.g. `@1.19`). Read it from `Packages/packages-lock.json`, because `manifest.json` can hold a range.
- **Unity Manual on docs.unity.com** (`https://docs.unity.com/en-us/engine/<VERSION>/manual/<page>.md`): the project's Unity version, major.minor (e.g. `6000.3`).

## 0. Connect

Connect to the project's Editor before anything else, in this order (the `unity-cli` skill has the command details):

1. **Pipeline package first.** If `Packages/manifest.json` has no `com.unity.pipeline`, run `unity pipeline install` before opening the Editor, because Unity reads the manifest when it loads the project. Against an already-open Editor:

   | Message | Cause | Fix |
   |---|---|---|
   | `PIPELINE_MANIFEST_WRITE_FAILED` | The open Editor holds the manifest | Ask the user to close the Editor, then run the install again |
   | `STATUS_PIPELINE_LOAD_PENDING` (from `unity status`) | Installed, but loads only when the Editor window regains focus | Ask the user to switch to the Editor window |

2. **Then the Editor.** `unity status` only lists Editors that already have the Pipeline package, so before opening one, check for `Temp/UnityLockfile` in the project: if it exists, an Editor probably has the project open already. Don't run `unity open` (a second Editor would replace it, and the user could lose unsaved work); ask the user to switch to the Editor window instead. If they say Unity isn't open, the file is left over and you can open it. If no Editor is open on the project, run `unity open "<PROJECT_PATH>"`, then `unity status --until-ready`. The first open imports the project and can take several minutes: if the wait ends before the Editor is ready, run it again.
3. **Then check that the Pipeline version works with this skill.** From `<PROJECT_PATH>`, run `bash "<SKILL_DIR>/scripts/pipeline_check.sh"`, with `--switch` if you installed the package in step 1. If the version doesn't work with the skill's scripts, `--switch` changes it to one that does: tell the user in the final report. Without `--switch` it prints `ASK` instead: the package was already in the project, so ask the user before changing it, re-run with `--switch` if they agree, and stop if they don't. If it prints `FAILED`, report the line and stop.

Don't run the skill's scripts until the Editor is connected. Do not check the Pipeline commands by hand: `status.sh` (step 2) checks every command the skill needs and stops with exit code 4 naming any that is missing — then follow its message.

Run the skill's scripts from `<PROJECT_PATH>`, because with several Editors open the CLI picks the project from the current folder; the scripts stop if it isn't a Unity project. Run them as the procedures say; do not read their source, because their output tells you everything you need. Read [eval-guide.md](references/eval-guide.md) only before writing an `eval` or Pipeline command that is not one of the scripts.

## 1. Questions

Ask about anything the user has not already specified, **in one message**:

1. **Target device(s)** — Meta Quest (Quest 2, 3, 3S, Pro), PC VR (SteamVR), Android XR (Samsung Galaxy XR, etc.). More than one allowed. **Apple Vision Pro:** this skill doesn't cover it. If the user asks for it, say so and continue the skill with any other devices they named.
2. **Input:** controllers only / controllers and hand tracking / hand tracking only — setup configures hand tracking from this answer, so ask it even when no rig is wanted.
3. **Rig:**
   - **Interactions:** near (grab up close) / far (point at distant objects) / poke (press buttons) — any combination; UI interaction is always included
   - **Movement:** teleport / continuous / both / stationary
   - **Turning:** snap / continuous / none

Skip question 3 for narrow requests that don't involve a rig (e.g., "optimize my project for XR", "just install the XR packages", "configure OpenXR"); otherwise ask it, because the rig answers decide every rig override. For hand tracking only, skip movement and turning — this skill does not configure hand-based locomotion.

If the user leaves the rig choices to you ("set it all up", "just do it", "use the defaults"), use the default rig: the controller rig, or the hands rig if they said hands, with its default settings — near, far and poke, teleport and continuous movement, snap turn. For hand tracking only, the default rig has no movement or turning: say so in the report instead of listing teleport, continuous movement and snap turn. Don't change or turn off anything beyond what the input choice needs, and say in the final report that you used the default rig. If the request is a concrete task that spells out what to build (e.g., "add a rig with hands and controllers and a cube I can grab"), do the same for the rig questions it leaves open, and list those defaults in the final report. A vague or short request ("make it run on Quest") is not a hand-over: ask the missing rig questions in one message before changing anything. Don't turn the user's goals into rig choices ("look around" doesn't mean stationary, "pick things up" doesn't mean near only): if the request doesn't name the rig pieces and doesn't hand the choices over, ask.

If the user asks for mixed reality, passthrough, or other environment features, tell them this skill does not yet configure MR, passthrough, or any environment-related features, and continue with the rest of the setup.

Tell the user what this skill will change, in the same message as your questions, or at the top of the final report if you asked nothing: it installs XR packages and samples, switches the build platform, and changes project, XR and graphics settings. If it adds a rig, it also changes the scene. If the project isn't under version control, suggest the user commit or back it up first if they want to be able to undo this.

Map variations: "AVP" / "Vision Pro" → Apple Vision Pro (not covered by this skill), "Galaxy XR" → Android XR, "Quest 3" / "Meta Quest" → Meta Quest, "Index" / "Vive" → PC VR. If the user asks how devices differ, explain and return to the question. Explain XR terms in plain language if the user seems unfamiliar with them.

## 2. Check

Run `bash "<SKILL_DIR>/scripts/status.sh" <devices>` with the same arguments the setup script takes (devices, plus `--hands`) (read-only, one call). It prints one line per procedure: `setup`, `quest-graphics` (Quest only): `done` / `todo`; `rig`: `present` / `not present`; then `console`: `compile ok` / `compile failed`. Run only the procedures that are not done, because each one takes minutes even when it skips everything. If every procedure the user asked for is done, report the status lines and stop. Do not inspect the project any other way first, because `status.sh` already reads what the procedures need.

## 3. Run the procedures

Run them in this order, because each one depends on the one before it. Read each procedure only when you reach it, so you load only what you use, and finish one — including its done check — before starting the next.

Procedures 1–2 are the **base setup**: packages (including the XR Interaction Toolkit), build profile, OpenXR, and — for Meta Quest — Quest graphics. Procedure 3 adds the rig.

| # | Procedure | Run when | Skip when | Done when |
|---|---|---|---|---|
| 1 | [setup.md](references/setup.md) | Always: check, then do what's missing | Never | Target devices confirmed; base packages, profile, OpenXR, player settings set |
| 2 | [meta-graphics-setup.md](references/meta-graphics-setup.md) | Quest is a target, and the request is a full setup or mentions graphics | No Quest target, or a narrow request that doesn't mention graphics* | Quest URP asset assigned, single-pass, foveated on |
| 3 | [rig.md](references/rig.md) | Always, unless skipped | Narrow request that doesn't mention a rig* | XRI installed; linked rig; read-back matches answers |

\* E.g., "just install the XR packages", "configure OpenXR".

Setup always runs all its steps (packages, build profile, OpenXR, player settings), because each needs the one before. For a narrow request, tell the user that before running it.

The procedures point to their supporting files when they need them; do not read these ahead of that point: [setup-manual.md](references/setup-manual.md) (when the setup script can't be used), [rig-pitfalls.md](references/rig-pitfalls.md), [rig-recipes.md](references/rig-recipes.md), [rig-structure.md](references/rig-structure.md).

- If graphics can't finish (e.g., the project isn't on URP), tell the user and continue to the rig, because the rig doesn't depend on it.
- If everything is already done, report the current state to the user — do not re-run any steps.

## 4. Final step

The done checks confirm each setting was saved, but a saved setting can still break the project, so finish with these checks. If one fails, reread the pages in [Docs](#docs) for that procedure, fix the cause, and run the checks again.

1. Re-run `status.sh` with the same arguments, then run `unity command console status --format json` and confirm `groundTruth.consoleErrors` is 0 and `compilationFailed` is false. If there are errors, list them in the report; investigate them only if compilation failed or a script printed `FAILED`. Stop here unless the user asked for proof that the result works or you suspect a fault.
2. Only to chase a suspected fault, enter Play mode once and read the state tied to that fault through `eval`. Without a headset connected the XR loader doesn't start in Play mode; that is expected, not a fault. Add no logging and take no screenshots, because they take too long. Exit Play mode and check the console again.

## 5. Report

End with a short report: what each procedure did and any defaults you chose; side effects the user didn't ask for (a Pipeline version switch, build platform switch, Editor restart needed for input, Android used because Android XR is disabled); every rig override, revertible in the Inspector with **Overrides → Revert**; and what's left for the user. Offer to save the configured rig as a prefab variant.
