using UnityEngine;

public static class DelayUtility
{
    public static bool WaitForDuration(float seconds)
    {
        // Wait for the specified duration.
        // You can use WaitForSeconds or WaitForSecondsRealtime here.
        // For scaled time (affected by Time.timeScale):
        // yield return new WaitForSeconds(seconds);
        // For unscaled time (not affected by Time.timeScale):
        // yield return new WaitForSecondsRealtime(seconds);

        // For demonstration purposes, let's just wait using scaled time:
        float startTime = Time.time;
        while (Time.time < startTime + seconds)
        {
            // You can add any additional checks or conditions here.
            // For example, check if a specific condition is met during the wait.
        }

        // Return true after waiting.
        return true;
    }
}
