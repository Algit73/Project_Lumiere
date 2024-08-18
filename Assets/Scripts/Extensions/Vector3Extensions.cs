using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 FixRotForLerp(this ref Vector3 original)
    {
        return new Vector3(original.x.FixRotForLerp(), original.y.FixRotForLerp(), original.z.FixRotForLerp());
    }
}