using System;

namespace DropProtocol
{
/// <summary>
///     Server-side <see cref="IPlayerCommandSource" /> fed by a remote owner's replicated commands. It hands
///     out the most recent command until it goes stale, so a client that freezes or loses packets does not
///     leave its character walking forever. The clock is injected so the rule is testable without Unity.
/// </summary>
public sealed class NetworkCommandSource : IPlayerCommandSource
{
    public const double DefaultStaleSeconds = 0.5;

    private readonly Func<double> m_clock;
    private readonly double m_staleSeconds;

    private PlayerCommand m_latest;
    private double m_receivedAt = double.NegativeInfinity;

    public NetworkCommandSource(Func<double> clock, double staleSeconds = DefaultStaleSeconds)
    {
        m_clock = clock ?? throw new ArgumentNullException(nameof(clock));
        m_staleSeconds = staleSeconds;
    }

    /// <summary>Time in seconds since the last command arrived, or infinity when none has.</summary>
    public double SecondsSinceLastCommand => m_clock() - m_receivedAt;

    public PlayerCommand GetCommand()
    {
        return SecondsSinceLastCommand > m_staleSeconds ? PlayerCommand.None : m_latest;
    }

    public void Push(PlayerCommand command)
    {
        m_latest = command;
        m_receivedAt = m_clock();
    }
}
}
