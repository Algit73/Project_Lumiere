using System.Collections;
using UnityEngine;

/// <summary>
/// Placed on the AR content root GameObject in the XR_Home scene.
///
/// On scene load:
///   • If SceneTransitionManager has a saved anchor pose from this session,
///     the content root is immediately repositioned to that pose — the user
///     doesn't need to re-place the scene on the table.
///   • If there is no saved pose, the content stays wherever it was placed
///     (normal first-placement flow).
///
/// On placement / reposition:
///   Call <see cref="NotifyPlaced"/> (wire to your AR placement event) so the
///   new pose is saved to SceneTransitionManager for future returns.
///
/// SETUP:
///   1. Attach this component to the root GameObject that holds your 3D scene
///      content (NPCs, environment, etc.) inside XR_Home.
///   2. In your AR placement script, call ARSceneAnchorController.Instance.NotifyPlaced()
///      after placing/moving the content root.
/// </summary>
public class ARSceneAnchorController : MonoBehaviour
{
    public static ARSceneAnchorController Instance { get; private set; }

    [Tooltip("Seconds to wait for AR tracking to stabilize before restoring the anchor pose.")]
    [SerializeField] private float trackingSettleDelay = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.HasSavedAnchor)
        {
            StartCoroutine(RestoreAfterDelay());
        }
    }

    /// <summary>
    /// Call this whenever the user places or moves the AR content root.
    /// Saves the current pose so it can be restored after a scene switch.
    /// </summary>
    public void NotifyPlaced()
    {
        if (SceneTransitionManager.Instance == null) return;
        SceneTransitionManager.Instance.SaveAnchorPose(
            new Pose(transform.position, transform.rotation));
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private IEnumerator RestoreAfterDelay()
    {
        // Give AR tracking a moment to settle before repositioning
        yield return new WaitForSeconds(trackingSettleDelay);

        // Restore AR content anchor pose
        Pose saved = SceneTransitionManager.Instance.SavedAnchorPose;
        transform.SetPositionAndRotation(saved.position, saved.rotation);
        Debug.Log("[ARSceneAnchorController] Anchor pose restored from previous session.");

        // Restore camera pose.
        // In a full AR scene the TrackedPoseDriver will override this immediately,
        // but it ensures the correct first frame and fully works in non-AR / editor mode.
        if (SceneTransitionManager.Instance.HasSavedCamera && Camera.main != null)
        {
            Pose cam = SceneTransitionManager.Instance.SavedCameraPose;
            Camera.main.transform.SetPositionAndRotation(cam.position, cam.rotation);
            Debug.Log("[ARSceneAnchorController] Camera pose restored.");
        }
    }
}
