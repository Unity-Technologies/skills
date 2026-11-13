#!/usr/bin/env bash
# Step 4 — OpenXR: enables the OpenXR loader and the device's features/interaction profiles on each target tab
# (Android: Quest, Android XR; Standalone: PC VR, Quest Link).
# Usage: bash 4_openxr.sh [--hands] quest|pcvr|androidxr ...
source "$(dirname "$0")/_lib.sh"
require_cmds EVAL EDITOR_STATUS || exit $?

q() { local out="" x; for x in "$@"; do out="$out${out:+, }\"$x\""; done; printf '%s' "$out"; }
openxr_tab() {   # $1 group, $2 label, $3 wanted features (space-separated), $4 required features
  local code="${LOADER_CS//__GROUP__/$1}"; code="${code//__LOADER_TYPE__/$OXR_LOADER_TYPE}"; code="${code//__LOADER__/$OXR_LOADER}"
  run_eval "4/6 OpenXR $2: loader" <<< "$code"
  code="${FEATURES_CS//__GROUP__/$1}"; code="${code//__WANT__/$(q $3)}"; code="${code//__REQUIRED__/$(q $4)}"
  run_eval "4/6 OpenXR $2: features" <<< "$code"
}
wait_ready || exit $?
use_dep com.unity.xr.management
load_feature_names                # feature class names from the OpenXR / Android XR adapters
if $ANDROID; then
  openxr_tab Android "Android tab" "$WANT_ANDROID" "$REQ_ANDROID"
fi
if $STANDALONE; then
  openxr_tab Standalone "Standalone tab" "$WANT_STANDALONE" "$REQ_STANDALONE"
fi
