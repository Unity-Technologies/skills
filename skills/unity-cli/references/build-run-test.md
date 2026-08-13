# Run, test & build — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

---

### Run — batch/headless execution

```bash
# Run a Unity project headless (batch mode is automatic — do NOT pass -batchmode/-quit)
unity run /path/to/MyProject -- -executeMethod Builder.Build

# Override editor version
unity run /path/to/MyProject --editor-version 6000.0.47f1 -- -nographics -logFile out.log

# Install editor automatically if missing
unity run /path/to/MyProject --allow-install -- -executeMethod Builder.Build

# Kill the Unity process after 300 seconds (useful in CI to prevent hangs)
unity run /path/to/MyProject --timeout 300 -- -executeMethod Builder.Build
# Equivalent via env var:
UNITY_RUN_TIMEOUT=300 unity run /path/to/MyProject -- -executeMethod Builder.Build
```

`unity run` always launches the editor in batch mode and forwards the args after `--` to the Unity executable, then returns the editor's exit code.

**Reserved flags — do NOT pass these after `--`.** The command manages `-batchmode`, `-quit`, and `-projectPath` itself, and deliberately never passes `-useHub`/`-hubIPC` (the CLI runs no Hub IPC server, so those flags would make the editor launch the Unity Hub). Passing any of the five fails fast (before launch) with exit code 6:

```
Error: Forwarded argument '-batchmode' conflicts with a reserved Unity flag managed by this command. Remove it from the args after `--`.
```

Flags like `-nographics`, `-logFile <path>`, and `-executeMethod <Class.Method>` are not reserved and are forwarded normally.

Reserved-flag matching is spelling-insensitive: Unity accepts `-projectPath`, `--projectPath` and `-projectPath=<value>` interchangeably, so all three spellings are rejected (case-insensitively). This applies to every command that forwards user args — `unity run`, `unity test`, `unity build --args`, and `unity open --args`.

When `--timeout <seconds>` is set, the process receives SIGTERM at the deadline; if still alive after 2 s it receives SIGKILL. The command exits with code 6 (EXIT_COMMAND_FAILURE) on timeout.

#### run --command — execute a registered Editor command headlessly

`unity run --command <name>` runs a registered `[CliCommand]` Editor command in a single invocation: the CLI starts the Editor in batch mode, waits for the project's Pipeline server, runs the command with the arguments after `--` parsed against the command's `[CliArg]` schema (no hand-written `Environment.GetCommandLineArgs()` parsing), prints the return value, and shuts the Editor down. A running Editor with the project already open is reused (and left running) instead of spawning a second one. Requires the `com.unity.pipeline` package (`unity pipeline install` — see [integration-advanced.md](integration-advanced.md)).

```bash
# Run a registered command; arguments after -- are parsed against its [CliArg] schema
unity run /path/to/MyProject --command my_command -- --count 3 --label demo

# JSON result envelope (data carries the return value); bound the wait
unity run /path/to/MyProject --command my_command --format json --timeout 120
```

**Worked example.** Given this command in the project (authoring details in [integration-advanced.md](integration-advanced.md)):

```csharp
public static class MyPipelineCommands
{
    [CliCommand("greet", "Log a greeting and return its length")]
    public static int Greet(
        [CliArg("name", "Who to greet", Required = true)] string name)
    {
        Debug.Log($"Hello, {name}!");
        return name.Length;
    }
}
```

`unity run . --command greet -- --name Ada` prints the return value (`name.Length` → `3`) last on stdout, while the Editor log — including the `Hello, Ada!` from `Debug.Log` — streams to stderr:

```text
Starting Unity 6000.0.47f1 (Apple Silicon)...
Waiting for the Pipeline server to start...
Executing "greet" on the Editor...
Command "greet" completed.
3
```

With `--format json`, stdout carries a single result envelope instead — `data.result` is the return value, `data.parameters` the parsed args, and `data.reusedRunningEditor` tells you whether an already-open Editor was used:

```json
{
  "success": true,
  "command": "run",
  "data": {
    "projectPath": "/path/to/MyProject",
    "command": "greet",
    "parameters": {
      "name": "Ada"
    },
    "result": 3,
    "reusedRunningEditor": false,
    "success": true
  },
  "errors": [],
  "warnings": []
}
```

The Editor log — including `Debug.Log` output — streams to stderr, and a failed command exits non-zero. Unlike a bare `unity run` (which forwards args to the Unity executable), `--command` targets a Pipeline command by name; use `unity command` / `unity list` in [integration-advanced.md](integration-advanced.md) to discover what a connected Editor exposes.

---

### Test — run EditMode/PlayMode tests

