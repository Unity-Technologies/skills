# LevelPlay Native Dependency Resolution

Detailed guidance for Step 4 of SKILL.md. LevelPlay requires native Android/iOS libraries that Unity's
package manager alone doesn't handle; a dependency manager bridges this gap. Read this when resolving
native dependencies, when the user does not yet have a dependency manager, or when diagnosing gradle
(Android) / CocoaPods (iOS) build failures.

## Contents
- [Check for an Existing Dependency Manager](#check-for-an-existing-dependency-manager)
- [If They Have a Dependency Manager](#if-they-have-a-dependency-manager)
- [If They Don't Have a Dependency Manager](#if-they-dont-have-a-dependency-manager)
- [Verification](#verification)
- [Android Custom Main Gradle Template (Older Versions)](#android-custom-main-gradle-template-older-versions)
- [Android API 33+ Requirement](#android-api-33-requirement)

LevelPlay requires native Android/iOS libraries that Unity's package manager alone doesn't handle. A
dependency manager bridges this gap. If this is a new project without a dependency manager already set
up, the steps below cover installing one.

## Check for an Existing Dependency Manager

Ask: "Do you have a dependency manager like Mobile Dependency Resolver (MDR), Unity External Dependency
Manager (UEDM), or External Dependency Manager for Unity (EDM4U) installed? Check your Assets folder for
these tools."

**If unsure**: Check Assets folder in Unity for folders named 'Mobile Dependency Resolver', 'External
Dependency Manager', or 'EDM4U'. If you see any folder with these names, answer 'Yes'. If not, answer 'No'.

## If They Have a Dependency Manager

**For Android:**
- With newer versions of Mobile Dependency Resolver (shipped with the Ads Mediation package): Dependencies auto-resolve on build (no manual action needed). If unsure, try building first — if it fails, manually resolve via the Android Resolver menu.
- With older MDR or other managers: Go to Assets > [Your Dependency Manager] > Android Resolver > Resolve
- Example paths:
  - MDR: `Assets > Mobile Dependency Resolver > Android Resolver > Resolve`
  - EDM4U: `Assets > External Dependency Manager > Android Resolver > Resolve`
- Menu paths may vary depending on dependency manager version. Look for 'Android Resolver' under your dependency manager's menu.

**For iOS:**
- All dependency managers require manual CocoaPods installation:
  - MDR: `Assets > Mobile Dependency Resolver > iOS Resolver > Install Cocoapods`
  - EDM4U: `Assets > External Dependency Manager > iOS Resolver > Install Cocoapods`
  - UEDM: Similar path under Unity External Dependency Manager

Ask: "Have you run the dependency resolution for your target platform(s)?"

**If targeting both Android and iOS**, complete dependency resolution for both platforms before proceeding.

## If They Don't Have a Dependency Manager

If the user didn't see the Mobile Dependency Resolver prompt during installation (see Step 3), restart
Unity Editor — the prompt may appear after restart. If it still doesn't appear, they may already have a
dependency manager installed.

When prompted:
1. Click **Import** on the prompt to install Mobile Dependency Resolver
2. After installation:
   - **Android**: Dependencies will auto-resolve on build (newer MDR versions)
   - **iOS**: Go to Assets > Mobile Dependency Resolver > iOS Resolver > Install Cocoapods

**Alternative**: Install another dependency manager like EDM4U if preferred (search for installation instructions in their documentation).

**Note**: Unity is transitioning to Unity External Dependency Manager (UEDM). If available in your Unity version, prefer UEDM over MDR.

## Verification

After resolution, verify:
- **Android**: Gradle dependencies in `Assets/Plugins/Android/`
- **iOS**: Podfile references or CocoaPods installation confirmation in console

**If dependency resolution fails**: Check the Unity console for specific error messages and share them for troubleshooting — or see `references/troubleshooting.md` for gradle and CocoaPods error guidance.

## Android Custom Main Gradle Template (Older Versions)

For older LevelPlay Unity package versions, manually enable the Custom Main Gradle Template:
1. Go to Edit > Project Settings > Player
2. Select Android tab
3. Expand Publishing Settings
4. Under Build, check Custom Main Gradle Template

In newer LevelPlay Unity package versions (with newer Mobile Dependency Resolver), this is enabled
automatically by default.

## Android API 33+ Requirement

If targeting Android API level 33 or higher, declare the AD_ID permission in AndroidManifest.xml:

```xml
<uses-permission android:name="com.google.android.gms.permission.AD_ID"/>
```

This permission is required for advertising ID access on Android 13+.

**If you skip this step and target Android API 33+:** advertising ID access will fail on Android 13+ devices.
