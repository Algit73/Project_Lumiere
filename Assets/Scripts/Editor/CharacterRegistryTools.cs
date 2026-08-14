using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates and maintains the unified <see cref="CharacterProfileSO"/> registry asset.
/// Run via: Tools > Lumiere > Create Character Registry Asset
///
/// The asset cannot be safely hand-authored as a raw .asset YAML file (it needs Unity's
/// own script GUID reference), so this editor tool creates it the standard way via
/// ScriptableObject.CreateInstance + AssetDatabase.CreateAsset.
/// </summary>
public static class CharacterRegistryTools
{
    private const string DefaultFolder = "Assets/ScriptableObjects";
    private const string DefaultAssetPath = DefaultFolder + "/CharacterRegistry.asset";

    [MenuItem("Tools/Lumiere/Create Character Registry Asset")]
    public static void CreateRegistryAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<CharacterProfileSO>(DefaultAssetPath) != null)
        {
            Debug.LogWarning($"[CharacterRegistryTools] Asset already exists at {DefaultAssetPath} — selecting it instead.");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<CharacterProfileSO>(DefaultAssetPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            return;
        }

        if (!AssetDatabase.IsValidFolder(DefaultFolder))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

        var registry = ScriptableObject.CreateInstance<CharacterProfileSO>();
        PopulateFromScene(registry);

        AssetDatabase.CreateAsset(registry, DefaultAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = registry;
        EditorGUIUtility.PingObject(registry);

        Debug.Log($"[CharacterRegistryTools] Created {DefaultAssetPath} with {registry.characters.Count} character entr" +
                  (registry.characters.Count == 1 ? "y" : "ies") +
                  " auto-detected from the current scene. Fill in bio/displayName for each, then assign this asset to " +
                  "Lumier_MainController.findGameRegistry and FindObjectTask.characterRegistry.");
    }

    /// <summary>
    /// Re-scans the currently open scene for <see cref="CharacterAgent"/> components and adds any
    /// missing characterIDs to the existing registry asset (does not remove or overwrite existing entries).
    /// Run via: Tools > Lumiere > Refresh Character Registry From Scene
    /// </summary>
    [MenuItem("Tools/Lumiere/Refresh Character Registry From Scene")]
    public static void RefreshFromScene()
    {
        var registry = AssetDatabase.LoadAssetAtPath<CharacterProfileSO>(DefaultAssetPath);
        if (registry == null)
        {
            Debug.LogWarning("[CharacterRegistryTools] No CharacterRegistry.asset found — run " +
                              "'Create Character Registry Asset' first.");
            return;
        }

        int added = PopulateFromScene(registry);
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CharacterRegistryTools] Added {added} new character entr" + (added == 1 ? "y" : "ies") +
                  $" from the scene. Total: {registry.characters.Count}.");
    }

    /// <summary>Adds one <see cref="CharacterProfile"/> per unique characterID found on active CharacterAgents in the scene.</summary>
    private static int PopulateFromScene(CharacterProfileSO registry)
    {
#if UNITY_2023_1_OR_NEWER
        CharacterAgent[] agents = Object.FindObjectsByType<CharacterAgent>(FindObjectsSortMode.None);
#else
        CharacterAgent[] agents = Object.FindObjectsOfType<CharacterAgent>();
#endif
        int added = 0;
        foreach (var agent in agents)
        {
            if (agent == null || string.IsNullOrEmpty(agent.characterID)) continue;
            if (registry.characters.Any(p => p.characterID == agent.characterID)) continue;

            registry.characters.Add(new CharacterProfile
            {
                characterID = agent.characterID,
                displayName = agent.characterID,
                bio         = "",
                lastUpdate  = ""
            });
            added++;
        }
        return added;
    }
}
