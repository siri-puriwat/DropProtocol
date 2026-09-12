using NUnit.Framework;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
public sealed class SfxCueRulesTests
{
    [Test]
    public void IsEmpty_TreatsNullEmptyAndAllNullArraysAsEmpty()
    {
        Assert.That(SfxCueRules.IsEmpty(null), Is.True);
        Assert.That(SfxCueRules.IsEmpty(new AudioClip[0]), Is.True);
        Assert.That(SfxCueRules.IsEmpty(new AudioClip[] { null, null }), Is.True);
        Assert.That(SfxCueRules.IsEmpty(new[] { AudioClip.Create("clip", 44, 1, 44100, false) }), Is.False);
    }

    [Test]
    public void Pick_StaysInsideTheArray()
    {
        Assert.That(SfxCueRules.Pick(0, 0.5f), Is.EqualTo(-1));
        Assert.That(SfxCueRules.Pick(5, 0f), Is.EqualTo(0));
        Assert.That(SfxCueRules.Pick(5, 0.5f), Is.EqualTo(2));
        Assert.That(SfxCueRules.Pick(5, 1f), Is.EqualTo(4));
        Assert.That(SfxCueRules.Pick(5, 7f), Is.EqualTo(4));
    }

    [Test]
    public void Pitch_ZeroRangeMeansUnity_OtherwiseWithinRange()
    {
        Assert.That(SfxCueRules.Pitch(Vector2.zero, 0.3f), Is.EqualTo(1f));
        Assert.That(SfxCueRules.Pitch(new Vector2(0.9f, 1.1f), 0f), Is.EqualTo(0.9f).Within(0.0001f));
        Assert.That(SfxCueRules.Pitch(new Vector2(0.9f, 1.1f), 1f), Is.EqualTo(1.1f).Within(0.0001f));
        Assert.That(SfxCueRules.Pitch(new Vector2(1.1f, 0.9f), 0.5f), Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void PickVoice_PrefersIdleVoices_ElseTheOldest()
    {
        Assert.That(SfxCueRules.PickVoice(null, null), Is.EqualTo(-1));
        Assert.That(SfxCueRules.PickVoice(new[] { true, false, true }, new[] { 1.0, 2.0, 3.0 }), Is.EqualTo(1));
        Assert.That(SfxCueRules.PickVoice(new[] { true, true, true }, new[] { 5.0, 2.0, 3.0 }), Is.EqualTo(1));
        Assert.That(SfxCueRules.PickVoice(new[] { true, true }, null), Is.EqualTo(0));
    }

    [Test]
    public void DefaultCue_IsSilentButPlaysAtFullVolumeAndUnityPitch()
    {
        var cue = SfxCue.Default;
        Assert.That(cue.IsEmpty, Is.True);
        Assert.That(cue.Volume, Is.EqualTo(1f));
        Assert.That(SfxCueRules.Pitch(cue.PitchRange, 0.5f), Is.EqualTo(1f));
    }
}
}
