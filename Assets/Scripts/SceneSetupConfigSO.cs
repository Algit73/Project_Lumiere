using UnityEngine;

/// <summary>
/// ScriptableObject holding scene-setup configuration for Lumiere.
///
/// For the demo this drives the chance-based NPC icon system.
/// In production, fields will be populated from API data instead of
/// being authored by hand — see the TODO comments below.
///
/// Create via: Assets › Create › Lumiere › Scene Setup Config
/// </summary>
[CreateAssetMenu(fileName = "SceneSetupConfig", menuName = "Lumiere/Scene Setup Config")]
public class SceneSetupConfigSO : ScriptableObject
{
    [Header("NPC Floating Icons")]
    [Range(0f, 1f)]
    [Tooltip("Probability (0–1) that each NPC with an NPCFloatingIcon component will show " +
             "their icon when the scene is set up.\n" +
             "0 = never  |  0.5 = 50% chance  |  1 = always.\n\n" +
             "TODO: Replace with per-NPC task data received from the API.")]
    public float iconShowChance = 0.5f;

    // ─────────────────────────────────────────────────────────────────────────
    // TODO (API integration) — add fields below as data sources become available
    // ─────────────────────────────────────────────────────────────────────────
    // public string        eventTag;                           // "birthday", "winter", …
    // public List<string>  forcedIconNPCIds;                   // always show (by characterID)
    // public List<PropPlacementData>    propPlacements;        // item positions in scene
    // public List<TaskAssignmentData>   taskAssignments;       // per-NPC tasks from server
}
