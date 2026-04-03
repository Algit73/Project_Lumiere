using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-space speech bubble that floats above an NPC and displays dialogue.
///
/// FEATURES:
///   • Auto-builds its own UI hierarchy — no prefab required.
///   • Displays the current DialogueLine text and speaker name.
///   • Speaker icon button (🔊) replays TTS for the current line.
///   • Speed slider (0.5–2.0) adjusts TTS playback speed in real time.
///   • Billboard: always faces Camera.main (Y-axis only, no tilt).
///   • Rounded corners via Custom/RoundedUI shader + RoundedCorners component.
///
/// SETUP:
///   Call FindObjectTask.cs — it instantiates and positions this automatically.
///   Or: Attach to any GameObject, call Show(DialogueLine) to display a line.
///
/// HIERARCHY (auto-created on Awake):
///   SpeechBubble [this, Canvas — World Space]
///     └── BubblePanel   [Image + RoundedCorners]
///           ├── SpeakerLabel  [TMP — character name]
///           ├── BodyText      [TMP — dialogue text]
///           └── ControlsRow
///                 ├── ReplayButton   [Button "🔊"]
///                 └── SpeedSlider    [Slider 0.5–2.0]
/// </summary>
[RequireComponent(typeof(Canvas))]
public class SpeechBubbleUI : MonoBehaviour
{
    // ── Inspector (optional overrides) ────────────────────────────────────────
    [Header("Appearance")]
    [Tooltip("Width of the bubble in world units.")]
    [SerializeField] private float bubbleWidth  = 1.4f;
    [Tooltip("Height of the bubble in world units.")]
    [SerializeField] private float bubbleHeight = 0.65f;
    [Tooltip("Background color of the bubble panel.")]
    [SerializeField] private Color panelColor   = new Color(0.08f, 0.08f, 0.14f, 0.93f);
    [Tooltip("Corner radius in pixels (matches RoundedUI shader).")]
    [SerializeField] private float cornerRadius = 18f;

    [Header("Billboard")]
    [Tooltip("Offset above the attachment point (world units).")]
    [SerializeField] private float verticalOffset = 0.1f;

    // ── Cached UI references ──────────────────────────────────────────────────
    private Canvas              _canvas;
    private TextMeshProUGUI     _speakerLabel;
    private TextMeshProUGUI     _bodyText;
    private Button              _replayButton;
    private Slider              _speedSlider;
    private RoundedCorners      _panelRC;

    // ── Currently displayed line ──────────────────────────────────────────────
    private DialogueLine _currentLine;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode  = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;

        // Scale the CANVAS RectTransform so that 1 canvas pixel = 1 / pixelsPerUnit world unit.
        // This lets the entire hierarchy work in intuitive "pixel" units (e.g. 280 px → 1.4 m)
        // without needing per-element scale overrides.
        const float ppu = 200f;
        RectTransform canvasRT = GetComponent<RectTransform>();
        canvasRT.localScale = Vector3.one / ppu;
        canvasRT.sizeDelta  = Vector2.zero;   // canvas has no fixed size; the panel child defines it

