# iOS-Specific Setup for LevelPlay

## Contents
- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [SKAdNetwork Configuration](#skadnetwork-configuration)
- [App Tracking Transparency (ATT)](#app-tracking-transparency-att)
- [Privacy Manifest](#privacy-manifest)
- [Xcode Build Settings](#xcode-build-settings)
- [Testing on iOS](#testing-on-ios)
- [Common iOS Issues](#common-ios-issues)
- [iOS Privacy Checklist](#ios-privacy-checklist)
- [Version-Specific Notes](#version-specific-notes)
- [Best Practices](#best-practices)

## Overview

LevelPlay SDK on iOS requires additional configuration beyond the basic Unity integration. This guide covers iOS-specific setup, including SKAdNetwork, App Tracking Transparency (ATT), and privacy manifest requirements.

## Prerequisites

For the current supported Unity, iOS deployment target, and Xcode versions, see the [LevelPlay iOS SDK integration guide](https://docs.unity.com/en-us/grow/levelplay/sdk/ios/sdk-integration).

## SKAdNetwork Configuration

### What is SKAdNetwork?

SKAdNetwork is Apple's privacy-preserving ad attribution framework. It allows advertisers to measure campaign effectiveness without accessing user-level data.

### Setup Steps

LevelPlay SDK automatically includes the required SKAdNetwork IDs in your app's Info.plist during the build process. However, you should verify this after building:

1. Build your Unity project for iOS
2. Open the generated Xcode project
3. Check `Info.plist` for the `SKAdNetworkItems` array
4. Verify it contains multiple SKAdNetwork IDs (format: `XXXX1234abc.skadnetwork`)

**If SKAdNetwork IDs are missing:**

The LevelPlay Unity Package should handle this automatically. If you notice missing IDs:
- Ensure you're using the latest LevelPlay Unity Package version
- Check the Unity console for any import warnings
- Manually add the SKAdNetwork IDs if necessary (see Unity documentation)

### Verifying SKAdNetwork Setup

After building to Xcode, you should see entries like this in Info.plist:

```xml
<key>SKAdNetworkItems</key>
<array>
    <dict>
        <key>SKAdNetworkIdentifier</key>
        <string>cstr6suwn9.skadnetwork</string>
    </dict>
    <dict>
        <key>SKAdNetworkIdentifier</key>
        <string>4fzdc2evr5.skadnetwork</string>
    </dict>
    <!-- Many more entries... -->
</array>
```

## App Tracking Transparency (ATT)

### What is ATT?

Apple requires ATT authorization before your app tracks users or accesses the device's advertising identifier on iOS 14.5+. Request ATT authorization before calling `LevelPlay.Init()` — this is both an Apple platform requirement and necessary for personalized ads (which also affects fill rate).

### Implementation

ATT requires two parts: a post-build script that writes the permission description string into `Info.plist`, and a native plugin that performs the actual permission request.

### Part 1: Permission Description (Post-Build Script)

Apple requires a user-facing description explaining why your app requests tracking permission. Add it via a post-build script — this is the reliable approach across all Unity versions. The **User Tracking Usage Description** field in Player Settings is not present in Unity 6.

Create `Assets/Editor/iOSPostBuild.cs`:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
using System.IO;
#endif

public class iOSPostBuild
{
#if UNITY_IOS
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS) return;

        var plistPath = Path.Combine(buildPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // Required for ATT prompt — shown to users in the system dialog
        plist.root.SetString(
            "NSUserTrackingUsageDescription",
            "We use tracking to show you relevant ads and support free gameplay."
        );

        plist.WriteToFile(plistPath);
        Debug.Log("iOSPostBuild: NSUserTrackingUsageDescription added to Info.plist");
    }
#endif
}
```

**Customise the description** to match your app's purpose. Apple reviews this text and may reject vague descriptions.

### Part 2: Native Plugin for the Permission Request

Create `Assets/Plugins/iOS/ATTRequester.mm`:

```objc
#import <AppTrackingTransparency/AppTrackingTransparency.h>

typedef void (*ATTCallback)(int status);

extern "C"
{
    // Returns current ATT status without prompting:
    // 0 = Not Determined, 1 = Restricted, 2 = Denied, 3 = Authorized
    int _ATT_GetStatus()
    {
        if (@available(iOS 14, *))
        {
            return (int)[ATTrackingManager trackingAuthorizationStatus];
        }
        return 3; // Pre-iOS 14: treat as Authorized
    }

    // Requests ATT permission and invokes callback with the resulting status.
    // On pre-iOS 14 devices, immediately calls back with Authorized (3).
    void _ATT_RequestPermission(ATTCallback callback)
    {
        if (@available(iOS 14, *))
        {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status)
            {
                dispatch_async(dispatch_get_main_queue(), ^{
                    if (callback) callback((int)status);
                });
            }];
        }
        else
        {
            if (callback) callback(3);
        }
    }
}
```

**Note:** `dispatch_async(dispatch_get_main_queue(), ...)` returns the callback to Unity's main thread, which is required for safe IL2CPP interop.

### Part 3: Integrating ATT into LevelPlayInitializer

ATT must complete before `LevelPlay.Init()`. Modify your `LevelPlayInitializer.cs` from Step 7 to use a coroutine `Start()`:

```csharp
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Services.LevelPlay;

public class LevelPlayInitializer : MonoBehaviour
{
    [SerializeField] private string appKey;

#if UNITY_IOS && !UNITY_EDITOR
    private delegate void ATTCallbackDelegate(int status);

    [DllImport("__Internal")] private static extern int _ATT_GetStatus();
    [DllImport("__Internal")] private static extern void _ATT_RequestPermission(ATTCallbackDelegate callback);

    private const int ATT_NOT_DETERMINED = 0;
    private static bool _attDone = false;

    [AOT.MonoPInvokeCallback(typeof(ATTCallbackDelegate))]
    private static void OnATTCallback(int status)
    {
        Debug.Log($"ATT: user responded — status {status}");
        _attDone = true;
    }

    private IEnumerator RequestATT()
    {
        // Skip prompt if user has already responded (e.g. returning user)
        if (_ATT_GetStatus() != ATT_NOT_DETERMINED)
        {
            Debug.Log($"ATT: already determined (status {_ATT_GetStatus()})");
            yield break;
        }

        _attDone = false;
        _ATT_RequestPermission(OnATTCallback);
        yield return new WaitUntil(() => _attDone);
    }
#endif

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // Request ATT before initializing LevelPlay to maximise ad fill rate
        yield return RequestATT();
#else
        yield return null; // Editor / Android: skip ATT
#endif
        InitializeLevelPlay();
    }

    private void InitializeLevelPlay()
    {
        // Add privacy settings here before Init (see Step 6.5)
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK initialized successfully");
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay initialization failed: {error.ErrorMessage}");
    }

    void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }
}
```

**Key points:**
- `[AOT.MonoPInvokeCallback]` is required for IL2CPP builds — without it the native callback will crash on device
- `[DllImport("__Internal")]` binds to the native plugin functions
- `_ATT_GetStatus()` prevents re-prompting users who have already responded
- `IEnumerator Start()` with `WaitUntil(() => _attDone)` pauses until the user responds to the ATT dialog
- `#if UNITY_IOS && !UNITY_EDITOR` guards ensure ATT code only compiles for iOS device builds

## Privacy Manifest

### iOS 17+ Requirements

Apple requires apps to declare data types collected and the reasons for using certain APIs. LevelPlay SDK includes a privacy manifest, but you should be aware of what it declares.

### What LevelPlay Declares

The LevelPlay SDK's privacy manifest typically declares:
- **Data Collection**: Device identifiers (IDFA), usage data, location (if used)
- **Required Reason APIs**: User defaults, file timestamp, system boot time, disk space
- **Tracking**: Yes (for personalized advertising)

### Your App's Privacy Manifest

Ensure your app's privacy manifest (if you have one) aligns with LevelPlay's declarations:

1. Check for `PrivacyInfo.xcprivacy` in your Xcode project
2. Verify it includes LevelPlay's data types
3. Update your App Store privacy declarations accordingly

## Xcode Build Settings

### Required iOS Capabilities

LevelPlay SDK requires certain capabilities to function properly:

1. Open your Xcode project after building from Unity
2. Select your target > **Signing & Capabilities**
3. Verify these frameworks are included (LevelPlay should add them automatically):
   - **AdSupport.framework**: For IDFA access
   - **AppTrackingTransparency.framework**: For ATT (iOS 14+)
   - **StoreKit.framework**: For SKAdNetwork

### App Transport Security (ATS) Configuration

To ensure ads load correctly, configure App Transport Security in your Info.plist:

**Option 1: Allow arbitrary loads (easiest, less secure)**

Add this to your Info.plist:
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
</dict>
```

**Option 2: Allow specific domains (more secure)**

If you prefer to only allow specific ad network domains:
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSExceptionDomains</key>
    <dict>
        <key>ironsrc.com</key>
        <dict>
            <key>NSIncludesSubdomains</key>
            <true/>
            <key>NSExceptionAllowsInsecureHTTPLoads</key>
            <true/>
        </dict>
        <!-- Add other ad network domains as needed -->
    </dict>
</dict>
```

**Note:** Most ad networks require HTTP access for legacy ad creatives. Without proper ATS configuration, some ads may fail to load.

**When to configure:** Before building for iOS. This can be done in Unity's PostProcessBuild or manually in Xcode after export.

### Recommended Xcode Build Settings

In Xcode, verify these settings:

- **Deployment Target**: see the [LevelPlay iOS SDK integration guide](https://docs.unity.com/en-us/grow/levelplay/sdk/ios/sdk-integration) for the currently supported minimum
- **Enable Bitcode**: No
- **Other Linker Flags**: Should include `-ObjC` (automatically added by Unity)

## Testing on iOS

### Test Device Setup

1. Build and run on a physical iOS device (simulators have limitations)
2. For ATT testing:
   - Go to **Settings > Privacy & Security > Tracking**
   - You can reset ATT prompt status by going to **Settings > General > Transfer or Reset iPhone > Reset > Reset Location & Privacy**

### Integration Validation

After building to iOS:

```csharp
void Start()
{
    #if UNITY_IOS
    LevelPlay.OnInitSuccess += OnInitSuccess;
    LevelPlay.OnInitFailed += OnInitFailed;
    LevelPlay.Init("YOUR_APP_KEY");
    #endif
}

void OnInitSuccess(LevelPlayConfiguration config)
{
    Debug.Log("LevelPlay initialized successfully on iOS");
}

void OnInitFailed(LevelPlayInitError error)
{
    Debug.LogError($"LevelPlay init failed on iOS: {error.ErrorMessage}");
}
```

Check the Xcode console for initialization output. Look for:
- ✅ "LevelPlay initialized successfully on iOS"
- ❌ Any initialization errors or warnings

### Testing Ads on iOS

1. **Test mode**: Enable test mode in your LevelPlay dashboard
2. **Real ads**: Disable test mode and test with real ads (use low-value test account)
3. **Different ATT states**: Test with:
   - ATT granted (tracking allowed)
   - ATT denied (tracking disallowed)
   - ATT restricted (device-level restriction)

## Common iOS Issues

### Issue: ATT prompt not showing

**Causes:**
- Missing `NSUserTrackingUsageDescription` in Info.plist
- ATT already determined on device
- Testing on iOS Simulator (use real device)

**Solutions:**
- Verify `iOSPostBuild.cs` exists in `Assets/Editor/` and ran successfully (check console for "iOSPostBuild: NSUserTrackingUsageDescription added to Info.plist")
- Reset privacy settings on device: **Settings > General > Transfer or Reset iPhone > Reset > Reset Location & Privacy**
- Test on physical iOS device

### Issue: Ads not loading after ATT denial

**This is expected behavior:**
- When users deny tracking, personalized ads are limited
- Non-personalized ads may still serve
- Revenue may be lower for users who deny tracking

**What to do:**
- Continue with LevelPlay initialization regardless of ATT response
- Don't block features if tracking is denied
- Comply with Apple's guidelines

### Issue: SKAdNetwork IDs missing

**Causes:**
- Outdated LevelPlay package
- Custom build postprocessor conflict
- Manual Info.plist modifications

**Solutions:**
- Update to latest LevelPlay Unity Package
- Check for conflicting build scripts
- Let LevelPlay handle SKAdNetwork IDs automatically

### Issue: App rejected for privacy reasons

**Common causes:**
- Missing privacy manifest declarations
- Incorrect ATT usage description
- Data collection not properly declared

**Solutions:**
- Review Apple's App Privacy Details guidelines
- Ensure ATT description is clear and user-friendly
- Declare all data types collected in App Store Connect
- Keep LevelPlay SDK updated (includes privacy manifest updates)

## iOS Privacy Checklist

Before submitting to App Store:

- [ ] ATT prompt implemented: `ATTRequester.mm` in `Assets/Plugins/iOS/`, `iOSPostBuild.cs` in `Assets/Editor/`, `LevelPlayInitializer.cs` updated to `IEnumerator Start()`
- [ ] SKAdNetwork IDs included in Info.plist
- [ ] Privacy manifest present and accurate
- [ ] App Store privacy declarations filled out
- [ ] Data collection disclosed transparently
- [ ] Tested on real iOS devices
- [ ] Verified ads load and show correctly
- [ ] Tested both ATT granted and denied scenarios
- [ ] No tracking of users who denied ATT
- [ ] Compliant with Apple's advertising policies

## Additional Resources

### Apple Documentation
- [App Tracking Transparency Guide](https://developer.apple.com/documentation/apptrackingtransparency)
- [SKAdNetwork Documentation](https://developer.apple.com/documentation/storekit/skadnetwork)
- [User Privacy and Data Use](https://developer.apple.com/app-store/user-privacy-and-data-use/)

### LevelPlay Documentation
- Refer to LevelPlay's Unity documentation for the latest iOS setup requirements
- Check for SDK update notes regarding iOS changes
- Review LevelPlay's privacy manifest documentation

## Version-Specific Notes

### iOS 14.5+
- ATT required for IDFA access
- Personalized ads limited without tracking permission

### iOS 15+
- Custom product pages support
- App privacy report visible to users

### iOS 16+
- Additional privacy enhancements
- More granular location privacy

### iOS 17+
- Privacy manifest required
- Increased API usage disclosure requirements
- SDK transparency requirements

## Best Practices

1. **Request ATT at an appropriate time**: Not immediately on first launch, but before showing ads
2. **Explain value to users**: Help users understand why you're requesting tracking
3. **Respect user choice**: Never block app functionality if tracking is denied
4. **Keep SDK updated**: Apple requirements change frequently
5. **Test thoroughly**: Test on real devices with different iOS versions
6. **Monitor metrics**: Track opt-in rates and ad performance post-iOS 14.5
7. **Stay compliant**: Follow Apple's Human Interface Guidelines for ATT
8. **Prepare for rejections**: Have documentation ready if Apple requests clarification
