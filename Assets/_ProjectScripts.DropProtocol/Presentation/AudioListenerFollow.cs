using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Keeps the listener at the local player so distance and panning are measured from the character, not
///     from a camera 18 m away. Only the position moves: the object stays a child of the camera so the
///     stereo field follows the fixed top-down view, never the aim yaw. Falls back to the camera while no
///     local player exists, so every scene keeps exactly one listener.
/// </summary>
[RequireComponent(typeof(AudioListener))]
public sealed class AudioListenerFollow : MonoBehaviour
{
    [SerializeField]
    private Transform m_fallback;

    [SerializeField]
    private Vector3 m_offset = new(0f, 1f, 0f);

    private void LateUpdate()
    {
        var player = NetworkPlayer.LocalPlayer;
        if (player != null)
        {
            transform.position = player.transform.position + m_offset;
        }
        else if (m_fallback != null)
        {
            transform.position = m_fallback.position;
        }
    }
}
}
