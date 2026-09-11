namespace DropProtocol
{
public enum MissionPhase
{
    Deployment,
    Active,
    Extraction,
    Complete,
    Failed
}

public enum MissionOutcome
{
    None,
    Extracted,
    SquadWiped,
    TimedOut
}
}
