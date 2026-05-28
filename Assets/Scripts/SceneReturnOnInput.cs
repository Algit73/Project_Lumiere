using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary test helper.
/// When added to a GameObject, shows a "Tap anywhere to return" overlay
/// and calls SceneTransitionManager.ReturnHome() on any touch or click.
///
/// Remove or disable this component when the real task flow is ready.
/// </summary>
public class SceneReturnOnInput : MonoBehaviour
{
    private Canvas     _canvas;
    private CanvasGroup _group;

    private void Awake()
    {
        BuildOverlay();
        EnsureCameraController();
    }

    private static void EnsureCameraController()
    {
        if (Camera.main == null) return;
        // Add gyro/mouse camera controller if nothing is driving the camera
        if (Camera.main.GetComponent<MobileCameraController>() == null)
            Camera.main.gameObject.AddComponent<MobileCameraController>();
    }

    private void Update()
    {
        bool tapped = false;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            tapped = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            tapped = true;
#else
        if (Input.GetMouseButtonDown(0)) tapped = true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) tapped = true;
#endif

        if (tapped)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.ReturnHome();
            else
                Debug.LogWarning("[SceneReturnOnInput] SceneTransitionManager not found.");
        }
    }

    private void BuildOverlay()
    {
        GameObject canvasGO    = new GameObject("ReturnOverlay");
        canvasGO.transform.SetParent(transform, false);
        _canvas                = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder   = 100;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        _group               = canvasGO.AddComponent<CanvasGroup>();
        _group.interactable  = false;
        _group.blocksRaycasts = false;

        // Background strip at bottom
        GameObject bgGO  = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin  = new Vector2(0, 0);
        bgRT.anchorMax  = new Vector2(1, 0);
        bgRT.pivot      = new Vector2(0.5f, 0);
        bgRT.sizeDelta  = new Vector2(0, 80);
        Image bgImg     = bgGO.AddComponent<Image>();
        bgImg.color     = new Color(0, 0, 0, 0.55f);

        // Label
        GameObject txtGO = new GameObject("Label");
        txtGO.transform.SetParent(canvasGO.transform, false);
        RectTransform txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin  = new Vector2(0, 0);
        txtRT.anchorMax  = new Vector2(1, 0);
        txtRT.pivot      = new Vector2(0.5f, 0);
        txtRT.sizeDelta  = new Vector2(0, 80);
        txtRT.anchoredPosition = Vector2.zero;
        Text lbl         = txtGO.AddComponent<Text>();
        lbl.text         = "Tap anywhere to return";
        lbl.alignment    = TextAnchor.MiddleCenter;
        lbl.fontSize     = 32;
        lbl.color        = Color.white;
        lbl.font         = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
