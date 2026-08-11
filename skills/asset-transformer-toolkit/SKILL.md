---
name: asset-transformer-toolkit
description: Imports 3D models and point clouds into Unity using Asset Transformer Toolkit (formerly Pixyz Plugin), and creates, modifies, and executes RuleSets and Actions for optimization and transformation. Use this skill for any task involving RuleSets (.asset files) or ImporterScriptableObjects, including explaining, inspecting, creating, or modifying rules and actions. It also handles LOD generation. This is the primary method to import 3D models and pointclouds.
required_packages:
    com.unity.industry.toolkit: ">=4.0.0"
---
### API Reference
Tool functions are provided by `Unity.Pixyz.Plugin4Unity.Editor.AI.ATTAssistantUtilities`. They are documented inline in each reference file below alongside the classes they operate on.

### Technical Notes
Pixyz Plugin is the former name of Asset Transformer Toolkit. Prefer using the term 'Asset Transformer Toolkit' when addressing the user, unless the user is using the term 'Pixyz'
Most classes and code are in the Unity.Pixyz.Plugin4Unity.Editor assembly.
Asset Transformer Toolkit is NOT the same thing as Asset Transformer Studio/Pixyz Studio. NEVER rely on information about Asset Transformer Studio.

### Importers
Asset Transformer Toolkit can import 3D file from outside the project.
When modifying Importers, NEVER assume it should be immediately followed by a reimport. The import process can be very long, so it must only be launched when the user requests it.
Modify fields in Importers using the ScriptableObject API. NEVER use reflection to access or modify Importer fields — reflection is not available.
To create a Pixyz/Asset Transformer Toolkit Importer for importing a file, or to reimport a model using an existing ImporterScriptableObject, read [references/create-importer](references/create-importer.md)

### RuleSets and Actions
Read [references/rulesets-and-actions](references/rulesets-and-actions.md)
Read [api-docs/ruleset-api](api-docs/ruleset-api.md) when the RuleSet API is needed.
Read [api-docs/rule-api](api-docs/rule-api.md) when the Rule API is needed.
Read [api-docs/ruleblock-api](api-docs/ruleblock-api.md) when the RuleBlock API is needed, including `ActionBase.Id` for constructing `RuleBlock` instances.

### Levels of Detail
Read [references/lods](references/lods.md)
