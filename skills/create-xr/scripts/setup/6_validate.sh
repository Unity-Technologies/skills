#!/usr/bin/env bash
# Step 6 — validation (read-only): every expected package installed; primary profile active; OpenXR loader and required
# features on each tab; Android player settings; Run In Background. Same check as status.sh (check.sh).
# Usage: bash 6_validate.sh [--hands] quest|pcvr|androidxr ...
source "$(dirname "$0")/_lib.sh"
source "$(dirname "$0")/check.sh"
require_cmds EVAL EDITOR_STATUS PACKAGE_LIST BUILD_LIST_PLATFORMS || exit $?
wait_ready || exit $?
resolve_platforms "6/6 validate" quiet
use_dep unity
use_dep com.unity.xr.management
use_dep com.unity.xr.openxr
miss=$(missing_packages "$_PKG_LIST")   # the list use_dep already fetched
[ -z "$miss" ] || { echo "FAILED [6/6 packages]: not installed: $miss"; exit 1; }
echo "OK [6/6 packages]: ${PKGS[*]}"
run_eval "6/6 validate" <<< "$(setup_check_code)"
