using System;
using UnityEngine;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Abstract base class for edit tool logic components. Provides common infrastructure
    /// for managing edit mode state, events, and lifecycle. Derived classes implement
    /// tool-specific editing behavior (color, scale, mesh, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class handles the common patterns shared by all edit tools:
    /// - Edit object management and lifecycle
    /// - Edit mode state tracking (isRunning, isInEditMode)
    /// - Event system for widgets to respond to state changes
    /// - Auto-start behavior when edit object is assigned at startup
    /// </para>
    /// <para>
    /// Derived classes must implement:
    /// - StartRun(): Tool-specific logic for entering edit mode
    /// - StopRun(): Tool-specific logic for exiting edit mode
    /// - OnEditObjectChanging(): Tool-specific initialization when edit object changes
    /// </para>
    /// </remarks>
    public abstract class BaseEditToolLogic : MonoBehaviour, IEditTool
    {
        // Common Fields

        [SerializeField]
        protected GameObject m_EditObject;

        protected bool m_IsRunning;
        protected bool m_IsInEditMode;

        // Common Events

        /// <summary>
        /// Fired when edit mode is entered. Widgets should create handles/visualizations in response.
        /// </summary>
        public event Action editModeEntered;

        /// <summary>
        /// Fired when edit mode is exited. Widgets should destroy handles/visualizations in response.
        /// </summary>
        public event Action editModeExited;

        // Common Properties

        /// <summary>
        /// The GameObject being edited. Setting this property updates the edit object reference
        /// but does not automatically enter edit mode.
        /// </summary>
        public virtual GameObject editObject
        {
            get => m_EditObject;
            set
            {
                if (m_EditObject == null || value != m_EditObject)
                {
                    m_EditObject = value;
                    OnEditObjectChanging(value);
                }
            }
        }

        /// <summary>
        /// Whether the edit tool is currently running.
        /// </summary>
        public bool isRunning => m_IsRunning;

        /// <summary>
        /// Whether the edit tool is currently in edit mode.
        /// </summary>
        public bool isInEditMode => m_IsInEditMode;

        // Unity Lifecycle

        /// <summary>
        /// If editObject is manually assigned in inspector, automatically enter edit mode.
        /// Derived classes can override to add tool-specific initialization.
        /// </summary>
        protected virtual void Start()
        {
            // If editObject is manually assigned in inspector, automatically enter edit mode
            if (m_EditObject != null)
            {
                EnterEditMode();
            }
        }

        // IEditTool Implementation

        /// <summary>
        /// Enters edit mode. Derived classes must implement tool-specific behavior.
        /// Typically sets m_IsRunning and m_IsInEditMode to true and fires events.
        /// </summary>
        public abstract void EnterEditMode();

        /// <summary>
        /// Exits edit mode. Derived classes must implement tool-specific cleanup behavior.
        /// Typically sets m_IsRunning and m_IsInEditMode to false and fires events.
        /// </summary>
        public abstract void ExitEditMode();

        /// <summary>
        /// Toggles edit mode on or off.
        /// This is a concrete implementation that derived classes typically don't need to override.
        /// </summary>
        public virtual void ToggleEditMode()
        {
            if (m_IsInEditMode)
            {
                ExitEditMode();
            }
            else
            {
                EnterEditMode();
            }
        }

        // IEditTool Implementation

        /// <summary>
        /// Called when the edit object is about to change to a new object.
        /// Derived classes must implement tool-specific initialization logic.
        /// Typically sets m_IsInEditMode to true and fires editModeEntered event.
        /// </summary>
        /// <param name="newObject">The new object that will become the edit target</param>
        public abstract void OnEditObjectChanging(GameObject newObject);

        // Helper Methods for Derived Classes

        /// <summary>
        /// Invokes the editModeEntered event. Derived classes call this when entering edit mode.
        /// </summary>
        protected void InvokeEditModeEntered()
        {
            editModeEntered?.Invoke();
        }

        /// <summary>
        /// Invokes the editModeExited event. Derived classes call this when exiting edit mode.
        /// </summary>
        protected void InvokeEditModeExited()
        {
            editModeExited?.Invoke();
        }
    }
}
