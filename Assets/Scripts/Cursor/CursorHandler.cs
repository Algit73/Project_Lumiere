using UnityEngine;

// this class will handle cursor actions like which object is currently being hover or which one we will click on

public class CursorHandler : MonoBehaviour
{
    [SerializeField] private LayerMask clickableMask;

    private Camera _mainCam;
    private Transform _objectTr;
    private IClickable _clickable;
    private bool _isFound;

    private void Awake() => _mainCam = Camera.main;
    private void Update()
    {
        Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, clickableMask, QueryTriggerInteraction.Ignore))
        {
            bool newObjectIsFound = _objectTr == null || (_objectTr != null && _objectTr != hit.transform);

            if (newObjectIsFound)
            {
                _isFound = true;
                _objectTr = hit.transform;
                _clickable = _objectTr.GetComponent<IClickable>();
            }
        }
        else if (Physics.Raycast(ray, out hit, 1000f))
        {
            _isFound = false;
            _objectTr = hit.transform;
        }
        else
        {
            _isFound = false;
            _objectTr = null;
        }

        if (_isFound && Input.GetMouseButtonDown(0)) 
            _clickable.OnClick();
    }
}