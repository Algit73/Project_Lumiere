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

    [Header("Obstacle Avoidance")]
    [Tooltip("If true, the NPC will probe ahead and temporarily steer around blocking colliders.")]
    public bool avoidObstacles = true;

    [Tooltip("Layers treated as blocking obstacles during route movement.")]
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;

    [Tooltip("How far ahead to probe for blocking colliders.")]
    public float obstacleCheckDistance = 0.9f;

    [Tooltip("How far to the side the NPC probes before choosing a detour.")]
    public float obstacleSideOffset = 0.35f;

    [Tooltip("How far the NPC rotates its desired direction when trying left/right detours.")]
    [Range(10f, 85f)]
    public float avoidanceTurnAngle = 40f;

    [Tooltip("How long the NPC keeps a chosen avoidance direction before re-evaluating.")]
    public float avoidanceHoldTime = 0.35f;

    [Tooltip("If the target is far off the current facing direction, rotate first instead of walking in a wide arc.")]
    [Range(0f, 180f)]
    public float turnInPlaceAngle = 35f;

    [Tooltip("How often to check whether the NPC is making progress toward its current waypoint.")]
    public float stuckCheckInterval = 0.75f;

    [Tooltip("Minimum movement required during a stuck check before the NPC tries to recover.")]
    public float stuckDistanceThreshold = 0.12f;

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
    private CharacterController _controller;

    // The computed sub-path for the current trip (subset of waypoints in order)
    private readonly List<Transform> _currentRoute = new List<Transform>();
    private int   _routeIndex;   // which point in _currentRoute we're heading to
    private bool  _waiting;
    private float _waitTimer;
    private Vector3 _avoidDirection;
    private float _avoidTimer;
    private Vector3 _lastProgressPosition;
    private float _stuckTimer;

    // External pause flag (set via Pause() / Resume() for dialogue/cutscene use)
    private bool _paused;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _mover          = GetComponent<CharacterMover>();
        _characterAgent = GetComponent<CharacterAgent>();
        _controller     = GetComponent<CharacterController>();
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
        _lastProgressPosition = transform.position;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Externally pauses NPC movement (e.g. during dialogue or camera focus).
    /// The NPC will stand idle until Resume() is called.
    /// </summary>
    public void Pause()  { _paused = true;  SendIdle(); }

    /// <summary>Resumes movement after a Pause() call.</summary>
    public void Resume() { _paused = false; }

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Update()
    {
        _avoidTimer = Mathf.Max(0f, _avoidTimer - Time.deltaTime);

        if (_currentRoute.Count == 0) return;

        // ── External pause (dialogue / cutscene) ──────────────────────────────
        if (_paused)
        {
            SendIdle();
            return;
        }

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
        bool noJump = false;

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

        Vector3 moveDirection = ResolveMoveDirection(toTarget.normalized);
        float turnAngle = Vector3.Angle(transform.forward, moveDirection);
        if (turnAngle > turnInPlaceAngle)
        {
            TrackProgress(false);
            Vector2 zeroAxis = Vector2.zero;
            Vector3 turnLookTarget = transform.position + new Vector3(moveDirection.x, 0f, moveDirection.z);
            _mover.SetInput(in zeroAxis, in turnLookTarget, in runBetweenWaypoints, in noJump);
            return;
        }

        Vector3 localDir   = transform.InverseTransformDirection(moveDirection);
        Vector2 axis       = new Vector2(localDir.x, localDir.z);
        Vector3 lookTarget = transform.position + new Vector3(moveDirection.x, 0f, moveDirection.z);

        _mover.SetInput(in axis, in lookTarget, in runBetweenWaypoints, in noJump);
        TrackProgress(true);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void PickNewRoute()
    {
        int count = waypoints.Count;

        int startIdx = FindNearestWaypointIndex();
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
        _avoidDirection = Vector3.zero;
        _avoidTimer = 0f;
        _stuckTimer = 0f;
        _lastProgressPosition = transform.position;

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
        _avoidDirection = Vector3.zero;
        _avoidTimer = 0f;
        _stuckTimer = 0f;
        _lastProgressPosition = transform.position;
        Vector3 fwd = ForwardLookTarget();
        _mover.SetInput(in zero, in fwd, in no, in no);
    }

    private int FindNearestWaypointIndex()
    {
        int nearestIndex = 0;
        float bestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform waypoint = waypoints[i];
            if (waypoint == null)
                continue;

            Vector3 delta = waypoint.position - transform.position;
            delta.y = 0f;
            float sqrDistance = delta.sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void TrackProgress(bool isTryingToMove)
    {
        if (!isTryingToMove)
        {
            _stuckTimer = 0f;
            _lastProgressPosition = transform.position;
            return;
        }

        _stuckTimer += Time.deltaTime;
        if (_stuckTimer < stuckCheckInterval)
            return;

        float moved = Vector3.Distance(transform.position, _lastProgressPosition);
        _lastProgressPosition = transform.position;
        _stuckTimer = 0f;

        if (moved >= stuckDistanceThreshold)
            return;

        RecoverFromStuck();
    }

    private void RecoverFromStuck()
    {
        _avoidDirection = Vector3.zero;
        _avoidTimer = 0f;

        if (_routeIndex < _currentRoute.Count - 1)
        {
            AdvanceRouteIndex();
            _waiting = false;
        }
        else
        {
            PickNewRoute();
        }

        _lastProgressPosition = transform.position;
        _stuckTimer = 0f;
        Debug.Log($"[NPCRouteWander] '{name}' was stuck and switched to a new route target.", this);
    }

    private Vector3 ResolveMoveDirection(Vector3 desiredDirection)
    {
        if (!avoidObstacles || desiredDirection.sqrMagnitude <= Mathf.Epsilon)
            return desiredDirection;

        if (_avoidTimer > 0f && _avoidDirection.sqrMagnitude > Mathf.Epsilon)
        {
            if (!TryGetBlockingHit(_avoidDirection, obstacleCheckDistance * 0.75f, out _))
                return _avoidDirection;

            _avoidDirection = Vector3.zero;
            _avoidTimer = 0f;
        }

        if (!TryGetBlockingHit(desiredDirection, obstacleCheckDistance, out var hit))
            return desiredDirection;

        _avoidDirection = ComputeAvoidanceDirection(desiredDirection, hit.normal);

        if (TryGetBlockingHit(_avoidDirection, obstacleCheckDistance * 0.85f, out _))
        {
            Vector3 left = Quaternion.AngleAxis(-avoidanceTurnAngle, Vector3.up) * desiredDirection;
            Vector3 right = Quaternion.AngleAxis(avoidanceTurnAngle, Vector3.up) * desiredDirection;

            bool leftBlocked = TryGetBlockingHit(left, obstacleCheckDistance, out _);
            bool rightBlocked = TryGetBlockingHit(right, obstacleCheckDistance, out _);

            if (!leftBlocked || !rightBlocked)
                _avoidDirection = (!leftBlocked ? left : right).normalized;
        }

        _avoidTimer = avoidanceHoldTime;
        return _avoidDirection;
    }

    private Vector3 ComputeAvoidanceDirection(Vector3 desiredDirection, Vector3 obstacleNormal)
    {
        obstacleNormal.y = 0f;
        if (obstacleNormal.sqrMagnitude <= Mathf.Epsilon)
            return Vector3.Cross(Vector3.up, desiredDirection).normalized;

        obstacleNormal.Normalize();

        Vector3 slide = Vector3.ProjectOnPlane(desiredDirection, obstacleNormal);
        slide.y = 0f;
        if (slide.sqrMagnitude > Mathf.Epsilon)
            return slide.normalized;

        Vector3 tangentA = Vector3.Cross(Vector3.up, obstacleNormal).normalized;
        Vector3 tangentB = -tangentA;
        return Vector3.Dot(tangentA, desiredDirection) >= Vector3.Dot(tangentB, desiredDirection)
            ? tangentA
            : tangentB;
    }

    private bool TryGetBlockingHit(Vector3 direction, float distance, out RaycastHit blockingHit)
    {
        blockingHit = default;

        if (direction.sqrMagnitude <= Mathf.Epsilon || distance <= 0f)
            return false;

        direction = direction.normalized;

        float probeHeight = ProbeHeight();
        Vector3 origin = transform.position + Vector3.up * probeHeight;
        float castRadius = ProbeRadius();

        if (!Physics.SphereCast(origin, castRadius, direction, out RaycastHit hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.transform == null || hit.transform.IsChildOf(transform))
            return false;

        blockingHit = hit;
        return true;
    }

    private float ProbeHeight()
    {
        if (_controller == null)
            return 0.9f;

        float halfHeight = Mathf.Max(_controller.height * 0.5f, _controller.radius);
        return _controller.center.y + halfHeight * 0.5f;
    }

    private float ProbeSideOffset()
    {
        if (_controller == null)
            return Mathf.Max(0.1f, obstacleSideOffset);

        return Mathf.Max(_controller.radius * 0.8f, obstacleSideOffset);
    }

    private float ProbeRadius()
    {
        if (_controller == null)
            return Mathf.Max(0.15f, obstacleSideOffset * 0.5f);

        return Mathf.Max(_controller.radius * 0.75f, obstacleSideOffset * 0.5f);
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
