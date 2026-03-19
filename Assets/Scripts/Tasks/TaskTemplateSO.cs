using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data template for a single task. Create instances via:
///   Right-click in Project > Create > Tasks > Task Template
/// </summary>
[CreateAssetMenu(fileName = "NewTask", menuName = "Tasks/Task Template")]
public class TaskTemplateSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this task (e.g. 'task_find_apple_01').")]
    public string taskID;

    [Tooltip("Cognitive/language category of the task.")]
    public TaskType taskType;

    [Tooltip("Difficulty level from 1 (easiest) to 5 (hardest).")]
    [Range(1, 5)]
    public int difficulty = 1;

    [Header("Objects")]
    [Tooltip("Pool of InteractiveDataSO profiles eligible to appear in this task.")]
    public List<InteractiveDataSO> objectPool = new List<InteractiveDataSO>();

    [Header("Prompts")]
    [Tooltip("Localized prompt strings. Use {target} as a placeholder for the target object name.")]
    public List<string> promptTemplates = new List<string>();

    [Header("Cue Hierarchy")]
    [Tooltip("Ordered list of cues from least to most supportive. Presented progressively if the player struggles.")]
    public List<CueType> cueHierarchy = new List<CueType>
    {
        CueType.NoCue,
        CueType.VisualHighlight,
        CueType.AudioCue,
        CueType.SemanticHint,
        CueType.PhonemicCue,
    };

    [Header("Success Rule")]
    public SuccessCriteria successCriteria;

    [Header("Timing")]
    [Tooltip("Time limit in seconds. Set to 0 for no limit.")]
    public float timeLimit = 0f;

    [Header("Feedback")]
    public FeedbackConfig feedbackConfig;
}
