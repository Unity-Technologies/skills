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
- Step 5: Check the verdict
- Report

## Commands

- **CLI check:** `unity pipeline cloud-build builds failures --help`, because older CLI versions can't read failures, steps or logs.
- Run only these `unity` commands: `unity auth status`, `unity cloud org current`, `unity cloud project current`, `unity logs --console --level error`, `--help` on any command, and `unity pipeline cloud-build builds`, `builds get`, `failures`, `steps` and `log`, `targets get` and `targets auditlog`. Never run `builds start`, `builds cancel` or any other command that changes something, because the user decides what to change and when to rebuild.
- Run only the read-only source control commands in [references/source-control.md](source-control.md), never one that changes the checkout, because the user decides what changes in it.
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
4. Run `unity pipeline cloud-build builds get <number> --build-target <target> <scope> --format json`, and pass its `buildtargetid` as `--build-target` on every later read, because the same value works for all of them.
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

Keep these fields from the same output for later steps: `unityVersion`, `xcodeVersion`, `androidSdkVersion`, `machineType.name`, `operatingSystemVersion`, `lastBuiltRevision`, `scmBranch`, `cleanBuild`, `canceledBy`, `credentialsOutdated`, `orgFk`, `projectGuid`, `deletedLogs`, `checkoutStartTime`, `created`, `finished` and `buildGUID`. Some can be missing; leave a missing field out rather than guess it.

## Step 3: Read the evidence

Read the sources in this order, because each is cheaper than the next. Run each read as its own command, and keep stderr out of any JSON you parse, because the CLI writes notices there. If a read fails, note it for the report and go on.

1. **Failure categories.** Run `unity pipeline cloud-build builds failures <number> --build-target <target> <scope> --format json`. Each entry in `data.failures[]` has a `failureIndicatorLabel`, a `displayName`, a `stage`, a `publicMessage`, an optional `link`, the `logline` where the pattern matched, and `stageMatch`.
   - An empty list is normal.
   - If `stageMatch` is `false`, the pattern matched outside the stage it was written for, so weigh that category below what the log shows.
   - Look each `failureIndicatorLabel` up in [references/failure-knowledge.md](failure-knowledge.md).
2. **Steps.** Run `unity pipeline cloud-build builds steps <number> --build-target <target> <scope> --format json`. `data` is an array of the build's steps in order, with `name`, `duration` in milliseconds and `messages[]`. Read any messages on the last steps. Steps are often missing.
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
   - Find the decisive line by working back from the summary lines (`Unity player export failed`, `Scripts have compiler errors`, `Finished: FAILURE`) to the error that explains them, such as a `Player export failed. Reason:` line, an exception, or an `error CS` line.
   - Unity compiles more than once. Decide from the `error CS` lines just above `Scripts have compiler errors`, because errors only in an earlier pass, such as `[Script Updater]` lines, often clear before the end.
   - The failing section is the last section header above the decisive line. Don't take the last section in the log, because `+ Publishing` runs even after a failed build.
   - Read no more than ten lines past a `Player export failed` line, and keep keyword searches above it, because the build machine's environment follows it. This limit wins over the 30-line window.
   - If `deletedLogs` is `true` or the read returns not found, there's no log. Decide from the other evidence.
   - A one-line log that starts with `[error] Unity Build Automation encountered an error while validating your build` means the build never reached a build machine. The rest of the line is the reason.
4. **Neighbouring builds.** Run `unity pipeline cloud-build builds --build-target <target> --limit 100 <scope> --format json`, save it, and read only `build`, `buildStatus`, `cleanBuild`, `lastBuiltRevision`, `scmBranch`, `unityVersion`, `androidSdkVersion`, `xcodeVersion`, `machineType.name` and `operatingSystemVersion` from `data.items[]`. Read `--page 2` and on until you have the build before this one, a page holds fewer than 100 builds, or you've read five pages. Report any neighbour you don't have, such as a later build of the newest build, and decide from the rest.
   - A later clean build (`cleanBuild` `true`) that succeeded at the same revision after non-clean failures points at the build's cached files: suggest a clean build.
   - A later success at the same `lastBuiltRevision`, with the same other values, means the cause was probably temporary. A later success only at other revisions shows the failure hasn't repeated, not that it was temporary, because a commit may have fixed it.
   - If the failures started where exactly one of those values changed, name it as the likely trigger. If several changed together, list them without picking one.
