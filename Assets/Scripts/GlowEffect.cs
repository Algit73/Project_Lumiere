using UnityEngine;
using System.Collections;

public class GlowEffect : MonoBehaviour
{
    public Color emissionColor = Color.white; // Set your desired emission color
    public float glowSpeed = 2f; // Speed of glowing
    public float minIntensity = 0f; // Minimum emission intensity
    public float maxIntensity = 1f; // Maximum emission intensity
    public int glowMaterialIndex = 1; // Index of the glow material in the material array
    private Material glowMaterial;

    private Material material;
    private float emissionIntensity;
    private bool increasing = true;
    // private bool isActive = false; // Indicates whether the glow effect is active
    private Coroutine glowCoroutine;

    void Start()
    {
        /// Get the material of the object
        Renderer renderer = GetComponent<Renderer>();

        if (renderer != null && renderer.materials.Length > glowMaterialIndex)
        {
            /// Access the material at the specified index
            glowMaterial = renderer.materials[glowMaterialIndex];

            /// Set initial emission color and intensity
            glowMaterial.SetColor("_EmissionColor", emissionColor * minIntensity);
        }
        else
        {
            Debug.LogWarning("GlowEffect: The specified glow material index is out of bounds or no renderer found.");
        }

        
    }

    void Update()
    {

    }

    private IEnumerator GlowCoroutine()
    {
        increasing = true;
        while (true) // This will run indefinitely
        {
            if (glowMaterial != null)
            {
                // Adjust the emission intensity over time
                if (increasing)
                {
                    emissionIntensity += Time.deltaTime * glowSpeed;
                    if (emissionIntensity >= maxIntensity)
                    {
                        emissionIntensity = maxIntensity;
                        increasing = false;
                    }
                }
                else
                {
                    emissionIntensity -= Time.deltaTime * glowSpeed;
                    if (emissionIntensity <= minIntensity)
                    {
                        emissionIntensity = minIntensity;
                        increasing = true;
                    }
                }

                // Apply the emission color with intensity to the glow material
                glowMaterial.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            }

            // Wait for the next frame
            yield return null;
        }
    }

    private void set_material_transparent (bool set)
    {
        if (set) glowMaterial.renderQueue = 3000;
        else glowMaterial.renderQueue = 1;
        
    }

    public void Activate()
    {
        emissionIntensity = minIntensity; // Reset intensity when activated

        set_material_transparent(true);

        // Stop any existing coroutine to prevent multiple instances
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
        }

        // Start the glow coroutine
        glowCoroutine = StartCoroutine(GlowCoroutine());
    }

    /// Method to deactivate the glow effect
    public void Deactivate()
    {
        set_material_transparent(false);

        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
            glowCoroutine = null;

            /// Reset the glow to its minimum intensity
            if (glowMaterial != null)
                glowMaterial.SetColor("_EmissionColor", emissionColor * minIntensity);
        }
    }
}
