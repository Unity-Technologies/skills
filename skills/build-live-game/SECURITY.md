# Security notes: build-live-game

This skill has the agent read state from the user's open Unity Editor through `unity command script eval` to read the Cloud project ID after linking and the result of a feature it changed. Automated skill scanners flag that as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's open Editor through `unity command script eval` | It only reaches the Editor the user already has open, on their own machine, as that user, so it grants nothing they couldn't do themselves. `script eval` sits behind the Pipeline capability gate. No code fetched from a remote source is run. The skill uses it only to read state, and uses `unity command console status` for the console check. |

## Mitigations

- **Only the user's own Editor.** `unity command script eval` talks to the Editor open on this machine. It can't reach another machine or another user's Editor.
- **Capability gate.** `script eval` is only available when the project's Pipeline package provides it.
- **No remote code.** The agent runs C# it writes itself to read Editor state. Nothing downloaded from outside is executed.
- **Reads, not logging.** `script eval` only reads state; the skill reads results with it instead of adding logging. The one write it makes for verification is a temporary client Cloud Save call that checks an Access Control policy rejects it, removed afterwards.
- **Play mode only when needed.** The skill enters Play mode for that Access Control check and, when the user asks for proof or a fault is suspected, once to run the feature.
- **One Editor.** Every `unity command` names the project with `--project-path`, so it can't drive another project's Editor.
