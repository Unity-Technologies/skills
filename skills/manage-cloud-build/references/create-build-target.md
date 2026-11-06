# Set up a build target

Relative links in this file resolve to the skill directory's `references/` folder, where this file sits.

Set up a Unity Build Automation build target for the local project and start its first build. Fill in every setting the project shows, and ask the user only for what it doesn't.

## Contents

- Commands
- Glossary
- Step 1: Read the project
- Step 2: Read the source control
- Step 3: Ask what the project doesn't show
- Step 4: Choose the settings
- Step 5: Check before the first build
- Step 6: Show the summary
- Step 7: Create
- Step 8: Start the first build
- Report

## Commands

- **CLI check:** `unity pipeline cloud-build targets create --help`, because older CLI versions can't create targets.
- Keep stderr out of any JSON you parse, because the CLI writes notices there.
- **Reads**, which you may run at any point: `unity auth status`, `unity cloud org current`, `unity cloud project current`, `unity logs --console --level error`, `--help` on any command, `unity vcs status` and `unity vcs doctor` (Git only), `unity vcs setup --dry-run`, and `unity pipeline cloud-build project get`, `connections`, `targets`, `targets get`, `tooling platforms`, `tooling unity`, `tooling os`, `tooling machine-types`, `org free-tier` and `scm check-url`.
- **Writes**, which you may run only after the user approves the summary in Step 6, because each one adds something to their organization or repository: `unity vcs setup`, `connections create`, `credentials <platform> upload`, `targets create` and `builds start`. The one exception is `unity projects add`, which Step 2 runs only when the user agrees to it. Run each one once. If it fails, report its error instead of running it again, because a second run can add a duplicate.
- Never run `targets update`, `builds cancel`, `unity vcs doctor --fix` or any other command that changes something that already exists, because this workflow only adds.
- Read the checkout only with read-only commands, such as those in Step 2, `git ls-files` and those in [references/source-control.md](source-control.md), because the user decides what changes in it. Never fetch, because that changes the checkout too.
- Never ask the user to type a password, token or key into the conversation, and never put one on a command line, because both end up in logs. Step 7 reads each one from a file the user creates.

## Glossary

- **Connection**: the link between a Build Automation project and one repository, with the credentials to read it. A target builds from one connection.
- **Destination**: where the build goes: a store, such as Google Play or the App Store, or only the user's own devices and testers.
- **Free tier**: the builds an organization runs at no cost, on the machine types `tooling machine-types` marks `freeTierEligible`.

## Step 1: Read the project

Read these from the project's directory, the folder that holds `Assets` and `ProjectSettings`. Use the path the user gave, or else the current directory. If that isn't a project, ask the user for the project's path, because every value below comes from it.

| Value | Where |
|---|---|
| Editor version | `m_EditorVersion` in `ProjectSettings/ProjectVersion.txt` |
| Product name | `productName` in `ProjectSettings/ProjectSettings.asset` |
| Bundle ID | `applicationIdentifier` in the same file: its `Android` entry for Android, and its `iPhone` entry for iOS. An empty or missing entry means the project sets none |
| Scenes | the `m_Scenes` entries with `enabled: 1` in `ProjectSettings/EditorBuildSettings.asset` |
| Build profiles | `.asset` files under `Assets/Settings/Build Profiles/` |
| Addressables | `com.unity.addressables` in `Packages/manifest.json`, and an `Assets/AddressableAssetsData` folder. The package alone has no content to build |
| Android build | `scriptingBackend` and `AndroidTargetArchitectures` in `ProjectSettings.asset`. A missing `Android` entry in `scriptingBackend` means Mono, and `1` means IL2CPP. The architectures value is a bit mask in which ARM64 is `2` |
| Local packages | dependencies in `Packages/manifest.json` whose value starts with `file:` |
| Tests | `.asmdef` files whose `references` name `UnityEngine.TestRunner`, or whose `optionalUnityReferences` name `TestAssemblies`. One whose `includePlatforms` is only `Editor` holds Edit Mode tests, and any other holds Play Mode tests |
| Export methods | public static methods in `Assets/**/*.cs` that take a `UnityEngine.CloudBuild.BuildManifestObject` (pre-export), and ones whose name contains `PostExport` and that take a `string` (post-export) |

## Step 2: Read the source control

Below, `<dir>` is the project's directory. Pass it to every command, because the commands otherwise read the current directory.

