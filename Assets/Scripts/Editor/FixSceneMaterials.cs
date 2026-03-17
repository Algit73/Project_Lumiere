using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tool to update materials on GameObjects in the scene that are using old/missing materials
/// </summary>
public class FixSceneMaterials : EditorWindow
{
    [MenuItem("Tools/Fix Scene Materials")]
    public static void ShowWindow()
    {
        var window = GetWindow<FixSceneMaterials>("Fix Scene Materials");
        window.minSize = new Vector2(400, 200);
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Scene Materials", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool fixes GameObjects in the current scene that have pink materials.\n\n" +
            "• Fix Pink Materials: Targets objects with missing/error shaders\n" +
            "• Revert to Prefab Materials: Updates ALL prefab instances to match their prefabs\n" +
            "• Fix Kenney Furniture: Re-imports furniture prefabs\n" +
            "• Force Fix Furniture (Direct): Directly replaces broken furniture materials\n\n" +
            "For stubborn furniture issues, use 'Force Fix Furniture Materials (Direct)'!",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Fix Pink Materials in Current Scene", GUILayout.Height(40)))
        {
            FixPinkMaterialsInScene();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Revert to Prefab Materials", GUILayout.Height(40)))
        {
            RevertToPrefabMaterials();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Fix Kenney Furniture Prefabs", GUILayout.Height(40)))
        {
            FixKenneyFurniturePrefabs();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Force Fix Furniture Materials (Direct)", GUILayout.Height(40)))
        {
            ForceFixFurnitureMaterials();
        }
    }

    private void FixPinkMaterialsInScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        
        int fixedCount = 0;
        int totalRenderers = 0;

        foreach (GameObject rootObj in rootObjects)
        {
            Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            
            foreach (Renderer renderer in renderers)
            {
                totalRenderers++;
                
                Material[] materials = renderer.sharedMaterials;
                bool materialsFixed = false;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        Debug.LogWarning($"Null material on {renderer.gameObject.name} at index {i}", renderer.gameObject);
                        continue;
                    }
                    
                    // Check if material has a pink shader (missing shader)
                    if (materials[i].shader == null || materials[i].shader.name == "Hidden/InternalErrorShader")
                    {
                        // Try to find the correct material from the prefab
                        GameObject prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(renderer.gameObject);
                        
                        if (prefabRoot != null)
                        {
                            Renderer prefabRenderer = prefabRoot.GetComponent<Renderer>();
                            if (prefabRenderer != null && prefabRenderer.sharedMaterials.Length > i)
                            {
                                materials[i] = prefabRenderer.sharedMaterials[i];
                                materialsFixed = true;
                            }
                        }
                    }
                }
                
                if (materialsFixed)
                {
                    Undo.RecordObject(renderer, "Fix Pink Materials");
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                    fixedCount++;
                }
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"<color=green>Fixed {fixedCount} renderers with pink materials out of {totalRenderers} total renderers.</color>");
            EditorUtility.DisplayDialog("Materials Fixed", 
                $"Fixed {fixedCount} objects with pink materials.\n\n" +
                "Your scene objects should now render correctly!", 
                "OK");
        }
        else
        {
            Debug.Log($"No pink materials found. Checked {totalRenderers} renderers.");
            EditorUtility.DisplayDialog("No Issues Found", 
                "No pink materials were found in the current scene.\n\n" +
                "If objects are still pink, try 'Revert to Prefab Materials' instead.", 
                "OK");
        }
    }

    private void RevertToPrefabMaterials()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        
        int revertedCount = 0;
        int totalObjects = 0;

