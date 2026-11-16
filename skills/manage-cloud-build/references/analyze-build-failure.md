# Analyze a failed cloud build

Relative links in this file resolve to the skill directory's `references/` folder, where this file sits.

Diagnose one failed Unity Build Automation build, then tell the user what failed, which side the cause is on, and how to fix it.

## Contents

- Commands
- Glossary
- Step 1: Identify the build
- Step 2: Check the status
- Step 3: Read the evidence
- Step 4: Decide the side
- Step 5: Find what the user can change
- Step 6: Check the verdict
- Report

## Commands

- **CLI check:** `unity pipeline cloud-build builds failures --help`, because older CLI versions can't read failures, steps or logs.
- Run only these `unity` commands: `unity auth status`, `unity cloud org current`, `unity cloud project current`, `unity command console status`, `unity logs --console --level error`, `--help` on any command, and `unity pipeline cloud-build builds`, `builds get`, `failures`, `steps`, `log`, `artifacts` and `download`, `targets get` and `targets auditlog`. Never run `builds start`, `builds cancel` or any other command that changes something, because the user decides what to change and when to rebuild.
- Read the checkout only as [references/source-control.md](source-control.md) describes, never with a command that changes it, because the user decides what changes in it.
- Name a side only on evidence, because a wrong side sends the user to the wrong fix. When the evidence is thin, the side is Unclear.

## Glossary

- **Failure category**: a label Build Automation adds to a build when a known error pattern matches its log. `builds failures` returns them. Many failed builds have none.
- **Section**: a phase of the build. The log opens each one with an `INFO: + <section name>` line, for example `INFO: + Checking out Source Code` or `INFO: + Publishing`.
- **Side**: who has to act on the cause.
  - **Your project**: the project's code, assets, packages, settings, credentials or target configuration.
  - **Unity Editor version**: a defect or limit of the Editor version the target builds with. The fix is another version.
  - **Third party**: a service outside Unity, such as Apple's servers or a package registry.
  - **Build Automation**: the build service or its machines.
  - **Unclear**: the evidence doesn't decide it.

## Step 1: Identify the build

1. If the user gave a dashboard link, read the target from the segment after `buildtargets/` and the build number from the one after `builds/`.
2. If the user gave a target and a build number, use them.
3. If the user asked about their latest build, run `unity pipeline cloud-build builds --limit 100 <scope> --format json`, which lists builds, adding `--build-target <target>` if they named one, and save the output to the temporary directory. The builds are in `data.items[]`, newest first. Take the first whose `buildStatus` is `failure` if the user said it failed, and the first build otherwise. If the user said it failed and none has, read `--page 2` and on until one has, a page holds fewer than 100 builds, or you've read five pages, and then ask for the build number.
4. Run `unity pipeline cloud-build builds get <number> --build-target <target> <scope> --format json`, save it, and read fields from the saved file, because a build with unit tests can return megabytes. Pass its `buildtargetid` as `--build-target` on every later read, because the same value works for all of them.
5. If `builds get` returns not found, run `builds --limit 100 <scope> --format json` without `--build-target`, save it, and match the user's name against `buildtargetid` and `buildTargetName`, because users often give a target's display name. If exactly one target matches, run `builds get` again with its `buildtargetid`. If none or several match, ask the user which target.
6. If items 1 to 3 gave no target and build number, ask the user for both, because every read needs both.

## Step 2: Check the status

Read `buildStatus` from `data` in the `builds get` output:

- If it's `failure`, go on to Step 3.
- If it's `unknown`, go on to Step 3, because a build the service lost track of ends in this status.
- If it's `canceled` and `canceledBy` is any value other than `api`, the service stopped the build, so go on to Step 3 and look `canceledBy` up in the knowledge reference.
- If it's `success`, or `canceled` with `canceledBy` `api` or missing, tell the user there's no failure to analyze, because a person or a tool canceled it or it passed. Offer to analyze the target's latest failed build, and stop.
- If it's `queued`, `sentToBuilder`, `started` or `restarted`, tell the user the build hasn't finished, and stop, because the evidence isn't complete yet.
- Otherwise, tell the user the status isn't one you recognize, and stop.

