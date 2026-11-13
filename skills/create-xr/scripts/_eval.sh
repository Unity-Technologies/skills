#!/usr/bin/env bash
# _eval.sh — shared helpers, sourced by the skill's scripts. Not meant to be run directly.
#   require_cmds <KEY>...        resolve Pipeline command names against the Editor's catalog (see below)
#   eval_result <code>           print the eval's return value, or EVAL_FAILED:/EVAL_NOT_FINISHED: + details
#   run_eval <label>             C# body on stdin; prints "OK [label]: ..." or "FAILED [label]: ..." and exits on failure
#   editor_state                 ready | busy | blocked:<dialog title> | unknown
#   wait_ready [secs]            wait until the Editor accepts commands; stops if a dialog blocks it
#   install_package <id> [label] skip if installed, else add + bounded poll + wait for the reload
#   use_dep <component>          source scripts/dependency/<component>/<version>.sh for the installed version
# What depends on the CLI or Pipeline version (busy/timeout/dialog text, reply framing, command names, result fields)
# comes from the adapters in scripts/dependency/unity-cli and com.unity.pipeline; this file holds only the logic.
#
# Exit/return codes: 1 = step failed, 2 = not finished: timed out (may still be applying), 3 = blocked by an Editor dialog,
#                    4 = a Pipeline command this script needs is not in the Editor's catalog.
export PATH="$PATH:${UNITY_BIN:-$HOME/AppData/Local/Unity/bin}"

# Keep the scripts Bash 3.2 compatible (the version macOS ships): no associative arrays, no sort -V (use ver_sort).

# The parsers below read the CLI's default reply format; a user's UNITY_FORMAT would change it. Calls that need JSON ask for it.
unset UNITY_FORMAT

# Every `unity command` must reach the same project. The CLI resolves it per call from UNITY_PROJECT_PATH or the current
# directory, so with several Editors open an unset path could reach another project. Pin it once; children inherit it.
if [ -z "${UNITY_PROJECT_PATH:-}" ]; then
  if [ -f ProjectSettings/ProjectVersion.txt ]; then
    UNITY_PROJECT_PATH="$(pwd -W 2>/dev/null || pwd)"; export UNITY_PROJECT_PATH   # pwd -W: Windows path under Git Bash
  else
    echo "FAILED [project]: run the script from the Unity project folder, or set UNITY_PROJECT_PATH=<project path>." >&2; exit 1
  fi
fi

# ---------------------------------------------------------------- versioned adapters (scripts/dependency/)
# Everything that changes with a version lives in scripts/dependency/<component>/<version>.sh, sourced from here:
#   unity-cli/<full version>.sh           how `unity command` replies are framed, busy/timeout/dialog text (picked by `unity --version`)
#   com.unity.pipeline/<major.minor>.sh   command names and result fields (picked by the project's Pipeline version)
#   unity/, com.unity.xr.*/<major.minor>.sh   C# for eval (use_dep, below)
# Each component's highest file that is not newer than the installed version wins; versions are resolved once per run.
SKILL_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
_PKG_LIST=""   # package list fetched once per script; set it yourself to reuse a list you already have
dep_var() { printf 'DEPVER_%s' "$(printf '%s' "$1" | tr -c 'A-Za-z0-9' '_')"; }
ver_sort() {   # stdin: one version per line -> oldest first, numbers compared as numbers (what sort -V does)
  awk '{ k = ""; s = $0
         while (match(s, /[0-9]+/)) { k = k substr(s, 1, RSTART - 1) sprintf("%012d", substr(s, RSTART, RLENGTH)); s = substr(s, RSTART + RLENGTH) }
         print k s "\t" $0 }' | LC_ALL=C sort | cut -f2-
}
dep_pick() {   # $1 component, $2 installed version, $3 minor|full -> sets _DEP_FILE (oldest file + WARN if all are newer)
  local dir="$SKILL_ROOT/scripts/dependency/$1" key f pick="" first=""
  if [ "$3" = full ]; then key="$2"; else key=$(printf '%s' "$2" | grep -oE '^[0-9]+\.[0-9]+'); fi
  for f in $(command ls "$dir" 2>/dev/null | sed -n 's/\.sh$//p' | ver_sort); do   # `command`: skip aliases (Git Bash: ls -F appends *)
    [ -n "$first" ] || first="$f"
    if [ -z "$key" ] || [ "$(printf '%s\n%s\n' "$f" "$key" | ver_sort | head -1)" = "$f" ]; then pick="$f"; fi
  done
  [ -n "$first" ] || { echo "FAILED [dependency]: no version files in $dir"; exit 1; }
  if [ -z "$pick" ]; then pick="$first"; echo "WARN [dependency]: $1 $2 is older than every version file; using $first.sh" >&2; fi
  _DEP_FILE="$dir/$pick.sh"
}

