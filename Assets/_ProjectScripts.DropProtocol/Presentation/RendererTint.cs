using UnityEngine;

namespace DropProtocol
{
/// <summary>Tints a set of renderers through property blocks so shared materials stay untouched.</summary>
public sealed class RendererTint
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private readonly Renderer[] m_renderers;
    private readonly MaterialPropertyBlock m_block = new();
    private bool m_applied;

    public RendererTint(Renderer[] renderers)
    {
        m_renderers = renderers ?? System.Array.Empty<Renderer>();
    }

    public void Apply(Color color)
    {
        m_applied = true;
        foreach (var renderer in m_renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(m_block);
            m_block.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(m_block);
        }
    }

    public void Clear()
    {
        if (!m_applied)
        {
            return;
        }

        m_applied = false;
        foreach (var renderer in m_renderers)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }
        }
    }
}
}
