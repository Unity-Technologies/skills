#!/usr/bin/env bash
# _lib.sh — sourced by every setup step. Parses the shared arguments and resolves each device's build platform.
#   args:    [--hands] quest|pcvr|androidxr ...   (first device = primary; its build profile is activated)
#   sets:    DEVICES, PRIMARY, HANDS, ANDROID, STANDALONE, PKGS, PID_/PNAME_/PBT_<device>, primary (profile name)
#   has <device>, plat <PID|PNAME|PBT> <device>, field <json> <key>, resolve_platforms <label>
set -u
source "$(dirname "${BASH_SOURCE[0]}")/../_eval.sh"

HANDS=false; DEVICES=()
for a in "$@"; do
  case "$a" in
    --hands) HANDS=true ;;
    quest|pcvr|androidxr) DEVICES+=("$a") ;;
    avp|visionos|visionpro) echo "FAILED [args]: this skill doesn't cover Apple Vision Pro. Tell the user."; exit 1 ;;
    *) echo "FAILED [args]: unknown argument '$a'. Usage: bash $(basename "$0") [--hands] quest|pcvr|androidxr ..."; exit 1 ;;
  esac
done
[ "${#DEVICES[@]}" -gt 0 ] || { echo "FAILED [args]: name at least one device: quest, pcvr, androidxr"; exit 1; }
has() { local d; for d in "${DEVICES[@]}"; do [ "$d" = "$1" ] && return 0; done; return 1; }
PRIMARY="${DEVICES[0]}"
ANDROID=false; has quest && ANDROID=true; has androidxr && ANDROID=true
STANDALONE=false; has quest && STANDALONE=true; has pcvr && STANDALONE=true   # Quest: Standalone tab for Link

PKGS=(com.unity.xr.management com.unity.xr.openxr com.unity.inputsystem com.unity.xr.interaction.toolkit)
if has quest; then PKGS+=(com.unity.xr.meta-openxr); fi        # Unity: OpenXR Meta, as in Unity's VR template
if has androidxr; then PKGS+=(com.unity.xr.androidxr-openxr); fi   # brings AR Foundation as a dependency
if $HANDS; then PKGS+=(com.unity.xr.hands); fi   # hand tracking is project setup, rig or not

# OpenXR features per tab for the chosen devices, from the package adapters (class names change with package versions).
# Sets REQ_ANDROID / REQ_STANDALONE (must be on) and WANT_ANDROID / WANT_STANDALONE (enabled when available, incl. REQ).
# Needs the Editor (use_dep reads package versions): call it after wait_ready. Used by 4_openxr.sh and setup_check_code.
load_feature_names() {
  use_dep com.unity.xr.openxr
  has androidxr && use_dep com.unity.xr.androidxr-openxr
  $HANDS && use_dep com.unity.xr.hands
  REQ_ANDROID=""; REQ_STANDALONE=""; WANT_ANDROID=""; WANT_STANDALONE=""
  has quest         && { REQ_ANDROID="$REQ_ANDROID $OXR_QUEST_REQUIRED"; WANT_ANDROID="$WANT_ANDROID $OXR_QUEST_OPTIONAL"; }
  has androidxr     && { REQ_ANDROID="$REQ_ANDROID $AXR_REQUIRED"; WANT_ANDROID="$WANT_ANDROID $OXR_ANDROIDXR_OPTIONAL"; }
  $STANDALONE       && REQ_STANDALONE="$OXR_STANDALONE_REQUIRED"
  $HANDS && $ANDROID    && REQ_ANDROID="$REQ_ANDROID $HANDS_REQUIRED"
  $HANDS && has quest  && REQ_ANDROID="$REQ_ANDROID $HANDS_QUEST_ANDROID"
  $HANDS && $STANDALONE && REQ_STANDALONE="$REQ_STANDALONE $HANDS_REQUIRED"
  has quest         && WANT_STANDALONE="$WANT_STANDALONE $OXR_QUEST_OPTIONAL"
  has pcvr          && WANT_STANDALONE="$WANT_STANDALONE $OXR_PCVR_OPTIONAL"
  REQ_ANDROID="${REQ_ANDROID# }"; REQ_STANDALONE="${REQ_STANDALONE# }"
  WANT_ANDROID="$REQ_ANDROID$WANT_ANDROID"; WANT_STANDALONE="$REQ_STANDALONE$WANT_STANDALONE"   # required are always enabled too
}

