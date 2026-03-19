using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a GameObject as a character eligible to receive tasks.
/// Attach this alongside the character's animator / AI components.
///
/// This is what differentiates task-eligible characters from all other
/// Interactive objects in the scene — no tag/layer hacks needed.
/// </summary>
public class CharacterAgent : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique ID for this character (e.g. 'npc_doctor', 'npc_teacher').")]
    public string characterID;

    [Header("Task Eligibility")]
    [Tooltip("Which task types this character is allowed to host. Leave empty to allow all.")]
    public List<TaskType> eligibleTaskTypes = new List<TaskType>();

    [Tooltip("Difficulty range this character will be assigned. Clamped to 1..5.")]
    [Range(1, 5)] public int minDifficulty = 1;
    [Range(1, 5)] public int maxDifficulty = 3;

    [Header("Icon")]
    [Tooltip("Transform above the character's head where the task icon will appear. " +
             "Create an empty child GameObject at head height and assign it here.")]
    public Transform iconAnchor;

    [Tooltip("Prefab for the task icon (assigned later when UI is ready). " +
             "Leave null to skip icon spawning.")]
    public GameObject iconPrefab;

    // ── Runtime ───────────────────────────────────────────────────────────────

    /// <summary>The live icon instance above this character, if any.</summary>
    public GameObject ActiveIcon { get; private set; }

    /// <summary>The task currently assigned to this character.</summary>
    public TaskInstance CurrentTask => TaskManager.Instance != null
        ? TaskManager.Instance.GetTask(gameObject)
        : null;

    // ── Icon API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns the task icon above this character.
    /// Called by TaskRandomizer after a task is assigned.
    /// </summary>
    public void ShowIcon(TaskType taskType)
    {
        if (iconPrefab == null) return;

        Transform anchor = iconAnchor != null ? iconAnchor : transform;

        if (ActiveIcon != null)
            Destroy(ActiveIcon);

        ActiveIcon = Instantiate(iconPrefab, anchor.position, Quaternion.identity, anchor);

        // TODO: set the glyph on the icon based on taskType once UI is ready
        Debug.Log($"[CharacterAgent] Icon shown for '{characterID}' — task type: {taskType}");
    }

    /// <summary>Removes the task icon from above this character.</summary>
    public void HideIcon()
    {
        if (ActiveIcon != null)
        {
            Destroy(ActiveIcon);
            ActiveIcon = null;
        }
    }

    private void OnValidate()
    {
        // Keep min <= max in the Inspector
        if (minDifficulty > maxDifficulty)
            maxDifficulty = minDifficulty;
    }
}
