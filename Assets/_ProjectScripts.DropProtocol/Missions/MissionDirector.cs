using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Server-side owner of the mission in the gameplay scene. <see cref="MissionState" /> decides the
///     phase from registry counts; this component feeds it, replicates the result for every HUD, and
///     drives the enemy director. Director settings are reapplied every frame because in-scene
///     NetworkObjects spawn in an undefined order and the director resets itself on spawn.
/// </summary>
public sealed class MissionDirector : NetworkBehaviour
{
    [SerializeField]
    private CommRelay[] m_relays = new CommRelay[0];

    [SerializeField]
    private ExtractionZone m_zone;

    [SerializeField]
    private EnemyDirector m_director;

    [SerializeField]
    private MissionTuning m_tuning = MissionTuning.Default;

    public NetworkVariable<MissionPhase> Phase = new();
    public NetworkVariable<MissionOutcome> Outcome = new();
    public NetworkVariable<int> RelayCount = new();
    public NetworkVariable<int> RelaysActivated = new();
    public NetworkVariable<float> DeployRemaining = new();
    public NetworkVariable<float> Elapsed = new();
    public NetworkVariable<float> TimeLimit = new();
    public NetworkVariable<float> ExtractionRemaining = new();
    public NetworkVariable<int> ExtractedCount = new();
    public NetworkVariable<int> EnemiesKilled = new();

    private MissionState m_state;

    /// <summary>The scene's mission, or null in the sandbox and in tests without one.</summary>
    public static MissionDirector Instance { get; private set; }

    public MissionTuning Tuning => m_tuning;
    public ExtractionZone Zone => m_zone;
    public IReadOnlyList<CommRelay> Relays => m_relays;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || m_state == null)
        {
            return;
        }

        if (!m_state.IsOver)
        {
            m_state.Step(ReadInputs(), Time.deltaTime);
            Publish();
        }

        ApplyDirector();
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        EnemyCharacter.Died -= HandleEnemyDied;
        base.OnDestroy();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    /// <summary>Tests configure in code before the object spawns.</summary>
    public void Configure(CommRelay[] relays, ExtractionZone zone, EnemyDirector director, MissionTuning tuning)
    {
        m_relays = relays;
        m_zone = zone;
        m_director = director;
        m_tuning = tuning;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        m_state = new MissionState(m_tuning);
        RelayCount.Value = m_relays.Length;
        TimeLimit.Value = m_tuning.TimeLimitSeconds;
        Publish();
        EnemyCharacter.Died += HandleEnemyDied;
        ApplyDirector();
    }

    public override void OnNetworkDespawn()
    {
        EnemyCharacter.Died -= HandleEnemyDied;
    }

    /// <summary>
    ///     Server-side objective for bots: the nearest unactivated relay while Active, the extraction
    ///     zone while extracting. False when there is nothing to do.
    /// </summary>
    public bool TryGetObjective(Vector3 origin, out Vector3 position, out bool needsInteract)
    {
        position = default;
        needsInteract = false;

        switch (Phase.Value)
        {
            case MissionPhase.Active:
                return TryGetNearestRelay(origin, out position, out needsInteract);
            case MissionPhase.Extraction when m_zone != null:
                position = m_zone.Centre;
                return true;
            default:
                return false;
        }
    }

    private bool TryGetNearestRelay(Vector3 origin, out Vector3 position, out bool needsInteract)
    {
        position = default;
        needsInteract = true;
        float nearest = float.PositiveInfinity;
        bool found = false;

        foreach (var relay in m_relays)
        {
            if (relay == null || relay.IsActivated.Value)
            {
                continue;
            }

            float distance = EnemyRules.FlatDistance(origin, relay.transform.position);
            if (distance < nearest)
            {
                nearest = distance;
                position = relay.transform.position;
                found = true;
            }
        }

        return found;
    }

    private MissionInputs ReadInputs()
    {
        int players = 0;
        int alive = 0;
        foreach (var player in NetworkPlayer.All)
        {
            players++;
            if (player.Health != null && !player.Health.IsDowned)
            {
                alive++;
            }
        }

        int activated = 0;
        foreach (var relay in m_relays)
        {
            if (relay != null && relay.IsActivated.Value)
            {
                activated++;
            }
        }

        return new MissionInputs
        {
            RelayCount = m_relays.Length,
            RelaysActivated = activated,
            PlayerCount = players,
            AliveCount = alive,
            AliveInsideZone = m_zone != null ? m_zone.CountAliveInside() : 0
        };
    }

    private void Publish()
    {
        Phase.Value = m_state.Phase;
        Outcome.Value = m_state.Outcome;
        DeployRemaining.Value = m_state.DeployRemaining;
        Elapsed.Value = m_state.Elapsed;
        ExtractionRemaining.Value = m_state.ExtractionRemaining;
        ExtractedCount.Value = m_state.ExtractedCount;

        int activated = 0;
        foreach (var relay in m_relays)
        {
            if (relay != null && relay.IsActivated.Value)
            {
                activated++;
            }
        }

        RelaysActivated.Value = activated;
    }

    private void ApplyDirector()
    {
        if (m_director == null)
        {
            return;
        }

        m_director.Running = MissionRules.DirectorShouldRun(m_state.Phase);
        m_director.Intensity = MissionRules.Intensity(m_state.Phase, RelaysActivated.Value,
            m_tuning.IntensityPerRelay, m_tuning.ExtractionIntensity);
    }

    private void HandleEnemyDied(EnemyCharacter enemy)
    {
        if (IsServer && IsSpawned)
        {
            EnemiesKilled.Value++;
        }
    }
}
}
