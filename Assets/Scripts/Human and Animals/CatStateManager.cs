using System.Collections;
using UnityEngine;

public class CatStateManager : Interactive
{
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Points points;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private int walkChance = 30;
    [SerializeField] private int sitChance = 30;
    [SerializeField] private int miauChance = 10;

    public enum State 
    { 
        idle,
        walk,
        sit,
        miau
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
        _currentState = State.idle;
        _delay = new WaitForSeconds(3f);

        StartCoroutine(_update);
    }

    private IEnumerator UpdateState()
    {
        while (true)
        {
            switch (_currentState)
            {
                case State.idle:
                    yield return idleState();
                    break;
                case State.walk:
                    yield return walkState();
                    break;
                case State.sit:
                    yield return sitState();
                    break;
                case State.miau:
                    yield return miauState();
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

        if (chance <= limit) return State.walk;

        limit += sitChance;

        if (chance <= limit) return State.sit;

        limit += miauChance;

        if (chance <= limit) return State.miau;

        return State.idle;
    }
    private IEnumerator idleState()
    {
        animator.Play ("idle", 0, 0.5f);
        yield return _delay;

        _currentState = ChangeState();
    }
    private IEnumerator walkState()
    {
        if (transform.parent != null && !transform.parent.CompareTag("UICam"))
        {
            while (!TryFindPoint(ref _lastPoint))
            {
                yield return null;
                continue;
            }

            // animator.Play("walk", 0, 0.5f);
            animator.SetTrigger("walk");

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
            animator.SetTrigger("walk");

            yield return new WaitForSeconds(2f);
        }

        _currentState = State.idle;
    }
    private IEnumerator sitState()
    {
        animator.SetTrigger("sit");
        // animator.Play("sit", 0, 0.5f);
        yield return _delay;

        _currentState = State.idle;
    }
    private IEnumerator miauState()
    {
        animator.SetTrigger("miau");
        // animator.Play("miau", 0, 0.5f);
        if (audioSource != null) audioSource.Play();
        yield return _delay;
        if (audioSource != null) audioSource.Stop();

        _currentState = State.idle;
    }
    
    private void idle()
    {
        StopCoroutine(_update);
        _isFixState = true;
        _currentState = State.idle;
        StartCoroutine(_update);
        IsWorking = false;
    }
    private void walk()
    {
        StopCoroutine(_update);
        _isFixState = true;
        _currentState = State.walk;
        StartCoroutine(_update);
        IsWorking = false;
    }
    private void sit()
    {
        StopCoroutine(_update);
        _isFixState = true;
        _currentState = State.sit;
        StartCoroutine(_update);
        IsWorking = false;
    }
    private void miau()
    {
        StopCoroutine(_update);
        _isFixState = true;
        _currentState = State.miau;
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
        _currentState = State.idle;
        StartCoroutine(_update);
    }
}