# Voice Channels

## Channel Types

| Type | Join method | Use for |
|---|---|---|
| Non-positional (group) | `JoinGroupChannelAsync` | Party, team, lobby, guild — all participants hear each other equally |
| Echo | `JoinEchoChannelAsync` | Test-only — your own audio is echoed back |
| Positional (3D) | `JoinPositionalChannelAsync` | Proximity / spatial audio driven by transform position |

## Join Signatures

```csharp
Task JoinGroupChannelAsync(
    string channelName,
    ChatCapability chatCapability,
    ChannelOptions channelOptions = null);

Task JoinEchoChannelAsync(
    string channelName,
    ChatCapability chatCapability,
    ChannelOptions channelOptions = null);

Task JoinPositionalChannelAsync(
    string channelName,
    ChatCapability chatCapability,
    Channel3DProperties positionalChannelProperties,
    ChannelOptions channelOptions = null);
```

The returned `Task` completes when the *request* has been sent, not when the join is complete. The actual join fires `ChannelJoined(string channelName)`. Bind that event **before** calling the join method.

## ChatCapability

- `ChatCapability.TextOnly` — text-only channel (no audio at all)
- `ChatCapability.AudioOnly` — voice-only, no text
- `ChatCapability.TextAndAudio` — both

## ChannelOptions

Optional. Common use: set this channel as the active transmit target on join success. Leave `null` for default behavior (join without changing transmission mode).

## Positional Channels — Channel3DProperties

`Channel3DProperties` controls how distance and direction affect voice attenuation. Key fields:

- `AudibleDistance` — beyond this, participant is inaudible.
- `ConversationalDistance` — below this, participant is at full volume.
- `AudioFadeIntensityByDistance` — falloff steepness between conversational and audible distance.
- `AudioFadeModel` — `InverseByDistance`, `LinearByDistance`, `ExponentialByDistance`.

Example call-site:

```csharp
var props = new Channel3DProperties(
    audibleDistance: 50,
    conversationalDistance: 5,
    audioFadeIntensityByDistance: 1.0f,
    audioFadeModel: AudioFadeModel.InverseByDistance);

await VivoxService.Instance.JoinPositionalChannelAsync(
    "world-proximity", ChatCapability.AudioOnly, props);
```

Drive per-frame position updates by calling `VivoxService.Instance.Set3DPosition(GameObject speakerObject, string channelName)` from a listener/speaker script (typically on the player camera and on remote player representations).

For >200 participants in a positional channel, enable the enterprise-tier Large 3D channels setting; see the documentation map's positional channels page.

## Leaving

```csharp
await VivoxService.Instance.LeaveChannelAsync(channelName);
await VivoxService.Instance.LeaveAllChannelsAsync();
```

Both fire `ChannelLeft(string channelName)` for each channel exited.

## Mic Permission

Joining an `AudioOnly` or `TextAndAudio` channel requires microphone access.

- **Android:** request `RECORD_AUDIO` at runtime with `Permission.RequestUserPermission(Permission.Microphone)` before the first audio-capable join. Merge `<uses-permission android:name="android.permission.RECORD_AUDIO"/>` if not present.
- **iOS:** add `NSMicrophoneUsageDescription` to the Info.plist (Project Settings → Player → iOS → Microphone Usage Description).
- **Desktop / WebGL:** the browser or OS prompts on first capture attempt; no code change required, but WebGL has additional limitations — see [troubleshooting.md](troubleshooting.md).

## Muting

- **Local mic mute (self):** `VivoxService.Instance.MuteInputDevice()` / `UnmuteInputDevice()` — parameterless pair that stops your audio from being sent anywhere. Read state via the `IsInputDeviceMuted` property.
- **Mute another player locally (only you stop hearing them):** `participant.MutePlayerLocally()` / `participant.UnmutePlayerLocally()` on the `VivoxParticipant` from `ParticipantAddedToChannel`.
- **Server-side kick / mute-all:** requires a privileged Vivox Access Token minted server-side.
