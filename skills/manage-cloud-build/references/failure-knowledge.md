# Failure knowledge

Relative paths in this file resolve to the skill directory. Doc pages without a host are under `https://docs.unity.com/en-us/build-automation/`.

## Contents

- How to read this file
- Categories
- Playbooks by failing section
- Builds the service canceled
- Log signatures

## How to read this file

- **Side** is the side to report once the log confirms the evidence. "From the log" means the row lists the lines that decide it.
- **Name side** says whether the report may name that side. If it says no, report the cause, the fix and the next steps, but no side.
- **Confirm** is what to find in the saved log before you trust the category, at or near its `logline`.
- **Read** is the doc page to read before you decide and to link in the fix.
- The `builds failures` output carries each category's own `publicMessage`. Use it as a hint, and prefer the evidence in the log when they differ.

## Categories

| Label | Side | Name side | Confirm | Read |
|---|---|---|---|---|
| `android_sdk_missing` | Build Automation | no | `Android SDK not found` | `check-build-results/troubleshoot-build-failures/android-build-failures.md` |
| `error_resolving_packages` | From the log: an authentication error, a 401 or 403, or a package or version that doesn't exist means Your project; a timeout or 5xx from a registry outside Unity means Third party | no | `An error occurred while resolving packages`, and the package and registry named after it | Unity Manual `upm-errors.html` |
| `invalid_ios_dependency` | Your project | no | `ld: symbol(s) not found for architecture arm64`, and the library named above it | `check-build-results/troubleshoot-build-failures/error-migrating-xcode.md` |
| `sdk_directory_not_writable` | Your project: a plugin asks for Android build tools the target doesn't have | no | `The SDK directory is not writable`, and the build-tools version requested above it | `check-build-results/troubleshoot-build-failures/android-build-failures.md` |
| `unrecognized_project` | Your project | no | `Error: unrecognized project!` | `check-build-results/troubleshoot-build-failures/unrecognized-project-error.md` |
| `fastlane_xcode_deprecated_version` | From the log: a deployment target below the range the Xcode version supports means Your project; otherwise Unity Editor version | no | `but the range of supported deployment target versions` | `check-build-results/troubleshoot-build-failures/error-migrating-xcode.md` |
| `missing_asset_store_entitlement` | Your project: the organization doesn't own an Asset Store package the project uses | no | `Your account does not grant permission to use the package`, and the package name | Unity Manual `upm-errors.html` |
| `missing_using_or_reference` | As `script_compiler_error`. The `logline` often points at an early compile pass, so decide from the last one | no | `error CS0246` with the file and line, repeated in the last compile pass | `check-build-results/troubleshoot-build-failures/overview.md` |
| `ucb_unlicensed_runner_encountered` | Build Automation | no | `Failed to acquire Unity licenses` or `Unlicensed runner encountered`, in `+ Acquiring Unity License` | `check-build-results/troubleshoot-build-failures/overview.md` |
| `unity_compilation_syntax_error` | Your project | no | `error CS1002: ; expected` with the file and line | `check-build-results/troubleshoot-build-failures/overview.md` |
| `unity_out_of_memory_exception` | Apply the skill's machine-size rule | no | `Could not allocate memory: System out of memory!` | `optimize-build-speed/choose-machine-specification.md` |
| `unity_player_export_failure` | None by itself: it's the summary line Unity prints after another error. Decide from the `Player export failed. Reason:` line or the `error CS` lines above it | no | `Unity player export failed`, then the `Player export failed. Reason:` line or the `error CS` lines above it | `check-build-results/troubleshoot-build-failures/overview.md` |
| `bvr_checkout_failure` | From the log: see the checkout playbook | no | `! checkout failed`, and the source control error above it | `get-started-with-build-automation/connect-your-version-control-system.md` |
| `uvcs_checkout_workspace_error` | From the log: in `+ Checking out Source Code`, Your project; in `+ Publishing`, Build Automation, because the upload of the build's output failed | no | `Failed to create new workspace`, the error above it, and which section it's in | `get-started-with-build-automation/connect-your-version-control-system.md` |
| `apple_server_502_bad_gateway` | Third party: Apple's servers. Retrying later can help | no | `Status Code: 502 (bad gateway` | `check-build-results/troubleshoot-build-failures/osx-notarization-failure.md` |
| `CocoaPodsDeploymentTargetError` | Your project. The CocoaPods error above the line names the cause. `required a higher minimum deployment target` means a pod needs a higher minimum iOS version than the app's **Target minimum iOS Version** Player setting: raise that setting to the pod's minimum, or use a pod version that supports the current one. An error that names an Xcode or Swift version means the target needs a newer Xcode | no | `CocoaPods installation failure`, and the CocoaPods error and pod named above it | Unity Manual `class-PlayerSettingsiOS.html` for the deployment target, `reference/available-xcode-versions.md` for Xcode |
| `script_compiler_error` | Your project. A path under `Assets/` is the project's code. A path under `Library/PackageCache/<package>@` that's still failing in the last compile pass is a package version that doesn't match the Editor version | no | `Scripts have compiler errors`, then the `error CS` lines just above it | `check-build-results/troubleshoot-build-failures/overview.md` |
| `unity_cpu_lightmapper` | Unity Editor version | no | `Unable to fall back to CPU lightmapper` | `check-build-results/troubleshoot-build-failures/lightmap-baking.md` |
| `microphone_description_empty` | Your project: Player settings | no | `Microphone Usage Description is empty` | The category's `publicMessage` |
| `ucb_builder_command_execution` | Build Automation, unless the line comes from the project's own pre-build or post-build script | no | `command execution failed`, and which section it's in | `check-build-results/troubleshoot-build-failures/overview.md` |
| `ucb_unexpected_termination` | Build Automation | no | `Unexpected termination of the channel` | `check-build-results/troubleshoot-build-failures/overview.md` |

