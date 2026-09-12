using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol.Tests.EditMode
{
    public class FillBarTests
    {
        private GameObject m_track;
        private FillBar m_bar;
        private RectTransform m_fill;

        [SetUp]
        public void SetUp()
        {
            m_track = new GameObject("Track", typeof(RectTransform));
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(m_track.transform, false);
            m_fill = fill.GetComponent<RectTransform>();
            m_fill.anchorMin = Vector2.zero;
            m_fill.anchorMax = Vector2.one;

            m_bar = m_track.AddComponent<FillBar>();
            var serialized = new SerializedObject(m_bar);
            serialized.FindProperty("m_fill").objectReferenceValue = fill.GetComponent<Image>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_track);
        }

        [Test]
        public void SetFraction_MovesOnlyTheRightAnchor()
        {
            m_bar.SetFraction(0.25f);

            Assert.That(m_fill.anchorMax.x, Is.EqualTo(0.25f));
            Assert.That(m_fill.anchorMax.y, Is.EqualTo(1f));
            Assert.That(m_fill.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(m_bar.Fraction, Is.EqualTo(0.25f));
        }

        [Test]
        public void SetFraction_Clamps()
        {
            m_bar.SetFraction(1.7f);
            Assert.That(m_bar.Fraction, Is.EqualTo(1f));

            m_bar.SetFraction(-1f);
            Assert.That(m_bar.Fraction, Is.EqualTo(0f));
        }

        [Test]
        public void SetColor_TintsTheFill()
        {
            m_bar.SetColor(Color.red);

            Assert.That(m_fill.GetComponent<Image>().color, Is.EqualTo(Color.red));
        }
    }
}
