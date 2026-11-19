# Mapping rules — legacy event names to standard events

## Contents

- How to map
- Name families
- Routed, not mapped
- Below the floor
- Confidence

Read at Workflow B step 2. The output is `telemetry_migration.json` (format in
the retrofit reference); `insights.events telemetry_validate_migration` checks it.

## How to map

Map each legacy name from the survey's evidence, not from the name alone: open the call site
and ask what moment it records. `game_start` in a puzzle game is an attempt start, in a
shooter it may be a match start, and at boot it is neither. The name families below say
where a name usually lands; the call site decides. Then:

1. Pick the target from the standard events (`insights.events get_api_surface` lists them),
   or a declared `x_` when no standard event is the same moment. Never a near enough event.
2. Write the param transforms: `rename_param` for a legacy param that carries the same fact
   under another name, `set_param` for a fact the legacy name itself carries
   (`settings_sfx` → `setting: "sfx"`), `param_from_name` for a number in the name.
3. Give it a confidence (below) and a one-line `rationale` a reviewer can check against the
   call site.
4. If the best target is below the threshold, the name goes to `unmapped` with that target as
   a near-miss. Never raise a confidence to get a name over the floor.

Name mapping is not trigger repair. A mapped `level_start` still fires wherever the legacy
call did — at scene load in one game, at player control in another. Placement rules apply to
the new calls the plan places; the manifest records what the legacy name meant.

## Name families

Prefixes such as a studio or game tag (`s3_`, `fu_`) do not change the family.

| Family | Typical names | Target | Notes |
|---|---|---|---|
| Already standard | `screen_view`, `level_start`, `purchase` | the same event | Check the params match too; a same-name event with other params is a rename_param case. |
| GA4 verbatim | `earn_virtual_currency`, `level_end`, `tutorial_begin`, `tutorial_complete` | `resource_earn`/`resource_spend`, `level_end` by `success`, `flow_start`/`flow_complete` with `flow: flow_tutorial` | GA4 param names rename: `virtual_currency_name` → `resource`, `value` → `amount`, `level_name` → `node`. |
| Numbered level, stage, wave, mission | `win_level_12`, `level_3_start`, `fail_level_7`, `out_of_moves_level_4` | `level_complete` / `level_start` / `level_fail` | One family, not one mapping per number. `param_from_name` reads the number (`level`); the template puts it back: `win_level_{level}`. A dynamic name built in code (`"win_level_" + n`) is keyed `win_level_N`. `out_of_X` adds `outcome_reason`. |
| Outcome words, no number | `level_won`, `level_failed`, `level_quit`, `level_restart` | `level_complete`, `level_fail`, `level_quit` | `level_end`-style names without an outcome are `variants` split on the legacy param that carries it. A restart is a quit (`outcome_reason: restart`) plus a new start. |
| Run or attempt dialects | `run_start`, `race_end`, `battle_fail` | `level_start`, `level_end` variants, `level_fail` | Run-based games map the run to the attempt. Bare `game_start`/`game_end` are homonyms: decide from the call site, or leave unmapped. |
| Revive | `revive_ad`, `revive_gems`, `continue_used` | `revive` with `method` | Check before ad rules: an `ads_revive_*` name is a revive. |
| Ad funnel | `rewarded_show`, `inter_load_failed`, `ad_clicked`, `monetization_opportunity` | `ad_show_start`, `ad_load_failed`, `ad_click`, `ad_opportunity`, … | The stage word decides the event; the format word sets `ad_format` (`rewarded`, `interstitial`, `banner`). `closed` is not `complete` for skippable formats. An ad name with no stage word is unmapped. |
| Currency | `coins_earned`, `spend_gems`, `currency_gain` | `resource_earn` / `resource_spend` with `resource` | Without a currency in the name, the resource must come from a legacy param, so map it in the transforms or lower the confidence. |
| Shop and IAP | `iap_start`, `purchase_failed`, `shop_opened` | `purchase_initiate`, `purchase_failed`, `shop_view`, … | Where the IAP+ SDK is present it raises the purchase funnel itself; map the names, but the plan places no funnel calls of its own. |
| Flows, FTUE | `tutorial_step_3`, `onboarding_done`, `ftue_skip` | `flow_step_complete`, `flow_complete`, `flow_abandoned` | `flow` is a registry id. |
| Consent, settings | `consent_given`, `settings_music_off` | `consent_change`, `settings_changed` with `setting` | |
| Claims, daily loops | `daily_reward_claimed`, `wheel_spin` | `reward_claim` | Grant versus claim matters: claim is the player's action. |
| Progression | `player_level_up`, `achievement_unlocked`, `powerup_used` | `level_up`, `unlock_achievement`, `powerup_used` | A content unlock (`world_2_unlocked`) is `node_unlock`, not `level_up`. |
| Screens, navigation | `shop_screen_open`, `settings_menu`, `go_highscores` | `screen_view` with `screen` | The screen id must be declared in the registry. `go_*` is navigation in some games and a gameplay verb in others. `X_open` alone is weak: `chest_open` is a claim. |

## Routed, not mapped

Some legacy names are decisions but not events. They go to `routed` with a `route`, and the
plan places no call for them:

- `fold_derivation` — the value is derived from events already sent: balance snapshots
  (`coins_balance`), session milestones (`session_5m`), daily activation streaks.
- `engine_diagnostics` — FPS, memory, crash and exception names; Diagnostics owns them.
- `drop_vendor_noise` — events a vendor SDK sends on its own.
- `x_custom_recipe` — a known custom pattern the developer declares as `x_`.

## Below the floor

`unmapped` is an answer. Each name lists its `near_misses` (the targets considered, with their
confidence) and a `note` saying what would decide it — usually a fact about what the call records or
a legacy param the survey did not see. Leave it unmapped; do not drop a name.

## Confidence

| Confidence | When |
|---|---|
| 0.9–0.95 | The name is the standard event, or a GA4 name with a fixed translation, or a numbered family whose call site confirms the moment. |
| 0.8–0.85 | A clear family match (outcome word, ad stage word, currency verb) and the call site agrees. |
| 0.65–0.75 | The family fits but one fact is inferred: a run treated as an attempt, a resource taken from a param. |
| Below 0.6 | A homonym, or the call site does not settle it. Unmapped. |

The threshold is 0.6 unless the developer sets `confidence_threshold` in the manifest. The
validator rejects a mapping or route below it.
