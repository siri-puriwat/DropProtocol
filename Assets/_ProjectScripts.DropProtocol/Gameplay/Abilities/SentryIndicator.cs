using Unity.Netcode;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Effects for a sentry: a puff when it finishes deploying, a flash per replicated shot and a hum while it
///     stands. The hum is state and re-applies on spawn so a late joiner hears it; the puff is an edge and only
///     plays on the transition. Presentation only.
/// </summary>
public sealed class SentryIndicator : NetworkBehaviour
{
    [SerializeField]
    private SentryTurret m_turret;

    [SerializeField]
    private GameObject m_deployVfx;

    [SerializeField]
    private GameObject m_muzzleFlash;

    [SerializeField]
    private GameObject m_impactVfx;

    [SerializeField]
    private SfxCue m_deployCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_fireCue = SfxCue.Default;

    [SerializeField]
    private SfxCue m_impactCue = SfxCue.Default;

    [SerializeField]
    private LoopFader m_hum;

    private void OnEnable()
    {
        if (m_turret == null)
        {
            return;
        }

        m_turret.IsDeployed.OnValueChanged += HandleDeployedChanged;
        m_turret.ShotFired += HandleShotFired;
        Apply();
    }

    private void OnDisable()
    {
        if (m_turret == null)
        {
            return;
        }

        m_turret.IsDeployed.OnValueChanged -= HandleDeployedChanged;
        m_turret.ShotFired -= HandleShotFired;
    }

    public override void OnNetworkSpawn()
    {
        Apply();
    }

    public override void OnNetworkDespawn()
    {
        if (m_hum != null)
        {
            m_hum.DetachAndFadeOut();
            m_hum = null;
        }
    }

    private void HandleDeployedChanged(bool previous, bool current)
    {
        if (current && !previous)
        {
            Vfx.Spawn(m_deployVfx, transform.position, Vector3.up);
            SfxPlayer.Play(m_deployCue, transform.position);
        }

        Apply();
    }

    private void HandleShotFired(Vector3 muzzle, Vector3 end, bool hit)
    {
        Vfx.Spawn(m_muzzleFlash, muzzle, end - muzzle);
        SfxPlayer.Play(m_fireCue, muzzle);
        if (hit)
        {
            Vfx.Spawn(m_impactVfx, end, muzzle - end);
            SfxPlayer.Play(m_impactCue, end);
        }
    }

    private void Apply()
    {
        if (m_hum != null && m_turret != null)
        {
            m_hum.SetPlaying(m_turret.IsDeployed.Value);
        }
    }
}
}
