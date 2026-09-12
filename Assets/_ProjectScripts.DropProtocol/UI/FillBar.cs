using UnityEngine;
using UnityEngine.UI;

namespace DropProtocol
{
/// <summary>
///     Horizontal bar whose sliced fill is sized by its anchors, the way the uGUI Slider does it.
///     <see cref="Image.fillAmount" /> would stretch the caps of a 9-sliced sprite; anchors keep them round.
/// </summary>
public sealed class FillBar : MonoBehaviour
{
    [SerializeField]
    private Image m_fill;

    public float Fraction => m_fill != null ? m_fill.rectTransform.anchorMax.x : 0f;

    public void SetFraction(float fraction)
    {
        if (m_fill == null)
        {
            return;
        }

        fraction = Mathf.Clamp01(fraction);
        RectTransform rect = m_fill.rectTransform;
        Vector2 max = rect.anchorMax;
        if (Mathf.Approximately(max.x, fraction))
        {
            return;
        }

        max.x = fraction;
        rect.anchorMax = max;
    }

    public void SetColor(Color color)
    {
        if (m_fill != null && m_fill.color != color)
        {
            m_fill.color = color;
        }
    }
}
}
