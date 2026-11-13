#!/usr/bin/env bash
# Step 1 — platform modules: checks build support is installed for every target device. Changes nothing.
# Usage: bash 1_modules.sh [--hands] quest|pcvr|androidxr ...
source "$(dirname "$0")/_lib.sh"
wait_ready || exit $?
resolve_platforms "1/6 modules"
