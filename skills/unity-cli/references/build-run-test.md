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

**Reserved flags — do NOT pass these after `--`.** The command adds them itself: `-batchmode`, `-quit`, `-projectPath`, `-useHub`, `-hubIPC`. Passing any of them fails fast (before launch) with exit code 6:

```
Error: Forwarded argument '-batchmode' conflicts with a reserved Unity flag managed by this command. Remove it from the args after `--`.
```

Flags like `-nographics`, `-logFile <path>`, and `-executeMethod <Class.Method>` are not reserved and are forwarded normally.

When `--timeout <seconds>` is set, the process receives SIGTERM at the deadline; if still alive after 2 s it receives SIGKILL. The command exits with code 6 (EXIT_COMMAND_FAILURE) on timeout.

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
```

`unity test` launches the editor's built-in test runner in batch mode (`-runTests -testPlatform <mode> -testResults <path> -testFilter <pattern>`), waits for it to finish, and writes the report to `--output` (default `test-results.xml`). It exits 0 when the run succeeds and 6 (EXIT_COMMAND_FAILURE) when the editor exits non-zero — i.e. reports test failures or fails to run. It runs the tests **directly via the editor command line** — no pipeline package or server is involved. `--mode` is optional; when omitted, `-testPlatform` is not passed and the editor runs its default platform.

It deliberately does **not** pass `-quit`: `-runTests` quits the editor itself once results are written, so forcing `-quit` would terminate it before the report exists. Anything after `--` is forwarded to the editor verbatim, except reserved flags managed by the command (`-projectPath`, `-batchmode`, `-runTests`, `-testPlatform`, `-testResults`, `-testFilter`, `-quit`, `-useHub`, `-hubIPC`), which are rejected.

Options: `--mode EditMode|PlayMode`, `--filter <pattern>`, `--output <path>`, `--editor-version <version>` (env `UNITY_EDITOR_VERSION`), `-e, --editor-path <path>`, `-a, --architecture <arch>`, `--allow-install`, `--timeout <seconds>` (env `UNITY_TEST_TIMEOUT`).

---

### Build

`--target` and `--execute-method` are both **required** — Unity has no built-in command-line build, so your `executeMethod` is responsible for the actual build (including honoring `--output-path`).

```bash
# Build a project (requires --target and --execute-method)
unity build /path/to/MyProject \
  --target StandaloneOSX \
  --execute-method Builder.PerformBuild \
  --output-path ./build/output

# Common build targets: StandaloneOSX, StandaloneWindows64, StandaloneLinux64, Android, iOS, WebGL
```

**Options:**

| Flag | Description |
|---|---|
| `--target <target>` | Build target (required). |
| `--execute-method <method>` | Static C# method to invoke, e.g. `Builder.PerformBuild` (required). |
| `--build-target-group <group>` | Forwarded to Unity as `-buildTargetGroup`. |
| `-o, --output-path <path>` | Passed as `-buildOutput` (your method must honor it). |
| `-l, --log-file <path>` | Log file path. Default: `<project>/Logs/build-<target>-<timestamp>.log`. |
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

