using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates a fully wired SpeechBubble Screen-Space Overlay Canvas in the active scene.
///
/// Run via: Tools > Create Speech Bubble Prefab
///
/// Workflow:
///   1. Run the tool — "SpeechBubble_Preview" appears in the Hierarchy.
///   2. Switch the Game view to your target resolution to preview it correctly.
///   3. Edit sizes, colors and fonts in the Inspector (BubblePanel RectTransform,
///      Image color, TMP font size, etc.).
///   4. Drag the GameObject from the Hierarchy into Assets/Prefabs.
///   5. Assign the prefab to FindObjectTask > Speech Bubble Prefab.
///
/// Panel is anchored to the TOP-CENTER of the screen, 60 px from the top edge.
/// Change topMargin on SpeechBubbleUI, or move the BubblePanel's anchoredPosition.
/// </summary>
public static class SpeechBubbleBuilder
{
    // ── Visual constants — change here or in Inspector after running ──────────
    private const  float PanelW       = 700f;
    private const  float PanelH       = 220f;
    private const  float TopMargin    = 60f;
    private const  float CornerRadius = 18f;
    private static readonly Color PanelBg      = new Color(0.08f, 0.08f, 0.14f, 0.93f);
    private static readonly Color SpeakerColor = new Color(0.85f, 0.85f, 1.00f);
    private static readonly Color ReplayBtnBg  = new Color(0.3f, 0.3f, 0.5f, 1f);
    private static readonly Color SliderFill   = new Color(0.35f, 0.6f, 1f);
    private static readonly Color SliderBg     = new Color(0.2f, 0.2f, 0.3f);
    private static readonly Color LabelGrey    = new Color(0.7f, 0.7f, 0.7f);

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Create Speech Bubble Prefab")]
    public static void Build()
    {
        var existing = GameObject.Find("SpeechBubble_Preview");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[SpeechBubbleBuilder] Replaced existing SpeechBubble_Preview.");
        }

        // ── Root: Screen Space Overlay Canvas ─────────────────────────────────
        GameObject root = new GameObject("SpeechBubble_Preview");
        Undo.RegisterCreatedObjectUndo(root, "Create SpeechBubble_Preview");

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        root.AddComponent<GraphicRaycaster>();

        SpeechBubbleUI bubbleUI = root.AddComponent<SpeechBubbleUI>();

        // ── Panel ──────────────────────────────────────────────────────────────
        GameObject panelGO = Rect("BubblePanel", root.transform);
        RectTransform panelRT = panelGO.GetComponent<RectTransform>();

