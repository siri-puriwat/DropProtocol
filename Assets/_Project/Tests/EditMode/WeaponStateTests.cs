using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class WeaponStateTests
    {
        private const int Magazine = 3;
        private const double Interval = 0.1;
        private const double Reload = 2.0;

        private WeaponState m_state;

        [SetUp]
        public void SetUp()
        {
            m_state = new WeaponState(Magazine, Interval, Reload);
        }

        [Test]
        public void NewState_HasFullMagazine()
        {
            Assert.That(m_state.Ammo, Is.EqualTo(Magazine));
            Assert.That(m_state.IsReloading, Is.False);
            Assert.That(m_state.CanFire(0.0), Is.True);
        }

        [Test]
        public void TryFire_ConsumesOneRound()
        {
            Assert.That(m_state.TryFire(10.0), Is.True);
            Assert.That(m_state.Ammo, Is.EqualTo(Magazine - 1));
        }

        [Test]
        public void TryFire_BeforeIntervalElapses_IsBlocked()
        {
            m_state.TryFire(10.0);

            Assert.That(m_state.TryFire(10.0 + Interval * 0.5), Is.False);
            Assert.That(m_state.Ammo, Is.EqualTo(Magazine - 1));
        }

        [Test]
        public void TryFire_AtExactlyInterval_FiresAgain()
        {
            m_state.TryFire(10.0);

            Assert.That(m_state.TryFire(10.0 + Interval), Is.True);
        }

        [Test]
        public void TryFire_WithEmptyMagazine_IsBlocked()
        {
            double now = 10.0;
            for (int i = 0; i < Magazine; i++)
                m_state.TryFire(now += Interval);

            Assert.That(m_state.Ammo, Is.Zero);
            Assert.That(m_state.TryFire(now + Interval), Is.False);
        }

        [Test]
        public void TryStartReload_WhenFull_IsRefused()
        {
            Assert.That(m_state.TryStartReload(10.0), Is.False);
            Assert.That(m_state.IsReloading, Is.False);
        }

        [Test]
        public void TryStartReload_WhileReloading_IsRefused()
        {
            m_state.TryFire(10.0);
            Assert.That(m_state.TryStartReload(11.0), Is.True);

            Assert.That(m_state.TryStartReload(11.5), Is.False);
        }

        [Test]
        public void Update_PastReloadDuration_RefillsMagazine()
        {
            m_state.TryFire(10.0);
            m_state.TryStartReload(11.0);

            m_state.Update(11.0 + Reload - 0.01);
            Assert.That(m_state.IsReloading, Is.True);
            Assert.That(m_state.Ammo, Is.EqualTo(Magazine - 1));

            m_state.Update(11.0 + Reload);
            Assert.That(m_state.IsReloading, Is.False);
            Assert.That(m_state.Ammo, Is.EqualTo(Magazine));
        }

        [Test]
        public void Refill_FillsMagazineAndCancelsReload()
        {
            m_state.TryFire(10.0);
            m_state.TryStartReload(11.0);

            m_state.Refill();

            Assert.That(m_state.Ammo, Is.EqualTo(Magazine));
            Assert.That(m_state.IsReloading, Is.False);
            Assert.That(m_state.CanFire(11.5), Is.True);
        }

        [Test]
        public void TryFire_DuringReload_IsBlocked()
        {
            m_state.TryFire(10.0);
            m_state.TryStartReload(11.0);

            Assert.That(m_state.TryFire(11.5), Is.False);
        }
    }
}
