# Multiplayer Play Mode (MPPM) testing

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

## Contents

- [Overview](#overview)
- [Availability — probe first](#availability--probe-first)
- [Commands](#commands)
- [Clones: drive the game, never the project](#clones-drive-the-game-never-the-project)
- [Netcode commands](#netcode-commands)
- [Who hosts, and how clients join](#who-hosts-and-how-clients-join)
- [Dedicated server: Multiplayer Roles](#dedicated-server-multiplayer-roles)
- [Running a host + client test](#running-a-host--client-test)
- [Network conditions in clones](#network-conditions-in-clones)
- [Final testing with built players](#final-testing-with-built-players)
- [Notes](#notes)

## Overview

Verifying netcode needs two or more player processes in the same project. **Multiplayer Play Mode (MPPM)** runs them as *virtual players* — clone Editors of the main project that enter Play mode together and run the **same build**, so it is the closest local stand-in for separate machines (host + client).

A **Play Mode Scenario** is a project asset (default `Assets/Settings/PlayMode/<name>.asset`) that declares the roster: the Main Editor plus any `clone` (virtual-player Editor) or `local` (built player) players. You author a scenario, make it active, then start it — which enters Play mode and launches every declared player.

The `com.unity.pipeline` package exposes all of this as `editor.playmode` CLI commands, invoked as `unity command editor.playmode <command>`, so the roster, the run, and each instance's state can be driven and read without a human clicking. Running these commands — installing the package, keeping the Editor ticking (`editor set_auto_tick --enabled true`), auth — is the **unity-cli** skill's job; this page covers only the multiplayer-specific commands and flow.

## Availability — probe first

A scenario runs inside the live Editor, so first confirm one is connected, then probe for the scenario commands. They exist only when the project has **`com.unity.multiplayer.playmode`** installed; otherwise they are compiled out. Command names have changed between releases, so list them first:

```bash
unity status --format json             # a connected Editor reports state: ready; if none, open the project first
unity command editor.playmode          # lists the group's commands
```

If `start_scenario` and the other `*_scenario` commands are absent, MPPM is unavailable — verify with standalone host and client players launched by hand, as in the main workflow, and record a gap in the report's Verification line only if that is also not possible. `list_instances` works without MPPM; the scenario and player commands need it. The command surface is the same for MPPM 2.x and 3.x; read the installed `com.unity.multiplayer.playmode` version from `packages-lock.json` when an API differs.

A missing command fails as "unknown command". When a name fails, re-list the group before debugging the name.

To see why, or to confirm which netcode package and version the project uses, read the installed packages rather than relying on the command listing alone:

```bash
unity command package list             # is com.unity.multiplayer.playmode present? which netcode package and version?
```

## Commands

All are `unity command editor.playmode <command>`. They split into authoring the roster, running it, and discovering the live instances.

| Command | Does |
|---|---|
| `list_scenarios` | List the Play Mode Scenario assets in the project. |
| `create_scenario --name <N> [--path <asset>]` | Create a scenario (auto-adds the Main Editor player). Path defaults under `Assets/Settings/PlayMode/`. It is **not active**: run `set_active_scenario` next, because `add_player` and `start_scenario` act on the active scenario. |
| `list_players [--path <asset>]` | The players a scenario declares, with the ids `remove_player` takes. Defaults to the active scenario. Use `list_instances` to find running clones. |
| `add_player --type clone\|local [--name <N>] [--path <asset>]` | Add a player to the active scenario (or `--path`) — **state must be Idle**, so add players before `start_scenario`. `clone` = a virtual-player Editor (at most 3); `local` = a built player. MPPM renames clones to `Player 2`, `Player 3`, … when it reloads the scenario, so don't rely on `--name`. It can't set a player's tags or role. |
| `remove_player --id <id> --confirm true [--dry-run true] [--path <asset>]` | Remove a player (destructive; `confirm` required, `--dry-run` previews). |
| `set_active_scenario --path <asset>` | Make a scenario active — **Idle only**. |
| `start_scenario` | Enter Play mode and launch the active scenario's players; with none set it runs the default scenario, the main Editor alone. Returns immediately; players boot **asynchronously**. |
| `get_scenario_status` | Live scenario state plus one entry per declared player: `state`, `reachable`, and once up, `port`, `vpId`, `projectPath`. |
| `stop_scenario` | Exit Play mode and stop the players; state settles asynchronously. |
| `list_instances` | Every Editor reachable over the pipeline — the main Editor plus each live clone — with the `projectPath` and `port` to target it directly. |
| `set_run_in_background --enabled true` | Keep an unfocused Editor's Play mode updating at full rate. This Editor's session only: clones and builds use the project's own value. For a scenario, set that instead — see [Notes](#notes). |

Other pipeline commands the flow uses. **Where** each may run matters — see [Clones: drive the game, never the project](#clones-drive-the-game-never-the-project):

| Command | Runs on | Use in an MPPM test |
|---|---|---|
| `script recompile`, `script recompile_status` | Main Editor only | Compile before starting the scenario, so compile errors surface before players launch. |
| `test run --mode playmode` | Main Editor only, scenario stopped | Run netcode Play mode tests. It enters and exits Play mode itself, which MPPM owns on a clone. |
| `editor status` | Any instance | A clone is ready when its own status reports `playMode: playing`. |
| `console get [--level error]` | Any instance | Read an instance's console (errors and the packages' connect messages). Bare `console` is only the group name. |
| `script eval --code "<C#>"` | Any instance | Read live state the [netcode commands](#netcode-commands) don't show, or call a test hook. |
| `job wait --condition '<json>' --tolerate-missing true` | Any instance | Wait until a condition holds (client connected, ghost spawned) instead of polling with `script eval`. Add `--async true` (then `job wait_status`) when the condition depends on a command you still need to send: a sync wait holds that instance's command queue. |

## Clones: drive the game, never the project

A clone runs out of `<project>/Library/VP/<vpId>`, a mirror of the main project with a **read-only asset database**. Its running game — loaded scene, live objects, memory — is its own; its project is not.

| On a clone | |
|---|---|
| **Runtime state** — reading live objects, console, screenshots, input, calling methods on the running game | Fine. This is what a virtual player is for. |
| **Project state** — creating, editing, importing or saving assets and scenes, settings, packages, recompiles | Never. Do it on the main Editor. |
| **Entering or exiting Play mode** | Never. MPPM drives each clone's Play mode from the scenario; use `start_scenario` / `stop_scenario` on the main Editor. |

Nothing enforces this: a project write sent to a clone is accepted, then fails or lands on the mirror and surfaces later as a desync rather than an error. When a fix needs a project change, stop the scenario, make the change on the main Editor, and start the scenario again.

## Netcode commands

Both netcode packages add their own `unity command` groups. Use them instead of `script eval` wherever they cover what you need, because they read the running worlds without adding code to the project and name the fields to compare. List a group before using it (`unity command netcode`, `unity command netcode.gameobjects`), because groups and commands differ between package versions; when a group or command is missing, fall back to `script eval`. Each command's description comes with the listing; NGO's are also on the NGO `unity-cli-commands` page.

| Need | Unity Netcode | NGO |
|---|---|---|
| Setup problems, before Play mode | `netcode audit` | `netcode.gameobjects audit` |
| Start a role without a session | No command: turn the automatic bootstrap off (`netcode.config set --field enable_client_server_bootstrap --value DisableAutomaticBootstrap`), then `script eval` `Netcode.StartAsHost` on the main Editor and `Netcode.Connect` on each clone. Play Mode Type (`netcode.playmode get_settings`) is shared by the main Editor and its clones, so it can't give them different roles | `netcode.gameobjects.session start --mode Host` (main Editor) or `--mode Client` (clone) |
| Wait until clients are connected | `netcode.session wait_ready --min_clients <n>` | `netcode.gameobjects.session wait_ready` |
| Connection state and RTT | `netcode get_connection_state` | `netcode.gameobjects status`, `list_clients` |
| Replicated state on this instance | `netcode.ghost list`, then `netcode.ghost get --ghost_id <id>` (shows `GhostField` values) | `netcode.gameobjects list_objects`, then `get_object` (ownership and behaviours only; read `NetworkVariable` values with `script eval`) |
| Disconnect and rejoin | `netcode.session disconnect`, `reconnect`, `kick` | `netcode.gameobjects.session kick`, `reconnect` |
| Prediction cost and mispredictions | `netcode.profiler get_rollback_metrics` on a client | Not applicable (no prediction) |
| Connect and driver log lines | `netcode.debug set_log_level` to Debug, then `console get` | `netcode.gameobjects.config set --field log_level` |
| Network conditions | `netcode.simulator` | `netcode.gameobjects.simulator` |

`netcode.ghost diff` and `netcode.tracing` compare client and server worlds inside one process, so they don't work across MPPM instances: run `netcode.ghost get` on the host and on each clone and compare the fields instead.

## Who hosts, and how clients join

Starting a scenario only launches the processes. Every instance runs the same code, so nothing decides on its own which one hosts and which ones join — decide it explicitly.

Drive the roles from outside, through the pipeline: the main Editor hosts, each clone joins.

- **Netcode only (no session):** for NGO, start the roles with the [netcode commands](#netcode-commands). For Unity Netcode, start them with `script eval` calls to the `Netcode` facade, as that table says, because no command starts a role.
- **Through session connectors** (the game's create and join run on `SessionConnectorBehaviour`s): no hook class, because the game already has the entry points. Once every instance reports `playMode: playing`, run `unity command services.multiplayer run_session_connector --game-object "<host behaviour's GameObject>"` on the main Editor (a GameObject name, or its `Parent/Child` path when the name repeats) and read the join code from the session it returns, then run it on each clone with `--instance <clone> --game-object "<join behaviour's GameObject>" --join-code <code>`. If the group has no `run_session_connector`, call what the buttons call with `script eval` instead: the host behaviour's `Execute()` on the main Editor, `list_sessions` for the code, then `SessionConnector.WithJoinByCode` and the join behaviour's `Execute()` on each clone. Pass when `list_sessions` on every instance shows the same session `Id`, `IsHost` true only on the main Editor, and `PlayerCount` equal to the number of instances.
- **Through session code** (create, then join by code, written against the Session API): the netcode commands can't create or join a Multiplayer Services session, so add a small test-only hook class to the project (on the main Editor, before starting the scenario), and remove it when verification is done, because verification code must not stay in the user's project. It exposes, for example:

- `StartHost()` — creates the session and stores its join code in a static `JoinCode`,
- `Join(string code)` — joins that session,
- static `HasJoinCode` for `job wait`.

Then, once every instance reports `playMode: playing`, start the host on the main Editor, read `JoinCode` back, and pass it to each clone (step 6 below).

MPPM player tags (`CurrentPlayer.Tags`, or `CurrentPlayer.ReadOnlyTags()` on MPPM 2.x; always empty outside the Editor) are the in-project alternative, but `add_player` can't set tags, so a scenario built from the CLI has none.

Two consequences of clones being separate processes:

- **Each clone signs in as its own anonymous player**, because it has its own data path. Host and clients get distinct player ids without any extra setup.
- **Services only progress in Play mode.** Sign-in, session and Cloud Code calls advance only while the scenario is playing, so issue them after the instances are ready.

## Dedicated server: Multiplayer Roles

When step 3 chose a dedicated server, run the main Editor as the server and the clones as clients, so each process strips what its build would. Multiplayer Roles decide this: each GameObject or component is kept for some roles and stripped from the others, and each Editor plays as one role. Use the `multiplayer.role` and `settings.multiplayer_roles` commands, not `script eval` over the roles API. The group needs Unity 6000.6+, so run `unity command multiplayer.role` first and check that it is listed.

Before you set roles, and again if a step below fails, read the version-matched pages, because what a role strips and where a scenario sets it can change between releases. Read them the way the skill's Documentation section describes for packages: the installed package's `Documentation~` first, otherwise `https://docs.unity3d.com/Packages/<package>@<VERSION>/manual/<page>.html`.

- Dedicated Server (`com.unity.dedicated-server`): `multiplayer-roles`, `content-selection`
- Multiplayer Play Mode (`com.unity.multiplayer.playmode`): `play-mode-scenario/play-mode-scenario-configurations`, `mppm-reference/play-mode-scenario-window-reference`

First check that `Packages/manifest.json` lists the Dedicated Server package, `com.unity.dedicated-server`. The Editor turns on its Multiplayer Roles features, including content selection and the role field, only while that package is installed, so without it the steps below strip nothing. If it is missing, ask the user, then add it through the **unity-package-management** skill.

1. Turn roles on, because nothing is stripped and no scenario applies a role until they are:

   ```bash
   unity command settings.multiplayer_roles get
   unity command settings.multiplayer_roles set --settings '{"enabled":true}' --confirm true
   ```

2. Mark what each role keeps. `role` is `client`, `server` or `client_and_server`:

   ```bash
   unity command multiplayer.role set_game_object --target <go> --role server                     # the GameObject
   unity command multiplayer.role set_component --target <go> --type AudioSource --role client   # one of its components
   ```

   Don't run `multiplayer.role set_build` for a Play Mode test. It changes the role the active build profile builds for and nothing restores it, so the next client build would strip its client-only objects. Use it only when the user asks for a server build. That build also needs the target platform's dedicated server module; see [Final testing with built players](#final-testing-with-built-players).

3. Give each scenario player its role. In a Play Mode Scenario every player's role comes from the scenario, the main Editor included, and every player defaults to `client`. The scenario applies those roles when it starts and restores the main Editor's own role when it stops, so `multiplayer.role set_active` does not change a scenario run. Clones are therefore clients already; make the main Editor the server by writing its role into the scenario asset while the scenario is Idle:

   ```bash
   unity command editor.playmode list_players --path <scenario.asset>   # rows are in stored order: row N (from 0) is item N; find the row whose type is main
   unity command component set_field --target <scenario.asset> --field "m_Settings.m_InstanceItems.Array.data[<N>].m_Settings.RoleMask" --value Server
   unity command editor save_project                                     # set_field changes the asset in memory; this writes it to disk
   ```

   The value is a bare enum name, `Client`, `Server` or `ClientAndServer`; a quoted `'"Server"'` is refused. `component get_fields` can't show this field, so check the result with step 5. The field path is the scenario's internal layout, so if `set_field` reports it not found, ask the user to set **Multiplayer Role** to **Server** for the Main Editor in the Play Mode Scenario window instead, and say that the main Editor would otherwise run as a client. If `editor save_project` isn't listed, ask the user to save the project. Then tell the user the scenario asset now runs the main Editor as the server, and ask whether to keep that or set it back to `Client` after the test, because the change is in a project file their next commit will pick up.

   `list_players` leaves out a broken player entry, such as one left by a removed package, so its rows no longer match the stored items. If the role check in step 5 reads `client` on the main Editor after the write, use the Play Mode Scenario window. `editor save_project` also writes every other unsaved asset and setting in the project. If the user has unsaved changes of their own, ask before running it.
4. Without a scenario, plain Play mode runs the main Editor alone, as the role `unity command multiplayer.role set_active --role server` sets. Run `set_active` in Edit Mode, because it refuses while playing.
5. After `start_scenario`, check each instance before driving it, because a wrong role fails later as a missing object rather than an error: `unity command multiplayer.role get_active` on the main Editor reads `server`, and `unity command --instance <clone> multiplayer.role get_active` reads `client`.

Then start a server, not a host, on the main Editor in step 6 of the run below, and join from each clone as before.

## Running a host + client test

Drive these through the **unity-cli** skill.

Before the run, check how to address a clone, because the flag depends on the installed CLI. Run `unity command --help`: if it lists `--instance`, address a clone with `--instance <clone>`, the name or `vpId` that `list_instances` reports for it once the scenario runs, because MPPM can rename clones when it reloads the scenario, so the name you gave `add_player` may no longer match. If it doesn't, the CLI predates the flag: replace every `--instance <clone>` below with `--project-path <clone projectPath>`, taking the path from `list_instances`.

A minimal host-plus-one-client run:

```bash
unity status --format json                                      # 0. a connected Editor reports state: ready
unity command --help | grep -- --instance                       #    no match: use --project-path <clone projectPath> wherever --instance <clone> appears below
unity command editor.playmode                                   # 1. probe; use standalone host/client players if the *_scenario commands are missing
unity command editor.playmode list_scenarios                    # 2. reuse an existing scenario if one fits
unity command editor.playmode create_scenario --name NetTest    # else make one (Main Editor player is added for you)
unity command editor.playmode set_active_scenario --path Assets/Settings/PlayMode/NetTest.asset   # create_scenario does not make it active
unity command editor.playmode add_player --type clone --name Client1   #    add a client to the active scenario — must be Idle
unity command editor.playmode get_scenario_status              #    confirm active is NetTest and isDefaultScenario:false (the default runs the main Editor alone)
unity command script recompile                                  # 3. add the host/join hooks if the test goes through session code (not connectors), compile on the main Editor, confirm no errors
unity command script recompile_status
unity command netcode audit                                     #    or netcode.gameobjects audit: fix what it reports before starting
unity command settings.player get                               #    run-in-background must be true: every instance needs it, and clones read only the project's value
unity command settings.player set --settings '{"run-in-background":true}' --confirm true   #    if it is false; see Notes
unity command editor.playmode start_scenario                    # 4. started:true means accepted, not ready; a clone's first launch can take a minute or more
unity command editor.playmode list_instances                    # 5. poll until each clone is listed; note its name or vpId as <clone>
unity command --instance <clone> editor status  #    then until that clone reports playMode: playing and runInBackground: true (no clone after ~2 min: see Notes)
# 6. assign roles, then drive each instance's running game by its own instance name:
unity command script eval --code "MyGame.NetTestHooks.StartHost(); return true;"      # main Editor hosts (dedicated server: a StartServer() hook instead, see Dedicated server: Multiplayer Roles)
unity command job wait --condition '{"member":"MyGame.NetTestHooks.HasJoinCode","op":"equals","value":true}' --timeout 60
unity command script eval --code "return MyGame.NetTestHooks.JoinCode;"              # read the join code
unity command --instance <clone> script eval --code "MyGame.NetTestHooks.Join(\"<code>\"); return true;"   # clone joins
unity command netcode.session wait_ready --min_clients 1        #    host waits for the client (NGO: netcode.gameobjects.session wait_ready)
unity command --instance <clone> netcode get_connection_state   # the clone's connection, endpoints and RTT (NGO: netcode.gameobjects status on the clone, list_clients on the host)
unity command --instance <clone> console get    #    check the client's console for errors
unity command netcode.ghost list                                #    find the ghost id on the host (NGO: netcode.gameobjects list_objects)
unity command netcode.ghost get --ghost_id <id>                 #    the host's replicated fields
unity command --instance <clone> netcode.ghost get --ghost_id <id>   # the same ghost on the clone; compare
unity command editor.playmode stop_scenario                    # 7. stop; mutating the roster again needs Idle, so stop first
```

Find clones with `list_instances`, because it reports each running clone's name, `projectPath` and `port` directly. Don't gate readiness on a player's `state` either: it can stay at `Launching` for the whole run while the clone is up and playing.

`NetTestHooks` and its members are placeholders for the hook class described in [Who hosts, and how clients join](#who-hosts-and-how-clients-join). With session connectors, replace the three hook lines with the connector steps there. Without a session, replace the three hook lines with the role commands in [Netcode commands](#netcode-commands).

**Assert parity (invariant 9):** what worked in one process holds across instances — the host's authoritative values and the clients' replicated ghosts agree, and late-join and disconnect behave. Read the same ghost or object on the host instance and a client instance with the [netcode commands](#netcode-commands) and compare the fields, using `script eval` only for values they don't show, because reading live state needs no logging code that would have to be removed afterwards.

## Network conditions in clones

Test latency, jitter and packet loss in the clones throughout development, not in a late pass, because both netcode stacks' emulators run in the Editor and so in every clone. Read the stack's page before configuring it:

- Unity Netcode: the PlayMode Tool network emulator, opened in each clone (*Use the Playmode Tool window with Multiplayer Play Mode* on the Netcode `testing/playmode-tool` page). From the CLI, the `netcode.simulator` group (`list_presets`, `set`, `get`, `lag_spike`, `timeout`) sets it per instance through `--instance` (or `--project-path`); it applies live to client worlds in Play mode.
- NGO: the Multiplayer Tools Network Simulator (NGO `tutorials/testing/testing_with_artificial_conditions` page), or the `netcode.gameobjects.simulator` group with the same commands (its `list_presets` needs Multiplayer Tools installed; `set` with explicit values works without it). A hybrid session's GhostObject traffic needs `netcode.simulator` instead.

List the group first (`unity command netcode.simulator`), because command names change between releases. Set conditions on the clients, then confirm the measured RTT rose before trusting the run.

## Final testing with built players

Once the work passes in the clones, add built players to the same scenario as local instances (`add_player --type local`) for what clones cannot show: build-only code and stripping, target platforms and devices (MPPM local instances include Android devices), release-build network conditions, a run over the real transport or Relay path, and anything the user needs as a shippable build. The scope and order are in the verification gate's *Final testing* section; the MPPM docs describe local instances as the step closer to the final build (`instance-types/local-instance` in `https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@<VERSION>/manual/`, with `<VERSION>` the installed `com.unity.multiplayer.playmode` major.minor, or the package's `Documentation~`).

A dedicated-server local instance, or any server build, needs the dedicated server module for its platform, because the scenario rejects a build profile whose module isn't installed. The main Editor and clones need no module to play the server role. Check with `unity modules <editor version>` for `linux-server`, `windows-server` or `mac-server`. If the one you need is missing, ask the user before installing it, because it is a download of 0.5–2 GB, then run `unity install-modules --editor-version <editor version> --module <id>`.

## Notes

- Each clone is a separate Editor with **its own pipeline server** (own `port`, project under `Library/VP/<vpId>`), so you address it by `--instance <name>`, or by `--project-path` on a CLI without `--instance` (see [Running a host + client test](#running-a-host--client-test)). Never pass a name to `--project-path`. `list_instances` and the `get_scenario_status` players give the name, path and port.
- **Every instance needs Run in Background, and clones read only the project's value.** An unfocused instance without it stops its player loop, which looks like a netcode timeout, and a clone is almost always unfocused. `set_run_in_background` changes only the Editor it runs in, so turn the project's value on from the main Editor with `settings.player set` before `start_scenario`; a clone that is already running may keep the old value until it restarts. If `start_scenario` returns a `note` about Run in Background, act on it. If `settings.player get` has no `run-in-background` field, the installed pipeline cannot set it: turn it on in the main Editor with `unity command script eval --code "UnityEditor.PlayerSettings.runInBackground = true; UnityEditor.AssetDatabase.SaveAssets(); return true;"`.
- **Never cache a port.** Starting a scenario can move the main Editor's pipeline port, and it doesn't move back on stop. Re-read `list_instances` each time.
- `stop_scenario` exits Play mode. With MPPM's **Keep Active** setting on, the clone Editors stay running and reachable, which makes the next start fast. Close them from the Multiplayer Play Mode window to free the machine.
- If no clone appears in `list_instances` within about two minutes of `start_scenario`, stop the scenario and verify with standalone host and client players instead of waiting longer. Record a gap in the report's Verification line only if that is also not possible.
- Judge a clone's readiness by its own `editor status`, not by messages in the main Editor's console.
- The mutating commands (`add_player`, `remove_player`, `set_active_scenario`) are refused unless the scenario state is `Idle`; `stop_scenario` first.
- Before each `start_scenario`, check that `get_scenario_status` shows your scenario as `active`, because a plain `editor.playmode start` can reset it to the default scenario, which runs the main Editor alone.
- Don't hand-edit the scenario `.asset` YAML while the Editor is open. Change the roster through the `editor.playmode` commands so the running Editor stays in sync.
- **At most 3 clones**, so 4 players including the main Editor. For more, add built players with `add_player --type local`.
- MPPM shares the main project's assets and scripts across clones, so it exercises the **same** netcode code the host runs, and with the emulator on it covers behaviour under latency too. A multi-machine run over the real network, a dedicated-server build, and final performance numbers on target hardware belong to final testing, not to everyday verification.
- A clone joining the main Editor satisfies the verification gate's cross-process connect: the IPC driver works only inside one process, so a clone that connects, and has no server world of its own, used the socket driver; its console shows that selection only with the Netcode log level set to Debug. Build a player (`build set_profile`, then `build run`) and add it with `add_player --type local` only for final testing (see [Final testing with built players](#final-testing-with-built-players)).
- `runtime simulate_*` drives a clone's Game view as well as a development Player, but only while that instance's window has focus: an unfocused Editor drops the event and still reports success. Before simulating input on an instance, run `unity command --instance <name> editor focus`, then confirm the effect by reading the game's state, never by the command's result. When two instances need input in the same step, call a test hook through `script eval` instead, because only one window can have focus at a time. `runtime set_time_scale` works only on development Player builds. For network conditions in clones, use the netcode simulator commands in [Network conditions in clones](#network-conditions-in-clones).
