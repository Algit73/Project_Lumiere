using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single hardcoded filler line: display text plus its pre-recorded/baked AudioClip.
/// Played instantly on a wrong tap or hint request to mask OpenAI API latency and make
/// the character feel responsive in real time.
/// </summary>
[Serializable]
public class FillerEntry
{
    [Tooltip("Text shown in the speech bubble while the clip plays.")]
    [TextArea(1, 3)]
    public string text;

    [Tooltip("Pre-recorded/baked audio clip for this line. Assign a real voice recording — " +
             "fillers do NOT go through the live TTS pipeline (that would add latency).")]
    public AudioClip clip;
}

/// <summary>
/// Bank of hardcoded filler lines with matching pre-recorded audio, used by <see cref="FindObjectTask"/>
/// to give an instant, natural-feeling reaction to a wrong tap or hint request while the real
/// AI response is still in flight.
///
/// Keep a good variety (15-20+ entries) so repeated wrong taps don't feel obviously canned.
/// Create via <b>Assets › Create › Lumiere › Filler Line Bank</b>.
/// </summary>
[CreateAssetMenu(fileName = "FillerLineBank", menuName = "Lumiere/Filler Line Bank")]
public class FillerLineBankSO : ScriptableObject
{
    [Tooltip("Filler lines shown/played on a wrong tap (before the real AI reaction arrives).")]
    public List<FillerEntry> wrongGuessFillers = new List<FillerEntry>();

    [Tooltip("Filler lines shown/played right after the player requests a hint (before the real AI hint arrives).")]
    public List<FillerEntry> hintRequestFillers = new List<FillerEntry>();

    /// <summary>Returns a random wrong-guess filler, or null if the bank is empty.</summary>
    public FillerEntry GetRandomWrongGuessFiller() => PickRandom(wrongGuessFillers);

    /// <summary>Returns a random hint-request filler, or null if the bank is empty.</summary>
    public FillerEntry GetRandomHintFiller() => PickRandom(hintRequestFillers);

    private static FillerEntry PickRandom(List<FillerEntry> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }
}
