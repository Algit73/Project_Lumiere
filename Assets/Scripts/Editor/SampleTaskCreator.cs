using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates a ready-to-use placeholder TaskTemplateSO for testing.
/// Tools > Create Sample Task Template
/// </summary>
public static class SampleTaskCreator
{
    private const string OutputPath = "Assets/ScriptableObjects/Tasks";

    [MenuItem("Tools/Create Sample Task Template")]
    public static void CreateSampleTask()
    {
        if (!System.IO.Directory.Exists(OutputPath))
        {
            System.IO.Directory.CreateDirectory(OutputPath);
            AssetDatabase.Refresh();
        }

        TaskTemplateSO asset = ScriptableObject.CreateInstance<TaskTemplateSO>();

        // ── Identity ────────────────────────────────────────────────────────
        asset.taskID    = "task_find_sample_01";
        asset.taskType  = TaskType.Find;
        asset.difficulty = 1;

        // ── Prompts ─────────────────────────────────────────────────────────
        asset.promptTemplates = new List<string>
        {
            "Can you find the {target}?",
            "Where is the {target}?",
            "Point to the {target}!",
        };

        // ── Cue Hierarchy ───────────────────────────────────────────────────
        asset.cueHierarchy = new List<CueType>
        {
            CueType.NoCue,
            CueType.VisualHighlight,
            CueType.AudioCue,
            CueType.SemanticHint,
            CueType.PhonemicCue,
        };

        // ── Object Pool: empty for now ───────────────────────────────────────
        // Add InteractiveDataSO assets here once your scene profiles are ready.
        asset.objectPool = new List<InteractiveDataSO>();

        // ── Success Criteria ─────────────────────────────────────────────────
        asset.successCriteria = new SuccessCriteria
        {
            type           = SuccessCriteriaType.SelectedObjectIsTarget,
            targetObjectID = "sample_object",   // replace with a real object name
            requiredCount  = 1,
        };

        // ── Timing ───────────────────────────────────────────────────────────
        asset.timeLimit = 30f;   // 30 seconds; set to 0 for no limit

        // ── Feedback: no clips/VFX yet ───────────────────────────────────────
        asset.feedbackConfig = new FeedbackConfig
        {
            successColor = Color.green,
            failColor    = Color.red,
        };

        string path = AssetDatabase.GenerateUniqueAssetPath($"{OutputPath}/SampleTask.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        Debug.Log($"[SampleTaskCreator] Created sample task at: {path}");
    }
}
