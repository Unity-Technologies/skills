# Open Unity Search Window

Use this pattern when the skill should open Unity Search pre-populated with a generated query.

This is a read-only Editor UI action. It must not create, modify, delete, import, install, or save anything.

## Editor-side snippet

Replace `QUERY_HERE` with the generated Unity Search query. If the query contains double quotes, double them inside the verbatim C# string.

Run it through the Editor with `unity command eval --code '<snippet>'`. Fully qualified, with no
`using` directives, because `eval` compiles a statement block rather than a file.

```csharp
const string query = @"QUERY_HERE";

var candidates = new[]
{
    UnityEditor.Search.SearchService.GetProvider("asset"),
    UnityEditor.Search.SearchService.GetProvider("scene")
};
var preferredProviders = System.Linq.Enumerable.ToArray(
    System.Linq.Enumerable.Where(candidates, provider => provider != null));

var context = preferredProviders.Length > 0
    ? new UnityEditor.Search.SearchContext(preferredProviders, query)
    : new UnityEditor.Search.SearchContext(UnityEditor.Search.SearchService.GetActiveProviders(), query);

UnityEditor.Search.SearchService.ShowWindow(context);
return $"Opened Unity Search with query: {query}";
```

## Fallback Behavior

If compilation or execution fails:

1. Do not claim Search opened.
2. Show the query to the user.
3. Tell the user to paste it into Unity Search manually.
4. If the error indicates `UnityEditor.Search` is unavailable, say the project/editor version may not expose the Modern Search API used by the opener.

## Provider Choice

For v1, prefer the `asset` and `scene` providers because this skill is scoped to project assets and scene objects.

Use active providers only as a fallback when one or both preferred providers are unavailable.

## Safety Rules

- Do not call APIs that act on search results.
- Do not select, ping, rename, delete, move, import, or edit results.
- Do not save assets or scenes.
- Do not create project scripts or editor windows.
- Do not run package, menu, settings, or dependency searches as v1 behavior.
