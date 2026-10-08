# Security notes: unity-pipeline

This skill has the agent run C# inside a Unity Editor or development Player the user already has running, through `unity command eval`, `eval_file`, `run_script` and `reload_file`. Automated skill scanners flag that as a powerful capability. It is intentional, and it is limited by the safeguards below.

## Accepted risks

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Runs C# in the user's running Editor or development Player through `unity command eval`, `eval_file`, `run_script` and `reload_file` | It only reaches an instance the user started, with the Pipeline package they installed, so it grants nothing they couldn't do in that Editor themselves. No code fetched from a remote source is run. The skill steers ad-hoc code into versioned project files run with `run_script`, and keeps `eval` for one-liners. |

## Mitigations

- **Only the user's own instances.** Commands go to an Editor or development Player the user is running with the Pipeline package installed. Runtime commands need a development build; release Players have no Pipeline server.
- **No remote code.** The agent runs C# it writes for the user's task. Nothing downloaded from outside is executed.
- **Code lives in reviewable files.** Bulk work goes into scripts on disk run with `run_script`, so the user can read and version what ran. `eval` is reserved for short one-liners.
- **Compile-only check.** `run_script --dry_run true` reports diagnostics without loading or running anything.
- **Named commands first.** When a dedicated `unity command` covers a step, the skill uses it instead of running C#.
