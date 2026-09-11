using UnityEngine;

namespace DropProtocol
{
/// <summary>Effects for a sentry: a puff when it finishes deploying and a flash per replicated shot. Presentation only.</summary>
public sealed class SentryIndicator : MonoBehaviour
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
    private AudioClip m_deployClip;

    [SerializeField]
    private AudioClip m_fireClip;

    private void OnEnable()
    {
        if (m_turret == null)
        {
            return;
        }

        m_turret.IsDeployed.OnValueChanged += HandleDeployedChanged;
        m_turret.ShotFired += HandleShotFired;
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

    private void HandleDeployedChanged(bool previous, bool current)
    {
        if (current && !previous)
        {
            Vfx.Spawn(m_deployVfx, transform.position, Vector3.up);
            SfxPlayer.Play(m_deployClip, transform.position);
        }
    }

    private void HandleShotFired(Vector3 muzzle, Vector3 end, bool hit)
    {
        Vfx.Spawn(m_muzzleFlash, muzzle, end - muzzle);
        SfxPlayer.Play(m_fireClip, muzzle);
        if (hit)
        {
            Vfx.Spawn(m_impactVfx, end, muzzle - end);
        }
    }
}
}
