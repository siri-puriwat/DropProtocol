using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class SpawnSlotsTests
    {
        [Test]
        public void NextFree_NothingTaken_ReturnsZero()
        {
            Assert.That(SpawnSlots.NextFree(new int[0], 4), Is.EqualTo(0));
        }

        [Test]
        public void NextFree_FillsLowestGapFirst()
        {
            Assert.That(SpawnSlots.NextFree(new[] { 0, 2 }, 4), Is.EqualTo(1));
        }

        [Test]
        public void NextFree_AllTaken_ReturnsMinusOne()
        {
            Assert.That(SpawnSlots.NextFree(new[] { 0, 1, 2, 3 }, 4), Is.EqualTo(-1));
        }

        [Test]
        public void NextFree_IgnoresIndicesOutsideRange()
        {
            Assert.That(SpawnSlots.NextFree(new[] { 7, -1 }, 2), Is.EqualTo(0));
        }

        [Test]
        public void SlotToEvict_ReturnsHighestBotSlot()
        {
            Assert.That(SpawnSlots.SlotToEvict(new[] { 1, 3, 2 }), Is.EqualTo(3));
        }

        [Test]
        public void SlotToEvict_NoBots_ReturnsMinusOne()
        {
            Assert.That(SpawnSlots.SlotToEvict(new int[0]), Is.EqualTo(-1));
        }
    }
}
