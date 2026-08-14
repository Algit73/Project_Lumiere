using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates and maintains the <see cref="FillerLineBankSO"/> asset used by
/// <see cref="FindObjectTask"/> for instant wrong-tap/hint reactions.
///
/// Workflow:
///   1. Tools > Lumiere > Create Filler Line Bank Asset
///        Creates the asset pre-populated with the predefined texts below (no audio yet).
///   2. Tools > Lumiere > Export Filler Texts To JSON
///        Writes filler_lines.json so the offline Python generator (generate_filler_audio.py)
///        narrates EXACTLY the same text shown in the bubble.
///   3. Run generate_filler_audio.py (see Assets/Scripts/OpenAI/Tools/) to synthesize
///      Assets/Audio/Fillers/Wrong/wrong_NN.mp3 and Assets/Audio/Fillers/Hint/hint_NN.mp3
///      via the OpenAI TTS API.
///   4. Tools > Lumiere > Auto-Assign Filler Audio Clips
///        Matches the generated clips back onto the bank entries by index.
/// </summary>
public static class FillerLineBankTools
{
    private const string DefaultFolder     = "Assets/ScriptableObjects";
    private const string DefaultAssetPath  = DefaultFolder + "/FillerLineBank.asset";
    private const string JsonExportPath    = "Assets/Scripts/OpenAI/Tools/filler_lines.json";
    private const string WrongAudioFolder  = "Assets/Audio/Fillers/Wrong";
    private const string HintAudioFolder   = "Assets/Audio/Fillers/Hint";

    // ── Predefined filler texts (source of truth — also exported to JSON for narration) ──
    // Kept varied and non-repetitive so repeated wrong taps don't feel obviously canned.
    // All phrasing is deliberately warm/patient — never frustrated or judgmental (aphasia-friendly).

    private static readonly string[] WrongGuessTexts =
    {
        "Hmm, let's see...",
        "Oh, not quite that one.",
        "Let me think for a second...",
        "Close, but not this time.",
        "That's a good guess, let me check.",
        "Ooh, almost — hold on.",
        "Let's take another look.",
        "Not this one, but good try!",
        "Give me just a moment...",
        "Hmm, let me picture it again.",
        "That's okay, let's keep looking.",
        "One moment, let me think it through.",
        "Good try — let's see what else.",
        "Let's have another look around.",
        "Almost — just a little more thinking.",
        "That's alright, we'll find it together."
    };

    private static readonly string[] HintTexts =
    {
        "Sure, let me help a bit more.",
        "No problem, here's a little help.",
        "Let's break it down together.",
        "Of course — let me give you a clue.",
        "Here, this might help.",
        "Let's think about it together.",
        "Okay, let me point you closer.",
        "Sure thing, one more clue coming up.",
        "Let's narrow it down a little.",
        "Here's something that might help you."
    };

    [MenuItem("Tools/Lumiere/Create Filler Line Bank Asset")]
    public static void CreateBankAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<FillerLineBankSO>(DefaultAssetPath) != null)
        {
            Debug.LogWarning($"[FillerLineBankTools] Asset already exists at {DefaultAssetPath} — selecting it instead.");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<FillerLineBankSO>(DefaultAssetPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            return;
        }

        if (!AssetDatabase.IsValidFolder(DefaultFolder))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

        var bank = ScriptableObject.CreateInstance<FillerLineBankSO>();

        foreach (string text in WrongGuessTexts)
            bank.wrongGuessFillers.Add(new FillerEntry { text = text, clip = null });

        foreach (string text in HintTexts)
            bank.hintRequestFillers.Add(new FillerEntry { text = text, clip = null });

        AssetDatabase.CreateAsset(bank, DefaultAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = bank;
        EditorGUIUtility.PingObject(bank);

        Debug.Log($"[FillerLineBankTools] Created {DefaultAssetPath} with {bank.wrongGuessFillers.Count} wrong-guess " +
                  $"and {bank.hintRequestFillers.Count} hint fillers (text only, no audio yet). " +
                  "Next: Tools > Lumiere > Export Filler Texts To JSON, then run generate_filler_audio.py, " +
                  "then Tools > Lumiere > Auto-Assign Filler Audio Clips.");
    }

    [MenuItem("Tools/Lumiere/Export Filler Texts To JSON")]
    public static void ExportTextsToJson()
    {
        string dir = Path.GetDirectoryName(JsonExportPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var export = new FillerJsonExport { wrong = WrongGuessTexts, hint = HintTexts };
        string json = JsonUtility.ToJson(export, prettyPrint: true);
        File.WriteAllText(JsonExportPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"[FillerLineBankTools] Exported filler texts to {JsonExportPath}. " +
                  "Run generate_filler_audio.py next to synthesize matching audio clips.");
    }

    [MenuItem("Tools/Lumiere/Auto-Assign Filler Audio Clips")]
    public static void AutoAssignClips()
    {
        var bank = AssetDatabase.LoadAssetAtPath<FillerLineBankSO>(DefaultAssetPath);
        if (bank == null)
        {
            Debug.LogWarning("[FillerLineBankTools] No FillerLineBank.asset found — run " +
                              "'Create Filler Line Bank Asset' first.");
            return;
        }

        int wrongAssigned = AssignClipsFromFolder(WrongAudioFolder, "wrong_", bank.wrongGuessFillers);
        int hintAssigned  = AssignClipsFromFolder(HintAudioFolder, "hint_", bank.hintRequestFillers);

        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();

        Debug.Log($"[FillerLineBankTools] Assigned {wrongAssigned} wrong-guess clip(s) and {hintAssigned} hint clip(s).");
    }

    private static int AssignClipsFromFolder(string folder, string prefix, System.Collections.Generic.List<FillerEntry> entries)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[FillerLineBankTools] Folder not found: {folder} — generate audio there first.");
            return 0;
        }

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
        var clipsByPath = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)
            .ToList();

        int assigned = 0;
        for (int i = 0; i < entries.Count && i < clipsByPath.Count; i++)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipsByPath[i]);
            if (clip == null) continue;
            entries[i].clip = clip;
            assigned++;
        }
        return assigned;
    }

    [System.Serializable]
    private class FillerJsonExport
    {
        public string[] wrong;
        public string[] hint;
    }
}
