using UnityEngine;

public class XYVibration : MonoBehaviour
{
    public float amplitudeX = 0.1f; // Intensity along the x-axis
    public float amplitudeY = 0.1f; // Intensity along the y-axis
    public float frequency = 2.0f;  // Vibration speed

    private Vector3 originalPosition;
    private float timeElapsed = 0.0f;

    private void Start()
    {
        originalPosition = transform.position;
    }

    private void Update()
    {
        // Calculate the new position based on sine waves for x and y
        timeElapsed += Time.deltaTime;
        float xOffset = amplitudeX * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        float yOffset = amplitudeY * Mathf.Cos(2 * Mathf.PI * frequency * timeElapsed);

        // Apply the vibration to the object
        transform.position = originalPosition + new Vector3(xOffset, yOffset, 0f);
    }
}
