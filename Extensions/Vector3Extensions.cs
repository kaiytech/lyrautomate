using gdio.common.objects;

namespace LyrAutomate.Extensions;

public static class Vector3Extensions
{
    public static Vector3 Add(this Vector3 val1, Vector3 val2)
    {
        return new Vector3
        {
            x = val1.x + val2.x,
            y = val1.y + val2.y,
            z = val1.z + val2.z
        };
    }

    public static bool InRangeOf(this Vector3 val, Vector3 target, float range)
    {
        return (Math.Abs(val.x - target.x) < range && Math.Abs(val.y - target.y) < range &&
                Math.Abs(val.z - target.z) < range);
    }
}