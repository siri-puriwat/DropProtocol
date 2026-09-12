using System.Collections.Generic;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Added by <see cref="Vfx" /> to every spawned effect. Parks the object in its pool when the particle
///     system reports it stopped, which requires the Callback stop action; it is forced here so a prefab
///     left on Destroy still pools.
/// </summary>
public sealed class PooledVfx : MonoBehaviour
{
    private Stack<PooledVfx> m_pool;
    private ParticleSystem m_system;

    public void Bind(Stack<PooledVfx> pool)
    {
        m_pool = pool;
        m_system = GetComponent<ParticleSystem>();
        if (m_system != null)
        {
            var main = m_system.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
    }

    public void Play()
    {
        gameObject.SetActive(true);
        if (m_system != null)
        {
            m_system.Play(true);
        }
    }

    private void OnParticleSystemStopped()
    {
        gameObject.SetActive(false);
        m_pool?.Push(this);
    }
}
}
