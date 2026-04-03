using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds all dialogue lines for a "Find Object" session.
///
/// HOW TO CREATE:
///   Right-click in the Project window → Create → Lumiere → Find Object Dialogue
///
/// HOW TO USE:
///   • Assign to FindObjectTask.dialogueData in the Inspector to override the
///     default hardcoded lines.
///   • Leave FindObjectTask.dialogueData null to use the built-in fallback lines.
///
/// The default lines are baked into DefaultLines() and used whenever the
/// Lines list is empty (i.e. a freshly created asset has no lines yet).
/// This guarantees the game always has playable content without requiring
/// any asset setup.
/// </summary>
[CreateAssetMenu(
    fileName = "FindObjectDialogue",
    menuName  = "Lumiere/Find Object Dialogue",
    order     = 51)]
public class FindObjectDialogueData : ScriptableObject
{
    [Tooltip("Ordered list of dialogue lines spoken by this NPC during a Find Object session.")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    /// <summary>
    /// Returns the configured lines, or the built-in hardcoded fallback
    /// if the list is empty.
    /// </summary>
    public IReadOnlyList<DialogueLine> GetLines()
    {
        if (lines != null && lines.Count > 0)
            return lines;

        return DefaultLines();
    }

    // ── Built-in fallback dialogue ─────────────────────────────────────────────
    // These are used when no custom lines are configured.
    // Replace or extend via the ScriptableObject asset in the Inspector.

    private static readonly DialogueLine[] _defaults = DefaultLines();

    private static DialogueLine[] DefaultLines() => new[]
    {
        new DialogueLine("Character",
            "Hi there! I'm glad you found me."),
        new DialogueLine("Character",
            "I've been looking for something special. Can you help me find it?"),
        new DialogueLine("Character",
            "Look around carefully. It might be somewhere close by."),
        new DialogueLine("Character",
            "Take your time. I believe you can find it!"),
        new DialogueLine("Character",
            "Let me know when you spot it. Good luck!"),
    };
}
