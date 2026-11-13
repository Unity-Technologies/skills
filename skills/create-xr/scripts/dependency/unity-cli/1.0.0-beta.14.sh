#!/usr/bin/env bash
# dependency/unity-cli/1.0.0-beta.14.sh — Unity CLI adapter: how `unity command` replies are framed and how failures read.
# Picked by the full `unity --version` (every release is 1.0.0-beta.N): the highest file not newer than the installed CLI.
# When a CLI release changes one of these: copy this file to <that version>.sh, fix it there, keep this one.

# The Editor is busy or reloading: the command did not run, so retrying is safe. Since beta.14 the CLI shows the
# Editor's reason (e.g. "Compiling scripts.") instead of a bare 503 or a cannot-connect error.
CLI_BUSY_PATTERN='Compiling scripts|503|Server Busy|still settling|ECONNREFUSED|Could not connect|not reachable|No Unity Editor instances'
cli_is_busy()    { printf '%s' "$1" | grep -qiE "$CLI_BUSY_PATTERN"; }
# The Editor did not finish the command: it timed out, or (since beta.14) failed at once because a modal dialog is open;
# that error names the dialog and the `unity command editor.dialog answer|dismiss` to run. Either way run_eval asks the
# Editor's status, which names the dialog (exit 3), or says the step may still be applying (exit 2).
cli_not_finished() { printf '%s' "$1" | grep -qiE 'timed out|editor\.dialog (answer|dismiss|list)'; }

# Default (TSV) reply: a header row, then <command> TAB <true|false> TAB <result json> TAB <params>.
cli_row() {   # $1 reply, $2 command name -> "<true|false><TAB><result json>"
  printf '%s\n' "$1" | awk -F'\t' -v c="${2%% *}" '$1==c{print $2"\t"$3; exit}'   # row label = first word ("script" for "script eval")
}

# Command catalog (`unity command --detail full --format json`): is it readable, and does it list a command?
cli_catalog_ok() { printf '%s' "$1" | grep -q '"name"'; }
catalog_has() {   # $1 = name as typed; reads $_CATALOG ("group leaf", "group.sub leaf" or "leaf")
  local name="$1" grp leaf g l
  case "$name" in
    *" "*) grp="${name% *}"; leaf="${name##* }" ;;
    *)     grp=""; leaf="$name" ;;
  esac
  g=$(printf '%s' "$grp" | sed 's/[.]/[.]/g'); l=$(printf '%s' "$leaf" | sed 's/[.]/[.]/g')
  if [ -z "$grp" ]; then
    printf '%s' "$_CATALOG" | grep -qE "\"$l\""; return
  fi
  # Full name in one string ("group leaf" or "group.leaf"), or group and leaf as two fields of the same entry.
  # "group_leaf" is NOT a match: that is a different command, typed as one word (e.g. editor_status).
  printf '%s' "$_CATALOG" | grep -qE "\"$g[ .]$l\"" && return 0
  printf '%s' "$_CATALOG" | grep -oE '\{[^{}]*\}' | grep -E "\"$l\"" | grep -qE "\"$g\""
}