## Playbooks by failing section

When no confirmed category decided the side, find the failing section (Step 3 of the analysis workflow) and use its playbook. Every row here also has name side: no.

### The build never reached a build machine

The log is one `[error] Unity Build Automation encountered an error while validating your build` line, or there's no log and no `checkoutStartTime`.

1. If the line says the configured source control is unreachable or access has expired, the side is Your project: the user reconnects or reauthorizes source control. Read `get-started-with-build-automation/connect-your-version-control-system.md`.
2. If the line names a plan, a usage limit or a build minute cap, the side is Your project. Read `check-build-results/troubleshoot-build-failures/builds-failing-immediately.md`.
3. If there's no log at all, the side is Unclear. Read `check-build-results/troubleshoot-build-failures/builds-failing-immediately.md`.

### Before checkout: `+ Acquiring Unity License`, `+ Configuring Build Attempt`

1. If `Unity is not available at expected path` or `mount_nfs: can't mount ... Operation timed out` appears, and an earlier or later build of the target succeeded on the same `unityVersion`, the side is Build Automation: the machine couldn't load the Editor. If no build on that version ever succeeded, the side is Unclear; check the version on `reference/supported-unity-versions.md`.
2. If a license line appears (`Failed to acquire Unity licenses`), the side is Build Automation.

### Checkout: `+ Checking out Source Code`, `+ Updating Post-Checkout Status`

1. `Permission denied (publickey)`, `Authentication failed`, a 401 or 403, or `repository not found`: Your project. The target's credentials or repository URL are wrong or expired. Read `get-started-with-build-automation/connect-your-version-control-system.md`.
2. A submodule clone that stops without an error: Your project. Read `check-build-results/troubleshoot-build-failures/git-clone-hangs.md`.
3. `smudge filter lfs failed`: Unclear. Check the files exist in the remote's LFS storage.
4. `we cannot build this project with your current billing plan`: Your project, a plan limit. Read `check-build-results/troubleshoot-build-failures/builds-failing-immediately.md`.

### The project's own scripts

Lines between the pre-build or post-build script's start and its exit code come from the project. The side is Your project. Read `advanced-build-configuration/run-custom-scripts-during-the-build-process.md`.

