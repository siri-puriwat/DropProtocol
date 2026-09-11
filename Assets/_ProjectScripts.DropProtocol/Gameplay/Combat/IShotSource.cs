using System;
using UnityEngine;

namespace DropProtocol
{
/// <summary>Anything that fires replicated hitscan shots; <see cref="HitscanTracer" /> draws them.</summary>
public interface IShotSource
{
    /// <summary>Muzzle position, shot end point, and whether something was hit. Raised on every peer.</summary>
    event Action<Vector3, Vector3, bool> ShotFired;
}
}
