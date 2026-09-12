using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DropProtocol.Tests.EditMode
{
/// <summary>
///     Audio cannot be heard in a test run, so this is the only guard against a definition whose cue lost
///     its clips in a migration or a bad merge: every shipped cue must carry at least one clip.
/// </summary>
public sealed class DefinitionCueTests
{
    private static IEnumerable<Object> Shipped<T>() where T : ScriptableObject
    {
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/_Project/Settings" }))
        {
            yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }

    private static void AssertCuesHaveClips(Object asset, params string[] fields)
    {
        var serialized = new SerializedObject(asset);
        foreach (string field in fields)
        {
            SerializedProperty cue = serialized.FindProperty(field);
            Assert.That(cue, Is.Not.Null, $"{asset.name}.{field} missing");
            SerializedProperty clips = cue.FindPropertyRelative("Clips");
            bool any = false;
            for (int i = 0; i < clips.arraySize; i++)
            {
                any |= clips.GetArrayElementAtIndex(i).objectReferenceValue != null;
            }

            Assert.That(any, Is.True, $"{asset.name}.{field} has no clip");
            Assert.That(cue.FindPropertyRelative("Volume").floatValue, Is.GreaterThan(0f), $"{asset.name}.{field} is silent");
        }
    }

    [Test]
    public void EveryWeaponDefinition_HasFireReloadAndImpactCues()
    {
        int count = 0;
        foreach (Object asset in Shipped<WeaponDefinition>())
        {
            count++;
            AssertCuesHaveClips(asset, "m_fireCue", "m_reloadCue", "m_impactCue");
        }

        Assert.That(count, Is.GreaterThan(0));
    }

    [Test]
    public void EveryEnemyDefinition_HasAttackHurtDeathAndFootstepCues()
    {
        int count = 0;
        foreach (Object asset in Shipped<EnemyDefinition>())
        {
            count++;
            AssertCuesHaveClips(asset, "m_attackCue", "m_hurtCue", "m_deathCue", "m_footstepCue");
        }

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public void EveryProtocolDefinition_HasACallCue()
    {
        int count = 0;
        foreach (Object asset in Shipped<ProtocolDefinition>())
        {
            count++;
            AssertCuesHaveClips(asset, "m_callCue");
        }

        Assert.That(count, Is.EqualTo(3));
    }
}
}