```bash
# Run tests and write an NUnit XML report (omitting --mode runs the editor's default platform)
unity test /path/to/MyProject

# Run a specific platform (--mode is case-insensitive: EditMode/editmode both work)
unity test /path/to/MyProject --mode EditMode
unity test /path/to/MyProject --mode PlayMode --output ./results/play.xml

# Run only tests whose names match a filter
unity test /path/to/MyProject --filter "MyNamespace.MyTests"

# Pin the editor version, installing it if missing; cap the run at 600 s
unity test /path/to/MyProject --editor-version 6000.0.47f1 --allow-install --timeout 600
# Equivalent via env var:
UNITY_TEST_TIMEOUT=600 unity test /path/to/MyProject

# Forward extra editor args after -- (reserved test flags are rejected)
unity test /path/to/MyProject -- -nographics

# Write a JUnit report for CI instead of NUnit: --output IS the JUnit file
unity test /path/to/MyProject --report-format junit --output ./results/junit.xml

# Write both from one editor run (JUnit defaults to <output>.junit.xml)
unity test /path/to/MyProject --report-format nunit,junit
unity test /path/to/MyProject --report-format nunit,junit --junit-output ./results/ci.xml

# Collect code coverage (requires com.unity.testtools.codecoverage in the project)
unity test /path/to/MyProject --coverage --coverage-output ./coverage
unity test /path/to/MyProject --coverage --coverage-options "generateHtmlReport"
```

`unity test` launches the editor's built-in test runner in batch mode (`-runTests -testPlatform <mode> -testResults <path> -testFilter <pattern>`), waits for it to finish, and writes the report to `--output` (default `test-results.xml`). It exits 0 when the run succeeds and 6 (EXIT_COMMAND_FAILURE) when the editor exits non-zero — i.e. reports test failures or fails to run. It runs the tests **directly via the editor command line** — no pipeline package or server is involved. `--mode` is optional; when omitted, `-testPlatform` is not passed and the editor runs its default platform.

It deliberately does **not** pass `-quit`: `-runTests` quits the editor itself once results are written, so forcing `-quit` would terminate it before the report exists. Anything after `--` is forwarded to the editor verbatim, except reserved flags (`-projectPath`, `-batchmode`, `-runTests`, `-testPlatform`, `-testResults`, `-testFilter`, `-quit`, `-useHub`, `-hubIPC`, `-enableCodeCoverage`, `-coverageResultsPath`, `-coverageOptions`), which are rejected — those are managed by the command (use `--coverage` for the coverage trio); `-useHub`/`-hubIPC` are deliberately never passed (the CLI runs no Hub IPC server).

#### Report formats (CI-native JUnit)

The editor only ever writes NUnit3, so JUnit is produced by converting that report after the run. `--report-format` decides what `--output` contains:

