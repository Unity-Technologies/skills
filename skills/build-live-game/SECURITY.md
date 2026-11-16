# Security notes: build-live-game

This skill has the agent change the user's Unity project and its Unity Gaming Services environment: it adds UGS packages, creates Cloud Code modules and cloud resource files, deploys them, and, when no named command covers a step, runs C# inside the user's open Unity Editor through `unity command script eval`. Automated skill scanners flag these as powerful capabilities. They are intentional, and limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command script eval`, and deploys cloud resources from the Editor to the project's linked UGS environment | It only reaches the Editor the user already has open, on their own machine, as that user, behind the Pipeline capability gate, and an Editor deploy goes only to the project and environment the project is linked to. Where a named `unity command` or `unity ugs` command covers a step, the skill uses that instead of `eval`. |
| `SEC_POWER_CAP` | Reads and writes live UGS data and deploys with the `unity ugs` CLI, which has its own credentials (a service account or Unity Hub sign-in) and its own target project and environment | It is outside the Editor and the Pipeline gate, so the skill has the agent check the CLI's configured project ID and environment against the linked project and the environment the user asked for before any write, and stop and ask if they differ. The credentials are the user's own. |
| `SEC_POWER_CAP` | Imports Unity's Asset Store Building Blocks and sample projects, whose runtime code and legacy Cloud Code (`.ccmr` modules, JavaScript scripts) then compile, deploy and run | These are Unity's own published packages, imported only when the user's feature matches one. The agent reviews the imported Cloud Code before deploying it, and the final report names every package it imported. |

## What it changes

- **Packages:** adds or upgrades UGS packages in `Packages/manifest.json`: `com.unity.services.core`, `com.unity.services.authentication`, `com.unity.services.cloudcode`, `com.unity.services.cloudsave`, `com.unity.remote-config`, `com.unity.services.leaderboards`, `com.unity.services.deployment`, `com.unity.services.tooling` and `com.unity.services.apis`, only those the feature needs, and `com.unity.pipeline` with `unity pipeline install` when linking the project needs it.
- **Project files:** creates scripts, Cloud Code modules (`.ccmu`, `.ccmr`) and cloud resource files (`.rc`, `.ac`, `.lb`, `.ugo`).
- **Project settings:** links the project to a Unity project ID and environment, after asking the user, and sets the API Compatibility Level a Cloud Code module needs.
- **Cloud environment:** deploys those resources to the active UGS environment, which changes the live configuration, Cloud Code and access policies that environment's players see.
- **CLI plugin:** installs the `unity ugs` plugin with `unity plugin install ugs` when it is missing, and sets its target project and environment.
- **Imported packages:** imports Asset Store Building Blocks or sample projects when the feature matches one.

## Mitigations

- **Restore point first.** The skill requires a commit or a copy of the project before it changes anything.
- **Only the user's own Editor and linked project.** Everything runs on this machine as this user, against the environment the project is linked to.
- **No arbitrary remote code.** The agent runs C# it writes for the user's task. The only downloaded code is Unity's own Building Blocks and samples, imported when the feature matches one.
- **CLI target checked first.** Before a `unity ugs` write, the agent confirms the CLI's project ID and environment match the linked project and the requested environment.
- **Named commands first.** When a `unity command` or `unity ugs` command covers a step, the skill uses it instead of `eval`.
- **One Editor.** Every `unity command` names the project with `--project-path`, so it can't drive another project's Editor.
- **Temporary checks are removed.** A temporary call added to test an Access Control policy is removed afterwards, and the final report lists it.
