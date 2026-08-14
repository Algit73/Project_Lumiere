/// <summary>
/// Static cache that holds a pre-warmed Find-Object session so it is ready
/// the moment the user selects "Find Object".
///
/// Pre-warming fires in <see cref="Lumier_MainController"/> at scene start,
/// picking a random character and kitchen item while the user is still on
/// the main screen. NOTE: the OpenAI story request itself cannot fire here —
/// it needs the item's live <c>Interactive.Meta["description"]</c>, which only
/// exists once the Farm scene's item GameObjects are loaded. That request is
/// fired at the top of <see cref="FindObjectTask.Begin"/> instead, concurrently
/// with the camera tween (same latency-hiding pattern, just one scene later).
///
/// <see cref="FindObjectTask.Begin"/> consumes the cache; calling
/// <see cref="Clear"/> afterwards resets it for the next session.
/// </summary>
public static class FindObjectPrewarm
{
    // ── Cached pre-warm data ──────────────────────────────────────────────────

    /// <summary>The randomly picked kitchen item.</summary>
    public static KitchenFindGame.KitchenGameItem Item    { get; private set; }

    /// <summary>The randomly picked character profile (may be null if registry is empty).</summary>
    public static CharacterProfile                Profile { get; private set; }

    /// <summary>True when an item has been pre-picked and is ready to consume.</summary>
    public static bool IsReady => Item != null;

    // ── API ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Picks a random character from <paramref name="registry"/> and a random kitchen item.
    /// Safe to call multiple times — each call replaces the previous cache.
    /// </summary>
    /// <param name="registry">
    /// The character registry asset. A random profile is picked from it.
    /// Pass <c>null</c> if no registry is available (the story will use a generic persona).
    /// </param>
    public static void Execute(CharacterProfileSO registry)
    {
        var game = new KitchenFindGame();

        Item    = game.PickRandomItem();
        Profile = PickRandomProfile(registry);

        string profileInfo = Profile != null
            ? $"{Profile.displayName} ({Profile.characterID})"
            : "(no profile)";

        UnityEngine.Debug.Log(
            $"[FindObjectPrewarm] Pre-warm started — item: {Item.DisplayName} ({Item.Key})" +
            $" | character: {profileInfo}");
    }

    /// <summary>
    /// Clears the cache after it has been consumed by <see cref="FindObjectTask"/>.
    /// </summary>
    public static void Clear()
    {
        Item    = null;
        Profile = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CharacterProfile PickRandomProfile(CharacterProfileSO registry)
    {
        if (registry == null || registry.characters == null || registry.characters.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, registry.characters.Count);
        return registry.characters[index];
    }
}
