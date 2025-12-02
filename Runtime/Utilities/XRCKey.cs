using UnityEngine;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// ScriptableObject for storing API keys securely outside version control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Create instances of this class via the Unity menu: Create > Scriptable Objects > XRC > XRC Key.
    /// Store API keys for external services (OpenAI, Ollama, etc.) in these assets.
    /// </para>
    /// <para>
    /// Important: Add XRCKey assets to your .gitignore to prevent accidentally committing
    /// sensitive API keys to version control.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "XRCKey", menuName = "Scriptable Objects/XRC/XRC Key")]
    public class XRCKey : ScriptableObject
    {
        /// <summary>
        /// The API key value stored in this asset.
        /// </summary>
        public string key;
    }
}