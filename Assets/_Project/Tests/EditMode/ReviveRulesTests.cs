using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class ReviveRulesTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Step_WhileHolding_AdvancesByFractionOfDuration()
        {
            float progress = ReviveRules.Step(0.5f, holdingOnSameTarget: true, deltaTime: 0.3f, durationSeconds: 3f);

            Assert.That(progress, Is.EqualTo(0.6f).Within(Tolerance));
        }

        [Test]
        public void Step_WhenNotHolding_Resets()
        {
            float progress = ReviveRules.Step(0.9f, holdingOnSameTarget: false, deltaTime: 0.1f, durationSeconds: 3f);

            Assert.That(progress, Is.Zero);
        }

        [Test]
        public void Step_ZeroDuration_CompletesImmediately()
        {
            float progress = ReviveRules.Step(0f, holdingOnSameTarget: true, deltaTime: 0.01f, durationSeconds: 0f);

            Assert.That(ReviveRules.IsComplete(progress), Is.True);
        }

        [Test]
        public void IsComplete_OnlyAtOrPastOne()
        {
            Assert.That(ReviveRules.IsComplete(0.999f), Is.False);
            Assert.That(ReviveRules.IsComplete(1f), Is.True);
            Assert.That(ReviveRules.IsComplete(1.2f), Is.True);
        }
    }
}
