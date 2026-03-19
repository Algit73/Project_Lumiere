using UnityEngine;

[System.Flags]
public enum InteractableFlags
{
    None          = 0,
    Draggable     = 1 << 0,
    Pickable      = 1 << 1,
    Highlightable = 1 << 2,
}

[CreateAssetMenu(fileName = "InteractiveData", menuName = "Interaction/Interactive Data")]
public class InteractiveDataSO : ScriptableObject
{
    public string category;
    public string description;
    public string objectName;
    public string color;
    public InteractableFlags flags;
}