using UnityEngine;

public static class FloatExtensions
{
    public static float FixRotForLerp(this ref float original)
    {
        if (original > 0f)
            return original >= 180f ? original -= 360f : original;
        else
            return original <= -180f ? original += 360f : original;
    }
}