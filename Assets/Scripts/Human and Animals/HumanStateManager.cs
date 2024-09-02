using System.Collections;
using UnityEngine;

public class HumanStateManager : Interactive
{
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Points points;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private int walkChance = 30;
    [SerializeField] private int jumpChance = 30;
    [SerializeField] private int talkChance = 10;

    public enum State 
    { 
        Idle,
        Walk,
        Jump,
        Talk
    }

    private IEnumerator _update;
    private State _currentState;
    private WaitForSeconds _delay;
    private Transform _lastPoint;
    private bool _isFixState;

    public override void Start()
    {
        _lastPoint = points.SetFirstPoint(name);
        _lastPoint ??= transform;

        base.Start();

        if (transform.parent.CompareTag("UICam")) return;

        transform.position = _lastPoint.position;
        _update = UpdateState();
        _currentState = State.Idle;
        _delay = new WaitForSeconds(3f);

        StartCoroutine(_update);
    }

    private IEnumerator UpdateState()
    {
        while (true)
        {
            switch (_currentState)
            {
                case State.Idle:
                    yield return IdleState();
                    break;
                case State.Walk:
                    yield return WalkState();
                    break;
                case State.Jump:
                    yield return JumpState();
                    break;
                case State.Talk:
                    yield return TalkState();
                    break;
            }

            yield return null;
        }
    }
    private State ChangeState()
    {
        if (_isFixState) return _currentState;

        int chance = Random.Range(0, 101);
        int limit = walkChance;

        if (chance <= limit) return State.Walk;

        limit += jumpChance;

        if (chance <= limit) return State.Jump;

        limit += talkChance;

        if (chance <= limit) return State.Talk;

        return State.Idle;
    }
    private IEnumerator IdleState()
    {
        animator.Play ("Idle", 0, 0.5f);
        yield return _delay;

        _currentState = ChangeState();
    }
    private IEnumerator WalkState()
    {
        if (transform.parent != null && !transform.parent.CompareTag("UICam"))
        {
            while (!TryFindPoint(ref _lastPoint))
            {
                yield return null;
                continue;
            }

            animator.Play("Walk", 0, 0.5f);

            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = _lastPoint.localPosition;
            float timer = 0;
            float dis = Vector3.Distance(startPos, targetPos);

            while (timer < 1f)
            {
                timer += Time.deltaTime * walkSpeed / dis;
                transform.localPosition = Vector3.Lerp(startPos, targetPos, timer);
                transform.LookAt(_lastPoint.position);

                yield return null;
            }
        }
        else
        {
            animator.Play("Walk", 0, 0.5f);

            yield return new WaitForSeconds(2f);
        }

        _currentState = State.Idle;
    }
    private IEnumerator JumpState()
    {
        animator.Play("Jump", 0, 0.5f);
        yield return _delay;

        _currentState = State.Idle;
    }
    private IEnumerator TalkState()
    {
        animator.Play("Talk", 0, 0.5f);
        if (audioSource != null) audioSource.Play();
        yield return _delay;
        if (audioSource != null) audioSource.Stop();

        _currentState = State.Idle;
    }
    
    private void Idle()
    {
        StopCoroutine(_update);
        _isFixState = true;
        _currentState = State.Idle;
        StartCoroutine(_update);
        IsWorking = false;
    }
    private void Walk()
    {
        StopCoroutine(_update);
        _isFixState = true;
        _currentState = State.Walk;
        StartCoroutine(_update);
        IsWorking = false;
    }
    private void Jump()
    {
        StopCoroutine(_update);
        _isFixState = true;
        _currentState = State.Jump;
        StartCoroutine(_update);
        IsWorking = false;
    }
    private void Talk()
    {
        StopCoroutine(_update);
        _isFixState = true;
        _currentState = State.Talk;
        StartCoroutine(_update);
        IsWorking = false;
    }
    private void StartUpdate()
    {
        _isFixState = false;
        IsWorking = false;
    }

    private bool TryFindPoint(ref Transform targetPoint)
    {
        Transform newPoint = points.GetPoint();

        if (newPoint == null || targetPoint == newPoint) return false;

        Vector3 startPos = transform.position + Vector3.up * 0.5f;
        Ray ray = new Ray(startPos, ((newPoint.position + Vector3.up * 0.5f) - startPos).normalized);

        if (Physics.Raycast(ray, 1000f, obstacleLayers, QueryTriggerInteraction.Ignore)) return false;

        targetPoint = newPoint;
        points.SetPoint(name, targetPoint);

        return true;
    }

    public override void Action()
    {
        ResetObject();
        IsWorking = false;
    }

    public override void ResetObject()
    {
        StopCoroutine(_update);
        _currentState = State.Idle;
        StartCoroutine(_update);
    }
}