`export method '<name>' not found` means the target names a pre-export or post-export method that the project doesn't have, or one with the wrong signature. `preExportMethod '<name>' failed` follows it, or follows an exception the method threw, logged just above. Either way the side is Your project. The fix is the method's name in the target's settings, the method's signature, or the exception's cause, and the target's change history often shows which one changed.

### Signing: `installing iOS credentials failed`, certificate or profile errors

1. `Certificate is expired` or `Provisioning profile is expired`: Your project. Read `sign-build-artifacts.md`.
2. A 403 while downloading credentials: Unclear. Ask the user to check the target's signing credentials still exist and belong to this project, then follow the Build Automation next steps.
3. If `credentialsOutdated` is `true`, the target's signing credentials need replacing: Your project.
4. `Keystore was tampered with, or password was incorrect`: Your project. The target's Android keystore or its password is wrong. Read `sign-build-artifacts.md`.

### Unity build: `+ Building Unity player (<platform>)`

1. `error CS` lines: as `script_compiler_error` in the categories table.
2. `Player export failed. Reason:` names the cause. For Android, a reason about updating or finding the SDK, NDK or JDK is Unclear. Find the tool version the log says the Editor needs, compare it with the Manual's supported dependency versions for the build's Unity version, and say which Unity version change, if any, the neighbouring builds show. Read `check-build-results/troubleshoot-build-failures/android-build-failures.md`.
3. `Abort trap` or another Editor crash with no earlier error: Unclear. Suggest a clean build, then the Build Automation next steps.

### Xcode build

`** ARCHIVE FAILED **` is a summary line. Find the first `error:` line above it, then apply the signing playbook or the `invalid_ios_dependency` row. `Platform Not Installed` is Build Automation.

### Unit tests

Failed tests are Your project. Read `reference/unit-tests.md`.

### Publishing: `+ Publishing`

This section runs after every build, including failed ones. It's the failing section only when it holds the first decisive `[error]` line. An upload error there, such as `Connection refused`, is Build Automation.

## Builds the service canceled

`canceledBy` on the build says why the service stopped it. Every row here has name side: no.

| `canceledBy` | Meaning | Side | Next |
|---|---|---|---|
| `service-timelimit` | The build ran past the target's build time limit | Apply the skill's machine-size rule if the log's last section was still working, such as compiling, importing or baking; otherwise Unclear | Read the log's last section |
| `job-timelimit` | The log printed nothing for too long | From the log: a wait in the project's own script or a dialog the Editor opened is Your project; otherwise Unclear | Read the last 30 lines of the log |
| `jenkins-timelimit`, `restart-limit` | The build couldn't start or stay on a build machine | Build Automation | Rebuild once, then the Build Automation next steps |
| `evaluation-timelimit`, `concurrency-timelimit` | The build waited too long for a free build slot | Unclear | Check the organization's concurrent build limit, then rebuild. Read `check-build-results/troubleshoot-build-failures/builds-failing-immediately.md` |
| `billing-invalidsubscription` | The organization's subscription isn't valid | Your project | Read `check-build-results/troubleshoot-build-failures/builds-failing-immediately.md` |
| `service-badconfiguration` | The build's configuration couldn't be used | Your project | Check the target's settings. Read `check-build-results/troubleshoot-build-failures/builds-failing-immediately.md` |

## Log signatures

When a line matches and no category or playbook decided the side:

| Signature | Side | Name side | Fix |
|---|---|---|---|
| `No space left on device` or `IOException: Disk full`, including beside a `tar: Write error` | Apply the skill's machine-size rule | no | A machine type with a larger disk |
| `nodename nor servname provided` for a Unity host | Build Automation | no | Rebuild once, then the Build Automation next steps |
| `nodename nor servname provided` for the project's own server | Your project | no | Fix the server's host name in the target's settings |
| `Bad runner encountered` | Build Automation | no | Rebuild once, then the Build Automation next steps |
| `Platform Not Installed` in the Xcode build | Build Automation | no | Rebuild once, then the Build Automation next steps |
| `Connection refused` in `+ Publishing` | Build Automation | no | Rebuild once, then the Build Automation next steps |
