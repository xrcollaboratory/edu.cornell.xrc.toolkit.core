using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Spatial feedback component for gestural float control. Renders a world-space line along the
    /// gesture's axis with a tick that slides between the target's min and max as the value changes,
    /// an optional text label that travels with the tick, and haptic pulses at configurable step intervals.
    /// </summary>
    /// <remarks>
    /// The line is centered on <see cref="m_Anchor"/> + <see cref="m_AnchorOffset"/> (in the anchor's
    /// local space) and damped toward that target each frame. Assign the head transform for a soft
    /// headlock, an empty scene GameObject for a fixed-location crosshair, or any other transform.
    ///
    /// When multiple <c>GesturalFloatFeedback</c> instances on different gesture logics share the same
    /// anchor and offset, each derives the same world position every frame and the lines render
    /// through a common point — emergent 2- or 3-axis crosshair, no orchestrator required.
    ///
    /// Discrete mode is implicit: when <see cref="m_DiscreteLabels"/> is populated, the tick snaps to
    /// integer step positions and the label shows <c>m_DiscreteLabels[RoundToInt(value - minValue)]</c>.
    /// Otherwise the tick slides continuously and the label uses <see cref="m_DisplayFormat"/>.
    ///
    /// Visual transforms are written from <see cref="Application.onBeforeRender"/> to stay in lockstep
    /// with the XR camera's render-time pose; this prevents reprojection strobe during head motion.
    /// </remarks>
    [RequireComponent(typeof(GesturalFloatLogic))]
    public class GesturalFloatFeedback : MonoBehaviour
    {
        [Header("Anchor")]
        [SerializeField]
        [Tooltip("Transform the indicator is anchored to. Assign the head transform for a soft headlock, " +
                 "or any scene GameObject for a fixed location. Falls back to GesturalFloatLogic.head when null.")]
        private Transform m_Anchor;

        [SerializeField]
        [Tooltip("Offset from the anchor in the anchor's local space (x = right, y = up, z = forward). " +
                 "Scaled by XR Origin scale at runtime.")]
        private Vector3 m_AnchorOffset = new Vector3(0f, -0.1f, 0.5f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("Damping speed for the anchor. Higher = follows the anchor more aggressively. 0 = fully world-locked once the gesture starts.")]
        private float m_AnchorSmoothing = 6f;

        [Header("Line")]
        [SerializeField]
        [Tooltip("Total length of the axis line in Unity units, scaled by XR Origin scale to stay perceptually constant.")]
        private float m_AxisLength = 0.3f;

        [SerializeField]
        [Tooltip("Material used for the line and tick. Use an overlay material to render on top of scene geometry.")]
        private Material m_LineMaterial;

        [SerializeField]
        [Tooltip("Color tinted onto the line for this gesture axis. Use distinct colors per gesture (e.g. blue/red/green for vertical/horizontal/depth) to differentiate axes in a crosshair.")]
        private Color m_LineColor = new Color(0.2f, 0.6f, 1.0f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("Width of the axis line.")]
        private float m_LineWidth = 0.005f;

        [Header("Tick")]
        [SerializeField]
        [Tooltip("Prefab for the tick that slides along the line. Tinted with AccentColor on instantiate.")]
        private GameObject m_TickPrefab;

        [SerializeField]
        [Tooltip("Color applied to the tick material and the label text. Set independently from LineColor so the value indicator can stand out against the line.")]
        private Color m_AccentColor = Color.white;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Multiplier applied to the tick prefab's local scale. Scaled by XR Origin scale.")]
        private float m_TickScale = 1f;

        [Header("Tick Label (optional)")]
        [SerializeField]
        [Tooltip("Optional prefab containing a TextMeshPro component, instantiated once and positioned with the tick. Leave null to disable the spatial label.")]
        private GameObject m_LabelPrefab;

        [SerializeField]
        [Tooltip("Offset from the tick (in the tick's local space) for the label position. Scaled by XR Origin scale.")]
        private Vector3 m_LabelOffset = new Vector3(0f, 0.05f, 0f);

        [SerializeField]
        [Tooltip("Optional prefix prepended to the displayed value (e.g. \"Playback Speed\", \"Trajectory Mode\"). A space is inserted between the prefix and the value when both are non-empty.")]
        private string m_LabelPrefix = "";

        [SerializeField]
        [Tooltip("Format string used when DiscreteLabels is empty. Uses string.Format with {0} as the value.")]
        private string m_DisplayFormat = "{0:F1}";

        [SerializeField]
        [Tooltip("When populated, the tick snaps to integer step positions and the label uses these strings indexed by RoundToInt(value - MinValue). " +
                 "Step count is derived from MaxValue - MinValue + 1. Use for discrete targets such as TrajectoryModeTarget.")]
        private string[] m_DiscreteLabels;

        [Header("Haptic Feedback")]
        [SerializeField]
        [Tooltip("Whether to trigger haptic feedback at each value step.")]
        private bool m_DoHapticFeedback = true;

        [SerializeField]
        [Tooltip("The XR controller that receives haptic feedback impulses.")]
        private HapticsUtility.Controller m_HapticController = HapticsUtility.Controller.Right;

        [SerializeField]
        [Min(0)]
        [Tooltip("The intensity of the haptic impulse, from 0 (none) to 1 (maximum).")]
        private float m_HapticAmplitude = 0.1f;

        [SerializeField]
        [Min(0)]
        [Tooltip("The duration of the haptic impulse in seconds.")]
        private float m_HapticDuration = 0.01f;

        [SerializeField]
        [Min(0.001f)]
        [Tooltip("The value increment between haptic pulses.")]
        private float m_HapticStepSize = 0.1f;

        private GesturalFloatLogic m_Logic;
        private GameObject m_LineObject;
        private LineRenderer m_LineRenderer;
        private GameObject m_Tick;
        private GameObject m_Label;
        private TextMeshPro m_LabelText;
        private Material m_RuntimeLineMaterial;
        private Material m_RuntimeTickMaterial;
        private Vector3 m_AnchorPosition;
        private bool m_AnchorInitialized;
        private int m_LastHapticStep;

        private bool isDiscrete => m_DiscreteLabels != null && m_DiscreteLabels.Length > 0;

        private void Awake()
        {
            m_Logic = GetComponent<GesturalFloatLogic>();
            CreateLineRenderer();
            CreateTick();
            CreateLabel();
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (m_Logic == null)
                return;

            m_Logic.gestureStarted += OnGestureStarted;
            m_Logic.gestureStopped += OnGestureStopped;
            m_Logic.valueChanged += OnValueChanged;
            Application.onBeforeRender += OnBeforeRender;
        }

        private void OnDisable()
        {
            if (m_Logic == null)
                return;

            m_Logic.gestureStarted -= OnGestureStarted;
            m_Logic.gestureStopped -= OnGestureStopped;
            m_Logic.valueChanged -= OnValueChanged;
            Application.onBeforeRender -= OnBeforeRender;
        }

        private void OnDestroy()
        {
            if (m_RuntimeLineMaterial != null)
                Destroy(m_RuntimeLineMaterial);
            if (m_RuntimeTickMaterial != null)
                Destroy(m_RuntimeTickMaterial);
        }

        [BeforeRenderOrder(100)]
        private void OnBeforeRender()
        {
            if (m_Logic == null || !m_Logic.isGestureActive)
                return;

            UpdateAnchor();
            UpdateLineAndTick(m_Logic.currentValue);
        }

        private void CreateLineRenderer()
        {
            m_LineObject = new GameObject($"{nameof(GesturalFloatFeedback)} Line");
            m_LineObject.transform.SetParent(transform, false);

            m_LineRenderer = m_LineObject.AddComponent<LineRenderer>();
            m_LineRenderer.useWorldSpace = true;
            m_LineRenderer.positionCount = 2;
            m_LineRenderer.startWidth = m_LineWidth;
            m_LineRenderer.endWidth = m_LineWidth;
            m_LineRenderer.startColor = m_LineColor;
            m_LineRenderer.endColor = m_LineColor;

            if (m_LineMaterial != null)
            {
                m_RuntimeLineMaterial = new Material(m_LineMaterial);
                m_RuntimeLineMaterial.SetColor(Shader.PropertyToID("_BaseColor"), m_LineColor);
                m_LineRenderer.material = m_RuntimeLineMaterial;
            }
            else
            {
                Debug.LogWarning($"[{nameof(GesturalFloatFeedback)}] No line material assigned. Assign a material to ensure the axis renders correctly in builds.", this);
            }
        }

        private void CreateTick()
        {
            if (m_TickPrefab == null)
                return;

            m_Tick = Instantiate(m_TickPrefab, transform);

            var tickRenderer = m_Tick.GetComponentInChildren<Renderer>();
            if (tickRenderer != null && m_LineMaterial != null)
            {
                m_RuntimeTickMaterial = new Material(m_LineMaterial);
                m_RuntimeTickMaterial.SetColor(Shader.PropertyToID("_BaseColor"), m_AccentColor);
                tickRenderer.material = m_RuntimeTickMaterial;
            }
        }

        private void CreateLabel()
        {
            if (m_LabelPrefab == null)
                return;

            m_Label = Instantiate(m_LabelPrefab, transform);
            m_LabelText = m_Label.GetComponentInChildren<TextMeshPro>();

            if (m_LabelText != null)
                m_LabelText.color = m_AccentColor;
            else
                Debug.LogWarning($"[{nameof(GesturalFloatFeedback)}] LabelPrefab is missing a TextMeshPro component. Label will not display text.", this);
        }

        private void OnGestureStarted()
        {
            CaptureAnchor();
            UpdateLineAndTick(m_Logic.currentValue);
            m_LastHapticStep = Mathf.RoundToInt(m_Logic.currentValue / m_HapticStepSize);
            SetVisible(true);
        }

        private void OnGestureStopped()
        {
            SetVisible(false);
        }

        private void OnValueChanged(float value)
        {
            if (!m_DoHapticFeedback || !m_Logic.isGestureActive)
                return;

            int step = Mathf.RoundToInt(value / m_HapticStepSize);
            if (step != m_LastHapticStep)
            {
                m_LastHapticStep = step;
                HapticsUtility.SendHapticImpulse(m_HapticAmplitude, m_HapticDuration, m_HapticController);
            }
        }

        private void UpdateAnchor()
        {
            Vector3 target = ComputeAnchorPosition();

            if (!m_AnchorInitialized || m_AnchorSmoothing <= 0f)
            {
                m_AnchorPosition = target;
                m_AnchorInitialized = true;
                return;
            }

            float t = 1f - Mathf.Exp(-m_AnchorSmoothing * Time.deltaTime);
            m_AnchorPosition = Vector3.Lerp(m_AnchorPosition, target, t);
        }

        private void CaptureAnchor()
        {
            m_AnchorPosition = ComputeAnchorPosition();
            m_AnchorInitialized = true;
        }

        private Vector3 ComputeAnchorPosition()
        {
            Transform anchor = m_Anchor != null ? m_Anchor : m_Logic.head;
            if (anchor == null)
                return m_AnchorPosition;

            Transform xrOrigin = m_Logic.xrOrigin;
            float scale = xrOrigin != null ? Mathf.Max(xrOrigin.localScale.x, 0.001f) : 1f;

            return anchor.position + anchor.rotation * (m_AnchorOffset * scale);
        }

        private void UpdateLineAndTick(float value)
        {
            if (m_LineRenderer == null)
                return;

            Vector3 axis = m_Logic.GetGestureAxisDirection().normalized;
            float scale = m_Logic.xrOrigin != null ? Mathf.Max(m_Logic.xrOrigin.localScale.x, 0.001f) : 1f;
            float halfLength = 0.5f * m_AxisLength * scale;

            Vector3 minPoint = m_AnchorPosition - axis * halfLength;
            Vector3 maxPoint = m_AnchorPosition + axis * halfLength;

            m_LineRenderer.startWidth = m_LineWidth * scale;
            m_LineRenderer.endWidth = m_LineWidth * scale;
            m_LineRenderer.SetPosition(0, minPoint);
            m_LineRenderer.SetPosition(1, maxPoint);

            float min = m_Logic.minValue;
            float max = m_Logic.maxValue;
            float normalized = Mathf.Approximately(max, min)
                ? 0f
                : Mathf.InverseLerp(min, max, value);

            if (isDiscrete)
            {
                int steps = Mathf.Max(1, Mathf.RoundToInt(max - min));
                int stepIndex = Mathf.Clamp(Mathf.RoundToInt(value - min), 0, steps);
                normalized = (float)stepIndex / steps;
            }

            Vector3 tickPosition = Vector3.Lerp(minPoint, maxPoint, normalized);

            if (m_Tick != null)
            {
                m_Tick.transform.position = tickPosition;
                m_Tick.transform.localScale = m_TickPrefab.transform.localScale * (m_TickScale * scale);
            }

            UpdateLabel(value, tickPosition, scale);
        }

        private void UpdateLabel(float value, Vector3 tickPosition, float scale)
        {
            if (m_Label == null)
                return;

            Vector3 up = m_Logic.xrOrigin != null ? m_Logic.xrOrigin.up : Vector3.up;
            Quaternion labelOrientation = Quaternion.LookRotation(m_Logic.head != null ? m_Logic.head.forward : Vector3.forward, up);
            m_Label.transform.position = tickPosition + labelOrientation * (m_LabelOffset * scale);

            if (m_Logic.head != null)
            {
                Vector3 toHead = m_Logic.head.position - m_Label.transform.position;
                if (toHead.sqrMagnitude > 0.0001f)
                    m_Label.transform.rotation = Quaternion.LookRotation(-toHead, up);
            }

            if (m_LabelText != null)
                m_LabelText.text = FormatValue(value);
        }

        private string FormatValue(float value)
        {
            string formatted;
            if (isDiscrete)
            {
                int min = Mathf.RoundToInt(m_Logic.minValue);
                int index = Mathf.Clamp(Mathf.RoundToInt(value) - min, 0, m_DiscreteLabels.Length - 1);
                formatted = m_DiscreteLabels[index];
            }
            else
            {
                try
                {
                    formatted = string.Format(m_DisplayFormat, value);
                }
                catch (System.FormatException)
                {
                    Debug.LogError($"[{nameof(GesturalFloatFeedback)}] Invalid display format: '{m_DisplayFormat}'. Falling back to default.", this);
                    m_DisplayFormat = "{0:F1}";
                    formatted = string.Format(m_DisplayFormat, value);
                }
            }

            if (string.IsNullOrEmpty(m_LabelPrefix))
                return formatted;

            return $"{m_LabelPrefix} {formatted}";
        }

        private void SetVisible(bool visible)
        {
            if (m_LineObject != null)
                m_LineObject.SetActive(visible);
            if (m_Tick != null)
                m_Tick.SetActive(visible);
            if (m_Label != null)
                m_Label.SetActive(visible);
        }
    }
}