Keep these fields from the same output for later steps: `unityVersion`, `xcodeVersion`, `androidSdkVersion`, `machineType.name`, `operatingSystemVersion`, `lastBuiltRevision`, `requestedRevision`, `scmBranch`, `cleanBuild`, `causedBy`, `canceledBy`, `credentialsOutdated`, `orgFk`, `projectGuid`, `deletedLogs`, `checkoutStartTime`, `created`, `finished` and `buildGUID`. Some can be missing; leave a missing field out rather than guess it.

## Step 3: Read the evidence

Read the sources in this order, because each is cheaper than the next. Run each read as its own command, and keep stderr out of any JSON you parse, because the CLI writes notices there. If a read fails, note it for the report and go on.

1. **Failure categories.** Run `unity pipeline cloud-build builds failures <number> --build-target <target> <scope> --format json`. Each entry in `data.failures[]` has a `failureIndicatorLabel`, a `displayName`, a `stage`, a `publicMessage`, an optional `link`, the `logline` where the pattern matched, and `stageMatch`.
   - An empty list is normal.
   - If `stageMatch` is `false`, the pattern matched outside the stage it was written for, so weigh that category below what the log shows.
   - Look each `failureIndicatorLabel` up in [references/failure-knowledge.md](failure-knowledge.md). If it isn't there, confirm its `publicMessage` against the log around its `logline`, and decide from the log as for a build with no category, because the knowledge reference lists only the categories seen before. If the `publicMessage` or the log line already says what to change, pass that on.
2. **Steps.** Run `unity pipeline cloud-build builds steps <number> --build-target <target> <scope> --format json`. `data` is an array of the build's steps in order, with `name`, `duration` in milliseconds and `messages[]`. Read the messages on every step, because the step that failed can sit in the middle, and its message can hold an error the log leaves out. Steps are often missing.
3. **The log.** Save it once, masking access tokens as you save, then read slices. `cut` keeps very long lines readable:

   ```sh
   unity pipeline cloud-build builds log <number> --build-target <target> <scope> --format human \
     | sed -E 's/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/***/g' > "<temp-dir>/build-<number>.log"
   grep -a -c '' "<temp-dir>/build-<number>.log"
   grep -a -n 'INFO: + [A-Z]' "<temp-dir>/build-<number>.log"
   grep -a -n '^\[error\]' "<temp-dir>/build-<number>.log" \
     | grep -v -E 'AssemblyUpdater|LogAssemblyErrors|FallbackError|BuildLogger|GuiMessage:ShowError|JAVA_TOOL_OPTIONS|Loading package information|\] +at ' | cut -c1-300 | tail -n 40
   sed -n '<from>,<to>p' "<temp-dir>/build-<number>.log" | cut -c1-300
   ```

   On Windows PowerShell, use `-replace` to mask, and `Select-String`, `Get-Content -Tail` and `Select-Object -Skip <n> -First <count>` to read slices.
   - If the line count is 0, the read failed: report the error the CLI printed, because the pipe hides its exit code.
   - Line numbers in the saved file match each category's `logline`, so read 30 lines either side of each `logline`.
   - The second `grep` drops `[error]` lines that are Unity's own noise, because they appear in builds that succeed too. If more than 40 lines remain, read the first ones with `head` as well.
   - Judge a line by what it says, not by its `[error]` prefix, because the prefix also marks warnings and progress lines from tools such as Xcode, fastlane and Unity, and a real error can sit on a line without it.
   - Find the decisive line by working back from the summary lines (`Unity player export failed`, `Scripts have compiler errors`, `Finished: FAILURE`) to the error that explains them, such as a `Player export failed. Reason:` line, an exception, or an `error CS` line.
   - If the decisive line only says that something failed without naming it, such as unit tests, an Xcode archive, an upload or a validation, find what failed before you decide, because the fix depends on it. Look in the earlier section that ran it, then in `testResults` from the `builds get` output for unit tests, then in a report among the build's artifacts. Name what failed.
     - `testResults` holds `unit_test_editmode` and `unit_test_playmode`, each with `passed`, `failed` and an NUnit `test-run` tree. Read only the test cases in the tree, the objects with a `methodname`, whose `result` is `Failed`, with their `fullname` and `failure.message`, because the tree can be megabytes.
     - `builds artifacts <number> --build-target <target> <scope> --format json` lists `data[]`, each with a `key` and `files[]` that have a `filename` and a `size`. The Xcode log is under `gym_log`, and unit test results are under a `unit_tests` key. Download only such a log or report, with `builds download <number> <filename> --build-target <target> <scope> --output "<temp-dir>/<file name>"`, never the build's own output, because that can be gigabytes. Read it in slices, like the build log.
   - If the decisive line is a stack trace with no error message above it, read the method and file names in its top frames, and the last `INFO:` lines before it, to tell what the step was doing and what it was reading, such as a configuration file, the checked-out project or credentials. Quote the first frame.
   - Unity compiles more than once. Decide from the `error CS` lines just above `Scripts have compiler errors`, because errors only in an earlier pass, such as `[Script Updater]` lines, often clear before the end.
   - The failing section is the last section header above the decisive line. Don't take the last section in the log, because `+ Publishing` runs even after a failed build.
   - Read no more than ten lines past a `Player export failed` line, and keep keyword searches above it, because the build machine's environment follows it. This limit wins over the 30-line window.
   - If `deletedLogs` is `true` or the read returns not found, there's no log. Decide from the other evidence.
   - A one-line log that starts with `[error] Unity Build Automation encountered an error while validating your build` means the build never reached a build machine. The rest of the line is the reason.
