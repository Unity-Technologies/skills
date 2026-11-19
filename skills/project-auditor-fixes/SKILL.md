---
name: project-auditor-fixes
description: "Runs a Project Auditor scan on a Unity project and fixes the reported issues in small, reviewable batches. Use when asked to audit a Unity project and fix the code, asset or settings issues Project Auditor finds."
---

Using Project Auditor
Project Auditor carries out a static analysis scan of the Unity Project and reports a list of CSV issues that may be fixed.  Trigger, poll, read.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Run C# with `unity command script eval --code '<snippet>'`. Don't use `using` directives or unqualified types in the snippet. The compiler reads using `UnityEngine` and a bare AssetDatabase, Volume, or Object causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

## Step 1

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

## Full topic map

The docs location depends on the project's Unity version, because Project Auditor is part of the Editor from Unity 6000.4, and a separate package (`com.unity.project-auditor`) in earlier versions. Check the Unity version first:

- **Unity 6000.4 and later**: If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual/project-auditor` folder. Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/analysis/project-auditor.md`. docs.unity.com has no 6000.4 pages, so for 6000.4 use `https://docs.unity3d.com/6000.4/Documentation/Manual/project-auditor/<local file>` instead.
- **Earlier versions**: The docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/com.unity.project-auditor@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time. If that folder has no `.md` files, use `https://docs.unity3d.com/Packages/com.unity.project-auditor@<VERSION>/manual/` instead.

Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`
- **docs.unity.com**: The product version, eg `7000.0`

Before fetching, check that `<VERSION>` is listed as a `## Version` heading in https://docs.unity.com/en-us/engine/llms.txt. If it isn't, use the docs.unity3d.com Manual instead. The two sites use different page names, so don't reuse the paths below. Instead, find each page by its title in `https://docs.unity3d.com/<VERSION>/Documentation/Manual/docdata/toc.js`, then fetch `https://docs.unity3d.com/<VERSION>/Documentation/Manual/<link>.html`, where `<link>` is the `link` value next to the title.

Read the Unity major.minor from `ProjectSettings/ProjectVersion.txt` (for example `6000.5` from `6000.5.0f1`, because the full build number gives a 404), and each package's major.minor from `Packages/packages-lock.json` (for example `1.0` for `com.unity.project-auditor`).

