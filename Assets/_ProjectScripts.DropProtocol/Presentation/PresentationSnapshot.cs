using UnityEngine;

namespace DropProtocol
{
/// <summary>What the animator was last fed; lets tests assert presentation without an Animator.</summary>
public struct PresentationSnapshot
{
    public Vector2 Move;
    public bool IsDowned;
    public bool IsReloading;
    public bool IsInteracting;
}
}
