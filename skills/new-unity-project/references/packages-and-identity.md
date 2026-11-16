# Packages and player identity (Steps 5 and 6) — new-unity-project reference

The detail behind the main skill file's Steps 5 and 6. Read it before either step.

## Step 5 — Packages

Map the brief to a package list and install via **`unity-package-management`** (**never**
hand-edit `manifest.json`): `package add` once the Editor is open with `com.unity.pipeline`
(Step 7); its C# installer script when packages must land pre-open (the `-quit` gotcha applies).
That skill has the genre/platform/monetization → package mapping. A `--bare` run installs only the
packages the user named, through the C# installer script (bare has no `com.unity.pipeline`, so
`package add` can't run; never `unity pipeline install` to get it, which undoes the bare ask), and
stops there. Otherwise paved
adds the floors' needs: Cinemachine for any gameplay camera (2D rigs included);
`com.unity.services.authentication` when Step 6 wires identity; and the live floor's services set
on cloud-linked projects — in this pass, not at shell time. Cloud save and remote config come from
create: when its `steps` report `cloudSave` and `remoteConfig` as `done` or `present`, skip them and
add only what is left (leaderboards). The template already provides the
render pipeline — never add `com.unity.render-pipelines.universal` on a Built-in template
(materials go pink) or vice versa. Read the list back before installing (paved: print it now);
verify `manifest.json` after.

## Step 6 — Player identity, then the craft floors

**Identity — the create result is the switch** (a `--bare` run skips this whole step: bare
projects still cloud-link unless `--no-cloud`, and the unwired cost was stated at Mode time):
identity addressed in the result → confirm it in the reconciliation and **skip**, never wire auth
twice. No identity and the project cloud-linked →
wire it: services init plus anonymous sign-in kicked off at bootstrap in **one small bootstrap
script**, never blocked on (mechanics: **`build-live-game`**, auth section — checked in Before
Step 1; missing and declined → skip the wiring, gap named); each service dependency is a named
plan line, declining removes only that line. Cloud link failed or skipped →
skip with the cost stated (saves and ranks stay local) — never wire services init against a
project with no cloud id. Invisible to players, upgradeable later — the root the live floor
builds on.

**A `--bare` run skips the floors too** (the Mode section's stated cost). Otherwise **load the
`craft-game-floors` skill** (checked in Before Step 1) and follow its floors through the
build — art bible and lighting with the baseline (Step 7), then camera, character motion, the
playable loop, feedback, audio, shell, live floor, and its done gate (Step 9).
Its values supersede the skill's own fallbacks (`visual-baseline.md` items 1 to 6 and the main
skill file's Step 9 loop) whenever present, version skew included. Missing
and declined → fallbacks, gap named in the report.