4. **Neighbouring builds.** Run `unity pipeline cloud-build builds --build-target <target> --limit 100 <scope> --format json`, save it, and read only `build`, `buildStatus`, `causedBy`, `canceledBy`, `created`, `cleanBuild`, `lastBuiltRevision`, `scmBranch`, `unityVersion`, `androidSdkVersion`, `xcodeVersion`, `machineType.name`, `operatingSystemVersion` and `changeset` from `data.items[]`. Print only the builds around this one and those the checks below need, and give a long run of failures as its count and first build, because a listing holds up to 100 builds. Read `--page 2` and on until you have a success older than this build, a page holds fewer than 100 builds, or you've read five pages, because the checks below start from the last success before the failures. Report any neighbour you don't have, such as a later build when this one is the newest, and decide from the rest.
   - A later clean build (`cleanBuild` `true`) that succeeded at the same revision after non-clean failures points at the build's cached files: suggest a clean build.
   - A later success at the same `lastBuiltRevision`, with the same other values, means the cause was probably temporary. A later success only at other revisions shows the failure hasn't repeated, not that it was temporary, because a commit may have fixed it.
   - If the failures started where exactly one of those values changed, name it as the likely trigger. If several changed together, list them without picking one.
   - If this build and the failures just before it were started automatically, count the builds since the last success, or back to the last page you read, and note the `created` of the first, because a schedule or automatic build keeps spending build time on a target that can't succeed until the cause is fixed. `causedBy` is `scheduling` for a build schedule, `polling` for a new commit, and `autoBuildLatest` for a new Unity version. A long run of `api` builds can come from a tool outside the target, such as a CI pipeline: say so, because the user pauses it there.
   - If the target failed and then succeeded before, take the last failure before each of up to three such successes, newest first. Save each one's log as in item 3 and search it for the decisive line's text with `grep -a -F`, even when it has the same category, because a summary category such as `unity_player_export_failure` covers different errors. If one holds that text, list what changed at the success that followed it, such as the branch, the Unity version or a clean build, as possible fixes. Name one as the fix only if it's the only value that changed or it relates to the decisive line, because the others may have changed for other reasons.
5. **Target changes.** Run `unity pipeline cloud-build targets auditlog <target> --limit 100 <scope> --format json` and save it. Each entry in `data.items[]` has an `updated` time, a `scope` (`buildTarget`, or `project` for a setting of the whole project) and `lines[]`, each with a `field`, a `label`, and a `from` and `to` that can be missing or null. The window runs from the `created` of the success just before the failures began to the `checkoutStartTime` of the first failure, or its `created` if that's missing, because a change made while that build waited may still apply. If no build before this one succeeded and the last page you read held fewer than 100 builds, the target never succeeded: start the window 24 hours before its oldest build, because the settings it started with can be the cause. If you stopped at five pages instead, start the window at the oldest build you read, and say the window is cut off. Read every entry from the start of the window to now: the entries are newest first, so while every entry on a page is newer than the window's start and `nextPage` isn't null, read the next page, up to five pages.
   - If the CLI reports `auditlog` as an unknown command, skip this read and note it, because older CLI versions lack it.
   - Leave out `updatingUserEmail` if an entry has one, because it's a user's email address.
   - Environment variable changes aren't recorded, and a signing change shows only that the credentials changed, so an empty window doesn't rule those out.
   - Name a changed setting as the likely trigger only if it relates to the decisive line, such as a new Unity version and a compiler error, or a new subfolder and an unrecognized project. Otherwise list the changes without picking one. A setting that relates to the decisive line wins over item 4's list of changed values, because it explains the line.
   - If a later entry sets the trigger back, say so, because the fix may already be in place.
   - If the same field changed three or more times since the window's start, name the values it moved between, because the user is trying settings to get past the failure, and that shows which input they suspect.
   - If an entry newer than this build's `created` changed a setting, and no build in the listing was created after that entry, say so, because the user may already have tried the fix and no build has tested it yet.
