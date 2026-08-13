# Troubleshooting and Platform Notes

For the authoritative error table, see the Vivox SDK error codes page linked from the Vivox documentation map.

## Common Errors

| Code | Name | Cause | Fix |
|---|---|---|---|
| `5041` | `VxErrorAlreadyInitialized` | `VivoxService.Instance.InitializeAsync()` called twice | Guard with `IsInitialized` or make bootstrap `DontDestroyOnLoad` |
| `20502` | `VxXmppServerErrorServiceUnavailable` | Exceeded 10 non-positional channels per user, or 200 participants per channel | Leave a channel before joining another; for large positional channels use Large 3D Channels setting |
| Login fails silently | — | Subscribed to `LoggedIn` **after** `LoginAsync` returned | Subscribe first, then call `LoginAsync` |
| `ChannelJoined` never fires | — | Awaited `JoinGroupChannelAsync` as if it completes the join | Bind `ChannelJoined` before calling; treat the await as "request queued" |
| No audio in / out | — | Mic permission denied, wrong `ChatCapability` (e.g. `TextOnly` when audio expected), or muted input device | Check runtime permission, `ChatCapability`, and `IsInputDeviceMuted` — call `UnmuteInputDevice()` if muted |

## Platform Notes

### Android

- Merge `<uses-permission android:name="android.permission.RECORD_AUDIO"/>` into `AndroidManifest.xml`.
- Request at runtime with `UnityEngine.Android.Permission.RequestUserPermission(Permission.Microphone)` **before** joining an audio channel — Android will not prompt automatically for you.
- Bluetooth SCO underruns cause choppy input — see the Android troubleshooting page in the documentation map.
- If shrinking / obfuscating with R8/ProGuard, add the Vivox ProGuard rules from the docs.

### iOS

- Add `NSMicrophoneUsageDescription` to Info.plist (Project Settings → Player → iOS → Microphone Usage Description).
- The orange/red iOS recording indicator is shown any time Vivox is capturing — this is OS-enforced and expected.

### WebGL

- The Vivox WebGL SDK is a subset of the native SDK. Audio Taps, some codecs, and certain positional-audio features are unavailable. Read the WebGL support page in the documentation map before promising a feature on web.
- Browsers require a user gesture before capturing the mic — trigger the first `JoinGroupChannelAsync`/`JoinPositionalChannelAsync` from a button click, not from `Start()`.

### NDA Platforms (console)

Vivox ships NDA-gated packages for consoles. Contact Unity for access; the public UPM package does not include console binaries.

## Diagnostic Checklist

When integration seems broken and no clear error surfaces:

1. Confirm init order — `UnityServices.InitializeAsync` → `AuthenticationService.Instance.SignInAnonymouslyAsync` → `VivoxService.Instance.InitializeAsync` → `VivoxService.Instance.LoginAsync`.
2. Log every event handler entry (`LoggedIn`, `ChannelJoined`, `ChannelMessageReceived`). If a handler you expect never enters, you subscribed after the event already fired.
3. Confirm the joined channel's `ChatCapability` matches what you're trying to do (text vs audio).
4. Confirm mic permission on the platform you're testing.
5. If audio was working then stopped after a scene reload, you have leaked event subscriptions from destroyed MonoBehaviours — audit `OnDestroy` unsubscribes.
6. If a directed message never arrives, verify `SendDirectTextMessageAsync` is targeting the recipient's **UAS PlayerId** (not display name), and that the recipient has subscribed to `DirectedMessageReceived`.
