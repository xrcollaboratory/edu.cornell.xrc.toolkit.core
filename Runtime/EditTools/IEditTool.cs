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
    /// This interface extends <see cref="IRunnable"/> to integrate with the XRC Toolkit's
    /// standard run state management pattern. The <see cref="IRunnable.isRunning"/> property
    /// indicates whether the tool is in edit mode, and <see cref="IRunnable.StartRun"/> /
    /// <see cref="IRunnable.StopRun"/> methods control entering and exiting edit mode.
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
    public interface IEditTool : IRunnable
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
        /// <remarks>
        /// This property typically returns the same value as <see cref="IRunnable.isRunning"/>.
        /// It exists as a semantic alias to make edit tool code more readable.
        /// </remarks>
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
    }
}