        // Anchor top-center, grows downward
        panelRT.anchorMin        = new Vector2(0.5f, 1f);
        panelRT.anchorMax        = new Vector2(0.5f, 1f);
        panelRT.pivot            = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0f, -TopMargin);
        panelRT.sizeDelta        = new Vector2(PanelW, PanelH);

        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = PanelBg;

        // Try to load rounded-corner material
        Material roundedMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoundedButton.mat");
        if (roundedMat == null)
        {
            Shader sh = Shader.Find("Custom/RoundedUI");
            if (sh != null) { roundedMat = new Material(sh); roundedMat.name = "RoundedUI (bubble)"; }
        }
        if (roundedMat != null) panelImg.material = roundedMat;

        RoundedCorners panelRC = panelGO.AddComponent<RoundedCorners>();
        panelRC.cornerRadius = CornerRadius;

        // ── Speaker label ──────────────────────────────────────────────────────
        GameObject speakerGO = Rect("SpeakerLabel", panelGO.transform);
        RectTransform speakerRT = speakerGO.GetComponent<RectTransform>();
        speakerRT.anchorMin        = new Vector2(0f, 1f);
        speakerRT.anchorMax        = new Vector2(1f, 1f);
        speakerRT.pivot            = new Vector2(0.5f, 1f);
        speakerRT.anchoredPosition = new Vector2(0f, -10f);
        speakerRT.sizeDelta        = new Vector2(-20f, 34f);

        TextMeshProUGUI speakerTMP = speakerGO.AddComponent<TextMeshProUGUI>();
        speakerTMP.text      = "Character Name";
        speakerTMP.fontSize  = 18f;
        speakerTMP.fontStyle = FontStyles.Bold;
        speakerTMP.color     = SpeakerColor;
        speakerTMP.alignment = TextAlignmentOptions.Left;

        // ── Body text ──────────────────────────────────────────────────────────
        GameObject bodyGO = Rect("BodyText", panelGO.transform);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.offsetMin = new Vector2(14f, 54f);
        bodyRT.offsetMax = new Vector2(-14f, -48f);

        TextMeshProUGUI bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTMP.text               = "Sample dialogue text that wraps across multiple lines of the panel.";
        bodyTMP.fontSize           = 16f;
        bodyTMP.color              = Color.white;
        bodyTMP.alignment          = TextAlignmentOptions.TopLeft;
        bodyTMP.enableWordWrapping = true;

        // ── Controls row ───────────────────────────────────────────────────────
        GameObject rowGO = Rect("ControlsRow", panelGO.transform);
        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0f, 0f);
        rowRT.anchorMax        = new Vector2(1f, 0f);
        rowRT.pivot            = new Vector2(0.5f, 0f);
        rowRT.anchoredPosition = new Vector2(0f, 10f);
        rowRT.sizeDelta        = new Vector2(-20f, 42f);

        HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.spacing                = 12f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;

        // Replay button
        GameObject replayGO = Rect("ReplayButton", rowGO.transform);
        replayGO.GetComponent<RectTransform>().sizeDelta = new Vector2(42f, 42f);
        Image replayImg = replayGO.AddComponent<Image>();
        replayImg.color = ReplayBtnBg;
        Button replayBtn = replayGO.AddComponent<Button>();

        GameObject replayIconGO = Rect("Icon", replayGO.transform);
        RectTransform iconRT = replayIconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        TextMeshProUGUI iconTMP = replayIconGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text      = "\uD83D\uDD0A";  // 🔊
        iconTMP.fontSize  = 20f;
        iconTMP.alignment = TextAlignmentOptions.Center;

        // Speed slider
        GameObject sliderGO = Rect("SpeedSlider", rowGO.transform);
        sliderGO.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 26f);
        Slider slider = BuildSlider(sliderGO);

        // Speed label
        GameObject spdLblGO = Rect("SpeedLabel", rowGO.transform);
        spdLblGO.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 26f);
        TextMeshProUGUI spdTMP = spdLblGO.AddComponent<TextMeshProUGUI>();
        spdTMP.text      = "Speed";
        spdTMP.fontSize  = 13f;
        spdTMP.color     = LabelGrey;
        spdTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // ── Exit button (top-right corner of panel) ────────────────────────────
        GameObject exitGO = Rect("ExitButton", panelGO.transform);
        RectTransform exitRT = exitGO.GetComponent<RectTransform>();
        exitRT.anchorMin        = new Vector2(1f, 1f);
        exitRT.anchorMax        = new Vector2(1f, 1f);
        exitRT.pivot            = new Vector2(1f, 1f);
        exitRT.anchoredPosition = new Vector2(-8f, -8f);
        exitRT.sizeDelta        = new Vector2(36f, 36f);
        exitGO.AddComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f, 0.90f);
        Button exitBtn = exitGO.AddComponent<Button>();

        GameObject exitLblGO = Rect("Label", exitGO.transform);
        RectTransform exitLblRT = exitLblGO.GetComponent<RectTransform>();
        exitLblRT.anchorMin = Vector2.zero;
        exitLblRT.anchorMax = Vector2.one;
        exitLblRT.offsetMin = exitLblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI exitTMP = exitLblGO.AddComponent<TextMeshProUGUI>();
        exitTMP.text      = "\u2715";
        exitTMP.fontSize  = 18f;
        exitTMP.fontStyle = FontStyles.Bold;
        exitTMP.color     = Color.white;
        exitTMP.alignment = TextAlignmentOptions.Center;

        // ── Wire serialized refs on SpeechBubbleUI ─────────────────────────────
        SerializedObject so = new SerializedObject(bubbleUI);
        so.FindProperty("_speakerLabel").objectReferenceValue = speakerTMP;
        so.FindProperty("_bodyText").objectReferenceValue     = bodyTMP;
        so.FindProperty("_replayButton").objectReferenceValue = replayBtn;
        so.FindProperty("_speedSlider").objectReferenceValue  = slider;
        so.FindProperty("_exitButton").objectReferenceValue   = exitBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("[SpeechBubbleBuilder] Created SpeechBubble_Preview (Screen Space Overlay).\n" +
                  "Edit in the Hierarchy/Inspector using the Game view for accurate sizing.\n" +
                  "Drag to Assets/Prefabs, then assign to FindObjectTask > Speech Bubble Prefab.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static Slider BuildSlider(GameObject parent)
    {
        Slider slider = parent.AddComponent<Slider>();

        // Background
        GameObject bgGO = Rect("Background", parent.transform);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = SliderBg;

        // Fill area
        GameObject fillAreaGO = Rect("FillArea", parent.transform);
        RectTransform fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5f, 0f);
        fillAreaRT.offsetMax = new Vector2(-15f, 0f);

        GameObject fillGO = Rect("Fill", fillAreaGO.transform);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.up;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        fillGO.AddComponent<Image>().color = SliderFill;

        // Handle area
        GameObject handleAreaGO = Rect("HandleSlideArea", parent.transform);
        RectTransform handleAreaRT = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10f, 0f);
        handleAreaRT.offsetMax = new Vector2(-10f, 0f);

        GameObject handleGO = Rect("Handle", handleAreaGO.transform);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(14f, 14f);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect      = fillRT;
        slider.handleRect    = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = 0.5f;
        slider.maxValue      = 2.0f;
        slider.value         = 1.0f;

        return slider;
    }
}
