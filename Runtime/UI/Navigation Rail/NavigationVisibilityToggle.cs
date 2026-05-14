using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Toggles the visibility of the navigation rail and its panels without disabling
    /// any GameObjects, so each UIDocument's visual tree and any C# event subscriptions
    /// wired to it stay alive across hide/show cycles. Sits on the parent GameObject
    /// that holds the navigation rail and panel children as descendants.
    /// </summary>
    public sealed class NavigationVisibilityToggle : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Input action that toggles the navigation UI on and off.")]
        private InputActionProperty m_ToggleAction;

        [SerializeField]
        [Tooltip("Navigation rail controller that drives panel selection. If unset, located via GetComponentInChildren at runtime.")]
        private NavigationRailController m_Controller;

        [SerializeField]
        [Tooltip("Whether the navigation UI starts visible when this component first enables.")]
        private bool m_StartVisible = true;

        private UIDocument m_NavRailDocument;
        private bool m_IsVisible;

        /// <summary>
        /// Whether the navigation UI is currently visible.
        /// </summary>
        public bool isVisible => m_IsVisible;

        /// <summary>
        /// Raised after the visibility state changes, with the new state.
        /// </summary>
        public event Action<bool> visibilityChanged;

        private void Awake()
        {
            if (m_Controller == null)
                m_Controller = GetComponentInChildren<NavigationRailController>(true);

            if (m_Controller != null)
                m_NavRailDocument = m_Controller.GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var action = m_ToggleAction.action;
            if (action != null)
            {
                action.performed += OnTogglePerformed;
                action.Enable();
            }

            SetVisible(m_StartVisible);
        }

        private void OnDisable()
        {
            var action = m_ToggleAction.action;
            if (action != null)
                action.performed -= OnTogglePerformed;
        }

        /// <summary>
        /// Flip the current visibility state.
        /// </summary>
        public void Toggle() => SetVisible(!m_IsVisible);

        /// <summary>
        /// Show or hide the navigation UI. Uses UI Toolkit display + collider toggling
        /// so the underlying UIDocuments stay enabled and never rebuild their visual trees.
        /// </summary>
        public void SetVisible(bool visible)
        {
            m_IsVisible = visible;

            ApplyDocumentVisibility(m_NavRailDocument, visible);

            if (m_Controller != null)
            {
                if (visible)
                {
                    var panelToRestore = m_Controller.activePanel;
                    if (panelToRestore == null && m_Controller.panels.Count > 0)
                        panelToRestore = m_Controller.panels[0];

                    if (panelToRestore != null)
                        m_Controller.SelectPanel(panelToRestore);
                }
                else
                {
                    foreach (var panel in m_Controller.panels)
                    {
                        if (panel != null && panel.PanelDocument != null)
                            ApplyDocumentVisibility(panel.PanelDocument, false);
                    }
                }
            }

            visibilityChanged?.Invoke(visible);
        }

        private static void ApplyDocumentVisibility(UIDocument document, bool visible)
        {
            if (document == null)
                return;

            var root = document.rootVisualElement;
            if (root != null)
            {
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
            }

            foreach (var documentCollider in document.GetComponentsInChildren<Collider>(true))
                documentCollider.enabled = visible;
        }

        private void OnTogglePerformed(InputAction.CallbackContext _) => Toggle();
    }
}
