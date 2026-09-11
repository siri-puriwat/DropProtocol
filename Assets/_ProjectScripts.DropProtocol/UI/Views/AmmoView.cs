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
        if (m_weaponName != null)
        {
            m_weaponName.text = m_weapon.Definition != null ? m_weapon.Definition.DisplayName : string.Empty;
        }

        Apply();
    }

    protected override void OnUnbind(NetworkPlayer player)
    {
        if (m_weapon != null)
        {
            m_weapon.Ammo.OnValueChanged -= HandleAmmoChanged;
            m_weapon.IsReloading.OnValueChanged -= HandleReloadingChanged;
        }

        m_weapon = null;
    }

    private void HandleAmmoChanged(int previous, int current)
    {
        Apply();
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
