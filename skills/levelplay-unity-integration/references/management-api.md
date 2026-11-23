# LevelPlay Management API: Create the App and Ad Units

How the agent finds or creates the user's LevelPlay app and ad units through the LevelPlay
Management API, so the user gets the App Key (Step 7) and the ad unit ids (Step 9) without clicking
through the dashboard. Everything here is the user's choice at Step 5; the dashboard path in
the workflow stays available and gives the same result.

## Contents

- [When this runs](#when-this-runs)
- [Credentials, and how the agent handles them](#credentials-and-how-the-agent-handles-them)
- [Calls](#calls)
- [Answers that are not errors, and errors](#answers-that-are-not-errors-and-errors)
- [Procedure](#procedure)
- [What this does not do](#what-this-does-not-do)

Read the API docs before the first call, and again if a call answers in a way this file does not
describe, because the docs are the source of truth for fields and limits:
[API overview](https://docs.unity.com/en-us/grow/levelplay/platform/api.md),
[authentication](https://docs.unity.com/en-us/grow/levelplay/platform/api/authentication.md),
[applications](https://docs.unity.com/en-us/grow/levelplay/platform/api/application.md),
[ad units](https://docs.unity.com/en-us/grow/levelplay/platform/api/ad-units.md).

## When this runs

The app and the ad units are needed at different steps, so this runs in two parts:

- **At Step 5 (the app):** procedure steps 1–3, then 5–6 for the app only. This gives the App Key
  that Step 7 needs.
- **After the Step 8 proposal is approved (the ad units):** procedure steps 1–2, then 4–7. The
  approved proposal decides which ad units the game uses. The only units that can exist before it
  are the defaults LevelPlay creates with a new app, and the Step 5 table lists those.

When the user already has an app, the first part creates nothing; it only finds the App Key.

- **Account resources only (Step 0 route):** the same two parts, back to back, each with its own
  confirmation. The app name, platforms, audience answer, formats and rewards come from the user's
  request instead of a profile or a proposal. First the app part: confirm, create, then list the
  new app's default ad units, because they do not exist until the app does. Then the ad unit part:
  reuse the defaults by format and confirm only what is still missing, so no unit is created
  before it has been shown in a table.

## Credentials, and how the agent handles them

The API takes a **secret key** and a **refresh token**, which the user copies from the LevelPlay
dashboard (https://platform.ironsrc.com/) under **My Account → API**. The API user needs the
**Add & edit apps** permission, because without it every data call answers 403.

Rules, each with its reason:

- **The user sets two environment variables, `LEVELPLAY_SECRET_KEY` and `LEVELPLAY_REFRESH_TOKEN`,**
  in the shell the agent runs in, or in a file outside the project that is sourced. They give full
  control of the user's LevelPlay account, and an environment variable keeps them out of the
  conversation and out of the project.
- **Never ask the user to paste either value into the conversation.** A conversation can be logged,
  shared or sent to a model provider; the user would have to rotate the credentials afterwards.
- **If the user pastes a key or token anyway, do not use it and do not repeat it.** Tell them it is
  now in the chat log, so they should rotate it in the dashboard (**My Account → API**), and set up
  the new values as described below. A beginner may paste despite the warning; the rule exists for
  that case, not against it.
- **Never print, echo or log them,** and never run `curl -v`, `set -x` or `env` while they are set,
  because each of those writes the values or the request headers to the output you read and the
  user's terminal history.
- **Never write them into any project file:** no `.env` in the project, no config, no commit, no
  generated C#. The project is shared and shipped; a key in a build is readable by anyone with the
  build.
- **Reference them only by name in commands** (`$LEVELPLAY_SECRET_KEY`, `$LEVELPLAY_REFRESH_TOKEN`),
  so the values exist only in the shell's memory.
- **Keep the bearer token in a shell variable (`TOKEN`) and never print it.** It grants the same
  access as the credentials for 24 hours.
- **Pass secret headers to `curl` through its config on standard input (`-K -`), never as `-H`
  arguments.** A command's arguments are visible to every user on the machine through `ps`, while
  `printf` is a shell builtin and a pipe is private to the two processes.
- **The `appKey` and ad unit ids are not secrets** and may be shown: they ship inside the game and
  are visible to anyone who unpacks it.

Check the variables without revealing them:

```sh
test -n "$LEVELPLAY_SECRET_KEY" && test -n "$LEVELPLAY_REFRESH_TOKEN" && echo "credentials set" || echo "credentials missing"
```

If they are missing, tell the user, then stop until they say it's done. Offer the file first,
because it needs no restart and no terminal:

1. In the [LevelPlay dashboard](https://platform.ironsrc.com/), open **My Account → API** and copy
   the secret key and the refresh token; check the API user has **Add & edit apps**.
2. **macOS / Linux, recommended:** put these two lines in a plain-text file **outside the project**,
   for example `~/.levelplay-env` (TextEdit works: Format → Make Plain Text), and tell the agent the
   file's path, not its contents:
   `export LEVELPLAY_SECRET_KEY='...'` and `export LEVELPLAY_REFRESH_TOKEN='...'`.
   The agent then sources the file by path at the start of every command (`. ~/.levelplay-env`), so
   nothing needs restarting and the values never appear in the conversation.
3. **macOS / Linux, alternative:** add the same two lines to the shell profile (`~/.zshrc` or
   `~/.bashrc`). This only takes effect in a new shell, so the user then opens a new terminal **or
   restarts the agent** — say this as its own step, because an agent started before the edit cannot
   see the values.
4. **Windows:** search the Start menu for **"Edit environment variables for your account"**, add
   `LEVELPLAY_SECRET_KEY` and `LEVELPLAY_REFRESH_TOKEN` under **User variables**, then restart the
   agent so its shell picks them up.
5. Reply "done" here, without the values (with the file's path when they chose the file).

Some agent tools start a fresh shell for each command, so a variable set in one command is gone in
the next. If `TOKEN` is empty in a later command, put the auth block at the start of that same
command. When the user keeps the credentials in a file, source it by path at the start of every
command (`. ~/.levelplay-env && …`) and never `cat` it; the check above becomes
`. ~/.levelplay-env && test -n "$LEVELPLAY_SECRET_KEY" && …`.

## Calls

**Docs first.** Before building any request, open the linked documentation page for that call
(authentication, applications, ad units) and take the request fields, response shape, token
lifetime and rate limits from there; this file keeps only the credential-safe transport, the
confirmation procedure, and the behaviours we observed live that the docs do not state (the
"No records found" empty list, platform casing, an empty create body). When this file and the docs
disagree on a field or a limit, the docs win; when the docs are silent, the observed behaviour here
applies.

JSON is parsed with `python3 -c`, because it is on most development machines; use `jq` instead if
`command -v jq` finds it. Response bodies go to a file under `${TMPDIR:-/tmp}`, outside the project,
so nothing lands in the user's repository; each call makes its own file with `mktemp` and removes
it after parsing, so two runs on the same machine never read each other's account data. They hold app names and ids only, never credentials.

**Authenticate** (token valid 24 hours):

```sh
TOKEN=$(printf 'header = "secretkey: %s"\nheader = "refreshToken: %s"\n' \
    "$LEVELPLAY_SECRET_KEY" "$LEVELPLAY_REFRESH_TOKEN" \
  | curl -sS --fail -K - "https://platform.ironsrc.com/partners/publisher/auth" \
  | python3 -c 'import json,sys; t=sys.stdin.read().strip(); print(json.loads(t) if t.startswith("\"") else t)')
test -n "$TOKEN" && echo "auth ok" || echo "auth failed"
```

Every other call sends `Authorization: Bearer $TOKEN` the same way: each example starts with its
own `printf … | curl -K -` pipe, so it works in a tool that opens a fresh shell per command, where
a function defined in an earlier command no longer exists.

**List apps:**

```sh
OUT=$(mktemp "${TMPDIR:-/tmp}/levelplay-apps.XXXXXX")
printf 'header = "Authorization: Bearer %s"\n' "$TOKEN" | curl -sS -K - -o "$OUT" -w 'HTTP %{http_code}\n' \
  "https://platform.ironsrc.com/partners/publisher/applications/v6?appStatus=active"
python3 -c 'import json,sys; d=json.load(open(sys.argv[1]))
if isinstance(d, list):
    [print(a.get("appKey"), a.get("platform"), a.get("appName"), sep=" | ") for a in d]
else: print(d)' "$OUT"
rm -f "$OUT"
```

**Create an app** (one per platform; `platform` is exactly `Android` or `iOS`; `coppa` is `1` for a
child-directed or mixed-audience app, `0` for a general audience). The body carries a name from the
project, so it goes through a file, never through the command line: write it with your
file-writing tool, as JSON, to a file outside the project in the directory that
`echo "${TMPDIR:-/tmp}"` prints, for example `levelplay-create-app.json`:

```json
{"appName": "Neon Runner", "platform": "Android", "coppa": 0}
```

Then check that the file is valid JSON, send it, and delete it:

```sh
BODY="${TMPDIR:-/tmp}/levelplay-create-app.json"
python3 -m json.tool "$BODY" > /dev/null && printf 'header = "Authorization: Bearer %s"\n' "$TOKEN" | curl -sS --fail-with-body -K - -X POST "https://platform.ironsrc.com/partners/publisher/applications/v6" \
  -H "Content-Type: application/json" \
  --data-binary @"$BODY"
rm -f "$BODY"
```

The response should include the new `appKey`; if the body is empty or has no `appKey`, run
**List apps** again and match the new app by name and platform. `--fail-with-body` needs curl 7.76 or later; on an older
curl, drop it and add `-w 'HTTP %{http_code}\n'` to see the status.

**List an app's ad units:**

```sh
OUT=$(mktemp "${TMPDIR:-/tmp}/levelplay-adunits.XXXXXX")
printf 'header = "Authorization: Bearer %s"\n' "$TOKEN" | curl -sS -K - -o "$OUT" -w 'HTTP %{http_code}\n' \
  "https://platform.ironsrc.com/levelPlay/adUnits/v1/APP_KEY"
python3 -c 'import json,sys; d=json.load(open(sys.argv[1]))
if isinstance(d, list):
    [print(u.get("mediationAdUnitId"), u.get("adFormat"), u.get("mediationAdUnitName"), u.get("reward"), sep=" | ") for u in d]
else: print(d)' "$OUT"
rm -f "$OUT"
```

**Create ad units** (the body is an array; `reward` only on a `rewarded` unit; names 1–255
characters). Same as for the app: write the body with your file-writing tool to a file outside the
project, for example `levelplay-create-adunits.json` in the same directory:

```json
[{"mediationAdUnitName": "Revive after death", "adFormat": "rewarded",
  "reward": {"rewardItemName": "Revive", "rewardAmount": 1}},
 {"mediationAdUnitName": "Between runs", "adFormat": "interstitial"}]
```

```sh
BODY="${TMPDIR:-/tmp}/levelplay-create-adunits.json"
python3 -m json.tool "$BODY" > /dev/null && printf 'header = "Authorization: Bearer %s"\n' "$TOKEN" | curl -sS --fail-with-body -K - -X POST "https://platform.ironsrc.com/levelPlay/adUnits/v1/APP_KEY" \
  -H "Content-Type: application/json" \
  --data-binary @"$BODY"
rm -f "$BODY"
```

The response may return the created units with their `mediationAdUnitId`, but in live use it has
also answered 2xx with an **empty body**, so never rely on it for ids: after a successful create,
run **List an app's ad units** again and take each new unit's `mediationAdUnitId` by name. The id
may be a number; write it into C# as a string, because the SDK constructors take a string.

Replace `APP_KEY` with the matched or created `appKey`. Never put names from the project inside a
shell command. The app and ad unit names come from the project's files, and a quote in a name
(`Bob's Game`) ends a quoted argument early, so the shell would read the rest of the name as
commands of its own and run them. The body file keeps the names out of the shell entirely, and
`json.tool` refuses to send a body that is not valid JSON.

## Answers that are not errors, and errors

| Answer | Meaning | Do |
|---|---|---|
| HTTP 404 with `{"code":404,"error":"No records found"}` on a list call | The account has no apps, or the app has no ad units | Treat as an empty list and continue; it is not a failure |
| HTTP 401 on `partners/publisher/*`, or HTTP 400 on `levelPlay/*` whose body gives error code 102 (prefixed `ERR`) | The bearer token expired | Authenticate again once and retry the call; if it fails again, stop and ask the user to check the credentials are still valid on the dashboard |
| Auth call fails | The secret key or refresh token is wrong or revoked | Stop; ask the user to copy fresh values into the environment variables, never into the chat |
| HTTP 403, or HTTP 400 whose body is `{"code":403,"message":...}` | The API user lacks **Add & edit apps** | Stop; ask the user to grant it under **My Account → API**, because retrying cannot succeed |
| HTTP 429 | Rate limit (4000 requests per 30 minutes) | Stop and tell the user to retry later; do not loop, because each retry counts against the limit |

## Procedure

1. **Check the credentials** with the `test -n` command above. If they are missing, give the user
   the setup steps from [Credentials](#credentials-and-how-the-agent-handles-them) and stop until
   they reply.
2. **Authenticate** and confirm `auth ok`. On failure, use the table above.
3. **Match the app for each target platform.** Take the target platforms the user confirmed at Step 4,
   because the Step 2 row is only an inference and the user may have chosen differently, and the
   product name from the Step 2 profile. List the active apps and match each platform by app name and platform. Reuse
   a match: show its `appKey`, because creating a second app for the same game splits its reporting.
   When more than one active app matches a platform, list them with their `appKey` and ask the user
   which one the game uses before reusing any, because name and platform alone do not identify it
   and the wrong key would send the game's traffic to another app.
   For each platform without a match, plan one app with the product name, and set `coppa` from the
   Step 2 Audience row: `1` for child-directed or mixed-audience, `0` for general audience. When
   that row is unanswered or the user came in through the account-only route, ask the question
   now. The user decides; this is a legal question the agent does not answer, so point to Step 6.5
   and `references/privacy-settings.md` (COPPA Compliance). A game on both platforms needs two
   apps with the same name, because one app serves one platform.
4. **Match the ad units** (after Step 8 approval). List the matched app's ad units, including any
   that LevelPlay created with a new app (one per format, status Off). For each format in the
   approved proposal, reuse an existing unit of that format, because a duplicate unit splits that
   format's reporting; name what already exists. For a rewarded unit, reuse only when its reward
   name and amount match the approved rule, because the SDK hands the dashboard's reward to the
   game's grant method and a `50 Coins` unit would pay out as coins where the proposal promised a
   revive; when the reward differs, plan a new rewarded unit for the placement and say why. Plan a
   new unit for a format with none. Name it with the proposal's placement name. Interstitial and banner placements of one format share
   its unit, so use the first placement's name and list the others beside it. Rewarded placements
   group by reward: placements with the same reward name and amount share one unit, and each
   distinct reward (`1 Revive`, `50 Coins`) gets its own unit, reused when a matching one exists,
   because a unit carries one reward definition and `OnAdRewarded` hands it to whichever placement
   showed the ad. For a rewarded unit, take
   `rewardItemName` and `rewardAmount` from the proposal's rewarded rule (for example `Revive`, `1`).
5. **Confirm before any create call.** Show one table of exactly what will be created, with one row
   per app (app name, platform, coppa) or ad unit (platform, app key, name, format, reward), so
   that on a two-platform game each unit row shows which app receives it. Under each app row, add
   that LevelPlay also creates one default ad unit per format with it, status Off, because those
   stay on the account too. Then ask:

   **"Create these on your LevelPlay account? They cannot be deleted through the API."**

   Proceed only on an explicit yes, because the API has no delete call and a wrong app stays on the
   account. If nothing needs creating, skip the question and say what was reused. If the user edits
   a row, show the changed table and ask again.
6. **Create, then hand the ids on.** Send only the confirmed rows, each call's body written to a
   file as described under [Calls](#calls), then list again (apps or ad units) to read the ids, because a create call can succeed with an empty body. Show the resulting
   `appKey` per platform and `mediationAdUnitId` per ad unit, and use them yourself: the `appKey` goes into
   Step 7's `LevelPlay.Init(appKey)` setup, each `mediationAdUnitId` into Step 9's ad constructors
   (`new LevelPlayRewardedAd(adUnitId)` and the others), so the user never copies them by hand.
   With more than one rewarded unit, Step 9's rewarded manager holds one `LevelPlayRewardedAd` per
   unit and each placement's show method uses its own unit's ad, so that unit's `OnAdRewarded`
   grants that placement's reward and never another's. On
   two platforms, choose each value with `#if UNITY_ANDROID` / `#elif UNITY_IOS`, because each
   platform's app has its own key and ad unit ids. This path creates no LevelPlay placements, so
   the ad code calls `ShowAd()` without a placement name and tracks which placement asked locally
   (set the manager's `currentPlacement` before the call and read it in `OnAdRewarded`), because a
   `ShowAd(placementName)` whose name is not configured on the dashboard has no placement behind
   it. If the user wants named dashboard placements (capping and pacing per placement), they create
   them first — see the [Placements API](https://docs.unity.com/en-us/grow/levelplay/platform/api/placements.md)
   — and only then does Step 9 pass the names.
7. **Tell the user what to check on the dashboard.** New apps are pre-launch and new ad units are
   created Off; turning them on and setting up ad networks and instances are dashboard steps. Do not
   promise fill, eCPM or revenue, because nothing created here predicts them; this is a setup, and
   the proposal stays a proposal.

## What this does not do

Each of these stays on the dashboard, because the procedure above only creates apps and ad units:

- Enable ad networks or bidding instances: see
  [Create ad units](https://docs.unity.com/en-us/grow/levelplay/platform/get-started/ad-units) and
  the network setup pages it links.
- Set up billing or payments.
- Change an app's store URL or take it live: see
  [Add your app](https://docs.unity.com/en-us/grow/levelplay/platform/get-started/add-app).
- Delete or rename an app or ad unit.
