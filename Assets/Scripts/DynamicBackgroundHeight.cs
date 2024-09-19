using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DynamicBackgroundHeight : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI textMeshPro;   // Reference to the TextMeshPro component
    public RectTransform background;  // Reference to the background (assumed to be a UI element or a 2D object)
    public Image background_glow;  // Reference to the background (assumed to be a UI element or a 2D object)

    [SerializeField] float padding = 10f;
    [SerializeField] float hei = 0f;

    void Update()
    {
        // Check if the necessary components are assigned
        if (textMeshPro == null || background == null || background_glow == null)
        {
            Debug.LogError("Missing references: Please ensure TextMeshPro, Background, and Background Glow are assigned in the Inspector.");
            return;
        }

        // Get the preferred height based on the text content and settings (like word wrapping)
        float textHeight = textMeshPro.preferredHeight;

        // Adjust the TextMeshPro's RectTransform height to match the text height
        RectTransform textRectTransform = textMeshPro.GetComponent<RectTransform>();
        RectTransform bglow_RectTransform = background_glow.rectTransform;  // Access rectTransform directly

        Vector2 text_newSize = textRectTransform.sizeDelta;
        text_newSize.y = textHeight + padding;  // Add some padding if needed
        textRectTransform.sizeDelta = text_newSize;

        // Set the background size based on the text height
        Vector2 newSize = background.sizeDelta;
        newSize.y = textHeight + padding;  // Adjust the height based on the text height
        background.sizeDelta = newSize;
        bglow_RectTransform.sizeDelta = newSize;

        // Adjust the background position based on the text's position
        Vector3 newPosition = background.localPosition;
        newPosition.y = textMeshPro.transform.localPosition.y + hei;  // Align Y position with the text
        background.localPosition = newPosition;
        bglow_RectTransform.localPosition = newPosition;
    }
}
