using System.Collections;
using UnityEngine;

public class Car : Interactive_Anim
{
    [SerializeField] private Transform[] points;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float moveSpeed = 1f;

    private Transform _lastPoint;

    public override void Start()
    {
        base.Start();

        CarMove();
    }

    private void CarMove()
    {
        if (points.Length > 1)
        {
            StopAllCoroutines();
            StartCoroutine(WalkState());
        }

        IsWorking = false;
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

            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = _lastPoint.localPosition;
            float timer = 0;
            float dis = Vector3.Distance(startPos, targetPos);

            while (timer < 1f)
            {
                timer += Time.deltaTime * moveSpeed / dis;
                transform.localPosition = Vector3.Lerp(startPos, targetPos, timer);
                transform.LookAt(_lastPoint.position);

                yield return null;
            }
        }
    }
    private bool TryFindPoint(ref Transform targetPoint)
    {
        Transform newPoint = points[Random.Range(0, points.Length)];

        if (newPoint == null || targetPoint == newPoint) return false;

        Vector3 startPos = transform.position + Vector3.up * 0.5f;
        Ray ray = new Ray(startPos, ((newPoint.position + Vector3.up * 0.5f) - startPos).normalized);

        if (Physics.Raycast(ray, 1000f, obstacleLayers, QueryTriggerInteraction.Ignore)) return false;

        targetPoint = newPoint;

        return true;
    }

    public override void ResetObject()
    {
        base.ResetObject();


    }
}