1. Find the source control. `git -C <dir> rev-parse --is-inside-work-tree` prints `true` in Git and fails outside it, and a `.plastic` folder in `<dir>` or a folder above it means Unity Version Control. If neither, ask the user whether the project is in Perforce, SVN or no source control, because Perforce leaves no folder to look for. If it's in none, go to item 5. If it's in Perforce or SVN, tell the user to set the target up in the Unity Cloud Dashboard, and stop, because the CLI can neither create nor check those connections.
2. **Git:** run `git -C <dir> remote get-url origin`, then read `branch` and `projectPrefix` from `unity vcs status <dir> --format json`. `branch` is the target's branch, and `projectPrefix` without its trailing `/` is its subfolder. If there's no `origin`, tell the user to push the project to a hosted repository, and stop, because the build machines can't reach a local one.
3. **Unity Version Control:** read `.plastic/plastic.selector` in the folder that holds `.plastic`. Its `repository "<repository>@<server>"` line names both, where `<server>` is the last two `@`-separated parts, such as `20066974886447@cloud`. Its `br "/<branch>"` line names the branch: pass it without the leading `/`. Read the file rather than `cm wi`, because `cm wi` can print a display name for the server that a connection doesn't accept. The subfolder is the project's path from the folder that holds `.plastic`.
4. Keep these for Step 5, because the cloud builds only what's on the server:
   - Changes not checked in: `unity vcs status <dir> --format json` for Git, where `clean` is `false` if there are any. For Unity Version Control, save `cm status <dir> --machinereadable` to a file, running it in the background so you can stop it, with the cautions in [references/source-control.md](source-control.md). It needs the user's sign-in, which that reference says how to ask for. Each line starts with a status: `PR` is private, never checked in, `AD` is added but not checked in, and `CH` or `CO` is changed but not checked in. Count each status rather than reading the whole file. A file that holds only its `STATUS` line means nothing is waiting to be checked in.
   - Unity Version Control problems: a line in `ignore.conf`, in the folder that holds `.plastic`, that ignores every `.meta` file, such as `*.meta`, because `unity vcs doctor` reads only Git.
   - Git commits not pushed: `git -C <dir> rev-list --count @{upstream}..HEAD`, which counts against the last fetch. If it fails because the branch has no upstream, run `git -C <dir> rev-parse --verify --quiet refs/remotes/origin/<branch>`. If that fails too, the branch isn't on the server.
   - Git repository problems: the `data.findings[]` of `unity vcs doctor <dir> --format json`, each with its `code` and `message`. The command reports `success: false` whenever it finds anything, so go by the codes.
5. **No source control:** run `unity vcs setup <dir> --vcs uvcs --dry-run --cloud-org <org> --cloud-project <project> --format json`, and keep what it says it would create, and its `billable` field. It signs in with the user's Unity ID.
   - If it fails with `PROJECT_NOT_REGISTERED`, ask the user whether to add the project to Unity Hub's project list with `unity projects add <dir>`, because the setup works only on a project the Hub knows. If they agree, run it and the dry run again.
   - If it fails with `UVCS_REGION_REQUIRED`, ask the user to pick one of `data.availableRegions`, and pass it as `--vcs-region` to the dry run and to the setup in Step 7, because the organization's first repository needs a region.
   - If it fails otherwise, report its error, and stop, because the target has nothing to build from.
6. Run `unity pipeline cloud-build connections <scope> --format json`. A connection matches when its `url` names the same host and repository, ignoring the protocol and a trailing `.git`. A `plastic` connection's `url` names only a server, such as `20066974886447@cloud`, so one on the project's server matches every repository there, the one item 5's setup creates included. The CLI prints a URL that holds a user name, such as `git@…` or `https://user@…`, as `***`. If any connection's `url` is `***`, list the connections by `name` and `type` and ask the user which one, if any, is this repository.
7. **Git with no matching connection:** ask the user to connect the repository in the Unity Cloud Dashboard, linking the "Connect your version control system" page from the [topic map](topic-map.md), and wait until they say it's done. Then run item 6 again. The CLI can't give the build machines access to a Git repository: the service checks access with the organization's SSH key before it creates a connection, and the CLI can't show that key.

## Step 3: Ask what the project doesn't show

Ask everything below that the user hasn't already said, in one message, because each answer changes the settings:

1. The platform: iOS, Android, WebGL, Windows, or another that `unity pipeline cloud-build tooling platforms <scope>` lists.
2. The destination.
3. When to build: only when the user starts a build, after every push to the branch, or on a schedule, and which one.
4. If Step 1 found build profiles: which one the target builds with, or none.
5. If Step 1 found more than one pre-export or post-export method: which one the target runs, or none.
6. For Android or iOS, if the project sets no bundle ID for the platform: the app's bundle ID.
7. For an iOS build, or an Android build going to a store: the path to the signing files and the key alias. That's a keystore for Android, and a `.p12` certificate and a `.mobileprovision` profile for iOS. If the user has none, link the signing page from the [topic map](topic-map.md), and stop, because the build can't be signed without them.

