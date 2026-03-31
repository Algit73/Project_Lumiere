using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Lumiere task-selection menu.
///
/// Hierarchy expected (auto-created by Tools > Build Lumiere Task Menu):
///   Canvas
///     └── LumiereMenuOverlay          ← this component lives here
///           ├── LumiereMenuPanel      ← centered square card
///           │     ├── Title (TMP)
///           │     ├── CloseButton
///           │     └── ButtonGrid      ← GridLayoutGroup  (2 columns)
///           │           ├── Btn_Find
///           │           ├── Btn_Name
///           │           ├── Btn_Story
///           │           ├── Btn_Manipulate
///           │           ├── Btn_Sort
///           │           └── Btn_Match
/// </summary>
public class TaskMenuController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject overlay;     // full-screen dim layer
    [SerializeField] private RectTransform menuPanel; // centered square card

    [Header("Task Buttons (assign in order: Find, Name, Story, Manipulate, Sort, Match)")]
    [SerializeField] private Button btnFind;
    [SerializeField] private Button btnName;
    [SerializeField] private Button btnStory;
    [SerializeField] private Button btnManipulate;
    [SerializeField] private Button btnSort;
    [SerializeField] private Button btnMatch;

    [Header("Close")]
    [SerializeField] private Button btnClose;

    [Header("Animation")]
    [Tooltip("Duration of the scale-in/out animation.")]
    [SerializeField] private float animDuration = 0.18f;

    // ── Events ─────────────────────────────────────────────────────────────────
    /// <summary>Fired when the user selects one of the six task types.</summary>
    public event Action<TaskType> OnTaskSelected;

    /// <summary>Fired when the menu closes (either via close button or task selection).</summary>
    public event Action OnMenuClosed;

    // ── State ──────────────────────────────────────────────────────────────────
    private bool _isOpen;
    private Coroutine _animCoroutine;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Wire buttons
        WireButton(btnFind,       TaskType.Find);
        WireButton(btnName,       TaskType.Name);
        WireButton(btnStory,      TaskType.Story);
        WireButton(btnManipulate, TaskType.Manipulate);
        WireButton(btnSort,       TaskType.Sort);
        WireButton(btnMatch,      TaskType.Match);

        if (btnClose != null)
            btnClose.onClick.AddListener(Close);

        // Start hidden
        if (overlay != null) overlay.SetActive(false);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (overlay != null) overlay.SetActive(true);

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(ScalePanel(Vector3.zero, Vector3.one));
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(ScalePanel(Vector3.one, Vector3.zero, () =>
        {
            if (overlay != null) overlay.SetActive(false);
            OnMenuClosed?.Invoke();
        }));
    }

    public bool IsOpen => _isOpen;

    // ── Private ────────────────────────────────────────────────────────────────

    private void WireButton(Button btn, TaskType type)
    {
        if (btn == null) return;
        btn.onClick.AddListener(() =>
        {
            Close();
            OnTaskSelected?.Invoke(type);
        });
    }

    private IEnumerator ScalePanel(Vector3 from, Vector3 to, Action onComplete = null)
    {
        if (menuPanel == null) { onComplete?.Invoke(); yield break; }

        menuPanel.localScale = from;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            menuPanel.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        menuPanel.localScale = to;
        onComplete?.Invoke();
    }
}
