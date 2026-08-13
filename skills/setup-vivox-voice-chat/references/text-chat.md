# Text Chat

Text works over any channel joined with `ChatCapability.TextOnly` or `ChatCapability.TextAndAudio`, plus directed (peer-to-peer) messages that don't require a shared channel.

## Channel Messages

**Send:**

```csharp
await VivoxService.Instance.SendChannelTextMessageAsync(
    string channelName,
    string message);
```

**Receive:**

```csharp
VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;

void OnChannelMessageReceived(VivoxMessage m)
{
    // m.ChannelName, m.SenderDisplayName, m.SenderPlayerId,
    // m.MessageText, m.ReceivedTime, m.Language, m.FromSelf, m.MessageId
}
```

## Directed Messages

**Send:** (note spelling — `SendDirect…`, not `SendDirected…`)

```csharp
await VivoxService.Instance.SendDirectTextMessageAsync(
    string playerId,   // recipient's UAS PlayerId
    string message);
```

**Receive:** (event *is* `Directed…`)

```csharp
VivoxService.Instance.DirectedMessageReceived += OnDirectedMessageReceived;

void OnDirectedMessageReceived(VivoxMessage m)
{
    // Same VivoxMessage fields, but m.ChannelName is null and m.FromSelf is false.
}
```

## VivoxMessage Fields

| Field | Notes |
|---|---|
| `ChannelName` | The channel the message came in on. **`null` for directed messages.** |
| `SenderDisplayName` | As set in the sender's `LoginOptions`. |
| `SenderPlayerId` | UAS PlayerId — stable identity to reply/DM back. |
| `MessageText` | The message body. |
| `ReceivedTime` | `DateTime` of receipt. |
| `Language` | Sender's language tag if set. |
| `FromSelf` | `true` for the local player's own channel messages; `false` for directed messages. |
| `MessageId` | Server-assigned ID — required to edit or delete. |

## Chat History

Retention: **7 days** by default (30 days if Text Evidence Management is enabled).

```csharp
IReadOnlyCollection<VivoxMessage> GetChannelTextMessageHistoryAsync(
    string channelName,
    int requestSize = 10,
    ChatHistoryQueryOptions options = null);

IReadOnlyCollection<VivoxMessage> GetDirectTextMessageHistoryAsync(
    string playerId,
    int requestSize = 10,
    ChatHistoryQueryOptions options = null);
```

Both return messages **newest-first**. Reverse when rendering a chat log.

## Edit and Delete

Only the original sender can edit or delete their own messages.

| Op | Channel | Directed |
|---|---|---|
| Edit | `EditChannelTextMessageAsync(channelName, messageId, newText)` | `EditDirectTextMessageAsync(messageId, newText)` |
| Delete | `DeleteChannelTextMessageAsync(channelName, messageId)` | `DeleteDirectTextMessageAsync(messageId)` |
| Notify (all participants) | `ChannelMessageEdited`, `ChannelMessageDeleted` | `DirectedMessageEdited`, `DirectedMessageDeleted` |

All notify events carry the updated `VivoxMessage`.

## Anti-flooding

Vivox rate-limits messages per player. When implementing chat UI, disable the send button after each send until acknowledged, and surface a "try again in a moment" hint on rate-limit errors — do not spam-retry.