6. **Code changes.** If the revision changed where the failures started, compare the last success's `lastBuiltRevision` (`<good>`) with the first failure's (`<bad>`):
   - From the saved build listing, read `changeset[]` (`commitId`, `message`, `numAffectedFiles`) of each build after `<good>`, up to `<bad>`. It works for every source control, but some builds have none. Leave out authors, because they're user names.
   - If the project's directory is a checkout, read the changed files and their diffs as [references/source-control.md](source-control.md) describes for its source control. Read any file the decisive line names first, then `Packages/manifest.json`, `ProjectSettings/` and editor build scripts.
   - Name a commit as the likely trigger only if it changes what the decisive line points to. Otherwise list the changed areas without picking one.

If no confirmed category decided the side, read the playbook in [references/failure-knowledge.md](failure-knowledge.md) that matches the failing section, and the log signatures to look for.

## Step 4: Decide the side

Take the first rule that applies, because each later rule rests on weaker evidence:

1. If a category's `stageMatch` is `true` and the log around its `logline` confirms it, use that category's row in the knowledge reference. If two confirmed categories disagree, use the one whose `logline` is the decisive line.
2. If the failing section has a playbook in the knowledge reference, follow it.
3. If a log signature in the knowledge reference, or a category's confirming log text, matches the decisive line, use its side. A category counts here even if `builds failures` didn't record it.
4. If the decisive line names a value that a target change in the window set, the side is Your project, because the target's settings are part of it.
5. Otherwise the side is Unclear, because guessing would send the user to the wrong fix.

Then apply these checks:

- Name Build Automation only on positive evidence: a category or signature the knowledge reference assigns to it, matched in the log. Never conclude it because nothing else fits.
- Name a side in the report only if its row in the knowledge reference says the side may be named. Otherwise don't name or hint at one, even if the user asked whose fault it was. Answer with the cause, and with whether the user has anything to change.
- If the cause needs a bigger machine (out of memory, out of disk, or a time limit hit during heavy work), name the issue and the build's machine (`machineType.name`), suggest a machine type with more of the resource that ran out, and link [Choose a machine specification](https://docs.unity.com/en-us/build-automation/optimize-build-speed/choose-machine-specification.md). Don't name a specific paid machine type, because the user chooses what to pay for.

## Step 5: Find what the user can change

Do this for every failure, whatever side you decided, because the knowledge reference covers only the failures seen before, and an Unclear side still leaves the user things to check.

1. Say in plain words what the decisive line means: what the step was doing, what it expected, and what it found instead.
2. List the inputs that step reads and the user controls: the target's settings (run `unity pipeline cloud-build targets get <target> <scope> --format json` and save it), the branch, the revision the build asked for (`requestedRevision`) and the repository, the connected source control account, the signing credentials, and the project's files at the built revision.
3. Keep the inputs that could produce the decisive line. For each, give one check the user can make and the change to make if the check fails. Put first an input that changed where the failures began, in the neighbouring builds or the target changes, or one the user kept changing.
4. If the decisive line says something is missing, such as a type, a file, a framework or a package, check that it exists at the built revision before you blame the code, because a file that's ignored, left out of LFS or only on the user's machine fails the same way. If the project's directory is a checkout, check it as [references/source-control.md](source-control.md) describes; otherwise give it as a check.
5. Call each input a likely cause unless the log names it, and leave out any input the evidence rules out, such as a setting that's unchanged since a passing build.
6. If you have neither a cause nor a check, investigate further before you report, because the steps above cover the common cases only. Read the rest of the log section by section, keeping Step 3's limit past `Player export failed`, then the logs of up to three earlier failures, the project's files and the doc pages that cover the failing section. Stop once you have a cause or a check, or after ten such reads, because some failures stay unexplained however much you read. Use only the commands the Commands section allows. Treat what you find as Step 3's evidence, and decide the side again as in Step 4.

