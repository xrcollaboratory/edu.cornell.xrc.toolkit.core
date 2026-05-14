using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Drives a UI Toolkit-based scene loader panel. Generates one button per scene name
    /// in the serialized list and loads the corresponding scene when clicked. Intended to
    /// be authored once in the start / home scene and persist across scene loads via
    /// <see cref="UnityEngine.Object.DontDestroyOnLoad"/> so subsequent scenes don't need
    /// to carry their own copy.
    /// </summary>
    /// <remarks>
    /// Scene names must match scenes present in Build Settings. The currently active scene
    /// is highlighted by toggling the <c>scene-loader-button--active</c> USS class on its
    /// generated button.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SceneLoaderController : MonoBehaviour
    {
        private const string k_SceneListName = "SceneList";
        private const string k_SceneButtonClass = "scene-loader-button";
        private const string k_SceneButtonActiveClass = "scene-loader-button--active";

        [SerializeField]
        [Tooltip("Scenes shown as buttons in the loader, in display order. Each name must match a scene in Build Settings.")]
        private List<string> m_Scenes = new();

        [SerializeField]
        [Tooltip("If enabled, the GameObject in Persistent Root (or this transform's root, if unset) is marked DontDestroyOnLoad so the loader survives scene loads.")]
        private bool m_PersistAcrossScenes = true;

        [SerializeField]
        [Tooltip("GameObject to mark as DontDestroyOnLoad. Leave empty to use this transform's root, which is the typical setup when the loader sits under a shared UI parent that owns the Navigation Rail.")]
        private GameObject m_PersistentRoot;

        private static SceneLoaderController s_Instance;

        private UIDocument m_Document;
        private VisualElement m_SceneList;
        private readonly Dictionary<string, Button> m_Buttons = new();

        /// <summary>
        /// Scene names currently configured on the loader.
        /// </summary>
        public IReadOnlyList<string> scenes => m_Scenes;

        private void Awake()
        {
            var target = m_PersistentRoot != null ? m_PersistentRoot : transform.root.gameObject;

            if (s_Instance != null && s_Instance != this)
            {
                Destroy(target);
                return;
            }

            s_Instance = this;

            if (m_PersistAcrossScenes)
                DontDestroyOnLoad(target);
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        private void OnEnable()
        {
            m_Document = GetComponent<UIDocument>();
            if (!TryBuild())
                StartCoroutine(BuildWhenReady());

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            m_SceneList?.Clear();
            m_Buttons.Clear();
            m_SceneList = null;
            m_Document = null;
        }

        /// <summary>
        /// Load the named scene in single mode. No-op if <paramref name="sceneName"/> is null or empty.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator BuildWhenReady()
        {
            while (!TryBuild())
                yield return null;
        }

        private bool TryBuild()
        {
            var root = m_Document != null ? m_Document.rootVisualElement : null;
            if (root == null)
                return false;

            m_SceneList = root.Q<VisualElement>(k_SceneListName);
            if (m_SceneList == null)
                return false;

            m_SceneList.Clear();
            m_Buttons.Clear();

            foreach (var sceneName in m_Scenes)
            {
                if (string.IsNullOrEmpty(sceneName))
                    continue;

                var button = CreateSceneButton(sceneName);
                m_SceneList.Add(button);
                m_Buttons[sceneName] = button;
            }

            HighlightActive();
            return true;
        }

        private Button CreateSceneButton(string sceneName)
        {
            var button = new Button
            {
                text = sceneName,
                name = $"Scene_{sceneName}"
            };
            button.AddToClassList(k_SceneButtonClass);
            button.clicked += () => LoadScene(sceneName);
            return button;
        }

        private void HighlightActive()
        {
            var active = SceneManager.GetActiveScene().name;
            foreach (var pair in m_Buttons)
            {
                if (pair.Value == null)
                    continue;
                pair.Value.EnableInClassList(k_SceneButtonActiveClass, pair.Key == active);
            }
        }

        private void OnActiveSceneChanged(Scene previous, Scene current) => HighlightActive();
    }
}
