using UnityEngine;


// this class will handle cursor actions like which object is currently being hover or which one we will click on

public class CursorHandler : MonoBehaviour
{
    [SerializeField] private LayerMask clickableMask;

    private Camera _mainCam;
    private Transform _objectTr;
    private IClickable _clickable;
    private bool _isFound;
    private GameObject LumiereFindObj;

    private const string LUMIERE_FIND_NAME = "Lumiere_Find";
    private Finding_Items Finding_Items_Component;

    private void Awake() => _mainCam = Camera.main;

    private void Start() 
    {
        LumiereFindObj = GameObject.Find(LUMIERE_FIND_NAME);
        if (LumiereFindObj == null)
        {
            Debug.LogError("CursorHandler: Lumiere_Find object not found in the scene.");
            return;
        }
        Finding_Items_Component = LumiereFindObj.GetComponent<Finding_Items>();
    }
    private void Update()
    {
        Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, clickableMask, QueryTriggerInteraction.Ignore))
        {
            Transform hitTransform = hit.transform;
            bool newObjectIsFound = _objectTr == null || (_objectTr != null && _objectTr != hitTransform);

            if (newObjectIsFound)
            {
                _isFound = true;
                _objectTr = hitTransform;
                _clickable = hitTransform.GetComponent<IClickable>();
                
                // Process the hit object and all its children
                ProcessAllChildren(hitTransform);
            }
        }



        // if (Physics.Raycast(ray, out hit, 1000f, clickableMask, QueryTriggerInteraction.Ignore))
        // {
        //     bool newObjectIsFound = _objectTr == null || (_objectTr != null && _objectTr != hit.transform);

        //     if (newObjectIsFound)
        //     {
        //         _isFound = true;
        //         _objectTr = hit.transform;
        //         _clickable = _objectTr.GetComponent<IClickable>();
        //     }
        // }
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
        { 
            _clickable.OnClick();
            Finding_Items_Component.OnObjectClick();
        
        }
    }

    // private Transform FindRootTransform(Transform hitTransform)
    // {
    // // Find the topmost parent with a specific component or tag
    // // This could be your custom component, or a tag like "SelectableParent"
    // while (hitTransform.parent != null && 
    //        !hitTransform.GetComponent<YourParentIdentifierComponent>() && 
    //        !hitTransform.CompareTag("SelectableParent"))
    //     {
    //     hitTransform = hitTransform.parent;
    //     }
    // return hitTransform;
    // }

    private void ProcessAllChildren(Transform parent)
    {
    // Do something with the parent
        Debug.Log("Processing: " + parent.name);

        // Process all immediate children
        foreach (Transform child in parent)
        {
            // Do something with each child
            Debug.Log("Processing child: " + child.name);

            // Recursively process grandchildren
            ProcessAllChildren(child);
        }
    }

    

    void SelectAllChildren(Transform parent)
    {
        // Select or highlight the parent
        Debug.Log("Selected: " + parent.name);

        // Iterate through all children
        foreach (Transform child in parent)
        {
            // Select or highlight each child
            Debug.Log("Selected child: " + child.name);

            // Recursively select grandchildren if needed
            SelectAllChildren(child);
        }
    }


    

    
}