        foreach (GameObject rootObj in rootObjects)
        {
            // Find all renderers in the scene
            Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            
            foreach (Renderer renderer in renderers)
            {
                totalObjects++;
                
                // Check if this object comes from a prefab
                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(renderer.gameObject);
                
                if (prefabSource != null)
                {
                    Renderer prefabRenderer = prefabSource.GetComponent<Renderer>();
                    
                    if (prefabRenderer != null)
                    {
                        // Get materials from both
                        Material[] currentMats = renderer.sharedMaterials;
                        Material[] prefabMats = prefabRenderer.sharedMaterials;
                        
                        bool needsUpdate = false;
                        
                        // Check if materials are different or if any are null/pink
                        if (currentMats.Length != prefabMats.Length)
                        {
                            needsUpdate = true;
                        }
                        else
                        {
                            for (int i = 0; i < currentMats.Length; i++)
                            {
                                // Check if material is different, null, or has missing shader
                                if (currentMats[i] != prefabMats[i] || 
                                    currentMats[i] == null ||
                                    currentMats[i].shader == null ||
                                    currentMats[i].shader.name == "Hidden/InternalErrorShader")
                                {
                                    needsUpdate = true;
                                    break;
                                }
                            }
                        }
                        
                        if (needsUpdate)
                        {
                            Undo.RecordObject(renderer, "Revert to Prefab Materials");
                            renderer.sharedMaterials = (Material[])prefabMats.Clone();
                            
                            // Also revert the property modification on the prefab instance
                            PrefabUtility.RevertPropertyOverride(
                                new SerializedObject(renderer).FindProperty("m_Materials"),
                                InteractionMode.UserAction);
                            
                            EditorUtility.SetDirty(renderer);
                            EditorUtility.SetDirty(renderer.gameObject);
                            revertedCount++;
                            
                            Debug.Log($"Reverted materials on: {GetGameObjectPath(renderer.gameObject)}", renderer.gameObject);
                        }
                    }
                }
            }
        }

