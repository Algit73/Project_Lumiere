using UnityEngine;

public class MultiaxisVibration : MonoBehaviour
{
    public float amplitudeX = 10f; // Intensity along the x-axis
    public float amplitudeY = 5f;  // Intensity along the y-axis
    public float amplitudeZ = 15f; // Intensity along the z-axis
    public float frequency = 2.0f; // Vibration speed

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
        // Calculate the new position based on sine waves for each axis
        timeElapsed += Time.deltaTime;
        // float xOffset = amplitudeX * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        // float yOffset = amplitudeY * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        // float zOffset = amplitudeZ * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);

        // // Apply the vibration to the object's position
        // transform.position = originalPosition + new Vector3(xOffset, yOffset, zOffset);

        // Calculate the new rotation based on sine waves for each axis
        float xRot = amplitudeX * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        float yRot = amplitudeY * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        float zRot = amplitudeZ * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);

        // Apply the rotational vibration to the object's rotation
        Quaternion targetRotation = originalRotation * Quaternion.Euler(xRot, yRot, zRot);
        transform.rotation = targetRotation;
    }
}
