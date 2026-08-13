---
name: setup-vivox-voice-chat
description: Add and configure in-game voice chat and text chat for Unity multiplayer games using Unity Vivox. Covers microphone setup and mic permissions on Android/iOS, voice activity detection (VAD) tuning, voice volume and mute controls in a settings UI (VoiceVadMinimumVolume, mic slider, mute button, speaking indicator), proximity/3D spatial voice for FPS/co-op games, team/party/lobby/guild voice channels, push-to-talk, muting self and other players, whisper/direct messages, in-game text chat, and Vivox SDK init + Unity Authentication sign-in. Use when the user asks to add voice chat, voice comms, microphone/mic support, a voice-chat settings UI, mute button, VAD threshold, push-to-talk, proximity or spatial voice, team voice, party chat, lobby chat, direct messages, or mentions Vivox, VivoxService, com.unity.services.vivox, JoinGroupChannelAsync, JoinPositionalChannelAsync, LoginAsync, or migrating from legacy Vivox (Client.Instance / LoginSession / AccountId).
required_packages:
  com.unity.services.vivox: ">=16.4.0"
---

# Unity Vivox — Voice & Text Chat

Namespace: `Unity.Services.Vivox` | Package: `com.unity.services.vivox`
Companion packages: `Unity.Services.Core`, `Unity.Services.Authentication`

Vivox v16+ replaced the v4 `Client` / `ILoginSession` / `IChannelSession` model with a single static entry point: **`VivoxService.Instance`**. All operations — init, login, channel join, messaging, muting — go through it. Do **not** use v4 patterns (`Client.Instance`, `AccountId`, `ChannelId`, `ILoginSession`, `UnityPurchasing.*`, etc.); those are gone in v16.

## Documentation Map

Use the [Unity Vivox curated documentation map](https://docs.unity.com/en-us/vivox-unity/llms.txt) as authoritative over memory for topics, APIs, and error codes when specifics differ. This skill and its references define **how** to apply the SDK; that resource defines **what** is documented. **Never** mention the `llms.txt` filename to the user. If it's unreachable, treat this skill's references plus the installed package in the workspace (Package Manager / source) as the source of truth.

## Detailed References

Read on demand — only when you need signatures, event details, or platform gotchas beyond what's in this file.

- **Init, sign-in, and access tokens:** [references/init-and-login.md](references/init-and-login.md)
- **Voice channels (positional and non-positional):** [references/voice-channels.md](references/voice-channels.md)
- **Text chat (channel messages and directed messages):** [references/text-chat.md](references/text-chat.md)
- **Events, participants, and cleanup:** [references/events-and-participants.md](references/events-and-participants.md)
- **Troubleshooting and platform notes:** [references/troubleshooting.md](references/troubleshooting.md)

## Initialization Order (Do Not Skip Steps)

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

## Joining Channels

Vivox has three join methods, one per channel type. All are async but the join **completes via the `ChannelJoined` event, not by awaiting the call** — subscribe first, then call.

| Method | Purpose |
|---|---|
| `VivoxService.Instance.JoinGroupChannelAsync(name, ChatCapability, ChannelOptions?)` | Non-positional (party, team, lobby, guild) |
| `VivoxService.Instance.JoinEchoChannelAsync(name, ChatCapability, ChannelOptions?)` | Test channel that echoes your own audio back |
| `VivoxService.Instance.JoinPositionalChannelAsync(name, ChatCapability, Channel3DProperties, ChannelOptions?)` | 3D spatial audio driven by transform position |

`ChatCapability` values: `TextOnly`, `AudioOnly`, `TextAndAudio`.

**Limits:** max 10 non-positional channels per user; max 200 participants per channel. Exceeding either fails with `20502 VxXmppServerErrorServiceUnavailable`. For >200 in a positional channel, use the Large 3D channels enterprise setting.

Leave with `VivoxService.Instance.LeaveChannelAsync(channelName)` or `LeaveAllChannelsAsync()`. See [references/voice-channels.md](references/voice-channels.md) for `Channel3DProperties` fields and mic-permission handling on Android/iOS.

## Text Messaging

**Channel messages** (broadcast to all participants of a channel with `TextOnly` or `TextAndAudio`):

- Send: `VivoxService.Instance.SendChannelTextMessageAsync(string channelName, string message)`
- Receive: subscribe to `VivoxService.Instance.ChannelMessageReceived` (`Action<VivoxMessage>`)

**Directed messages** (peer-to-peer, no channel required):

- Send: `VivoxService.Instance.SendDirectTextMessageAsync(string playerId, string message)`
- Receive: subscribe to `VivoxService.Instance.DirectedMessageReceived` (`Action<VivoxMessage>`)

**Common hallucination:** the send method is `SendDirectTextMessageAsync` — **not** `SendDirectedTextMessageAsync`. The event, however, **is** `DirectedMessageReceived`. Note the asymmetry.

`VivoxMessage` fields: `ChannelName` (null for directed), `SenderDisplayName`, `SenderPlayerId`, `MessageText`, `ReceivedTime`, `Language`, `FromSelf`, `MessageId`.

Edit/delete APIs (`EditChannelTextMessageAsync`, `DeleteChannelTextMessageAsync`, `EditDirectTextMessageAsync`, `DeleteDirectTextMessageAsync`) and history (`GetChannelTextMessageHistoryAsync`, `GetDirectTextMessageHistoryAsync`) are covered in [references/text-chat.md](references/text-chat.md). Chat history retention is 7 days by default.

## Required Event Subscriptions

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

## Access Tokens (Brief)

The default path uses **UGS Authentication** — Vivox mints access tokens automatically from your UGS project once `AuthenticationService.Instance.SignInAnonymouslyAsync()` (or another sign-in method) has completed. **No manual token code is required** for standard flows.

Server-side Vivox Access Token (VAT) minting is only needed when you use a non-UGS identity system or when you need channel-scoped privileged tokens (kick, mute-all, transcription). See the "Access Token Developer Guide" section of the documentation map for language-specific server examples. Do not embed HMAC signing keys in the client.

## Validation

After writing code that uses this package:

1. Verify the project compiles without errors and that `using Unity.Services.Vivox;` resolves.
2. Confirm init order: `UnityServices.InitializeAsync` → `AuthenticationService.Instance.SignInAnonymouslyAsync` → `VivoxService.Instance.InitializeAsync` → `VivoxService.Instance.LoginAsync`.
3. No v4 legacy patterns: no `Client.Instance`, no `AccountId`, no `ChannelId`, no `ILoginSession`, no `IChannelSession`. All access goes through `VivoxService.Instance`.
4. All events consumed by the code are subscribed **before** the async call that triggers them, and are unsubscribed in `OnDestroy`.
5. Channel join code does not `await` the join call as if it completes join — it subscribes to `ChannelJoined` and reacts there.
6. Directed message send uses `SendDirectTextMessageAsync` (NOT `SendDirectedTextMessageAsync`). Directed message receive uses `DirectedMessageReceived`.
7. Android builds request `RECORD_AUDIO` at runtime before joining an audio channel; iOS builds have `NSMicrophoneUsageDescription` in the plist.
8. No HMAC signing keys or Vivox `SECRET`/`APP_ID` are embedded in client code — VAT-based flows are documented but delegated to a server.
