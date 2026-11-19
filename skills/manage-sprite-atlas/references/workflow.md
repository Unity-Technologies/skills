# Complete workflow

```
User Request
    ↓
Step 2: Check for existing scripts with [UNITY-SKILL:SPRITEATLAS] identifier
    ↓
    ├─ Found existing scripts?
    │   ↓ YES
    │   Ask user: Update existing / Create new / Abort
    │   ↓
    │   Handle user choice
    │
    └─ NO existing scripts or user chose "Create new"
        ↓
        Ask "Built-in data or Addressables?"
        ↓
        ├─ Option A: Built-in
        │   ↓
        │   Generate 1 script: Prebuild generator (includeInBuild=true)
        │
        └─ Option B: Addressables
            ↓
            Generate 3 scripts:
              1. Prebuild: Generate atlases + create Addressables entries (includeInBuild=false)
              2. Postprocess: Build Addressables bundles
              3. Runtime: Late-binding loader component
```
