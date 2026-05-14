using System;
using UnityEngine;

namespace XRC.Toolkit.Core
{
    public interface IPrefabPublisher
    {
        public event Action<GameObject> onPrefabRegistered;
        public event Action<GameObject> onPrefabInstantiated;
    }
}
