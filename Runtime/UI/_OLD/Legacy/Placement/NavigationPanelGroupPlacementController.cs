using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace XRC.Toolkit.Core.Legacy
{
    public enum PanelPlacementMode
    {
        ControllerFollow = 0,
        WorldPinned = 1
    }

    /// <summary>
    /// Controls whether a navigation panel group follows the controller
    /// or is detached and pinned in world space.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NavigationPanelGroupPlacementController : MonoBehaviour, INavigationPanelPlacement
    {
        private const string DockedGrabColliderObjectName = "Docked Grab Collider";
        private static readonly FieldInfo WorldSpaceWidthField =
            typeof(UIDocument).GetField("m_WorldSpaceWidth", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo WorldSpaceHeightField =
            typeof(UIDocument).GetField("m_WorldSpaceHeight", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo WorldSpaceSizeModeField =
            typeof(UIDocument).GetField("m_WorldSpaceSizeMode", BindingFlags.Instance | BindingFlags.NonPublic);

        private const int WorldSpaceSizeModeDynamic = 1;

        [Header("Placement")]
        [SerializeField] private PanelPlacementMode m_DefaultMode = PanelPlacementMode.ControllerFollow;
        [SerializeField] private bool m_ApplyDefaultModeOnEnable = true;
        [SerializeField] private bool m_DockedVisible = true;

        [Header("Input")]
        [SerializeField] private InputActionReference m_ToggleVisibilityAction;

        [Header("Docked Grab")]
        [SerializeField] private bool m_EnableGrabToPinFromDocked = true;
        [SerializeField] private Vector2 m_DockedGrabPadding = new(0.16f, 0.14f);
        [SerializeField, Min(0.01f)] private float m_DockedGrabDepth = 0.18f;

        [NonSerialized] private PanelPlacementMode m_CurrentMode = PanelPlacementMode.ControllerFollow;
        [SerializeField, HideInInspector] private ControllerRelativeFollowTransform m_FollowController;
        [SerializeField, HideInInspector] private UIDocument m_PanelDocument;
        [SerializeField, HideInInspector] private XRGrabInteractable m_DockedGrabInteractable;
        [SerializeField, HideInInspector] private BoxCollider m_DockedGrabCollider;
        [SerializeField, HideInInspector] private Rigidbody m_Rigidbody;

        public event Action<PanelPlacementMode> PlacementModeChanged;
        public event Action PlacementStateChanged;

        public PanelPlacementMode CurrentMode => m_CurrentMode;
        public bool IsPinned => m_CurrentMode == PanelPlacementMode.WorldPinned;
        public bool IsDockedVisible => m_CurrentMode == PanelPlacementMode.WorldPinned || m_DockedVisible;
        public Transform PlacementRoot => transform;
        public Transform PlacementTransform => transform;
        public XRGrabInteractable DockedGrabInteractable => m_DockedGrabInteractable;

        private bool m_KeepDockedGrabActiveUntilRelease;
        private XRHoverFilterDelegate m_DirectHoverFilter;
        private XRSelectFilterDelegate m_DirectSelectFilter;

        private void Reset()
        {
            AutoWireReferences();
        }

        private void Awake()
        {
            AutoWireReferences();
            EnsureDockedGrabComponents();
            BindDockedGrabEvents();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            EnsureDockedGrabComponents();
            BindDockedGrabEvents();
            BindToggleInput();

            if (!Application.isPlaying)
                return;

            if (m_ApplyDefaultModeOnEnable)
                m_CurrentMode = m_DefaultMode;

            ApplyMode(snapDockPose: true);
        }

        private void OnValidate()
        {
            AutoWireReferences();
            RefreshDockedGrabCollider();

            if (Application.isPlaying)
                ApplyMode(snapDockPose: false);
        }

        private void OnDisable()
        {
            UnbindDockedGrabEvents();
            UnbindToggleInput();
        }

        private void OnDestroy()
        {
            UnbindDockedGrabEvents();
            UnbindToggleInput();
        }

        private void LateUpdate()
        {
            if (m_CurrentMode == PanelPlacementMode.ControllerFollow || m_KeepDockedGrabActiveUntilRelease)
                RefreshDockedGrabCollider();
        }

        public void SetPlacementMode(PanelPlacementMode mode)
        {
            if (m_CurrentMode == mode)
            {
                ApplyMode(snapDockPose: mode == PanelPlacementMode.ControllerFollow);
                return;
            }

            m_CurrentMode = mode;
            ApplyMode(snapDockPose: mode == PanelPlacementMode.ControllerFollow);
            PlacementModeChanged?.Invoke(m_CurrentMode);
            PlacementStateChanged?.Invoke();
        }

        public void ToggleDockedVisibility()
        {
            if (m_CurrentMode != PanelPlacementMode.ControllerFollow)
                return;

            SetDockedVisibility(!m_DockedVisible);
        }

        public void SetDockedVisibility(bool visible)
        {
            if (m_CurrentMode != PanelPlacementMode.ControllerFollow)
            {
                m_DockedVisible = true;
                return;
            }

            if (m_DockedVisible == visible)
            {
                ApplyMode(snapDockPose: false);
                return;
            }

            m_DockedVisible = visible;
            ApplyMode(snapDockPose: false);
            PlacementStateChanged?.Invoke();
        }

        private void ApplyMode(bool snapDockPose)
        {
            AutoWireReferences();

            var isDocked = m_CurrentMode == PanelPlacementMode.ControllerFollow;
            if (isDocked)
            {
                if (m_FollowController != null)
                {
                    m_FollowController.enabled = true;
                    if (snapDockPose)
                        m_FollowController.SnapToTarget();
                }

                SetManagedUiVisible(m_DockedVisible);
                SetDockedGrabActive(m_EnableGrabToPinFromDocked && m_DockedVisible);
                return;
            }

            m_DockedVisible = true;

            if (m_FollowController != null)
                m_FollowController.enabled = false;

            SetManagedUiVisible(true);
            SetDockedGrabActive(m_EnableGrabToPinFromDocked);
        }

        private void AutoWireReferences()
        {
            m_FollowController ??= GetComponent<ControllerRelativeFollowTransform>();
            m_PanelDocument ??= GetComponentInChildren<UIDocument>(true);
            m_DockedGrabInteractable ??= GetComponent<XRGrabInteractable>();
            m_Rigidbody ??= GetComponent<Rigidbody>();
            m_DockedGrabCollider ??= ResolveDockedGrabCollider();
        }

        private void EnsureDockedGrabComponents()
        {
            if (!Application.isPlaying || !m_EnableGrabToPinFromDocked)
                return;

            if (!TryGetComponent(out Rigidbody existingRigidbody))
                existingRigidbody = gameObject.AddComponent<Rigidbody>();

            m_Rigidbody = existingRigidbody;

            m_Rigidbody.useGravity = false;
            m_Rigidbody.isKinematic = true;

            m_DockedGrabCollider = EnsureDockedGrabCollider();
            if (m_DockedGrabCollider == null)
                return;

            m_DockedGrabCollider.isTrigger = false;

            if (!TryGetComponent(out XRGrabInteractable existingInteractable))
                existingInteractable = gameObject.AddComponent<XRGrabInteractable>();

            m_DockedGrabInteractable = existingInteractable;

            m_DockedGrabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            m_DockedGrabInteractable.throwOnDetach = false;
            m_DockedGrabInteractable.trackPosition = true;
            m_DockedGrabInteractable.trackRotation = true;
            m_DockedGrabInteractable.useDynamicAttach = true;
            m_DockedGrabInteractable.snapToColliderVolume = false;

            EnsureInteractorFilters();
        }

        private BoxCollider EnsureDockedGrabCollider()
        {
            if (TryGetComponent(out BoxCollider rootCollider))
                rootCollider.enabled = false;

            if (m_DockedGrabCollider != null && m_DockedGrabCollider.transform == transform)
            {
                m_DockedGrabCollider.enabled = false;
                m_DockedGrabCollider = null;
            }

            if (m_DockedGrabCollider == null)
                m_DockedGrabCollider = ResolveDockedGrabCollider();

            Transform colliderTransform = m_DockedGrabCollider != null ? m_DockedGrabCollider.transform : null;
            if (colliderTransform == null)
            {
                var colliderObject = new GameObject(DockedGrabColliderObjectName);
                colliderObject.transform.SetParent(transform, false);
                colliderObject.transform.localPosition = Vector3.zero;
                colliderObject.transform.localRotation = Quaternion.identity;
                colliderObject.transform.localScale = Vector3.one;
                colliderObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                m_DockedGrabCollider = colliderObject.AddComponent<BoxCollider>();
                colliderTransform = colliderObject.transform;
            }

            colliderTransform.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            return m_DockedGrabCollider;
        }

        private BoxCollider ResolveDockedGrabCollider()
        {
            Transform existing = transform.Find(DockedGrabColliderObjectName);
            if (existing != null)
                return existing.GetComponent<BoxCollider>();

            return null;
        }

        private void EnsureInteractorFilters()
        {
            if (m_DockedGrabInteractable == null)
                return;

            m_DirectHoverFilter ??= new XRHoverFilterDelegate((interactor, _) => IsAllowedGrabInteractor(interactor));
            m_DirectSelectFilter ??= new XRSelectFilterDelegate((interactor, _) => IsAllowedGrabInteractor(interactor));

            if (!HasFilter(m_DockedGrabInteractable.hoverFilters, m_DirectHoverFilter))
                m_DockedGrabInteractable.hoverFilters.Add(m_DirectHoverFilter);

            if (!HasFilter(m_DockedGrabInteractable.selectFilters, m_DirectSelectFilter))
                m_DockedGrabInteractable.selectFilters.Add(m_DirectSelectFilter);
        }

        private static bool HasFilter<TFilter>(IXRFilterList<TFilter> filterList, TFilter filter)
        {
            if (filterList == null || filter == null)
                return false;

            for (int i = 0; i < filterList.count; i++)
            {
                TFilter existingFilter = filterList.GetAt(i);
                if (ReferenceEquals(existingFilter, filter))
                    return true;
            }

            return false;
        }

        private void BindDockedGrabEvents()
        {
            if (!Application.isPlaying || m_DockedGrabInteractable == null)
                return;

            m_DockedGrabInteractable.selectEntered.RemoveListener(OnDockedGrabEntered);
            m_DockedGrabInteractable.selectExited.RemoveListener(OnDockedGrabExited);
            m_DockedGrabInteractable.selectEntered.AddListener(OnDockedGrabEntered);
            m_DockedGrabInteractable.selectExited.AddListener(OnDockedGrabExited);
        }

        private void BindToggleInput()
        {
            if (!Application.isPlaying || m_ToggleVisibilityAction == null)
                return;

            m_ToggleVisibilityAction.action.Enable();
            m_ToggleVisibilityAction.action.performed -= OnToggleVisibilityPerformed;
            m_ToggleVisibilityAction.action.performed += OnToggleVisibilityPerformed;
        }

        private void UnbindToggleInput()
        {
            if (m_ToggleVisibilityAction == null)
                return;

            m_ToggleVisibilityAction.action.performed -= OnToggleVisibilityPerformed;
            m_ToggleVisibilityAction.action.Disable();
        }

        private void UnbindDockedGrabEvents()
        {
            if (m_DockedGrabInteractable == null)
                return;

            m_DockedGrabInteractable.selectEntered.RemoveListener(OnDockedGrabEntered);
            m_DockedGrabInteractable.selectExited.RemoveListener(OnDockedGrabExited);
        }

        private void OnToggleVisibilityPerformed(InputAction.CallbackContext context)
        {
            if (m_CurrentMode == PanelPlacementMode.WorldPinned)
            {
                m_DockedVisible = true;
                SetPlacementMode(PanelPlacementMode.ControllerFollow);
                return;
            }

            ToggleDockedVisibility();
        }

        private void OnDockedGrabEntered(SelectEnterEventArgs args)
        {
            if (m_CurrentMode != PanelPlacementMode.ControllerFollow)
                return;

            m_KeepDockedGrabActiveUntilRelease = true;
            SetPlacementMode(PanelPlacementMode.WorldPinned);
        }

        private void OnDockedGrabExited(SelectExitEventArgs args)
        {
            if (!m_KeepDockedGrabActiveUntilRelease)
                return;

            m_KeepDockedGrabActiveUntilRelease = false;
            ApplyMode(snapDockPose: false);
        }

        private void SetDockedGrabActive(bool active)
        {
            if (m_DockedGrabCollider != null)
                m_DockedGrabCollider.enabled = active;

            if (m_DockedGrabInteractable == null)
            {
                if (active)
                    RefreshDockedGrabCollider();
                return;
            }

            var colliders = m_DockedGrabInteractable.colliders;
            if (colliders != null)
            {
                colliders.Clear();
                if (active && m_DockedGrabCollider != null)
                    colliders.Add(m_DockedGrabCollider);
            }

            m_DockedGrabInteractable.enabled = active;

            if (active)
                RefreshDockedGrabCollider();
        }

        private void SetManagedUiVisible(bool visible)
        {
            var documents = GetComponentsInChildren<UIDocument>(true);
            if (documents == null || documents.Length == 0)
                return;

            foreach (var document in documents)
            {
                if (document == null)
                    continue;

                document.gameObject.SetActive(visible);
            }
        }

        private void RefreshDockedGrabCollider()
        {
            if (m_DockedGrabCollider == null)
                return;

            if (!TryGetPanelBounds(out var center, out var size))
                return;

            m_DockedGrabCollider.center = center;
            m_DockedGrabCollider.size = new Vector3(
                Mathf.Max(0.01f, size.x + (m_DockedGrabPadding.x * 2f)),
                Mathf.Max(0.01f, size.y + (m_DockedGrabPadding.y * 2f)),
                Mathf.Max(0.01f, m_DockedGrabDepth));
        }

        private bool TryGetPanelBounds(out Vector3 center, out Vector3 size)
        {
            center = Vector3.zero;
            size = Vector3.zero;

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
            size = new Vector3(max.x - min.x, max.y - min.y, 0f);
            return true;
        }

        private static bool TryGetDocumentSize(UIDocument document, out Vector2 size)
        {
            size = default;
            if (document == null)
                return false;

            if (TryGetWorldSpaceSizeMode(document, out var sizeMode) &&
                sizeMode == WorldSpaceSizeModeDynamic &&
                TryGetResolvedLayoutSize(document, out size))
            {
                return true;
            }

            if (TryGetWorldSpaceDimension(document, WorldSpaceWidthField, out var width) &&
                TryGetWorldSpaceDimension(document, WorldSpaceHeightField, out var height) &&
                width > 0f &&
                height > 0f)
            {
                size = new Vector2(width, height) * 0.01f;
                return true;
            }

            return TryGetResolvedLayoutSize(document, out size);
        }

        private static bool TryGetResolvedLayoutSize(UIDocument document, out Vector2 size)
        {
            size = default;
            var root = document.rootVisualElement;
            if (root == null)
                return false;

            var layout = root.layout;
            if (layout.width <= 0f || layout.height <= 0f)
                return false;

            size = new Vector2(layout.width, layout.height) * 0.01f;
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

        private static bool IsAllowedGrabInteractor(IXRInteractor interactor)
        {
            if (interactor == null)
                return false;

            if (interactor is XRDirectInteractor)
                return true;

            var component = interactor.transform != null ? interactor.transform.GetComponent<XRDirectInteractor>() : null;
            if (component != null)
                return true;

            var name = interactor.transform != null ? interactor.transform.name : string.Empty;
            return !string.IsNullOrWhiteSpace(name) && name.IndexOf("Direct", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
