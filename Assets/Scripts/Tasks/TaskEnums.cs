/// <summary>
/// Category of cognitive/language task.
/// </summary>
public enum TaskType
{
    Find,       // Locate a target object in the scene
    Story,      // Narrative / sequencing task
    Manipulate, // Physical interaction (move, rotate, scale)
    Sort,       // Group objects by a property
    Match,      // Pair objects together
    Phono,      // Phonological awareness task
    Name,       // Name or label an object
}

/// <summary>
/// Ordered cue levels — presented from least to most supportive.
/// </summary>
public enum CueType
{
    NoCue,
    VisualHighlight,
    AudioCue,
    SemanticHint,
    PhonemicCue,
}

/// <summary>
/// How success is evaluated at runtime.
/// </summary>
public enum SuccessCriteriaType
{
    SelectedObjectIsTarget, // player selects the correct single object
    AllObjectsSorted,       // all objects placed in correct category
    AllObjectsMatched,      // all pairs correctly matched
    CountReached,           // a required number of correct interactions reached
    Custom,                 // evaluated externally via script
}

/// <summary>
/// Runtime lifecycle state of a TaskInstance.
/// </summary>
public enum TaskState
{
    Idle,        // Created but not yet started
    Running,     // Active — prompt shown, waiting for user interaction
    Evaluating,  // User acted — checking SuccessCriteria
    Complete,    // Task passed — feedback shown, icon removed
    Failed,      // Time expired or too many wrong attempts
}
