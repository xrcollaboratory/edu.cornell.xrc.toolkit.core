using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace XRC.Toolkit.Core.Utilities
{
    /// <summary>
    /// Simple script for toggling game objects based on user input.
    /// Used for enabling and disabling UIs etc.
    /// </summary>
    public class ToggleObjects : MonoBehaviour
    {
        [SerializeField]
        List<GameObject> m_ToggleObjects = new List<GameObject>();

        [SerializeField]
        private InputActionProperty m_ToggleAction;

        void Start()
        {
            m_ToggleAction.action.performed += (ctx) =>
            {
                foreach (var toggleObject in m_ToggleObjects)
                {
                    toggleObject.SetActive(!toggleObject.activeSelf);
                }
            };
        }

        private void OnEnable()
        {
            m_ToggleAction.action.Enable();
        }

        private void OnDisable()
        {
            m_ToggleAction.action.Disable();
        }
    }
}