# Unity CLI: `unity --version`, matched on the full version (every release is 1.0.0-beta.N).
if [ -z "${DEPVER_unity_cli:-}" ]; then
  DEPVER_unity_cli=$(unity --version 2>/dev/null | grep -oE '[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.]+)?' | head -1); export DEPVER_unity_cli
fi
dep_pick unity-cli "$DEPVER_unity_cli" full; source "$_DEP_FILE"
# Pipeline: start with the newest adapter; require_cmds switches to the project's version on its first call
# (reading that version already needs a Pipeline command).
dep_pick com.unity.pipeline "" minor; source "$_DEP_FILE"
_PL_ADAPTER=""
pipeline_adapter() {   # retries while the Editor is busy; marked done only once the version is known
  [ -n "$_PL_ADAPTER" ] && return 0
  local out tries=0
  if [ -z "${DEPVER_com_unity_pipeline:-}" ]; then
    # No parameters: their names differ between Pipeline versions, and the version isn't known yet.
    # com.unity.pipeline is a direct dependency, so the default (installed) list includes it.
    while :; do
      out=$(unity command ${CAND_PACKAGE_LIST%%|*} 2>&1)
      DEPVER_com_unity_pipeline=$(pl_package_version "$out" com.unity.pipeline)
      if [ -z "$DEPVER_com_unity_pipeline" ] && cli_is_busy "$out"; then
        tries=$((tries + 1)); [ "$tries" -lt 60 ] && { sleep 5; continue; }
      fi
      break
    done
    # Still unknown (Editor unreachable): keep the newest adapter for now and try again on the next call.
    [ -n "$DEPVER_com_unity_pipeline" ] || return 0
    export DEPVER_com_unity_pipeline
  fi
  dep_pick com.unity.pipeline "$DEPVER_com_unity_pipeline" minor; source "$_DEP_FILE"
  _PL_ADAPTER=1
}

_CATALOG=""

load_catalog() {   # once per script run; retries while the Editor is busy or reloading
  [ -n "$_CATALOG" ] && return 0
  local out tries=0
  while :; do
    out=$(unity command --detail full --format json 2>&1)
    if cli_is_busy "$out"; then
      tries=$((tries + 1)); [ "$tries" -lt 60 ] && { sleep 5; continue; }
    fi
    break
  done
  cli_catalog_ok "$out" || return 1
  _CATALOG="$out"
}


require_cmds() {
  local key var override cands c found missing="" IFS_SAVE
  pipeline_adapter
  for key in "$@"; do
    var="CMD_$key"; cands="CAND_$key"; cands="${!cands:-}"
    [ -n "${!var:-}" ] && continue   # already resolved in this run
    override="PIPELINE_CMD_$key"
    if [ -n "${!override:-}" ]; then printf -v "$var" '%s' "${!override}"; continue; fi
    if ! load_catalog; then
      # Catalog unreadable (very old Pipeline, or Editor unreachable): use the first known name, unverified.
      [ -n "${_CATALOG_WARNED:-}" ] || echo "WARN [commands]: could not read the Editor's command catalog; using the known command names unverified"
      _CATALOG_WARNED=1
      printf -v "$var" '%s' "${cands%%|*}"; continue
    fi
    found=""
    IFS_SAVE="$IFS"; IFS='|'
    for c in $cands; do
      if catalog_has "$c"; then found="$c"; break; fi
    done
    IFS="$IFS_SAVE"
    if [ -n "$found" ]; then printf -v "$var" '%s' "$found"
    elif [ "${OPTIONAL_CMDS#* $key }" = "$OPTIONAL_CMDS" ]; then missing="$missing $key"
    fi
  done
  [ -z "$missing" ] && return 0
  for key in $missing; do
    cands="CAND_$key"; cands="${!cands:-}"
    echo "FAILED [commands]: no Pipeline command for $key in the Editor's catalog (known names: ${cands//|/, })."
    echo "  Look up its current name with: unity command --query ${cands##* }"
    echo "  If one exists, re-run with PIPELINE_CMD_$key=\"<name>\". If none exists, report this line to the user."
  done
  return 4
}

