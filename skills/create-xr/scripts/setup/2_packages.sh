#!/usr/bin/env bash
# Step 2 — packages: Active Input Handling -> Both (before the Input System arrives), then the base and device packages.
# Usage: bash 2_packages.sh [--hands] quest|pcvr|androidxr ...
source "$(dirname "$0")/_lib.sh"
require_cmds EVAL EDITOR_STATUS PACKAGE_LIST PACKAGE_ADD PACKAGE_STATUS || exit $?
wait_ready || exit $?
use_dep unity

# Active Input Handling first: installing the Input System while handling is "Input Manager (old)" opens a
# blocking native dialog. Set Both before any package arrives.
run_eval "2/6 Active Input Handling" <<< "$ACTIVE_INPUT_CS"


n=0
for p in "${PKGS[@]}"; do
  n=$((n + 1))
  install_package "$p" "2/6 package $n/${#PKGS[@]}" || exit $?
done
