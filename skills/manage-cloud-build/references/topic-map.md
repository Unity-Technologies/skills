# Topic map

The documentation pages for each workflow. Paths in this file are doc pages, relative to the base URL above them, not files in the skill directory. Replace `<VERSION>` as the skill's topic map says.

The Build Automation pages are at `https://docs.unity.com/en-us/build-automation/`:

- **Troubleshooting** — `check-build-results/troubleshoot-build-failures.md`: index of the failure pages, which are in `check-build-results/troubleshoot-build-failures/`:
  - Troubleshoot common issues — `overview.md`: reading logs, clean builds, batch mode locally.
  - Builds fail immediately — `builds-failing-immediately.md`: plan limits, minute caps, invalid configuration.
  - Unrecognized project error — `unrecognized-project-error.md`: project subdirectory setting.
  - Clone hangs with Git submodules — `git-clone-hangs.md`: submodule checkout.
  - Lightmap baking — `lightmap-baking.md`: lightmapper failures in the cloud.
  - Android build failures — `android-build-failures.md`: JDK, SDK and NDK mismatches.
  - Errors after a new Xcode version — `error-migrating-xcode.md`: Xcode migration errors.
  - macOS notarization failure — `osx-notarization-failure.md`: notarization and signing.
  - Problems during Xcode export — `xcode-export-issues.md`: iOS export errors.
- **Configure a build** — `basic-build-configuration/overview.md`: every target setting, builder and schedule.
  - Set up an iOS build configuration — `basic-build-configuration/set-up-an-ios-build-configuration.md`: iOS-only settings.
- **Connect your version control system** — `get-started-with-build-automation/connect-your-version-control-system.md`: repository URL, credentials, access.
- **Run builds automatically** — `run-builds/run-builds-automatically.md`: auto-build on push and schedules.
- **Run custom scripts** — `advanced-build-configuration/run-custom-scripts-during-the-build-process.md`: pre-export and post-export methods, and pre-build and post-build scripts.
- **Build Addressables** — `advanced-build-configuration/build-addressables-using-build-automation.md`: Addressables content in a build.
- **Specify the scenes to build** — `advanced-build-configuration/specify-the-scene-to-be-built.md`: the scene list.
- **Sign build artifacts** — `sign-build-artifacts.md`: index of the signing pages, which are in `sign-build-artifacts/`:
  - Sign an Android application — `sign-an-android-application.md`: keystores, and the debug keystore.
  - Sign an iOS application — `sign-an-ios-application.md`: certificates and provisioning profiles.
  - Sign a macOS application — `sign-a-macos-application.md`: signing and notarization.
- **Choose a machine specification** — `optimize-build-speed/choose-machine-specification.md`: CPU, RAM and disk per machine type.
- **Select a caching strategy** — `optimize-build-speed/select-caching-strategy.md`: what each cache keeps.
- **Choose a compression level** — `optimize-build-speed/choose-build-output-compression-level.md`: artifact size against speed.
- **Supported platforms on each builder OS** — `reference/supported-platforms-on-each-builder-os.md`: which machine builds which platform.
- **Supported Unity versions** — `reference/supported-unity-versions.md`: Editor versions the service builds with.
- **Available Xcode versions** — `reference/available-xcode-versions.md`: Xcode versions for iOS and macOS.
- **Available Android SDK versions** — `reference/available-android-sdk-versions.md`: Android SDK versions per target.
- **Unit tests** — `reference/unit-tests.md`: enabling and configuring unit tests in a build.

The Unity Manual pages are at `https://docs.unity3d.com/<VERSION>/Documentation/Manual/`:

- **Package Manager errors** — `upm-errors.html`: package resolution failures.
- **Android supported dependency versions** — `android-supported-dependency-versions.html`: SDK, NDK and JDK versions each Editor needs.
- **Android Player settings** — `class-PlayerSettingsAndroid.html`: scripting backend and target architectures.
- **Create a new keystore** — `android-keystore-create.html`: making an Android keystore in the Editor.
- **iOS Player settings** — `class-PlayerSettingsiOS.html`: the app's Target minimum iOS Version.
- **Build profiles** — `build-profiles.html`: per-platform build settings in Unity 6.
