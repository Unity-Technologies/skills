#!/usr/bin/env bash
# setup.sh — base XR setup: runs steps 1–6 in order and stops at the first failure.
# Usage: bash setup.sh [--hands] quest|pcvr|androidxr ...   (first device = primary)
# Each step can also be run on its own with the same arguments, e.g. bash 4_openxr.sh quest
# Prints OK / SKIP / WARN / FAILED per step. Every step is idempotent: fix the cause, then re-run.
# Exit codes: 1 failed, 2 timed out, 3 blocked by an Editor dialog, 4 Pipeline command not in the catalog.
# Installs the XR Interaction Toolkit (and XR Hands with --hands); the rig procedure imports the samples. Apple Vision Pro: not covered.
source "$(dirname "$0")/_lib.sh"
# Resolve every command once and pass the names to the steps, so the catalog is read once per run.
require_cmds EVAL EDITOR_STATUS PACKAGE_LIST PACKAGE_ADD PACKAGE_STATUS \
             BUILD_LIST_PLATFORMS BUILD_LIST_PROFILES BUILD_CREATE_PROFILE TARGET_SWITCH TARGET_SWITCH_STATUS || exit $?
for v in ${!CMD_@}; do export "$v"; done
wait_ready || exit $?
dep_installed unity >/dev/null   # resolve the Unity version once; the steps inherit it
for step in 1_modules 2_packages 3_build_profile 4_openxr 5_player_settings 6_validate; do
  bash "$(dirname "$0")/$step.sh" "$@" || exit $?
done
echo "=== XR base setup complete ==="
