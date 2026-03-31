using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the _RectSize and _CornerRadius material properties in sync with the
/// RectTransform's actual pixel size, enabling the SDF rounded-corner logic in
/// Custom/RoundedUI and Custom/MaskedUIBlur shaders.
///
/// Creates a per-component material instance automatically so each element can
/// have a different size and corner radius without affecting other objects sharing
/// the same base material.
///
/// USAGE:
///   1. Add this component to any UI Image that uses Custom/RoundedUI or
///      Custom/MaskedUIBlur as its material.
///   2. Adjust 'Corner Radius' in the Inspector (in pixels).
///      0 = square corners.  Large value = pill shape.
///   The component handles everything else at runtime and in the editor.
/// </summary>
[RequireComponent(typeof(Image))]
[ExecuteAlways]
public class RoundedCorners : MonoBehaviour
{
    [Tooltip("Corner radius in pixels. 0 = square corners. Clamped to half the shortest side.")]
    [Range(0f, 300f)]
    public float cornerRadius = 12f;

    // Cached shader property IDs
    private static readonly int RectSizeID     = Shader.PropertyToID("_RectSize");
    private static readonly int CornerRadiusID = Shader.PropertyToID("_CornerRadius");

    private Image         _image;
    private RectTransform _rt;
    private Material      _instance;    // per-component material instance (not serialized → recreated on recompile)

    // Serialized so it survives domain reloads and recompiles.
    // Stores the original assigned material so we always instantiate from the clean source.
    [SerializeField, HideInInspector]
    private Material _baseMaterial;

    // Change-detection cache — avoids redundant SetVector/SetFloat calls every frame
    private Vector2 _cachedSize   = Vector2.negativeInfinity;
    private float   _cachedRadius = float.NegativeInfinity;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        _image = GetComponent<Image>();
        _rt    = GetComponent<RectTransform>();
        if (_image == null) return;

        // First enable: capture the original (shared) material as our base
        if (_baseMaterial == null && _image.material != null)
            _baseMaterial = _image.material;

        // Create or re-create the per-instance material
        if (_baseMaterial != null)
        {
            if (_image.material == _baseMaterial)
            {
                // Still pointing to the shared asset — create a fresh instance
                _instance      = new Material(_baseMaterial);
                _instance.name = _baseMaterial.name + " (Instance)";
                _image.material = _instance;
            }
            else
            {
                // After recompile: image already holds the serialised instance reference
                _instance = _image.material;
            }
        }

        // Force a full refresh on first enable
        _cachedSize   = Vector2.negativeInfinity;
        _cachedRadius = float.NegativeInfinity;
        Refresh();
    }

    private void OnDisable()
    {
        // Drop the live reference; the instance itself stays serialised in the scene
        _instance = null;
    }

    // Fires whenever the RectTransform dimensions change (layout, window resize, etc.)
    private void OnRectTransformDimensionsChange() => Refresh();

    // Fires in the editor when the Inspector value changes
    private void OnValidate()
    {
        // _instance may be null during OnValidate in the editor before OnEnable runs
        if (_instance == null && _image != null)
            _instance = _image.material;
        Refresh();
    }

    // Fallback: catches canvas scaler changes, parent scale changes, etc.
    private void LateUpdate() => Refresh();

    // ── Private ────────────────────────────────────────────────────────────────

    private void Refresh()
    {
        if (_instance == null || _rt == null) return;

        Rect   rect   = _rt.rect;
        float  w      = rect.width;
        float  h      = rect.height;
        float  rad    = cornerRadius;

        // Fast-exit if nothing changed (LateUpdate calls this every frame)
        if (Mathf.Approximately(w,   _cachedSize.x)   &&
            Mathf.Approximately(h,   _cachedSize.y)   &&
            Mathf.Approximately(rad, _cachedRadius))
            return;

        _cachedSize   = new Vector2(w, h);
        _cachedRadius = rad;

        _instance.SetVector(RectSizeID,    new Vector4(w, h, 0f, 0f));
        _instance.SetFloat (CornerRadiusID, rad);
    }
}
