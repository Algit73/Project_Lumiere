using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Reads the legacy serialized 'data' array on every Interactive in the scene,
/// creates an InteractiveDataSO asset for each unique identity, and assigns it
/// to the new 'profile' field — without touching existing scene values.
///
/// Run once via:  Tools > Migrate Interactive Data to ScriptableObjects
/// </summary>
public class InteractiveDataMigrator : EditorWindow
{
    private const string OutputFolder = "Assets/ScriptableObjects/InteractiveProfiles";

    private string _outputFolder = OutputFolder;
    private bool _reuseExisting = true;
    private Vector2 _scroll;
    private List<string> _log = new List<string>();

    [MenuItem("Tools/Migrate Interactive Data to ScriptableObjects")]
    public static void Open() => GetWindow<InteractiveDataMigrator>("Interactive Data Migrator");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Interactive Data -> ScriptableObject Migrator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool reads the hidden 'data' array on each Interactive in the current scene, " +
            "creates an InteractiveDataSO asset for each object, and assigns it to the 'profile' field.\n\n" +
            "Your existing scene values are NOT lost — they are copied into the new assets.",
            MessageType.Info);

        EditorGUILayout.Space();
        _outputFolder  = EditorGUILayout.TextField("Output Folder", _outputFolder);
        _reuseExisting = EditorGUILayout.Toggle("Reuse existing asset if name matches", _reuseExisting);

        EditorGUILayout.Space();
        if (GUILayout.Button("Run Migration", GUILayout.Height(36)))
        {
            _log.Clear();
            RunMigration();
        }

        if (_log.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(300));
            foreach (string line in _log)
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }
    }

    private void RunMigration()
    {
        if (!Directory.Exists(_outputFolder))
        {
            Directory.CreateDirectory(_outputFolder);
            AssetDatabase.Refresh();
            _log.Add($"Created folder: {_outputFolder}");
        }

        Interactive[] allInteractives = FindObjectsOfType<Interactive>(includeInactive: true);
        if (allInteractives.Length == 0)
        {
            _log.Add("No Interactive objects found in the current scene.");
            return;
        }

        int created = 0, reused = 0, skipped = 0;

        foreach (Interactive interactive in allInteractives)
        {
            SerializedObject so = new SerializedObject(interactive);

            // Read the hidden legacy data array
            SerializedProperty dataProp = so.FindProperty("data");
            if (dataProp == null || !dataProp.isArray)
            {
                _log.Add($"  SKIP {interactive.name}: 'data' property not found.");
                skipped++;
                continue;
            }

            // Check if profile is already assigned
            SerializedProperty profileProp = so.FindProperty("profile");
            if (profileProp != null && profileProp.objectReferenceValue != null)
            {
                _log.Add($"  SKIP {interactive.name}: profile already assigned ({profileProp.objectReferenceValue.name}).");
                skipped++;
                continue;
            }

            // Extract identity fields from the data array
            string type = "", description = "", objectName = interactive.gameObject.name, color = "";

            for (int i = 0; i < dataProp.arraySize; i++)
            {
                SerializedProperty element = dataProp.GetArrayElementAtIndex(i);
                string key   = element.FindPropertyRelative("Key").stringValue;
                string value = element.FindPropertyRelative("Value").stringValue;

                switch (key)
                {
                    case "type":        type        = value; break;
                    case "description": description = value; break;
                    case "name":        objectName  = string.IsNullOrEmpty(value) ? interactive.gameObject.name : value; break;
                    case "color":       color       = value; break;
                }
            }

            // Build a sanitized asset name
            string safeName = SanitizeFileName($"{interactive.gameObject.name}_Profile");
            string assetPath = $"{_outputFolder}/{safeName}.asset";

            InteractiveDataSO asset = null;

            if (_reuseExisting && File.Exists(assetPath))
            {
                asset = AssetDatabase.LoadAssetAtPath<InteractiveDataSO>(assetPath);
                reused++;
                _log.Add($"  REUSE  {interactive.name} → {assetPath}");
            }
            else
            {
                // Make unique path if file already exists with different content
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                asset = ScriptableObject.CreateInstance<InteractiveDataSO>();
                AssetDatabase.CreateAsset(asset, assetPath);
                created++;
                _log.Add($"  CREATE {interactive.name} → {assetPath}");
            }

            // Populate the asset
            asset.type        = type;
            asset.description = description;
            asset.objectName  = objectName;
            asset.color       = color;
            EditorUtility.SetDirty(asset);

            // Assign to profile field
            if (profileProp != null)
            {
                so.Update();
                profileProp.objectReferenceValue = asset;
                so.ApplyModifiedProperties();
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        _log.Insert(0, $"Done — Created: {created}  Reused: {reused}  Skipped: {skipped}  (out of {allInteractives.Length} Interactive objects)");
        _log.Insert(1, "");
        Debug.Log($"[InteractiveDataMigrator] Migration complete. Created={created} Reused={reused} Skipped={skipped}");
    }

    private static string SanitizeFileName(string input)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, '_');
        return input;
    }
}
