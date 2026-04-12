using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace XRC.Projects.Axis
{
    /// <summary>
    /// Manages AR passthrough mode toggle for the camera.
    /// Allows switching between VR (solid background) and AR (transparent/passthrough) modes.
    /// </summary>
    public class PassthroughSetup : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("If not assigned, defaults to main camera in scene.")]
        private Camera m_Camera;

        [SerializeField]
        private bool m_EnablePassthroughOnStart = true;

        [SerializeField]
        private InputActionProperty m_TogglePassthrough;

        [SerializeField]
        [Tooltip("GameObjects to disable when passthrough is enabled (e.g., virtual environments).")]
        private List<GameObject> m_DisableOnPassthrough = new List<GameObject>();

        private bool m_IsPassthroughEnabled;
        private Color m_OriginalBackgroundColor;
        private ARCameraManager m_ARCameraManager;
        private ARSession m_ARSession;

        void Start()
        {
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
            }

            m_OriginalBackgroundColor = m_Camera.backgroundColor;
            m_ARCameraManager = m_Camera.gameObject.AddComponent<ARCameraManager>();
            m_ARSession = gameObject.AddComponent<ARSession>();

            if (m_EnablePassthroughOnStart)
            {
                EnablePassthrough();
            }
            else
            {
                DisablePassthrough();
            }
        }

        void OnEnable()
        {
            if (m_TogglePassthrough.action != null)
            {
                m_TogglePassthrough.action.Enable();
                m_TogglePassthrough.action.performed += OnTogglePassthrough;
            }
        }

        void OnDisable()
        {
            if (m_TogglePassthrough.action != null)
            {
                m_TogglePassthrough.action.performed -= OnTogglePassthrough;
                m_TogglePassthrough.action.Disable();
            }
        }

        /// <summary>
        /// Toggles passthrough mode on/off.
        /// </summary>
        public void TogglePassthrough()
        {
            if (m_IsPassthroughEnabled)
            {
                DisablePassthrough();
            }
            else
            {
                EnablePassthrough();
            }
        }

        private void OnTogglePassthrough(InputAction.CallbackContext context)
        {
            TogglePassthrough();
        }

        private void EnablePassthrough()
        {
            m_IsPassthroughEnabled = true;
            m_Camera.backgroundColor = new Color(0, 0, 0, 0);
            m_ARCameraManager.enabled = true;


            foreach (var obj in m_DisableOnPassthrough)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        private void DisablePassthrough()
        {
            m_IsPassthroughEnabled = false;
            m_ARCameraManager.enabled = false;
            m_Camera.backgroundColor = m_OriginalBackgroundColor;

            foreach (var obj in m_DisableOnPassthrough)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
    }
}
