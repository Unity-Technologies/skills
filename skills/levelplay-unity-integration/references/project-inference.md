# Profiling a Unity Project Before Proposing Ads

What to read, what to search for, and what each signal means, so Step 2 builds a short project
profile from a handful of targeted reads instead of a crawl through the project. Step 8 builds its
placement proposal from this profile, so every finding here should name the file and line it came
from.

This step only reads files. Do not edit anything while profiling, because the user has not approved
any change yet.

## Contents

- [Budget and order](#budget-and-order)
- [Game type](#game-type)
- [Session length](#session-length)
- [Key moments](#key-moments)
- [UI a placement needs](#ui-a-placement-needs)
- [Economy](#economy)
- [Existing monetization](#existing-monetization)
- [Target platforms](#target-platforms)
- [Audience](#audience)
- [The readout](#the-readout)
- [Confidence and how to phrase it](#confidence-and-how-to-phrase-it)
- [When the project tells you nothing](#when-the-project-tells-you-nothing)

## Budget and order

Aim for roughly ten reads, not a hundred. Work in this order, and stop on a row as soon as it is
settled:

1. **Scene names.** Read `ProjectSettings/EditorBuildSettings.asset` for the scenes in the build;
   if it lists none, list the `.unity` files under `Assets/`. Names alone establish structure.
2. **`ProjectSettings/ProjectSettings.asset`** for the product name and the per-platform
   application identifiers.
3. **`Packages/manifest.json` and `Packages/packages-lock.json`** for the packages the project uses.
4. **One search pass over the game's own scripts** (`Assets/**/*.cs`) with the patterns below.
   Leave out `Library/`, `Packages/` and third-party folders under `Assets/` (for example
   `Assets/Plugins/` or an imported asset's own folder), because a hit there is not the game's code.
5. **Two or three whole-file reads** of the scripts the search ranked highest: usually the player
   controller, a game manager, and whatever owns the score or currency. These reads also confirm
   each key moment before you cite it.

Never read every script. The profile has seven rows, and a wrong guess costs the user one correction,
which is cheaper than a long silent crawl.

The patterns below are extended regular expressions. With `grep`, for example:
`grep -rnE --include='*.cs' '\b(Die|OnDeath|GameOver)\b' Assets`. Use `|` for alternation, not
`\|`, because the escaped form matches a literal bar under extended syntax and finds nothing. When
you need to know which of two literals is present, search for each separately, so each result is
quotable on its own.

## Game type

**Scene names** are the strongest single signal and cost nothing:

| Scene names like | Suggests |
|---|---|
| `Main`, `Game`, `GameOver` only | A single-loop arcade game or runner |
| `Level_01`, `Level_02`, `LevelSelect` | Level-based progression |
| `Menu`, `Shop`, `Inventory`, `Base` | An economy-driven sim or builder |
| `Lobby`, `Match`, `Results` | Match-based, possibly multiplayer |
| `Intro`, `Chapter_*`, `Dialogue` | Narrative |

**Class names** that discriminate genre:

```
class\s+\w*(Spawner|Runner|Obstacle|Endless|Generator)   -> runner, endless
class\s+\w*(Level|Puzzle|Grid|Board|Tile)Manager         -> puzzle, board
class\s+\w*(Idle|Production|Tick|Offline)                -> idle, incremental
class\s+\w*(Weapon|Enemy|Health|Damage)                  -> action, shooter
class\s+\w*(Dialogue|Story|Chapter|Quest)                -> narrative
```

**Packages** are a cross-check: `com.unity.netcode.gameobjects`, `com.unity.services.lobby` or
`com.unity.services.multiplayer` in `Packages/manifest.json` points at match-based multiplayer.

## Session length

Derive it, and say what it was derived from. In order of reliability:

1. **An explicit timer or run length** in code: a countdown, a `roundDuration`, a `timeLimit`
   serialized field. Read the value.
2. **Level count and pacing**: a level-based game with 60 short levels has minute-scale sessions.
3. **Loop shape**: an endless runner with a death-and-restart cycle and no timer has runs of tens of
   seconds; an idle game with offline-earnings logic has sessions of minutes with long gaps.

```
(timeLimit|roundDuration|sessionLength|matchDuration|countdown)
(offlineEarnings|lastPlayedTime|TimeSpan|DateTime\.(Now|UtcNow))
```

The second pattern also tells you whether the game models time away, which is the idle-game tell.

## Key moments

These are where ads go: Step 8 anchors each placement to one, and Step 9 puts the ad call there (or,
for a rewarded ad, in the button handler that accepts it). This row deserves the most care. Find the
method, and name it: "death at `PlayerHealth.Die()`" is worth far more than "death".

```
(void|public|private|protected).*\b(Die|OnDeath|GameOver|PlayerDied|HandleDeath)\b
\b(LevelComplete|OnLevelFinished|WinLevel|StageClear|OnVictory)\b
\b(Restart|Retry|PlayAgain|ReturnToMenu|BackToMenu)\b
\bSceneManager\.LoadScene\b
\b(OnPause|Pause|Resume|OnApplicationPause)\b
\b(LevelUp|OnLevelUp|Unlock|OpenShop|ShowShop)\b
```

Map what you find:

- A death or fail method is where a rewarded revive or continue is **offered**. The placement's
  call goes in the Revive button's handler, found per [UI a placement needs](#ui-a-placement-needs),
  never in the death method itself.
- A level-complete or victory method is an interstitial moment.
- A restart or return-to-menu path is the between-runs interstitial moment.
- A level-up, unlock or shop method is a rewarded moment (bonus currency, a speed-up), again
  through a button.
- A pause handler matters because no forced ad may fire while the player has paused the game.

**Record each moment as `Class.Method` at `file:line`**, straight from the search hit: the path is
project-relative under `Assets/`, the line is the method's declaration. Hold every entry to these
rules:

- **Confirm before you cite.** Read the method and enough around it to see the enclosing class and
  what it does, because a match on `Die` in a comment, a string or `EnemyHealth.Die` is not the
  player's death. Only a method you have read goes in the profile.
- **Never invent a symbol.** No guessed class, no method you expect to exist, no path you did not
  see in a result, because Step 9 edits exactly the location you cite. A moment with no confirmed
  method is named in plain words ("return to menu") with no location, and Step 8 says so.
- **Prefer the most specific method**: the one where the moment happens (`PlayerHealth.Die`), not a
  dispatcher that eventually reaches it (`GameManager.Update`), and not a Unity callback shared by
  many moments (`OnTriggerEnter2D`) when a named handler exists.
- **Keep the alternates.** When two methods fit one moment, name the chosen one first and the others
  after it ("death at `PlayerHealth.Die()`, also `GameManager.OnGameOver()`"), so a wrong pick is one
  correction away.

## UI a placement needs

A rewarded ad is a button the player presses, so a moment is only half a placement: a results screen
is where a **Double coins** offer would go, not proof that a Double coins button exists. Before
Step 8 names a button, check that it exists:

1. **All of the game's own scripts**, not only the folder of the class that shows the moment,
   because a view class often lives apart from the manager that owns the moment (`UI/RewardScreen/`
   next to `Managers/`). Search `Assets/**/*.cs` (minus third-party folders) for buttons and
   handlers:
   ```
   (Button|onClick|OnClick|AddListener|clicked)
   (Double|Multiply|2x|x2|Bonus|Watch|Revive|Continue|Hint|Free|Restart|Retry|Return|Home|Again|Replay|Next)\w*
   ```
   The second pattern names the screen's own buttons (Restart, Retry, Return, Next) as well as
   reward words, because a lose or results screen that no script shows by name is found through the
   buttons on it, and that screen is where the rewarded offer goes.
2. **Its prefab, scene or UXML**, because the UI itself lives there. For uGUI, search `.prefab` and
   `.unity` files for
   `m_Name: .*(Double|2x|x2|Bonus|Watch|Revive|Continue|Hint|Restart|Retry|Return|Home|Again|Replay|Next)`.
   A button wired in the Inspector has no `AddListener` in code: search the same files for
   `m_MethodName:` followed by the handler's name. For UI Toolkit, search `.uxml` files for a
   `Button` element with a matching `name`.
3. **Record the finding with the moment**: "results screen `ResultsPanel.Show()` has a Double coins
   button, handled by `ResultsPanel.OnDoubleClicked()` at `Assets/UI/ResultsPanel.cs:41`", or "I
   couldn't find a Double coins button (searched all scripts and `Results.prefab`)".

**A miss is "I couldn't find one", never "there is none."** A search can miss a button whose label
or folder you did not guess, so phrase the miss as what you searched, and let the readout invite the
correction ("if the game has one, tell me its script"). Still never write a profile row or a
proposal line that implies the button exists when these reads did not find it, because Step 9 would
then wire an ad to UI that is not there. Step 8 proposes such a placement as needing UI the user
adds, or hooks it to the button the user names.

## Economy

```
\b(coins?|gems?|currency|gold|credits?|lives|energy|stars|hints?)\b
\bPlayerPrefs\.(Get|Set)(Int|Float|String)\b
class\s+\w*(Shop|Store|Upgrade|Inventory|Wallet|SaveData)
\b(Continue|Revive|ExtraLife|AddLife)\b
```

What to conclude:

- A named currency plus a shop or upgrade class means a real economy: rewarded currency and
  multipliers have something to attach to.
- Saved score or currency (`PlayerPrefs` writes, a save class) means progression persists, which
  gives a rewarded reward something to keep.
- Lives, energy or an existing continue path give a rewarded continue a reward the game already
  knows how to grant.
- A score with no currency and no shop means the only durable reward is a revive or a continue. Say
  "score only, no currency" rather than "none", because it leads to a different proposal.

## Existing monetization

Read `Packages/packages-lock.json` first, because it records what is actually installed; then search
the code, because a package can be installed and unused:

```
(LevelPlay|IronSource|LevelPlayRewardedAd|LevelPlayInterstitialAd|LevelPlayBannerAd)
\bIronSource\.Agent\b
(UnityEngine\.Advertisements|Advertisement\.Show)
(UnityEngine\.Purchasing|IStoreListener|IAPButton|CodelessIAPButton)
```

Report what exists by name, and route on what you find:

- **Package installed, no LevelPlay code** (`com.unity.services.levelplay` in the lock file and no
  hit in the code): this is still a fresh integration, because the lock entry only records that the
  package resolved. Report it as installed; Step 3 confirms the install and continues.
- **LevelPlay code present**: not a fresh integration. `IronSource.Agent` calls mean a migration:
  say so and offer the migration path from Step 0. Current Ad Unit API code means the user is adding
  to an existing setup: Step 9 then starts from their code.
- **Unity Ads** (`com.unity.ads`, `UnityEngine.Advertisements`): offer the Unity Ads migration from
  Step 0.
- **Existing placements are audited, not inherited.** For each ad call the search finds, whatever
  the SDK, read the method around it and record its format, what triggers it, and what the game
  grants when it closes. Check each against the placement guards Step 8 applies (a rewarded ad
  only from a press the player chose; a forced ad only at a transition, never mid-action; never
  one directly after the other).
  One that breaks a guard (an interstitial that fires when the player walks up to an object or on a
  timer and then pays out a reward; a rewarded ad shown from a death method rather than a button; a
  banner over controls) goes in the readout as found **and** as needing a change, and Step 8
  proposes the conversion at the same moment (a forced ad that pays a reward becomes a rewarded
  offer behind a press there) rather than a second placement beside it. Keeping it as it is stays
  the user's call, made on the proposal, never the default.
- **In-app purchases** (`com.unity.purchasing`, `UnityEngine.Purchasing`): note it, because a
  rewarded offer must not undercut a product the game sells (a hint pack, extra lives).
- **Another ad SDK**: report that it is present and move on. Do not describe or compare it, for the
  same reason the skill never characterizes other mediation products.

## Target platforms

LevelPlay runs on Android and iOS only, so these are the only platforms the profile reports. Read
the `applicationIdentifier` entries in `ProjectSettings/ProjectSettings.asset` (iOS appears there as
`iPhone`): an entry that is not the default `com.DefaultCompany.*` identifier suggests the platform
was prepared. An `Assets/Plugins/Android/` or `Assets/Plugins/iOS/` folder corroborates. Any other
target (Standalone, WebGL) is named as not covered by LevelPlay.

Treat this row as inferred. Step 4 still asks the platform question, because the active build
target is not recorded in a readable project file.

## Audience

Whether the game is directed at children changes the proposal more than any other row: it rules
formats out before the goal sets any number (Step 8 has rules for child-directed games), it
makes the `SetCOPPA(true)` call in Step 6.5 required, and on the API
path it sets the app's `coppa` flag in Step 5. The project files rarely state it, so this row is
**asked, not inferred**, in the same message as the goal question:

**"Who is the game for: a general audience, children, or both (mixed)?"**

The user's answer is the only source. Whether a game is child-directed is a determination the
developer makes, with `references/privacy-settings.md` (COPPA Compliance) and their own counsel;
the agent never makes it for them and never states what a law permits or requires. Signals in the
project (a toddler-style soundboard or free-play toy with no fail state, nursery art, `Kids`,
`Toddler`, `ChildSafe` or `Parental` in scene, class or product names) mean the question is asked
with the signal named, so the user can confirm it in a word ("the product name is Turtle Fishing
and there is no fail state, which reads like a game for young children: is it directed at
children?"), never that the row is filled in without asking.

Record the answer as `General`, `Child-directed` or `Mixed`, marked as from the user. If the user
does not know, record `Unanswered`, propose for a general audience in Step 8 and say so, and say
that the answer is needed before Step 6.5 and before any app is created in Step 5.

## The readout

Open with one line saying the profile was built from the project's files without changing
anything. Right after that line, and before the table, show this notice exactly as written. Do not
reword, shorten, summarize, or translate it. Show it the first time the profile appears in a
conversation.

Please read everything carefully. You are solely responsible for reviewing, validating, and accepting any actions taken or content produced as a result of these interactions. You are responsible for the results of any automated action initiated through this skill, including changes to campaigns, budgets, bids, or spend, and any associated financial impact.

Show the profile as one short table before you ask anything, so the user can correct it in one
reply. Seven rows, in this order, each with its evidence:

| Row | What it holds |
|---|---|
| Game type | The closest genre, and the scenes or classes it came from |
| Session length | The estimate, and what it was derived from |
| Key moments | Each moment as `Class.Method()` at `file:line`, alternates after; the button found or missing for each rewarded moment |
| Economy | Currency, lives, continue, shop, or "score only, no currency" |
| Existing monetization | Each package or SDK found, by name, or "none found"; each ad call found, with its trigger, and whether it breaks a guard |
| Target platforms | Android, iOS, or both, with the evidence; other targets named as not covered |
| Audience | "Asked below", with any signal the files show; the user's answer replaces it |

Keep it to the table and one line after it. A correction from the user replaces the row; restate
only the corrected rows.

## Confidence and how to phrase it

Name the evidence instead of hedging: "about 30-second runs, from `ObstacleSpawner` and the restart
loop". That invites a correction rather than making the user feel they are contradicting you, and a
wrong inference is then obviously wrong rather than mysteriously wrong.

Do not hedge every row. A row read straight out of `ProjectSettings.asset` or the lock file is a
fact and reads like one.

## When the project tells you nothing

A very early prototype may have one scene, one script and no loop. Say that directly in the readout,
then ask at most these three questions in one message, before the goal question, because they are
the only facts the proposal cannot do without:

1. "What does one play session look like: runs, levels, matches, or something else, and roughly how
   long?"
2. "Where does a run or level end, and where can the player fail?"
3. "Does the game have a currency, lives, or hints a reward could give?"

Use the answers in place of the missing rows, marked as from the user. Do not invent scripts,
methods or UI to fill the gaps, because Step 9 would wire ads to code that does not exist. If the
user cannot answer either, Step 8 names a single opt-in rewarded placement as **deferred**: no code
location, no reward, and not part of what the user approves for implementation, because Step 9 needs
a press handler and the game's reward method and neither exists yet. Say that the proposal resumes
once the game has a run or level end and a button a player can press, and stop there; do not ask
for implementation approval of a deferred placement.
