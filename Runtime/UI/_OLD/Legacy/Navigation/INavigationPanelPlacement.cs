using System;
using UnityEngine;

namespace XRC.Toolkit.Core.Legacy
{
    /// <summary>
    /// Optional bridge for panel placement systems that need navigation refreshes
    /// without making the shared navigation implementation depend on a package.
    /// </summary>
    public interface INavigationPanelPlacement
    {
        bool IsPinned { get; }
        Transform PlacementTransform { get; }
        event Action PlacementStateChanged;
    }
}
