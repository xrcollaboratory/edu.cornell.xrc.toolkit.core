using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Populates a simple informational panel built from <c>CoreUIDocument.uxml</c> with
    /// player-settings values pulled from <see cref="Application"/> at runtime: product
    /// name, company, application identifier (Android package name / iOS bundle id),
    /// version, plus the active scene name and Unity version. Attach to a GameObject
    /// that owns a <see cref="UIDocument"/> using <c>CoreUIDocument.uxml</c> as its
    /// source asset.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CoreUIController : MonoBehaviour
    {
        private UIDocument m_Document;
        private Label m_SceneNameLabel;
        private Label m_ProductNameLabel;
        private Label m_CompanyNameLabel;
        private Label m_IdentifierLabel;
        private Label m_VersionLabel;
        private Label m_UnityVersionLabel;

        private void OnEnable()
        {
            m_Document = GetComponent<UIDocument>();
            if (!TryBind())
                StartCoroutine(BindWhenReady());

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            m_SceneNameLabel = null;
            m_ProductNameLabel = null;
            m_CompanyNameLabel = null;
            m_IdentifierLabel = null;
            m_VersionLabel = null;
            m_UnityVersionLabel = null;
        }

        /// <summary>
        /// Re-populate every label from the current scene and player settings.
        /// </summary>
        public void Refresh()
        {
            if (m_SceneNameLabel != null)
                m_SceneNameLabel.text = SceneManager.GetActiveScene().name;
            if (m_ProductNameLabel != null)
                m_ProductNameLabel.text = Application.productName;
            if (m_CompanyNameLabel != null)
                m_CompanyNameLabel.text = Application.companyName;
            if (m_IdentifierLabel != null)
                m_IdentifierLabel.text = Application.identifier;
            if (m_VersionLabel != null)
                m_VersionLabel.text = Application.version;
            if (m_UnityVersionLabel != null)
                m_UnityVersionLabel.text = Application.unityVersion;
        }

        private IEnumerator BindWhenReady()
        {
            while (!TryBind())
                yield return null;
        }

        private bool TryBind()
        {
            var root = m_Document != null ? m_Document.rootVisualElement : null;
            if (root == null)
                return false;

            m_SceneNameLabel = root.Q<Label>("SceneName");
            m_ProductNameLabel = root.Q<Label>("ProductName");
            m_CompanyNameLabel = root.Q<Label>("CompanyName");
            m_IdentifierLabel = root.Q<Label>("Identifier");
            m_VersionLabel = root.Q<Label>("Version");
            m_UnityVersionLabel = root.Q<Label>("UnityVersion");

            Refresh();
            return true;
        }

        private void OnActiveSceneChanged(Scene previous, Scene current) => Refresh();
    }
}
