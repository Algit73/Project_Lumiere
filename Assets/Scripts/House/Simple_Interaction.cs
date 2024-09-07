using UnityEngine;
using System.Collections;
using System;

public class Simple_Interaction : Interactive
{
    // [SerializeField] private GameObject object1, object2;

    [SerializeField] private float initialAmplitudeX = 0f; // Initial intensity along the x-axis
    [SerializeField] private float initialAmplitudeY = 0f;  // Initial intensity along the y-axis
    [SerializeField] private float initialAmplitudeZ = 3f; // Initial intensity along the z-axis
    [SerializeField] private float frequency = 15f;        // Vibration speed
    [SerializeField] private float fadeDuration = 2.0f;     // Duration of the fade-out (in seconds)

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool _object1IsActive;

    public override void Start()
    {
        base.Start();
        originalRotation = transform.rotation;

        // _object1IsActive = object1.activeInHierarchy;
    }

    public override void Action()
    {
        // object1.SetActive(!object1.activeInHierarchy);
        // object2.SetActive(!object2.activeInHierarchy);
        if (glowEffect != null)
            glowEffect.Activate();
        StartCoroutine(FadeVibration(deactivateGlow));
        

        IsWorking = false;
    }

    public override void ResetObject()
    {
        // object1.SetActive(_object1IsActive);
        // object2.SetActive(!_object1IsActive);
    }

    private void deactivateGlow()
    {
        if (glowEffect != null)
            glowEffect.Deactivate();
    }

    private IEnumerator FadeVibration(Action deactivateGlow)
    {
        float timeElapsed = 0.0f;
        while (timeElapsed < fadeDuration)
        {
            /// Calculate the fade factor based on time
            float fadeFactor = Mathf.Clamp01(1.0f - timeElapsed / fadeDuration);

            /// Calculate the new rotation based on sine waves for each axis
            float xRot = initialAmplitudeX * fadeFactor * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
            float yRot = initialAmplitudeY * fadeFactor * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
            float zRot = initialAmplitudeZ * fadeFactor * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);

            /// Apply the rotational vibration to the object's rotation
            Quaternion targetRotation = originalRotation * Quaternion.Euler(xRot, yRot, zRot);
            transform.rotation = targetRotation;

            /// Wait for the next frame
            yield return null;

            /// Update the elapsed time
            timeElapsed += Time.deltaTime;
        }
        // glowEffect.Deactivate();
        deactivateGlow();
        /// Ensure the object settles at the original position and rotation
        transform.rotation = originalRotation;
    }
}