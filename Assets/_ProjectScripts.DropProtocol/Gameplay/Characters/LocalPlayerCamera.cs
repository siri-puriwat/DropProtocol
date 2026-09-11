using Unity.Cinemachine;
using UnityEngine;

namespace DropProtocol
{
/// <summary>
///     Points the top-down Cinemachine camera at whichever player character the local client owns.
///     Presentation only: the character never knows a camera exists.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public sealed class LocalPlayerCamera : MonoBehaviour
{
    private CinemachineCamera m_camera;

    private void Awake()
    {
        m_camera = GetComponent<CinemachineCamera>();
    }

    private void OnEnable()
    {
        NetworkPlayer.LocalPlayerSpawned += Follow;
        NetworkPlayer.LocalPlayerDespawned += Unfollow;

        if (NetworkPlayer.LocalPlayer != null)
        {
            Follow(NetworkPlayer.LocalPlayer);
        }
    }

    private void OnDisable()
    {
        NetworkPlayer.LocalPlayerSpawned -= Follow;
        NetworkPlayer.LocalPlayerDespawned -= Unfollow;
    }

    private void Follow(NetworkPlayer player)
    {
        m_camera.Target.TrackingTarget = player.transform;
    }

    private void Unfollow(NetworkPlayer player)
    {
        if (m_camera.Target.TrackingTarget == player.transform)
        {
            m_camera.Target.TrackingTarget = null;
        }
    }
}
}
