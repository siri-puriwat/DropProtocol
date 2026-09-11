using UnityEngine;

namespace DropProtocol
{
public static class EnemyPresentationRules
{
    public const float MinAttackSpeed = 0.25f;
    public const float MaxAttackSpeed = 4f;

    /// <summary>
    ///     Playback speed that lands the clip's hit frame (<paramref name="hitFraction" /> of its length) exactly
    ///     when the server windup completes, so the swing and the damage line up on every peer.
    /// </summary>
    public static float AttackSpeed(float clipSeconds, float hitFraction, float windupSeconds)
    {
        if (clipSeconds <= 0f || windupSeconds <= 0f || hitFraction <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp(clipSeconds * hitFraction / windupSeconds, MinAttackSpeed, MaxAttackSpeed);
    }

    public static bool IsTelegraphing(EnemyState state)
    {
        return state == EnemyState.Attack;
    }
}
}
