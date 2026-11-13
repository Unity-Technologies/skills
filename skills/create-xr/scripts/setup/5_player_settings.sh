#!/usr/bin/env bash
# Step 5 — player settings: Android IL2CPP / ARM64 / min API 29 / Vulkan; Run In Background;
# then saves the active Build Profile (player settings live in it and are lost on restart otherwise).
# Usage: bash 5_player_settings.sh [--hands] quest|pcvr|androidxr ...
source "$(dirname "$0")/_lib.sh"
require_cmds EVAL EDITOR_STATUS || exit $?
wait_ready || exit $?
use_dep unity

if $ANDROID; then
  run_eval "5/6 Android player settings" <<< "${ANDROID_PS_CS//__MINSDK__/29}"
fi
run_eval "5/6 Run In Background + save" <<< "$RUN_IN_BG_SAVE_CS"