## Step 4: Choose the settings

Look each value up with the command named here rather than writing it from memory, because the service accepts only the values its tooling lists. Every option goes on `targets create`:

| Option | Value |
|---|---|
| `--name` | the platform's short name and the branch, such as `android-main` or `windows-main`. If `targets --limit 100` already lists that name, add `-2` |
| `--platform` | `ios`, `android`, `webgl` or `standalonewindows64` for Windows. For another platform, the `platform` value `tooling platforms` lists |
| `--editor-version` | the Editor version with `_` for `.`, such as `6000_3_27f1` for `6000.3.27f1`, as `tooling unity` lists it |
| `--auto-detect-editor-version` | for a `plastic` or `oauth` connection, so the target follows the project's version when it's upgraded. For a `git` connection, pass `--no-auto-detect-editor-version` instead, because the service can't read the project's files over plain Git, so every build would fail |
| `--branch`, `--subdirectory`, `--repo` | from Step 2, or for a project with no source control from `.plastic/plastic.selector` after Step 7's setup, because the repository it creates can be named differently from the dry run's. Leave `--subdirectory` out when the project is at the repository's root, and pass `--repo` only for Unity Version Control |
| `--connection` | the matching connection's `id`, or the one Step 7 creates |
| `--executable-name` | the product name |
| `--bundle-id` | for Android and iOS, the project's bundle ID or the user's answer |
| `--build-profile-path` | the user's answer, if they chose one |
| `--builder-os`, `--machine-type`, `--builder-os-version` | from the builder steps below |
| `--remote-cache-strategy` | `library`, because a workspace cache is 2.24 times bigger and slower |
| `--artifact-compression-level` | `6`, for WebGL only |
| `--no-development-build` | unless the user asked for a development build, because on iOS one took about 25% longer and was about 44% bigger |
| `--app-bundle` | for Android going to Google Play, because the store takes App Bundles. For other Android builds, `--no-app-bundle`, because an APK installs straight onto a device |
| `--build-addressables` | if the project uses Addressables |
| `--run-unit-tests`, `--run-edit-mode-tests`, `--run-play-mode-tests` | if Step 1 found tests, with the mode flags for the kinds it found |
| `--pre-export-method`, `--post-export-method` | the method Step 1 found, or the user's choice, as `Namespace.Class.Method` |
| `--auto-build`, or `--schedule` with `--schedule-start` | the user's answer, adding `--schedule-cron` for a `cron` schedule. Pass neither when the user starts builds themselves |
| `--credential` | the ID Step 7 uploads. Pass none for an Android build that isn't going to a store, because the service then signs it with the organization's auto-generated debug keystore |

Leave out `--scene`, so the build uses the project's own scene list, and `--xcode-version`, so the service picks the default for the macOS version.

Choose the builder:

1. Take the operating system and machine size for the platform from this table, for the reason in its last column. For another platform, use the first `operatingSystems[].family` that `tooling platforms --platform <platform> <scope> --format json` lists, and no size.

   | Platform | `--builder-os` | Size | Why |
   |---|---|---|---|
   | iOS | `mac` | `standard` | iOS builds need a Mac |
   | Android | `mac` | `standard` | Windows took about 80% longer in 31 of 31 comparisons |
   | WebGL | `mac` | `standard` | Mac builds were generally faster |
   | Windows | `windows` | `micro` | Windows players built with IL2CPP need Windows |

2. Run `unity pipeline cloud-build tooling os --family <os> --editor-version <version> <scope> --format json`, and pass the `value` of the entry marked `default` as `--builder-os-version`. If `tooling unity` doesn't list the project's Editor version, use the newest listed one with the same major and minor numbers, because the lookup fails for an unlisted version.
3. Run `unity pipeline cloud-build tooling machine-types --operating-system <os> <scope> --format json`. Keep the `data.items[]` entries whose `enabled` is `true` and whose `operatingSystemVersions` include the builder OS version, because a machine type runs only on the versions it lists. Skip an entry whose list is empty, because it names no version.
4. If any kept entry has `freeTierEligible` `true`, pass as `--machine-type` the `label` of the free-tier entry whose `shortName` is the table's size, ignoring case, or else of the free-tier entry marked `default`, because the user starts in the free tier.
5. If no kept entry is free-tier eligible, the service marks none, so pass the `label` of the entry of the table's size, or the one marked `default`. Say in the summary that the CLI doesn't show whether it's in the free tier. Name a bigger machine only as an upgrade the user can choose, because the user decides what to pay for.

