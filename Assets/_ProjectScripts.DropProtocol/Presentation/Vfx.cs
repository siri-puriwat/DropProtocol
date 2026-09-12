using System.Collections.Generic;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     One-shot effect spawner with a pool per prefab. An instance parks itself when its particle system
///     stops and is reused by the next spawn of the same prefab, so a full squad firing at ten shots a
///     second no longer instantiates and destroys a flash and an impact per round.
/// </summary>
public static class Vfx
{
    private static readonly Dictionary<GameObject, Stack<PooledVfx>> Pools = new();

    public static GameObject Spawn(GameObject prefab, Vector3 position, Vector3 forward)
    {
        if (prefab == null)
        {
            return null;
        }

        var rotation = Rotation(forward);
        var pool = PoolFor(prefab);

        PooledVfx effect = null;
        // A scene unload destroys parked instances; skip the dead ones.
        while (pool.Count > 0 && effect == null)
        {
            effect = pool.Pop();
        }

        if (effect == null)
        {
            var instance = Object.Instantiate(prefab, position, rotation);
            effect = instance.GetComponent<PooledVfx>();
            if (effect == null)
            {
                effect = instance.AddComponent<PooledVfx>();
            }

            effect.Bind(pool);
        }
        else
        {
            effect.transform.SetPositionAndRotation(position, rotation);
        }

        effect.Play();
        return effect.gameObject;
    }

    public static Quaternion Rotation(Vector3 forward)
    {
        return forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
    }

    /// <summary>Parked instances waiting for the next spawn of <paramref name="prefab" />.</summary>
    public static int ParkedCount(GameObject prefab)
    {
        return prefab != null && Pools.TryGetValue(prefab, out var pool) ? pool.Count : 0;
    }

    private static Stack<PooledVfx> PoolFor(GameObject prefab)
    {
        if (!Pools.TryGetValue(prefab, out var pool))
        {
            pool = new Stack<PooledVfx>();
            Pools[prefab] = pool;
        }

        return pool;
    }
}
}
