#!/usr/bin/env bash
# dependency/com.unity.pipeline/0.9.sh — Pipeline package adapter: which Editor commands exist and how their results read.
# Picked by the project's com.unity.pipeline version (major.minor). When a Pipeline release renames a command or a
# result field: copy this file to <new major.minor>.sh, fix it there, keep this one.

# ---- command names (CAND_<KEY>, spellings separated by |). require_cmds resolves each key against the Editor's catalog (first spelling found wins);
# PIPELINE_CMD_<KEY>="<name>" overrides one without editing the scripts.
CAND_EVAL="eval"
CAND_EDITOR_STATUS="editor status|editor_status"
CAND_PACKAGE_LIST="package list"
CAND_PACKAGE_ADD="package add"
CAND_PACKAGE_STATUS="package status"
CAND_BUILD_LIST_PLATFORMS="build list_platforms"
CAND_BUILD_LIST_PROFILES="build list_profiles"
CAND_BUILD_CREATE_PROFILE="build create_profile"
CAND_TARGET_SWITCH="build.target switch"
CAND_TARGET_SWITCH_STATUS="build.target switch_status"
CAND_CONSOLE_STATUS="console status|console_status"
OPTIONAL_CMDS=" EDITOR_STATUS CONSOLE_STATUS "   # missing -> the helper falls back (wait_ready probes with eval; status.sh prints console: unknown)

# ---- parameter names that change between versions (used by _eval.sh)
ARG_PACKAGE_ADD_NAME="--identifier"
ARG_INCLUDE_INDIRECT="--include_indirect"
ARG_SHOW_ALL="--showAll"

# ---- result fields (results are compact JSON: "key":value, no spaces)
pl_success()  { printf '%s' "$1" | grep -q '"success":true'; }
pl_result()   { printf '%s\n' "$1" | sed -e 's/.*"result":"\{0,1\}//' -e 's/"\{0,1\}}$//'; }   # eval's return value
pl_message()  { printf '%s' "$1" | grep -o '"message":"[^"]*"' | head -1; }
pl_field()    { printf '%s' "$1" | grep -o "\"$2\":[^,}]*" | head -1 | sed -e 's/^[^:]*://' -e 's/"//g'; }   # $1 object, $2 key
pl_objects()  { printf '%s' "$1" | grep -o '{[^{}]*}'; }               # flat objects of a listing, one per line
pl_where()    { printf '%s\n' "$1" | grep "\"$2\":$3"; }               # $1 objects, $2 key, $3 JSON value ("\"Android\"", false)
pl_is_true()  { printf '%s' "$1" | grep -q "\"$2\":true"; }

# editor status (needs --format json; this one is pretty-printed, so allow spaces)
pl_editor_state() {   # $1 reply -> ready | busy | blocked:<dialog title> | unknown
  if printf '%s' "$1" | grep -q '"status": *"blocked_by_dialog"'; then
    echo "blocked:$(printf '%s' "$1" | grep -o '"title": *"[^"]*"' | head -1 | sed -e 's/.*: *"//' -e 's/"$//')"; return
  fi
  if printf '%s' "$1" | grep -q '"status": *"ready"' && printf '%s' "$1" | grep -q '"compiling": *false' \
     && printf '%s' "$1" | grep -q '"domainReloadInProgress": *false'; then echo ready; return; fi
  if printf '%s' "$1" | grep -q '"status":'; then echo busy; return; fi
  echo unknown
}

# console status (needs --format json; allow spaces).
pl_console_state() {   # $1 reply -> "compile ok" | "compile failed" | unknown
  local cf
  cf=$(printf '%s' "$1" | grep -o '"compilationFailed": *[a-z]*' | head -1 | sed 's/.*: *//')
  case "$cf" in true) echo "compile failed" ;; false) echo "compile ok" ;; *) echo unknown ;; esac
}

# packages
pl_has_packages()    { printf '%s' "$1" | grep -q '"name":"'; }
pl_package_version() { printf '%s' "$1" | grep -o "\"name\":\"$2\",\"version\":\"[^\"]*\"" | head -1 | sed -e 's/.*"version":"//' -e 's/"$//'; }
# One package's latest operation in `package status`. Split at each operation's start ({"status":"...) so an entry keeps
# its nested "package":{...} and "manifest":{...} objects, which a [^{}]* match cannot cross.
pl_package_op()      { printf '%s' "$1" | sed 's/{"status":"/\n{"status":"/g' | grep "\"argument\":\"$2\"" | tail -1; }
pl_op_status()       { printf '%s' "$1" | grep -o '^{"status":"[a-z_]*"' | sed 's/.*:"//; s/"$//'; }   # completed | failed | …
pl_op_error()        { printf '%s' "$1" | grep -o '"error":"[^"]*"' | head -1; }

# build profiles and target switch
pl_profiles()     { printf '%s' "$1" | grep -o '{[^{}]*"guid"[^{}]*}'; }
pl_switch_state() {   # $1 reply of switch / switch_status -> done | failed | switching | other
  if printf '%s' "$1" | grep -q '"status":"completed"'; then pl_success "$1" && echo done || echo failed
  elif printf '%s' "$1" | grep -q '"status":"switching"'; then echo switching
  else echo other; fi
}
