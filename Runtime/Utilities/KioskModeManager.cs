using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Manages a kiosk mode for a scene by enabling or disabling a set of GameObjects.
    /// When kiosk mode is active, the configured objects are deactivated; when inactive, they are reactivated.
    /// Useful for restricting interactions in demo or exhibition contexts.
    /// </summary>
    public class KioskModeManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Objects that are deactivated when kiosk mode is on and reactivated when kiosk mode is off.")]
        private List<GameObject> m_DisabledInKioskModeObjects = new List<GameObject>();
        /// <summary>
        /// Objects that are deactivated when kiosk mode is on and reactivated when kiosk mode is off.
        /// </summary>
        public List<GameObject> disabledInKioskModeObjects
        {
            get => m_DisabledInKioskModeObjects;
            set => m_DisabledInKioskModeObjects = value;
        }

        [SerializeField]
        [Tooltip("Objects that are activated when kiosk mode is on and deactivated when kiosk mode is off.")]
        private List<GameObject> m_EnabledInKioskModeObjects = new List<GameObject>();
        /// <summary>
        /// Objects that are activated when kiosk mode is on and deactivated when kiosk mode is off.
        /// </summary>
        public List<GameObject> enabledInKioskModeObjects
        {
            get => m_EnabledInKioskModeObjects;
            set => m_EnabledInKioskModeObjects = value;
        }

        [SerializeField]
        [Tooltip("Input action used to toggle kiosk mode on and off.")]
        private InputActionProperty m_ToggleKioskModeAction;
        /// <summary>
        /// Input action used to toggle kiosk mode on and off.
        /// </summary>
        public InputActionProperty toggleKioskModeAction
        {
            get => m_ToggleKioskModeAction;
            set => m_ToggleKioskModeAction = value;
        }

        [SerializeField]
        [Tooltip("Whether kiosk mode should be active when the scene starts.")]
        private bool m_StartInKioskMode = false;
        /// <summary>
        /// Whether kiosk mode should be active when the scene starts.
        /// </summary>
        public bool startInKioskMode
        {
            get => m_StartInKioskMode;
            set => m_StartInKioskMode = value;
        }

        private bool m_IsInKioskMode;
        /// <summary>
        /// Current kiosk mode state. <c>true</c> when kiosk mode is active.
        /// </summary>
        public bool isInKioskMode => m_IsInKioskMode;

        /// <summary>
        /// Fired whenever kiosk mode is toggled. The argument is the new kiosk mode state.
        /// </summary>
        public event Action<bool> kioskModeChanged;

        private void Start()
        {
            ApplyKioskMode(m_StartInKioskMode);
        }

        private void OnEnable()
        {
            m_ToggleKioskModeAction.action.Enable();
            m_ToggleKioskModeAction.action.performed += OnToggleKioskMode;
        }

        private void OnDisable()
        {
            m_ToggleKioskModeAction.action.performed -= OnToggleKioskMode;
            m_ToggleKioskModeAction.action.Disable();
        }

        /// <summary>
        /// Enables kiosk mode, deactivating the configured objects.
        /// </summary>
        public void EnterKioskMode()
        {
            ApplyKioskMode(true);
        }

        /// <summary>
        /// Disables kiosk mode, reactivating the configured objects.
        /// </summary>
        public void ExitKioskMode()
        {
            ApplyKioskMode(false);
        }

        /// <summary>
        /// Toggles kiosk mode between on and off.
        /// </summary>
        public void ToggleKioskMode()
        {
            ApplyKioskMode(!m_IsInKioskMode);
        }

        private void OnToggleKioskMode(InputAction.CallbackContext context)
        {
            ToggleKioskMode();
        }

        private void ApplyKioskMode(bool isKioskModeOn)
        {
            m_IsInKioskMode = isKioskModeOn;
            foreach (var disabledObject in m_DisabledInKioskModeObjects)
            {
                if (disabledObject != null)
                {
                    disabledObject.SetActive(!isKioskModeOn);
                }
            }
            foreach (var enabledObject in m_EnabledInKioskModeObjects)
            {
                if (enabledObject != null)
                {
                    enabledObject.SetActive(isKioskModeOn);
                }
            }
            kioskModeChanged?.Invoke(m_IsInKioskMode);
        }
    }
}
