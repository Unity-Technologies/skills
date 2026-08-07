void GenerateVariantAtlas(string masterPath, string variantPath, float scale)
{
    // Load master atlas (must exist and be imported first)
    SpriteAtlas masterRuntime = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(masterPath);
    if (masterRuntime == null)
    {
        Debug.LogError($"Master atlas not found: {masterPath}");
        return;
    }

    // Get original packables from master
    Object[] originalPackables = masterRuntime.GetPackables();

    // Create variant
    SpriteAtlasAsset variantAsset = new SpriteAtlasAsset();
    variantAsset.SetIsVariant(true);
    variantAsset.Add(originalPackables);
    variantAsset.SetMasterAtlas(masterRuntime);

    SpriteAtlasAsset.Save(variantAsset, variantPath);
    AssetDatabase.ImportAsset(variantPath);

    // Configure variant scale
    SpriteAtlasImporter importer = AssetImporter.GetAtPath(variantPath) as SpriteAtlasImporter;
    if (importer != null)
    {
        importer.variantScale = scale;
        importer.includeInBuild = true;
        importer.SaveAndReimport();
    }
}

// Usage in OnPreprocessBuild:
GenerateAtlasByFolder("Assets/Art/UI", "Assets/Atlases/UI_Master.spriteatlasv2");
GenerateVariantAtlas(
    "Assets/Atlases/UI_Master.spriteatlasv2",
    "Assets/Atlases/UI_HD.spriteatlasv2",
    0.5f  // HD variant at 50% scale
);
