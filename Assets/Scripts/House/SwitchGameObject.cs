using UnityEngine;

public class SwitchGameObject : Interactive
{
    [SerializeField] private GameObject object1, object2;

    private bool _object1IsActive;

    public override void Start()
    {
        base.Start();

        if (object1 == null || object2 == null)
        {
            Debug.LogWarning($"[SwitchGameObject] '{name}': object1 or object2 is not assigned in the Inspector.", this);
            return;
        }

        _object1IsActive = object1.activeInHierarchy;
    }

    public override void Action()
    {
        if (object1 == null || object2 == null) { IsWorking = false; return; }

        object1.SetActive(!object1.activeInHierarchy);
        object2.SetActive(!object2.activeInHierarchy);

        IsWorking = false;
    }

    public override void ResetObject()
    {
        if (object1 == null || object2 == null) return;

        object1.SetActive(_object1IsActive);
        object2.SetActive(!_object1IsActive);
    }
}