field() { pl_field "$@"; }   # $1 object, $2 key (format: the Pipeline adapter)

# resolve_platforms <label> [quiet|soft]: sets PID_<device>, PNAME_<device>, PBT_<device> (platform id, profile name,
# build target); exits if a device's build support is missing (soft: returns 1 instead, prints nothing).
plat() { local v="$1_$2"; printf '%s' "${!v:-}"; }   # plat PNAME quest
resolve_platforms() {
  local label="$1" quiet="${2:-}" PLATFORMS d e m
  require_cmds BUILD_LIST_PLATFORMS || exit $?
  PLATFORMS=$(pl_objects "$(unity command $CMD_BUILD_LIST_PLATFORMS 2>&1)")
  [ -n "$PLATFORMS" ] || { echo "FAILED [$label]: '$CMD_BUILD_LIST_PLATFORMS' returned nothing"; exit 1; }
  for d in "${DEVICES[@]}"; do
    case "$d" in
      quest)     e=$(pl_where "$PLATFORMS" displayName '"Meta Quest"' | head -1)
                 [ -n "$e" ] || e=$(pl_where "$(pl_where "$PLATFORMS" buildTarget '"Android"')" isDerived false | head -1) ;;
      androidxr) e=$(pl_where "$PLATFORMS" displayName '"Android XR"' | head -1)   # dedicated platform (Unity 6.1+)
                 # On Unity 6000.x the Android XR platform is off until the user enables it (a per-user Editor setting):
                 # it is then listed only with show-all, as not installed. Then use plain Android and tell the user.
                 if [ -z "$e" ] && [ -z "$quiet" ] \
                    && pl_where "$(pl_objects "$(unity command $CMD_BUILD_LIST_PLATFORMS $ARG_SHOW_ALL true 2>&1)")" displayName '"Android XR"' | grep -q '"isInstalled":false'; then
                   echo "NOTE [$label]: the Android XR build platform is disabled in this Editor, so setup uses the Android platform."
                   echo "  Tell the user in the final report: to use the Android XR build profile, enable it (File > Build Profiles, select Android XR, click Enable), then run setup again."
                 fi
                 [ -n "$e" ] || e=$(pl_where "$(pl_where "$PLATFORMS" buildTarget '"Android"')" isDerived false | head -1) ;;
      pcvr)      e=$(pl_where "$(pl_where "$PLATFORMS" buildTarget '"StandaloneWindows64"')" isDerived false | head -1) ;;
    esac
    if [ -z "$e" ] || [ "$(field "$e" isInstalled)" != "true" ]; then
      [ "$quiet" = soft ] && return 1
      case "$d" in quest|androidxr) m="Android build support (with SDK/NDK and OpenJDK)";; pcvr) m="Windows build support";; esac
      echo "FAILED [$label]: $m is not installed for '$d'. Ask the user before installing it (large download; may need an Editor restart):"
      echo "  unity install-modules --editor-version <editor version> --list    then    unity install-modules --editor-version <editor version> -m <module id>"
      exit 1
    fi
    printf -v "PID_$d" '%s' "$(field "$e" id)"
    printf -v "PBT_$d" '%s' "$(field "$e" buildTarget)"
    case "$d" in quest|androidxr) printf -v "PNAME_$d" '%s' "$(field "$e" displayName | sed 's/™//')";; pcvr) PNAME_pcvr="Windows";; esac
    [ -n "$quiet" ] || echo "OK [$label]: $d -> platform '$(field "$e" displayName)' installed"
  done
  primary=$(plat PNAME "$PRIMARY")
}
