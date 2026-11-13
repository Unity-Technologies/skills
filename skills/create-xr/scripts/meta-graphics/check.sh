#!/usr/bin/env bash
# check.sh — Quest graphics' read-only done check. Sourced by meta-graphics-setup.sh (verify) and scripts/status.sh.
#   graphics_check_code  C#: "done: ..." or "ERROR: ...", built from scripts/dependency/ fragments (use_dep unity, openxr first)

graphics_check_code() {
  printf '%s\n' "var todo = new System.Collections.Generic.List<string>();
var r = \"\";
{
$GRAPHICS_CHECK_CS
}
{
$URP_CHECK_CS
}
return (todo.Count == 0 ? \"done: \" : \"ERROR: \") + r;"
}
