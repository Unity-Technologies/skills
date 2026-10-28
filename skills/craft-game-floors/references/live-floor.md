# Live Floor

> Craft reference for the `craft-game-floors` skill. **Load:** when wiring services — after player identity exists, verified with a standalone probe (`UnityServices.State == ServicesInitializationState.Initialized` and `AuthenticationService.Instance.IsSignedIn` with a non-empty `PlayerId`), never by assuming an earlier step ran. In the creation flow the canonical moment is [game-shell](game-shell.md) time, after the flow's auth setup. Service mechanics live in `build-live-game`; this reference owns which slice, in what shape, wired when. Relative links in this file resolve to the skill directory. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The opinion

A build without persistence, comparison, or an off-switch is a demo with a backend. The live floor is the smallest slice of UGS that makes the artifact a *live game*: the save the player never sees, the rank the end screen shows, and the lever you can pull at 2am. All three are free-tier, invisible or nearly so, need zero genre awareness, and compose directly on the anonymous PlayerId — which is why identity precedes this floor.

**Identity precondition.** Every piece below keys on the PlayerId from the project's auth setup — run the probe in the load line above rather than trusting any prior session's record. If the probe fails (cloud link failed, sign-in skipped, or never set up), skip the floor and state its cost plainly: no persistence beyond PlayerPrefs, no rank, no remote lever — reduced scope, never UGS calls against a project with no cloud id. **Anonymous identity is device-local**: [`SignInAnonymouslyAsync`](https://docs.unity.com/en-us/authentication/use-anon-sign-in.md) creates a fresh account where no cached credentials exist — a reinstall can clear that cache, so the save is only as durable as the device's sign-in cache and never follows the player to a second device. Say "persists on this device while you stay signed in" in any user-facing claim, and name account linking (the graduation below) as what unlocks cross-device.

## 1 · Cloud Save — progression persists by default

`PlayerPrefs` is the default-gray of save systems: device-bound, gone on reinstall. Floor shape: one [Cloud Save](https://docs.unity.com/en-us/cloud-save.md) slot (`progress`) holding the progression state the game already tracks (level, XP, collected, settings), written on milestone and on quit, read at boot. **Deliberate deviation:** `build-live-game`'s validation rule routes sensitive writes (XP, scores, currency) through Cloud Code; the floor is client-authoritative *on purpose* — zero server code at creation time — and the server-authoritative shape in Expansion scope is the graduation, not an alternative reading. Offline-safe: cache locally, sync when the service answers.

## 2 · Leaderboard — derived from the game's progression design, never from genre

The game's own design names its milestone/score moment — the point where the loop calls progress (a level completed, resources banked, a run scored). **That event is the leaderboard's oracle**: if the progression design defines a score-shaped milestone, create one [leaderboard](https://docs.unity.com/en-us/leaderboards.md) from it, submit on that milestone, and show rank on the game shell's end screen ([game-shell](game-shell.md)'s stats block gains one line). No score-shaped milestone in the design, no leaderboard — the rule stays genre-blind. Anonymous players get generated display names ("Otter-7f2a" style), which defers the name-moderation question past the floor.

## 3 · The kill switch — one Remote Config flag

`liveops_enabled` (bool, default true) in [Remote Config](https://docs.unity.com/en-us/remote-config.md) gates the live features: save sync, leaderboard submit/read. Fetched at boot **and re-fetched on a timer or at natural seams** (scene load, app focus) — a boot-only cache means the 2am toggle can't reach sessions already running, which defeats the lever. One flag, not a config schema — it exists so a live problem is a toggle, not a hotfix.

## Wiring order

Identity → *(the loop and feel passes)* → **live floor wires with the shell**: rank belongs on the end screen and save-on-quit belongs to the shell's state machine, so this reference applies at [game-shell](game-shell.md) time, not before. Verify before done, each check naming its call: write the save and read it back (`CloudSaveService.Instance.Data.Player` save then load — the loaded value must equal the written one); submit a score and read the rank back (`LeaderboardsService.Instance` add-score then get-player-score — rank non-null); flip `liveops_enabled` in Remote Config's web console, wait past the re-fetch interval (or force a re-fetch), and assert the gated calls stop — a flip can only reach a running session through the re-fetch above, so this check also proves the re-fetch works. The flag flip is the one human-gated check — in a headless session, assert instead by eval that every live call path reads the flag first and that the re-fetch path exists.

**Provisioning floor (the honest headless line):** creating the leaderboard and Remote Config resources needs the Deployment window or a service-account key — a human gate this floor never consumes. A fully headless run wires the code paths, verifies the Cloud Save round-trip for real, and lets rank and the flag degrade gracefully ("Rank: unavailable"; flag defaults on) — naming leaderboard and Remote Config provisioning as the first human-session deploys, the same pattern as the audio floor's mixer authoring.

## What the floor deliberately excludes

Cloud Code (infrastructure, not an outcome), **Economy** (check [its documentation](https://docs.unity.com/en-us/economy.md) for availability before ever choosing it — never default a new game onto a service whose docs say it cannot accept one; its documented replacement for new projects is a server-authoritative Cloud Code + Cloud Save + Remote Config setup, exactly the graduation shape below), matchmaking/multiplayer, Vivox, achievements/battle passes. All prompt-routed via `build-live-game` when asked for.

## Expansion scope — the server-authoritative shape

When the leaderboard starts mattering or money enters, the floor graduates to `build-live-game`'s validated shape: a Cloud Code module owns every write (save becomes a server ledger, scores are validated server-side, the client is never trusted). Scar tissue that graduates with it: pin the Cloud Code package to the stable line, never the experimental major; keep the kill switch in the release-config template so no build ships without the lever.