- [Project Auditor introduction](https://docs.unity.com/en-us/engine/<VERSION>/manual/analysis/project-auditor/introduction.md) — local `project-auditor/project-auditor-introduction.html`, package `project-auditor-introduction.md`: the issue areas Project Auditor reports, and the window.
- [Analyze your project](https://docs.unity.com/en-us/engine/<VERSION>/manual/analysis/project-auditor/analyze-project.md) — local `project-auditor/analyze-project.html`, package `analyze-project.md`: run an analysis, install the rules package, load reports.
- [Project issues introduction](https://docs.unity.com/en-us/engine/<VERSION>/manual/analysis/project-auditor/project-issues/introduction.md) — local `project-auditor/project-issues-introduction.html`, package `project-issues-introduction.md`: common project issues and resources for fixing them.
- [Domain reloading issues](https://docs.unity.com/en-us/engine/<VERSION>/manual/analysis/project-auditor/project-issues/domain-reloading-issues.md) — local `project-auditor/domain-reloading-issues.html`, package `domain-reloading-issues.md`: how domain reloading affects your project, and code fixes.

### API reference

The Project Auditor API is `Unity.ProjectAuditor.Editor`. Fixes can also change any Engine API (`UnityEngine`/`UnityEditor`) the reported code uses. In Unity 6000.4 and later, both are part of the Unity Engine. They are versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder (e.g. `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)` => `https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/rigidbody/addforce.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/vector3/ctor.md`), and indexers use `item` (e.g. `unityengine/vector3/item.md`).
   - For operators, fetch the class page (e.g. `unityengine/vector3.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'Rigidbody.AddForce.html'` => `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForce.html`).

Always use the docs for the project's Unity version rather than relying on training knowledge, since APIs are frequently marked obsolete between versions.

In versions earlier than 6000.4, `Unity.ProjectAuditor.Editor` is part of the `com.unity.project-auditor` package and is versioned by package version. Read the version from `Packages/packages-lock.json`, fetch and cache `https://docs.unity3d.com/Packages/com.unity.project-auditor@<version>/xrefmap.yml`, and grep it locally for a class or method instead of refetching per lookup. Don't read the whole file.

## Procedure

1. Get the project context: the Unity version from `ProjectSettings/ProjectVersion.txt`, and the `com.unity.project-auditor` or `com.unity.project-auditor-rules` version from `Packages/packages-lock.json`. If a value is missing, assume the latest Unity 6 version and the latest package version, name that assumption in your report, and carry on. If there's no project or Editor you can run the scan in, give the user the steps below for their project instead of stopping.
2. Read the [docs pages](#full-topic-map) for the issue types you intend to fix.
3. Check the [Prerequisites](#prerequisites), then follow [Best Practice](#best-practice) and the [Recipe](#recipe).
4. Run the test case again and compare it with the result from before your changes.
5. Do the checks in [Final step](#final-step).
6. Give the user the [Final report](#final-report), including which issues you fixed, and whether the checks passed.

## Prerequisites
To use Project Auditor you'll need the Unity CLI installed - see the unity-cli skill for more details.
Once you have that, the `diagnostics run_audit` and `diagnostics audit_status` commands run Project Auditor checks.

```bash
unity command diagnostics run_audit              # optional: --categories Code,ProjectSetting --output my.csv
unity command diagnostics audit_status           # repeat until a terminal status is returned
```

`diagnostics audit_status` terminal status are `completed` (with `csvPath` + `issueCount`), `failed`, `unavailable`, and
`interrupted` (a domain reload killed the scan — just re-run `diagnostics run_audit`). Polling is responsive while a
scan compiles assemblies and is busy on the main thread. You can only run one scan at a time: a second `diagnostics run_audit`
returns `busy`. There is no cancel — stop polling to abandon a scan.

The output CSV has columns: `Category, Severity, Areas, Description, RelativePath, Line, DescriptorId, Recommendation`
(these are diagnostics only, so every row is something to fix; `Recommendation` says how to do it).

> **Requires Project Auditor plus its rules.** `unavailable` means the Editor has no Project Auditor,
> or it has no analysis rules — in a built-in-module Editor the rules live in the separate
> `com.unity.project-auditor-rules` package (`unity command package add --identifier
> com.unity.project-auditor-rules --confirm true`). Read the `message` field; the command never reports
> an empty `completed` scan that would look like a clean project.

## Gotchas

- **`editor set_auto_tick` first.** Without it, recompile and tests can hang while the editor is
  unfocused. The package's watchdog relies on the tick loop staying alive.
- **Method reload needs the game running.** `script.methodreload reload` applies to a live
  game — enter Editor Play Mode (`unity command editor.playmode start`) first, or run a dev Player.
- **Player-only commands need a dev Player.** `console write`, `runtime set_time_scale`, `runtime status`, etc. hit
  the Runtime server, which does not run in the Editor.
- **Async commands poll.** `recompile`→`recompile_status`, `run_tests --async_tests`→
  `test_status`. Never assume completion from the trigger call's response.
- **Target a specific instance** with `--instance host:port` or `--project-path <path>`
  when more than one is running.

## Best Practice

When fixing a list of issues it's best practice to have a test case to make sure things are still working correctly.  This might be a particular scene to open and start play mode, or a particular platform to build and run.  If this isn’t present and there are no instructions, suggest a test case to the user and ask for confirmation.  Run the test case before and after changes to make sure the project is still working.

As you work, commit your changes in line with the user's version control setup.  Wherever possible, commit step-by-step, breaking things down into changes that a user can easily understand.

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
5.c.ii. When making code changes, make sure the code compiles between each commit.
5.d. Make changes based on the recommendations from Project Auditor. Commit each change once there are no errors in the editor.

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `script eval` rather than adding logging. Take no screenshots as they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Give the user a concise but not dense checklist of what you did and why, including any incidental side effects (for example, settings you toggled to test something, or temporary files you created). If the user needs to follow up with more steps or decisions, list them. For example:

```
I made the following changes:

[✓] Created a `ColorChange.cs` script.

Follow these steps to see the effect:

1. Drag the script onto a GameObject in the Hierarchy window.
2. Enter Play mode to see the color change effect.
3. Use the `Speed` property in the Inspector window to change the speed the color changes.
```
