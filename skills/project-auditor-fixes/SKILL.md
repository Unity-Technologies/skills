---
name: project_auditor_fixes
description: "Instructions for how to use Project Auditor to find and fix a list of issues."
version: 1.0.0
—
Using Project Auditor
Project Auditor carries out a static analysis scan of the Unity Project and reports a list of CSV issues that may be fixed.  Trigger, poll, read.

## Prerequisites
To use Project Auditor you'll need the Unity CLI installed - see the unity-cli skill for more details.
Once you have that, the audit and audit_status commands run Project Auditor checks.

```bash
unity command audit                              # optional: --categories Code,ProjectSetting --output my.csv
unity command audit_status                       # repeat until a terminal status is returned
```

`audit_status` terminal status are `completed` (with `csvPath` + `issueCount`), `failed`, `unavailable`, and
`interrupted` (a domain reload killed the scan — just re-run `audit`). Polling is responsive while a
scan compiles assemblies and is busy on the main thread. You can only run one scan at a time: a second `audit`
returns `busy`. There is no cancel — stop polling to abandon a scan.

The output CSV has columns: `Category, Severity, Areas, Description, RelativePath, Line, DescriptorId, Recommendation`
(these are diagnostics only, so every row is something to fix; `Recommendation` says how to do it).

> **Requires Project Auditor plus its rules.** `unavailable` means the Editor has no Project Auditor,
> or it has no analysis rules — in a built-in-module Editor the rules live in the separate
> `com.unity.project-auditor-rules` package (`unity command package_add --identifier
> com.unity.project-auditor-rules --confirm true`). Read the `message` field; the command never reports
> an empty `completed` scan that would look like a clean project.

## Gotchas

- **`set_autotick` first.** Without it, recompile and tests can hang while the editor is
  unfocused. The package's watchdog relies on the tick loop staying alive.
- **Hot reload needs the game running.** `reload_file_override` / `reload_file` apply to a live
  game — enter Editor Play Mode (`unity command editor_play`) first, or run a dev Player.
- **Player-only commands need a dev Player.** `log`, `set_timescale`, `runtime_status`, etc. hit
  the Runtime server, which does not run in the Editor.
- **Async commands poll.** `recompile`→`recompile_status`, `run_tests --async_tests`→
  `test_status`. Never assume completion from the trigger call's response.
- **Target a specific instance** with `--instance host:port` or `--project-path <path>`
  when more than one is running.

## Best Practice

When fixing a list of issues it's best practice to have a test case to make sure things are still working correctly.  This might be a particular scene to open and start play mode, or a particular platform to build and run.  If this isn’t present and there are no instructions, suggest a test case to the user and ask for confirmation.  Run the test case before and after changes to make sure the project is still working.

As you work, make sure any changes are committed in alignment with user version control setup.  Wherever possible, commit step-by-step, breaking things down into changes that a user can easily understand.

## Recipe
1. Make sure the project opens cleanly (with no compile or other errors in the Editor).  If there are issues present, check with the user; they may know about them and want to ignore them.  Run the test case and make sure you know what state it's in, fixing things if necessary.
2. Commit any changes so that we start the upgrade process with clean version control.
3. Open the project in the Unity Editor.  Then use Unity CLI to run the audit command.
4. Group the issues by file and by type.
5. Choose a type of issue to work through one group or file at a time.
5.a. Default to fixing settings issues on assets or the projects by type.
5.b. Default to fixing code issues by file and subfolder.
5.c. However, it’s important to fix issues in human manageable and reviewable batches.
5.c.i. Making the same small change across a large number of files (for example, switching a boolean setting across many textures) is probably best done in one commit, or a commit per subfolder.
5.c.i.. When making code changes, the code should compile between each commit.
5.d. Make changes based on the recommendations from Project Auditor. Commit each change once there are no errors in the editor.

