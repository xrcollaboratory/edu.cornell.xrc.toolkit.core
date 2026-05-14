using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRC.Toolkit.Core.Legacy
{
    /// <summary>
    /// Adds a border-only highlight when the docked panel is actually hoverable for grab.
    /// This visual is independent from UI Toolkit content and only reflects grab affordance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PanelHoverAffordance : MonoBehaviour
    {
        private static readonly FieldInfo WorldSpaceWidthField =
            typeof(UIDocument).GetField("m_WorldSpaceWidth", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo WorldSpaceHeightField =
            typeof(UIDocument).GetField("m_WorldSpaceHeight", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo WorldSpaceSizeModeField =
            typeof(UIDocument).GetField("m_WorldSpaceSizeMode", BindingFlags.Instance | BindingFlags.NonPublic);

        private const int WorldSpaceSizeModeDynamic = 1;
        private const string OverlayObjectName = "Panel Proximity Affordance";
        private const string TopBorderName = "Top Border";
        private const string BottomBorderName = "Bottom Border";
        private const string LeftBorderName = "Left Border";
        private const string RightBorderName = "Right Border";

        [Header("Bindings")]
        [SerializeField] private NavigationPanelGroupPlacementController m_PlacementController;

        [Header("Visibility")]
        [SerializeField] private bool m_ShowOnlyWhileDocked = true;
        [SerializeField, Min(0.1f)] private float m_FadeSpeed = 9f;
        [SerializeField] private float m_LocalDepthOffset = -0.0015f;

        [Header("Shape")]
        [SerializeField, Range(0f, 0.2f)] private float m_PaddingRatio = 0.03f;
        [SerializeField, Range(0.001f, 0.15f)] private float m_BorderThicknessRatio = 0.02f;
        [SerializeField, Range(0f, 0.2f)] private float m_CornerInsetRatio = 0.06f;

        [Header("Color")]
        [SerializeField] private Color m_BorderColor = new(0.84f, 0.92f, 1f, 0.82f);

        [Header("Auto-Wired")]
        [SerializeField] private Transform m_OverlayRoot;

        private readonly Transform[] m_BorderTransforms = new Transform[4];
        private readonly MeshRenderer[] m_BorderRenderers = new MeshRenderer[4];

        private Mesh m_QuadMesh;
        private Material m_Material;
        private float m_CurrentAlpha;
        private bool m_LastVisibleState;

        private void Awake()
        {
            AutoWireReferences();
            EnsureOverlay();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            EnsureOverlay();
        }

        private void OnDisable()
        {
            SetRenderersVisible(false);
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (m_Material != null)
                    Destroy(m_Material);
                if (m_QuadMesh != null)
                    Destroy(m_QuadMesh);
            }
            else
            {
                if (m_Material != null)
                    DestroyImmediate(m_Material);
                if (m_QuadMesh != null)
                    DestroyImmediate(m_QuadMesh);
            }
        }

        private void LateUpdate()
        {
            AutoWireReferences();
            EnsureOverlay();

            if (m_OverlayRoot == null || !TryGetPanelBounds(out var panelCenterLocal, out var panelSizeLocal))
            {
                SetRenderersVisible(false);
                return;
            }

            UpdateOverlayGeometry(panelCenterLocal, panelSizeLocal);

            var canShow = !m_ShowOnlyWhileDocked
                          || m_PlacementController == null
                          || m_PlacementController.CurrentMode == PanelPlacementMode.ControllerFollow;
            if (m_PlacementController != null && !m_PlacementController.IsDockedVisible)
                canShow = false;
            var desiredAlpha = canShow && GetHoverCount() > 0 ? 1f : 0f;

            m_CurrentAlpha = Mathf.MoveTowards(m_CurrentAlpha, desiredAlpha, m_FadeSpeed * Time.deltaTime);
            UpdateMaterialAlpha(m_CurrentAlpha);
            SetRenderersVisible(m_CurrentAlpha > 0.001f);
        }

        private void AutoWireReferences()
        {
            m_PlacementController ??= GetComponent<NavigationPanelGroupPlacementController>();
        }

        private void EnsureOverlay()
        {
            if (m_OverlayRoot == null)
            {
                var existing = transform.Find(OverlayObjectName);
                if (existing != null)
                    m_OverlayRoot = existing;
            }

            if (m_OverlayRoot == null)
            {
                var overlay = new GameObject(OverlayObjectName);
                overlay.transform.SetParent(transform, false);
                m_OverlayRoot = overlay.transform;
            }

            if (m_QuadMesh == null)
                m_QuadMesh = CreateQuadMesh();

            if (m_Material == null)
            {
                var shader = Shader.Find("Sprites/Default")
                             ?? Shader.Find("Unlit/Color")
                             ?? Shader.Find("Unlit/Texture");

                if (shader != null)
                {
                    m_Material = new Material(shader)
                    {
                        name = "PanelHoverAffordance (Runtime)"
                    };
                }
            }

            EnsureBorderStrip(0, TopBorderName);
            EnsureBorderStrip(1, BottomBorderName);
            EnsureBorderStrip(2, LeftBorderName);
            EnsureBorderStrip(3, RightBorderName);
            UpdateMaterialAlpha(m_CurrentAlpha);
        }

        private void EnsureBorderStrip(int index, string name)
        {
            if (m_BorderTransforms[index] == null)
            {
                var existing = m_OverlayRoot.Find(name);
                if (existing != null)
                    m_BorderTransforms[index] = existing;
            }

            if (m_BorderTransforms[index] == null)
            {
                var strip = new GameObject(name);
                strip.transform.SetParent(m_OverlayRoot, false);
                m_BorderTransforms[index] = strip.transform;
            }

            var filter = m_BorderTransforms[index].GetComponent<MeshFilter>();
            if (filter == null)
                filter = m_BorderTransforms[index].gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = m_QuadMesh;

            var renderer = m_BorderTransforms[index].GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = m_BorderTransforms[index].gameObject.AddComponent<MeshRenderer>();

            if (m_Material != null)
                renderer.sharedMaterial = m_Material;

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.enabled = false;
            m_BorderRenderers[index] = renderer;
        }

        private void UpdateOverlayGeometry(Vector3 panelCenterLocal, Vector2 panelSizeLocal)
        {
            var minDim = Mathf.Max(0.01f, Mathf.Min(panelSizeLocal.x, panelSizeLocal.y));
            var padding = minDim * m_PaddingRatio;
            var totalWidth = panelSizeLocal.x + padding * 2f;
            var totalHeight = panelSizeLocal.y + padding * 2f;
            var borderThickness = Mathf.Max(0.001f, minDim * m_BorderThicknessRatio);
            var cornerInset = Mathf.Max(borderThickness * 0.5f, minDim * m_CornerInsetRatio);

            m_OverlayRoot.localPosition = new Vector3(panelCenterLocal.x, panelCenterLocal.y, m_LocalDepthOffset);
            m_OverlayRoot.localRotation = Quaternion.identity;
            m_OverlayRoot.localScale = Vector3.one;

            var horizontalWidth = Mathf.Max(borderThickness, totalWidth - cornerInset * 2f);
            var verticalHeight = Mathf.Max(borderThickness, totalHeight - cornerInset * 2f);

            SetStripTransform(m_BorderTransforms[0], new Vector3(0f, (totalHeight - borderThickness) * 0.5f, 0f), new Vector3(horizontalWidth, borderThickness, 1f));
            SetStripTransform(m_BorderTransforms[1], new Vector3(0f, -(totalHeight - borderThickness) * 0.5f, 0f), new Vector3(horizontalWidth, borderThickness, 1f));
            SetStripTransform(m_BorderTransforms[2], new Vector3(-(totalWidth - borderThickness) * 0.5f, 0f, 0f), new Vector3(borderThickness, verticalHeight, 1f));
            SetStripTransform(m_BorderTransforms[3], new Vector3((totalWidth - borderThickness) * 0.5f, 0f, 0f), new Vector3(borderThickness, verticalHeight, 1f));
        }

        private void UpdateMaterialAlpha(float alpha)
        {
            if (m_Material == null)
                return;

            var color = m_BorderColor;
            color.a *= alpha;
            m_Material.color = color;
        }

        private void SetRenderersVisible(bool visible)
        {
            if (m_LastVisibleState == visible)
                return;

            foreach (var renderer in m_BorderRenderers)
            {
                if (renderer != null)
                    renderer.enabled = visible;
            }

            m_LastVisibleState = visible;
        }

        private static void SetStripTransform(Transform strip, Vector3 localPosition, Vector3 localScale)
        {
            if (strip == null)
                return;

            strip.localPosition = localPosition;
            strip.localRotation = Quaternion.identity;
            strip.localScale = localScale;
        }

        private int GetHoverCount()
        {
            var interactable = m_PlacementController != null ? m_PlacementController.DockedGrabInteractable : null;
            return interactable != null ? interactable.interactorsHovering.Count : 0;
        }

        private bool TryGetPanelBounds(out Vector3 center, out Vector2 panelSize)
        {
            center = Vector3.zero;
            panelSize = default;
            var documents = GetComponentsInChildren<UIDocument>(true);
            if (documents == null || documents.Length == 0)
                return false;

            var hasBounds = false;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (var document in documents)
            {
                if (!TryGetDocumentSize(document, out var documentSize))
                    continue;

                if (!document.isActiveAndEnabled || !document.gameObject.activeInHierarchy)
                    continue;

                var root = document.rootVisualElement;
                if (root == null || root.resolvedStyle.display == DisplayStyle.None)
                    continue;

                var localCenter = transform.InverseTransformPoint(document.transform.position);
                var halfSize = documentSize * 0.5f;
                min = Vector2.Min(min, new Vector2(localCenter.x - halfSize.x, localCenter.y - halfSize.y));
                max = Vector2.Max(max, new Vector2(localCenter.x + halfSize.x, localCenter.y + halfSize.y));
                hasBounds = true;
            }

            if (!hasBounds)
                return false;

            center = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
            panelSize = new Vector2(max.x - min.x, max.y - min.y);
            return true;
        }

        private static bool TryGetDocumentSize(UIDocument document, out Vector2 panelSize)
        {
            panelSize = default;
            if (document == null)
                return false;

            var preferResolvedLayout = TryGetWorldSpaceSizeMode(document, out var sizeMode) &&
                                      sizeMode == WorldSpaceSizeModeDynamic;
            if (preferResolvedLayout && TryGetResolvedLayoutSize(document, out panelSize))
                return true;

            if (TryGetWorldSpaceDimension(document, WorldSpaceWidthField, out var width) &&
                TryGetWorldSpaceDimension(document, WorldSpaceHeightField, out var height) &&
                width > 0f &&
                height > 0f)
            {
                panelSize = new Vector2(width, height) * 0.01f;
                return true;
            }

            return TryGetResolvedLayoutSize(document, out panelSize);
        }

        private static bool TryGetResolvedLayoutSize(UIDocument document, out Vector2 panelSize)
        {
            panelSize = default;
            var root = document.rootVisualElement;
            if (root == null)
                return false;

            var resolvedWidth = root.resolvedStyle.width;
            var resolvedHeight = root.resolvedStyle.height;
            if (float.IsNaN(resolvedWidth) || float.IsNaN(resolvedHeight) || resolvedWidth <= 0f || resolvedHeight <= 0f)
                return false;

            panelSize = new Vector2(resolvedWidth, resolvedHeight) * 0.01f;
            return true;
        }

        private static bool TryGetWorldSpaceDimension(UIDocument document, FieldInfo field, out float value)
        {
            value = 0f;
            if (document == null || field == null)
                return false;

            if (field.GetValue(document) is not float dimension)
                return false;

            value = dimension;
            return true;
        }

        private static bool TryGetWorldSpaceSizeMode(UIDocument document, out int sizeMode)
        {
            sizeMode = 0;
            if (document == null || WorldSpaceSizeModeField == null)
                return false;

            if (WorldSpaceSizeModeField.GetValue(document) is not int modeValue)
                return false;

            sizeMode = modeValue;
            return true;
        }

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh
            {
                name = "PanelHoverAffordanceQuad"
            };

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };

            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };

            mesh.triangles = new[]
            {
                0, 2, 1,
                2, 3, 1,
                1, 2, 0,
                1, 3, 2
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
