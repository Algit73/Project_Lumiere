using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a floating icon above an NPC's head in world space.
///
/// LIFECYCLE:
///   Attach this component alongside <see cref="CharacterAgent"/>.
///   The component builds its visual geometry in Start() but starts HIDDEN.
///   Call <see cref="ShowIcon"/> / <see cref="HideIcon"/> from CharacterAgent
///   (or any other script) to control visibility.
///   If this component is absent, CharacterAgent silently skips icon calls.
/// </summary>
public class NPCFloatingIcon : MonoBehaviour
{
    // ── Icon type ──────────────────────────────────────────────────────────────
    public enum IconType { Random = -1, Question = 0, Exclamation = 1, Story = 2 }

    [Header("3D Model (optional)")]
    [Tooltip("Assign a 3D prefab (e.g. yellow_question_box.glb) to show a real model.\n" +
             "When set, the built-in text-badge is replaced by this model.")]
    public GameObject modelPrefab;

    [Tooltip("World-space size of the model in meters. Adjust in Play mode — changes take effect immediately.")]
    [SerializeField] private float modelScale = 0.25f;

    [Tooltip("Euler rotation applied to the model so its front face points toward the camera. " +
             "e.g. (0, 180, 0) if the model appears mirrored/backwards.")]
    [SerializeField] private Vector3 modelRotationOffset = Vector3.zero;

    [Header("Icon (text fallback, used when Model Prefab is empty)")]
    [Tooltip("Which text icon to show. Random picks one at Start.")]
    [SerializeField] public IconType iconType = IconType.Random;

    [Header("Position")]
    [Tooltip("World-space height above iconAnchor (or this transform).")]
    [SerializeField] private float verticalOffset = 0.4f;

    [Header("Appearance")]
    [Tooltip("Diameter of the icon circle in world units (text fallback only).")]
    [SerializeField] private float iconWorldSize = 0.32f;

    [Header("Bob Animation")]
    [SerializeField] private bool  bobEnabled   = true;
    [SerializeField] private float bobSpeed     = 1.5f;
    [SerializeField] private float bobAmplitude = 0.04f;

    [Header("Glow (3D model only)")]
    [Tooltip("Enable sinusoidal emission glow on the model.")]
    [SerializeField] private bool glowEnabled = true;
    [Tooltip("Pulses per second.")]
    [SerializeField] private float glowSpeed = 2f;
    [Tooltip("Emission intensity at the darkest point of the pulse.")]
    [SerializeField] private float glowMinIntensity = 0.0f;
    [Tooltip("Emission intensity at the brightest point of the pulse.")]
    [SerializeField] private float glowMaxIntensity = 2.5f;
    [Tooltip("Base glow colour. Default bright yellow.")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0f);

    [Header("Visibility")]
    [Tooltip("Show the icon immediately on Start without waiting for CharacterAgent.ShowIcon().\n" +
             "Leave false in production — CharacterAgent controls this.")]
    [SerializeField] private bool startVisible = false;

    // ── Internals ──────────────────────────────────────────────────────────────
    private static readonly string[] Labels = { "?", "!", "..." };
    private static readonly Color[]  BgCols =
    {
        new Color(0.20f, 0.50f, 1.00f, 0.95f),   // blue  – question
        new Color(1.00f, 0.38f, 0.12f, 0.95f),   // orange – exclamation
        new Color(0.42f, 0.80f, 1.00f, 0.95f),   // sky   – story / cloud
    };

    private Transform         _iconRoot;
    private TextMeshProUGUI   _label;
    private Image             _bg;
    private int               _resolved;
    private float             _bobPhase;
    private CharacterAgent    _agent;
    private Material[]        _glowMaterials;   // material instances from model children (glow)

    // ── Unity ──────────────────────────────────────────────────────────────────

    // Awake() builds the icon geometry so _iconRoot is ready before any Start()
    // fires — including LumiereSceneDirector.Start() which may call ShowIcon().
    private void Awake()
    {
        _agent    = GetComponent<CharacterAgent>();
        _bobPhase = Random.Range(0f, Mathf.PI * 2f);
        _resolved = (iconType == IconType.Random)
            ? Random.Range(0, Labels.Length)
            : Mathf.Clamp((int)iconType, 0, Labels.Length - 1);

        if (modelPrefab != null)
            BuildModel();
        else
            BuildCanvas();

        // Start hidden — LumiereSceneDirector / CharacterAgent controls visibility.
        _iconRoot.gameObject.SetActive(startVisible);
    }

