using TMPro;
using UnityEngine;

namespace DropProtocol
{
public sealed class AmmoView : HudView
{
    [SerializeField]
    private TMP_Text m_label;

    [SerializeField]
    private TMP_Text m_weaponName;

    [SerializeField]
    private UiSfx m_sfx;

    [SerializeField]
    private AudioClip m_hitClip;

    [SerializeField]
    private AudioClip m_emptyClip;

    [SerializeField]
    private AudioClip m_lowAmmoClip;

    [SerializeField]
    [Min(0)]
    private int m_lowAmmoThreshold = 5;

    private WeaponController m_weapon;

    public string Label => m_label != null ? m_label.text : string.Empty;

    protected override void OnBind(NetworkPlayer player)
    {
        m_weapon = player.GetComponent<WeaponController>();
        if (m_weapon == null)
        {
            return;
        }

        m_weapon.Ammo.OnValueChanged += HandleAmmoChanged;
        m_weapon.IsReloading.OnValueChanged += HandleReloadingChanged;
        m_weapon.DamageConfirmed += HandleDamageConfirmed;
        m_weapon.WeaponChanged += HandleWeaponChanged;
        HandleWeaponChanged(m_weapon.Definition);
    }

    protected override void OnUnbind(NetworkPlayer player)
    {
        if (m_weapon != null)
        {
            m_weapon.Ammo.OnValueChanged -= HandleAmmoChanged;
            m_weapon.IsReloading.OnValueChanged -= HandleReloadingChanged;
            m_weapon.DamageConfirmed -= HandleDamageConfirmed;
            m_weapon.WeaponChanged -= HandleWeaponChanged;
        }

        m_weapon = null;
    }

    private void HandleWeaponChanged(WeaponDefinition definition)
    {
        if (m_weaponName != null)
        {
            m_weaponName.text = definition != null ? definition.DisplayName : string.Empty;
        }

        Apply();
    }

    private void HandleAmmoChanged(int previous, int current)
    {
        if (AmmoRules.MagazineEmptied(previous, current))
        {
            PlayUi(m_emptyClip);
        }
        else if (AmmoRules.CrossedLow(previous, current, m_lowAmmoThreshold))
        {
            PlayUi(m_lowAmmoClip);
        }

        Apply();
    }

    private void HandleDamageConfirmed()
    {
        PlayUi(m_hitClip);
    }

    private void PlayUi(AudioClip clip)
    {
        if (m_sfx != null)
        {
            m_sfx.PlayUi(clip);
        }
    }

    private void HandleReloadingChanged(bool previous, bool current)
    {
        Apply();
    }

    private void Apply()
    {
        if (m_label == null)
        {
            return;
        }

        int magazine = m_weapon.Definition != null ? m_weapon.Definition.MagazineSize : 0;
        m_label.text = HudFormat.AmmoLabel(m_weapon.Ammo.Value, magazine, m_weapon.IsReloading.Value);
    }
}
}
