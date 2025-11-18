using System;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Interface to be implemented by components, such as edit tools and interaction techniques, that have a distinct run condition.
    /// A run condition is where the tool, or technique, execute its primary tasks.
    /// </summary>
    /// <remarks>
    /// An example of a tool with a run condition is the XRC Color Tool, which is running when an object's color is being updated by the tool.
    /// </remarks>
    public interface IRunnable 
    {
        
        /// <summary>
        /// Whether the tool is running or not.
        /// </summary>
        public bool isRunning { get;}
        
        /// <summary>
        /// Start running the tool.
        /// </summary>
        public void StartRun();

        /// <summary>
        /// End running the tool.
        /// </summary>
        public void StopRun();
        
        /// <summary>
        /// Toggle the tool.
        /// </summary>
        public void ToggleRun();

    }
}