## Step 5: Check before the first build

Run every check. A blocker stops the build from working, and a warning only changes what it contains. For a project with no source control, checks 3 and 5 can't run until Step 7's setup has checked it in, so run them then, and say so in the summary.

1. **Editor version:** if `tooling unity` doesn't list the project's version, or marks it `deprecated`, that's a blocker, because the build machines can't run it. Suggest the newest listed version with the same major and minor numbers. An alpha or beta version, with an `a` or `b` before its last number, is a warning, because pre-release Editors change between builds.
2. **Platform:** if `tooling platforms --platform <platform> --editor-version <version>` returns no entry, or one whose `operatingSystems` is empty, that's a blocker, because that version can't build the platform in the cloud.
3. **Source control:** a branch that isn't on the server is a blocker. So is the `vcs doctor` finding `META_FILES_IGNORED`, or a `*.meta` line in Unity Version Control's `ignore.conf`, because the checkout then lacks the `.meta` files that hold asset references. Changes not checked in and commits not pushed are warnings, because the build won't contain them. Every other `vcs doctor` finding is a warning, such as `GENERATED_DIR_TRACKED`, which slows every checkout.
4. **Local packages:** a `file:` package whose path leads outside the repository is a blocker, because the build machine won't have it.
5. **Project on the server:** if `Assets`, `ProjectSettings` or `ProjectSettings/ProjectVersion.txt` isn't checked in, that's a blocker, because the cloud checkout isn't a Unity project. With no build profile, if no enabled scene in `EditorBuildSettings.asset` is checked in, that's a blocker too, because the player has nothing to load. Git shows what's checked in with `git -C <dir> ls-files`, and Unity Version Control with the `PR` and `AD` lines Step 2 saved. The fix is for the user to check those files in, because this workflow never changes the checkout.
6. **Editor-only code:** a `.cs` file in a player assembly that has `using UnityEditor` outside `#if UNITY_EDITOR` is a blocker, because players compile without the `UnityEditor` assembly. A file is Editor-only when the nearest `.asmdef` above it has `includePlatforms` of only `Editor`, or when no `.asmdef` covers it and it sits in an `Editor` folder.
7. **Bundle ID:** for a store build on Android or iOS, a bundle ID that starts with `com.DefaultCompany` or `com.Company` is a blocker, because the store needs the app's own ID.
8. **Google Play:** for Android going to Google Play, if the project builds with Mono or without ARM64, that's a blocker, because the store accepts only 64-bit apps, and Mono can't build them. The fix is in the Android Player settings page of the [topic map](topic-map.md).
9. **Signing files:** a signing file the user named that doesn't exist is a blocker, because the upload would fail after approval. For iOS, read the profile's ID with `grep -a -A1 '<key>application-identifier</key>' '<profile>'`: it prints `<team ID>.<bundle ID>`. If that bundle ID differs from the target's, and isn't a `*` wildcard that covers it, that's a blocker, because the signed app wouldn't install.
10. **Free tier:** run `unity pipeline cloud-build org free-tier <scope> --format json`. If `freeTierLimitReached` is `true`, that's a warning, because the organization has used its free builds, so this one may be billed or refused.

## Step 6: Show the summary

Show one summary before you create anything, and wait for the user to approve it, because each write adds something to their organization. It holds:

- The settings, as a table of option, value and source: the project, the user, or a default with its reason.
- The source control: the connection to reuse, the one to create, or what the Unity Version Control dry run would create. A new Unity Version Control connection reads the repository with the user's own Unity sign-in, so say so. If the dry run's `billable` is `true`, say that the setup starts a paid Unity Version Control subscription, because the user decides what to pay for.
- The signing files to upload.
- The builder, and whether the CLI shows it in the free tier.
- Each blocker and warning, with its fix.
- The writes Step 7 runs, in order, and whether Step 8 starts the first build. It does unless a blocker remains or the user said not to, because a target that never built hasn't shown it works.

If a blocker remains, offer to create the target without starting a build. If the user changes anything, show the summary again. For example:

```
Target android-main: Android, Google Play
  --editor-version 6000_3_27f1                   project; no auto-detect over plain Git
  --branch main (connection runner-repo)         project
  --bundle-id com.example.runner                 project
  --machine-type orka_standard_v1                default: fastest for Android
  --remote-cache-strategy library                default: smallest cache
  --app-bundle                                   Google Play takes App Bundles
Warning: 2 commits on main aren't pushed, so this build won't contain them.
Then: upload the keystore, create the target, start build 1.
```

