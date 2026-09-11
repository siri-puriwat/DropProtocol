using System;
using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Static support-protocol configuration. Per-character state (entered prefix, cooldowns) lives in
///     <see cref="ProtocolState" />; the payload prefab owns whatever happens after the call-in.
/// </summary>
[CreateAssetMenu(menuName = "DropProtocol/Protocol Definition", fileName = "NewProtocol")]
public sealed class ProtocolDefinition : ScriptableObject
{
    [SerializeField]
    private string m_displayName = "Protocol";

    [Tooltip("Directions the player enters. No loadout entry may be a prefix of another.")]
    [SerializeField]
    private ProtocolDirection[] m_sequence = Array.Empty<ProtocolDirection>();

    [SerializeField]
    [Min(0f)]
    private float m_cooldownSeconds = 30f;

    [Tooltip("Spawned by the host at the call-in point.")]
    [SerializeField]
    private NetworkObject m_payload;

    [SerializeField]
    private AudioClip m_callClip;

    public string DisplayName => m_displayName;
    public ProtocolDirection[] Sequence => m_sequence;
    public float CooldownSeconds => m_cooldownSeconds;
    public NetworkObject Payload => m_payload;
    public AudioClip CallClip => m_callClip;

    public static ProtocolDefinition Create(string displayName, ProtocolDirection[] sequence, float cooldownSeconds,
        NetworkObject payload)
    {
        var definition = CreateInstance<ProtocolDefinition>();
        definition.m_displayName = displayName;
        definition.m_sequence = sequence;
        definition.m_cooldownSeconds = cooldownSeconds;
        definition.m_payload = payload;
        return definition;
    }
}
}
