namespace DropProtocol
{
/// <summary>Edges the ammo HUD reacts to, kept pure so they are testable without a weapon.</summary>
public static class AmmoRules
{
    public static bool MagazineEmptied(int previous, int current)
    {
        return previous > 0 && current == 0;
    }

    public static bool CrossedLow(int previous, int current, int threshold)
    {
        return threshold > 0 && current > 0 && current <= threshold && previous > threshold;
    }
}
}
