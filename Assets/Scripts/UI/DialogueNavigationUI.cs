using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space navigation HUD that shows ◀ / ▶ arrows for paging through
/// dialogue lines during a character interaction.
///
/// FEATURES:
///   • Auto-builds its own Canvas + two arrow buttons — no prefab needed.
///   • Anchored near the bottom of the screen, above the Lumiere button.
///   • ◀ and ▶ buttons fire OnPrevious / OnNext events (wired by FindObjectTask).
///   • First/last line: corresponding button is automatically disabled.
///   • Entire panel hides via Hide() when the dialogue session ends.
///
/// SETUP:
///   FindObjectTask.cs instantiates and wires this automatically.
///   You can also drop it on any GameObject and call Show() / Hide() manually.
///
/// HIERARCHY (auto-created on Awake):
///   DialogueNavCanvas [Canvas — ScreenSpaceOverlay]
///     └── NavRow [HorizontalLayoutGroup]
///           ├── PrevButton   "◀"
///           └── NextButton   "▶"
/// </summary>
public class DialogueNavigationUI : MonoBehaviour
{
    // ── Events ────────────────────────────────────────────────────────────────
    public System.Action OnPrevious;
    public System.Action OnNext;

    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Appearance")]
    [SerializeField] private Color buttonColor     = new Color(0.18f, 0.18f, 0.28f, 0.92f);
    [SerializeField] private Color disabledColor   = new Color(0.18f, 0.18f, 0.28f, 0.35f);
    [SerializeField] private Vector2 buttonSize    = new Vector2(72f, 72f);
    [Tooltip("Distance from the bottom edge of the screen (pixels).")]
    [SerializeField] private float bottomOffset    = 120f;
    [Tooltip("Horizontal distance from screen centre to each button (pixels).")]
    [SerializeField] private float sideOffset      = 220f;

    // ── Cached refs ────────────────────────────────────────────────────────────
    private Canvas  _canvas;
    private Button  _prevButton;
    private Button  _nextButton;
    private Image   _prevImage;
    private Image   _nextImage;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildCanvas();
        gameObject.SetActive(false);   // hidden until Show() is called
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the navigation, refreshing enabled state based on current position.
    /// </summary>
    /// <param name="currentIndex">0-based index of the current dialogue line.</param>
    /// <param name="totalCount">Total number of dialogue lines.</param>
    public void Show(int currentIndex, int totalCount)
    {
        gameObject.SetActive(true);
        RefreshButtons(currentIndex, totalCount);
    }

    /// <summary>Updates which arrow buttons are enabled without toggling visibility.</summary>
    public void RefreshButtons(int currentIndex, int totalCount)
    {
        SetButtonEnabled(_prevButton, _prevImage, currentIndex > 0);
        SetButtonEnabled(_nextButton, _nextImage, currentIndex < totalCount - 1);
    }

    /// <summary>Hides the navigation HUD.</summary>
    public void Hide() => gameObject.SetActive(false);

    // ── Private helpers ────────────────────────────────────────────────────────

    private void SetButtonEnabled(Button btn, Image img, bool enable)
    {
        btn.interactable = enable;
        img.color = enable ? buttonColor : disabledColor;
    }

    private void BuildCanvas()
    {
        // Canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;    // above most UI but below the fader
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // ◀ button
        _prevButton = BuildArrowButton("PrevButton", "◀", -sideOffset, out _prevImage);
        _prevButton.onClick.AddListener(() => OnPrevious?.Invoke());

        // ▶ button
        _nextButton = BuildArrowButton("NextButton", "▶", sideOffset, out _nextImage);
        _nextButton.onClick.AddListener(() => OnNext?.Invoke());
    }

    private Button BuildArrowButton(string goName, string arrow, float xOffset, out Image imgOut)
    {
        GameObject go     = new GameObject(goName);
        go.transform.SetParent(transform, false);

        RectTransform rt  = go.AddComponent<RectTransform>();
        rt.anchorMin      = new Vector2(0.5f, 0f);
        rt.anchorMax      = new Vector2(0.5f, 0f);
        rt.pivot          = new Vector2(0.5f, 0f);
        rt.sizeDelta      = buttonSize;
        rt.anchoredPosition = new Vector2(xOffset, bottomOffset);

        Image img    = go.AddComponent<Image>();
        img.color    = buttonColor;
        imgOut       = img;

        Button btn   = go.AddComponent<Button>();

        // Apply rounded-ish appearance via ColorBlock
        ColorBlock cb       = btn.colors;
        cb.normalColor      = buttonColor;
        cb.highlightedColor = new Color(buttonColor.r + 0.1f, buttonColor.g + 0.1f, buttonColor.b + 0.15f, buttonColor.a);
        cb.pressedColor     = new Color(buttonColor.r - 0.05f, buttonColor.g - 0.05f, buttonColor.b, buttonColor.a);
        cb.disabledColor    = disabledColor;
        btn.colors          = cb;

        // Arrow label
        GameObject lblGO     = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        RectTransform lblRT  = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin      = Vector2.zero;
        lblRT.anchorMax      = Vector2.one;
        lblRT.offsetMin      = Vector2.zero;
        lblRT.offsetMax      = Vector2.zero;

        TextMeshProUGUI tmp  = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text             = arrow;
        tmp.fontSize         = 28f;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.color            = Color.white;
        tmp.alignment        = TextAlignmentOptions.Center;

        return btn;
    }
}
