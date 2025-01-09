using UnityEngine;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// An interface to be implemented by edit tools that require an object to edit.
    /// </summary>
    public interface IEditTool: IRunnable
    {
        /// <summary>
        /// The object to be edited by the tool.
        /// </summary>
        public GameObject editObject { get; set; }
        
    }
}
