# Privacy and Regulation Settings

## Contents
- [Overview](#overview)
- [Quick Reference](#quick-reference)
- [GDPR Consent Management](#gdpr-consent-management)
- [CCPA Compliance](#ccpa-compliance)
- [COPPA Compliance](#coppa-compliance)
- [Combined Compliance Example](#combined-compliance-example)
- [Deprecated APIs (SDK 9.3.0 and Lower)](#deprecated-apis-sdk-930-and-lower)
- [Best Practices](#best-practices)
- [Testing Privacy Settings](#testing-privacy-settings)
- [Common Issues](#common-issues)

## Overview

LevelPlay SDK provides APIs to comply with privacy regulations including GDPR, CCPA, and COPPA. These settings must be configured before SDK initialization to ensure compliance with regional privacy laws.

**SDK Version Requirement:** SDK 9.4.0+ for current APIs (older deprecated APIs available in SDK 9.3.0 and lower)

## Quick Reference

| Regulation | SDK 9.5.0+ API | SDK 9.4.x API | When to Use |
|------------|---------------|---------------|-------------|
| **GDPR** | `LevelPlayPrivacySettings.SetGDPRConsent(bool)` | `LevelPlayPrivacySettings.SetGDPRConsents(Dictionary)` | EU users, requires explicit consent |
| **CCPA** | `LevelPlayPrivacySettings.SetCCPA(true)` | `LevelPlayPrivacySettings.SetCCPA(true)` | California users opting out of data sale |
| **COPPA** | `LevelPlayPrivacySettings.SetCOPPA(true)` | `LevelPlayPrivacySettings.SetCOPPA(true)` | Apps directed at children under 13 |

## GDPR Consent Management

### What is GDPR?

The General Data Protection Regulation (GDPR) is a European Union privacy law requiring explicit user consent for data collection and processing. Apps serving EU users must implement GDPR consent flows.

### Set GDPR Consent

The GDPR consent API changed between SDK versions. Check your installed SDK version in **Ads Mediation > Network Manager**.

**SDK 9.5.0+ — global consent boolean:**

```csharp
using Unity.Services.LevelPlay;

// true = user has granted consent, false = user has not consented
// Call BEFORE SDK initialization
LevelPlayPrivacySettings.SetGDPRConsent(true);
```

**Parameters:**
- `true`: User has granted consent for data collection across all networks
- `false`: User has denied consent (non-personalized ads only)

**SDK 9.4.x — per-network consent dictionary:**

```csharp
using Unity.Services.LevelPlay;
using System.Collections.Generic;

// Set consent for each installed ad network
// Call BEFORE SDK initialization
LevelPlayPrivacySettings.SetGDPRConsents(new Dictionary<string, bool> {
    { "UnityAds", true },
    { "IronSource", true }
    // Add an entry for each network you have installed — see Supported Network Keys below
});
```

**Important:**
- Call **BEFORE** `LevelPlay.Init()`
- `SetGDPRConsent(bool)` (singular) was introduced in SDK 9.5.0. If you are on SDK 9.4.x, use `SetGDPRConsents(Dictionary)` instead.
- `SetGDPRConsents(Dictionary)` is deprecated as of SDK 9.5.0+ — migrate to `SetGDPRConsent(bool)` when you upgrade.

### Supported Network Keys (for SetGDPRConsents)

| Network Key | Ad Network |
|-------------|------------|
| `UnityAds` | Unity Ads |
| `AdMob` | Google AdMob |
| `AppLovin` | AppLovin |
| `APS` | Amazon Publisher Services |
| `BidMachine` | BidMachine |
| `Bigo` | Bigo Ads |
| `Chartboost` | Chartboost |
| `Facebook` | Meta Audience Network |
| `Fyber` | Digital Turbine (Fyber) |
| `HyprMx` | HyprMX |
| `InMobi` | InMobi |
| `Line` | LINE Ads |
| `Mintegral` | Mintegral |
| `MobileFuse` | MobileFuse |
| `Moloco` | Moloco |
| `MyTarget` | myTarget |
| `Ogury` | Ogury |
| `Pangle` | Pangle (TikTok) |
| `PubMatic` | PubMatic |
| `Smaato` | Smaato |
| `SuperAwesome` | SuperAwesome |
| `Verve` | Verve |
| `Voodoo` | Voodoo |
| `Vungle` | Vungle |
| `Yandex` | Yandex Ads |
| `YSO` | YSO |

### Complete GDPR Implementation Example

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class GDPRConsentManager : MonoBehaviour
{
    [SerializeField] private string appKey;

    void Start()
    {
        // Check if user is in GDPR region (EU)
        if (IsUserInGDPRRegion())
        {
            // Show consent dialog and get user's answer
            ShowConsentDialog();
        }
        else
        {
            // Not in GDPR region, initialize normally
            InitializeLevelPlay();
        }
    }

    private bool IsUserInGDPRRegion()
    {
        // Implement your region detection logic
        // Options: IP geolocation, device locale, or third-party consent SDK
        return false; // Placeholder
    }

    private void ShowConsentDialog()
    {
        // Show your GDPR consent UI
        // When user makes a choice, call OnConsentReceived()
    }

    private void OnConsentReceived(bool userConsented)
    {
        // Store consent for future sessions
        PlayerPrefs.SetInt("GDPR_Consent", userConsented ? 1 : 0);
        PlayerPrefs.Save();

        // Apply consent to LevelPlay (BEFORE Init)
        LevelPlayPrivacySettings.SetGDPRConsent(userConsented);

        // Initialize SDK
        InitializeLevelPlay();
    }

    private void InitializeLevelPlay()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay initialized with GDPR consent applied");
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay init failed: {error.ErrorMessage}");
    }
}
```

### Updating Consent

If user changes consent preferences after initialization:

```csharp
// User updates consent — apply immediately
LevelPlayPrivacySettings.SetGDPRConsent(false); // User revoked consent

// Note: Changes apply to future ad requests
// Currently loaded ads are not affected
```

---

## CCPA Compliance

### What is CCPA?

The California Consumer Privacy Act (CCPA) gives California residents the right to opt out of the "sale" of their personal information. Apps must provide a "Do Not Sell My Personal Information" option.

### Set CCPA Opt-Out (SDK 9.4.0+)

Indicate that the user has opted out of data sale:

```csharp
using Unity.Services.LevelPlay;

// User has opted out of data sale (call BEFORE SDK initialization)
LevelPlayPrivacySettings.SetCCPA(true);
```

**Parameters:**
- `true`: User has opted out of data sale (restrict data collection)
- `false`: User has not opted out (default behavior)

**When to call:** BEFORE `LevelPlay.Init()`

### Complete CCPA Implementation Example

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class CCPAComplianceManager : MonoBehaviour
{
    [SerializeField] private string appKey;
    private bool userOptedOut = false;

    void Start()
    {
        // Check if user is in California
        if (IsUserInCalifornia())
        {
            // Load previously saved opt-out preference
            userOptedOut = PlayerPrefs.GetInt("CCPA_OptOut", 0) == 1;

            // Show "Do Not Sell My Info" option in settings
            // If user hasn't made a choice, you may show a prompt
        }

        // Apply CCPA setting before initialization
        if (userOptedOut)
        {
            LevelPlayPrivacySettings.SetCCPA(true);
        }

        // Initialize SDK
        InitializeLevelPlay();
    }

    private bool IsUserInCalifornia()
    {
        // Implement your region detection logic
        // Options: IP geolocation, device locale
        return false; // Placeholder
    }

    // Call this when user toggles "Do Not Sell" in settings
    public void OnUserToggleCCPAOptOut(bool optOut)
    {
        userOptedOut = optOut;

        // Save preference
        PlayerPrefs.SetInt("CCPA_OptOut", optOut ? 1 : 0);
        PlayerPrefs.Save();

        // Apply to SDK
        LevelPlayPrivacySettings.SetCCPA(optOut);

        Debug.Log($"CCPA opt-out set to: {optOut}");
    }

    private void InitializeLevelPlay()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay initialized with CCPA settings applied");
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay init failed: {error.ErrorMessage}");
    }
}
```

---

## COPPA Compliance

### What is COPPA?

The Children's Online Privacy Protection Act (COPPA) is a US federal law protecting children under 13. Apps directed at children must not collect personal information without parental consent.

### Set Child-Directed Treatment (SDK 9.4.0+)

Indicate that your app is directed at children:

```csharp
using Unity.Services.LevelPlay;

// App is directed at children under 13 (call BEFORE SDK initialization)
LevelPlayPrivacySettings.SetCOPPA(true);
```

**Parameters:**
- `true`: App is child-directed, apply COPPA restrictions
- `false`: App is not child-directed (default)

**When to call:** BEFORE `LevelPlay.Init()`

### COPPA Implementation Example

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class COPPACompliantInitializer : MonoBehaviour
{
    [SerializeField] private string appKey;
    [SerializeField] private bool isChildDirectedApp = true; // Set this based on your app

    void Start()
    {
        // Set COPPA compliance BEFORE initialization
        if (isChildDirectedApp)
        {
            LevelPlayPrivacySettings.SetCOPPA(true);
            Debug.Log("COPPA child-directed treatment enabled");
        }

        // Initialize SDK
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay initialized with COPPA compliance");
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay init failed: {error.ErrorMessage}");
    }
}
```

### Additional COPPA Considerations

If your app is child-directed:

1. **Disable personalized ads**: COPPA requires non-personalized ads only
2. **Google Play Families Policy**: If targeting children on Google Play:
   - Set COPPA flag: `LevelPlayPrivacySettings.SetCOPPA(true)`
   - Ensure ad networks comply with Google Play Families requirements
   - Test thoroughly before submission
3. **App Store age ratings**: Set appropriate age ratings in both app stores
4. **Privacy policy**: Clearly state data collection practices for children

---

## Combined Compliance Example

If you need to support multiple regulations:

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;
using System.Collections.Generic;

public class PrivacyComplianceManager : MonoBehaviour
{
    [SerializeField] private string appKey;
    [SerializeField] private bool isChildDirectedApp = false;

    void Start()
    {
        // Apply all applicable privacy settings BEFORE initialization

        // 1. COPPA (if child-directed app)
        if (isChildDirectedApp)
        {
            LevelPlayPrivacySettings.SetCOPPA(true);
        }

        // 2. GDPR (if user in EU)
        if (IsUserInGDPRRegion())
        {
            if (HasStoredGDPRConsent())
            {
                LevelPlayPrivacySettings.SetGDPRConsent(LoadGDPRConsent());
            }
            else
            {
                // Show consent dialog first, then initialize
                ShowGDPRConsentDialog();
                return; // Don't initialize yet
            }
        }

        // 3. CCPA (if user in California)
        if (IsUserInCalifornia())
        {
            bool ccpaOptOut = LoadCCPAOptOut();
            if (ccpaOptOut)
            {
                LevelPlayPrivacySettings.SetCCPA(true);
            }
        }

        // Initialize after applying all privacy settings
        InitializeLevelPlay();
    }

    private bool HasStoredGDPRConsent()
    {
        return PlayerPrefs.HasKey("GDPR_Consent");
    }

    private bool LoadGDPRConsent()
    {
        return PlayerPrefs.GetInt("GDPR_Consent", 0) == 1;
    }

    private bool LoadCCPAOptOut()
    {
        return PlayerPrefs.GetInt("CCPA_OptOut", 0) == 1;
    }

    private bool IsUserInGDPRRegion()
    {
        // Implement region detection
        return false;
    }

    private bool IsUserInCalifornia()
    {
        // Implement region detection
        return false;
    }

    private void ShowGDPRConsentDialog()
    {
        // Show your consent UI, then call OnGDPRConsentsReceived()
    }

    private void OnGDPRConsentReceived(bool userConsented)
    {
        PlayerPrefs.SetInt("GDPR_Consent", userConsented ? 1 : 0);
        PlayerPrefs.Save();
        LevelPlayPrivacySettings.SetGDPRConsent(userConsented);
        InitializeLevelPlay();
    }

    private void InitializeLevelPlay()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay initialized with privacy compliance");
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay init failed: {error.ErrorMessage}");
    }
}
```

---

## Deprecated APIs (SDK 9.3.0 and Lower)

**⚠️ These APIs are deprecated. Use the current `LevelPlayPrivacySettings` APIs instead.**

### Deprecated GDPR API — LevelPlay.SetConsent

```csharp
// DEPRECATED - Do not use
LevelPlay.SetConsent(true); // Grants consent for all networks
LevelPlay.SetConsent(false); // Denies consent for all networks
```

**Status:** Marked as `[Obsolete]` in SDK code.

**Migration:** Use `LevelPlayPrivacySettings.SetGDPRConsents(Dictionary)` (SDK 9.4.x) or `LevelPlayPrivacySettings.SetGDPRConsent(bool)` (SDK 9.5.0+).

### Deprecated GDPR API — SetGDPRConsents (per-network dictionary, SDK 9.5.0+)

```csharp
// DEPRECATED as of SDK 9.5.0+ — generates compiler warning
// NOTE: This is the CORRECT API for SDK 9.4.x. Only deprecated from 9.5.0 onwards.
Dictionary<string, bool> consents = new Dictionary<string, bool>
{
    { "UnityAds", true },
    { "AdMob", true }
};
LevelPlayPrivacySettings.SetGDPRConsents(consents);
```

**Note:** `SetGDPRConsents(Dictionary)` is the correct API for SDK 9.4.x. It becomes deprecated only in SDK 9.5.0+, where it is replaced by `SetGDPRConsent(bool)`. If you are on 9.5.0+, migrate to the boolean API.

**Migration (9.5.0+ only):** Use `LevelPlayPrivacySettings.SetGDPRConsent(true/false)` instead.

### Deprecated CCPA API

```csharp
// DEPRECATED - Do not use
LevelPlay.SetMetaData("do_not_sell", "true");
LevelPlay.SetMetaData("do_not_sell", "false");
```

**Migration:** Use `LevelPlayPrivacySettings.SetCCPA(true)` instead.

### Deprecated COPPA API

```csharp
// DEPRECATED - Do not use
LevelPlay.SetMetaData("is_child_directed", "true");
LevelPlay.SetMetaData("is_child_directed", "false");
```

**Migration:** Use `LevelPlayPrivacySettings.SetCOPPA(true)` instead.

---

## Best Practices

1. **Call before initialization**: All privacy settings must be applied before `LevelPlay.Init()`
2. **Persist user choices**: Store consent preferences and reapply on each app launch
3. **Detect user region**: Use geolocation or device locale to determine applicable regulations
4. **Provide UI controls**: Give users easy access to privacy settings
5. **Respect user choices**: Honor opt-outs and consent denials
6. **Update promptly**: When user changes preferences, apply immediately via privacy APIs
7. **Test thoroughly**: Verify compliance in all supported regions
8. **Document clearly**: Include privacy policy links in your app
9. **Use consent management platforms**: Consider third-party CMPs for complex consent requirements
10. **Stay updated**: Privacy regulations evolve, monitor SDK updates for compliance changes

---

## Testing Privacy Settings

### Verify Settings Are Applied

```csharp
void Start()
{
    // Set privacy settings
    LevelPlayPrivacySettings.SetCOPPA(true);
    LevelPlayPrivacySettings.SetGDPRConsent(true); // true = user consented
    LevelPlayPrivacySettings.SetCCPA(true);

    // Initialize
    LevelPlay.OnInitSuccess += OnInitSuccess;
    LevelPlay.Init(appKey);
}

private void OnInitSuccess(LevelPlayConfiguration config)
{
    Debug.Log("Privacy settings applied successfully");
    // Check console logs from SDK for confirmation
}
```

### Test Scenarios

1. **GDPR - All consents granted**: Set all network consents to `true`, verify ads load
2. **GDPR - All consents denied**: Set all network consents to `false`, verify limited ads
3. **CCPA opt-out**: Set `SetCCPA(true)`, verify data collection restricted
4. **COPPA enabled**: Set `SetCOPPA(true)`, verify child-safe ad delivery
5. **Combined**: Apply multiple regulations, verify all honored

---

## Common Issues

### Issue: Privacy settings not taking effect

**Causes:**
- Privacy APIs called after SDK initialization
- Settings not persisted across app launches

**Solutions:**
- Always call privacy APIs BEFORE `LevelPlay.Init()`
- Store user preferences and reapply on every launch

### Issue: Using deprecated APIs

**Cause:** Following outdated documentation or examples using `LevelPlay.SetConsent()` or `SetGDPRConsents(Dictionary)`

**Solution:** Migrate to current APIs — use `LevelPlayPrivacySettings.SetGDPRConsent(bool)` for GDPR

---

## Additional Resources

- [LevelPlay Privacy Documentation](https://docs.unity.com/en-us/grow/levelplay)
- [GDPR Overview](https://gdpr.eu/)
- [CCPA Information](https://oag.ca.gov/privacy/ccpa)
- [COPPA Requirements](https://www.ftc.gov/legal-library/browse/rules/childrens-online-privacy-protection-rule-coppa)
- [Google Play Families Policy](https://support.google.com/googleplay/android-developer/answer/9893335)
