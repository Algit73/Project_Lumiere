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

    [Tooltip("Seconds to wait at each waypoint before moving on.")]
    public float waitAtWaypoint = 1.5f;

    [Header("Task Integration")]
    [Tooltip("If true, the NPC stops walking while it has a Running or Evaluating task.")]
    public bool pauseDuringTask = true;

    // ── Private state ──────────────────────────────────────────────────────────
    private CharacterMover _mover;
    private CharacterAgent _agent;   // optional — null if no CharacterAgent attached

    private int   _waypointIndex = 0;
    private int   _direction     = 1;          // +1 forward, -1 backward (PingPong)
    private float _waitTimer     = 0f;
    private bool  _waiting       = false;

    private void Awake()
    {
        _mover = GetComponent<CharacterMover>();
        _agent = GetComponent<CharacterAgent>();  // may be null
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Count == 0) return;

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
        Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);
        Vector2 axis     = new Vector2(localDir.x, localDir.z);

        // Use character's own Y for the look target so IK doesn't tilt up/down
        Vector3 lookTarget = new Vector3(target.position.x, transform.position.y, target.position.z);

        SendInputToMover(axis, lookTarget, runBetweenWaypoints, false);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void SendInputToMover(Vector2 axis, Vector3 lookTarget, bool isRun, bool isJump)
    {
        _mover.SetInput(in axis, in lookTarget, in isRun, in isJump);
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
