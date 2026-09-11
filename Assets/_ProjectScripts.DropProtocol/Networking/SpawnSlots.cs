using System.Collections.Generic;

namespace DropProtocol
{
/// <summary>Pure slot bookkeeping for <see cref="PlayerSpawner" />: players take the lowest free squad slot.</summary>
public static class SpawnSlots
{
    /// <summary>Lowest index in <c>[0, slotCount)</c> not present in <paramref name="taken" />, or -1 when full.</summary>
    public static int NextFree(IEnumerable<int> taken, int slotCount)
    {
        var used = new HashSet<int>(taken);
        for (int slot = 0; slot < slotCount; slot++)
        {
            if (!used.Contains(slot))
            {
                return slot;
            }
        }

        return -1;
    }

    /// <summary>Highest of <paramref name="botSlots" />, or -1 when empty: the last bot to fill in is the first to make room.</summary>
    public static int SlotToEvict(IEnumerable<int> botSlots)
    {
        int highest = -1;
        foreach (int slot in botSlots)
        {
            if (slot > highest)
            {
                highest = slot;
            }
        }

        return highest;
    }
}
}
