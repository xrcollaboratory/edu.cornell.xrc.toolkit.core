using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Minimal world-space navigation rail. Switches between UI Toolkit panels contributed
    /// by different packages by showing one panel at a time. At runtime each panel
    /// GameObject is reparented as a sibling of this navigation rail (under its
    /// parent) and its local pose is set to the navigation rail's local pose plus
    /// the shared offset, so the rail and the panels move together when the shared
    /// parent (for example a controller) moves.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class NavigationRailController : MonoBehaviour
    {
        private const string k_RailItemsName = "RailItems";
        private const string k_TabClass = "navigation-rail-tab";
        private const string k_IconClass = "navigation-rail-icon";
        private const string k_SelectedClass = "navigation-rail-tab--selected";

        [SerializeField]
        [Tooltip("Panels shown as tabs in the navigation rail, in display order.")]
        private List<UIPanelDefinition> m_Panels = new();

        [SerializeField]
        [Tooltip("Horizontal offset (along the shared parent's X axis) from the navigation rail to every panel. Y and Z match the navigation rail so panel tops stay aligned with it.")]
        private float m_PanelXOffset;

        private UIDocument m_Document;
        private VisualElement m_RailItems;
        private readonly Dictionary<UIPanelDefinition, Button> m_Buttons = new();
        private UIPanelDefinition m_ActivePanel;

        /// <summary>
        /// Panels currently configured on the navigation rail.
        /// </summary>
        public IReadOnlyList<UIPanelDefinition> panels => m_Panels;

        /// <summary>
        /// Panel currently selected, or null if none.
        /// </summary>
        public UIPanelDefinition activePanel => m_ActivePanel;

        /// <summary>
        /// Raised after a different panel becomes active.
        /// </summary>
        public event Action<UIPanelDefinition> activePanelChanged;

        private void OnEnable()
        {
            m_Document = GetComponent<UIDocument>();
            if (!TryBuildRail())
                StartCoroutine(BuildWhenReady());
        }

        private void OnDisable()
        {
            m_RailItems?.Clear();
            m_Buttons.Clear();
            m_RailItems = null;
            m_Document = null;
            m_ActivePanel = null;
        }

        /// <summary>
        /// Show the given panel and hide the others. Returns false if the panel is not in the list.
        /// </summary>
        public bool SelectPanel(UIPanelDefinition panel)
        {
            if (panel == null || !m_Panels.Contains(panel))
                return false;

            m_ActivePanel = panel;

            foreach (var entry in m_Panels)
            {
                if (entry == null)
                    continue;

                var isSelected = entry == panel;

                if (m_Buttons.TryGetValue(entry, out var button) && button != null)
                    button.EnableInClassList(k_SelectedClass, isSelected);

                if (entry.PanelDocument == null)
                    continue;

                var entryRoot = entry.PanelDocument.rootVisualElement;
                if (entryRoot != null)
                {
                    entryRoot.style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;
                    entryRoot.pickingMode = isSelected ? PickingMode.Position : PickingMode.Ignore;
                }

                foreach (var panelCollider in entry.PanelDocument.GetComponentsInChildren<Collider>(true))
                    panelCollider.enabled = isSelected;
            }

            activePanelChanged?.Invoke(m_ActivePanel);
            return true;
        }

        private IEnumerator BuildWhenReady()
        {
            while (!TryBuildRail())
                yield return null;
        }

        private bool TryBuildRail()
        {
            var root = m_Document != null ? m_Document.rootVisualElement : null;
            if (root == null)
                return false;

            m_RailItems = root.Q<VisualElement>(k_RailItemsName);
            if (m_RailItems == null)
                return false;

            m_RailItems.Clear();
            m_Buttons.Clear();

            var sharedParent = transform.parent;
            var railLocalPosition = transform.localPosition;
            var railLocalRotation = transform.localRotation;
            var panelLocalPosition = railLocalPosition + new Vector3(m_PanelXOffset, 0f, 0f);

            foreach (var panel in m_Panels)
            {
                if (panel == null || panel.PanelDocument == null)
                    continue;

                var panelTransform = panel.PanelDocument.transform;
                panelTransform.SetParent(sharedParent, worldPositionStays: false);
                panelTransform.localPosition = panelLocalPosition;
                panelTransform.localRotation = railLocalRotation;

                var button = CreateTabButton(panel);
                m_RailItems.Add(button);
                m_Buttons[panel] = button;
            }

            if (m_Panels.Count > 0)
                SelectPanel(m_Panels[0]);

            return true;
        }

        private Button CreateTabButton(UIPanelDefinition panel)
        {
            var button = new Button
            {
                text = string.Empty,
                name = $"Tab_{panel.name}"
            };
            button.AddToClassList(k_TabClass);
            button.clicked += () => SelectPanel(panel);

            var image = new Image
            {
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit
            };
            image.AddToClassList(k_IconClass);
            panel.ApplyIcon(image);
            button.Add(image);
            return button;
        }
    }
}