## Step 6: Check the verdict

1. Re-read each line you'll quote, by its line number in the saved log. Confirm it says what you claim, and that it supports the side you decided. If it doesn't, change the side to Unclear.
2. Confirm the failing section you report contains the decisive line.
3. If the evidence and the doc page you read disagree, reread the page from [references/topic-map.md](topic-map.md), and check whether you missed something. If they still disagree, change the side to Unclear.

## Report

[references/example-report.md](example-report.md) shows a finished one.

1. **Verdict**: "Build #`<number>` of `<target>` failed in `<section>` because `<cause>`." Then your confidence in the cause, and the side if the knowledge reference lets you name it. Confidence is high when the log shows the cause and the neighbouring builds or the target changes agree with it, medium when only the log shows the cause, and low otherwise. A category doesn't add confidence, because it matched the same log.
2. **Evidence**: the category display names, if any; one to five quoted log lines with their line numbers; what the neighbouring builds, the target changes and the commits between them show; any read that failed; and the doc pages you read.
3. **Fix**: numbered steps, with a link to the doc page that covers them: the fix for the cause if you found one, then the possible fixes Step 3 found at earlier recoveries, then Step 5's checks, likely causes first.
4. **Next step**:
   - Before you suggest a rebuild, read `enabled` in the `targets get` output you saved in Step 5. If it's `false`, say the target must be enabled first, because a disabled target doesn't build.
   - If Step 3 found repeated failures that the target started automatically, and no later build of the target succeeded, say how many and since when, and suggest turning that trigger off in the Unity Cloud Dashboard until the fix is in: the build schedule (`settings.buildSchedule`), building on new commits (`settings.autoBuild`) or building on a new Unity version (`settings.autoBuildLatest`). For a run of `api` builds, suggest pausing the tool that starts them. If a later build succeeded, say how many failed and when they recovered, and suggest no pause, because the trigger works again.
   - If Step 3 found a setting changed after this build that no build has used yet, suggest a rebuild to test it, with the command below.
   - If the side you decided in Step 4 is Your project or Unity Editor version, whether or not you named it: "After the fix, rebuild with `unity pipeline cloud-build builds start --build-target <target> --cloud-org <org> --cloud-project <project>`." The user runs it. If the fix is in the project's files, add that their agent can apply it if they ask. If it's a target setting, say to change it in the Unity Cloud Dashboard, because this workflow doesn't change targets.
   - If the side is Third party: nothing in the project needs to change. Suggest retrying later and checking that service's status page, then rebuilding with the same command.
   - If the side is Build Automation or Unclear and the Fix has checks, say to make them before the steps below, because Unclear decides who has to act, not whether the user has something to try.
   - If the side is Build Automation or Unclear and later builds of the target succeeded, say the failure hasn't repeated, and give the steps below for if it comes back.
   - If the side is Build Automation or Unclear otherwise, suggest one rebuild first, because many failures don't repeat, unless the same failure already repeated across builds. Then list: check the [Unity status page](https://status.unity.com/) for an open incident; read the [Build Automation FAQ](https://support.unity.com/hc/en-us/sections/13795481434004-Unity-Build-Automation) and the troubleshooting page you read first; ask on [Unity Discussions](https://discussions.unity.com/); and if none of those helps, [open a support ticket](https://support.unity.com/hc/en-us/requests/new?ticket_form_id=186883). Give a hand-off block to paste into the ticket: organization ID and project ID (`orgFk` and `projectGuid`), target, build number, `buildGUID`, the finish time, the category labels and the quoted lines, leaving out any value that's missing or all zeros.
5. **Side effects**: "I only read this build's record, log and build history, and the target's settings and change history. I changed nothing." Add the project's source control history, other builds' logs and the build's artifacts if you read them. Name every temporary file you saved, so the user can delete them.
