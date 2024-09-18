using TMPro;
using UnityEngine;

public class SyncText : MonoBehaviour
{
    public TextMeshProUGUI masterText; // Assign the master TextMeshPro object here in the Inspector
    public TextMeshProUGUI[] linkedTexts; // Add all the TextMeshPro objects you want to sync with the master

    void Update()
    {
        foreach (var textObj in linkedTexts)
        {
            textObj.text = masterText.text; // Sync all text objects with the master's text
        }
    }
}
