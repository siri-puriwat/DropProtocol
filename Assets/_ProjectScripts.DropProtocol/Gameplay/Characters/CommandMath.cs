using UnityEngine;

namespace DropProtocol
{
public static class CommandMath
{
    /// <summary>
    ///     Rotates camera-relative input (x = camera right, y = camera forward) into world-space XZ.
    /// </summary>
    public static Vector2 InputToWorldXZ(Vector2 input, float cameraYawDegrees)
    {
        var clamped = Vector2.ClampMagnitude(input, 1f);
        float radians = cameraYawDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(clamped.x * cos + clamped.y * sin, -clamped.x * sin + clamped.y * cos);
    }

    /// <summary>
    ///     Zero inside the deadzone so a cursor resting on the character does not spin it.
    /// </summary>
    public static Vector2 AimDirection(Vector3 origin, Vector3 target, float deadzoneRadius)
    {
        var delta = new Vector2(target.x - origin.x, target.z - origin.z);
        if (delta.sqrMagnitude <= deadzoneRadius * deadzoneRadius)
        {
            return Vector2.zero;
        }

        return delta.normalized;
    }

    public static bool TryProjectToPlane(Ray ray, float planeHeight, out Vector3 point)
    {
        var plane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
        if (plane.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = default;
        return false;
    }
}
}
