using UnityEngine;

namespace DropProtocol
{
/// <summary>Shared closest-hit raycast for weapons and sentries; the shooter's own root never counts.</summary>
public static class Hitscan
{
    public static bool TryResolveHit(Vector3 origin, Vector3 direction, float range, LayerMask mask,
        RaycastHit[] buffer, Transform ignoreRoot, out RaycastHit closest)
    {
        closest = default;
        int count = Physics.RaycastNonAlloc(origin, direction, buffer, range, mask, QueryTriggerInteraction.Ignore);
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            var candidate = buffer[i];
            if (ignoreRoot != null && candidate.transform.root == ignoreRoot)
            {
                continue;
            }

            if (!found || candidate.distance < closest.distance)
            {
                closest = candidate;
                found = true;
            }
        }

        return found;
    }
}
}
