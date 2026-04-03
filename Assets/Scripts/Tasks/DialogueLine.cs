using System;
using UnityEngine;

/// <summary>
/// A single line of dialogue: the text to display and speak, plus the
/// speaker's display name shown in the speech bubble header.
///
/// Marked [Serializable] so it appears cleanly in the Inspector inside
/// FindObjectDialogueData lists.
/// </summary>
[Serializable]
public struct DialogueLine
{
    [Tooltip("Name shown in the bubble header (e.g. 'Layla', 'The Doctor').")]
    public string speakerName;

    [Tooltip("The sentence(s) to display in the speech bubble and speak via TTS.")]
    [TextArea(2, 5)]
    public string text;

    public DialogueLine(string speaker, string line)
    {
        speakerName = speaker;
        text        = line;
    }
}
