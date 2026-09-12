using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Presentation: colours a marker under the character and skins the body by squad slot so players can
///     tell each other apart from the top-down view. The body takes a per-slot material rather than a
///     property block because the hit flash clears property blocks on every renderer under the visual.
///     Reads replicated state (<see cref="NetworkPlayer.PlayerIndex" />) and never influences gameplay.
/// </summary>
[RequireComponent(typeof(NetworkPlayer))]
public sealed class PlayerTint : NetworkBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    private Renderer m_marker;

    [Tooltip("One colour per squad slot, indexed by PlayerIndex.")]
    [SerializeField]
    private Color[] m_slotColors =
    {
        new(0.25f, 0.65f, 1f),
        new(1f, 0.85f, 0.25f),
        new(0.35f, 0.9f, 0.45f),
        new(1f, 0.4f, 0.35f)
    };

    [SerializeField]
    private Color m_unassignedColor = new(0.6f, 0.6f, 0.6f);

    [Tooltip("One skin material per squad slot, indexed by PlayerIndex; empty keeps the model material.")]
    [SerializeField]
    private Material[] m_slotMaterials = System.Array.Empty<Material>();

    [SerializeField]
    private Renderer[] m_body = System.Array.Empty<Renderer>();

    private NetworkPlayer m_player;
    private MaterialPropertyBlock m_block;

    public Color CurrentColor { get; private set; }

    private void Awake()
    {
        m_player = GetComponent<NetworkPlayer>();
        m_block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        m_player.PlayerIndex.OnValueChanged += HandleIndexChanged;
        Apply(m_player.PlayerIndex.Value);
    }

    private void OnDisable()
    {
        m_player.PlayerIndex.OnValueChanged -= HandleIndexChanged;
    }

    public override void OnNetworkSpawn()
    {
        Apply(m_player.PlayerIndex.Value);
    }

    private void HandleIndexChanged(int previous, int current)
    {
        Apply(current);
    }

    private void Apply(int slot)
    {
        var color = slot >= 0 && slot < m_slotColors.Length ? m_slotColors[slot] : m_unassignedColor;
        CurrentColor = color;

        var skin = m_slotMaterials != null && slot >= 0 && slot < m_slotMaterials.Length ? m_slotMaterials[slot] : null;
        if (skin != null && m_body != null)
        {
            foreach (var renderer in m_body)
            {
                if (renderer != null)
                {
                    renderer.sharedMaterial = skin;
                }
            }
        }

        if (m_marker == null)
        {
            return;
        }

        m_marker.GetPropertyBlock(m_block);
        m_block.SetColor(BaseColorId, color);
        m_marker.SetPropertyBlock(m_block);
    }
}
}
