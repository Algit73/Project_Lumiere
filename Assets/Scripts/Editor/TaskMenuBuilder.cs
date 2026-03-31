using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Generates the full Lumiere Task Menu UI hierarchy inside an existing Canvas.
/// Run via: Tools > Build Lumiere Task Menu
///
/// What it creates:
///   Canvas (existing, selected)
///     └── LumiereMenuOverlay      full-screen dark overlay
///           ├── LumiereMenuPanel  centered 560×480 white card
///           │     ├── Title       "Select Activity" text
///           │     ├── CloseButton  X button (top-right)
///           │     └── ButtonGrid  GridLayoutGroup — 2 columns × 3 rows
///           │           ├── Btn_Find        "Find Objects"
///           │           ├── Btn_Name        "Word Naming"
///           │           ├── Btn_Story       "Story Telling"
///           │           ├── Btn_Manipulate  "Object Manipulation"
///           │           ├── Btn_Sort        "Category Sorting"
///           │           └── Btn_Match       "Attribute Matching"
///           └── TaskMenuController component (on LumiereMenuOverlay)
/// </summary>
public static class TaskMenuBuilder
{
    // ── Visual constants ──────────────────────────────────────────────────────
    private static readonly Color OverlayColor       = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color PanelColor         = new Color(0.12f, 0.12f, 0.18f, 0.97f);
    private static readonly Color ButtonNormal       = new Color(0.20f, 0.45f, 0.85f, 1f);
    private static readonly Color ButtonHighlight    = new Color(0.28f, 0.55f, 0.95f, 1f);
    private static readonly Color ButtonPressed      = new Color(0.14f, 0.35f, 0.70f, 1f);
    private static readonly Color TitleColor         = Color.white;
    private static readonly Color ButtonTextColor    = Color.white;
    private static readonly Color CloseBgColor       = new Color(0.75f, 0.20f, 0.20f, 1f);

    private static readonly Vector2 PanelSize        = new Vector2(560f, 500f);
    private static readonly Vector2 ButtonSize       = new Vector2(230f, 90f);
    private const float GridSpacing                  = 16f;
    private const float TopPadding                  = 72f;   // space for title
    private const float SidePadding                 = 24f;
    private const float PanelCornerRadius            = 24f;
    private const float ButtonCornerRadius           = 10f;
    private const string RoundedBtnMatPath           = "Assets/Materials/RoundedButton.mat";

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Build Lumiere Task Menu")]
    public static void Build()
    {
        // 1. Find or create Canvas
        Canvas canvas = FindOrCreateCanvas();
        GameObject canvasGO = canvas.gameObject;

        // 2. Remove existing overlay if rebuilding
        Transform existing = canvasGO.transform.Find("LumiereMenuOverlay");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
            Debug.Log("[TaskMenuBuilder] Removed existing LumiereMenuOverlay.");
        }

        // 3. Overlay (full-screen, catches clicks outside panel)
        GameObject overlay = CreateUIObject("LumiereMenuOverlay", canvasGO.transform);
        StretchFull(overlay.GetComponent<RectTransform>());
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = OverlayColor;
        overlayImg.raycastTarget = true;

        // 4. Panel (centered card)
        GameObject panel = CreateUIObject("LumiereMenuPanel", overlay.transform);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.sizeDelta  = PanelSize;
        panelRT.anchoredPosition = Vector2.zero;

        Image panelImg = panel.AddComponent<Image>();

        // Try to load the BackBlur material (uses Custom/MaskedUIBlur shader).
        // If found, the panel background will blur the scene behind it.
        // Requires "Opaque Texture" enabled in the active URP Renderer Asset.
        Material blurMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BackBlur.mat");
        if (blurMat != null)
        {
            panelImg.material = blurMat;
            // Semi-transparent dark tint layered over the blur
            panelImg.color = new Color(0.08f, 0.08f, 0.14f, 0.72f);
            Debug.Log("[TaskMenuBuilder] BackBlur.mat assigned to panel. " +
                      "Make sure 'Opaque Texture' is ON in your URP Renderer Asset.");
        }
        else
        {
            // Fallback: solid dark panel (blur material not found)
            panelImg.color = PanelColor;
            Debug.LogWarning("[TaskMenuBuilder] BackBlur.mat not found at Assets/Materials/BackBlur.mat. " +
                             "Panel will use a solid background.");
        }

