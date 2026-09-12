using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    public class VfxPoolTests
    {
        private const string EffectName = "VfxPoolTestEffect";
        private const float Lifetime = 0.05f;

        private GameObject m_template;

        [SetUp]
        public void SetUp()
        {
            m_template = new GameObject(EffectName);
            m_template.SetActive(false);
            var system = m_template.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = false;
            main.duration = Lifetime;
            main.startLifetime = Lifetime;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject instance in Instances())
                Object.Destroy(instance);
            Object.Destroy(m_template);
        }

        [UnityTest]
        public IEnumerator Spawn_AfterTheEffectStops_ReusesTheParkedInstance()
        {
            GameObject first = Vfx.Spawn(m_template, Vector3.zero, Vector3.forward);
            Assert.That(first.activeSelf, Is.True);

            yield return HostTestHarness.Wait(Lifetime * 8f);
            Assert.That(first.activeSelf, Is.False, "the effect should park itself when it stops");
            Assert.That(Vfx.ParkedCount(m_template), Is.EqualTo(1));

            GameObject second = Vfx.Spawn(m_template, new Vector3(3f, 0f, 0f), Vector3.right);

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.activeSelf, Is.True);
            Assert.That(second.transform.position.x, Is.EqualTo(3f).Within(1e-3f));
            Assert.That(Instances().Count(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Spawn_WhileTheEffectPlays_InstantiatesAnother()
        {
            GameObject first = Vfx.Spawn(m_template, Vector3.zero, Vector3.forward);
            GameObject second = Vfx.Spawn(m_template, Vector3.zero, Vector3.forward);

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(Instances().Count(), Is.EqualTo(2));

            yield return HostTestHarness.Wait(Lifetime * 8f);
            Assert.That(Vfx.ParkedCount(m_template), Is.EqualTo(2));
        }

        private static System.Collections.Generic.IEnumerable<GameObject> Instances()
        {
            return Object.FindObjectsByType<PooledVfx>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(effect => effect.gameObject)
                .Where(instance => instance.name.StartsWith(EffectName));
        }
    }
}
