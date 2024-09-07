using UnityEngine;

public class FadingVibration : MonoBehaviour
{
    public float initialAmplitudeX = 10f; // Initial intensity along the x-axis
    public float initialAmplitudeY = 5f;  // Initial intensity along the y-axis
    public float initialAmplitudeZ = 15f; // Initial intensity along the z-axis
    public float frequency = 2.0f;        // Vibration speed
    public float fadeDuration = 3.0f;     // Duration of the fade-out (in seconds)

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float timeElapsed = 0.0f;

    private void Start()
    {
        // Store the original position and rotation of the object
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    private void Update()
    {
        // Calculate the fade factor based on time
        float fadeFactor = Mathf.Clamp01(1.0f - timeElapsed / fadeDuration);

        // Calculate the new position based on sine waves for each axis
        timeElapsed += Time.deltaTime;
        // float xOffset = initialAmplitudeX * fadeFactor * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        // float yOffset = initialAmplitudeY * fadeFactor * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        // float zOffset = initialAmplitudeZ * fadeFactor * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);

        // // Apply the vibration to the object's position
        // transform.position = originalPosition + new Vector3(xOffset, yOffset, zOffset);

        // Calculate the new rotation based on sine waves for each axis
        float xRot = initialAmplitudeX * fadeFactor * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        float yRot = initialAmplitudeY * fadeFactor * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        float zRot = initialAmplitudeZ * fadeFactor * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);

        // Apply the rotational vibration to the object's rotation
        Quaternion targetRotation = originalRotation * Quaternion.Euler(xRot, yRot, zRot);
        transform.rotation = targetRotation;
    }
}
