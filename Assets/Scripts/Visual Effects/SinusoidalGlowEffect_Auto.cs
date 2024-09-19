using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SinusoidalGlowEffect_Auto : MonoBehaviour
{
    public Outline glowOutline; // Assign the Outline component in the Inspector
    public float frequencyX = 2f; // Frequency of the sine wave
    public float frequencyY = 3f; // Frequency of the sine wave
    public float amplitudeX = 15f; // Amplitude of the effect distance change
    public float amplitudeY = 10f; // Amplitude of the effect distance change
    public Vector2 baseEffectDistance = new Vector2(1, 1); // Base outline effect distance
    private Coroutine glowCoroutine; // To store reference to the coroutine
    public Button targetButton; // The button to apply the effect to

    // Use this method to initialize the button and outline component
    public void Initialize(Button targetButton)
    {
        this.targetButton = targetButton;
        glowOutline = targetButton.GetComponent<Outline>();

        // If the button doesn't already have an Outline component, add one
        if (glowOutline == null)
        {
            glowOutline = targetButton.gameObject.AddComponent<Outline>();
        }
    }

    // Method to start the sinusoidal glow effect
    public void StartGlow()
    {
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
        }
        glowCoroutine = StartCoroutine(GlowEffectCoroutine());
    }

    // Method to stop the sinusoidal glow effect
    public void StopGlow()
    {
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
            glowCoroutine = null;
        }
        glowOutline.effectDistance = baseEffectDistance;
    }

    // Coroutine to handle the sinusoidal glow effect
    private IEnumerator GlowEffectCoroutine()
    {
        float timeElapsed = 0f;

        while (true)
        {
            timeElapsed += Time.deltaTime;
            float sineWaveX = Mathf.Sin(timeElapsed * frequencyX) * amplitudeX;
            float sineWaveY = Mathf.Sin(timeElapsed * frequencyY) * amplitudeY;
            glowOutline.effectDistance = baseEffectDistance + new Vector2(sineWaveX, sineWaveY);
            yield return null;
        }
    }

    private void Start() 
    {
        StartGlow();
        StartGlow();
    }

    private void Update() 
    {
        
        
    }
}
