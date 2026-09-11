using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     One-shot effect spawner. Prefabs destroy themselves when their particle system stops, so nothing is
///     tracked here; rates are low enough (a rifle at ten shots a second is the peak) that pooling would be
///     speculative.
/// </summary>
public static class Vfx
{
    public static GameObject Spawn(GameObject prefab, Vector3 position, Vector3 forward)
    {
        if (prefab == null)
        {
            return null;
        }

        return Object.Instantiate(prefab, position, Rotation(forward));
    }

    public static Quaternion Rotation(Vector3 forward)
    {
        return forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
    }
}
}