## Step 7: Create

1. Save `connections` and `targets --limit 100` as JSON to the temporary directory. Create a restore point you can roll back to if your changes fail: if a write fails, compare these with new listings, and tell the user exactly what this run added, so they can remove it in the Unity Cloud Dashboard.
2. **Source control:**
   - No source control: run `unity vcs setup <dir> --vcs uvcs --cloud-org <org> --cloud-project <project> --non-interactive --format json`, adding `--vcs-region` if Step 2 needed it, which creates the repository and checks the project in. Read the new repository, server and branch from `.plastic/plastic.selector` as Step 2 does, then run Step 5's checks 3 and 5.
   - Unity Version Control, only when Step 2 matched no connection: run `unity pipeline cloud-build connections create --name <repository> --type plastic --url <server> <scope> --format json`, and keep the new connection's `data.id`.
3. Run `unity pipeline cloud-build scm check-url --type <type> --url <url> --connection <id> <scope> --format json`, with the connection's `type`, and the URL you created it with or the local remote's URL, because the listing can mask it. If it fails, report its error and stop, because the build would fail at checkout.
4. **Signing files**, only when Step 3 asked for them. Ask the user to save each password in its own file outside the project and give you the paths, because then the password never enters the conversation. Pass those files to the CLI only as the commands below do, and never print or copy them.
   - Android: `UNITY_KEY_PASSWORD="$(cat '<key-password-file>')" unity pipeline cloud-build credentials android upload --label <label> --keystore '<keystore>' --alias <alias> --keystore-password-from-stdin <scope> --format json < '<keystore-password-file>'`. On Windows PowerShell, set ``$env:UNITY_KEY_PASSWORD = (Get-Content -Raw '<key-password-file>').TrimEnd("`r", "`n")`` first, because the CLI uses the variable as it is, newline included. Pipe `Get-Content -Raw '<keystore-password-file>'` into the command instead of `<`, then run `Remove-Item Env:UNITY_KEY_PASSWORD`.
   - iOS: `unity pipeline cloud-build credentials ios upload --label <label> --certificate '<p12>' --provisioning-profile '<profile>' --certificate-password-from-stdin <scope> --format json < '<certificate-password-file>'`. Then compare `data.provisioningProfile.bundleId` with the bundle ID. If they differ, and the profile's ID isn't a `*` wildcard that covers it, stop, because the signed app wouldn't install.
   - Keep `data.credentialid`, and tell the user to delete the password files.
5. Run `unity pipeline cloud-build targets create <options> <scope> --format json`, and keep `data.buildtargetid`. If it fails, report its `errors[].message`, which carries the service's reason, and stop.
6. Run `unity pipeline cloud-build targets get <buildtargetid> <scope> --format json`, and compare each setting in the summary with the one stored. Tell the user about any that differ. `targets get` shows neither the development-build setting nor the signing credential, so those can't be compared, and settings the service adds on its own, such as `xcodeVersion`, aren't differences.

## Step 8: Start the first build

Skip this step if a blocker remains or the user said not to start a build. A user who starts builds themselves, rather than on every push or a schedule, still gets this first one, because it shows the target works.

1. Run `unity pipeline cloud-build builds start --build-target <buildtargetid> <scope> --format json`. `data` is an array: if `data[0]` has an `error`, report it, because the service refused the build. Otherwise keep `data[0].build`.
2. Run `unity pipeline cloud-build builds get <build> --build-target <buildtargetid> <scope> --format json`, and confirm that `buildStatus` is `created`, `queued`, `sentToBuilder` or `started`.
3. About a minute later, run `builds get` once more, because the service can refuse a build at validation within seconds. If `buildStatus` is `failure`, analyze the build with [references/analyze-build-failure.md](analyze-build-failure.md). Otherwise don't wait for the build to finish, because a first build has nothing cached and runs longest.

## Report

1. **Result**: "Created `<target>` for `<platform>` and started build #`<build>`." If no build started, "Created `<target>` for `<platform>`. No build started, because `<reason>`." Otherwise, what you created and what stopped you.
2. **Settings**: if you showed a summary, its table, marking any setting that `targets get` showed differently.
3. **Next step**: if a build started, the `builds get` command to follow it, and an offer to analyze it with [references/analyze-build-failure.md](analyze-build-failure.md) if it fails. Then the fix for each remaining blocker or warning, then, if no build started, the `builds start` command to run once they're fixed, and any upgrade the user can choose.
4. **Side effects**: everything this run added, such as a repository, a connection, signing credentials, a target and a build, and every temporary file you saved, so the user can remove them.
