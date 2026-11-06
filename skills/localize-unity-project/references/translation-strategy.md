## Recommended translation strategy
To efficiently translate an existing project, follow this multi-step workflow:

1. **Extraction & Component Setup:**
   - **Find all occurrences. There are two separate hiding places, and scanning one misses the other.**
     - **Authored text** sits on components in scenes and prefabs: legacy `UnityEngine.UI.Text` and
       TextMeshPro (`TMP_Text`, the base of `TextMeshProUGUI` and `TextMeshPro`). Walk **both**
       families. Measured on a real project: `FindObjectsByType<Text>` found 1 component while
       `FindObjectsByType<TMP_Text>` found 13, so a legacy-only pass reports success having done
       almost nothing.
     - **Text composed in code** never appears on a component at edit time, so no scene walk can see
       it. `scoreLabel.text = $"EXP {value}"` is invisible to every component-based scan and is the
       string that survives a "finished" localization pass. Find it in the C# instead:
       ```bash
       # Assignments and SetText calls that carry a string literal.
       grep -rnE '\.text\s*(=|\+=)\s*\$?"|SetText\(\s*\$?"' --include='*.cs' Assets/
       ```
       That pattern catches plain, interpolated, concatenated and `+=` forms plus `SetText`, and
       deliberately does not match `label.text = someVariable` or `label.text = Localize("KEY")`
       (nothing to extract at the first, already routed at the second). Its blind spot is a literal
       held in a variable or const declared elsewhere; if the count looks low for the project, grep
       that file's string literals too.
   - **Report what you did not convert.** A composed string usually needs a smart string or a format
     argument, which is a judgment call, and some are genuinely not worth localizing. Whichever you
     choose, list every site the scan found alongside whether it was converted, and why not if it
     wasn't. Reporting "localized 24 strings" while nine found sites went untouched is the failure
     mode this list exists to prevent: the work looks finished and the gap only surfaces in a
     screenshot from another locale.
   - **Shared Table:** Create a central String Table (e.g., `UIStrings`) with the base language and a "Context" column for each key to guide translators.
   - **Attach Components:** For every UI element found, attach a `LocalizeStringEvent` (for text) and the skill's `LocalizedFontAsset` component (for font swapping).
   - **Validation:** Ensure these components are set up with persistent listeners (`EditorAndRuntime`) so they update in the Editor immediately when the locale changes.

2. **Context-Aware Translation:**
   - **Translate:** Once the table is populated, provide translations for each locale.
   - **Context is King:** Always refer to the "Context" column or inspect the UI layout to ensure the translation fits the intended meaning and space.
   - **Grammar & Tone:** Ensure the tone matches the game's style. For example, use imperative verbs for buttons (e.g., German: "Lauf!" instead of "Laufen") and correct pluralization for labels (e.g., "Punkte" instead of "Punkt").

3. **Quality Assurance (QA):**
   - **Switch locale:** Through `eval`, run `LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("de");` for each target locale in turn, because a script call is repeatable and doesn't depend on an Editor window being open.
   - **Visual Inspection:** Methodically inspect every prefab and scene in the base language and all target languages.
   - **Layout Fit:** Check for text overflows or "tofu" (missing glyphs). Adjust font sizes or use `ContentSizeFitter` if strings are too long.
