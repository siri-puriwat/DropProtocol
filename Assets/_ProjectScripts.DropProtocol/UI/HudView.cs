using UnityEngine;

namespace DropProtocol
{
/// <summary>A HUD element bound to the local player by <see cref="HudController" />. Reads replicated state only.</summary>
public abstract class HudView : MonoBehaviour
{
    protected NetworkPlayer Player { get; private set; }

    public void Bind(NetworkPlayer player)
    {
        Unbind();
        Player = player;
        if (player != null)
        {
            OnBind(player);
        }
    }

    public void Unbind()
    {
        if (Player != null)
        {
            OnUnbind(Player);
        }

        Player = null;
    }

    protected virtual void OnBind(NetworkPlayer player)
    {
    }

    protected virtual void OnUnbind(NetworkPlayer player)
    {
    }

    protected virtual void OnDisable()
    {
        Unbind();
    }
}
}
