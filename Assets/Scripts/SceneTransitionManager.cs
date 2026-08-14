using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent singleton that survives scene loads (DontDestroyOnLoad).
///
/// Responsibilities:
///   • Carries the pending task from the AR/Home scene to the 3D scene.
///   • Remembers the name of the scene to return to after a task finishes.
///   • Stores the world pose of the placed AR anchor so it can be restored
///     when the app returns to the AR scene within the same session.
///
/// USAGE — leaving AR scene:
///   SceneTransitionManager.Instance.GoTo("Farm", TaskType.Find, "XR_Home", anchorPose);
///
/// USAGE — returning from 3D scene:
///   SceneTransitionManager.Instance.ReturnHome();
///
/// USAGE — checking on 3D scene start:
///   if (SceneTransitionManager.Instance.ConsumePendingTask(out TaskType t)) { ... }
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static SceneTransitionManager Instance { get; private set; }

    // ── Persistent state ───────────────────────────────────────────────────────

    /// <summary>Scene to load when ReturnHome() is called.</summary>
    public string ReturnScene { get; private set; } = "XR_Home";

    /// <summary>
    /// Saved world pose of the placed AR content root.
    /// Valid only when <see cref="HasSavedAnchor"/> is true.
    /// </summary>
    public Pose SavedAnchorPose { get; private set; }

    /// <summary>True if an anchor pose has been saved this session.</summary>
    public bool HasSavedAnchor { get; private set; }

    /// <summary>
    /// Camera world pose at the moment <see cref="GoTo"/> was called.
    /// Restored by <see cref="ARSceneAnchorController"/> when the home scene reloads.
    /// </summary>
    public Pose SavedCameraPose { get; private set; }

    /// <summary>True if a camera pose has been captured this session.</summary>
    public bool HasSavedCamera { get; private set; }

    // Pending task — consumed once by the destination scene
    private bool     _hasPendingTask;
    private TaskType _pendingTask;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves state and loads <paramref name="targetScene"/>.
    /// </summary>
    /// <param name="targetScene">Name of the 3D scene to load.</param>
    /// <param name="task">Task the destination scene should auto-start.</param>
    /// <param name="returnScene">Scene name to come back to (usually "XR_Home").</param>
    /// <param name="anchorPose">Current world pose of the placed AR content root.</param>
    /// <param name="hasAnchor">Whether a valid anchor pose is provided.</param>
    public void GoTo(string targetScene, TaskType task, string returnScene,
                     Pose anchorPose = default, bool hasAnchor = false)
    {
        _pendingTask    = task;
        _hasPendingTask = true;
        ReturnScene     = returnScene;

        if (hasAnchor)
        {
            SavedAnchorPose = anchorPose;
            HasSavedAnchor  = true;
        }

        // Auto-capture the current camera pose so it can be restored on return.
        if (Camera.main != null)
        {
            SavedCameraPose = new Pose(Camera.main.transform.position,
                                       Camera.main.transform.rotation);
            HasSavedCamera  = true;
        }

        SceneManager.LoadScene(targetScene);
    }

    /// <summary>
    /// Loads the return scene (AR/Home scene).
    /// </summary>
    public void ReturnHome()
    {
        SceneManager.LoadScene(ReturnScene);
    }

    /// <summary>
    /// Returns true and outputs the pending task the first time it is called
    /// after a <see cref="GoTo"/>. Subsequent calls return false (consumed).
    /// </summary>
    public bool ConsumePendingTask(out TaskType task)
    {
        if (_hasPendingTask)
        {
            task            = _pendingTask;
            _hasPendingTask = false;
            return true;
        }
        task = default;
        return false;
    }

    /// <summary>
    /// Saves (or updates) the AR anchor pose without triggering a scene load.
    /// Call this whenever the user repositions the AR content.
    /// </summary>
    public void SaveAnchorPose(Pose pose)
    {
        SavedAnchorPose = pose;
        HasSavedAnchor  = true;
    }

    /// <summary>Clears the saved anchor (e.g. user explicitly resets placement).</summary>
    public void ClearAnchorPose()
    {
        HasSavedAnchor = false;
    }
}
