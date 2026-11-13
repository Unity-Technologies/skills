#!/usr/bin/env bash
# meta-graphics-setup.sh — Quest graphics: single-pass instanced stereo, foveated rendering,
# Quest URP asset + renderer, and Quest_RPAsset on the "Meta Quest (Build Profile)" quality level.
#
# Each step's C# is sent to the Editor with `unity command eval`; nothing is written to Assets/Editor.
# Prints "OK [n/5 ...]" or "FAILED [n/5 ...]" per step and stops at the first failure.
# Every step is idempotent: fix the cause, then re-run this script.
# Requires the setup procedure (scripts/setup/setup.sh) first (OpenXR loader on the Android tab, Vulkan as the Android graphics API).
# Usage: bash meta-graphics-setup.sh
# Exit codes: 1 failed, 2 timed out, 3 blocked by an Editor dialog, 4 Pipeline command not in the catalog.
# Step 3 creates Quest_RPAsset and Quest_Renderer fresh (public API), not copied from the project's current asset: a copy
# of the PC asset brought an SSAO feature and GPU Resident Drawer, and either one renders a Quest build black.
set -u
source "$(dirname "$0")/../_eval.sh"
source "$(dirname "$0")/check.sh"
wait_ready || exit $?
use_dep unity
use_dep com.unity.xr.openxr

run_eval "1/5 single-pass instanced stereo" <<< "$SPI_CS"

run_eval "2/5 foveated rendering + performance settings" <<< "$VULKAN_REQUIRED_CS
$FOVEATED_CS"

run_eval "3/5 Quest URP asset + renderer" <<< "$URP_COPY_CS"

run_eval "4/5 Quest URP settings" <<< "$URP_SETTINGS_CS"

run_eval "5/5 Meta Quest quality level uses Quest_RPAsset" <<< "$QUALITY_LEVELS_CS"

run_eval "verify" <<< "$(graphics_check_code)"
echo "=== Quest graphics setup complete ==="
