using UnityEngine;

[CreateAssetMenu(fileName = "InteractiveData", menuName = "Interaction/Interactive Data")]
public class InteractiveDataSO : ScriptableObject
{
    public string type;
    public string description;
    public string objectName;
    public string color;
}
