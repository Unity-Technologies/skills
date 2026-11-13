#!/usr/bin/env bash
# check.sh — setup's read-only done check. Sourced after _lib.sh by 6_validate.sh and scripts/status.sh.
#   missing_packages     prints the expected packages (PKGS) that are not installed, space-separated
#   setup_check_code     C#: "done: <summary>" or "ERROR: <what is not done>", built from scripts/dependency/ fragments

missing_packages() {   # [package list]; fetches one (retrying only while it is empty) when not given
  local out="${1:-}" p miss=""
  require_cmds PACKAGE_LIST || return $?
  [ -n "$out" ] || out=$(package_list)
  for p in "${PKGS[@]}"; do [ -n "$(pl_package_version "$out" "$p")" ] || miss="$miss $p"; done
  printf '%s' "${miss# }"
}

setup_check_code() {   # needs _lib.sh variables and use_dep unity, com.unity.xr.management, com.unity.xr.openxr
  local groups="" reqs="" minsdk=29 code
  load_feature_names
  if $ANDROID; then groups="UnityEditor.BuildTargetGroup.Android"; reqs="\"${REQ_ANDROID// /,}\""; fi
  if $STANDALONE; then groups="$groups${groups:+, }UnityEditor.BuildTargetGroup.Standalone"; reqs="$reqs${reqs:+, }\"${REQ_STANDALONE// /,}\""; fi
  code="var todo = new System.Collections.Generic.List<string>();
var r = \"\";
{
${SETUP_CHECK_CS//__PRIMARY__/$primary}
}
{
${LOADER_CHECK_CS//__GROUPS__/$groups}
}
{
${FEATURES_CHECK_CS//__GROUPS__/$groups}
}"
  code="${code//__REQS__/$reqs}"; code="${code//__LOADER__/$OXR_LOADER}"   # loader class name: OpenXR adapter
  $ANDROID && code="$code
{
${ANDROID_PS_CHECK_CS//__MINSDK__/$minsdk}
}"
  printf '%s\n%s\n' "$code" 'return todo.Count == 0 ? "done: " + r : "ERROR: " + string.Join("; ", todo);'
}
