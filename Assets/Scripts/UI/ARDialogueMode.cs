using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_AR_FOUNDATION
using UnityEngine.XR.ARFoundation;
#endif
#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
#endif

/// <summary>
/// Manages the transition between live AR mode and a "pinned 3D" dialogue mode
/// used when the camera moves to face an NPC for a conversation.
///
/// APPROACH — "Pin" (recommended over a full AR↔3D scene switch):
///   • Disables ARCameraBackground  → the phone camera feed disappears, showing the 3D scene.
///   • Disables TrackedPoseDriver   → the camera stops tracking head movement; we now own it.
///   • Tweens Camera.main to a target pose using a smooth curve.
///   • To return: fade out → snap camera back to remembered pose → re-enable both components → fade in.
///
/// A fullscreen dark overlay (ScreenFader) is used to mask the hard position snap moments.
///
/// SETUP:
///   Add this component to any persistent GameObject (e.g. the one holding Lumier_MainController).
///   It will auto-find the AR components on Camera.main at runtime.
///   No Inspector fields required. The component self-configures.
/// </summary>
public class ARDialogueMode : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ARDialogueMode Instance { get; private set; }

    // ── Inspector settings ────────────────────────────────────────────────────
    [Header("Transition")]
    [Tooltip("Duration of the screen fade to black (seconds).")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Tooltip("Duration of the camera tween from its current position to the target (seconds).")]
    [SerializeField] private float moveDuration = 0.8f;

    [Tooltip("Fade overlay color (usually black).")]
    [SerializeField] private Color fadeColor = Color.black;

    // ── State ─────────────────────────────────────────────────────────────────
    private bool         _inDialogueMode;
    private Pose         _savedPose;          // camera world pose before entering dialogue mode
    private Canvas       _faderCanvas;
    private CanvasGroup  _faderGroup;
    private Coroutine    _activeCoroutine;

    // Cached AR components (found once on first Enter)
    private Behaviour _arCameraBackground;
    private Behaviour _trackedPoseDriver;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildFaderCanvas();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Fades to black, disables AR tracking/background, moves the camera
    /// to <paramref name="targetPosition"/>/<paramref name="targetRotation"/>,
    /// then fades back in.  <paramref name="onComplete"/> fires when the scene
    /// is fully visible at the new pose.
    /// </summary>
    public void EnterDialogueMode(Vector3 targetPosition, Quaternion targetRotation, Action onComplete = null)
    {
        if (_inDialogueMode) return;
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(EnterRoutine(targetPosition, targetRotation, onComplete));
    }

    /// <summary>
    /// Fades to black, re-enables AR tracking/background, restores the camera
    /// to its pre-dialogue pose, then fades back in.
    /// <paramref name="onComplete"/> fires when the AR view is fully visible again.
    /// </summary>
    public void ExitDialogueMode(Action onComplete = null)
    {
        if (!_inDialogueMode) return;
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(ExitRoutine(onComplete));
    }

    public bool IsInDialogueMode => _inDialogueMode;

    // ── Coroutines ─────────────────────────────────────────────────────────────

    private IEnumerator EnterRoutine(Vector3 targetPos, Quaternion targetRot, Action onComplete)
    {
        // 1. Fade to black
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 2. Save current camera pose
        Transform cam = Camera.main.transform;
        _savedPose = new Pose(cam.position, cam.rotation);

        // 3. Disable AR (safe: won't crash if components are absent)
        SetAREnabled(false);
        _inDialogueMode = true;

        // 4. Snap camera to near the start of its tween
        cam.SetPositionAndRotation(targetPos, targetRot);

        // 5. Fade back in
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // 6. (optional) Smooth tween from current to target -- already there after snap
        //    If a slow glide is preferred, swap step 4 for a tween here.
        //    For now we snap under the fade cover, which looks clean on mobile.

        onComplete?.Invoke();
    }

    private IEnumerator ExitRoutine(Action onComplete)
    {
        // 1. Fade to black
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 2. Re-enable AR
        SetAREnabled(true);

        // 3. Snap camera back to saved pose
        Camera.main.transform.SetPositionAndRotation(_savedPose.position, _savedPose.rotation);
        _inDialogueMode = false;

        // 4. Short grace period for AR to settle before revealing
        yield return new WaitForSeconds(0.1f);

        // 5. Fade back in
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        onComplete?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        _faderGroup.alpha = from;
        _faderCanvas.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _faderGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        _faderGroup.alpha = to;
        if (Mathf.Approximately(to, 0f))
            _faderCanvas.gameObject.SetActive(false);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void SetAREnabled(bool enabled)
    {
        if (_arCameraBackground == null || _trackedPoseDriver == null)
            CacheARComponents();

        if (_arCameraBackground != null) _arCameraBackground.enabled = enabled;
        if (_trackedPoseDriver  != null) _trackedPoseDriver.enabled  = enabled;
    }

    private void CacheARComponents()
    {
        if (Camera.main == null) return;
        GameObject camGO = Camera.main.gameObject;

        // ARCameraBackground (AR Foundation)
        _arCameraBackground = camGO.GetComponent("ARCameraBackground") as Behaviour;

        // TrackedPoseDriver — can come from either Input System or legacy XR packages
        _trackedPoseDriver = camGO.GetComponent("TrackedPoseDriver") as Behaviour;

        if (_arCameraBackground == null)
            Debug.LogWarning("[ARDialogueMode] ARCameraBackground not found on Camera.main. " +
                             "AR background will not be hidden during dialogue.");
        if (_trackedPoseDriver == null)
            Debug.LogWarning("[ARDialogueMode] TrackedPoseDriver not found on Camera.main. " +
                             "Camera will not be locked during dialogue.");
    }

    /// <summary>Creates a fullscreen black canvas used for fading.</summary>
    private void BuildFaderCanvas()
    {
        GameObject faderGO = new GameObject("ARDialogueFader");
        faderGO.transform.SetParent(transform, false);

        _faderCanvas             = faderGO.AddComponent<Canvas>();
        _faderCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _faderCanvas.sortingOrder = 9999;     // always on top

        _faderGroup       = faderGO.AddComponent<CanvasGroup>();
        _faderGroup.alpha = 0f;
        _faderGroup.blocksRaycasts = true;
        _faderGroup.interactable   = false;

        // Fullscreen black image
        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(faderGO.transform, false);
        RectTransform rt  = imgGO.AddComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        Image img     = imgGO.AddComponent<Image>();
        img.color     = fadeColor;
        img.raycastTarget = true;

        faderGO.SetActive(false); // hidden until a fade is needed
    }
}
