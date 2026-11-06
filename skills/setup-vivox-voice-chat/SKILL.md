---
name: setup-vivox-voice-chat
description: Adds and configures in-game voice and text chat with Unity Vivox. Use when asked to add voice or text chat to a multiplayer game, such as team channels, proximity voice, mute controls or private messages.
required_packages:
  com.unity.services.vivox: ">=16.4.0"
---

# Unity Vivox — Voice & Text Chat

Namespace: `Unity.Services.Vivox` | Package: `com.unity.services.vivox`
Companion packages: `Unity.Services.Core`, `Unity.Services.Authentication`

Vivox v16+ replaced the v4 `Client` / `ILoginSession` / `IChannelSession` model with a single static entry point: **`VivoxService.Instance`**. All operations — init, login, channel join, messaging, muting — go through it. Do **not** use v4 patterns (`Client.Instance`, `AccountId`, `ChannelId`, `ILoginSession`, `UnityPurchasing.*`, etc.); those are gone in v16.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Run C# with `unity command script eval --code '<snippet>'`. Don't use `using` directives or unqualified types in the snippet. The compiler reads using `UnityEngine` and a bare AssetDatabase, Volume, or Object causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

## Step 1

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

## Full topic map

The Vivox docs pages are online `.md` pages on docs.unity.com. Use the [Unity Vivox curated documentation map](https://docs.unity.com/en-us/vivox-unity/llms.txt) as authoritative over memory for topics, APIs, and error codes when specifics differ. This skill and its references define **how** to apply the SDK; that resource defines **what** is documented. **Never** mention the `llms.txt` filename to the user. If it's unreachable, treat this skill's references plus the installed package at `<project-folder>/Library/PackageCache/com.unity.services.vivox@*/` as the source of truth. Read only the `.md` and `.cs` files in that specific `PackageCache` subfolder, because the package doesn't ship the full Vivox docs. You must not look outside at other Library or PackageCache folders, otherwise it takes too much time.

Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. The following Vivox pages have no version in the URL, so fetch them as listed.

- [Vivox Unity SDK quickstart guide](https://docs.unity.com/en-us/vivox-unity/vivox-unity-first-steps.md): Enable Vivox, initialize, sign in, join channels, send messages.
- [Initiate a channel join](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/initiate-channel-join.md): Join request code with `ChannelJoined` and `ChannelLeft` handlers.
- [Positional channel properties](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/positional-channel-properties.md): `Channel3DProperties` distances, fade intensity and fade model defaults.
- [Participant management](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/participant-events.md): Participant added, removed and state-change events in channels.
- [Directed messages](https://docs.unity.com/en-us/vivox-unity/developer-guide/messaging/directed-messages.md): Send and receive private messages between signed-in players.
- [Error Codes](https://docs.unity.com/en-us/vivox-unity/developer-guide/troubleshooting/error-codes.md): Vivox SDK error codes with a handling recommendation for each.

### API reference

The API reference URLs contain `<VERSION>`. Replace it with the major.minor version only, for example `6000.0` from `6000.0.58f1` in `ProjectSettings/ProjectVersion.txt`, or `16.9` for the package in `Packages/packages-lock.json`, because the full build number gives a 404.

The API is `Unity.Services.Vivox`, part of the `com.unity.services.vivox` package. It is versioned by package version, not Unity version (e.g. `@16.4`, `@16.9`), so check the project's installed package version before relying on a specific class or method.

1. Read the installed package version from `Packages/packages-lock.json`.
2. Fetch and cache `https://docs.unity3d.com/Packages/com.unity.services.vivox@<VERSION>/xrefmap.yml` (replace `<VERSION>` with the installed package version, e.g. `16.9`). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/Unity.Services.Vivox.IVivoxService.html` => `https://docs.unity3d.com/Packages/com.unity.services.vivox@<VERSION>/api/Unity.Services.Vivox.IVivoxService.html`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

Always fetch the version matching the installed package rather than relying on training knowledge, since APIs are frequently marked obsolete between major versions (e.g. the v4 `Client` and `ILoginSession` model is gone in v16).

The microphone permission API is `UnityEngine.Android.Permission`, part of the Unity Engine. It is versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder (e.g. `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)` => `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/rigidbody/addforce.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/vector3/ctor.md`), and indexers use `item` (e.g. `unityengine/vector3/item.md`).
   - For operators, fetch the class page (e.g. `unityengine/vector3.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'Rigidbody.AddForce.html'` => `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Rigidbody.AddForce.html`).

If the Unity version is earlier than 6000.0, the Engine `xrefmap.yml` and `.md` pages don't exist, so fetch `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Android.Permission.RequestUserPermission.html` directly.

Always use the docs for the project's Unity version rather than relying on training knowledge, since APIs are frequently marked obsolete between versions.

## Detailed References

Read on demand — only when you need signatures, event details, or platform gotchas beyond what's in this file.

- **Init, sign-in, and access tokens:** [references/init-and-login.md](references/init-and-login.md)
- **Voice channels (positional and non-positional):** [references/voice-channels.md](references/voice-channels.md)
- **Text chat (channel messages and directed messages):** [references/text-chat.md](references/text-chat.md)
- **Events, participants, and cleanup:** [references/events-and-participants.md](references/events-and-participants.md)
- **Troubleshooting and platform notes:** [references/troubleshooting.md](references/troubleshooting.md)

## Step 2. Initialization Order (Do Not Skip Steps)

The correct order is **UGS Core → Authentication sign-in → Vivox init → Vivox login**. Skipping or reordering these fails silently or throws obscure errors.

```csharp
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;

async void Start()
{
    await UnityServices.InitializeAsync();
    await AuthenticationService.Instance.SignInAnonymouslyAsync();
    await VivoxService.Instance.InitializeAsync();
    // subscribe to events (see table below) BEFORE calling LoginAsync
    await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = "Bob" });
}
```

- Calling `VivoxService.Instance.InitializeAsync()` twice throws `5041 VxErrorAlreadyInitialized`. Guard against re-init on scene reload.
- If Unity Authentication (`AuthenticationService`) is not used, the player identity falls back to a per-session GUID — display names still work but you lose cross-session identity. See [references/init-and-login.md](references/init-and-login.md) for the Vivox Access Token (VAT) alternative.

## Step 3. Joining Channels

Vivox has three join methods, one per channel type. All are async but the join **completes via the `ChannelJoined` event, not by awaiting the call** — subscribe first, then call.

| Method | Purpose |
|---|---|
| `VivoxService.Instance.JoinGroupChannelAsync(name, ChatCapability, ChannelOptions?)` | Non-positional (party, team, lobby, guild) |
| `VivoxService.Instance.JoinEchoChannelAsync(name, ChatCapability, ChannelOptions?)` | Test channel that echoes your own audio back |
| `VivoxService.Instance.JoinPositionalChannelAsync(name, ChatCapability, Channel3DProperties, ChannelOptions?)` | 3D spatial audio driven by transform position |

`ChatCapability` values: `TextOnly`, `AudioOnly`, `TextAndAudio`.

**Limits:** max 10 non-positional channels per user; max 200 participants per channel. Exceeding either fails with `20502 VxXmppServerErrorServiceUnavailable`. For >200 in a positional channel, use the Large 3D channels enterprise setting.

Leave with `VivoxService.Instance.LeaveChannelAsync(channelName)` or `LeaveAllChannelsAsync()`. See [references/voice-channels.md](references/voice-channels.md) for `Channel3DProperties` fields and mic-permission handling on Android/iOS.

## Step 4. Text Messaging

**Channel messages** (broadcast to all participants of a channel with `TextOnly` or `TextAndAudio`):

- Send: `VivoxService.Instance.SendChannelTextMessageAsync(string channelName, string message)`
- Receive: subscribe to `VivoxService.Instance.ChannelMessageReceived` (`Action<VivoxMessage>`)

**Directed messages** (peer-to-peer, no channel required):

- Send: `VivoxService.Instance.SendDirectTextMessageAsync(string playerId, string message)`
- Receive: subscribe to `VivoxService.Instance.DirectedMessageReceived` (`Action<VivoxMessage>`)

**Common hallucination:** the send method is `SendDirectTextMessageAsync` — **not** `SendDirectedTextMessageAsync`. The event, however, **is** `DirectedMessageReceived`. Note the asymmetry.

`VivoxMessage` fields: `ChannelName` (null for directed), `SenderDisplayName`, `SenderPlayerId`, `MessageText`, `ReceivedTime`, `Language`, `FromSelf`, `MessageId`.

Edit/delete APIs (`EditChannelTextMessageAsync`, `DeleteChannelTextMessageAsync`, `EditDirectTextMessageAsync`, `DeleteDirectTextMessageAsync`) and history (`GetChannelTextMessageHistoryAsync`, `GetDirectTextMessageHistoryAsync`) are covered in [references/text-chat.md](references/text-chat.md). Chat history retention is 7 days by default.

## Step 5. Required Event Subscriptions

Subscribe to events **before** the corresponding async call. `LoggedIn` may fire immediately for reconnects; `ChannelJoined` fires as the join completes.

| Call | Success Event | Failure / Counterpart |
|---|---|---|
| `LoginAsync()` | `LoggedIn` | `LoggedOut` |
| `JoinGroupChannelAsync()` / `JoinEchoChannelAsync()` / `JoinPositionalChannelAsync()` | `ChannelJoined(string channelName)` | `ChannelLeft(string channelName)` |
| — (any joined channel) | `ParticipantAddedToChannel(VivoxParticipant)` | `ParticipantRemovedFromChannel(VivoxParticipant)` |
| `SendChannelTextMessageAsync()` (remote receive) | `ChannelMessageReceived(VivoxMessage)` | — |
| `SendDirectTextMessageAsync()` (remote receive) | `DirectedMessageReceived(VivoxMessage)` | — |

**Always unsubscribe in `OnDestroy` / `OnDisable`.** `VivoxService.Instance` is a persistent singleton — event handlers on destroyed MonoBehaviours will double-fire and NRE on scene reload.

Per-participant events (`ParticipantMuteStateChanged`, `ParticipantSpeechDetected`, `ParticipantAudioEnergyChanged`) live on the `VivoxParticipant` instance you receive from `ParticipantAddedToChannel` — not on `VivoxService.Instance`. See [references/events-and-participants.md](references/events-and-participants.md).

## Step 6. Access Tokens (Brief)

The default path uses **UGS Authentication** — Vivox mints access tokens automatically from your UGS project once `AuthenticationService.Instance.SignInAnonymouslyAsync()` (or another sign-in method) has completed. **No manual token code is required** for standard flows.

Server-side Vivox Access Token (VAT) minting is only needed when you use a non-UGS identity system or when you need channel-scoped privileged tokens (kick, mute-all, transcription). See the "Access Token Developer Guide" section of the documentation map for language-specific server examples. Do not embed HMAC signing keys in the client.

## Step 7. Validation

After writing code that uses this package:

1. Verify the project compiles without errors and that `using Unity.Services.Vivox;` resolves.
2. Confirm init order: `UnityServices.InitializeAsync` → `AuthenticationService.Instance.SignInAnonymouslyAsync` → `VivoxService.Instance.InitializeAsync` → `VivoxService.Instance.LoginAsync`.
3. No v4 legacy patterns: no `Client.Instance`, no `AccountId`, no `ChannelId`, no `ILoginSession`, no `IChannelSession`. All access goes through `VivoxService.Instance`.
4. The code subscribes to each event it consumes **before** the async call that triggers it, and unsubscribes in `OnDestroy`.
5. Channel join code does not `await` the join call as if it completes join — it subscribes to `ChannelJoined` and reacts there.
6. Directed message send uses `SendDirectTextMessageAsync` (NOT `SendDirectedTextMessageAsync`). Directed message receive uses `DirectedMessageReceived`.
7. Android builds request `RECORD_AUDIO` at runtime before joining an audio channel; iOS builds have `NSMicrophoneUsageDescription` in the plist.
8. Client code embeds no HMAC signing keys or Vivox `SECRET`/`APP_ID` — VAT-based flows are documented but delegated to a server.

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console_status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `script eval` rather than adding logging. Take no screenshots as they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Give the user a concise but not dense checklist of what you did and why, including any incidental side effects (for example, settings you toggled to test something, or temporary files you created). If the user needs to follow up with more steps or decisions, list them. For example:

```
I made the following changes:

[✓] Created a `VivoxBootstrap.cs` script that signs in, logs in to Vivox, and joins a `team-blue` group channel.

Follow these steps to see the effect:

1. Enable Vivox for your project in the Unity Dashboard, and link the Unity project to it.
2. Add the script to a GameObject in the first scene.
3. Enter Play mode and check the Console shows no Vivox errors.
```