5. **Target changes.** If the neighbouring builds include a success before the failures, run `unity pipeline cloud-build targets auditlog <target> --limit 100 <scope> --format json` and save it. Each entry in `data.items[]` has an `updated` time, a `scope` (`buildTarget`, or `project` for a setting of the whole project) and `lines[]`, each with a `field`, a `label`, and a `from` and `to` that can be missing or null. The window runs from the `created` of the success just before the failures began to the `checkoutStartTime` of the first failure, or its `created` if that's missing, because a change made while that build waited may still apply. Read the entries in the window. While every entry on a page is newer than the window and `nextPage` isn't null, read the next page, up to five pages.
   - If the CLI reports `auditlog` as an unknown command, skip this read and note it, because older CLI versions lack it.
   - Leave out `updatingUserEmail` if an entry has one, because it's a user's email address.
   - Environment variable changes aren't recorded, and a signing change shows only that the credentials changed, so an empty window doesn't rule those out.
   - Name a changed setting as the likely trigger only if it relates to the decisive line, such as a new Unity version and a compiler error, or a new subfolder and an unrecognized project. Otherwise list the changes without picking one. A setting that relates to the decisive line wins over item 4's list of changed values, because it explains the line.
   - If a later entry sets the trigger back, say so, because the fix may already be in place.
6. **Code changes.** If the revision changed where the failures started, compare the last success's `lastBuiltRevision` (`<good>`) with the first failure's (`<bad>`):
   - From the saved build listing, read `changeset[]` (`commitId`, `message`, `numAffectedFiles`) of each build after `<good>`, up to `<bad>`. It works for every source control, but some builds have none. Leave out authors, because they're user names.
   - If the project's directory is a checkout, read the changed files and their diffs with the commands for its source control in [references/source-control.md](source-control.md). Read any file the decisive line names first, then `Packages/manifest.json`, `ProjectSettings/` and editor build scripts.
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

## Step 5: Check the verdict

1. Re-read each line you'll quote, by its line number in the saved log. Confirm it says what you claim, and that it supports the side you decided. If it doesn't, change the side to Unclear.
2. Confirm the failing section you report contains the decisive line.
3. If the evidence and the doc page you read disagree, reread the page from [references/topic-map.md](topic-map.md), and check whether you missed something. If they still disagree, change the side to Unclear.

## Report

[references/example-report.md](example-report.md) shows a finished one.

1. **Verdict**: "Build #`<number>` of `<target>` failed in `<section>` because `<cause>`." Then your confidence in the cause, and the side if the knowledge reference lets you name it. Confidence is high when the log shows the cause and the neighbouring builds or the target changes agree with it, medium when only the log shows the cause, and low otherwise. A category doesn't add confidence, because it matched the same log.
2. **Evidence**: the category display names, if any; one to five quoted log lines with their line numbers; what the neighbouring builds, the target changes and the commits between them show; any read that failed; and the doc pages you read.
3. **Fix**: numbered steps, with a link to the doc page that covers them.
4. **Next step**:
   - Before you suggest a rebuild, run `unity pipeline cloud-build targets get <target> <scope> --format json`. If `enabled` is `false`, say the target must be enabled first, because a disabled target doesn't build.
   - If the side you decided in Step 4 is Your project or Unity Editor version, whether or not you named it: "After the fix, rebuild with `unity pipeline cloud-build builds start --build-target <target> --cloud-org <org> --cloud-project <project>`." The user runs it. If the fix is in the project's files, add that their agent can apply it if they ask. If it's a target setting, say to change it in the Unity Cloud Dashboard, because this skill doesn't change targets.
   - If the side is Third party: nothing in the project needs to change. Suggest retrying later and checking that service's status page, then rebuilding with the same command.
   - If the side is Build Automation or Unclear and later builds of the target succeeded, say the failure hasn't repeated, and give the steps below for if it comes back.
   - If the side is Build Automation or Unclear otherwise, suggest one rebuild first, because many failures don't repeat, unless the same failure already repeated across builds. Then list: check the [Unity status page](https://status.unity.com/) for an open incident; read the [Build Automation FAQ](https://support.unity.com/hc/en-us/sections/13795481434004-Unity-Build-Automation) and the troubleshooting page you read first; ask on [Unity Discussions](https://discussions.unity.com/); and if none of those helps, [open a support ticket](https://support.unity.com/hc/en-us/requests/new?ticket_form_id=186883). Give a hand-off block to paste into the ticket: organization ID and project ID (`orgFk` and `projectGuid`), target, build number, `buildGUID`, the finish time, the category labels and the quoted lines, leaving out any value that's missing or all zeros.
5. **Side effects**: "I only read this build's record, log and build history, and the target's change history. I changed nothing." Add the project's source control history if you read it. Name every temporary file you saved, so the user can delete them.
