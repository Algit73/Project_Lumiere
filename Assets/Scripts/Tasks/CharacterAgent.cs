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

    [Tooltip("(Legacy) Prefab instantiated directly when no NPCFloatingIcon component is present. " +
             "Prefer adding an NPCFloatingIcon component instead.")]
    public GameObject iconPrefab;

    [Header("Preview")]
    [Tooltip("Call ShowIcon on Start. Useful for testing in Edit/Play mode.")]
    public bool showIconOnStart = false;

    // ── Runtime ───────────────────────────────────────────────────────────────

    /// <summary>The live icon instance above this character, if any (legacy path).</summary>
    public GameObject ActiveIcon { get; private set; }

    /// <summary>The task currently assigned to this character.</summary>
    public TaskInstance CurrentTask => TaskManager.Instance != null
        ? TaskManager.Instance.GetTask(gameObject)
        : null;

    // Cached reference — null when the component is not present (silently skipped)
    private NPCFloatingIcon _floatingIcon;

    // ── Icon API ──────────────────────────────────────────────────────────────

    // Awake runs on ALL objects before any Start() fires, so _floatingIcon is
    // always ready by the time LumiereSceneDirector.Start() calls ShowIcon().
    private void Awake()
    {
        _floatingIcon = GetComponent<NPCFloatingIcon>();
    }

    private void Start()
    {
        if (showIconOnStart)
            ShowIcon(TaskType.Find);
    }

    /// <summary>
    /// Shows the floating icon for the given task type.
    /// Uses NPCFloatingIcon if the component exists; falls back to iconPrefab otherwise.
    /// </summary>
    public void ShowIcon(TaskType taskType)
    {
        // Primary path: delegate to NPCFloatingIcon component
        if (_floatingIcon != null)
        {
            _floatingIcon.ShowIcon();
            return;
        }

        // Legacy fallback: spawn iconPrefab directly
        if (iconPrefab == null) return;

        Transform anchor = iconAnchor != null ? iconAnchor : transform;

        if (ActiveIcon != null)
            Destroy(ActiveIcon);

        ActiveIcon = Instantiate(iconPrefab, anchor.position, Quaternion.identity, anchor);
        Debug.Log($"[CharacterAgent] Icon shown for '{characterID}' (legacy prefab path) — task: {taskType}");
    }

    /// <summary>Hides the floating icon above this character.</summary>
    public void HideIcon()
    {
        _floatingIcon?.HideIcon();

        // Legacy path cleanup
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
