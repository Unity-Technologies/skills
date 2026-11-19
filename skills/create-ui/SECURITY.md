# Security notes: create-ui

This skill has the agent run C# inside the user's open Unity Editor through `unity command script eval`, to set up scenes and Panel Settings, set UI states, and read styles and layout. Automated skill scanners flag that as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command script eval`, and runs a test script the agent writes that calls `unity command` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. `script eval` sits behind the Pipeline capability gate. No code fetched from a remote source is run. Where a named `unity command` covers a step, the skill uses that instead of `script eval`. |

## Mitigations

- **Local Editor only.** `unity command script eval` talks to the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `script eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes from this skill's own guides. Nothing downloaded from outside is executed.
- **Named commands first.** Compiling, entering Play mode, capturing the Game view, installing a package, and running tests use their dedicated `unity command` instead of `script eval`.
- **Scripts stay out of the project.** The C# files and the test script the agent runs are written to `Temp/`, not `Assets/`, so they aren't imported into the project or shipped in a build.
- **One package, from the Editor.** The only package the skill adds is the UI Test Framework, without a version, so Package Manager uses the one that ships with the Editor and nothing is downloaded. The final report names it so the user can remove it.
