# Security notes: transform-cli

This skill has the agent run Python against the Asset Transformer SDK in a Transform CLI session on the user's machine, through `run code`, `run file` and `workflow run`. It can also install a shared workflow file from an `https://` URL with `workflow install`. Running generated code is a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs agent-written Python, and workflow files, in a local Transform CLI session through `run code`, `run file` and `workflow run` | A session is a Python process on the user's own machine, running as that user, so a script can do what that user's account can do and no more, as a Python script the user ran with the SDK could. The skill uses a dedicated command when one covers the step, and keeps scripts for SDK calls no command makes. |

## Mitigations

- **Bounded by the user's own permissions, not sandboxed.** A script is ordinary Python: it can read and write what the user can and open network connections. The agent writes scripts for the user's task, and runs a downloaded workflow only when the user gave its source.
- **Commands before scripts.** The skill runs a named command (`scene`, `algo`, `occurrence`, `material`) when one covers the step, so most work never runs free-form code.
- **A shared workflow is code, trusted on purpose.** The skill installs one only from a URL or path the user gives, never one it finds itself. `workflow install` accepts `--sha256` to pin the exact file, refuses to replace a file edited since it was installed without `--force`, and records where each file came from (`workflow show`). `workflow check` checks only a workflow's SDK calls against the loaded SDK; it doesn't make the code safe to run.
- **A workflow the user didn't name runs only with their agreement.** A project's or the user's own workflow file outranks a built-in of the same name, so before running a workflow whose `origin` (`workflow show`) isn't `built-in` and that the user didn't name, the skill shows them its path and waits for their agreement.
- **The source model is never overwritten.** Exports go to a new path, so re-importing the source into a fresh session undoes any run.
- **Machine-wide changes need the user.** The skill asks before `license activate` or `license server set`, and `license activate` needs the user's own administrator approval.
