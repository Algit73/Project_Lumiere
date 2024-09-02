using System.Collections;
using UnityEditor;
using UnityEngine;

public class RotatePivot : Interactive
{
    [SerializeField] private Transform doorTr;
    [SerializeField] private Vector3 targetRot1, targetRot2;
    bool change_state;

    private IEnumerator _rotating;

    public override void Start()
    {
        base.Start();
        _rotating = RotateDoor(targetRot2);
    }

    public override void Action()
    {
        if (change_state == false)
        {
            OpenDoor();
            change_state = true;
        }
        else
        {
            CloseDoor();
            change_state = false;
        }
    }

    private void OpenDoor()
    {
        StopCoroutine(_rotating);
        _rotating = RotateDoor(targetRot2);
        StartCoroutine(_rotating);
    }
    private void CloseDoor()
    {
        StopCoroutine(_rotating);
        _rotating = RotateDoor(targetRot1);
        StartCoroutine(_rotating);
    }

    private IEnumerator RotateDoor(Vector3 targetRot)
    {
        Vector3 startRot = doorTr.localEulerAngles;
        startRot.FixRotForLerp();
        targetRot.FixRotForLerp();
        float timer = 0f;

        while (timer < 1f)
        {
            timer += Time.deltaTime;
            doorTr.localEulerAngles = Vector3.Lerp(startRot, targetRot, timer);

            yield return null;
        }

        IsWorking = false;
    }

    public override void ResetObject() => StopCoroutine(_rotating);
}
