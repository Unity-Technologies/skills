#!/usr/bin/env bash
# Step 3 — build profiles: creates missing profiles, switches the build target, activates the primary device's profile,
# syncs the selected build target group.
# Usage: bash 3_build_profile.sh [--hands] quest|pcvr|androidxr ...   (first device = primary)
source "$(dirname "$0")/_lib.sh"
require_cmds EVAL EDITOR_STATUS BUILD_LIST_PLATFORMS BUILD_LIST_PROFILES BUILD_CREATE_PROFILE TARGET_SWITCH TARGET_SWITCH_STATUS || exit $?
wait_ready || exit $?
resolve_platforms "3/6 profiles" quiet
use_dep unity

list_profiles() { pl_profiles "$(unity command $CMD_BUILD_LIST_PROFILES 2>&1)"; }
profile_entry() { pl_where "$(list_profiles)" name "\"$1\"" | head -1; }
is_active() { pl_is_true "$(profile_entry "$1")" isActive; }

# create_profile <device> <true|false>: CLI create (and optionally activate). No --confirm, so it never
# overwrites an existing profile.
create_profile() {
  local d="$1" act="$2" name out
  name=$(plat PNAME "$d")
  wait_ready || exit $?
  out=$(unity command $CMD_BUILD_CREATE_PROFILE --name "$name" --platform "$(plat PID "$d")" --activate "$act" 2>&1)
  if ! pl_success "$out"; then
    echo "FAILED [3/6 profile '$name']: could not create the profile ($CMD_BUILD_CREATE_PROFILE): $(printf '%s' "$out" | tail -2 | tr '\n' ' ' | cut -c1-300)"
    echo "  If this Pipeline version has no create command, ask the user to add it in File > Build Profiles, then re-run."
    exit 1
  fi
  echo "OK [3/6 profile '$name']: created$([ "$act" = true ] && echo " and activated") $(pl_field "$out" assetPath)"
}

# switch_target <BuildTarget>: target switch + bounded poll of its status command. Pipeline refuses to
# activate a profile that would change the build target, so the switch happens first, on its own.
switch_target() {
  local tgt="$1" out st s=0 state
  wait_ready || exit $?
  out=$(unity command $CMD_TARGET_SWITCH --target "$tgt" --confirm true 2>&1)
  case "$(pl_switch_state "$out")" in
    done) echo "SKIP [3/6 build target]: already $tgt"; return ;;
    switching) ;;
    *) echo "FAILED [3/6 build target]: switch to $tgt refused: $(pl_message "$out")"; exit 1 ;;
  esac
  echo "  Switching the build target to $tgt: full reimport + domain reload (several minutes the first time). Waiting..."
  while :; do
    sleep 10; s=$((s + 10))
    st=$(unity command $CMD_TARGET_SWITCH_STATUS 2>&1)   # fails while the Editor reloads; keep polling
    case "$(pl_switch_state "$st")" in
      done) break ;;
      failed)
        # Pipeline marks the switch failed if any domain reload (e.g. from a package install just before) lands
        # while it runs, even when the switch completes. Retry once: a finished switch reports "already <target>".
        if [ -z "${_SWITCH_RETRY:-}" ]; then _SWITCH_RETRY=1; switch_target "$tgt"; return; fi
        echo "FAILED [3/6 build target]: switch to $tgt failed: $(pl_message "$st")"; exit 1 ;;
    esac
    state=$(editor_state)
    if [ "${state#blocked:}" != "$state" ]; then dialog_message "3/6 build target" "${state#blocked:}"; exit 3; fi
    if [ "$s" -ge "${SWITCH_TIMEOUT:-1800}" ]; then
      echo "FAILED [3/6 build target]: switch to $tgt not finished after ${SWITCH_TIMEOUT:-1800}s. Wait, then re-run."
      exit 2
    fi
  done
  wait_ready 900 || exit $?
  echo "OK [3/6 build target]: switched to $tgt"
}


# Secondary profiles: create without activating.
done_names=" $primary "
for d in "${DEVICES[@]}"; do
  name=$(plat PNAME "$d")
  case "$done_names" in *" $name "*) continue;; esac   # quest + androidxr on 6000.0 would both map to Android
  done_names="$done_names$name "
  if [ -n "$(profile_entry "$name")" ]; then echo "SKIP [3/6 profile '$name']: already exists"; continue; fi
  create_profile "$d" false
done

# Primary profile: switch the build target first (Pipeline refuses to switch it during activation),
# then create + activate in one CLI call when it is new.
activated_by=""
if is_active "$primary"; then
  echo "SKIP [3/6 activate '$primary']: already active"
  activated_by="already"
else
  switch_target "$(plat PBT "$PRIMARY")"
  if [ -z "$(profile_entry "$primary")" ]; then
    create_profile "$PRIMARY" true
    activated_by="cli"
  fi
fi
if [ "$activated_by" = cli ]; then
  echo "  Activating can trigger a long reimport (minutes on the first Android switch). Waiting..."
  wait_ready 900 || exit $?
  is_active "$primary" || { echo "WARN [3/6 activate '$primary']: created but not active yet; activating the existing profile"; activated_by=""; }
fi

# Existing (or not-yet-active) primary profile: Pipeline has no command that activates an existing profile
# (create_profile --activate only works on create; set_profile only edits scenes/defines), so use the eval recipe.
if [ -z "$activated_by" ]; then
  guid=$(field "$(profile_entry "$primary")" guid)
  [ -n "$guid" ] || { echo "FAILED [3/6 activate '$primary']: profile not found in $CMD_BUILD_LIST_PROFILES"; exit 1; }
  wait_ready || exit $?
  run_eval "3/6 activate existing '$primary' (eval; no CLI command for existing profiles)" <<< "${ACTIVATE_PROFILE_CS//__GUID__/$guid}"
  echo "  Activating can trigger a long reimport (minutes on the first Android switch). Waiting..."
  wait_ready 900 || exit $?
  is_active "$primary" || { echo "FAILED [3/6 activate '$primary']: profile is not active after activation"; exit 1; }
fi

if [ "$activated_by" != already ]; then
  echo "OK [3/6 activate '$primary']: active ($([ "$activated_by" = cli ] && echo "$CMD_BUILD_CREATE_PROFILE --activate true" || echo "eval"))"
  run_eval "3/6 sync selected build target group" <<< "$SYNC_TARGET_GROUP_CS"
fi
