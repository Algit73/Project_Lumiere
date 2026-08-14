using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data for a single NPC character in the Lumiere system.
///
/// <c>characterID</c> is the link key — it must exactly match the
/// <c>CharacterAgent.characterID</c> field set on the NPC's GameObject in the scene.
/// </summary>
[Serializable]
public class CharacterProfile
{
    [Tooltip("Unique ID that must match CharacterAgent.characterID on the NPC in the scene.")]
    public string characterID;

    [Tooltip("Display name used in AI prompts and the speech bubble header (e.g. 'Layla').")]
    public string displayName;

    [Tooltip("Brief bio sent to the AI as personality context " +
             "(e.g. 'A friendly baker who loves teaching new recipes').")]
    [TextArea(2, 4)]
    public string bio;

    [Tooltip("Latest narrative update for this character — written by the AI after each session " +
             "and stored here so future sessions can build on it. Leave blank initially.")]
    [TextArea(2, 4)]
    public string lastUpdate;
}

/// <summary>
/// ScriptableObject that acts as the single unified registry of all NPC characters.
///
/// Create one asset via <b>Assets › Create › Lumiere › Character Registry</b> and assign
/// it to the <c>FindObjectTask.characterRegistry</c> field in each scene.
///
/// At runtime the registry is looked up by <c>characterID</c>, which must match the
/// <c>CharacterAgent.characterID</c> set on the NPC GameObject.
/// </summary>
[CreateAssetMenu(fileName = "CharacterRegistry", menuName = "Lumiere/Character Registry")]
public class CharacterProfileSO : ScriptableObject
{
    [Tooltip("All known characters. Add one entry per NPC that participates in tasks.")]
    public List<CharacterProfile> characters = new List<CharacterProfile>();

    /// <summary>
    /// Returns the profile whose <c>characterID</c> matches the given ID,
    /// or <c>null</c> if not found.
    /// </summary>
    public CharacterProfile GetProfile(string characterID)
    {
        if (string.IsNullOrEmpty(characterID)) return null;
        return characters.Find(p =>
            string.Equals(p.characterID, characterID, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Writes the AI-generated <paramref name="lastUpdate"/> back to the in-memory profile
    /// so subsequent sessions in the same Play session see the updated text.
    /// (Does not persist to disk — use a separate save mechanism for that.)
    /// </summary>
    public void SetLastUpdate(string characterID, string lastUpdate)
    {
        CharacterProfile p = GetProfile(characterID);
        if (p != null) p.lastUpdate = lastUpdate;
    }
}
