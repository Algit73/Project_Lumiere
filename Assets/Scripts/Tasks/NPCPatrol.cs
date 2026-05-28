using System.Collections.Generic;
using UnityEngine;
using Controller;

/// <summary>
/// Drives a CharacterMover along a list of waypoints automatically.
/// Add this alongside CharacterMover (and CharacterAgent if the NPC receives tasks).
///
/// The NPC pauses walking while a task is Running/Evaluating so it stands still
/// and faces the player during interaction.
/// </summary>
[RequireComponent(typeof(CharacterMover))]
public class NPCPatrol : MonoBehaviour
{
    public enum LoopMode { PingPong, Loop, Once }

    [Header("Waypoints")]
    [Tooltip("World-space positions to walk between. Add an empty child for each point " +
             "and drag them here, or just set positions manually.")]
    public List<Transform> waypoints = new List<Transform>();

    [Tooltip("How to handle the end of the path.")]
    public LoopMode loopMode = LoopMode.PingPong;

    [Header("Movement")]
    [Tooltip("Walk toward waypoints (false) or run (true).")]
    public bool runBetweenWaypoints = false;

    [Tooltip("Distance from a waypoint at which the NPC considers it reached.")]
    public float arrivalThreshold = 0.4f;

    [Header("Obstacle Avoidance")]
    [Tooltip("If true, the NPC will probe ahead and temporarily steer around blocking colliders.")]
    public bool avoidObstacles = true;

    [Tooltip("Layers treated as blocking obstacles during patrol movement.")]
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

    [Tooltip("Seconds to wait at each waypoint before moving on.")]
    public float waitAtWaypoint = 1.5f;

    [Header("Task Integration")]
    [Tooltip("If true, the NPC stops walking while it has a Running or Evaluating task.")]
    public bool pauseDuringTask = true;

    // ── Private state ──────────────────────────────────────────────────────────
    private CharacterMover _mover;
    private CharacterAgent _agent;   // optional — null if no CharacterAgent attached
    private CharacterController _controller;

    private int   _waypointIndex = 0;
    private int   _direction     = 1;          // +1 forward, -1 backward (PingPong)
    private float _waitTimer     = 0f;
    private bool  _waiting       = false;
    private Vector3 _avoidDirection;
    private float _avoidTimer;
    private Vector3 _lastProgressPosition;
    private float _stuckTimer;

    // External pause flag (set via Pause() / Resume() for dialogue/cutscene use)
    private bool _paused;

    private void Awake()
    {
        _mover = GetComponent<CharacterMover>();
        _agent = GetComponent<CharacterAgent>();  // may be null
        _controller = GetComponent<CharacterController>();
        _lastProgressPosition = transform.position;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Externally pauses NPC movement (e.g. during dialogue or camera focus).
    /// The NPC will stand idle until Resume() is called.
    /// </summary>
    public void Pause()  { _paused = true;  SendInputToMover(Vector2.zero, ForwardLookTarget(), false, false); }

    /// <summary>Resumes movement after a Pause() call.</summary>
    public void Resume() { _paused = false; }

    private void Update()
    {
        _avoidTimer = Mathf.Max(0f, _avoidTimer - Time.deltaTime);

        if (waypoints == null || waypoints.Count == 0) return;

        // External pause (dialogue / cutscene)
        if (_paused)
        {
            SendInputToMover(Vector2.zero, ForwardLookTarget(), false, false);
            return;
        }

        // Pause if a task is active and option is on
        if (pauseDuringTask && IsTaskActive())
        {
            SendInputToMover(Vector2.zero, ForwardLookTarget(), false, false);
            return;
        }

        // Wait timer at waypoint
        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            SendInputToMover(Vector2.zero, ForwardLookTarget(), false, false);
            if (_waitTimer <= 0f)
                _waiting = false;
            return;
        }

        // Move toward current waypoint
        Transform target = waypoints[_waypointIndex];
        if (target == null) { AdvanceWaypoint(); return; }

        Vector3 toTarget   = target.position - transform.position;
        toTarget.y         = 0f;            // ignore height difference
        float distance     = toTarget.magnitude;
        bool isJump = false;

        if (distance <= arrivalThreshold)
        {
            // Reached waypoint — keep looking forward, not at world origin
            SendInputToMover(Vector2.zero, ForwardLookTarget(), false, false);
            _waiting   = true;
            _waitTimer = waitAtWaypoint;
            AdvanceWaypoint();
            return;
        }

        // Convert world direction → local axis expected by CharacterMover (Space.Self mode)
        Vector3 moveDirection = ResolveMoveDirection(toTarget.normalized);
        float turnAngle = Vector3.Angle(transform.forward, moveDirection);
        if (turnAngle > turnInPlaceAngle)
        {
            TrackProgress(false);
            Vector2 zeroAxis = Vector2.zero;
            Vector3 turnLookTarget = transform.position + new Vector3(moveDirection.x, 0f, moveDirection.z);
            SendInputToMover(zeroAxis, turnLookTarget, runBetweenWaypoints, isJump);
            return;
        }

        Vector3 localDir = transform.InverseTransformDirection(moveDirection);
        Vector2 axis     = new Vector2(localDir.x, localDir.z);

        // Use character's own Y for the look target so IK doesn't tilt up/down
        Vector3 lookTarget = transform.position + new Vector3(moveDirection.x, 0f, moveDirection.z);

        SendInputToMover(axis, lookTarget, runBetweenWaypoints, isJump);
        TrackProgress(true);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void SendInputToMover(Vector2 axis, Vector3 lookTarget, bool isRun, bool isJump)
    {
        if (axis.sqrMagnitude <= Mathf.Epsilon)
        {
            _avoidDirection = Vector3.zero;
            _avoidTimer = 0f;
            _stuckTimer = 0f;
            _lastProgressPosition = transform.position;
        }

        _mover.SetInput(in axis, in lookTarget, in isRun, in isJump);
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
        _waiting = true;
        _waitTimer = 0.1f;
        AdvanceWaypoint();
        _lastProgressPosition = transform.position;
        _stuckTimer = 0f;
        Debug.Log($"[NPCPatrol] '{name}' was stuck and advanced to the next waypoint.", this);
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

    /// <summary>Returns a look target directly in front of the character at its own height — safe idle target.</summary>
    private Vector3 ForwardLookTarget() =>
        transform.position + transform.forward;

    private void AdvanceWaypoint()
    {
        switch (loopMode)
        {
            case LoopMode.PingPong:
                _waypointIndex += _direction;
                if (_waypointIndex >= waypoints.Count)      { _waypointIndex = waypoints.Count - 2; _direction = -1; }
                else if (_waypointIndex < 0)                { _waypointIndex = 1;                   _direction =  1; }
                break;

            case LoopMode.Loop:
                _waypointIndex = (_waypointIndex + 1) % waypoints.Count;
                break;

            case LoopMode.Once:
                _waypointIndex = Mathf.Min(_waypointIndex + 1, waypoints.Count - 1);
                break;
        }
    }

    private bool IsTaskActive()
    {
        if (_agent == null) return false;
        TaskInstance task = _agent.CurrentTask;
        return task != null &&
               (task.State == TaskState.Running || task.State == TaskState.Evaluating);
    }

    // Draw waypoint path in the Scene view
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] == null || waypoints[i + 1] == null) continue;
            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            Gizmos.DrawWireSphere(waypoints[i].position, 0.15f);
        }
        Gizmos.DrawWireSphere(waypoints[waypoints.Count - 1].position, 0.15f);
    }
}
