using System.Collections.Generic;
using UnityEngine;
using Controller;

/// <summary>
/// Moves an NPC along a circular waypoint loop using randomly selected subpaths.
///
/// The waypoints form a single ordered loop: A → B → C → D → E → F → (back to A).
/// On each trip, two random indices are chosen as start and destination.
/// The NPC walks every point in between in forward loop-order, wrapping around
/// when needed.
///
/// Example with 6 points (0=A … 5=F):
///   Start=B(1), End=E(4)  →  route: B → C → D → E
///   Start=F(5), End=C(2)  →  route: F → A → B → C   (wraps)
///
/// SETUP:
///   1. Add this component to the NPC.
///   2. CharacterMover must also be present on the same GameObject.
///   3. Create empty child GameObjects in the scene as waypoint markers,
///      then drag them into the Waypoints list in the Inspector (in route order).
///   4. Remove / disable NPCWander and NavMeshAgent if present.
/// </summary>
[RequireComponent(typeof(CharacterMover))]
public class NPCRouteWander : MonoBehaviour
{
    [Header("Route")]
    [Tooltip("Ordered circular waypoint loop. The NPC will walk through any subsection " +
             "of this loop in order, wrapping around from the last point back to the first.")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("Movement")]
    [Tooltip("Walk (false) or run (true) between waypoints.")]
    public bool runBetweenWaypoints = false;

    [Tooltip("Distance at which a waypoint is considered reached.")]
    public float arrivalThreshold = 0.4f;

    [Header("Waiting")]
    [Tooltip("Brief pause at each intermediate waypoint (seconds).")]
    public float waitAtWaypoint = 0.5f;

    [Tooltip("Minimum pause at the final destination before picking a new route.")]
    public float minWaitAtDestination = 2f;

    [Tooltip("Maximum pause at the final destination before picking a new route.")]
    public float maxWaitAtDestination = 5f;

    [Header("Task Integration")]
    [Tooltip("Pause wandering while a Running or Evaluating task is active.")]
    public bool pauseDuringTask = true;

    // ── Private ────────────────────────────────────────────────────────────────
    private CharacterMover _mover;
    private CharacterAgent _characterAgent;  // optional

    // The computed sub-path for the current trip (subset of waypoints in order)
    private readonly List<Transform> _currentRoute = new List<Transform>();
    private int   _routeIndex;   // which point in _currentRoute we're heading to
    private bool  _waiting;
    private float _waitTimer;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _mover          = GetComponent<CharacterMover>();
        _characterAgent = GetComponent<CharacterAgent>();
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Count < 2)
        {
            Debug.LogWarning($"[NPCRouteWander] '{name}' needs at least 2 waypoints.", this);
            enabled = false;
            return;
        }
        PickNewRoute();
    }

    private void Update()
    {
        if (_currentRoute.Count == 0) return;

        // ── Pause during task ─────────────────────────────────────────────────
        if (pauseDuringTask && IsTaskActive())
        {
            SendIdle();
            return;
        }

        // ── Waiting (at waypoint or destination) ──────────────────────────────
        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            SendIdle();
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                if (_routeIndex >= _currentRoute.Count)
                    PickNewRoute();            // finished route — start a new one
                // else continue to next waypoint (routeIndex already advanced)
            }
            return;
        }

        // ── Move toward current route point ───────────────────────────────────
        Transform target = _currentRoute[_routeIndex];
        if (target == null) { AdvanceRouteIndex(); return; }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= arrivalThreshold)
        {
            bool isDestination = (_routeIndex == _currentRoute.Count - 1);

            AdvanceRouteIndex();

            _waiting   = true;
            _waitTimer = isDestination
                ? Random.Range(minWaitAtDestination, maxWaitAtDestination)
                : waitAtWaypoint;
            return;
        }

        Vector3 localDir   = transform.InverseTransformDirection(toTarget.normalized);
        Vector2 axis       = new Vector2(localDir.x, localDir.z);
        Vector3 lookTarget = new Vector3(target.position.x, transform.position.y, target.position.z);
        bool    noJump     = false;

        _mover.SetInput(in axis, in lookTarget, in runBetweenWaypoints, in noJump);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void PickNewRoute()
    {
        int count = waypoints.Count;

        // Pick a random start and end that are different
        int startIdx = Random.Range(0, count);
        int endIdx;
        do { endIdx = Random.Range(0, count); }
        while (endIdx == startIdx);

        // Build the sub-path: walk forward through the loop from start to end (inclusive)
        _currentRoute.Clear();
        int i = startIdx;
        while (true)
        {
            _currentRoute.Add(waypoints[i]);
            if (i == endIdx) break;
            i = (i + 1) % count;
        }

        _routeIndex = 0;
        _waiting    = false;

        Debug.Log($"[NPCRouteWander] '{name}' new route: " +
                  $"{waypoints[startIdx].name} → {waypoints[endIdx].name} " +
                  $"({_currentRoute.Count} points)");
    }

    private void AdvanceRouteIndex()
    {
        _routeIndex++;
    }

    private void SendIdle()
    {
        Vector2 zero = Vector2.zero;
        bool no = false;
        Vector3 fwd = ForwardLookTarget();
        _mover.SetInput(in zero, in fwd, in no, in no);
    }

    private Vector3 ForwardLookTarget() =>
        transform.position + transform.forward;

    private bool IsTaskActive()
    {
        if (_characterAgent == null) return false;
        TaskInstance task = _characterAgent.CurrentTask;
        return task != null &&
               (task.State == TaskState.Running || task.State == TaskState.Evaluating);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        // Draw the full loop in grey
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            int next = (i + 1) % waypoints.Count;
            if (waypoints[next] == null) continue;
            Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            Gizmos.DrawWireSphere(waypoints[i].position, 0.12f);
        }

        // Draw the active route in yellow (runtime only)
        if (!Application.isPlaying || _currentRoute == null || _currentRoute.Count < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < _currentRoute.Count - 1; i++)
        {
            if (_currentRoute[i] == null || _currentRoute[i + 1] == null) continue;
            Gizmos.DrawLine(_currentRoute[i].position, _currentRoute[i + 1].position);
        }

        // Draw the current target in green
        if (_routeIndex < _currentRoute.Count && _currentRoute[_routeIndex] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_currentRoute[_routeIndex].position, 0.2f);
        }
    }
}
