# LevelPlay Ad Unit Implementation Best Practices

## Contents
- [Overview](#overview)
- [Strategic Framework: Revenue vs UX Optimization](#strategic-framework-revenue-vs-ux-optimization)
- [Ad Format Strategy by Goal](#ad-format-strategy-by-goal)
- [Placement Strategy](#placement-strategy)
- [Implementation Patterns](#implementation-patterns)
- [Frequency Management](#frequency-management)
- [Error Handling and Graceful Degradation](#error-handling-and-graceful-degradation)
- [A/B Testing Considerations](#ab-testing-considerations)
- [Common Mistakes to Avoid](#common-mistakes-to-avoid)
- [Platform-Specific Considerations](#platform-specific-considerations)
- [Success Metrics by Strategy](#success-metrics-by-strategy)
- [Final Recommendations](#final-recommendations)

## Overview

This reference compiles best practices from the LevelPlay enablement playbook, covering strategy selection, placement optimization, and common implementation patterns that drive both revenue and user satisfaction.

## Strategic Framework: Revenue vs UX Optimization

### Understanding the Trade-off

Every ad implementation involves a fundamental trade-off between revenue generation and user experience. Your optimization goal should guide every decision.

| Optimization Goal | Philosophy | Typical Outcome |
|-------------------|------------|-----------------|
| **Revenue-Focused** | Maximize impression opportunities | Higher revenue, potential UX impact |
| **UX-Focused** | Protect user experience at all costs | Better retention, lower short-term revenue |
| **Balanced** | Optimize for sustainable long-term value | Moderate revenue with good UX |

### Choosing Your Optimization Goal

**Ask yourself:**
1. What stage is your app in? (Early = UX focus, Mature = revenue optimization)
2. What's your business model? (Ads-only = revenue, Hybrid = balanced)
3. What's your competitive landscape? (High competition = UX focus)
4. What are your user expectations? (Casual games = more ads tolerated)

## Ad Format Strategy by Goal

### Revenue-Focused Implementation

**Goal**: Maximize ad revenue and impression opportunities

**Rewarded Ads** (Primary):
- Implement 3-5 placements across high-value moments
- Examples: Hints, extra lives, power-ups, bonus currency, skip levels
- Always keep a rewarded ad loaded and ready
- Reload immediately after showing

**Interstitial Ads** (Secondary):
- Show at every natural transition point
- Frequency cap: Every 3-5 minutes of active gameplay
- Placements: Level complete, game over, returning to menu
- Load proactively before transitions

**Banner Ads** (Tertiary):
- Show persistently during gameplay
- Position: Bottom (less intrusive than top)
- Keep loaded throughout session for continuous impressions
- Accept some visual clutter for revenue

**Expected Outcomes:** *(Industry benchmarks, not LevelPlay-specific)*
- ✅ 30-50% higher ad revenue
- ⚠️ 5-15% increase in early abandonment
- ⚠️ Lower user satisfaction scores
- ✅ Strong monetization from engaged users

### UX-Focused Implementation

**Goal**: Maintain excellent user experience while monetizing thoughtfully

**Rewarded Ads** (Only or Primary):
- Single placement or 2 maximum
- User-initiated only (player chooses to watch)
- High-value rewards that feel generous
- Never force ads

**Interstitial Ads** (Optional, Rare):
- Only at major session boundaries (returning to menu, closing app)
- Frequency cap: 10+ minutes or once per session
- Skip during active gameplay entirely

**Banner Ads** (Avoid or Minimize):
- Show only in menus, never during gameplay
- If used, small sizes (standard 320x50, not rectangles)
- Hide during any active user engagement

**Expected Outcomes:** *(Industry benchmarks, not LevelPlay-specific)*
- ⚠️ 40-60% lower ad revenue vs revenue-focused
- ✅ Better user retention and satisfaction
- ✅ Higher organic growth and word-of-mouth
- ✅ Premium brand perception

### Balanced Implementation (Recommended for Most Apps)

**Goal**: Sustainable monetization with good user experience

**Rewarded Ads** (Primary):
- 2-3 well-chosen placements
- Focus on moments of high user engagement
- Make rewards feel valuable but not exploitative
- Examples: Extra attempts, time skips, premium currency

**Interstitial Ads** (Moderate):
- Natural transition points only (level complete, game over)
- Frequency cap: Every 5-7 minutes
- Never interrupt active gameplay
- Respect user flow

**Banner Ads** (Selective):
- Show in menus and low-attention moments
- Hide during intense or immersive gameplay
- Position at bottom with proper UI offset
- Rotation: Show/hide based on context

**Expected Outcomes:** *(Industry benchmarks, not LevelPlay-specific)*
- ✅ Strong ad revenue (70-85% of revenue-focused approach)
- ✅ Good user retention
- ✅ Sustainable long-term monetization
- ✅ Positive user sentiment

## Placement Strategy

### Identifying Strong Placement Moments

**High-value moments** (Best for rewarded ads):
- Post-victory celebrations
- Level progression milestones
- Unlock opportunities
- User feels positive emotion

**Natural transitions** (Good for interstitials):
- Level complete
- Game over
- Returning to main menu
- Session boundaries

**Low-attention periods** (Acceptable for banners):
- Waiting screens
- Loading periods
- Menu browsing
- Non-interactive moments

### Placement Anti-Patterns (Avoid These)

❌ **Mid-gameplay interruptions**: Never show interstitials during active play
❌ **Bait-and-switch**: Don't offer rewards then show unrewarded interstitials
❌ **Excessive frequency**: Showing ads every 1-2 minutes frustrates users
❌ **Punitive ads**: Using ads as punishment for failure feels manipulative
❌ **Blocking progression**: Making ads mandatory to continue playing
❌ **Poor timing**: Showing ads right before key moments or cliffhangers

## Implementation Patterns

### Pattern 1: Rewarded Hints System

**Use case**: Puzzle or strategy games where hints add value

```csharp
public class HintSystem : MonoBehaviour
{
    [SerializeField] private RewardedAdManager adManager;
    [SerializeField] private int hintsAvailable = 3;

    public void RequestHint()
    {
        if (hintsAvailable > 0)
        {
            // User has free hints remaining
            UseHint();
            hintsAvailable--;
        }
        else
        {
            // Offer ad-based hint
            if (adManager.IsRewardedAdAvailable())
            {
                ShowHintAdOffer();
            }
            else
            {
                ShowHintNotAvailableMessage();
            }
        }
    }

    private void ShowHintAdOffer()
    {
        // Show dialog: "Watch an ad to get a hint?"
        // If user accepts:
        adManager.ShowRewardedAd(OnHintAdCompleted);
    }

    private void OnHintAdCompleted(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        if (reward != null && !string.IsNullOrEmpty(reward.Name))
        {
            // User completed ad, grant hint
            UseHint();
        }
    }

    private void UseHint()
    {
        // Show hint to player
        Debug.Log("Showing hint");
    }

    private void ShowHintNotAvailableMessage()
    {
        // Inform user that hints aren't available right now
        Debug.Log("Hints not available at the moment");
    }
}
```

**Why this works:**
- User has free hints first (generous)
- Ad is optional, not forced
- Clear value exchange (ad for hint)
- Graceful handling when ads unavailable

### Pattern 2: Level Transition Interstitial (Frequency Capped)

**Use case**: Showing interstitials between levels without annoying users

```csharp
public class LevelTransitionAds : MonoBehaviour
{
    [SerializeField] private InterstitialAdManager adManager;
    private int levelsCompletedSinceAd = 0;
    private int levelsRequiredBetweenAds = 3; // Show ad every 3 levels

    public void OnLevelComplete()
    {
        levelsCompletedSinceAd++;

        if (levelsCompletedSinceAd >= levelsRequiredBetweenAds)
        {
            ShowInterstitialOpportunistically();
            levelsCompletedSinceAd = 0;
        }

        ProceedToNextLevel();
    }

    private void ShowInterstitialOpportunistically()
    {
        // Try to show, but don't wait if not ready
        if (adManager.IsInterstitialReady())
        {
            adManager.ShowInterstitialAd();
        }
    }

    private void ProceedToNextLevel()
    {
        // Load next level regardless of ad status
        // Never block user flow waiting for ads
    }
}
```

**Why this works:**
- Frequency capping prevents ad fatigue
- Opportunistic showing (doesn't block if not ready)
- User flow never interrupted
- Predictable pattern users can adapt to

### Pattern 3: Context-Aware Banner Management

**Use case**: Showing banners in menus but hiding during gameplay

```csharp
public class ContextAwareBanners : MonoBehaviour
{
    [SerializeField] private BannerAdManager bannerManager;

    public enum AppContext
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    private AppContext currentContext;

    public void SetContext(AppContext newContext)
    {
        currentContext = newContext;
        UpdateBannerVisibility();
    }

    private void UpdateBannerVisibility()
    {
        switch (currentContext)
        {
            case AppContext.MainMenu:
            case AppContext.GameOver:
                // Show banners in menu contexts
                bannerManager.ShowBanner();
                break;

            case AppContext.Playing:
                // Hide during active gameplay
                bannerManager.HideBanner();
                break;

            case AppContext.Paused:
                // Optional: Show or hide based on your preference
                // For UX-focused: Hide
                // For revenue-focused: Show
                bannerManager.ShowBanner();
                break;
        }
    }
}
```

**Why this works:**
- Banners shown only in appropriate contexts
- Gameplay experience uninterrupted
- Easy to adjust strategy by changing visibility logic
- Clean separation of concerns

## Frequency Management

### Time-Based Frequency Capping

Implement minimum time intervals between ads:

```csharp
public class FrequencyManager : MonoBehaviour
{
    private float minSecondsBetweenInterstitials = 300f; // 5 minutes
    private float lastInterstitialTime = -300f; // Allow first ad immediately

    public bool CanShowInterstitial()
    {
        float timeSinceLast = Time.realtimeSinceStartup - lastInterstitialTime;
        return timeSinceLast >= minSecondsBetweenInterstitials;
    }

    public void RecordInterstitialShown()
    {
        lastInterstitialTime = Time.realtimeSinceStartup;
    }
}
```

**Recommended intervals:**
- Revenue-focused: 3-5 minutes
- Balanced: 5-7 minutes
- UX-focused: 10+ minutes

### Count-Based Frequency Capping

Implement minimum actions between ads:

```csharp
private int actionsRequiredBetweenAds = 3;
private int actionsSinceLastAd = 0;

public void OnUserAction()
{
    actionsSinceLastAd++;

    if (actionsSinceLastAd >= actionsRequiredBetweenAds)
    {
        // Eligible to show ad
        TryShowAd();
        actionsSinceLastAd = 0;
    }
}
```

**Examples of "actions":**
- Levels completed
- Games played
- Sessions started
- Feature uses

## Error Handling and Graceful Degradation

### Always Have a Fallback

Never let ad failures block user flow:

```csharp
public void OnAdLoadFailed()
{
    // Log for debugging
    Debug.LogWarning("Ad failed to load");

    // Continue with user flow
    ProceedWithoutAd();

    // Schedule retry
    Invoke(nameof(RetryLoadAd), 30f);
}

private void ProceedWithoutAd()
{
    // Your app continues normally
    // Never block users because ads aren't available
}
```

### Reward Users Even When Ads Fail

For rewarded ads, consider granting rewards even if ad fails:

```csharp
public void OnRewardedAdShowFailed()
{
    // User intended to watch ad, but it failed
    // Consider granting reward anyway (generous approach)
    if (shouldBeGenerousOnFailure)
    {
        GrantReward();
        ShowMessage("Reward granted! (Ad unavailable)");
    }
    else
    {
        ShowMessage("Ad unavailable, please try again later");
    }
}
```

**When to be generous:**
- UX-focused strategy
- High-value users
- Failure is on your end (not user's fault)

## A/B Testing Considerations

### What to Test

**High-impact variables:**
1. **Frequency caps**: 3min vs 5min vs 7min between interstitials
2. **Placement timing**: Immediate vs delayed interstitials
3. **Reward generosity**: 1x vs 2x multiplier for rewarded ads
4. **Banner visibility**: Always vs context-aware vs never during gameplay
5. **Interstitial presence**: With vs without interstitials

**How to test:**
- Split users into cohorts
- Track both revenue AND retention
- Run tests for 7-14 days minimum
- Focus on long-term value (LTV), not just day-1 revenue

### Metrics to Track

**Revenue metrics:**
- ARPDAU (Average Revenue Per Daily Active User)
- Impression per DAU
- eCPM (effective Cost Per Mille)

**UX metrics:**
- D1, D7, D30 retention
- Session length
- Session frequency
- Organic virality (shares, referrals)

**Balance metrics:**
- LTV (Lifetime Value)
- ARPU / Retention ratio
- User satisfaction scores

## Common Mistakes to Avoid

1. **Over-monetizing early**: Don't show tons of ads before users are engaged
2. **Ignoring retention**: High revenue means nothing if users quit
3. **Blocking without ads**: Never require watching ads to progress
4. **Poor frequency capping**: Ads every minute frustrate users
5. **Forcing rewarded ads**: Rewarded ads should always be optional
6. **Ignoring load failures**: Always handle ad failures gracefully
7. **One-size-fits-all**: Different user segments may need different strategies
8. **Neglecting testing**: Always A/B test major monetization changes

## Platform-Specific Considerations

### iOS
- Lower opt-in rates post-ATT (iOS 14.5+)
- Users who deny tracking see lower-value ads
- More privacy-conscious user base
- Consider more UX-focused approach

### Android
- Higher ad fill rates
- More permissive user expectations
- Diverse hardware requires testing
- Can push frequency slightly higher

## Success Metrics by Strategy

*(Example benchmarks from industry typical ranges, not LevelPlay-specific targets)*

### Revenue-Focused Success
- ARPDAU > $0.15
- Impression/DAU > 8
- D7 retention > 20%

### UX-Focused Success
- D7 retention > 35%
- Session length > 15 minutes
- Low uninstall rate (<5% weekly)
- ARPDAU > $0.05

### Balanced Success
- ARPDAU > $0.10
- D7 retention > 28%
- Impression/DAU > 5
- High user satisfaction

## Final Recommendations

1. **Start UX-focused**: Better to under-monetize early than drive users away
2. **Increase gradually**: Add monetization as users become engaged
3. **Always test**: Data beats assumptions
4. **Watch retention closely**: It's easier to add ads than win back users
5. **Be transparent**: Users appreciate honesty about ad-supported models
6. **Reward patience**: Give users free options before pushing ads
7. **Optimize continuously**: Ad strategy should evolve with your product

## Additional Resources

For detailed API implementation, see:
- `rewarded-api.md`: Rewarded ad implementation
- `interstitial-api.md`: Interstitial ad implementation
- `banner-api.md`: Banner ad implementation
- `initialization-api.md`: SDK setup and configuration