    private void LateUpdate()
    {
        if (_iconRoot == null) return;

        // Anchor position: prefer explicit iconAnchor; otherwise estimate the top of the character
        Vector3 pivot;
        if (_agent != null && _agent.iconAnchor != null)
        {
            pivot = _agent.iconAnchor.position + Vector3.up * verticalOffset;
        }
        else
        {
            float topY = GetCharacterTopY();
            pivot = new Vector3(transform.position.x, topY, transform.position.z)
                  + Vector3.up * verticalOffset;
        }

        float bob = bobEnabled
            ? Mathf.Sin(Time.time * bobSpeed + _bobPhase) * bobAmplitude
            : 0f;

        _iconRoot.position  = pivot + Vector3.up * bob;
        _iconRoot.localScale = Vector3.one * modelScale;   // live — reflects Inspector changes instantly

        // Sinusoidal emission glow — drive _EmissionColor directly on cached material instances
        if (glowEnabled && _glowMaterials != null)
        {
            float t         = (Mathf.Sin(Time.time * glowSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            Color emission  = glowColor * Mathf.Lerp(glowMinIntensity, glowMaxIntensity, t);
            foreach (var mat in _glowMaterials)
                mat.SetColor("_EmissionColor", emission);
        }

        // Billboard: rotate around Y only — X rotation is always 0
        if (Camera.main != null)
        {
            Vector3 toCamera = Camera.main.transform.position - _iconRoot.position;
            toCamera.y = 0f;   // flatten → pure yaw, X and Z stay 0
            if (toCamera.sqrMagnitude > 0.001f)
                _iconRoot.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
        }
    }

    /// <summary>
    /// Returns the world-space Y of the highest point of this character.
    /// Priority: humanoid head bone → renderer bounds → collider → +1.8 m estimate.
    /// </summary>
    private float GetCharacterTopY()
    {
        // Best: read the head bone directly from the humanoid rig — immune to
        // mis-fitted colliders and skinned-mesh bounds timing issues.
        var anim = GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            Transform head = anim.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
                return head.position.y + 0.15f;  // +15 cm: head bone → skull crown
        }

        // Fallback: union of all renderer world-space bounds
        float maxY = float.MinValue;
        foreach (var r in GetComponentsInChildren<Renderer>())
            if (r.bounds.max.y > maxY) maxY = r.bounds.max.y;

        if (maxY > float.MinValue)
            return maxY;

        // Fallback: collider (may be poorly fitted)
        var col = GetComponentInChildren<Collider>();
        if (col != null)
            return col.bounds.max.y;

        // Last resort
        return transform.position.y + 1.8f;
    }

    private void OnDestroy()
    {
        if (_iconRoot != null)
            Destroy(_iconRoot.gameObject);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Shows the icon.</summary>
    public void ShowIcon() { if (_iconRoot != null) _iconRoot.gameObject.SetActive(true); }

    /// <summary>Hides the icon without destroying it.</summary>
    public void HideIcon() { if (_iconRoot != null) _iconRoot.gameObject.SetActive(false); }

    /// <summary>Switches to a different icon type at runtime.</summary>
    public void SetType(IconType type)
    {
        _resolved = (type == IconType.Random)
            ? Random.Range(0, Labels.Length)
            : Mathf.Clamp((int)type, 0, Labels.Length - 1);
        if (_label != null) _label.text   = Labels[_resolved];
        if (_bg    != null) _bg.color     = BgCols[_resolved];
    }

    // ── 3D model builder ────────────────────────────────────────────────────────

    private void BuildModel()
    {
        // Root drives world size; instance scale is forced to (1,1,1) so that
        // the GLB's import scale (which can be anything: 1, 0.01, etc.) is
        // neutralised. root.localScale = modelScale means the model is exactly
        // modelScale meters tall in world space — easy to reason about.
        var root = new GameObject("NPCIcon_" + gameObject.name);
        root.transform.localScale = Vector3.one * modelScale;

        var instance = Instantiate(modelPrefab, root.transform);
        instance.transform.localPosition = Vector3.zero;
        // Preserve the prefab's own rotation (GLB exporters bake orientation here).
        // modelRotationOffset lets you add a correction on top without touching the asset.
        instance.transform.localRotation = modelPrefab.transform.localRotation
                                         * Quaternion.Euler(modelRotationOffset);
        instance.transform.localScale    = Vector3.one;   // normalise import scale

        // Cache material instances from all child renderers for per-frame emission glow.
        // Using renderer.materials (not sharedMaterials) creates per-instance copies so
        // we never modify the shared asset. EnableKeyword ensures the emission slot is active.
        if (glowEnabled)
        {
            var matList = new System.Collections.Generic.List<Material>();
            foreach (var r in instance.GetComponentsInChildren<Renderer>(includeInactive: true))
                matList.AddRange(r.materials);
            _glowMaterials = matList.ToArray();
            foreach (var mat in _glowMaterials)
                mat.EnableKeyword("_EMISSION");
        }

        _iconRoot = root.transform;
    }

    // ── Canvas builder ─────────────────────────────────────────────────────────

    private void BuildCanvas()
    {
        // ── Root — world-space canvas at scene root (no parent = no scale issues) ──
        var root = new GameObject("NPCIcon_" + gameObject.name);

        Canvas cv = root.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.WorldSpace;
        cv.sortingOrder = 5;

        // The canvas rect is 100 × 100 canvas-units; scale maps them to world units
        float s = iconWorldSize / 100f;
        root.transform.localScale = new Vector3(s, s, s);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 100f);

        // ── Circular background ────────────────────────────────────────────────
        var bgGO = Child("Bg", root.transform, new Vector2(100f, 100f));
        _bg       = bgGO.AddComponent<Image>();
        _bg.color = BgCols[_resolved];

        // ── Label ──────────────────────────────────────────────────────────────
        var lblGO = Child("Label", root.transform, new Vector2(100f, 100f));
        _label                   = lblGO.AddComponent<TextMeshProUGUI>();
        _label.text              = Labels[_resolved];
        _label.fontSize          = 62f;
        _label.fontStyle         = FontStyles.Bold;
        _label.color             = Color.white;
        _label.alignment         = TextAlignmentOptions.Center;
        _label.enableWordWrapping = false;

        _iconRoot = root.transform;
    }

    private static GameObject Child(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        return go;
    }
}
