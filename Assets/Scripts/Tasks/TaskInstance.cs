using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime object created from a TaskTemplateSO and assigned to one character.
/// Drives the full task lifecycle: Spawn → Trigger → Run → Evaluate → Feedback → Complete.
/// </summary>
public class TaskInstance
{
    // ── Data ────────────────────────────────────────────────────────────────
    public TaskTemplateSO Template       { get; private set; }
    public GameObject      AssignedTo    { get; private set; }  // the character GameObject
    public TaskState       State         { get; private set; } = TaskState.Idle;

    /// <summary>Active prompt string (one picked from Template.promptTemplates).</summary>
    public string          ActivePrompt  { get; private set; }

    /// <summary>Index into Template.cueHierarchy — advances each time the user needs more help.</summary>
    public int             CueLevel      { get; private set; } = 0;

    public float           ElapsedTime   { get; private set; } = 0f;
    public int             CorrectCount  { get; private set; } = 0;

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired when the task moves to Running state (UI should show prompt).</summary>
    public event Action<TaskInstance>           OnStarted;

    /// <summary>Fired each time an interaction is evaluated. bool = was it correct.</summary>
    public event Action<TaskInstance, bool>     OnEvaluated;

    /// <summary>Fired when task reaches Complete state.</summary>
    public event Action<TaskInstance>           OnCompleted;

    /// <summary>Fired when task reaches Failed state (timeout or externally failed).</summary>
    public event Action<TaskInstance>           OnFailed;

    /// <summary>Fired when the cue level advances.</summary>
    public event Action<TaskInstance, CueType>  OnCueAdvanced;

    // ── Constructor ──────────────────────────────────────────────────────────
    public TaskInstance(TaskTemplateSO template, GameObject assignedTo)
    {
        Template   = template  ?? throw new ArgumentNullException(nameof(template));
        AssignedTo = assignedTo ?? throw new ArgumentNullException(nameof(assignedTo));
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    /// <summary>
    /// TRIGGER — called when the user taps the character icon.
    /// Moves from Idle → Running and broadcasts OnStarted.
    /// </summary>
    public void StartTask()
    {
        if (State != TaskState.Idle)
        {
            Debug.LogWarning($"[TaskInstance] StartTask called on task '{Template.taskID}' in state {State}.");
            return;
        }

        ActivePrompt = PickPrompt();
        State        = TaskState.Running;
        ElapsedTime  = 0f;
        CueLevel     = 0;

        OnStarted?.Invoke(this);
    }

    /// <summary>
    /// RUN — called every frame by TaskManager while State == Running.
    /// Handles the optional time limit.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (State != TaskState.Running) return;

        if (Template.timeLimit > 0f)
        {
            ElapsedTime += deltaTime;
            if (ElapsedTime >= Template.timeLimit)
                Fail();
        }
    }

    /// <summary>
    /// EVALUATE — called when the user performs an interaction (taps an object, drops, speaks, etc.).
    /// Pass the Interactive the user acted on.
    /// </summary>
    public void Evaluate(Interactive interactedObject)
    {
        if (State != TaskState.Running) return;

        State = TaskState.Evaluating;

        bool success = CheckCriteria(interactedObject);

        if (success) CorrectCount++;

        OnEvaluated?.Invoke(this, success);

        if (IsSatisfied())
            Complete();
        else
        {
            // Wrong answer — advance cue if possible, then go back to Running
            if (!success) AdvanceCue();
            State = TaskState.Running;
        }
    }

    /// <summary>
    /// Manually advance the cue level (e.g., after a timeout between attempts).
    /// </summary>
    public void AdvanceCue()
    {
        if (CueLevel < Template.cueHierarchy.Count - 1)
        {
            CueLevel++;
            OnCueAdvanced?.Invoke(this, Template.cueHierarchy[CueLevel]);
        }
    }

    /// <summary>Returns the current CueType for the UI/audio system.</summary>
    public CueType CurrentCue =>
        Template.cueHierarchy.Count > 0
            ? Template.cueHierarchy[Mathf.Clamp(CueLevel, 0, Template.cueHierarchy.Count - 1)]
            : CueType.NoCue;

    // ── Private helpers ───────────────────────────────────────────────────────

    private bool CheckCriteria(Interactive obj)
    {
        SuccessCriteria rule = Template.successCriteria;

        switch (rule.type)
        {
            case SuccessCriteriaType.SelectedObjectIsTarget:
                return obj != null && obj.GetMeta("name") == rule.targetObjectID;

            case SuccessCriteriaType.CountReached:
                // Caller should pass the correct object; we just increment and check later
                return obj != null;

            case SuccessCriteriaType.AllObjectsSorted:
            case SuccessCriteriaType.AllObjectsMatched:
                // These are aggregate — individual interaction always counts as a step
                return obj != null && obj.GetMeta("category") == rule.targetCategory;

            case SuccessCriteriaType.Custom:
                // Delegate to an external evaluator — always returns false here
                return false;

            default:
                return false;
        }
    }

    private bool IsSatisfied()
    {
        SuccessCriteria rule = Template.successCriteria;

        switch (rule.type)
        {
            case SuccessCriteriaType.SelectedObjectIsTarget:
                return CorrectCount >= 1;

            case SuccessCriteriaType.CountReached:
            case SuccessCriteriaType.AllObjectsSorted:
            case SuccessCriteriaType.AllObjectsMatched:
                return CorrectCount >= rule.requiredCount;

            default:
                return false;
        }
    }

    private void Complete()
    {
        State = TaskState.Complete;
        OnCompleted?.Invoke(this);
    }

    private void Fail()
    {
        State = TaskState.Failed;
        OnFailed?.Invoke(this);
    }

    private string PickPrompt()
    {
        if (Template.promptTemplates == null || Template.promptTemplates.Count == 0)
            return string.Empty;

        int index = UnityEngine.Random.Range(0, Template.promptTemplates.Count);
        string prompt = Template.promptTemplates[index];

        // Replace {target} placeholder with the target object name
        return prompt.Replace("{target}", Template.successCriteria.targetObjectID);
    }
}
