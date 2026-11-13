#!/usr/bin/env bash
# scripts/dependency/com.unity.xr.hands/1.9.sh — OpenXR hand-tracking feature class names (setup step 4 with --hands).
# Picked by the installed com.unity.xr.hands version (highest file not newer). Class names, not display names:
#   HandTracking ("Hand Tracking Subsystem") and MetaHandTrackingAim ("Meta Hand Tracking Aim") come from com.unity.xr.hands;
#   HandInteractionProfile and HandCommonPosesInteraction ("Hand Interaction Poses") come from com.unity.xr.openxr.
HANDS_REQUIRED="HandTracking HandInteractionProfile HandCommonPosesInteraction"   # every target tab
HANDS_QUEST_ANDROID="MetaHandTrackingAim"                                         # Android tab, Meta Quest only
