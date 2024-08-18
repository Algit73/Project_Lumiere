using UnityEngine;

public class SwitchGameObject : Interactive
{
    [SerializeField] private GameObject object1, object2;

    private bool _object1IsActive;

    public override void Start()
    {
        base.Start();

        _object1IsActive = object1.activeInHierarchy;
    }

    public override void Action()
    {
        object1.SetActive(!object1.activeInHierarchy);
        object2.SetActive(!object2.activeInHierarchy);

        IsWorking = false;
    }

    public override void ResetObject()
    {
        object1.SetActive(_object1IsActive);
        object2.SetActive(!_object1IsActive);
    }
}