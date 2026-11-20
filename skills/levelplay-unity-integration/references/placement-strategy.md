# Placement Strategy: From Project Profile to Ad Placements

How to turn the Step 2 project profile and the user's optimization goal into concrete placements
that Step 8 proposes and Step 9 implements. Every placement is a **format, a moment, a rule and a
location in the code**, because that is what becomes real code, while "use rewarded ads" does not.

## Contents

- [How to use this reference](#how-to-use-this-reference)
- [A proposal, not a promise](#a-proposal-not-a-promise)
- [Anchor placements to the project's code](#anchor-placements-to-the-projects-code)
- [Genre to format mapping](#genre-to-format-mapping)
- [Rule templates](#rule-templates)
- [Guards that always apply](#guards-that-always-apply)
- [Child-directed and mixed-audience games](#child-directed-and-mixed-audience-games)
- [Naming the placements](#naming-the-placements)
- [Where each placement goes](#where-each-placement-goes)
- [A placement that needs UI the project lacks](#a-placement-that-needs-ui-the-project-lacks)
- [The reward on a rewarded placement](#the-reward-on-a-rewarded-placement)
- [Banner: usually off, and how to say so](#banner-usually-off-and-how-to-say-so)
- [Shifting the balance with the user's goal](#shifting-the-balance-with-the-users-goal)
- [Bid floors stay optional](#bid-floors-stay-optional)
- [The proposal table](#the-proposal-table)
- [Wiring an approved placement in Step 9](#wiring-an-approved-placement-in-step-9)
- [Adding a format later](#adding-a-format-later)
- [Worked examples](#worked-examples)

## How to use this reference

1. Take the profile rows from Step 2: game type, session length, key moments (each with its
   `Class.Method` and `file:line`), the buttons found or missing, the economy, any existing ad
   placement and whether it breaks a guard, and the audience answer.
2. **Start from the key moments found.** Each one that maps to a format becomes a placement,
   anchored to its method, per [Anchor placements to the project's code](#anchor-placements-to-the-projects-code).
3. Fill gaps from [Genre to format mapping](#genre-to-format-mapping): only formats the found
   moments did not already cover, and only where the genre calls for them.
4. Fill each format's [rule template](#rule-templates) with the game's own values: the real run
   length, the real death moment, the real currency name.
5. Apply every [guard that always applies](#guards-that-always-apply), and the
   [child-directed rules](#child-directed-and-mixed-audience-games) when the audience row says so.
6. Set the numbers from the user's goal, per [Shifting the balance](#shifting-the-balance-with-the-users-goal).
7. Present [the proposal table](#the-proposal-table) and ask the user to approve or edit it.

Present one proposal derived from this game, not a menu of strategies, because the user's edits are
a faster way to the right plan than choosing between abstract options.

## A proposal, not a promise

The user decides on the proposal: approve, edit, or decline. Word it that way: what each placement
is, when it fires, and what protects the player. **Never promise or imply an outcome**: no
"maximize" or "boost revenue", no earnings, eCPM or fill figure, and none of the industry benchmark
percentages from `best-practices.md`. What a placement earns depends on the players, the market and
the demand, none of which the project files show; the LevelPlay dashboard reports it once the game
is live. Explain fit, not income.

The same holds for the law. Never state what a regulation permits, requires or forbids ("under
COPPA only contextual ads are allowed"): the skill says which API to call for a setting the user
has decided on (`references/privacy-settings.md`), and the developer decides the setting with their
own counsel. Point there; do not paraphrase the regulation.

## Anchor placements to the project's code

A placement tied to the game's code ("an interstitial in `RunManager.ReturnToMenu()`") is one the
user can picture, check and correct. A generic trigger is one they have to wire themselves. So:

- **Derive placements from the found moments first.** A death offers the rewarded revive, a level
  complete takes the interstitial, the return to menu takes the between-runs interstitial. Genre
  defaults fill only what the found moments leave uncovered.
- **The hook depends on the format.** An interstitial is hooked to the **moment** itself: the method
  where the level completes or the run returns to the menu. A rewarded ad is hooked **only to the
  handler of the button the player presses** to accept it (the Revive button, the Double coins
  button), never to the moment that makes the offer appear (`PlayerHealth.Die()`,
  `ResultsPanel.Show()`), because a call there plays a rewarded ad nobody chose to watch.
- **Copy the location from the profile.** The `Class.Method` and `file:line` come from a method
  Step 2 read. Never invent or tidy a symbol, and never point at a file you have not read.
- **A banner belongs to a screen, not a method.** Name the screen; its show and hide calls go where
  that screen opens and closes.
- **No matching method found** means the placement has no code location, and the proposal says so
  in words ("no return-to-menu method found: you call it where a run ends"). Never fill the gap with
  a plausible method name.

## Genre to format mapping

Pick the closest row. A game that straddles two rows takes the more conservative of the two.

| Game type | Primary | Secondary | Banner | Why |
|---|---|---|---|---|
| Endless runner, arcade score-attack | Rewarded revive | Interstitial between runs | Off | Death is a high-intent moment; runs are short, so the between-runs break needs a cap |
| Level-based puzzle | Rewarded hint or extra moves | Interstitial on level complete | Menu only | Failure is a stuck state, not a death; a hint is a natural opt-in |
| Idle, incremental | Rewarded multiplier or offline-earnings boost | Interstitial rarely, if at all | Main idle screen acceptable | Long idle screens tolerate a banner; the economy gives a multiplier something to multiply |
| Casual sim, tycoon, builder | Rewarded currency or speed-up | Interstitial at a session boundary | Menu only | Progression gates create natural rewarded demand |
| Action, shooter, platformer with lives | Rewarded continue | Interstitial on game over | Off | Interrupting action breaks the play the player came for |
| Narrative, story, puzzle-adventure | Rewarded hint only | None | Off | Interruption breaks the story |
| Card, board, casual multiplayer | Rewarded bonus or second chance | Interstitial between matches | Off | Match boundaries are clean breaks |
| Unclear or prototype | Rewarded only | None | Off | With no established loop, one opt-in format is the honest proposal |

When Step 2 found no key moments, use the last row whatever the genre, and say that more placements
can be added once the loop exists.

## Rule templates

Fill each template with the game's own values. Bracketed parts change per game.

**Rewarded revive or continue**

- Moment: the player dies or fails, at the death or fail method from the profile.
- Hook: the Revive or Continue button's press handler.
- Rule: one per run.
- Reward: 1 `[Revive]`.

**Rewarded currency, hint or multiplier**

- Moment: the player is short of `[currency]` at a purchase point, asks for a hint, or sees the
  results screen.
- Hook: the button's press handler.
- Rule: a daily ceiling (for example 3 to 5), so the reward keeps its value.
- Reward: `[amount] [currency]`, 1 Hint, or 1 `[multiplier]`.

**Interstitial between runs or levels**

- Moment: the run or level ends and the player returns to `[the menu or next-level screen]`.
- Hook: that method, before the scene load or screen change.
- Rule: from run `[N]` onward, at most one every `[M]` minutes, and never right after a rewarded ad.

Choose `N` from session length, because short runs reach a break more often:

| Session shape | Start at |
|---|---|
| Runs under 1 minute | Run 4 |
| Runs of 1 to 3 minutes | Run 3 |
| Sessions longer than 3 minutes, or no run unit | The second session boundary |

Choose `M` from the user's goal, using the ranges in `best-practices.md` ("Frequency Management"),
because those are the skill's single source for frequency caps: **Balanced 5 to 7 minutes**,
**Revenue-focused 3 to 5 minutes**, **UX-focused session boundaries only** (once per session, or 10+
minutes). Start in the middle of the range and say the user can tune it once dashboard data exists.

**Banner**

- Moment: a named non-gameplay screen.
- Rule: shown while that screen is open, hidden when it closes, never over an interactive element.

## Guards that always apply

These hold in every proposal and every goal, because they protect players from outcomes the user
has not seen yet. A goal changes the numbers, never these:

- **Rewarded only from a press.** A rewarded ad plays only from an explicit button-press handler the
  player chose. Never from a death, a screen show, a timer or a scene load.
- **Interstitial at the moment, never mid-action.** A forced ad fires at the transition itself (level
  complete, return to menu), never while the player has control, and never while the player has
  paused the game. An event the game raises on its own, with no press from the player (a timer, a
  year-end or wave tick in a running simulation, a new room generating as the player walks in) is
  not a transition unless the game already stops play behind a screen at that event; otherwise it
  fires during play, and no interstitial goes there.
- **Protect the first runs, from forced ads only.** No interstitial before run `N`, and never on the
  first run of a first session, because a new player who meets an ad before they understand the
  game is the most likely to leave. Rewarded offers are available from the first run, because the
  player can decline them.
- **Never stack.** An interstitial never directly follows a rewarded ad, in either order.
- **Failure is silent.** If an ad is not ready or fails, gameplay continues as if it was never
  offered: no error dialog, no blocked progression, and a rewarded flow returns the player to where
  they were. See "Error Handling and Graceful Degradation" in `best-practices.md`.
- **No ad before initialization.** Create and load ads only after `LevelPlay.Init()` has been
  called — on device from `OnInitSuccess`; in the Editor, where that callback may not fire, creating
  them in `Start()` after `Init()` is the documented mock-ad path (see the initialization note in
  `testing-and-validation.md`) — and show only when the ad reports it is ready, because an ad created
  before `Init()` never loads.

## Child-directed and mixed-audience games

When the Step 2 audience row is `Child-directed` or `Mixed`, apply these before the goal sets any
number, and treat both answers the same, because a mixed-audience game is played by children too.
The user can relax a rule on the proposal; the agent does not relax one for them:

- **Banner off, on every screen.** A banner beside the play area or on a home screen invites
  accidental taps from small fingers, so it is off whatever the goal, and the proposal says why in
  one clause.
- **Rewarded off by default.** A rewarded offer is a value exchange ("watch this, get that") that a
  young child cannot weigh, so propose none. If the game has a screen a parent reaches (settings, a
  shop behind a gate), one rewarded offer may be proposed there, named as such.
- **Interstitial only at a break the player chose, never at session start.** The press that starts
  play is the first thing a child does, so no forced ad goes there, nor on a return to the home
  screen that a child reaches by stopping. A level complete in a game with levels qualifies, at most
  once per session, from the second session.
- **"No ads" is a valid proposal.** A free-play toy with no break and no economy (a soundboard, a
  toddler sandbox) has no placement that passes these rules, so the proposal says so in one line
  and stops. Nothing is invented so that there is something to propose.
- **The setting is the user's, the law is not the agent's to state.** `SetCOPPA(true)` before
  `LevelPlay.Init()` in Step 6.5, and `coppa: 1` on the app in Step 5, both follow from the user's
  answer; say that they do. What any regulation allows is not for the agent to say, per "A
  proposal, not a promise" above.

## Naming the placements

Give each placement one human-readable name and use it everywhere it appears: the proposal table,
code comments, and the ad unit the user creates in the LevelPlay dashboard. One name lets the user
follow one moment of their game from the proposal to the dashboard.

Real words, first word capitalised, 24 characters or fewer, naming the **moment**, not the format:

| Placement | Name |
|---|---|
| Rewarded revive offered at death | Revive after death |
| Rewarded hint on the hint button | Free hint |
| Rewarded 2x offline earnings | Offline earnings boost |
| Interstitial on return to menu after a run | Between runs |
| Interstitial on level complete | Level complete |
| Banner on the main menu | Main menu banner |

Never `Rewarded_Revive` or `DefaultRewardedVideo`: the format is already in its own column, and an
identifier says nothing about where the ad runs. If the user also uses LevelPlay dashboard
placements (the `placementName` passed to `ShowAd`), derive that name from the same one, for
example `between_runs`.

## Where each placement goes

Every placement answers two questions, because the user is agreeing to something that will appear
on their screens and in their code:

- **On screen**: the screen and position in the player's terms ("Death screen, before results",
  "Main menu, bottom", "Hint button, level screen").
- **In code**: `Class.Method()` at `file:line`, when a method was read. Otherwise say who adds the
  call.

**A project with no gameplay code to hook** (a template, an empty scene, a prototype with no loop)
gets on-screen positions only, and the proposal says so: "These are recommended positions to wire
once the game has a results screen and a death moment." Never present a recommended position as a
moment the project already has.

## A placement that needs UI the project lacks

A rewarded offer is always a press, so its button is part of the placement. Check whether the
button exists before saying it does, per "UI a placement needs" in `project-inference.md`.

- **Found**: hook the placement to the button's handler and name it in the proposal.
- **Not found**: still propose the placement, and say what is missing: "Double coins on the results
  screen: **needs a Double coins button on the results screen**. You add the button; Step 9 writes
  the ad call for its handler."
  - The on-screen position must not imply the button exists: "Results screen, button to add", never
    "Double coins button".
  - No code location, and never the method that shows the screen as a stand-in, because that would
    play a rewarded ad nobody pressed for.
  - Step 9 writes the ad-side method (show, cap, reward callback) and tells the user where to call
    it. Do not create, generate or lay out the button, because building the game's UI is outside
    this skill and an invented button would not match their screens.

## The reward on a rewarded placement

Every rewarded placement states its reward in the proposal, in the game's own word: `1 Revive`,
`50 Coins`, `1 Hint`, `1 Double earnings`. For currency, use the amount the game already hands out
at that moment when the code shows one; otherwise propose a modest round figure and say it is a
default to correct.

The reward the code grants is the game's own method (the existing revive, the existing add-coins
call), passed as the reward callback. Never invent game logic to match the reward, because the
user owns their economy.

## Banner: usually off, and how to say so

For a gameplay-heavy game a banner takes permanent screen space. Default it to off and give the
reason in one clause ("a portrait runner has no screen area to spare") rather than silently omitting
it. Mention an off placement once, in the proposal, then stop: it gets no code and no checklist row
unless the user turns it on.

Turn it on when the game has a static screen where the player lingers (an idle game's main screen,
a shop, a lobby), and name that screen.

## Shifting the balance with the user's goal

Map the Step 2 goal onto the proposal by changing numbers and adding or dropping placements, never
by removing a guard:

- **Balanced** (and "Not sure yet"): the templates as written. Interstitial gap 5 to 7 minutes,
  2 to 3 rewarded placements, banner only on a static screen.
- **Revenue-focused**: start interstitials at run 2, gap 3 to 5 minutes, add a second or third
  rewarded placement at a different moment, and propose a banner at the bottom of the gameplay
  screen, as `best-practices.md` does for this goal; when it would cover controls or the play area,
  propose it on the menus instead and say why. Say plainly that players will
  see more ads, which can cost retention, and that it can be tuned back once dashboard data exists.
  Never say it will earn more.
- **UX-focused**: rewarded only, 1 to 2 placements, every one opt-in. Interstitials at session
  boundaries only, or none; banner off or menu only. Say that players will see fewer ads.

## Bid floors stay optional

Do not put bid floors in the proposal. Say once that they are skipped by default and that Step 9
offers them as an optional setting, because a floor set before the app has dashboard data mostly
lowers fill.

## The proposal table

Open with one sentence summarising the proposal, then one table, one row per enabled placement.
The Moment column names the action that fires the ad (a button pressed, a screen closing, a scene
loading): "Player leaves the victory screen", "Restart pressed", never "on the victory screen",
because an ad "on" a screen fires while the player is still reading it:

| Placement | Format | Moment (on screen) | Rule | Where in code |
|---|---|---|---|---|
| Revive after death | Rewarded | Player dies; Revive button on the death screen | One per run; reward 1 Revive | `DeathScreen.OnReviveClicked()` at `Assets/Scripts/UI/DeathScreen.cs:27` |
| Between runs | Interstitial | Run ends, back to the menu | From run 4; at most one every 6 min; never after a rewarded | `RunManager.ReturnToMenu()` at `Assets/Scripts/RunManager.cs:88` |

After the table, add at most four short lines: placements that need UI the user adds, any game-code
change a reward needs (for example "Double coins needs a small `Economy.DoubleLastReward()` added
to `Assets/Scripts/Economy.cs`, because nothing in the game doubles a payout today"), any placement
proposed as off with its reason, and that bid floors are skipped. The user approves the game-code
change together with the placement, so Step 9 never springs it on them. Then ask the approval
question in workflow Step 8.

When the user edits the proposal, apply the edit, show only the changed rows, and ask again. Nothing
is implemented before the user approves the current version, because an edit can change which
files Step 9 touches.

## Wiring an approved placement in Step 9

The ad code (built from `rewarded-api.md`, `interstitial-api.md` and `banner-api.md`, in the code
organization the user picks) owns everything about the ad: loading, showing, the cap and every guard.
The user's own method gains **exactly one call into it**, and nothing else changes — with one
exception: when the game has no method that grants the reward a placement promises (nothing doubles
the payout, nothing adds a hint), the ad code's reward callback needs a **minimal reward hook** in the
economy code — one small method or flag, nothing more — and only one that the Step 8 proposal listed
and the user approved. Disclose it in the Step 9 file list with its `file:line`, because it is a
change to the game's rules, not to the ad code. This procedure
applies only when the user chose an organization option that puts you in their files (options 1, 2
and 4 in Step 9); under option 3, "Just show me the code snippets", produce a call-site snippet per
approved location, labelled with its `file:line`, and do not edit any gameplay script:

1. Before editing a game script, list the files you will change, because they are the user's code,
   and make a rollback point: under version control, ask the user to commit (or confirm the files are
   committed) before you edit; without version control, copy each file you will change to a folder
   outside `Assets/`, for example `<project>/LevelPlayBackups/`, because Unity imports anything under
   `Assets/` and a copy there would get its own `.meta` file. Tell the user the copy's path and that
   restoring is replacing the edited file with it.
2. Read the method at the cited `file:line` again, because the file may have changed since Step 2.
3. Insert one call. For an interstitial, at the moment itself. When that moment goes on to load a
   scene (`ReturnToMenu()` ending in `SceneManager.LoadScene`), the load must wait for the ad: give
   the ad code a show call that takes a continuation and invokes it exactly once on every path:
   from `OnAdClosed` or `OnAdDisplayFailed` once a show has started, and at once on every path that
   skips the ad — not ready, placement or dashboard cap active, the rule's own cap, the manager
   missing — because a skipped ad fires no callback and the player would stay on the results
   screen. Then pass the scene load as that continuation, because `ShowAd()` returns before the ad
   closes and a load that runs immediately tears the ad down with the scene. Keep the manager alive across scenes (the startup-scene object
   with `DontDestroyOnLoad`, as the reference places it), because a scene-scoped manager's
   `OnDestroy()` destroys the ad mid-display. For a rewarded placement, inside the button's press
   handler, binding the reward to this placement at show time. With the reference's multi-placement
   manager, first add a public show method for the approved placement (for example
   `ShowAdForRevive()` calling its private `ShowAdWithPlacement("revive_after_death")`) and a
   matching case in its `OnAdRewarded` dispatch that calls the game's reward method, because the
   reference's `hints` / `extra_lives` / `bonus_coins` cases are examples, not placements a script
   can call; then wire the button handler to that public method. With the single-placement manager,
   pass the reward callback into the show call. Either way each reward event resolves against the
   placement that produced it. Never overwrite a pending reward callback from another button, and ignore a press while a
   show is pending, because `OnAdRewarded` can arrive after `OnAdClosed` and a shared, replaced
   callback would hand the first ad's reward to the second button. First read what the handler
   does today: if it already grants the reward directly (a `RevivePlayer()` call, a hint added), move
   that grant into the reward callback so it runs only after the ad rewards, because leaving it in
   place beside the ad call grants the reward twice, or for free when the ad fails. Move with it every
   statement that assumes the reward happened — closing the death screen, resuming the game, hiding
   the button — because a `Hide()` left in the handler closes the screen while the player is still
   dead when no ad is ready. Keep any path that does not depend on the ad (a paid revive, a free hint
   the game already gives) as its own branch. When no ad is available, leave the player where they
   were with a visible result (the button disabled or a short "No ad available" state), never a
   closed screen and no reward, and fall through to the ad-free path if the game has one.
4. Guard the call against a missing reference to the ad manager, so the game still runs before the
   user wires the manager in a scene; when the guard trips, run the continuation (the scene load,
   the game's own path) directly, so a missing manager skips the ad rather than the transition.
5. Keep the method's name, signature and callers, and change nothing in the file outside it; the
   only edits inside it are the single ad call and the statements step 3 moves into the callback.

Before a rewarded edit, check the hook is a press handler (an `onClick` listener, a `...Clicked()`
or `...Pressed()` method a button calls). If it is the moment itself, make no edit and tell the user
where the call belongs.

## Adding a format later

Do not repeat the profile. Re-read `Packages/packages-lock.json` to confirm the SDK is still
resolved, re-read the method you would hook, derive the new placement from this reference, show it
as a one-row proposal for approval, then follow "Adding More Ad Formats Later" in the workflow.

## Worked examples

**Endless runner, about 30-second runs, coin economy, Revive button found.** Summary: a rewarded
revive on death plus a capped interstitial between runs; Balanced goal.

| Placement | Format | Moment (on screen) | Rule | Where in code |
|---|---|---|---|---|
| Revive after death | Rewarded | Player dies; Revive button on the death screen | One per run; reward 1 Revive | `DeathScreen.OnReviveClicked()` at `Assets/Scripts/UI/DeathScreen.cs:27` |
| Between runs | Interstitial | Results screen, back to the menu | From run 4; at most one every 6 min; never after a rewarded | `RunManager.ReturnToMenu()` at `Assets/Scripts/RunManager.cs:88` |

Banner off: a portrait runner has no screen area to spare. Bid floors skipped.

**Level-based puzzle, 2-minute levels, hint counter, no free-hint button, existing in-app
purchases.** Summary: a rewarded hint plus an interstitial on level complete; UX-focused goal.

| Placement | Format | Moment (on screen) | Rule | Where in code |
|---|---|---|---|---|
| Free hint | Rewarded | Hint counter at zero; level screen, button to add | 3 per day; reward 1 Hint | None: needs a Free hint button on the level screen. You add it; Step 9 writes the call for its handler |
| Level complete | Interstitial | Level-complete screen, session end only | Once per session, never after a rewarded | `LevelManager.CompleteLevel()` at `Assets/Scripts/LevelManager.cs:142` |

Note that the game sells hint packs: do not show the free-hint offer to a player who has hints
left, so the offer does not undercut the purchase.

**Idle incremental, long sessions, deep currency economy, offline-earnings popup with a Claim 2x
button.** Summary: rewarded multipliers plus a banner on the main idle screen, no interstitials;
Balanced goal.

| Placement | Format | Moment (on screen) | Rule | Where in code |
|---|---|---|---|---|
| Offline earnings boost | Rewarded | Player returns; Claim 2x on the offline-earnings popup | Once per session; reward 1 Double earnings | `OfflinePopup.OnClaimDoubleClicked()` at `Assets/Game/UI/OfflinePopup.cs:53` |
| Main screen banner | Banner | Main idle screen, bottom | Hidden while any popup is open | Show in `MainScreen.OnEnable()` at `Assets/Game/UI/MainScreen.cs:18`; hide in `MainScreen.OnDisable()` at `Assets/Game/UI/MainScreen.cs:24` |

Interstitials off: an idle game has no run or level break to place one at, so any forced ad
would interrupt the player.
