using UnityEngine;
using UnityEngine.AI;
using Controller;

/// <summary>
/// Makes an NPC wander randomly within a radius using Unity's NavMesh for pathfinding
/// and CharacterMover for actual movement/physics/animation.
///
/// NavMeshAgent.updatePosition/updateRotation are disabled — the agent is used purely
/// as a path planner. CharacterMover drives the transform so there is no conflict
/// between NavMeshAgent and CharacterController.
///
/// SETUP:
///   1. Bake the NavMesh: add NavMeshSurface to a scene object → Bake.
///   2. Add NavMeshAgent to the NPC. Set Speed to match CharacterMover walk speed.
///      Set Stopping Distance ~0.4.
///   3. Add this component. CharacterMover must also be present.
///   4. Remove MovePlayerInput from this NPC.
///   5. Set WanderRadius. Optionally assign a WanderCenter to restrict to a room.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterMover))]
public class NPCWander : MonoBehaviour
{
    [Header("Wander Area")]
    [Tooltip("How far from the anchor the NPC may roam (metres).")]
    public float wanderRadius = 8f;

    [Tooltip("Anchor point for the wander area. Leave null to use the NPC's spawn position.")]
    public Transform wanderCenter;

    [Header("Timing")]
    [Tooltip("Seconds to wait after reaching a destination before picking the next one.")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    [Header("Movement")]
    public bool runWhileWandering = false;

    [Tooltip("Distance from destination considered 'arrived'.")]
    public float arrivalThreshold = 0.5f;

    [Header("Stuck Detection")]
    [Tooltip("If the character moves less than this distance over StuckCheckInterval seconds, it's considered stuck.")]
    public float stuckDistanceThreshold = 0.05f;
    [Tooltip("Seconds between stuck checks.")]
    public float stuckCheckInterval = 2f;

    [Header("Task Integration")]
    [Tooltip("Stop wandering while the character has an active Running/Evaluating task.")]
    public bool pauseDuringTask = true;

    // ── Private ────────────────────────────────────────────────────────────────
    private NavMeshAgent   _agent;
    private CharacterMover _mover;
    private CharacterAgent _characterAgent;

    private Vector3 _anchor;
    private float   _waitTimer;
    private bool    _waiting;

    // Stuck detection
    private Vector3 _lastCheckedPosition;
    private float   _stuckTimer;

    private void Awake()
    {
        _agent          = GetComponent<NavMeshAgent>();
        _mover          = GetComponent<CharacterMover>();
        _characterAgent = GetComponent<CharacterAgent>();

        // CRITICAL: disable agent auto-move — used for path calculation only
        _agent.updatePosition   = false;
        _agent.updateRotation   = false;
        _agent.stoppingDistance = arrivalThreshold;

        _anchor = wanderCenter != null ? wanderCenter.position : transform.position;
    }

    private void Start()
    {
        _agent.Warp(transform.position);
        _lastCheckedPosition = transform.position;
        PickNewDestination();
    }

    private void Update()
    {
        // Keep agent position synced to transform (agent doesn't auto-move)
        _agent.nextPosition = transform.position;

        // ── Pause during task ─────────────────────────────────────────────────
        if (pauseDuringTask && IsTaskActive())
        {
            SendIdle();
            return;
        }

        // ── Waiting at waypoint ───────────────────────────────────────────────
        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            SendIdle();
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                PickNewDestination();
            }
            return;
        }

        // ── Path not ready yet ────────────────────────────────────────────────
        if (_agent.pathPending)
        {
            SendIdle();
            return;
        }

        // ── Check arrival ─────────────────────────────────────────────────────
        if (_agent.remainingDistance <= arrivalThreshold)
        {
            _waiting   = true;
            _waitTimer = Random.Range(minWaitTime, maxWaitTime);
            SendIdle();
            return;
        }

        // ── Drive CharacterMover toward next path corner ──────────────────────
        // steeringTarget is the next corner — automatically routes around obstacles
        Vector3 steerTarget = _agent.steeringTarget;
        Vector3 toTarget    = steerTarget - transform.position;
        toTarget.y          = 0f;

        Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);
        Vector2 axis     = new Vector2(localDir.x, localDir.z);

        // Look target at character's own Y to prevent IK tilt
        Vector3 lookTarget = new Vector3(steerTarget.x, transform.position.y, steerTarget.z);

        bool isRun  = runWhileWandering;
        bool isJump = false;
        _mover.SetInput(in axis, in lookTarget, in isRun, in isJump);
        // ── Stuck detection ───────────────────────────────────────────────────────
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer >= stuckCheckInterval)
        {
            float moved = Vector3.Distance(transform.position, _lastCheckedPosition);
            if (moved < stuckDistanceThreshold)
            {
                // Character barely moved — abandon path and pick a new destination
                Debug.Log($"[NPCWander] '{name}' stuck. Picking new destination.");
                PickNewDestination();
            }
            _lastCheckedPosition = transform.position;
            _stuckTimer = 0f;
        }    }

    // ── Public ─────────────────────────────────────────────────────────────────

    public void MoveTo(Vector3 position)
    {
        _waiting = false;
        _agent.SetDestination(position);
    }

    public void SetWanderAnchor(Vector3 newAnchor)
    {
        _anchor = newAnchor;
        PickNewDestination();
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private void PickNewDestination()
    {
        Vector3 candidate = RandomNavMeshPoint(_anchor, wanderRadius);
        _agent.SetDestination(candidate);
        _lastCheckedPosition = transform.position;
        _stuckTimer = 0f;
    }

    private void SendIdle()
    {
        Vector3 lookTarget = transform.position + transform.forward;
        Vector2 zero       = Vector2.zero;
        bool    f          = false;
        _mover.SetInput(in zero, in lookTarget, in f, in f);
    }

    private static Vector3 RandomNavMeshPoint(Vector3 origin, float radius, int maxAttempts = 15)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 circle    = Random.insideUnitCircle * radius;
            Vector3 candidate = origin + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return origin;
    }

    private bool IsTaskActive()
    {
        if (_characterAgent == null) return false;
        TaskInstance task = _characterAgent.CurrentTask;
        return task != null &&
               (task.State == TaskState.Running || task.State == TaskState.Evaluating);
    }

    // Scene view visualisation
    private void OnDrawGizmosSelected()
    {
        Vector3 center = wanderCenter != null ? wanderCenter.position
                       : Application.isPlaying ? _anchor
                       : transform.position;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        // Draw a flat disc approximation
        int segments = 32;
        float step = 360f / segments;
        Vector3 prev = center + new Vector3(wanderRadius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * wanderRadius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