        // CanvasGroup — used by TaskMenuController for scale animation
        panel.AddComponent<CanvasGroup>();

        // Rounded corners on the panel background
        RoundedCorners panelRC = panel.AddComponent<RoundedCorners>();
        panelRC.cornerRadius   = PanelCornerRadius;

        // 5. Title
        GameObject titleGO = CreateUIObject("Title", panel.transform);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0f,  1f);
        titleRT.anchorMax        = new Vector2(1f,  1f);
        titleRT.pivot            = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -16f);
        titleRT.sizeDelta        = new Vector2(0f, 48f);

        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "Select Activity";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color     = TitleColor;
        titleTMP.fontSize  = 26f;
        titleTMP.fontStyle = FontStyles.Bold;

        // 6. Close button (top-right corner)
        GameObject closeGO  = CreateUIObject("CloseButton", panel.transform);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin        = new Vector2(1f, 1f);
        closeRT.anchorMax        = new Vector2(1f, 1f);
        closeRT.pivot            = new Vector2(1f, 1f);
        closeRT.anchoredPosition = new Vector2(-10f, -10f);
        closeRT.sizeDelta        = new Vector2(40f, 40f);

        Image closeBg = closeGO.AddComponent<Image>();
        closeBg.color = CloseBgColor;
        Button closeBtn = closeGO.AddComponent<Button>();
        SetupButtonColors(closeBtn, CloseBgColor);
        AddLabel(closeGO, "✕", 18f);

        // 7. Button Grid
        GameObject gridGO = CreateUIObject("ButtonGrid", panel.transform);
        RectTransform gridRT = gridGO.GetComponent<RectTransform>();
        gridRT.anchorMin        = new Vector2(0f, 0f);
        gridRT.anchorMax        = new Vector2(1f, 1f);
        gridRT.pivot            = new Vector2(0.5f, 0.5f);
        gridRT.offsetMin        = new Vector2(SidePadding,  SidePadding);
        gridRT.offsetMax        = new Vector2(-SidePadding, -(TopPadding + 8f));

        GridLayoutGroup grid         = gridGO.AddComponent<GridLayoutGroup>();
        grid.cellSize                = ButtonSize;
        grid.spacing                 = new Vector2(GridSpacing, GridSpacing);
        grid.startCorner             = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis               = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment          = TextAnchor.MiddleCenter;
        grid.constraint              = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount         = 2;

        // 8. Six activity buttons
        var items = new (string label, string name)[]
        {
            ("Find Objects",        "Btn_Find"),
            ("Word Naming",         "Btn_Name"),
            ("Story Telling",       "Btn_Story"),
            ("Object Manipulation", "Btn_Manipulate"),
            ("Category Sorting",    "Btn_Sort"),
            ("Attribute Matching",  "Btn_Match"),
        };

        Material roundedBtnMat = GetOrCreateRoundedButtonMat();
        Button[] buttons = new Button[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            GameObject btnGO = CreateUIObject(items[i].name, gridGO.transform);
            Image btnImg     = btnGO.AddComponent<Image>();
            if (roundedBtnMat != null) btnImg.material = roundedBtnMat;
            btnImg.color     = ButtonNormal;
            Button btn       = btnGO.AddComponent<Button>();
            SetupButtonColors(btn, ButtonNormal);
            AddLabel(btnGO, items[i].label, 15f);
            RoundedCorners btnRC = btnGO.AddComponent<RoundedCorners>();
            btnRC.cornerRadius   = ButtonCornerRadius;
            buttons[i] = btn;
        }

        // 9. Attach TaskMenuController and wire references
        TaskMenuController controller = overlay.AddComponent<TaskMenuController>();

        // Use SerializedObject to assign private serialized fields
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("overlay").objectReferenceValue    = overlay;
        so.FindProperty("menuPanel").objectReferenceValue  = panelRT;
        so.FindProperty("btnFind").objectReferenceValue       = buttons[0];
        so.FindProperty("btnName").objectReferenceValue       = buttons[1];
        so.FindProperty("btnStory").objectReferenceValue      = buttons[2];
        so.FindProperty("btnManipulate").objectReferenceValue = buttons[3];
        so.FindProperty("btnSort").objectReferenceValue       = buttons[4];
        so.FindProperty("btnMatch").objectReferenceValue      = buttons[5];
        so.FindProperty("btnClose").objectReferenceValue      = closeBtn;
        so.ApplyModifiedProperties();

        // 10. Auto-wire TaskMenuController into Lumier_MainController if present in the scene
        Lumier_MainController mainCtrl = Object.FindFirstObjectByType<Lumier_MainController>();
        if (mainCtrl != null)
        {
            SerializedObject mainSO = new SerializedObject(mainCtrl);
            SerializedProperty taskMenuProp = mainSO.FindProperty("taskMenu");
            if (taskMenuProp != null)
            {
                taskMenuProp.objectReferenceValue = controller;
                mainSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(mainCtrl);
                Debug.Log("[TaskMenuBuilder] TaskMenuController auto-assigned to Lumier_MainController.taskMenu.");
            }
        }
        else
        {
            Debug.LogWarning("[TaskMenuBuilder] Lumier_MainController not found in scene. " +
                             "Manually drag LumiereMenuOverlay's TaskMenuController into the 'Task Menu' field.");
        }

        // 11. Register undo and mark dirty
        Undo.RegisterCreatedObjectUndo(overlay, "Build Lumiere Task Menu");
        EditorUtility.SetDirty(canvasGO);

        // Select the overlay in the hierarchy
        Selection.activeGameObject = overlay;

        Debug.Log("[TaskMenuBuilder] Lumiere Task Menu created successfully.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Material GetOrCreateRoundedButtonMat()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(RoundedBtnMatPath);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Custom/RoundedUI");
        if (shader == null)
        {
            Debug.LogError("[TaskMenuBuilder] Shader 'Custom/RoundedUI' not found. " +
                           "Ensure RoundedUI.shader is imported under Assets/Shaders/.");
            return null;
        }

        mat      = new Material(shader);
        mat.name = "RoundedButton";
        mat.SetColor("_Color", Color.white);
        AssetDatabase.CreateAsset(mat, RoundedBtnMatPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static Canvas FindOrCreateCanvas()
    {
        // Prefer the canvas that is already selected
        if (Selection.activeGameObject != null)
        {
            Canvas c = Selection.activeGameObject.GetComponentInParent<Canvas>();
            if (c != null) return c;
        }
        // Fall back to any canvas in scene
        Canvas found = Object.FindFirstObjectByType<Canvas>();
        if (found != null) return found;

        // Create a new one
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas       = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void SetupButtonColors(Button btn, Color normal)
    {
        ColorBlock cb      = btn.colors;
        cb.normalColor     = normal;
        cb.highlightedColor = ButtonHighlight;
        cb.pressedColor    = ButtonPressed;
        cb.selectedColor   = normal;
        cb.fadeDuration    = 0.1f;
        btn.colors         = cb;
    }

    private static void AddLabel(GameObject parent, string text, float size)
    {
        GameObject labelGO = CreateUIObject("Label", parent.transform);
        RectTransform rt   = labelGO.GetComponent<RectTransform>();
        StretchFull(rt);

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = ButtonTextColor;
        tmp.fontSize  = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = true;
    }
}
