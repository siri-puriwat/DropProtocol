using UnityEngine;
using UnityEngine.EventSystems;

namespace DropProtocol
{
/// <summary>Hover and press sounds for a uGUI button, played through the scene UiSfx.</summary>
public sealed class ButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField]
    private UiSfx m_sfx;

    [SerializeField]
    private AudioClip m_hoverClip;

    [SerializeField]
    private AudioClip m_clickClip;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Play(m_hoverClip);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Play(m_clickClip);
    }

    private void Play(AudioClip clip)
    {
        if (m_sfx != null)
        {
            m_sfx.PlayUi(clip);
        }
    }
}
}