| `--report-format` | `--output` holds | Also written |
|---|---|---|
| `nunit` (default) | NUnit3 — today's behaviour, unchanged | — |
| `junit` | JUnit | nothing (the editor's NUnit3 goes to a scratch file that is converted and removed) |
| `nunit,junit` | NUnit3 | JUnit at `--junit-output`, defaulting to `--output` with the extension replaced by `.junit.xml` |

`--junit-output` is only valid with `nunit,junit` — with `junit` alone the JUnit report *is* `--output`, so passing both is an error rather than a silent no-op. It also may not resolve to the same file as `--output` (case-insensitively on Windows): writing both reports to one path would overwrite the NUnit report with the JUnit one while still claiming two artifacts were produced.

All of these flag-combination mistakes, and an unknown `--report-format` value, are usage errors and exit **2** (`EXIT_BAD_ARGS`) — not 6 — so a CI script can tell "I invoked the command wrongly" from "the operation failed". They are also checked before the project and editor are resolved, so a usage mistake reports itself rather than surfacing as a missing-editor error.

**The JUnit report is written even when tests fail**, before the non-zero exit is surfaced — that is exactly when a CI system needs it to annotate the failures. A run whose results cannot be converted (a truncated report from an editor that died mid-write, say) fails the command and names the file it could not read.

#### Code coverage

`--coverage` drives Unity's [Code Coverage package](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@latest) by passing `-enableCodeCoverage -coverageResultsPath <path>` (plus `-coverageOptions` when `--coverage-options` is given). `--coverage-output` defaults to `CodeCoverage` relative to the working directory.

Coverage **degrades gracefully**: if the project does not depend on `com.unity.testtools.codecoverage` (checked in `Packages/manifest.json`, then `Packages/packages-lock.json`), the CLI prints a warning naming the missing package, skips the coverage flags, and runs the tests normally. It never fails the test run for a missing coverage package — `-enableCodeCoverage` on a project without it silently produces nothing, which is the confusing outcome this replaces. `--coverage-output` / `--coverage-options` without `--coverage` is an error.

With `--format json` the envelope reports every artifact, so a pipeline can locate them without guessing:

```json
{
  "projectPath": "/path/to/MyProject",
  "output": "/path/to/results.xml",
  "reports": { "nunit": "/path/to/results.xml", "junit": "/path/to/results.junit.xml" },
  "coverage": { "requested": true, "enabled": true, "output": "/path/to/coverage" }
}
```

`reports.junit` is `null` when JUnit was not requested, `reports.nunit` is `null` when only JUnit was. `coverage.requested` with `enabled: false` is the missing-package case.

Options: `--mode EditMode|PlayMode`, `--filter <pattern>`, `--output <path>`, `--report-format nunit|junit|nunit,junit`, `--junit-output <path>`, `--coverage`, `--coverage-output <path>`, `--coverage-options <options>`, `--editor-version <version>` (env `UNITY_EDITOR_VERSION`), `-e, --editor-path <path>`, `-a, --architecture <arch>`, `--allow-install`, `--timeout <seconds>` (env `UNITY_TEST_TIMEOUT`).

---

### Build

The first-class build workflow. Rule of thumb vs `unity run`: building a player → `unity build`; anything else headless → `unity run`.

Pick one build strategy: a Unity 6+ Build Profile (`--profile`), a built-in desktop player build (`--target` with a desktop target, `--output-path` required), or a custom `--execute-method` (your method is responsible for the actual build, including honoring `--output-path`). Non-desktop targets need `--profile` or `--execute-method`.

The build log is always written to the log file **and** streamed to stdout at the same time; pass `--no-tail` to write the file only (the tail is also suppressed by `--quiet` and `--format ndjson`).

```bash
# Build with a custom build method
unity build /path/to/MyProject \
  --target StandaloneOSX \
  --execute-method Builder.PerformBuild \
  --output-path ./build/output

# Build with a Unity 6+ build profile
unity build /path/to/MyProject --profile "Windows Release" --output-path ./Build/MyGame.exe

# Common build targets: StandaloneOSX, StandaloneWindows64, StandaloneLinux64, Android, iOS, WebGL
```

**Options:**

| Flag | Description |
|---|---|
| `--target <target>` | Build target (required unless `--profile` is used). |
| `--execute-method <method>` | Static C# method to invoke, e.g. `Builder.PerformBuild`. Optional: without it, the CLI uses Unity's built-in build. |
| `--profile <profile>` | Build profile: a `.asset` path or a profile name in `Assets/Settings/Build Profiles` (Unity 6+; the profile defines the target). |
| `--build-target-group <group>` | Forwarded to Unity as `-buildTargetGroup`. |
| `-o, --output-path <path>` | Output path. With `--execute-method`, passed as `-buildOutput` (your method must honor it); otherwise the built-in build's destination (required). |
| `-l, --log-file <path>` | Log file path. Default: `<project>/Logs/build-<target>-<timestamp>.log`. Streamed to stdout by default (see `--no-tail`). |
| `--editor-version <version>` | Override editor version (default: from `ProjectVersion.txt`). |
| `-e, --editor-path <path>` | Use a specific editor binary. |
| `-a, --architecture <arch>` | Editor architecture (`x86_64` or `arm64`). |
| `--args <string>` | Extra arguments passed to Unity (shell-split). |
| `--no-tail` | Do not stream the log to stdout in real time. |
| `--allow-install` | Install the project's editor version if missing. |
| `--versioning-strategy <strategy>` | `semantic`, `tag`, `custom`, or `none` (default: `none`). |
| `--build-version <version>` | Explicit version string; only used with `--versioning-strategy custom`. |
| `--allow-dirty-build` | Skip the uncommitted-changes guard (default: false). |

**Android signing & export** (applied to Android targets only):

| Flag | Description |
|---|---|
| `--android-export-type <type>` | `apk`, `aab`, or `android-studio-project`. |
| `--android-keystore-base64 <b64>` | Keystore file, base64-encoded. |
| `--android-keystore-password <pass>` | Keystore password. |
| `--android-key-alias <alias>` | Key alias within the keystore. |
| `--android-key-alias-password <pass>` | Key alias password. |
| `--android-target-sdk-version <N>` | Target SDK version. |
| `--android-symbol-type <type>` | `none`, `public`, or `debugging`. |
| `--android-version-code <N>` | Android version code. |

Keystore flags are validated together. Secrets passed as command-line flags surface in the process list and can be echoed into CI logs. Supply `--android-keystore-base64`, `--android-keystore-password`, and `--android-key-alias-password` from CI secret environment variables (e.g. `--android-keystore-password "$KEYSTORE_PASSWORD"`), never as inline literals, and source those variables from a dedicated CI secret store. Note that sourcing from an env var only avoids hard-coding the literal — the expanded value still appears in `argv`, so also mask it in CI log output.

**Versioning** — `semantic` and `tag` derive the version from git tags/history; `custom` requires an explicit `--build-version`; a dirty working tree is rejected unless `--allow-dirty-build` is passed.

**Interrupt exit codes** — interrupting `unity build` exits with the conventional signal code (`130` for Ctrl-C / SIGINT, `143` for SIGTERM) rather than a generic `1`, so callers and CI can tell an aborted build apart from a failed one. The temporary Android keystore is scrubbed before exit.

```bash
# With --format json, stdout includes newline-delimited JSON progress frames before the final envelope:
unity build /path/to/MyProject --target StandaloneOSX --execute-method Builder.Build --format json
# Output (each line is a JSON object):
# {"type":"progress","command":"build","message":"Resolving project..."}
# {"type":"progress","command":"build","message":"Resolving editor..."}
# {"type":"progress","command":"build","message":"Starting Unity..."}
# {"type":"progress","command":"build","message":"Unity exited (code 0)"}
# { "success": true, "command": "build", "data": { "target": "...", "logFile": "..." } }
```

---

