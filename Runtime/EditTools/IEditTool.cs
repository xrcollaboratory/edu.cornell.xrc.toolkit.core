using UnityEngine;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Interface for tools that can edit objects in XR environments.
    /// This interface defines the contract for edit tools that can be controlled by
    /// object selection providers and other systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides edit-mode-specific methods for entering and exiting edit mode.
    /// The <see cref="isInEditMode"/> property indicates whether the tool is actively editing,
    /// and <see cref="EnterEditMode"/> / <see cref="ExitEditMode"/> methods control the
    /// edit mode lifecycle.
    /// </para>
    /// <para>
    /// This interface enables dependency inversion where object selection providers
    /// can work with any edit tool implementation without tight coupling.
    /// Edit tools that implement this interface can be used with <c>EditObjectProvider</c>
    /// and other object selection systems.
    /// </para>
    /// <para>
    /// Example implementations: XRC Mesh Tool, XRC Scale Tool, XRC Color Tool.
    /// </para>
    /// </remarks>
    public interface IEditTool
    {
        /// <summary>
        /// The object currently being edited by this tool.
        /// Setting this property should update the tool's target object and may
        /// trigger edit mode transitions if the object changes.
        /// </summary>
        /// <remarks>
        /// When this property is set to a new object, implementations should:
        /// <list type="bullet">
        /// <item>Call <see cref="OnEditObjectChanging"/> with the new object before updating</item>
        /// <item>Clean up any state related to the previous edit object</item>
        /// <item>Initialize state for the new edit object</item>
        /// </list>
        /// </remarks>
        GameObject editObject { get; set; }

        /// <summary>
        /// Whether the tool is currently in edit mode.
        /// When true, the tool is actively editing the target object.
        /// When false, the tool is in normal interaction mode.
        /// </summary>
        bool isInEditMode { get; }

        /// <summary>
        /// Called when the edit object is about to change to a new object.
        /// Implementations should use this to clean up state related to the
        /// previous edit object and prepare for the new one.
        /// </summary>
        /// <param name="newObject">The new object that will become the edit target, or null if clearing the target</param>
        /// <remarks>
        /// <para>
        /// This method is called automatically by the <see cref="editObject"/> setter before
        /// the object reference is updated. It provides an opportunity to:
        /// </para>
        /// <list type="bullet">
        /// <item>Exit edit mode if currently active</item>
        /// <item>Clean up handles, UI, or other temporary objects</item>
        /// <item>Unsubscribe from events on the previous object</item>
        /// <item>Save state or restore the previous object to its original state</item>
        /// </list>
        /// </remarks>
        void OnEditObjectChanging(GameObject newObject);

        /// <summary>
        /// Enters edit mode for the current edit object.
        /// Sets <see cref="isInEditMode"/> to true and activates edit-specific functionality.
        /// </summary>
        void EnterEditMode();

        /// <summary>
        /// Exits edit mode for the current edit object.
        /// Sets <see cref="isInEditMode"/> to false and deactivates edit-specific functionality.
        /// </summary>
        void ExitEditMode();

        /// <summary>
        /// Toggles edit mode on or off.
        /// Calls <see cref="ExitEditMode"/> if currently in edit mode, otherwise calls <see cref="EnterEditMode"/>.
        /// </summary>
        void ToggleEditMode();
    }
}
