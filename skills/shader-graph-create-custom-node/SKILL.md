---
name: shader-graph-create-custom-node
description: "Generates custom Shader Graph nodes from HLSL code. Use when the user wants to create a new Shader Graph node or make existing HLSL code work as a reflected function node."
required_packages:
  com.unity.shadergraph: ">=17.5.0"
---
# Generating a custom Shader Graph node

## Step 1: Generate an HLSL function definition
- The function must be preceded by the preprocessor define `UNITY_EXPORT_REFLECTION`

## Step 2: Decorate with Shader Graph hint tags
- See `resources/all_hints.hlsl` for all valid hint tags and usage patterns
- C#-style documentation tags are supported outside of `funchints` or `paramhints` blocks
- Required function hints:
  - `sg:ProviderKey`
  - `sg:SearchCategory`
  - `sg:SearchTerms`
  - `sg:DisplayName`

## Step 3: Write the code to an asset
- Search the project for a `ShaderInclude` asset (`.hlsl`) that already contains custom Shader Graph nodes
- If a matching asset exists, show the user its current contents and ask for confirmation before appending the new code
- If no matching asset exists, create a new `.hlsl` asset
- Ensure the file begins with `#include "ShaderApiReflectionSupport.hlsl"`
