# Table Completeness Check

Enumerating the tables answers "did every key get a value in every locale" mechanically, so a missing
entry is found before anyone plays the game. Run it and report the output.

```csharp
var gaps = new System.Collections.Generic.List<string>();
var checkedCount = 0;

foreach (var col in UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollections())
{
    foreach (var key in col.SharedData.Entries)
    {
        foreach (var table in col.StringTables)
        {
            checkedCount++;
            var entry = table.GetEntry(key.Id);
            // A missing entry and a present-but-empty entry both show as untranslated in game.
            if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
            {
                gaps.Add($"{col.TableCollectionName} / {table.LocaleIdentifier.Code} / {key.Key}");
            }
        }
    }
}

// Zero entries is not a pass. It means no collections or no locales were found, so the check
// examined nothing: report that distinctly instead of letting it read as success.
if (checkedCount == 0)
{
    return "INCONCLUSIVE: no table entries found. Either no String Table Collection exists yet, "
         + "or the collection has no locale tables. Fix that before trusting this check.";
}

return gaps.Count == 0
    ? $"COMPLETE: {checkedCount} entries checked, no gaps"
    : $"GAPS ({gaps.Count} of {checkedCount} checked):\n  " + string.Join("\n  ", gaps);
```

Verified on Unity 6000.5.8f1 against a table with one deliberately emptied `ja` value: it reports
`GAPS 1 of 4` naming exactly that entry, `COMPLETE (4 checked)` once the value is filled, and
`INCONCLUSIVE` in a project with no tables.

**Report the gap list rather than resolving it silently.** Some gaps are decisions, not mistakes: a
locale you were not asked to translate, or a key that is intentionally identical across languages.
Filling those with the English text hides the decision. List them and let the user say which are
intentional.
