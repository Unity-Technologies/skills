#!/usr/bin/env bash
# status.sh — read-only: is each procedure already done? Changes nothing.
# Usage: bash status.sh [--hands] quest|pcvr|androidxr ...   (same arguments as scripts/setup/setup.sh)
# Prints one line per procedure:
#   setup:          done (...) | todo (...)
#   quest-graphics: done (...) | todo (...)           (only when quest is a target)
#   rig:            present (...) | not present (...)
#   console:        compile ok | compile failed | unknown   (for the final step)
# A line can also read "unknown (...)" if the check itself failed — then run that procedure; it skips finished work.
# Also checks that every Pipeline command the skill's scripts need is in the Editor's catalog (exit 4 names a missing one).
# Exit codes: 0 checked, 2 timed out, 3 blocked by an Editor dialog, 4 Pipeline command not in the catalog.
D="$(dirname "$0")"
source "$D/setup/_lib.sh"          # arguments, PKGS, platforms
source "$D/setup/check.sh"         # missing_packages, setup_check_code
source "$D/meta-graphics/check.sh"   # graphics_check_code

require_cmds EVAL EDITOR_STATUS PACKAGE_LIST PACKAGE_ADD PACKAGE_STATUS \
             BUILD_LIST_PLATFORMS BUILD_LIST_PROFILES BUILD_CREATE_PROFILE TARGET_SWITCH TARGET_SWITCH_STATUS CONSOLE_STATUS || exit $?
wait_ready || exit $?
[ -n "$_PKG_LIST" ] || _PKG_LIST=$(package_list)   # one list for the version lookups and the package check
use_dep unity
use_dep com.unity.xr.management
use_dep com.unity.xr.openxr

show() {   # $1 label, $2 check result, $3 word for "done", $4 word for "not done"
  case "$2" in
    done:*)                       printf '%-15s %s (%s)\n' "$1:" "${3:-done}" "${2#done: }" ;;
    ERROR:*)                      printf '%-15s %s (%s)\n' "$1:" "${4:-todo}" "${2#ERROR: }" ;;
    EVAL_FAILED:*|EVAL_NOT_FINISHED:*) printf '%-15s unknown (%s)\n' "$1:" "$(printf '%s' "$2" | cut -c1-200)" ;;
    *)                            printf '%-15s %s\n' "$1:" "$2" ;;
  esac
}
wrap() { printf 'System.Func<string> %s = () => {\n%s\n};\n' "$1" "$2"; }   # a check as a lambda, so several share one eval

miss=$(missing_packages "$_PKG_LIST")
modules=true; resolve_platforms status soft || modules=false
want_setup=false; want_gfx=false
if ! $modules; then setup_line="setup:          todo (build support missing for a target device)"
elif [ -n "$miss" ]; then setup_line="setup:          todo (packages not installed: $miss)"
else want_setup=true; fi
if has quest; then
  case " $miss " in *" com.unity.xr.openxr "*) gfx_line="quest-graphics: todo (OpenXR package not installed)" ;; *) want_gfx=true ;; esac
fi

# One eval for every check that applies (each Editor call costs seconds). If it fails to compile — e.g. a check
# references a type this project lacks — fall back to one eval per check, so only that line reads "unknown".
code=""; ret='""'
$want_setup && { code="$code$(wrap __setup "$(setup_check_code)")"; ret="$ret + __setup() + \"|||\""; }
$want_gfx   && { code="$code$(wrap __gfx "$(graphics_check_code)")";       ret="$ret + __gfx() + \"|||\""; }
code="$code$(wrap __rig "$RIG_CHECK_CS")"; ret="$ret + __rig()"
all=$(eval_result "$code
return $ret;")
case "$all" in
  EVAL_FAILED:*|EVAL_NOT_FINISHED:*)
    $want_setup && r_setup=$(eval_result "$(setup_check_code)")
    $want_gfx   && r_gfx=$(eval_result "$(graphics_check_code)")
    r_rig=$(eval_result "$RIG_CHECK_CS") ;;
  *)
    rest="$all"
    $want_setup && { r_setup="${rest%%|||*}"; rest="${rest#*|||}"; }
    $want_gfx   && { r_gfx="${rest%%|||*}";   rest="${rest#*|||}"; }
    r_rig="$rest" ;;
esac

if $want_setup; then show setup "$r_setup" done todo; else echo "$setup_line"; fi
if has quest; then if $want_gfx; then show quest-graphics "$r_gfx" done todo; else echo "$gfx_line"; fi; fi
show rig "$r_rig"
if [ -n "${CMD_CONSOLE_STATUS:-}" ]; then
  printf '%-15s %s\n' "console:" "$(pl_console_state "$(unity command $CMD_CONSOLE_STATUS --format json 2>&1)")"
else
  printf '%-15s %s\n' "console:" "unknown (no console status command)"
fi
