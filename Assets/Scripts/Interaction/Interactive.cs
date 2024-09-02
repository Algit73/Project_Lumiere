using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public abstract class Interactive : MonoBehaviour, IClickable
{
    [SerializeField] private bool autoIconPos;
    [SerializeField] private Vector3 iconPos;
    [SerializeField] private bool autoCameraOffset;
    [SerializeField] private Vector3 objectPosAgainstCamera;
    [SerializeField] private KeyAndValue[] data = new KeyAndValue[]
    {
        new KeyAndValue { Key = "type", Value = "" },
        new KeyAndValue { Key = "color",Value = "" },
        new KeyAndValue { Key ="x_rotation", Value ="0" },
        new KeyAndValue { Key ="y_rotation", Value ="0" },
        new KeyAndValue { Key ="z_rotation", Value ="0" },
        new KeyAndValue { Key ="scale_change_x", Value ="0" },
        new KeyAndValue { Key ="scale_change_y", Value ="0" },
        new KeyAndValue { Key ="scale_change_z", Value ="0" },
        new KeyAndValue { Key ="position_change_x", Value ="0" },
        new KeyAndValue { Key ="position_change_y", Value ="0" },
        new KeyAndValue { Key ="position_change_z", Value ="0" }
    };
    

    [SerializeField] private Collider _col;
    private Transform _iconTr;
    private Vector3 _initialPos;
    private Vector3 _initialRot;
    private Vector3 _initialScale;
    private bool _isForCard;
    private WaitWhile _wait;

    protected Vector3 DataFloatValues;
    protected string DataStringValue;
    protected bool IsWorking;

    public Dictionary<string, string> Meta { private set; get; }
    public Vector3 ObjectPosAgainstCamera => objectPosAgainstCamera;

    [System.Serializable]
    private struct KeyAndValue 
    { 
        public string Key;
        public string Value;
    }

    // Method called when the object is destroyed
    private void OnDestroy() => CommandsCenter.Manager.RemoveInteractives(name);

    private void Awake()
    {
        // Check if the object is a child of card
        if (transform.parent == null) _isForCard = false;
        else _isForCard = transform.parent.CompareTag("UICam");

        if (_isForCard) return;

        Meta = new Dictionary<string, string>();



        foreach (KeyAndValue item in data)
            Meta.Add(item.Key, item.Value);

        // Get the collider component of the object
        if (_col == null)
            _col = GetComponent<Collider>();

        if (autoIconPos) SetIconPos();
        if (autoCameraOffset) SetCameraOffset();

        // Set the object layer to Clickable
        gameObject.layer = LayerMask.NameToLayer("Clickable");

        // Instantiate the icon prefab for this interactive object
        _iconTr = Instantiate(UIManager.Manager.IconPrefab, transform).transform;
        _iconTr.gameObject.SetActive(true);
        _wait = new WaitWhile(() => IsWorking);
    }

    public virtual void Start()
    {
        // Add this object to the list of interactives
        CommandsCenter.Manager.AddInteractives(this);

        // Save the initial position, rotation, and scale of the object
        _initialPos = transform.position;
        _initialRot = transform.eulerAngles;
        _initialScale = transform.localScale;
    }

    private void Update()
    {
        // Skip the update if the object is a child of card
        if (_isForCard) return;

        // Update the position and rotation of the icon related to this object
        _iconTr.localPosition = iconPos;
        _iconTr.LookAt(Camera.main.transform);
    }

    private void SetIconPos()
    {
        iconPos.y = _col.bounds.size.y * 1.3f;
    }
    private void SetCameraOffset()
    {
        float scale = _col.bounds.size.y / _col.bounds.size.x;
        float offsetY = _col.bounds.min.y - _col.bounds.center.y;
        float offsetZ = scale > 1f ? 1.5f * scale + 3.5f : 5f;

        //objectPosAgainstCamera.x = 0f;
        //objectPosAgainstCamera.y = -0.5f * (1f + scale);
        //objectPosAgainstCamera.z = 1.5f * scale + 3.5f;

        objectPosAgainstCamera.x = 0f;
        objectPosAgainstCamera.y = offsetY;
        objectPosAgainstCamera.z = offsetZ;
    }

    // Coroutine method for interacting with the object
    public IEnumerator Interact(InteractionData data)
    {
        // get needed data for the interaction
        DataStringValue = data.DataStringValue;
        DataFloatValues = new Vector3
        {
            x = data.DataFloatValues[0],
            y = data.DataFloatValues[1],
            z = data.DataFloatValues[2]
        };

        IsWorking = true;

        // Invoke the action specified in the data
        Invoke(data.ActionName, 0f);

        // Wait until IsWorking becomes false and the work of this object gets done
        yield return _wait;
    }

    private void Move()
    {
        transform.position = DataFloatValues;
        IsWorking = false;
    }
    private void Rotate()
    {
        transform.rotation = Quaternion.Euler(DataFloatValues);
        IsWorking = false;
    }
    private void Scale()
    {
        transform.localScale = DataFloatValues;
        IsWorking = false;
    }

    // Set the object back to its initial position, rotation, and scale
    private void ResetTransform()
    {
        transform.position = _initialPos;
        transform.eulerAngles = _initialRot;
        transform.localScale = _initialScale;
    }

    public abstract void ResetObject();

    // Virtual method for a specific action in each object that can be called easily without knowing the action name
    public virtual void Action() => IsWorking = false;
    public virtual void UseMeta() { }

    // this method is for IClickable that will use to understand what should happen when we click on this object
    public void OnClick()
    {
        if (!CardHandler.CardIsOpen || Prohibited_Objects.names.Contains(name)) Action();
        else
        {
            InteractionData data = new InteractionData();

            // check if the name is not in the list named prohibited_objs and if it was, return nothing
            // if (Prohibited_Objects.names.Contains(name)) return;
            data.DataStringValue = name;
            // write a code that if the name was in a list, the next line will be neglected
            CommandsCenter.Manager.AddObjectToCard(data);
        }
    }
    public string GetMeta(string key)
    {
        if (Meta.ContainsKey(key)) return Meta[key];
        else return null;
    }
}