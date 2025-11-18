using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Provides object selection management for editing operations. Tracks grabbed objects,
    /// saves their initial positions for snap-back, and coordinates with IEditTool implementations
    /// to ensure proper initialization order (snap-back → set object → fire events → start tool).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Works with any IEditTool implementation (ScaleLogic, ColorLogic, MeshLogic, etc.) to provide
    /// consistent object selection behavior. Supports both toggle mode (single button) and separate
    /// enter/exit actions (different buttons) for flexible input configuration.
    /// </para>
    /// <para>
    /// Input options:
    /// - Toggle mode: Single button toggles edit mode on/off
    /// - Separate actions: Different buttons for explicit enter and exit control
    /// - Mixed mode: Can use both toggle and separate buttons simultaneously
    /// </para>
    /// </remarks>
    public class EditObjectProvider : MonoBehaviour {
        // Serialized Fields

        [Header("Interactor Reference")]
        [SerializeField]
        [Tooltip("The interactor that will grab objects for editing. When this interactor grabs an object, the system will track its initial position for snap-back functionality.")]
        private XRBaseInteractor m_Interactor;

        [Header("Edit Tool Reference")]
        [SerializeField]
        [Tooltip("The GameObject containing an IEditTool component that will handle edit operations. If not assigned, the system will look for an IEditTool component on this same GameObject.")]
        private GameObject m_EditToolObject;

        [Header("Selection Behavior")]
        [SerializeField]
        [Tooltip("If true, automatically enters edit mode when an object is selected. If false, user must manually call StartRun on the edit tool.")]
        private bool m_StartEditOnSet = true;

        [Header("Snap-Back Settings")]
        [SerializeField]
        [Tooltip("Whether to automatically restore objects to their grab position when entering edit mode.")]
        private bool m_SnapBackOnEditMode = true;

        // Private Fields

        private IEditTool m_EditTool;
        private IXRSelectInteractable m_CurrentInteractable;
        private GameObject m_CurrentEditObject;

        // Snap-back transform for the grabbed object
        private Vector3 m_SnapBackPosition;
        private Quaternion m_SnapBackRotation;
        private Vector3 m_SnapBackScale;

        private bool m_IsRunning = false;

        // Public Properties

        public GameObject currentEditObject => m_CurrentEditObject;
        public bool isRunning => m_IsRunning;

        // Unity Lifecycle

        void Start()
        {
            // Get edit tool reference
            if (m_EditToolObject != null)
            {
                m_EditTool = m_EditToolObject.GetComponent<IEditTool>();
                if (m_EditTool == null)
                {
                    Debug.LogError($"EditObjectProvider: No IEditTool component found on {m_EditToolObject.name}");
                }
            }
            else
            {
                m_EditTool = GetComponent<IEditTool>();
                if (m_EditTool == null)
                {
                    Debug.LogWarning("EditObjectProvider: No edit tool object assigned and no IEditTool found on this GameObject");
                }
            }

            // Subscribe to primary interactor events for tracking grabbed objects
            if (m_Interactor != null)
            {
                m_Interactor.selectEntered.AddListener(OnSelectEntered);
                Debug.Log($"EditObjectProvider subscribed to {m_Interactor.name} grab events");
            }
            else
            {
                Debug.LogWarning("No primary interactor assigned to EditObjectProvider. Object selection may not work correctly.");
            }
        }

        void Update()
        {
            // Check if edit object was destroyed
            CheckObjectDestroyed();

            // Note: Automatic snap-back is NOT performed here to avoid timing issues
            // with handle position caching. Snap-back is only applied when explicitly
            // called via StartRun() for manual input action mode.
        }

        void OnDestroy()
        {
            // Unsubscribe from primary interactor events
            if (m_Interactor != null)
            {
                m_Interactor.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        // Event Handlers

        // Called when the primary interactor grabs any object - saves its current position for snap-back
        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            var grabbedObject = args.interactableObject.transform.gameObject;

            // TODO: This is a very poor approach, fix
            if (grabbedObject.name.ToLower().Contains("handle"))    
            {
                return;
            }

            // Store interactable and save snap-back position
            m_CurrentInteractable = args.interactableObject;
            m_SnapBackPosition = grabbedObject.transform.position;
            m_SnapBackRotation = grabbedObject.transform.rotation;
            m_SnapBackScale = grabbedObject.transform.localScale;
        }

        // Handles cases when the edit object has been destroyed while the tool is running
        private void CheckObjectDestroyed()
        {
            if (m_CurrentEditObject == null && m_IsRunning)
            {
                m_IsRunning = false;
            }
        }

        // Manual Selection Methods (for Input Action mode)

        /// <summary>
        /// Enters edit mode and provides the edit object based on the recently selected interactable.
        /// Implements correct ordering: snap-back → set editObject → fire events → EnterEditMode()
        /// This ensures handles are created at the correct snap-back position.
        /// </summary>
        public void EnterEditMode()
        {
            m_IsRunning = true;

            if (m_Interactor != null && m_Interactor.hasSelection)
            {
                // Get the most recently selected interactable
                var interactables = m_Interactor.interactablesSelected;
                m_CurrentInteractable = interactables[interactables.Count - 1];

                // Get the grabbed object
                var selectedObject = m_CurrentInteractable.transform.gameObject;

                // Apply snap-back FIRST (before setting edit object)
                if (m_SnapBackOnEditMode)
                {
                    selectedObject.transform.position = m_SnapBackPosition;
                    selectedObject.transform.rotation = m_SnapBackRotation;
                    selectedObject.transform.localScale = m_SnapBackScale;
                }

                // Set edit object reference (does NOT fire events)
                m_CurrentEditObject = selectedObject;
                if (m_EditTool != null)
                {
                    m_EditTool.editObject = selectedObject;
                }

                // Fire editModeEntered event (handles created at correct snap-back position)
                if (m_EditTool != null)
                {
                    m_EditTool.OnEditObjectChanging(selectedObject);
                }

                // Disable grabbing
                if (m_CurrentInteractable is XRGrabInteractable grabInteractable)
                {
                    m_Interactor.interactionManager.CancelInteractableSelection(m_CurrentInteractable);
                    grabInteractable.enabled = false;
                }

                // Enter edit mode if configured
                if (m_EditTool != null && m_StartEditOnSet)
                {
                    m_EditTool.EnterEditMode();
                }
            }
            else
            {
                m_IsRunning = false;
            }
        }

        public void ExitEditMode()
        {
            m_IsRunning = false;

            // Exit edit mode FIRST (even if we don't have a current object)
            // This handles cases where tool auto-started with pre-assigned object
            if (m_EditTool != null && m_EditTool.isInEditMode)
            {
                m_EditTool.ExitEditMode();
            }

            // Then handle current object cleanup
            if (m_CurrentEditObject == null) return;

            // Re-enable grabbing
            if (m_CurrentEditObject.TryGetComponent<XRGrabInteractable>(out var grabInteractable))
            {
                grabInteractable.enabled = true;
            }
        }
        public void ToggleEditMode()
        {
            if (m_IsRunning)
            {
                ExitEditMode();
            }
            else
            {
                EnterEditMode();
            }
        }
    }
}
