using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the full lifecycle of tasks in the scene.
///
/// Attach to a persistent GameObject (e.g., GameManager).
///
/// Lifecycle summary:
///   Spawn    → SpawnTask(template, character)
///   Trigger  → TriggerTask(character)  — called when player taps character icon
///   Run      → Ticked every frame automatically
///   Evaluate → EvaluateInteraction(character, interactedObject) — called from interaction system
///   Feedback → Handled via TaskInstance events (subscribe in UI/audio managers)
///   Complete → Automatic when SuccessCriteria satisfied
/// </summary>
public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    // Character GameObject → active TaskInstance
    private readonly Dictionary<GameObject, TaskInstance> _activeTasks = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Tick all running tasks (handles time limits)
        foreach (TaskInstance task in _activeTasks.Values)
            task.Tick(Time.deltaTime);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// SPAWN — creates a TaskInstance from a template and assigns it to a character.
    /// Call this from a level setup script or dialogue trigger.
    /// </summary>
    public TaskInstance SpawnTask(TaskTemplateSO template, GameObject character)
    {
        if (_activeTasks.ContainsKey(character))
        {
            Debug.LogWarning($"[TaskManager] '{character.name}' already has an active task. Remove it first.");
            return null;
        }

        TaskInstance instance = new TaskInstance(template, character);

        // Wire up lifecycle callbacks
        instance.OnStarted    += HandleTaskStarted;
        instance.OnEvaluated  += HandleTaskEvaluated;
        instance.OnCompleted  += HandleTaskCompleted;
        instance.OnFailed     += HandleTaskFailed;
        instance.OnCueAdvanced += HandleCueAdvanced;

        _activeTasks[character] = instance;

        Debug.Log($"[TaskManager] Task '{template.taskID}' spawned on '{character.name}'.");
        return instance;
    }

    /// <summary>
    /// TRIGGER — call when the player taps the character's icon.
    /// Moves the task from Idle → Running.
    /// </summary>
    public void TriggerTask(GameObject character)
    {
        if (!_activeTasks.TryGetValue(character, out TaskInstance task))
        {
            Debug.LogWarning($"[TaskManager] No task found for '{character.name}'.");
            return;
        }

        task.StartTask();
    }

    /// <summary>
    /// EVALUATE — call from the interaction system when the player acts on an object.
    /// Pass the character whose task is active and the Interactive the player used.
    /// </summary>
    public void EvaluateInteraction(GameObject character, Interactive interactedObject)
    {
        if (!_activeTasks.TryGetValue(character, out TaskInstance task))
            return;

        if (task.State != TaskState.Running) return;

        task.Evaluate(interactedObject);
    }

    /// <summary>Returns true if the given character has an active (non-complete) task.</summary>
    public bool HasActiveTask(GameObject character) =>
        _activeTasks.TryGetValue(character, out TaskInstance t) &&
        t.State != TaskState.Complete && t.State != TaskState.Failed;

    /// <summary>Returns the current TaskInstance for a character, or null.</summary>
    public TaskInstance GetTask(GameObject character) =>
        _activeTasks.TryGetValue(character, out TaskInstance t) ? t : null;

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void HandleTaskStarted(TaskInstance task)
    {
        Debug.Log($"[TaskManager] Task '{task.Template.taskID}' started on '{task.AssignedTo.name}'. Prompt: \"{task.ActivePrompt}\"");
        // TODO: tell UIManager to show the prompt and character icon state change
    }

    private void HandleTaskEvaluated(TaskInstance task, bool correct)
    {
        Debug.Log($"[TaskManager] Evaluation on '{task.Template.taskID}': {(correct ? "CORRECT" : "WRONG")}. Cue level: {task.CueLevel}");
        // TODO: play FeedbackConfig.successAudio / failAudio, spawn VFX
        PlayFeedback(task.Template.feedbackConfig, correct);
    }

    private void HandleTaskCompleted(TaskInstance task)
    {
        Debug.Log($"[TaskManager] Task '{task.Template.taskID}' COMPLETE on '{task.AssignedTo.name}'.");
        _activeTasks.Remove(task.AssignedTo);
        // TODO: tell UIManager to remove character icon
    }

    private void HandleTaskFailed(TaskInstance task)
    {
        Debug.Log($"[TaskManager] Task '{task.Template.taskID}' FAILED on '{task.AssignedTo.name}'.");
        _activeTasks.Remove(task.AssignedTo);
        // TODO: tell UIManager to remove character icon
    }

    private void HandleCueAdvanced(TaskInstance task, CueType newCue)
    {
        Debug.Log($"[TaskManager] Cue advanced to '{newCue}' for task '{task.Template.taskID}'.");
        // TODO: trigger visual highlight, audio cue, etc. based on newCue
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PlayFeedback(FeedbackConfig config, bool success)
    {
        AudioClip clip = success ? config.successAudio : config.failAudio;
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);

        // VFX instantiation — position at the character in future implementation
        GameObject vfx = success ? config.successVFX : config.failVFX;
        if (vfx != null)
            Object.Instantiate(vfx);
    }
}