# ---------------------------------------------------------------- eval
eval_result() {
  local code out row ok json tries=0
  # Drop full-line // comments, join lines (code is sent as a single argument).
  code=$(printf '%s\n' "$1" | sed -e 's#^[[:space:]]*//.*$##' | tr '\r\n' '  ')
  while :; do
    out=$(unity command $CMD_EVAL --timeout 120 --code "$code" 2>&1)
    # Editor busy or reloading: the command was not executed, so retrying is safe.
    if cli_is_busy "$out"; then
      tries=$((tries + 1))
      if [ "$tries" -lt 60 ]; then sleep 5; continue; fi
    fi
    break
  done
  if cli_not_finished "$out"; then
    echo "EVAL_NOT_FINISHED: $(printf '%s' "$out" | tail -2 | tr '\n' ' ')"
    return
  fi
  row=$(cli_row "$out" "$CMD_EVAL")
  ok=${row%%$'\t'*}
  json=${row#*$'\t'}
  if [ "$ok" != "true" ] || ! pl_success "$json"; then
    echo "EVAL_FAILED: $(printf '%s' "$out" | tail -3 | tr '\n' ' ' | cut -c1-600)"
    return
  fi
  pl_result "$json"
}

dialog_message() {
  echo "FAILED [$1]: the Editor is blocked by a dialog: \"$2\"."
  echo "  Ask the user to answer it in the Editor. Do not click it for them (no UI automation or simulated input). Then re-run this script."
}

run_eval() {
  local label="$1" r st rc
  require_cmds EVAL EDITOR_STATUS; rc=$?; [ "$rc" -eq 0 ] || exit "$rc"
  r=$(eval_result "$(cat)")
  case "$r" in
    EVAL_NOT_FINISHED:*)
      st=$(editor_state)
      if [ "${st#blocked:}" != "$st" ]; then dialog_message "$label" "${st#blocked:}"; exit 3; fi
      echo "FAILED [$label]: $r"
      echo "  The Editor may still be applying this step. Wait ~10 s, then re-run this script (every step is idempotent)."
      exit 2 ;;
    EVAL_FAILED:*|ERROR:*)
      echo "FAILED [$label]: $r"
      exit 1 ;;
  esac
  echo "OK [$label]: $r"
}

# ---------------------------------------------------------------- Editor state
editor_state() {   # needs require_cmds EDITOR_STATUS first (wait_ready and run_eval do it)
  [ -n "${CMD_EDITOR_STATUS:-}" ] || { echo unknown; return; }
  pl_editor_state "$(unity command $CMD_EDITOR_STATUS --format json 2>&1)"   # unknown: unreachable (reloading) or no status command
}

