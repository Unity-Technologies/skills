# Events, Participants, and Lifecycle

## Service-Level Events

All on `VivoxService.Instance`. Subscribe **before** the async call that produces them.

| Event | Signature | Fires on |
|---|---|---|
| `LoggedIn` | `Action` | `LoginAsync` success (also on reconnect) |
| `LoggedOut` | `Action` | `LogoutAsync` or disconnect |
| `ChannelJoined` | `Action<string channelName>` | Any `Join*ChannelAsync` success |
| `ChannelLeft` | `Action<string channelName>` | `LeaveChannelAsync` / `LeaveAllChannelsAsync` / disconnect |
| `ParticipantAddedToChannel` | `Action<VivoxParticipant>` | Any user joins a channel you're in (including yourself) |
| `ParticipantRemovedFromChannel` | `Action<VivoxParticipant>` | Any user leaves |
| `ChannelMessageReceived` | `Action<VivoxMessage>` | Any channel text message |
| `ChannelMessageEdited` | `Action<VivoxMessage>` | Any channel message edited |
| `ChannelMessageDeleted` | `Action<VivoxMessage>` | Any channel message deleted |
| `DirectedMessageReceived` | `Action<VivoxMessage>` | Any directed message to you |
| `DirectedMessageEdited` | `Action<VivoxMessage>` | Directed message edited |
| `DirectedMessageDeleted` | `Action<VivoxMessage>` | Directed message deleted |

## VivoxParticipant

Delivered by `ParticipantAddedToChannel` and `ParticipantRemovedFromChannel`. Represents one participant in one channel — the same user in two channels is two separate `VivoxParticipant` instances.

| Property | Purpose |
|---|---|
| `PlayerId` | Stable UAS PlayerId of the participant |
| `DisplayName` | From the participant's `LoginOptions.DisplayName` |
| `ChannelName` | Which channel this participation is in |
| `IsSelf` | `true` if this is the local player |
| `IsMuted` | Current locally-muted state |
| `AudioEnergy` | Continuous 0.0–1.0 signal for VU-meter UI |
| `SpeechDetected` | `true` when Vivox judges audio energy is speech, not noise |

## Per-Participant Events

Live on the `VivoxParticipant` instance, **not** on `VivoxService.Instance`:

- `ParticipantMuteStateChanged` — `IsMuted` flipped.
- `ParticipantSpeechDetected` — `SpeechDetected` flipped.
- `ParticipantAudioEnergyChanged` — `AudioEnergy` updated (higher-frequency; use for VU meter).

Typical wiring in a roster item that represents one participant:

```csharp
public void Bind(VivoxParticipant p)
{
    _participant = p;
    p.ParticipantMuteStateChanged += Refresh;
    p.ParticipantSpeechDetected   += Refresh;
}

void OnDestroy()
{
    if (_participant == null) return;
    _participant.ParticipantMuteStateChanged -= Refresh;
    _participant.ParticipantSpeechDetected   -= Refresh;
}
```

## Local Mute Actions

Called on the `VivoxParticipant` (not the service):

- `participant.MutePlayerLocally()` — you stop hearing them.
- `participant.UnmutePlayerLocally()` — you resume hearing them.

The remote participant is unaware. To mute globally (they cannot be heard by anyone), a moderator client needs a server-issued mute token.

## Cleanup Discipline

`VivoxService.Instance` is a persistent singleton across scene loads. Any handler you subscribe from a MonoBehaviour **must** be unsubscribed in `OnDestroy` or `OnDisable`, or the handler will fire against a destroyed object on the next scene load and throw a `MissingReferenceException`.

Pattern: subscribe in `Awake`/`Start`, mirror the list in `OnDestroy`, always null-guard `VivoxService.Instance` (it may already be null during application quit).

## Connection Recovery

On network blips Vivox will auto-reconnect and re-fire `LoggedIn` and (for previously-joined channels) `ChannelJoined`. Design handlers to be **idempotent** — do not assume `LoggedIn` fires exactly once per session, and don't grant one-shot benefits (analytics event, first-login reward) from inside it without a guard.
