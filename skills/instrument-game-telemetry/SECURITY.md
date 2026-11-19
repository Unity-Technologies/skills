# Security notes: instrument-game-telemetry

This skill changes the user's project to add telemetry: it installs packages, writes C# and
telemetry files, and can run C# in the user's live Unity Editor with `unity command script eval`
to read state while it chases a suspected fault and to drive Play mode during verification. It also uploads the telemetry registry and
publishes custom event schemas to the user's own Unity Gaming Services project. Automated skill
scanners flag running code in the Editor as a powerful capability. It is intentional, and it is
limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's live Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. The skill uses it to read state and `return` values while it chases a suspected fault, and to drive Play mode through the game's own run-state object during verification, not to change the project. |

## Mitigations

- **Only the user's own Editor.** The code runs in the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Changes only what telemetry needs.** The skill installs `com.unity.insights.events`, and, on a game with no analytics and no sign-in of its own, `com.unity.services.authentication`, shown as its own plan line and listed in the report; on a game that already has analytics and no identity system (the retrofit route) it is offered once. It writes the telemetry wiring, the registry manifest and its schemas under `Assets/`, the survey and plan files it names in its report, and one line in the project's `CLAUDE.md` or `AGENTS.md`. It tells the agent to create a restore point first and to change nothing unrelated.
- **Existing code waits for approval.** In a project with existing analytics, the skill adds new calls beside the old ones and never changes or removes existing code without the user's approval. Paths the user marks as protected are never edited.
- **Remote changes stay in the user's project.** The registry upload and schema publishing go to the user's own linked Unity Gaming Services project, through the Editor's deployment commands, and the report lists what was uploaded or published. A published schema version's content is fixed and a fix is the next version, so the skill copies each file's version into the manifest's pin before publishing and lists every published version in the report.
- **Deploys run without a prompt.** The upload goes to the active environment, production included, and is undone by uploading again; event definitions are versioned, so a wrong shape is fixed by publishing the next version and a wrong name by a new subject, and the upload changes no game behavior (it is stored as a Live Content config in the project). The restore point covers the repository, not the cloud project.
- **Nothing left behind.** The skill tells the agent never to leave verification code in the user's scripts.
- **No remote code.** The skill fetches package documentation to read it. Nothing downloaded from outside is executed.