wait_ready() {
  local t="${1:-300}" s=0 st rc row
  require_cmds EVAL EDITOR_STATUS; rc=$?; [ "$rc" -eq 0 ] || return "$rc"
  while :; do
    st=$(editor_state)
    case "$st" in
      ready) return 0 ;;
      blocked:*) dialog_message "wait" "${st#blocked:}"; return 3 ;;
      unknown)  # fallback probe for Pipeline versions without a status command
        row=$(cli_row "$(unity command $CMD_EVAL --code 'return "ready";' 2>/dev/null)" "$CMD_EVAL")
        [ "$(pl_result "${row#*$'\t'}")" = ready ] && return 0 ;;
    esac
    sleep 5; s=$((s + 5))
    if [ "$s" -ge "$t" ]; then echo "FAILED [wait]: Editor not ready after ${t}s (state: $st)"; return 2; fi
  done
}

# ---------------------------------------------------------------- packages
package_list() {   # needs require_cmds PACKAGE_LIST first; includes indirect dependencies.
  # Retries only while the list is empty (Editor busy); a package missing from a full list is simply not installed.
  local out i
  for i in 1 2 3 4 5 6; do
    out=$(unity command $CMD_PACKAGE_LIST $ARG_INCLUDE_INDIRECT true 2>&1)
    pl_has_packages "$out" && { printf '%s' "$out"; return 0; }
    sleep 5
  done
  return 1
}

package_version() {   # prints the installed version of $1, or nothing
  pl_package_version "$(package_list)" "$1"
}

install_package() {
  local id="$1" label="${2:-$1}" out st entry s=0 ver rc state
  require_cmds PACKAGE_LIST PACKAGE_ADD PACKAGE_STATUS; rc=$?; [ "$rc" -eq 0 ] || return "$rc"
  ver=$(package_version "$id")
  if [ -n "$ver" ]; then echo "SKIP [$label]: $id $ver already installed"; return 0; fi
  wait_ready; rc=$?; [ "$rc" -eq 0 ] || return "$rc"
  out=$(unity command $CMD_PACKAGE_ADD $ARG_PACKAGE_ADD_NAME "$id" --confirm true 2>&1)
  if ! pl_success "$out"; then
    echo "FAILED [$label]: '$CMD_PACKAGE_ADD' did not start: $(printf '%s' "$out" | tail -2 | tr '\n' ' ' | cut -c1-300)"
    echo "  If a parameter does not exist, this Pipeline version changed it: check it with 'unity command --query ${CMD_PACKAGE_ADD##* } --detail full' and report it to the user."
    return 1
  fi
  while :; do
    sleep 5; s=$((s + 5))
    st=$(unity command $CMD_PACKAGE_STATUS 2>&1)
    entry=$(pl_package_op "$st" "$id")   # only this package's operation; others may be earlier, finished ones
    if [ -n "$entry" ]; then
      case "$(pl_op_status "$entry")" in
        completed) break ;;
        failed) echo "FAILED [$label]: install failed: $(pl_op_error "$entry")"; return 1 ;;
      esac
    fi
    state=$(editor_state)
    if [ "${state#blocked:}" != "$state" ]; then dialog_message "$label" "${state#blocked:}"; return 3; fi
    if [ "$s" -ge "${PKG_TIMEOUT:-300}" ]; then
      echo "FAILED [$label]: install not finished after ${PKG_TIMEOUT:-300}s (last: $(pl_op_status "$entry"))"
      return 2
    fi
  done
  wait_ready 600; rc=$?; [ "$rc" -eq 0 ] || return "$rc"
  # The install can report complete before the Editor registers the package: recheck for up to 60 s.
  for s in 1 2 3 4 5 6 7 8 9 10 11 12; do
    ver=$(package_version "$id"); [ -n "$ver" ] && break
    sleep 5; wait_ready || return $?
  done
  if [ -z "$ver" ]; then echo "FAILED [$label]: install reported complete, but $id is not in the package list"; return 1; fi
  # The Package Manager's sample list can miss packages installed in this session (seen on Unity 7 alphas), so a
  # later sample import finds nothing. Resolving refreshes it.
  eval_result 'UnityEditor.PackageManager.Client.Resolve(); return "ok";' >/dev/null
  wait_ready || return $?
  echo "OK [$label]: $id $ver"
}

# ---------------------------------------------------------------- versioned C# (scripts/dependency/)
# use_dep <component>: source the C# file for the installed Unity (unity) or package version, matched on major.minor;
# the newest file if the package is not installed yet.
dep_resolve() {   # $1 component -> sets DEPVER_<component> (exported, so the steps setup.sh starts inherit it)
  local var v; var=$(dep_var "$1")
  [ -n "${!var:-}" ] && return 0
  if [ "$1" = unity ]; then
    require_cmds EVAL >/dev/null || return; v=$(eval_result 'return UnityEngine.Application.unityVersion;'); case "$v" in EVAL_*) v="" ;; esac
  else
    require_cmds PACKAGE_LIST >/dev/null || return
    [ -n "$_PKG_LIST" ] || _PKG_LIST=$(package_list)
    v=$(pl_package_version "$_PKG_LIST" "$1")
  fi
  printf -v "$var" '%s' "$v"; export "$var"
}
dep_installed() { local var; dep_resolve "$1"; var=$(dep_var "$1"); printf '%s' "${!var}"; }
use_dep() {   # $1 component
  local var; dep_resolve "$1"; var=$(dep_var "$1")
  dep_pick "$1" "${!var:-}" minor; source "$_DEP_FILE"
}
