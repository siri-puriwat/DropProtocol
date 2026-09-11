using UnityEngine;

namespace DropProtocol
{
/// <summary>What the mission sees at one tick. Counts come from the registries on the server.</summary>
public struct MissionInputs
{
    public int RelayCount;
    public int RelaysActivated;
    public int PlayerCount;
    public int AliveCount;
    public int AliveInsideZone;
}

/// <summary>
///     Mission phase machine: Deployment, Active, Extraction, then Complete or Failed. Inputs and time
///     are injected so the transitions are EditMode-tested. Inside a phase the checks run in the order
///     squad wipe, time limit, progress, so a wipe or a time-out always wins a tie.
/// </summary>
public sealed class MissionState
{
    private readonly MissionTuning m_tuning;

    public MissionState(MissionTuning tuning)
    {
        m_tuning = tuning;
        Phase = MissionPhase.Deployment;
        Outcome = MissionOutcome.None;
        DeployRemaining = tuning.DeploySeconds;
        ExtractionRemaining = tuning.ExtractionSeconds;
    }

    public MissionPhase Phase { get; private set; }
    public MissionOutcome Outcome { get; private set; }
    public float DeployRemaining { get; private set; }

    /// <summary>Seconds since Active began; Deployment does not count against the time limit.</summary>
    public float Elapsed { get; private set; }

    public float ExtractionRemaining { get; private set; }
    public int ExtractedCount { get; private set; }

    public bool IsOver => Phase == MissionPhase.Complete || Phase == MissionPhase.Failed;

    /// <summary>Advances one tick and returns true when the phase changed.</summary>
    public bool Step(MissionInputs inputs, float deltaTime)
    {
        switch (Phase)
        {
            case MissionPhase.Deployment:
                return StepDeployment(inputs, deltaTime);
            case MissionPhase.Active:
                return StepActive(inputs, deltaTime);
            case MissionPhase.Extraction:
                return StepExtraction(inputs, deltaTime);
            default:
                return false;
        }
    }

    private bool StepDeployment(MissionInputs inputs, float deltaTime)
    {
        if (MissionRules.IsSquadWiped(inputs.PlayerCount, inputs.AliveCount))
        {
            return Fail(MissionOutcome.SquadWiped);
        }

        DeployRemaining = Mathf.Max(0f, DeployRemaining - deltaTime);
        if (DeployRemaining > 0f)
        {
            return false;
        }

        Phase = MissionPhase.Active;
        return true;
    }

    private bool StepActive(MissionInputs inputs, float deltaTime)
    {
        Elapsed += deltaTime;
        if (MissionRules.IsSquadWiped(inputs.PlayerCount, inputs.AliveCount))
        {
            return Fail(MissionOutcome.SquadWiped);
        }

        if (MissionRules.IsTimedOut(Elapsed, m_tuning.TimeLimitSeconds))
        {
            return Fail(MissionOutcome.TimedOut);
        }

        if (!MissionRules.AllRelaysActivated(inputs.RelaysActivated, inputs.RelayCount))
        {
            return false;
        }

        Phase = MissionPhase.Extraction;
        ExtractionRemaining = m_tuning.ExtractionSeconds;
        return true;
    }

    private bool StepExtraction(MissionInputs inputs, float deltaTime)
    {
        Elapsed += deltaTime;
        if (MissionRules.IsSquadWiped(inputs.PlayerCount, inputs.AliveCount))
        {
            return Fail(MissionOutcome.SquadWiped);
        }

        if (MissionRules.IsTimedOut(Elapsed, m_tuning.TimeLimitSeconds))
        {
            return Fail(MissionOutcome.TimedOut);
        }

        ExtractionRemaining = MissionRules.ExtractionStep(ExtractionRemaining, inputs.AliveInsideZone > 0, deltaTime);
        if (!MissionRules.IsExtractionComplete(ExtractionRemaining) || inputs.AliveInsideZone <= 0)
        {
            return false;
        }

        Phase = MissionPhase.Complete;
        Outcome = MissionOutcome.Extracted;
        ExtractedCount = inputs.AliveInsideZone;
        return true;
    }

    private bool Fail(MissionOutcome outcome)
    {
        Phase = MissionPhase.Failed;
        Outcome = outcome;
        return true;
    }
}
}