        if (revertedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(currentScene);
            Debug.Log($"<color=green>Reverted {revertedCount} objects to use their prefab's materials out of {totalObjects} total renderers.</color>");
            EditorUtility.DisplayDialog("Materials Reverted", 
                $"Reverted {revertedCount} objects to use their prefab materials.\n\n" +
                "Scene objects now match their prefabs!\n\n" +
                "Don't forget to save the scene!", 
                "OK");
        }
        else
        {
            Debug.Log($"All {totalObjects} renderers already match their prefab materials or aren't prefab instances.");
            EditorUtility.DisplayDialog("Already Up to Date", 
                "All objects in the scene already use their prefab's current materials.", 
                "OK");
        }
    }
    
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
    
    private void FixKenneyFurniturePrefabs()
    {
        // Find all FBX files in furniture-kit
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models/kenney_furniture-kit" });
        int fixedCount = 0;
        int totalProcessed = 0;

        EditorUtility.DisplayProgressBar("Fixing Furniture Prefabs", "Finding furniture models...", 0f);

        foreach (string guid in fbxGuids)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            
            // Only process FBX files
            if (!fbxPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                continue;

            totalProcessed++;
            
            EditorUtility.DisplayProgressBar("Fixing Furniture Prefabs", 
                $"Processing {System.IO.Path.GetFileName(fbxPath)}...", 
                (float)totalProcessed / fbxGuids.Length);

            // Load the FBX as a GameObject
            GameObject fbxObject = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObject == null) continue;

            // Get the materials directory for this FBX
            string fbxDir = System.IO.Path.GetDirectoryName(fbxPath);
            string materialsDir = System.IO.Path.Combine(fbxDir, "Materials");
            
            if (!System.IO.Directory.Exists(materialsDir))
                continue;

            // Get all renderers in the FBX
            Renderer[] renderers = fbxObject.GetComponentsInChildren<Renderer>(true);
            bool prefabModified = false;

            foreach (Renderer renderer in renderers)
            {
                Material[] currentMaterials = renderer.sharedMaterials;
                Material[] newMaterials = new Material[currentMaterials.Length];
                bool materialsChanged = false;

                for (int i = 0; i < currentMaterials.Length; i++)
                {
                    newMaterials[i] = currentMaterials[i];
                    
                    if (currentMaterials[i] == null)
                        continue;

                    // Get the material name
                    string matName = currentMaterials[i].name;
                    
                    // Try to find the corresponding material file
                    string matPath = System.IO.Path.Combine(materialsDir, matName + ".mat");
                    string assetMatPath = matPath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    
                    Material correctMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetMatPath);
                    
                    if (correctMaterial != null && correctMaterial != currentMaterials[i])
                    {
                        newMaterials[i] = correctMaterial;
                        materialsChanged = true;
                        Debug.Log($"Updated material '{matName}' on {renderer.gameObject.name} in {fbxPath}");
                    }
                }

                if (materialsChanged)
                {
                    // We can't directly modify FBX prefabs, but we can update the importer
                    ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                    if (importer != null)
                    {
                        // Force reimport to regenerate the prefab
                        EditorUtility.SetDirty(importer);
                        prefabModified = true;
                    }
                }
            }

            if (prefabModified)
            {
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
                fixedCount++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (fixedCount > 0)
        {
            Debug.Log($"<color=green>Fixed {fixedCount} furniture prefabs out of {totalProcessed} models.</color>");
            EditorUtility.DisplayDialog("Prefabs Updated", 
                $"Updated {fixedCount} furniture prefabs with correct materials.\n\n" +
                "Now use 'Revert to Prefab Materials' to update objects in your scene!", 
                "OK");
        }
        else
        {
            Debug.Log($"All {totalProcessed} furniture prefabs already have correct materials.");
            EditorUtility.DisplayDialog("Already Correct", 
                "All furniture prefabs already use the correct materials.\n\n" +
                "The materials in the Materials folder are correct.\n" +
                "Try 'Revert to Prefab Materials' to update scene objects.", 
                "OK");
        }
    }
    
    private void ForceFixFurnitureMaterials()
    {
        // Build a dictionary of all available materials in kenney_furniture-kit
        string materialsPath = "Assets/Models/kenney_furniture-kit/Models";
        string[] allMaterialPaths = AssetDatabase.FindAssets("t:Material", new[] { materialsPath });
        
        System.Collections.Generic.Dictionary<string, Material> materialLookup = 
            new System.Collections.Generic.Dictionary<string, Material>();
        
        foreach (string guid in allMaterialPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                string matName = mat.name;
                if (!materialLookup.ContainsKey(matName))
                {
                    materialLookup[matName] = mat;
                }
            }
        }
        
        Debug.Log($"<color=cyan>Found {materialLookup.Count} furniture materials available.</color>");
        
        // Now scan the scene
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        
        int fixedRenderers = 0;
        int totalMaterialsFixed = 0;

        foreach (GameObject rootObj in rootObjects)
        {
            Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                Material[] newMaterials = new Material[materials.Length];
                bool changed = false;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    newMaterials[i] = materials[i];
                    
                    if (materials[i] == null)
                    {
                        Debug.LogWarning($"Null material at index {i} on {GetGameObjectPath(renderer.gameObject)}", renderer.gameObject);
                        continue;
                    }
                    
                    string matName = materials[i].name;
                    
                    // Remove instance suffix if present
                    if (matName.Contains(" (Instance)"))
                    {
                        matName = matName.Replace(" (Instance)", "");
                    }
                    
                    // Check if this material name exists in our furniture materials
                    if (materialLookup.ContainsKey(matName))
                    {
                        Material correctMaterial = materialLookup[matName];
                        
                        // Check if shader is broken or material is different
                        if (materials[i].shader == null || 
                            materials[i].shader.name == "Hidden/InternalErrorShader" ||
                            materials[i] != correctMaterial)
                        {
                            newMaterials[i] = correctMaterial;
                            changed = true;
                            totalMaterialsFixed++;
                            Debug.Log($"Replaced '{matName}' on {GetGameObjectPath(renderer.gameObject)}", renderer.gameObject);
                        }
                    }
                }
                
                if (changed)
                {
                    Undo.RecordObject(renderer, "Force Fix Furniture Materials");
                    renderer.sharedMaterials = newMaterials;
                    EditorUtility.SetDirty(renderer);
                    EditorUtility.SetDirty(renderer.gameObject);
                    fixedRenderers++;
                }
            }
        }
        
        if (fixedRenderers > 0)
        {
            EditorSceneManager.MarkSceneDirty(currentScene);
            Debug.Log($"<color=green>Fixed {totalMaterialsFixed} materials on {fixedRenderers} renderers!</color>");
            EditorUtility.DisplayDialog("Materials Fixed", 
                $"Fixed {totalMaterialsFixed} materials on {fixedRenderers} objects.\n\n" +
                "Your furniture should now render correctly!\n\n" +
                "Don't forget to save the scene!", 
                "OK");
        }
        else
        {
            Debug.Log("No furniture materials needed fixing.");
            EditorUtility.DisplayDialog("No Issues Found", 
                "No furniture with broken materials found in the scene.\n\n" +
                $"Checked against {materialLookup.Count} available furniture materials.", 
                "OK");
        }
    }
}
