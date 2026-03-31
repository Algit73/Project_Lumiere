using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// On app start:
///   1. Finds all CharacterAgents in the scene.
///   2. Picks N (2-4) randomly.
///   3. For each: picks a weighted TaskTemplate, randomizes difficulty, spawns icon.
///
/// Attach to the same persistent GameObject as TaskManager, or any scene starter.
/// Assign the full catalogue of TaskTemplateSOs in the Inspector.
/// </summary>
public class TaskRandomizer : MonoBehaviour
{
    [Header("Character Count")]
    [Range(2, 4)]
    [Tooltip("How many characters receive a task at scene start.")]
    public int minCharacters = 2;
    [Range(2, 4)]
    public int maxCharacters = 4;

    [Header("Task Catalogue")]
    [Tooltip("All available TaskTemplateSOs. The randomizer picks from this list.")]
    public List<TaskTemplateSO> taskCatalogue = new List<TaskTemplateSO>();

    // Tracks how many times each template has been used this session (for weighting)
    private readonly Dictionary<TaskTemplateSO, int> _usageCounts = new();

    private void Start()
    {
        Randomize();
    }

    /// <summary>
    /// Runs the full randomization pass. Can also be called manually to re-randomize.
    /// </summary>
    public void Randomize()
    {
        if (TaskManager.Instance == null)
        {
            Debug.LogError("[TaskRandomizer] TaskManager.Instance is null. Make sure TaskManager is in the scene.");
            return;
        }

        if (taskCatalogue == null || taskCatalogue.Count == 0)
        {
            Debug.LogError("[TaskRandomizer] Task catalogue is empty. Assign TaskTemplateSOs in the Inspector.");
            return;
        }

        // 1. Find all eligible characters in the scene
        List<CharacterAgent> allCharacters = FindObjectsByType<CharacterAgent>(FindObjectsSortMode.None)
            .ToList();

        if (allCharacters.Count == 0)
        {
            Debug.LogWarning("[TaskRandomizer] No CharacterAgent components found in the scene.");
            return;
        }

        // 2. Pick N characters randomly
        int n = Mathf.Clamp(
            Random.Range(minCharacters, maxCharacters + 1),
            0,
            allCharacters.Count);

        List<CharacterAgent> chosen = PickRandom(allCharacters, n);
        Debug.Log($"[TaskRandomizer] Assigning tasks to {chosen.Count} character(s).");

        // Track templates already assigned this pass to avoid giving same template twice
        HashSet<TaskTemplateSO> usedThisPass = new HashSet<TaskTemplateSO>();

        // 3. For each chosen character, assign a task
        foreach (CharacterAgent character in chosen)
        {
            // Filter catalogue by character's eligible task types
            List<TaskTemplateSO> eligible = FilterByEligibility(taskCatalogue, character, usedThisPass);

            if (eligible.Count == 0)
            {
                Debug.LogWarning($"[TaskRandomizer] No eligible templates left for '{character.characterID}'. Skipping.");
                continue;
            }

            // Pick a template weighted by inverse usage (less-used = higher chance)
            TaskTemplateSO template = PickWeighted(eligible);
            usedThisPass.Add(template);
            IncrementUsage(template);

            // Randomize difficulty within character's allowed range, clamped to template range
            int difficulty = Random.Range(character.minDifficulty, character.maxDifficulty + 1);
            difficulty = Mathf.Clamp(difficulty, 1, 5);

            // Spawn the task
            TaskInstance task = TaskManager.Instance.SpawnTask(template, character.gameObject);

            if (task != null)
            {
                // Wire icon hide on complete/fail
                task.OnCompleted += _ => character.HideIcon();
                task.OnFailed    += _ => character.HideIcon();

                // 4. Show icon above character
                character.ShowIcon(template.taskType);

                Debug.Log($"[TaskRandomizer] '{character.characterID}' → task '{template.taskID}' (difficulty {difficulty}).");
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Returns a random subset of <paramref name="count"/> items from <paramref name="source"/>.</summary>
    private static List<T> PickRandom<T>(List<T> source, int count)
    {
        List<T> pool = new List<T>(source);
        List<T> result = new List<T>(count);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return result;
    }

    /// <summary>
    /// Filters the catalogue to templates that:
    ///   - Match the character's eligible task types (or character allows all)
    ///   - Are NOT already used in this randomization pass
    ///   - Have objects in their pool that match the character's difficulty range
    /// </summary>
    private static List<TaskTemplateSO> FilterByEligibility(
        List<TaskTemplateSO> catalogue,
        CharacterAgent character,
        HashSet<TaskTemplateSO> usedThisPass)
    {
        bool allowAll = character.eligibleTaskTypes == null || character.eligibleTaskTypes.Count == 0;

        return catalogue.Where(t =>
            t != null &&
            !usedThisPass.Contains(t) &&
            (allowAll || character.eligibleTaskTypes.Contains(t.taskType)) &&
            t.difficulty >= character.minDifficulty &&
            t.difficulty <= character.maxDifficulty
        ).ToList();
    }

    /// <summary>
    /// Picks a template using inverse-usage weighting.
    /// Templates used fewer times get a proportionally higher chance.
    /// </summary>
    private TaskTemplateSO PickWeighted(List<TaskTemplateSO> candidates)
    {
        // Weight = 1 / (usageCount + 1)  →  unused = 1.0, used once = 0.5, twice = 0.33...
        float totalWeight = candidates.Sum(t => 1f / (GetUsage(t) + 1));
        float roll        = Random.Range(0f, totalWeight);
        float cumulative  = 0f;

        foreach (TaskTemplateSO t in candidates)
        {
            cumulative += 1f / (GetUsage(t) + 1);
            if (roll <= cumulative) return t;
        }

        return candidates[candidates.Count - 1]; // fallback
    }

    private int  GetUsage(TaskTemplateSO t)           => _usageCounts.TryGetValue(t, out int c) ? c : 0;
    private void IncrementUsage(TaskTemplateSO t)     => _usageCounts[t] = GetUsage(t) + 1;
}
