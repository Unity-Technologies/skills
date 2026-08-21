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

# Split the suite across parallel CI jobs (seed the inventory with one full run first)
unity test /path/to/MyProject                       # writes test-results.xml
unity test /path/to/MyProject --shard 2/5           # writes test-results.shard-2-of-5.xml
unity test /path/to/MyProject --shard 2/5 --shard-inventory ./ci/full-run.xml
```

`unity test` launches the editor's built-in test runner in batch mode (`-runTests -testPlatform <mode> -testResults <path> -testFilter <pattern>`), waits for it to finish, and writes the report to `--output` (default `test-results.xml`). It exits 0 when every test passes, 8 (EXIT_TESTS_FAILED) when the run completed and reported failing tests, and 6 (EXIT_COMMAND_FAILURE) when the run never produced a verdict — a compile error, an unavailable license, an editor crash, an unknown test platform, or `--timeout`. That split is what lets CI retry an infrastructure failure without ever retrying a failing test; under `--format json` it also appears as `errors[0].code` (`TESTS_FAILED` versus `TEST_RUN_ERROR` / `TEST_TIMED_OUT`), so pipelines never have to match the localized message. It runs the tests **directly via the editor command line** — no pipeline package or server is involved. `--mode` is optional; when omitted, `-testPlatform` is not passed and the editor runs its default platform.

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

#### GitHub Actions annotations (`--format github`)

`--format github` is a global format value — accepted on every command, and settable via `UNITY_FORMAT` — that emits [GitHub Actions workflow commands](https://docs.github.com/actions/reference/workflow-commands-for-github-actions) for failures instead of plain terminal text. It is listed here because CI is where it earns its keep:

```bash
unity test /path/to/MyProject --format github
unity build --target StandaloneWindows64 --output-path ./Build/Game.exe --format github
```

- **A failing command annotates the job.** Its error message is emitted as `::error::<message>`, and a warning as `::warning::<message>`, so the runner surfaces it as an annotation on the run rather than leaving it buried in the log.
- **Annotations go to stdout**, which is where GitHub's own documented examples emit them.
- **Human progress output still renders**, so the run stays readable — only the error and warning channels change shape.
- **Annotations are not anchored to a file and line yet.** Harvesting compile diagnostics out of the editor log (`::error file=…,line=…,col=…`) and wrapping that log in a collapsible `::group::` are not part of this format today, so a compile error annotates with the command's message rather than landing on the diff.

This is complementary to `--report-format junit`, not an alternative: a JUnit file needs an upload-and-report step and surfaces in a separate tab, while annotations need neither. Using both together is reasonable — and today the JUnit report is what gets you per-test detail.

Outside GitHub Actions the format is inert — the `::`-prefixed lines are ordinary text to any other terminal or log collector, so a local `--format github` run is reproducible and harmless.

> Every value is sanitized and percent-encoded before it reaches a workflow command. The protocol is line-oriented and the runner does not escape anything, so an unescaped newline in an editor-supplied message would end the command and let the rest be interpreted as a new one. Never hand-build `::` lines around the CLI's output.

#### Code coverage

`--coverage` drives Unity's [Code Coverage package](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@latest) by passing `-enableCodeCoverage -coverageResultsPath <path>` (plus `-coverageOptions` when `--coverage-options` is given). `--coverage-output` defaults to `CodeCoverage` relative to the working directory.

Coverage **degrades gracefully**: if the project does not depend on `com.unity.testtools.codecoverage` (checked in `Packages/manifest.json`, then `Packages/packages-lock.json`), the CLI prints a warning naming the missing package, skips the coverage flags, and runs the tests normally. It never fails the test run for a missing coverage package — `-enableCodeCoverage` on a project without it silently produces nothing, which is the confusing outcome this replaces. `--coverage-output` / `--coverage-options` without `--coverage` is an error.

#### Sharding across parallel CI jobs

`--shard N/M` runs one deterministic slice of the suite, so a matrix job can take `--shard 1/5` through `--shard 5/5` and finish in a fifth of the time. `N` is 1-based and must be between 1 and `M`; anything else is a usage error (exit **2**).

**It needs an inventory first.** The editor has no way to list tests without running them, so the CLI reads the full test names out of an NUnit3 report an earlier full run already wrote. That report defaults to the unsharded `--output` path (`test-results.xml`), and `--shard-inventory <path>` points at one somewhere else — a cached CI artifact, say. Seed it by running `unity test` once without `--shard`. Without a readable inventory the command fails and says so rather than guessing; an inventory holding no test cases fails the same way.

**The inventory must be NUnit, not JUnit.** The two formats spell the element differently (`test-case` against `testcase`), so a JUnit report reads as simply having no tests. Because `--report-format junit` makes `--output` *the JUnit report*, `--shard` without an explicit `--shard-inventory` is rejected up front in that combination — a usage error, exit **2** — rather than failing later as an empty inventory. Either point `--shard-inventory` at an NUnit report, or ask for both formats with `--report-format nunit,junit`, which keeps `--output` NUnit. Pointing `--shard-inventory` at a JUnit file by hand is caught too, and says so specifically.

Assignment is a hash of each test's fully qualified name, which gives the properties a CI matrix depends on:

- **Deterministic** — the same suite and shard count always produce the same assignment, so a failure reproduces on the shard that reported it.
- **Complete and disjoint** — every test in the inventory belongs to exactly one shard. Nothing is skipped and nothing runs twice.
- **Independent of outcome and timing** — nothing about a previous run's results or duration feeds the split, so it does not drift between runs.
- **Stable as the suite grows** — because the slice follows the name rather than a position in the file, adding a test leaves every other test on the shard it was already on. Changing `M` reshuffles everything, which is inherent to repartitioning.

Precedence with the other options:

| Option | When sharding |
|---|---|
| `--mode` | Unchanged — selects the platform, and is orthogonal to the split. |
| `--filter` | Applied **by the CLI**, to narrow the inventory before the shard is taken from what survives. It has to be: the editor accepts a single `-testFilter`, and on a sharded run the slice's explicit name list already occupies it, so a `--filter` passed through would be dropped. Filtering here also makes the `tests` reported in `--format json` the ones that will actually run. It accepts the editor's own syntax — a semicolon-separated list of full names or regular expressions, each optionally negated with `!`. Note this ordering does **not** rebalance anything: assignment hashes each name independently, so filtering before or after the partition gives the same membership, and a filter can still leave a shard empty. |
| `--output`, `--junit-output`, `--coverage-output` | Every artifact path gains a `.shard-N-of-M` suffix before its extension, so shards sharing a working directory never overwrite each other and a merge step can collect them by glob. |

Two cases end the run early rather than producing something misleading. A slice with **no tests assigned** (more shards than tests) skips the editor entirely and exits 0 with a warning — launching with an empty filter would run the whole suite, turning `M` shards into `M` full runs. A slice whose assembled filter is **too long for a single editor argument** fails and tells you to raise the shard count, which shortens every slice. A test whose name contains a semicolon cannot be expressed in the editor's filter syntax at all, so it fails rather than silently not running.

With `--format json` the envelope reports every artifact, so a pipeline can locate them without guessing:

```json
{
  "projectPath": "/path/to/MyProject",
  "output": "/path/to/results.xml",
  "reports": { "nunit": "/path/to/results.xml", "junit": "/path/to/results.junit.xml" },
  "coverage": { "requested": true, "enabled": true, "output": "/path/to/coverage" },
  "shard": { "index": 2, "count": 5, "testCount": 2, "tests": ["Acme.Tests.A", "Acme.Tests.B"] }
}
```

`reports.junit` is `null` when JUnit was not requested, `reports.nunit` is `null` when only JUnit was. `coverage.requested` with `enabled: false` is the missing-package case. `shard` is **absent entirely** on an unsharded run, so output every existing script already parses is unchanged.

#### Retrying failing tests and reporting flakes

`--retries N` re-runs the tests that failed, up to `N` extra attempts, stopping as soon as they all pass. Retries are **off by default**; `N` must be a whole number, zero or more (anything else is a usage error, exit **2**). `--timeout` applies **per attempt**, not to the whole sequence.

Only the failing tests are re-run — the CLI reads their names out of the NUnit report the run just wrote and hands them back to the editor as a single `-testFilter` — so a retry costs the failures, not another full pass.

**The point is reporting flakes, not hiding them.** The final summary separates three outcomes: passed the first time, passed only on a retry (**flaky**), and failed every attempt. A flaky test does not silently turn the job green: the run exits **0**, because the tests do pass, and the flakiness is stated in the human output, in `--format json`, and in a file. Tests that never pass still exit **8**.

**A test leaves the failing set only when a report says it passed.** The editor exits `0` for "ran them and they passed", for "matched none of them", and — it reads a filter entry as a name *or a regular expression* — for "matched a neighbouring test". A test that did not run, ran under a different name, was skipped, or was inconclusive is carried forward, because none of those contradict a failure; so is anything an attempt that failed again did not clear. When an attempt does not clear everything it was given, the survivors stay failing, the run exits **8**, and a warning says why.

**A run that never produced a verdict is not retried.** A compile error, a crashed editor, an expired license or a `--timeout` all keep exit **6** and stop immediately, without spending the budget: there is no failing set to narrow to, and re-running the same broken configuration would only reproduce the same error. That is the split `unity test` already draws between exit `8` and exit `6`.

Artifacts:

| Path | Contents |
|---|---|
| `--output` (e.g. `test-results.xml`) | The **first** attempt's report — the full-suite record. Deliberately not rewritten to show a flaky test as passing, since that would hide what the retry found. |
| `test-results.attempt-2.xml`, `.attempt-3.xml`, … | One report per retry. Not kept under `--report-format junit` alone, where the NUnit reports live in a scratch directory discarded with the run. |
| `test-results.retries.json` | The machine-readable retry summary, written whenever a retry ran, passing or failing. It exists because a failing run's JSON envelope carries `data: null`, and the per-test attempt counts matter most when the run failed. |

**A JUnit report is converted from the first attempt too**, so a run that went green on a retry still ships a JUnit file containing the original failure. Many CI systems gate on the ingested JUnit artifact rather than the exit code, and for those the flake still shows red — gate on the exit code, or on `retries.failed`, when you want a flake to pass.

**Coverage is collected on the first attempt only.** A retry runs a handful of tests, so letting it write the same coverage path would replace the whole suite's coverage with that subset's.

Under `--format json` the same summary appears as a `retries` block, **absent entirely** when `--retries` was not used or nothing failed. Only tests that failed at least once are listed; `passedFirstAttempt` counts the rest:

```json
"retries": {
  "requested": 2,
  "attempts": 3,
  "passedFirstAttempt": 41,
  "flaky": [{ "test": "Maths.Calc.Adds", "attempts": 2 }],
  "failed": [{ "test": "Maths.Calc.Divides", "attempts": 3 }]
}
```

`--rerun-failed` starts from the failures a **previous** run recorded in `--output` instead of running the whole suite, so a follow-up job can re-check just the failures of the first. It reads the same NUnit report `--shard` reads its inventory from, honours `--filter` (applied CLI-side, against the failing set), and can be combined with `--retries`. It writes to a derived path — `test-results.xml` becomes `test-results.rerun.xml` — rather than overwriting `--output`, which would replace the full-suite record with a partial one and shrink the inventory `--shard` reads from that same default path.

Three cases end early rather than doing something misleading. **Nothing failed** → the editor is not started and the run exits 0 with a warning, because an empty test filter is no filter at all and launching would run everything. **`--report-format junit` alone** → rejected up front (exit **2**): `--output` is then the JUnit report, and the failing set is read from NUnit, so ask for `--report-format nunit,junit`. **`--shard`** → rejected (exit **2**): the editor accepts one test filter and each option needs it, so to retry a shard, run that shard again with `--retries`.

Options: `--mode EditMode|PlayMode`, `--filter <pattern>`, `--output <path>`, `--report-format nunit|junit|nunit,junit`, `--junit-output <path>`, `--shard <n/m>`, `--shard-inventory <path>`, `--retries <n>` (0-10), `--rerun-failed`, `--coverage`, `--coverage-output <path>`, `--coverage-options <options>`, `--editor-version <version>` (env `UNITY_EDITOR_VERSION`), `-e, --editor-path <path>`, `-a, --architecture <arch>`, `--allow-install`, `--timeout <seconds>` (env `UNITY_TEST_TIMEOUT`).

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
| `--timeout <seconds>` (env `UNITY_BUILD_TIMEOUT`) | Abort a build that runs longer than this many seconds, exit **6**. Disabled by default. |
| `--provenance-path <path>` | Where to write the provenance manifest. Default: beside the build output, or beside the log file when `--output-path` is not set. |
| `--no-provenance` | Do not write the provenance manifest. |

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

**Provenance manifest** — every build that reaches the editor writes a JSON manifest recording what produced it: editor version and changeset, resolved package set, target, profile, execute method, version stamp, git revision and dirty flag, CLI version, timestamps, and outcome. Failed builds get one too, with the exit code, so they stay diagnosable. It is redacted for publication — paths are project-relative, `--args`, the Android keystore flags, the editor's install location and the hostname are never written, a git package reference keeps its locator but loses any embedded credentials, and a `file:` dependency is recorded as `file:<local>` — so it can be attached to a release next to the artifact. Under `--format json` / `--format ndjson` the path is reported as `data.provenance` (omitted when no manifest was written), on failed builds as well as successful ones. The git revision is captured before the build starts, so an artifact written into the project does not make the manifest claim the build came from a dirty tree. `--provenance-path` and a relative `--output-path` both resolve against the current directory, matching what the CLI hands Unity. A manifest that cannot be written warns instead of failing the build. Schema: `apps/cli/docs/build-provenance.md`.

**Interrupt exit codes** — interrupting `unity build` exits with the conventional signal code (`130` for Ctrl-C / SIGINT, `143` for SIGTERM) rather than a generic `1`, so callers and CI can tell an aborted build apart from a failed one. The temporary Android keystore is scrubbed before exit.

**Stall heartbeat** — a long build prints a periodic heartbeat (`Still building — 4m30s elapsed, last log output 3m10s ago`) tracking both total elapsed time and time since the Editor log last grew, so silence in the log no longer looks the same as a hang. Detection itself reads the Editor log's size directly, so it keeps working regardless of output mode — but whether the heartbeat is *printed* depends on the mode: on the human path it goes to stderr (so the streamed log stays clean) and is unaffected by `--no-tail`, but **`--quiet` suppresses it entirely** in human mode. Under `--format json`/`--format ndjson` it appears as periodic progress frames and is emitted regardless of `--quiet` — quiet only silences the human path.

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

