#!/usr/bin/env bash
# pipeline_check.sh — does the project's Unity Pipeline package work with this skill's scripts? Run it from the project
# folder once the Editor is connected. It checks that every command the scripts need is in the Editor's catalog, that an
# eval comes back readable and that the package list reads. If not, and --switch is given, it switches the project to the
# version in scripts/dependency/com.unity.pipeline/FALLBACK (a version these scripts are tested with), waits for the
# Editor to load it, and checks again. Without --switch it only reports that a switch is needed (exit 5).
# Usage: bash pipeline_check.sh [--switch]
# Prints OK / ASK / WARN / FAILED. Exit codes: 0 works (possibly after switching), 1 doesn't work, 3 blocked by an Editor
# dialog, 5 doesn't work and switching needs the user's OK.
set -u
SWITCH=false; [ "${1:-}" = --switch ] && SWITCH=true
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FALLBACK=$(tr -d ' \r\n' < "$D/dependency/com.unity.pipeline/FALLBACK")
[ -f Packages/manifest.json ] || { echo "FAILED [pipeline]: run this script from the Unity project folder."; exit 1; }
PROJ="${UNITY_PROJECT_PATH:-$(pwd -W 2>/dev/null || pwd)}"; export UNITY_PROJECT_PATH="$PROJ"   # every unity command below reaches this project

self_test() {   # in a fresh shell, so the version files are picked again for whatever Pipeline is loaded now
  bash -c '
    source "$1/_eval.sh"
    require_cmds EVAL EDITOR_STATUS PACKAGE_LIST PACKAGE_ADD PACKAGE_STATUS BUILD_LIST_PLATFORMS BUILD_LIST_PROFILES \
                 BUILD_CREATE_PROFILE TARGET_SWITCH TARGET_SWITCH_STATUS >/dev/null || { echo "commands missing from the catalog"; exit 1; }
    wait_ready >/dev/null || { echo "Editor not ready"; exit 3; }
    r=$(eval_result "return \"pipeline-ok\";"); [ "$r" = pipeline-ok ] || { echo "eval reply not readable ($(printf "%s" "$r" | cut -c1-120))"; exit 1; }
    v=$(package_version com.unity.pipeline); [ -n "$v" ] || { echo "package list not readable"; exit 1; }
    echo "$v"' _ "$D"
}

out=$(self_test); rc=$?
if [ $rc -eq 0 ]; then echo "OK [pipeline]: com.unity.pipeline $out works with this skill"; exit 0; fi
[ $rc -eq 3 ] && { echo "FAILED [pipeline]: the Editor is not ready or is blocked by a dialog. Ask the user to answer it, then re-run."; exit 3; }
cur=$(grep -o '"com.unity.pipeline": *"[^"]*"' Packages/manifest.json 2>/dev/null | sed 's/.*: *"//; s/"$//')
if [ "$cur" = "$FALLBACK" ]; then
  echo "FAILED [pipeline]: com.unity.pipeline $cur doesn't work with this skill ($out), and it is already the fallback version. Report this line to the user."; exit 1
fi
if ! $SWITCH; then
  echo "ASK [pipeline]: com.unity.pipeline ${cur:-?} doesn't work with this skill's scripts ($out). They work with $FALLBACK. Ask the user whether to switch the project's Pipeline package to $FALLBACK (then re-run with --switch) or stop."
  exit 5
fi
echo "WARN [pipeline]: com.unity.pipeline ${cur:-?} doesn't work with this skill ($out); switching the project to $FALLBACK."
inst=$(unity pipeline install --project-path "$PROJ" --package-version "$FALLBACK" 2>&1) \
  || { echo "FAILED [pipeline]: could not install $FALLBACK: $(printf '%s' "$inst" | tail -2 | tr '\n' ' ')"; exit 1; }
# The open Editor keeps the old package until it resolves again. Ask the running server to resolve; its reply format
# doesn't matter here, so it is not parsed.
unity command script eval --code 'UnityEditor.PackageManager.Client.Resolve(); return "ok";' >/dev/null 2>&1 || unity command eval --code 'UnityEditor.PackageManager.Client.Resolve(); return "ok";' >/dev/null 2>&1
for i in $(seq 1 60); do
  sleep 5
  unity command package list 2>&1 | grep -q "\"name\":\"com.unity.pipeline\",\"version\":\"$FALLBACK\"" && break
done
out=$(self_test); rc=$?
if [ $rc -eq 0 ]; then echo "OK [pipeline]: switched to com.unity.pipeline $out, which works with this skill. Tell the user in the final report."; exit 0; fi
echo "FAILED [pipeline]: com.unity.pipeline $FALLBACK doesn't work either ($out). Report this line to the user."; exit 1
