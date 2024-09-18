using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonGlowEffect : MonoBehaviour
{
    public Button myButton;
    public Image buttonImage;
    public Color startColor = Color.white;
    public Color glowColor = Color.yellow;
    public float glowDuration = 0.5f;

    void Start()
    {
        myButton.onClick.AddListener(OnButtonPressed);
    }

    void OnButtonPressed()
    {
        StartCoroutine(GlowEffect());
    }

    IEnumerator GlowEffect()
    {
        float elapsedTime = 0f;
        while (elapsedTime < glowDuration)
        {
            buttonImage.color = Color.Lerp(startColor, glowColor, (elapsedTime / glowDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        buttonImage.color = glowColor;
    }
}
