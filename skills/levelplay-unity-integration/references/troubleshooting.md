# LevelPlay Common Issues and Solutions

Detailed troubleshooting for issues that can occur during integration, referenced from the Common Issues
table in SKILL.md. Read this when the user reports a compile error, a build failure, ads not loading,
callbacks not firing, or a platform-specific build error.

**Note**: This section addresses issues that can occur during integration. If you haven't started the
integration yet, begin with Step 1: Verify Environment.

## Contents
- [CS0246 — namespace 'Unity.Services.LevelPlay' not found](#cs0246--namespace-unityserviceslevelplay-not-found)
- [Android gradle build fails / iOS build fails with dependency errors](#android-gradle-build-fails--ios-build-fails-with-dependency-errors)
- [Ads not loading](#ads-not-loading)
- [Callbacks not firing](#callbacks-not-firing)
- [Platform-specific build errors](#platform-specific-build-errors)

## CS0246 — namespace 'Unity.Services.LevelPlay' not found

**Root cause**: Ads Mediation package not installed in Unity project

**Symptoms:**
- Compiler errors: `The type or namespace name 'LevelPlay' could not be found`
- Compiler errors: `The type or namespace name 'Unity.Services.LevelPlay' could not be found`
- Red underlines in Unity Editor on all LevelPlay code

**Solutions:**
1. Stop providing code immediately
2. Read `Packages/packages-lock.json` and look for `com.unity.services.levelplay` (the package id behind the **Ads Mediation** display name). Answer this from the project, not by asking the user to read the Package Manager window: it is the same question, and reading it is both faster and reliable.
3. If the id is absent, the package is not installed. Direct to Step 3 to install via Unity Package Manager
4. Have them restart Unity Editor after installation (important!)
5. Verify installation by checking that `using Unity.Services.LevelPlay;` no longer shows errors
6. Only resume code generation after confirmation and verification

**Prevention**: Always verify package installation at Step 7 checkpoint before generating any code.

## Android gradle build fails / iOS build fails with dependency errors

**Root cause**: Native dependencies not resolved

**Symptoms:**
- **Android**: Gradle build errors mentioning missing dependencies or classes
- **iOS**: CocoaPods errors, missing frameworks, or linker errors
- Code compiles perfectly in Unity Editor but fails during platform build
- Build succeeds in Editor but crashes immediately on device

**Solutions:**
1. Verify you have a dependency manager installed (Mobile Dependency Resolver, Unity External Dependency Manager, or EDM4U)
2. Check your project's Assets folder for dependency manager tools
3. Run dependency resolution:
   - **Android (newer MDR versions)**: Should auto-resolve on build. If failing, manually resolve via Assets > Mobile Dependency Resolver > Android Resolver > Resolve
   - **Android (older/other managers)**: Assets > [Your Dependency Manager] > Android Resolver > Resolve
   - **iOS (all managers)**: Assets > [Your Dependency Manager] > iOS Resolver > Install Cocoapods
4. Verify resolution:
   - **Android**: Check `Assets/Plugins/Android/` for gradle files
   - **iOS**: Look for Podfile or CocoaPods confirmation in console
5. If you don't have a dependency manager, restart Unity - you should see a prompt to install Mobile Dependency Resolver
6. Rebuild for your target platform after resolution

**Prevention**: Complete Step 4 (dependency resolution) before building for Android/iOS.

## Ads not loading

**Possible causes:**
- SDK not initialized before loading ads
- Incorrect App Key
- Ad object created before initialization completes
- Network connectivity issues
- Ad inventory not available in test region

**Solutions:**
- Verify `LevelPlay.Init()` is called and `OnInitSuccess` fires before creating ad objects
- Create ad objects only after `OnInitSuccess` callback
- Check App Key matches LevelPlay dashboard
- Test on real devices with active internet connection
- Enable test mode in LevelPlay dashboard for guaranteed test ads (Note: dashboard test mode is separate from mock ads in the Unity Editor — it enables real test ads on device)

## Callbacks not firing

**Possible causes:**
- Events registered after SDK initialization
- Missing event subscriptions
- Script destroyed before callbacks execute

**Solutions:**
- Register callbacks before calling `Init()`
- Verify all callbacks are subscribed (check with Debug.Log statements)
- Use persistent GameObject with DontDestroyOnLoad if needed

## Platform-specific build errors

**iOS:**
- Ensure SKAdNetwork IDs are configured in Info.plist
- Verify ATT is implemented correctly (see `references/ios-setup.md`)
- Check Xcode build settings for required frameworks

**Android:**
- Verify Google Play Services is included
- Check AndroidManifest.xml for required permissions
- Ensure Gradle dependencies are resolved
