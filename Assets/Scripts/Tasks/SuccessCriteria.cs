using UnityEngine;

/// <summary>
/// Serializable rule that defines when a task is considered complete.
/// The runtime task evaluator reads these fields to check player actions.
/// </summary>
[System.Serializable]
public class SuccessCriteria
{
    [Tooltip("The rule type that determines how success is evaluated.")]
    public SuccessCriteriaType type;

    [Tooltip("ID of the target object (used for Find / Name tasks).")]
    public string targetObjectID;

    [Tooltip("Number of correct interactions required (used for Sort / Match / CountReached tasks).")]
    public int requiredCount = 1;

    [Tooltip("Optional tag or category name the target must belong to (used for Sort tasks).")]
    public string targetCategory;
}
