using UnityEngine;
using UnityEngine.InputSystem;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Abstract base class for edit tool input handlers. Automatically detects and routes
    /// to EditObjectProvider if present, otherwise routes directly to the IEditTool component.
    /// Supports both toggle mode (single button) and separate enter/exit actions (different buttons).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides automatic routing logic that adapts to two setup configurations:
    /// </para>
    /// <para>
    /// <b>Without EditObjectProvider:</b> Input actions route directly to IEditTool.StartRun()/StopRun().
    /// Edit object must be assigned manually in inspector. No snap-back or grab management.
    /// </para>
    /// <para>
    /// <b>With EditObjectProvider:</b> Input actions route to EditObjectProvider.StartRun()/StopRun(),
    /// which handles snap-back, grab disabling, and then calls IEditTool.StartRun()/StopRun().
    /// Edit object is set automatically when grabbed.
    /// </para>
    /// <para>
    /// Input binding options:
    /// - Toggle mode: Single button toggles edit mode on/off
    /// - Separate actions: Different buttons for explicit enter and exit control
    /// - Mixed mode: Can use both toggle and separate buttons simultaneously
    /// </para>
    /// </remarks>
    public abstract class BaseEditToolInput : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField]
        [Tooltip("Optional: Toggle edit mode on/off with a single button")]
        protected InputActionProperty m_ToggleEditModeAction;

        [SerializeField]
        [Tooltip("Optional: Enter edit mode (use instead of toggle for explicit control with separate buttons)")]
        protected InputActionProperty m_EnterEditModeAction;

        [SerializeField]
        [Tooltip("Optional: Exit edit mode (use instead of toggle for explicit control with separate buttons)")]
        protected InputActionProperty m_ExitEditModeAction;

        private EditObjectProvider m_EditObjectProvider;
        private IEditTool m_EditTool;

        /// <summary>
        /// Initializes component references and subscribes to input actions.
        /// Detects both IEditTool and EditObjectProvider automatically.
        /// </summary>
        protected virtual void Start()
        {
            // Get the IEditTool component (works for ColorLogic, ScaleLogic, MeshLogic, etc.)
            m_EditTool = GetComponent<IEditTool>();
            if (m_EditTool == null)
            {
                Debug.LogError($"{GetType().Name}: No IEditTool component found on {gameObject.name}!");
                enabled = false;
                return;
            }

            // Check for EditObjectProvider (optional - provides snap-back and grab management)
            m_EditObjectProvider = GetComponent<EditObjectProvider>();

            // Subscribe to toggle action if assigned
            if (m_ToggleEditModeAction.action != null)
            {
                m_ToggleEditModeAction.action.performed += OnToggleEditMode;
            }

            // Subscribe to enter action if assigned
            if (m_EnterEditModeAction.action != null)
            {
                m_EnterEditModeAction.action.performed += OnEnterEditMode;
            }

            // Subscribe to exit action if assigned
            if (m_ExitEditModeAction.action != null)
            {
                m_ExitEditModeAction.action.performed += OnExitEditMode;
            }
        }

        /// <summary>
        /// Unsubscribe from input actions when destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            // Unsubscribe from all actions
            if (m_ToggleEditModeAction.action != null)
            {
                m_ToggleEditModeAction.action.performed -= OnToggleEditMode;
            }

            if (m_EnterEditModeAction.action != null)
            {
                m_EnterEditModeAction.action.performed -= OnEnterEditMode;
            }

            if (m_ExitEditModeAction.action != null)
            {
                m_ExitEditModeAction.action.performed -= OnExitEditMode;
            }
        }

        /// <summary>
        /// Enable all assigned input actions when the component is enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            // Enable all assigned actions
            m_ToggleEditModeAction.action?.Enable();
            m_EnterEditModeAction.action?.Enable();
            m_ExitEditModeAction.action?.Enable();
        }

        /// <summary>
        /// Disable all assigned input actions when the component is disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            // Disable all assigned actions
            m_ToggleEditModeAction.action?.Disable();
            m_EnterEditModeAction.action?.Disable();
            m_ExitEditModeAction.action?.Disable();
        }

        /// <summary>
        /// Called when toggle edit mode action is performed.
        /// Routes to EditObjectProvider if present, otherwise to IEditTool.
        /// </summary>
        private void OnToggleEditMode(InputAction.CallbackContext context)
        {
            if (m_EditObjectProvider != null)
            {
                m_EditObjectProvider.ToggleRun();
            }
            else
            {
                m_EditTool?.ToggleRun();
            }
        }

        /// <summary>
        /// Called when enter edit mode action is performed.
        /// Routes to EditObjectProvider if present, otherwise to IEditTool.
        /// </summary>
        private void OnEnterEditMode(InputAction.CallbackContext context)
        {
            if (m_EditObjectProvider != null)
            {
                m_EditObjectProvider.StartRun();
            }
            else
            {
                m_EditTool?.StartRun();
            }
        }

        /// <summary>
        /// Called when exit edit mode action is performed.
        /// Routes to EditObjectProvider if present, otherwise to IEditTool.
        /// </summary>
        private void OnExitEditMode(InputAction.CallbackContext context)
        {
            if (m_EditObjectProvider != null)
            {
                m_EditObjectProvider.StopRun();
            }
            else
            {
                m_EditTool?.StopRun();
            }
        }
    }
}
