using UnityEngine;

/// <summary>
/// Audio and visual feedback played on task success or failure.
/// </summary>
[System.Serializable]
public struct FeedbackConfig
{
    [Header("Audio")]
    public AudioClip successAudio;
    public AudioClip failAudio;

    [Header("Visual Effects")]
    public GameObject successVFX;
    public GameObject failVFX;

    [Header("UI Colors")]
    public Color successColor;
    public Color failColor;
}
