namespace DropProtocol
{
/// <summary>Static so revive progress is testable without a scene.</summary>
public static class ReviveRules
{
    public static float Step(float progress, bool holdingOnSameTarget, float deltaTime, float durationSeconds)
    {
        if (!holdingOnSameTarget)
        {
            return 0f;
        }

        if (durationSeconds <= 0f)
        {
            return 1f;
        }

        return progress + deltaTime / durationSeconds;
    }

    public static bool IsComplete(float progress)
    {
        return progress >= 1f;
    }
}
}
