using UnityEngine;

/// <summary>
/// Central scene director for Lumiere.
///
/// Reads <see cref="SceneSetupConfigSO"/> and applies scene setup on Start.
/// Add one instance to every scene (XR_Home, Farm, …).
/// All scenes can share the same SO asset — swap the asset to change every
/// scene at once without touching any GameObjects.
///
/// CURRENT (demo): chance-based NPC icon visibility.
/// FUTURE: will consume API data to drive NPC tasks, prop placements, and
/// event/challenge themes. Replace <see cref="RollNPCIcons"/> with an
/// API-response handler when that integration is ready.
/// </summary>
public class LumiereSceneDirector : UnityEngine.MonoBehaviour
{
    [SerializeField] private SceneSetupConfigSO config;

    private void Start()
    {
        SetupScene();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the current config to the scene.
    /// Call whenever the config changes or a full scene refresh is needed.
    /// </summary>
    public void SetupScene()
    {
        if (config == null)
        {
            UnityEngine.Debug.LogWarning("[LumiereSceneDirector] No SceneSetupConfig assigned — skipping setup.");
            return;
        }

        RollNPCIcons();

        // TODO: call PlaceProps(), ApplyEventTheme(), AssignTasks() etc.
        //       as those features are added.
    }

    /// <summary>
    /// Re-rolls icon visibility for all NPCs in the scene.
    /// Available from the Inspector context-menu for quick demo re-rolls.
    /// </summary>
    [UnityEngine.ContextMenu("Refresh Icons")]
    public void RefreshIcons()
    {
        if (config == null) return;
        RollNPCIcons();
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private void RollNPCIcons()
    {
        // FindObjectsByType is the non-deprecated API in Unity 2022.2+
#if UNITY_2022_2_OR_NEWER
        var agents = UnityEngine.Object.FindObjectsByType<CharacterAgent>(
                         UnityEngine.FindObjectsSortMode.None);
#else
        var agents = UnityEngine.Object.FindObjectsOfType<CharacterAgent>();
#endif

        foreach (var agent in agents)
        {
            if (UnityEngine.Random.value <= config.iconShowChance)
            {
                agent.ShowIcon(TaskType.Find);
            }   // TODO: pass actual TaskType from API
            else
                agent.HideIcon();
        }
    }
}
