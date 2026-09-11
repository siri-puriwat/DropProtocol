using UnityEngine;

namespace DropProtocol
{
/// <summary>Static so the mission rules are testable without a scene.</summary>
public static class MissionRules
{
    public static bool IsSquadWiped(int playerCount, int aliveCount)
    {
        return playerCount > 0 && aliveCount <= 0;
    }

    public static bool IsTimedOut(float elapsed, float timeLimitSeconds)
    {
        return timeLimitSeconds > 0f && elapsed >= timeLimitSeconds;
    }

    public static bool AllRelaysActivated(int activated, int count)
    {
        return activated >= count;
    }

    // Unlike a revive, releasing keeps the progress: enemy pressure is the cost, not restarting.
    public static float RelayStep(float progress, bool held, float deltaTime, float activateSeconds)
    {
        if (!held)
        {
            return progress;
        }

        if (activateSeconds <= 0f)
        {
            return 1f;
        }

        return Mathf.Min(1f, progress + deltaTime / activateSeconds);
    }

    public static bool IsRelayActivated(float progress)
    {
        return progress >= 1f;
    }

    // Pauses while nobody alive is inside; it never resets.
    public static float ExtractionStep(float remaining, bool anyoneInside, float deltaTime)
    {
        return anyoneInside ? Mathf.Max(0f, remaining - deltaTime) : remaining;
    }

    public static bool IsExtractionComplete(float remaining)
    {
        return remaining <= 0f;
    }

    public static bool DirectorShouldRun(MissionPhase phase)
    {
        return phase == MissionPhase.Active || phase == MissionPhase.Extraction;
    }

    public static float Intensity(MissionPhase phase, int relaysActivated, float perRelay, float extractionIntensity)
    {
        switch (phase)
        {
            case MissionPhase.Extraction:
                return extractionIntensity;
            case MissionPhase.Active:
                return 1f + relaysActivated * perRelay;
            default:
                return 1f;
        }
    }

    public static bool IsInside(Vector3 point, Vector3 centre, float radius)
    {
        return EnemyRules.FlatDistance(point, centre) <= radius;
    }
}
}
