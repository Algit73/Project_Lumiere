using UnityEngine;
using UnityEngine.UI;

public class DynamicSizeAdjuster : MonoBehaviour
{
    public RectTransform holderRect; // The holder/rectangle (UI Panel)

    public float heightFraction = 0.2f; // 1/5th of the screen height
    public float widthFraction = 0.8f;
    
    void Start()
    {
        AdjustHolderSize();
    }

    private void Update() 
    {
        AdjustHolderSize();
    }

    void AdjustHolderSize()
    {
        // Get the screen height
        float screenHeight = Screen.height;
        float screenWidth = Screen.width;

        // Adjust the holder height based on the screen height
        holderRect.sizeDelta = new Vector2(screenWidth*widthFraction, screenHeight * heightFraction);
    }
}
