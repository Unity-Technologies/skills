# Exit codes — unity-cli reference

Part of the **`unity-cli`** skill. Read this when the main page points here.

## Exit codes

Branch on the exit code, then on `errors[0].code` in the json or ndjson envelope. **Retry** says
whether running the same command again, unchanged, can succeed. A command that starts work
(`pipeline cloud-build builds start`, `pipeline automation pipelines trigger`) would start it
again, so once its request may have been sent, never re-run it: follow what it started instead.
`unity commands --json` carries this table as `exitCodes`, with the error codes that pick each
exit. `6` covers every failure no more specific code names, including a `unity command` result
that reports `success: false` or a failed `status`; a missing project, an Editor that
isn't running, an unknown Editor command and a timeout have their own codes (`11` to `14`). A
request that gets no answer (offline, a failed DNS lookup, a refused connection, a dead proxy)
exits `7`; a timeout is `14`, and a reset connection or an unreadable body is
`6`. A TLS or certificate failure is `ERR_NETWORK` too, so retrying a `7` does not always help,
and a request already sent may have reached the server: retry only a command that is safe to
repeat.

<!-- exit-codes:begin (checked against the CLI's exit-code table) -->
| Code | Name | Retry | Meaning |
|---|---|---|---|
| 0 | `SUCCESS` | no | The command succeeded. |
| 1 | `ERROR` | no | An unexpected or unsupported state. |
| 2 | `BAD_ARGS` | no | A usage error: an unknown command or option, or a bad value. |
| 3 | `AUTH_ERROR` | no | Not signed in, the session expired, or permission was denied. |
| 4 | `NEEDS_CONFIG` | no | Setup is missing: a required tool, setting or link. |
| 6 | `COMMAND_FAILED` | no | The command failed, or its result reported a failure, and no more specific code names why. |
| 7 | `NETWORK_UNREACHABLE` | yes | A required service could not be reached. |
| 8 | `TESTS_FAILED` | no | The tests ran, and some failed. |
| 9 | `INSTALL_LOCKED` | yes | Another process holds the install lock. |
| 10 | `READY_TIMEOUT` | yes | The Editor did not become ready in time. |
| 11 | `NOT_FOUND` | no | What the command names does not exist: a project, a release, a module. |
| 12 | `EDITOR_UNREACHABLE` | yes | No running Editor answered: none is running, none matches, or it refused the connection. |
| 13 | `REMOTE_COMMAND_FAILED` | no | The Editor answered, but it has no command by that name. |
| 14 | `TIMEOUT` | yes | An operation ran out of time: an Editor request, an HTTP request, a batch run. |
| 15 | `RUN_FAILED` | no | A build or job the command waited on finished unsuccessfully. |
| 16 | `JOB_SUSPENDED` | yes | A job the command watched waits on a person to resume or approve it. |
| 17 | `UNAVAILABLE` | yes | The target answered but can't do this now: it is busy. |
| 18 | `AWAITING_INPUT` | yes | The target answered but waits on a person first: a dialog to dismiss, a prompt to answer. |
| 130 | `INTERRUPTED` | yes | Interrupted by Ctrl-C (SIGINT). |
| 141 | `OUTPUT_CLOSED` | no | The reader of stdout closed the pipe. |
| 143 | `TERMINATED` | yes | Terminated by SIGTERM. |
<!-- exit-codes:end -->

`8` is `unity test` only: the tests ran and some failed. A run that never completed exits `6`
and a timed-out one `14`, so CI can retry those and never blindly re-run a failing test. `9`:
earlier items of the same install may already be in place (`data.completedUids`). `10` is
`unity open --until-ready` only; the Editor is still running. `17` is an Editor that is loading,
compiling, reloading or in Safe Mode: retry later. `18` is a modal dialog blocking it: list and
answer it with `unity command editor dialog`, then retry.

