using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the full "Find Object" dialogue sequence:
///
///   1. Picks a random eligible NPC (has <see cref="CharacterAgent"/> component).
///   2. Computes an optimal camera pose in front of the NPC:
///        • Gets the NPC's Collider bounds.
///        • If bounds.y / bounds.x > 1 (humanoid), frames the upper half (waist → head).
///        • Otherwise (animal etc.), frames the full bounding box.
///        • Camera distance ensures framed region fills ~65 % of screen height,
///          leaving the top 35 % for the speech bubble.
///        • Also checks width fit and uses whichever constraint requires more distance.
///   3. Pauses the NPC's movement (NPCRouteWander / NPCPatrol / NPCWander).
///   4. Calls <see cref="ARDialogueMode.EnterDialogueMode"/> (fade → snap → fade).
///   5. Shows a world-space <see cref="SpeechBubbleUI"/> above the NPC.
///   6. Shows on-screen ◀ ▶ arrows via <see cref="DialogueNavigationUI"/>.
///   7. Pages through <see cref="FindObjectDialogueData"/> lines on arrow press.
///   8. On last Next press: hides bubble + nav, resumes NPC, exits AR dialogue mode.
///
/// SETUP:
///   • Add this component to the same GameObject as Lumier_MainController
///     (or any persistent scene object).
///   • Optionally assign a <see cref="FindObjectDialogueData"/> asset; if left
///     null, built-in hardcoded fallback lines are used automatically.
///   • ARDialogueMode and DialogueNavigationUI are instantiated automatically
///     on the same GameObject if not already present.
///   • SpeechBubbleUI is instantiated per-NPC at runtime.
///
/// CALLED FROM: Lumier_MainController.HandleTaskSelected(TaskType.Find)
/// </summary>
public class FindObjectTask : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Dialogue Content")]
    [Tooltip("Optional per-character dialogue asset. Leave null to use the built-in fallback lines.")]
    [SerializeField] private FindObjectDialogueData dialogueData;

    [Header("Camera Framing")]
    [Tooltip("Fraction of screen height the framed region should occupy (0.1–0.9). " +
             "The rest is reserved for the speech bubble.")]
    [SerializeField, Range(0.1f, 0.9f)] private float screenFillFraction = 0.60f;

    [Tooltip("Humanoid threshold: bounds.y/bounds.x > this value → focus on upper half.")]
    [SerializeField] private float humanoidRatioThreshold = 1.0f;

    [Tooltip("Minimum camera distance from the NPC (safety clamp).")]
    [SerializeField] private float minCameraDistance = 0.5f;

    [Tooltip("Maximum camera distance from the NPC (safety clamp).")]
    [SerializeField] private float maxCameraDistance = 8f;

    [Header("Dialogue Anchor")]
    [Tooltip("How far above the NPC's collider top edge the bubble appears (world units).")]
    [SerializeField] private float bubbleAboveHead = 0.25f;

    // ── Private state ──────────────────────────────────────────────────────────
    private CharacterAgent          _chosenAgent;
    private IReadOnlyList<DialogueLine> _lines;
    private int                     _lineIndex;
    private SpeechBubbleUI          _bubble;
    private DialogueNavigationUI    _navUI;
    private ARDialogueMode          _arMode;
    private bool                    _active;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-create ARDialogueMode and DialogueNavigationUI on this GameObject
        // so the user doesn't need to wire them separately.
        _arMode = GetComponent<ARDialogueMode>() ?? gameObject.AddComponent<ARDialogueMode>();
        EnsureNavUI();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the Find Object sequence. Call from Lumier_MainController.
    /// Does nothing if a session is already active.
    /// </summary>
    public void Begin()
    {
        if (_active)
        {
            Debug.LogWarning("[FindObjectTask] Already active — ignoring Begin().");
            return;
        }

        // 1. Pick a random NPC
        _chosenAgent = PickRandomNPC();
        if (_chosenAgent == null)
        {
            Debug.LogWarning("[FindObjectTask] No eligible CharacterAgent found in the scene. " +
                             "Add CharacterAgent components to your NPC GameObjects.");
            return;
        }

        _active = true;

        // 2. Prepare dialogue lines
        _lines     = dialogueData != null ? dialogueData.GetLines()
                                          : new FindObjectDialogueData().GetLines();
        _lineIndex = 0;

        // 3. Pause NPC movement
        PauseNPC(_chosenAgent);

        // 4. Compute camera pose from collider bounds
        ComputeCameraPose(_chosenAgent.gameObject,
            out Vector3 camPos, out Quaternion camRot, out Vector3 bubbleAnchorPos);

        // 5. Enter AR dialogue mode (fade + snap + fade)
        _arMode.EnterDialogueMode(camPos, camRot, () =>
        {
            // 6. Create/activate speech bubble
            SpawnBubble(_chosenAgent, bubbleAnchorPos);

            // 7. Show nav arrows
            EnsureNavUI();
            _navUI.OnPrevious = OnPrev;
            _navUI.OnNext     = OnNext;
            _navUI.Show(_lineIndex, _lines.Count);

            // 8. Display first line
            ShowLine(_lineIndex);
        });
    }

    /// <summary>Ends the session externally (e.g. if the user cancels).</summary>
    public void End() => FinishSession();

    // ── Private — sequence steps ───────────────────────────────────────────────

    private void OnPrev()
    {
        if (_lineIndex <= 0) return;
        _lineIndex--;
        ShowLine(_lineIndex);
        _navUI.RefreshButtons(_lineIndex, _lines.Count);
    }

    private void OnNext()
    {
        if (_lineIndex >= _lines.Count - 1)
        {
            // Last line — end session
            FinishSession();
            return;
        }
        _lineIndex++;
        ShowLine(_lineIndex);
        _navUI.RefreshButtons(_lineIndex, _lines.Count);
    }

    private void ShowLine(int index)
    {
        if (_bubble == null) return;
        _bubble.Show(_lines[index]);
    }

    private void FinishSession()
    {
        if (!_active) return;
        _active = false;

        // Hide UI
        _bubble?.Hide();
        _navUI?.Hide();

        // Resume NPC
        if (_chosenAgent != null) ResumeNPC(_chosenAgent);

        // Exit AR dialogue mode
        _arMode?.ExitDialogueMode(() =>
        {
            // Clean up bubble instance
            if (_bubble != null)
            {
                Destroy(_bubble.gameObject);
                _bubble = null;
            }
        });

        _chosenAgent = null;
    }

    // ── Private — NPC selection ────────────────────────────────────────────────

    private static CharacterAgent PickRandomNPC()
    {
#if UNITY_2023_1_OR_NEWER
        CharacterAgent[] all = Object.FindObjectsByType<CharacterAgent>(FindObjectsSortMode.None);
#else
        CharacterAgent[] all = Object.FindObjectsOfType<CharacterAgent>();
#endif
        // Filter: accept all agents, or those explicitly allowing TaskType.Find
        var eligible = new System.Collections.Generic.List<CharacterAgent>();
        foreach (var agent in all)
        {
            if (agent == null || !agent.gameObject.activeInHierarchy) continue;
            if (agent.eligibleTaskTypes == null || agent.eligibleTaskTypes.Count == 0 ||
                agent.eligibleTaskTypes.Contains(TaskType.Find))
            {
                eligible.Add(agent);
            }
        }

        if (eligible.Count == 0) return null;
        return eligible[Random.Range(0, eligible.Count)];
    }

    // ── Private — camera framing ───────────────────────────────────────────────

    /// <summary>
    /// Computes the ideal camera position / rotation to frame the NPC,
    /// and the world position where the speech bubble should be anchored.
    ///
    /// Algorithm:
    ///   1. Get Collider.bounds (falls back to Renderer aggregate bounds).
    ///   2. Compute height/width ratio → humanoid vs animal framing.
    ///   3. Define framing region (focusMin … focusMax in Y).
    ///   4. Camera distance = max(distFromHeight, distFromWidth) so the region
    ///      fits inside the screen with the configured fill fraction.
    ///   5. Camera placed behind the NPC (opposite its forward), looking at the
    ///      focus centre.
    /// </summary>
    private void ComputeCameraPose(
        GameObject npcGO,
        out Vector3 camPos,
        out Quaternion camRot,
        out Vector3 bubblePos)
    {
        Bounds bounds = GetNPCBounds(npcGO);

        float totalHeight = bounds.size.y;
        float totalWidth  = Mathf.Max(bounds.size.x, bounds.size.z);   // use widest dimension

        // Humanoid: height/width > threshold → frame upper half (waist → head)
        bool isHumanoid = (totalHeight > 0.01f) && (totalHeight / Mathf.Max(totalWidth, 0.01f) > humanoidRatioThreshold);

        float frameYMin, frameYMax;
        if (isHumanoid)
        {
            // Upper half: waist to top of head
            float midY  = bounds.min.y + totalHeight * 0.5f;
            frameYMin   = midY;
            frameYMax   = bounds.max.y;
        }
        else
        {
            // Full bounding box
            frameYMin   = bounds.min.y;
            frameYMax   = bounds.max.y;
        }

        float frameHeight  = frameYMax - frameYMin;
        float frameCentreY = (frameYMin + frameYMax) * 0.5f;
        Vector3 focusPt    = new Vector3(bounds.center.x, frameCentreY, bounds.center.z);

        // Camera FoV
        Camera cam = Camera.main;
        float fovY  = cam != null ? cam.fieldOfView : 60f;
        float fovX  = cam != null ? Camera.VerticalToHorizontalFieldOfView(fovY, cam.aspect) : 80f;

        // Distance required so frame fills `screenFillFraction` of screen height
        float halfFrameH  = frameHeight * 0.5f;
        float halfFrameW  = totalWidth  * 0.5f;
        float distFromH   = halfFrameH  / (screenFillFraction * Mathf.Tan(fovY * 0.5f * Mathf.Deg2Rad));
        float distFromW   = halfFrameW  / (Mathf.Tan(fovX * 0.5f * Mathf.Deg2Rad) * 0.9f);  // 90 % width fill
        float distance    = Mathf.Clamp(Mathf.Max(distFromH, distFromW), minCameraDistance, maxCameraDistance);

        // Place camera on the SAME side as the user's current AR viewpoint so we look at
        // the character's FACE, not their back.  Using the vector from the NPC to the current
        // Camera.main position avoids relying on Transform.forward (which varies by model import
        // orientation) and naturally puts the dialogue camera where the user already is.
        Vector3 toCamera = Camera.main != null
            ? Camera.main.transform.position - focusPt
            : npcGO.transform.forward;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.001f) toCamera = npcGO.transform.forward;
        toCamera.Normalize();

        camPos   = focusPt + toCamera * distance;
        camPos.y = focusPt.y;

        camRot = Quaternion.LookRotation(focusPt - camPos, Vector3.up);

        // Bubble anchor: just above the top of the collider
        bubblePos = new Vector3(bounds.center.x,
                                bounds.max.y + bubbleAboveHead,
                                bounds.center.z);
    }

    private static Bounds GetNPCBounds(GameObject go)
    {
        // 1. Prefer Collider bounds
        Collider col = go.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds;

        // 2. Aggregate Renderer bounds
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        // 3. Fallback: a 1×2×1 box at the NPC's feet
        return new Bounds(go.transform.position + Vector3.up, new Vector3(1f, 2f, 1f));
    }

    // ── Private — NPC pause / resume ──────────────────────────────────────────

    private static void PauseNPC(CharacterAgent agent)
    {
        GameObject go = agent.gameObject;
        go.GetComponent<NPCRouteWander>()?.Pause();
        go.GetComponent<NPCPatrol>()?.Pause();
        // NPCWander uses NavMeshAgent; disable the component to stop it
        var wander = go.GetComponent<NPCWander>();
        if (wander != null) wander.enabled = false;
    }

    private static void ResumeNPC(CharacterAgent agent)
    {
        if (agent == null) return;
        GameObject go = agent.gameObject;
        go.GetComponent<NPCRouteWander>()?.Resume();
        go.GetComponent<NPCPatrol>()?.Resume();
        var wander = go.GetComponent<NPCWander>();
        if (wander != null) wander.enabled = true;
    }

    // ── Private — speech bubble ────────────────────────────────────────────────

    private void SpawnBubble(CharacterAgent agent, Vector3 worldPos)
    {
        if (_bubble != null) Destroy(_bubble.gameObject);

        GameObject bubbleGO = new GameObject("SpeechBubble_" + agent.characterID);
        bubbleGO.transform.position = worldPos;

        // Attach to the NPC's iconAnchor (if set) so it moves with the character
        Transform anchor = agent.iconAnchor != null ? agent.iconAnchor : agent.transform;
        bubbleGO.transform.SetParent(anchor, true);

        _bubble = bubbleGO.AddComponent<SpeechBubbleUI>();
    }

    // ── Private — nav UI ──────────────────────────────────────────────────────

    private void EnsureNavUI()
    {
        if (_navUI != null) return;
        GameObject navGO = new GameObject("DialogueNavUI");
        // Keep it at scene root so it's not affected by NPC transforms
        _navUI = navGO.AddComponent<DialogueNavigationUI>();
    }
}
