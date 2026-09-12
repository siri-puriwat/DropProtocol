using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
public sealed class AmmoRulesTests
{
    [Test]
    public void MagazineEmptied_OnlyOnTheDropToZero()
    {
        Assert.That(AmmoRules.MagazineEmptied(1, 0), Is.True);
        Assert.That(AmmoRules.MagazineEmptied(0, 0), Is.False);
        Assert.That(AmmoRules.MagazineEmptied(0, 30), Is.False);
        Assert.That(AmmoRules.MagazineEmptied(5, 4), Is.False);
    }

    [Test]
    public void CrossedLow_FiresOnceWhenFallingThroughTheThreshold()
    {
        Assert.That(AmmoRules.CrossedLow(6, 5, 5), Is.True);
        Assert.That(AmmoRules.CrossedLow(5, 4, 5), Is.False);
        Assert.That(AmmoRules.CrossedLow(1, 0, 5), Is.False);
        Assert.That(AmmoRules.CrossedLow(0, 30, 5), Is.False);
        Assert.That(AmmoRules.CrossedLow(6, 5, 0), Is.False);
    }
}
}
