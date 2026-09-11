using UnityEngine;

namespace DropProtocol
{
/// <summary>Extraction area. No collider: standing inside is a flat-distance check the server runs.</summary>
public sealed class ExtractionZone : MonoBehaviour
{
    [SerializeField]
    [Min(0.1f)]
    private float m_radius = 4f;

    public float Radius => m_radius;
    public Vector3 Centre => transform.position;

    /// <summary>Tests configure in code.</summary>
    public void Configure(float radius)
    {
        m_radius = radius;
    }

    public int CountAliveInside()
    {
        int count = 0;
        foreach (var player in NetworkPlayer.All)
        {
            if (player.Health == null || player.Health.IsDowned)
            {
                continue;
            }

            if (MissionRules.IsInside(player.transform.position, transform.position, m_radius))
            {
                count++;
            }
        }

        return count;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, m_radius);
    }
}
}
