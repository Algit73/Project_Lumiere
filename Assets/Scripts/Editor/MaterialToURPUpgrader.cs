using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor utility to upgrade materials from Built-in Render Pipeline to URP
/// </summary>
public class MaterialToURPUpgrader : EditorWindow
{
    private static readonly string URPLitShaderName = "Universal Render Pipeline/Lit";
    private static readonly string URPUnlitShaderName = "Universal Render Pipeline/Unlit";
    private static readonly string URPSimpleLitShaderName = "Universal Render Pipeline/Simple Lit";

    private List<Material> incompatibleMaterials = new List<Material>();
    private Vector2 scrollPosition;
    private bool hasScanned = false;

    [MenuItem("Tools/URP Material Upgrader")]
    public static void ShowWindow()
    {
        GetWindow<MaterialToURPUpgrader>("URP Material Upgrader");
    }

    private void OnGUI()
    {
        GUILayout.Label("URP Material Upgrader", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool will scan your project for materials using incompatible shaders " +
            "and upgrade them to URP equivalents. Custom shaders (Toon, Unlit, etc.) will be " +
            "converted to URP Lit/Unlit, but some visual effects may be lost. " +
            "For best results with Toon shaders, install a URP-compatible Toon Shader package.\n\n" +
            "If you have FBX models with embedded materials, use 'Extract & Scan FBX Materials'.\n" +
            "If you have pink DAE models (Kenney food-kit), use 'Fix DAE Materials' button.",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Extract & Scan FBX Materials", GUILayout.Height(30)))
        {
            ExtractFBXMaterials();
            ScanMaterials();
        }
        
        if (GUILayout.Button("Fix DAE Materials (Kenney Food-Kit)", GUILayout.Height(30)))
        {
            FixDAEMaterials();
        }

        if (GUILayout.Button("Scan for Incompatible Materials", GUILayout.Height(30)))
        {
            ScanMaterials();
        }

        if (hasScanned)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Found {incompatibleMaterials.Count} materials that may need upgrading:");
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            foreach (var mat in incompatibleMaterials)
            {
                if (mat != null && mat.shader != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(mat, typeof(Material), false);
                    GUILayout.Label($"Shader: {mat.shader.name}", GUILayout.Width(300));
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            if (incompatibleMaterials.Count > 0)
            {
                if (GUILayout.Button("Upgrade All Materials to URP", GUILayout.Height(30)))
                {
                    UpgradeMaterials();
                }
            }
        }
    }

    private void ExtractFBXMaterials()
    {
        // Find all FBX files in Assets/Models
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models" });
        int extractedCount = 0;
        int totalCount = 0;

        EditorUtility.DisplayProgressBar("Extracting Materials", "Finding FBX files...", 0f);

        List<string> pathsToReimport = new List<string>();

        foreach (string guid in fbxGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Only process FBX files
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                continue;

            totalCount++;
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            // Check if materials need to be extracted (materialLocation 0 = InPrefab/embedded)
            if (importer.materialLocation != ModelImporterMaterialLocation.External)
            {
                // Change settings to extract materials
                importer.materialLocation = ModelImporterMaterialLocation.External;
                importer.materialName = ModelImporterMaterialName.BasedOnModelNameAndMaterialName;
                importer.materialSearch = ModelImporterMaterialSearch.Local;
                
                EditorUtility.SetDirty(importer);
                pathsToReimport.Add(path);
                extractedCount++;

                EditorUtility.DisplayProgressBar("Configuring FBX Import Settings", 
                    $"Configured {extractedCount} of {totalCount} models...", 
                    (float)totalCount / fbxGuids.Length);
            }
        }

        EditorUtility.DisplayProgressBar("Extracting Materials", "Saving changes...", 0.9f);
        
        if (pathsToReimport.Count > 0)
        {
            AssetDatabase.SaveAssets();
            
            // Reimport all modified FBX files to extract materials
            for (int i = 0; i < pathsToReimport.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Extracting Materials", 
                    $"Extracting materials from {System.IO.Path.GetFileName(pathsToReimport[i])} ({i + 1}/{pathsToReimport.Count})", 
                    (float)i / pathsToReimport.Count);
                
                AssetDatabase.ImportAsset(pathsToReimport[i], ImportAssetOptions.ForceUpdate);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
        if (extractedCount > 0)
        {
            Debug.Log($"<color=cyan>Extracted materials from {extractedCount} FBX files. Materials are now in Materials folders next to FBX files.</color>");
            EditorUtility.DisplayDialog("Extraction Complete", 
                $"Extracted materials from {extractedCount} FBX files.\n\n" +
                "Materials have been created as external .mat files in 'Materials' folders.\n\n" +
                "Now click 'Scan for Incompatible Materials' to find and upgrade them.", 
                "OK");
        }
        else
        {
            Debug.Log("No embedded materials found to extract. All FBX models already use external materials.");
            EditorUtility.DisplayDialog("No Extraction Needed", 
                "All FBX models already use external materials.\n\n" +
                "Click 'Scan for Incompatible Materials' to check for materials that need upgrading.", 
                "OK");
        }
    }

    private void FixDAEMaterials()
    {
        // Find all DAE files
        string[] daeGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models" });
        int processedCount = 0;
        int totalDAE = 0;
        
        Shader urpLitShader = Shader.Find(URPLitShaderName);
        if (urpLitShader == null)
        {
            Debug.LogError("Could not find URP Lit shader. Make sure URP is properly installed.");
            EditorUtility.DisplayDialog("Error", "URP Lit shader not found!", "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("Fixing DAE Materials", "Finding DAE files...", 0f);

        List<string> pathsProcessed = new List<string>();

        // First pass: ensure materials are imported and extract them
        foreach (string guid in daeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            if (!path.EndsWith(".dae", System.StringComparison.OrdinalIgnoreCase))
                continue;

            totalDAE++;
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            bool changed = false;
            
            // Ensure materials are imported
            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                changed = true;
            }
            
            // Use external materials
            if (importer.materialLocation != ModelImporterMaterialLocation.External)
            {
                importer.materialLocation = ModelImporterMaterialLocation.External;
                changed = true;
            }
            
            if (changed)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                pathsProcessed.Add(path);
                processedCount++;
            }

            EditorUtility.DisplayProgressBar("Fixing DAE Materials", 
                $"Processing {System.IO.Path.GetFileName(path)}...", 
                (float)totalDAE / daeGuids.Length * 0.5f);
        }
        
        if (pathsProcessed.Count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Wait a moment for Unity to process
            System.Threading.Thread.Sleep(500);
        }

        // Second pass: upgrade the extracted materials to URP
        int upgradedMaterials = 0;
        
        foreach (string modelPath in pathsProcessed)
        {
            // Get the directory where materials should be
            string modelDir = System.IO.Path.GetDirectoryName(modelPath);
            string materialsDir = System.IO.Path.Combine(modelDir, "Materials");
            
            if (System.IO.Directory.Exists(materialsDir))
            {
                string[] matFiles = System.IO.Directory.GetFiles(materialsDir, "*.mat");
                
                foreach (string matPath in matFiles)
                {
                    string assetPath = matPath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    
                    if (mat != null && mat.shader != null)
                    {
                        string shaderName = mat.shader.name;
                        
                        // Check if it needs upgrading
                        if (shaderName.Contains("Standard") || 
                            shaderName.Contains("Diffuse") || 
                            shaderName.Contains("Legacy") ||
                            IsIncompatibleBuiltInShader(shaderName))
                        {
                            // Store properties
                            Color? color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : (Color?)null;
                            Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                            
                            // Upgrade to URP
                            mat.shader = urpLitShader;
                            
                            // Restore properties
                            if (color.HasValue && mat.HasProperty("_BaseColor"))
                                mat.SetColor("_BaseColor", color.Value);
                            if (tex != null && mat.HasProperty("_BaseMap"))
                                mat.SetTexture("_BaseMap", tex);
                            
                            EditorUtility.SetDirty(mat);
                            upgradedMaterials++;
                        }
                    }
                }
            }
            
            EditorUtility.DisplayProgressBar("Upgrading Materials", 
                $"Upgraded {upgradedMaterials} materials...", 
                0.5f + ((float)pathsProcessed.IndexOf(modelPath) / pathsProcessed.Count) * 0.5f);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        if (processedCount > 0 || upgradedMaterials > 0)
        {
            Debug.Log($"<color=green>Processed {processedCount} DAE files and upgraded {upgradedMaterials} materials to URP!</color>");
            EditorUtility.DisplayDialog("DAE Materials Fixed", 
                $"Processed {processedCount} DAE files.\n" +
                $"Upgraded {upgradedMaterials} materials to URP.\n\n" +
                "Check your DAE models - they should now render correctly!", 
                "OK");
        }
        else
        {
            Debug.Log("No DAE files needed processing.");
            EditorUtility.DisplayDialog("No Changes Needed", 
                "All DAE files are already configured correctly.\n\n" +
                "If materials are still pink, try using 'Scan for Incompatible Materials' instead.", 
                "OK");
        }
    }
    
    private void UpgradeEmbeddedMaterialsToURP(List<string> modelPaths)
    {
        // This method is no longer used for DAE files
        // Keeping it for potential future use with other model types
    }

    private void ScanMaterials()
    {
        incompatibleMaterials.Clear();

        // Find all materials in the project (excluding Library and Packages)
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Skip samples, plugins (except if they're in the main Materials folder), and TextMesh Pro
            if (path.Contains("/Samples/") || 
                path.Contains("/Plugins/CW/") || 
                path.Contains("/TextMesh Pro/"))
            {
                continue;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader != null)
            {
                string shaderName = mat.shader.name;
                
                // Check if shader is a Built-in shader that needs upgrading
                if (IsIncompatibleBuiltInShader(shaderName))
                {
                    incompatibleMaterials.Add(mat);
                }
            }
        }

        hasScanned = true;
        Debug.Log($"Scan complete. Found {incompatibleMaterials.Count} materials that may need upgrading.");
    }

    private bool IsIncompatibleBuiltInShader(string shaderName)
    {
        // List of Built-in shaders that are incompatible with URP
        string[] incompatibleShaders = new string[]
        {
            "Standard",
            "Standard (Specular setup)",
            "Mobile/Diffuse",
            "Mobile/Bumped Specular",
            "Mobile/Unlit (Supports Lightmap)",
            "Unlit/Color",
            "Unlit/Texture",
            "Unlit/Transparent",
            "Unlit/UnlitColor",
            "Legacy Shaders/",
            "Particles/Standard Unlit",
            "Particles/Standard Surface",
            "Skybox/",
            "UnityChanToonShader/"
        };

        foreach (string incompatibleShader in incompatibleShaders)
        {
            if (shaderName.StartsWith(incompatibleShader))
            {
                return true;
            }
        }

        return false;
    }

    private void UpgradeMaterials()
    {
        if (incompatibleMaterials == null || incompatibleMaterials.Count == 0)
        {
            Debug.LogWarning("No materials to upgrade.");
            return;
        }

        int upgradedCount = 0;
        Shader urpLitShader = Shader.Find(URPLitShaderName);
        Shader urpUnlitShader = Shader.Find(URPUnlitShaderName);
        Shader urpSimpleLitShader = Shader.Find(URPSimpleLitShaderName);

        if (urpLitShader == null)
        {
            Debug.LogError("Could not find URP Lit shader. Make sure URP is properly installed.");
            return;
        }

        Undo.RecordObjects(incompatibleMaterials.ToArray(), "Upgrade Materials to URP");

        foreach (Material mat in incompatibleMaterials)
        {
            if (mat == null || mat.shader == null) continue;

            string oldShaderName = mat.shader.name;
            Shader newShader = null;

            // Determine which URP shader to use based on the old shader
            if (oldShaderName.StartsWith("UnityChanToonShader/"))
            {
                // Toon shaders need special handling
                // Try to find URP Toon Shader first, fallback to URP Lit
                newShader = Shader.Find("Universal Render Pipeline/Toon") ?? urpLitShader;
                if (newShader == urpLitShader)
                {
                    Debug.LogWarning($"No URP Toon Shader found for {mat.name}. Using URP Lit instead. " +
                        "Consider installing a URP-compatible Toon Shader for better results.");
                }
            }
            else if (oldShaderName.Contains("Unlit") || 
                     oldShaderName.Contains("Particles/") || 
                     oldShaderName == "Unlit/UnlitColor")
            {
                newShader = urpUnlitShader;
            }
            else if (oldShaderName.Contains("Mobile/"))
            {
                newShader = urpSimpleLitShader;
            }
            else
            {
                newShader = urpLitShader;
            }

            if (newShader != null)
            {
                // Store old properties
                Color? mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : (Color?)null;
                Color? baseColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : (Color?)null;
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Texture baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
                Texture normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                float? metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : (float?)null;
                float? smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 
                                   mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : (float?)null;
                Color? emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : (Color?)null;
                Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;

                // Change shader
                mat.shader = newShader;

                // Restore properties to URP equivalents
                // Priority: BaseColor > Color
                if (baseColor.HasValue && mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", baseColor.Value);
                else if (mainColor.HasValue && mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", mainColor.Value);
                
                // Priority: BaseMap > MainTex
                if (baseMap != null && mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", baseMap);
                else if (mainTex != null && mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", mainTex);
                
                if (normalMap != null && mat.HasProperty("_BumpMap"))
                    mat.SetTexture("_BumpMap", normalMap);
                
                if (metallic.HasValue && mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", metallic.Value);
                
                if (smoothness.HasValue && mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", smoothness.Value);
                
                // Handle emission
                if (emissionColor.HasValue && mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emissionColor.Value);
                    mat.EnableKeyword("_EMISSION");
                }
                
                if (emissionMap != null && mat.HasProperty("_EmissionMap"))
                    mat.SetTexture("_EmissionMap", emissionMap);

                EditorUtility.SetDirty(mat);
                upgradedCount++;

                Debug.Log($"Upgraded: {AssetDatabase.GetAssetPath(mat)} from {oldShaderName} to {newShader.name}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>Successfully upgraded {upgradedCount} materials to URP!</color>");
        EditorUtility.DisplayDialog("Upgrade Complete", 
            $"Successfully upgraded {upgradedCount} materials to Universal Render Pipeline.", 
            "OK");

        // Rescan
        incompatibleMaterials.Clear();
        hasScanned = false;
    }
}
