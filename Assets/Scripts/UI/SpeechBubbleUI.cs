using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space speech bubble shown at the top of the screen during NPC dialogue.
///
/// DESIGN DECISION — Screen Space Overlay (not World Space):
///   World Space canvases inherit the NPC's world scale and rely on collider
///   bounds for position, both of which vary wildly between characters.
///   Screen Space Overlay is immune to all of that:
///     • Size is in real screen pixels — identical on every device.
///     • Position is anchored to the screen top — never affected by NPC data.
///
/// FEATURES:
///   • Auto-builds its own UI hierarchy — no prefab required.
///   • When a prefab exists (Tools > Create Speech Bubble Prefab), it is adopted.
///   • Displays DialogueLine speaker name and text.
///   • 🔊 button replays TTS.  Speed slider (0.5–2.0) adjusts rate in real time.
///
/// HIERARCHY (auto-created):
///   SpeechBubble [this, Canvas — ScreenSpaceOverlay]
///     └── BubblePanel   [Image, anchored top-center]
///           ├── SpeakerLabel  [TMP]
///           ├── BodyText      [TMP]
///           └── ControlsRow
///                 ├── ReplayButton  [Button "🔊"]
///                 ├── SpeedSlider   [Slider 0.5–2.0]
///                 └── SpeedLabel    [TMP]
/// </summary>
[RequireComponent(typeof(Canvas))]
public class SpeechBubbleUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Panel Size (screen pixels)")]
    [Tooltip("Width of the bubble panel in screen pixels.")]
    [SerializeField] private float panelWidth  = 700f;
    [Tooltip("Height of the bubble panel in screen pixels.")]
    [SerializeField] private float panelHeight = 220f;
    [Tooltip("Gap from the top edge of the screen in pixels.")]
    [SerializeField] private float topMargin   = 60f;

    [Header("Appearance")]
    [SerializeField] private Color panelColor   = new Color(0.08f, 0.08f, 0.14f, 0.93f);
    [SerializeField] private float cornerRadius = 18f;

    // ── Wired references — auto-found from prefab or assigned in Inspector ────
    [Header("Wired References (auto-found; or assign manually for prefab use)")]
    [SerializeField] private TextMeshProUGUI _speakerLabel;
    [SerializeField] private TextMeshProUGUI _bodyText;
    [SerializeField] private Button          _replayButton;
    [SerializeField] private Slider          _speedSlider;
    [SerializeField] private Button          _exitButton;

    [Header("Hint Button")]
    [Tooltip("Shown after enough wrong attempts. Uses SinusoidalGlowEffect to catch the player's eye.")]
    [SerializeField] private Button          _hintButton;
    private SinusoidalGlowEffect             _hintGlow;

    // ── Events ─────────────────────────────────────────────────────────
    /// <summary>Fired when the user taps the ✕ exit button. Wire this to cancel/end the task.</summary>
    public System.Action OnExitClicked;

    /// <summary>Fired when the user taps the glowing hint button.</summary>
    public System.Action OnHintClicked;

    // ── Internal ───────────────────────────────────────────────────
    private Canvas       _canvas;
    private DialogueLine _currentLine;
    private AudioSource  _fillerAudioSource;
    private bool         _skipTtsForCurrentLine;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _canvas              = GetComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;   // appear above other UI

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // Try to find refs inside a prefab hierarchy before falling back to code-build
        AutoWireExistingHierarchy();

        bool hasPrefab = _speakerLabel != null && _bodyText != null;
        if (!hasPrefab)
        {
            BuildHierarchy();
            Debug.Log("[SpeechBubbleUI] No prefab wiring found — built hierarchy in code. " +
                      "Run Tools > Create Speech Bubble Prefab to design your own bubble.");
        }
        else
        {
            Debug.Log("[SpeechBubbleUI] Using wired/prefab hierarchy.");
        }

        WireListeners();
        gameObject.SetActive(false);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void Show(DialogueLine line)
    {
        _currentLine = line;
        _skipTtsForCurrentLine = false;
        gameObject.SetActive(true);

        if (_speakerLabel != null) _speakerLabel.text = line.speakerName;
        if (_bodyText     != null) _bodyText.text     = line.text;

        SpeakCurrentLine();
    }

    /// <summary>
    /// Shows a hardcoded filler line instantly, playing its pre-recorded <see cref="AudioClip"/>
    /// instead of going through the (slower) live TTS pipeline. Used to mask OpenAI API latency
    /// on a wrong tap or hint request.
    /// </summary>
    public void ShowFiller(FillerEntry filler)
    {
        if (filler == null) return;

        _currentLine = new DialogueLine { speakerName = _speakerLabel != null ? _speakerLabel.text : "", text = filler.text };
        _skipTtsForCurrentLine = true;
        gameObject.SetActive(true);

        if (_bodyText != null) _bodyText.text = filler.text;

        if (filler.clip != null)
        {
            EnsureFillerAudioSource();
            _fillerAudioSource.Stop();
            _fillerAudioSource.clip = filler.clip;
            _fillerAudioSource.Play();
        }
    }

    /// <summary>Shows/hides the glowing hint button. Call after enough wrong attempts.</summary>
    public void ShowHintButton()
    {
        if (_hintButton == null) return;
        _hintButton.gameObject.SetActive(true);
        EnsureHintGlow();
        _hintGlow?.StartGlow();
    }

    public void HideHintButton()
    {
        if (_hintButton == null) return;
        _hintGlow?.StopGlow();
        _hintButton.gameObject.SetActive(false);
    }

    public void Hide() => gameObject.SetActive(false);

    // ── Private helpers ────────────────────────────────────────────────────────

    private void SpeakCurrentLine()
    {
        if (_skipTtsForCurrentLine) return;
        if (!string.IsNullOrWhiteSpace(_currentLine.text))
            TTSManager.Manager.Speak(_currentLine.text);
    }

    private void EnsureFillerAudioSource()
    {
        if (_fillerAudioSource != null) return;
        _fillerAudioSource = gameObject.AddComponent<AudioSource>();
        _fillerAudioSource.playOnAwake = false;
        _fillerAudioSource.spatialBlend = 0f; // 2D — screen-space bubble, not positional
    }

    private void EnsureHintGlow()
    {
        if (_hintGlow != null || _hintButton == null) return;
        _hintGlow = _hintButton.GetComponent<SinusoidalGlowEffect>()
                 ?? _hintButton.gameObject.AddComponent<SinusoidalGlowEffect>();
        _hintGlow.Initialize(_hintButton);
    }

    private void OnReplayClicked() => SpeakCurrentLine();

    private void OnSpeedChanged(float value) => TTSManager.Manager.SetSpeechSpeed(value);

    private void WireListeners()
    {
        if (_replayButton != null)
            _replayButton.onClick.AddListener(OnReplayClicked);

        if (_speedSlider != null)
        {
            _speedSlider.minValue = 0.5f;
            _speedSlider.maxValue = 2.0f;
            if (_speedSlider.value < 0.5f) _speedSlider.value = 1.0f;
            _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }

        if (_exitButton != null)
            _exitButton.onClick.AddListener(() => OnExitClicked?.Invoke());

        if (_hintButton != null)
        {
            _hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
            _hintButton.gameObject.SetActive(false); // hidden until FindObjectTask.ShowHintButton()
        }
    }

    private void AutoWireExistingHierarchy()
    {
        if (_speakerLabel != null && _bodyText != null && _replayButton != null && _speedSlider != null)
            return;

        Transform panel = transform.Find("BubblePanel");
        if (panel == null) return;

        if (_speakerLabel == null)
        {
            var t = panel.Find("SpeakerLabel");
            if (t != null) _speakerLabel = t.GetComponent<TextMeshProUGUI>();
        }
        if (_bodyText == null)
        {
            var t = panel.Find("BodyText");
            if (t != null) _bodyText = t.GetComponent<TextMeshProUGUI>();
        }
        if (_replayButton == null)
        {
            var t = panel.Find("ControlsRow/ReplayButton");
            if (t != null) _replayButton = t.GetComponent<Button>();
        }
        if (_speedSlider == null)
        {
            var t = panel.Find("ControlsRow/SpeedSlider");
            if (t != null) _speedSlider = t.GetComponent<Slider>();
        }
        if (_exitButton == null)
        {
            var t = panel.Find("ExitButton");
            if (t != null) _exitButton = t.GetComponent<Button>();
        }
        if (_hintButton == null)
        {
            var t = panel.Find("HintButton");
            if (t != null) _hintButton = t.GetComponent<Button>();
        }
    }

    // ── Code-built hierarchy (used when no prefab is assigned) ─────────────────

    private void BuildHierarchy()
    {
        // ── Panel ──────────────────────────────────────────────────────────────
        GameObject panelGO = MakeChild("BubblePanel", transform);
        RectTransform panelRT = panelGO.GetComponent<RectTransform>();

        // Anchor to top-center; grows downward
        panelRT.anchorMin        = new Vector2(0.5f, 1f);
        panelRT.anchorMax        = new Vector2(0.5f, 1f);
        panelRT.pivot            = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0f, -topMargin);
        panelRT.sizeDelta        = new Vector2(panelWidth, panelHeight);

        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = panelColor;

        Material roundedMat = TryLoadRoundedMat();
        if (roundedMat != null) panelImg.material = roundedMat;

        RoundedCorners rc = panelGO.AddComponent<RoundedCorners>();
        rc.cornerRadius = cornerRadius;

        // ── Speaker label ──────────────────────────────────────────────────────
        GameObject speakerGO = MakeChild("SpeakerLabel", panelGO.transform);
        RectTransform speakerRT = speakerGO.GetComponent<RectTransform>();
        speakerRT.anchorMin        = new Vector2(0f, 1f);
        speakerRT.anchorMax        = new Vector2(1f, 1f);
        speakerRT.pivot            = new Vector2(0.5f, 1f);
        speakerRT.anchoredPosition = new Vector2(0f, -10f);
        speakerRT.sizeDelta        = new Vector2(-20f, 34f);

        _speakerLabel           = speakerGO.AddComponent<TextMeshProUGUI>();
        _speakerLabel.fontSize  = 18f;
        _speakerLabel.fontStyle = FontStyles.Bold;
        _speakerLabel.color     = new Color(0.85f, 0.85f, 1f);
        _speakerLabel.alignment = TextAlignmentOptions.Left;
        _speakerLabel.text      = "";

        // ── Body text ──────────────────────────────────────────────────────────
        GameObject bodyGO = MakeChild("BodyText", panelGO.transform);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.offsetMin = new Vector2(14f, 54f);
        bodyRT.offsetMax = new Vector2(-14f, -48f);

        _bodyText                    = bodyGO.AddComponent<TextMeshProUGUI>();
        _bodyText.fontSize           = 16f;
        _bodyText.color              = Color.white;
        _bodyText.alignment          = TextAlignmentOptions.TopLeft;
        _bodyText.enableWordWrapping = true;
        _bodyText.text               = "";

        // ── Exit button (top-right corner of panel) ────────────────────────────
        GameObject exitGO = MakeChild("ExitButton", panelGO.transform);
        RectTransform exitRT = exitGO.GetComponent<RectTransform>();
        exitRT.anchorMin        = new Vector2(1f, 1f);
        exitRT.anchorMax        = new Vector2(1f, 1f);
        exitRT.pivot            = new Vector2(1f, 1f);
        exitRT.anchoredPosition = new Vector2(-8f, -8f);
        exitRT.sizeDelta        = new Vector2(36f, 36f);

        Image exitImg = exitGO.AddComponent<Image>();
        exitImg.color = new Color(0.6f, 0.15f, 0.15f, 0.90f);
        _exitButton = exitGO.AddComponent<Button>();

        GameObject exitLblGO = MakeChild("Label", exitGO.transform);
        RectTransform exitLblRT = exitLblGO.GetComponent<RectTransform>();
        exitLblRT.anchorMin = Vector2.zero;
        exitLblRT.anchorMax = Vector2.one;
        exitLblRT.offsetMin = exitLblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI exitTMP = exitLblGO.AddComponent<TextMeshProUGUI>();
        exitTMP.text      = "\u2715";    // ✕
        exitTMP.fontSize  = 18f;
        exitTMP.fontStyle = FontStyles.Bold;
        exitTMP.color     = Color.white;
        exitTMP.alignment = TextAlignmentOptions.Center;
        // ── Hint button (top-left corner, gold/eye-catching, hidden until unlocked) ───
        GameObject hintGO = MakeChild("HintButton", panelGO.transform);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin        = new Vector2(0f, 1f);
        hintRT.anchorMax        = new Vector2(0f, 1f);
        hintRT.pivot            = new Vector2(0f, 1f);
        hintRT.anchoredPosition = new Vector2(8f, -8f);
        hintRT.sizeDelta        = new Vector2(36f, 36f);

        Image hintImg = hintGO.AddComponent<Image>();
        hintImg.color = new Color(0.95f, 0.75f, 0.15f, 0.95f);
        _hintButton = hintGO.AddComponent<Button>();

        GameObject hintLblGO = MakeChild("Label", hintGO.transform);
        RectTransform hintLblRT = hintLblGO.GetComponent<RectTransform>();
        hintLblRT.anchorMin = Vector2.zero;
        hintLblRT.anchorMax = Vector2.one;
        hintLblRT.offsetMin = hintLblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI hintTMP = hintLblGO.AddComponent<TextMeshProUGUI>();
        hintTMP.text      = "\uD83D\uDCA1";  // 💡
        hintTMP.fontSize  = 18f;
        hintTMP.alignment = TextAlignmentOptions.Center;
        // ── Controls row ───────────────────────────────────────────────────────
        GameObject rowGO = MakeChild("ControlsRow", panelGO.transform);
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
        GameObject replayGO = MakeChild("ReplayButton", rowGO.transform);
        replayGO.GetComponent<RectTransform>().sizeDelta = new Vector2(42f, 42f);
        Image replayImg = replayGO.AddComponent<Image>();
        replayImg.color = new Color(0.3f, 0.3f, 0.5f, 1f);
        _replayButton = replayGO.AddComponent<Button>();

        GameObject iconGO = MakeChild("Icon", replayGO.transform);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        TextMeshProUGUI iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text      = "\uD83D\uDD0A";
        iconTMP.fontSize  = 20f;
        iconTMP.alignment = TextAlignmentOptions.Center;

        // Speed slider
        GameObject sliderGO = MakeChild("SpeedSlider", rowGO.transform);
        sliderGO.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 26f);
        _speedSlider = BuildSlider(sliderGO);

        // Speed label
        GameObject spdLblGO = MakeChild("SpeedLabel", rowGO.transform);
        spdLblGO.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 26f);
        TextMeshProUGUI spdTMP = spdLblGO.AddComponent<TextMeshProUGUI>();
        spdTMP.text      = "Speed";
        spdTMP.fontSize  = 13f;
        spdTMP.color     = new Color(0.7f, 0.7f, 0.7f);
        spdTMP.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static Slider BuildSlider(GameObject parent)
    {
        Slider slider = parent.AddComponent<Slider>();

        // Background
        GameObject bgGO = MakeChild("Background", parent.transform);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);

        // Fill area
        GameObject fillAreaGO = MakeChild("FillArea", parent.transform);
        RectTransform fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5f, 0f);
        fillAreaRT.offsetMax = new Vector2(-15f, 0f);

        GameObject fillGO = MakeChild("Fill", fillAreaGO.transform);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.up;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        fillGO.AddComponent<Image>().color = new Color(0.35f, 0.6f, 1f);

        // Handle area
        GameObject handleAreaGO = MakeChild("HandleSlideArea", parent.transform);
        RectTransform handleAreaRT = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10f, 0f);
        handleAreaRT.offsetMax = new Vector2(-10f, 0f);

        GameObject handleGO = MakeChild("Handle", handleAreaGO.transform);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(18f, 18f);
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