        // GraphicRaycaster is required for button and slider interaction on a WorldSpace canvas
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        BuildHierarchy();
        gameObject.SetActive(false);   // hidden until Show() is called
    }

    private void LateUpdate()
    {
        // Billboard: rotate toward camera, Y-axis only
        if (Camera.main == null) return;
        Vector3 toCam = Camera.main.transform.position - transform.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Displays the bubble with the given dialogue line and triggers TTS.
    /// </summary>
    public void Show(DialogueLine line)
    {
        _currentLine = line;
        gameObject.SetActive(true);

        _speakerLabel.text = line.speakerName;
        _bodyText.text     = line.text;

        SpeakCurrentLine();
    }

    /// <summary>Hides the bubble and silences any in-progress TTS.</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void SpeakCurrentLine()
    {
        if (!string.IsNullOrWhiteSpace(_currentLine.text))
            TTSManager.Manager.Speak(_currentLine.text);
    }

    private void OnReplayClicked() => SpeakCurrentLine();

    private void OnSpeedChanged(float value)
    {
        TTSManager.Manager.SetSpeechSpeed(value);
    }

    // ── Auto-built hierarchy ───────────────────────────────────────────────────

    private void BuildHierarchy()
    {
        const float pixelsPerUnit = 200f;
        float pxW = bubbleWidth  * pixelsPerUnit;
        float pxH = bubbleHeight * pixelsPerUnit;

        // ── Panel ──────────────────────────────────────────────────────────────
        GameObject panelGO = new GameObject("BubblePanel");
        panelGO.transform.SetParent(transform, false);

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.sizeDelta        = new Vector2(pxW, pxH);
        panelRT.anchoredPosition = new Vector2(0f, verticalOffset * pixelsPerUnit);
        // Note: no localScale override here — the canvas itself is already scaled by 1/pixelsPerUnit.

        Image panelImg = panelGO.AddComponent<Image>();

        // Try to apply the rounded-UI shader
        Material roundedMat = TryLoadRoundedMat();
        if (roundedMat != null) panelImg.material = roundedMat;
        panelImg.color = panelColor;

        _panelRC = panelGO.AddComponent<RoundedCorners>();
        _panelRC.cornerRadius = cornerRadius;

        // ── Speaker label ──────────────────────────────────────────────────────
        GameObject speakerGO = MakeChild("SpeakerLabel", panelGO.transform);
        RectTransform speakerRT = speakerGO.GetComponent<RectTransform>();
        speakerRT.anchorMin        = new Vector2(0f, 1f);
        speakerRT.anchorMax        = new Vector2(1f, 1f);
        speakerRT.pivot            = new Vector2(0.5f, 1f);
        speakerRT.anchoredPosition = new Vector2(0f, -8f);
        speakerRT.sizeDelta        = new Vector2(-16f, 26f);

        _speakerLabel           = speakerGO.AddComponent<TextMeshProUGUI>();
        _speakerLabel.fontSize  = 14f;
        _speakerLabel.fontStyle = FontStyles.Bold;
        _speakerLabel.color     = new Color(0.85f, 0.85f, 1f);
        _speakerLabel.alignment = TextAlignmentOptions.Left;
        _speakerLabel.text      = "";

        // ── Body text ──────────────────────────────────────────────────────────
        GameObject bodyGO = MakeChild("BodyText", panelGO.transform);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin        = new Vector2(0f, 0f);
        bodyRT.anchorMax        = new Vector2(1f, 1f);
        bodyRT.offsetMin        = new Vector2(10f,  44f);   // leave room at bottom for controls
        bodyRT.offsetMax        = new Vector2(-10f, -32f);  // leave room at top for speaker name

        _bodyText           = bodyGO.AddComponent<TextMeshProUGUI>();
        _bodyText.fontSize  = 13f;
        _bodyText.color     = Color.white;
        _bodyText.alignment = TextAlignmentOptions.TopLeft;
        _bodyText.enableWordWrapping = true;
        _bodyText.text      = "";

        // ── Controls row ───────────────────────────────────────────────────────
        GameObject rowGO = MakeChild("ControlsRow", panelGO.transform);
        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0f, 0f);
        rowRT.anchorMax        = new Vector2(1f, 0f);
        rowRT.pivot            = new Vector2(0.5f, 0f);
        rowRT.anchoredPosition = new Vector2(0f, 8f);
        rowRT.sizeDelta        = new Vector2(-16f, 34f);

        HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.spacing                = 8f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.padding                = new RectOffset(0, 0, 0, 0);

        // Replay button
        GameObject replayGO   = MakeChild("ReplayButton", rowGO.transform);
        RectTransform replayRT = replayGO.GetComponent<RectTransform>();
        replayRT.sizeDelta     = new Vector2(34f, 34f);

        Image replayImg  = replayGO.AddComponent<Image>();
        replayImg.color  = new Color(0.3f, 0.3f, 0.5f, 1f);
        _replayButton    = replayGO.AddComponent<Button>();
        _replayButton.onClick.AddListener(OnReplayClicked);

        GameObject replayLblGO    = MakeChild("Icon", replayGO.transform);
        RectTransform replayLblRT = replayLblGO.GetComponent<RectTransform>();
        replayLblRT.anchorMin = Vector2.zero;
        replayLblRT.anchorMax = Vector2.one;
        replayLblRT.offsetMin = Vector2.zero;
        replayLblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI iconTMP = replayLblGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text      = "\uD83D\uDD0A";   // 🔊 speaker emoji
        iconTMP.fontSize  = 16f;
        iconTMP.alignment = TextAlignmentOptions.Center;

        // Speed slider
        GameObject sliderGO   = MakeChild("SpeedSlider", rowGO.transform);
        RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.sizeDelta     = new Vector2(140f, 20f);

        _speedSlider = BuildSlider(sliderGO);
        _speedSlider.minValue = 0.5f;
        _speedSlider.maxValue = 2.0f;
        _speedSlider.value    = 1.0f;
        _speedSlider.onValueChanged.AddListener(OnSpeedChanged);

        // Speed label
        GameObject spdLblGO    = MakeChild("SpeedLabel", rowGO.transform);
        RectTransform spdLblRT = spdLblGO.GetComponent<RectTransform>();
        spdLblRT.sizeDelta     = new Vector2(50f, 20f);
        TextMeshProUGUI spdTMP = spdLblGO.AddComponent<TextMeshProUGUI>();
        spdTMP.text      = "Speed";
        spdTMP.fontSize  = 10f;
        spdTMP.color     = new Color(0.7f, 0.7f, 0.7f);
        spdTMP.alignment = TextAlignmentOptions.MidlineLeft;
    }

    // Builds a minimal Slider — background + fill + handle
    private static Slider BuildSlider(GameObject parent)
    {
        Slider slider = parent.AddComponent<Slider>();

        // Background
        GameObject bgGO      = MakeChild("Background", parent.transform);
        RectTransform bgRT   = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin       = new Vector2(0f, 0.25f);
        bgRT.anchorMax       = new Vector2(1f, 0.75f);
        bgRT.offsetMin       = Vector2.zero;
        bgRT.offsetMax       = Vector2.zero;
        Image bgImg          = bgGO.AddComponent<Image>();
        bgImg.color          = new Color(0.2f, 0.2f, 0.3f);

        // Fill area & fill
        GameObject fillAreaGO    = MakeChild("FillArea", parent.transform);
        RectTransform fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin     = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax     = new Vector2(1f, 0.75f);
        fillAreaRT.offsetMin     = new Vector2(5f, 0f);
        fillAreaRT.offsetMax     = new Vector2(-15f, 0f);

        GameObject fillGO    = MakeChild("Fill", fillAreaGO.transform);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin     = Vector2.zero;
        fillRT.anchorMax     = Vector2.up;  // left-anchor stretches with value
        fillRT.offsetMin     = Vector2.zero;
        fillRT.offsetMax     = Vector2.zero;
        Image fillImg        = fillGO.AddComponent<Image>();
        fillImg.color        = new Color(0.35f, 0.6f, 1f);

        // Handle area & handle
        GameObject handleAreaGO     = MakeChild("HandleSlideArea", parent.transform);
        RectTransform handleAreaRT  = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRT.anchorMin      = Vector2.zero;
        handleAreaRT.anchorMax      = Vector2.one;
        handleAreaRT.offsetMin      = new Vector2(10f, 0f);
        handleAreaRT.offsetMax      = new Vector2(-10f, 0f);

        GameObject handleGO    = MakeChild("Handle", handleAreaGO.transform);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta     = new Vector2(14f, 14f);
        Image handleImg        = handleGO.AddComponent<Image>();
        handleImg.color        = Color.white;

        // Wire Slider references
        slider.fillRect    = fillRT;
        slider.handleRect  = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction   = Slider.Direction.LeftToRight;

        return slider;
    }

    private static GameObject MakeChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static Material TryLoadRoundedMat()
    {
        // Try the RoundedButton.mat created by TaskMenuBuilder first;
        // fall back to any material using Custom/RoundedUI
        Material mat = null;
#if UNITY_EDITOR
        mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                  "Assets/Materials/RoundedButton.mat");
#endif
        if (mat == null)
        {
            Shader sh = Shader.Find("Custom/RoundedUI");
            if (sh != null) { mat = new Material(sh); mat.name = "RoundedUI (bubble)"; }
        }
        return mat;
    }
}
