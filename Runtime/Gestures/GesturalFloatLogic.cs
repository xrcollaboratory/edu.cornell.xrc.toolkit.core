using System;
using UnityEngine;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Axis along which the gesture tracks controller displacement.
    /// </summary>
    public enum GestureAxis
    {
        /// <summary>Tracks displacement along the XR Origin's up axis.</summary>
        Vertical,

        /// <summary>Tracks displacement along the head's right axis.</summary>
        Horizontal,

        /// <summary>Tracks displacement along the head's forward axis.</summary>
        Depth
    }

    /// <summary>
    /// Generic gestural command that maps controller displacement along a configurable axis
    /// to changes on a float value provided by a <see cref="GesturalFloatTarget"/>.
    /// </summary>
    /// <remarks>
    /// The gesture reads the current value from the target when the gesture begins,
    /// then continuously updates the target as the controller moves.
    /// This component is agnostic to what float it controls — the concrete
    /// <see cref="GesturalFloatTarget"/> subclass determines the actual get/set behavior.
    /// </remarks>
    public class GesturalFloatLogic : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The target that provides and receives the float value.")]
        private GesturalFloatTarget m_Target;

        [SerializeField]
        [Tooltip("Transform of the controller used for position tracking.")]
        private Transform m_ControllerTransform;

        [SerializeField]
        [Tooltip("The head/camera transform for axis reference.")]
        private Transform m_Head;

        [SerializeField]
        [Tooltip("The XR Origin transform. Scale is read from localScale.x (uniform).")]
        private Transform m_XROrigin;

        [SerializeField]
        [Tooltip("Which axis to track for the gesture.")]
        private GestureAxis m_GestureAxis = GestureAxis.Vertical;

        [SerializeField]
        [Tooltip("Distance in Unity units of controller displacement required to produce a 1.0 change in value. Lower values produce faster changes. Default 0.1 (10cm per unit).")]
        private float m_Sensitivity = 0.1f;

        [SerializeField]
        [Tooltip("Minimum value clamp.")]
        private float m_MinValue = 0f;

        [SerializeField]
        [Tooltip("Maximum value clamp.")]
        private float m_MaxValue = 1.0f;

        [SerializeField]
        [Tooltip("Minimum displacement along the gesture axis before value changes apply (in Unity units). Prevents cross-axis contamination when multiple gestures are active.")]
        private float m_DeadZone = 0.03f;

        private bool m_IsGestureActive;
        private Vector3 m_StartWorldPosition;
        private float m_StartValue;
        private float m_CurrentValue;
        private bool m_DeadZoneBroken;
        private float m_DeadZoneSign;

        /// <summary>
        /// Whether the gesture is currently active.
        /// </summary>
        public bool isGestureActive => m_IsGestureActive;

        /// <summary>
        /// The most recent value set on the target during the current or last gesture.
        /// </summary>
        public float currentValue => m_CurrentValue;

        /// <summary>
        /// The controller transform used for position tracking.
        /// </summary>
        public Transform controllerTransform => m_ControllerTransform;

        /// <summary>
        /// The head/camera transform for axis reference and billboard orientation.
        /// </summary>
        public Transform head => m_Head;

        /// <summary>
        /// The XR Origin transform.
        /// </summary>
        public Transform xrOrigin => m_XROrigin;

        /// <summary>
        /// The target that provides and receives the float value.
        /// </summary>
        public GesturalFloatTarget target => m_Target;

        /// <summary>
        /// Minimum value clamp applied to the target.
        /// </summary>
        public float minValue => m_MinValue;

        /// <summary>
        /// Maximum value clamp applied to the target.
        /// </summary>
        public float maxValue => m_MaxValue;

        /// <summary>
        /// The axis the gesture tracks displacement along.
        /// </summary>
        public GestureAxis gestureAxis => m_GestureAxis;

        /// <summary>
        /// Returns the world-space unit vector for the configured gesture axis.
        /// Vertical uses XR Origin up, Horizontal uses head right, Depth uses head forward.
        /// </summary>
        public Vector3 GetGestureAxisDirection() => GetGestureAxis();

        /// <summary>
        /// Raised when the gesture begins.
        /// </summary>
        public event Action gestureStarted;

        /// <summary>
        /// Raised when the gesture ends.
        /// </summary>
        public event Action gestureStopped;

        /// <summary>
        /// Raised when the value changes during an active gesture.
        /// </summary>
        public event Action<float> valueChanged;

        private void Awake()
        {
            if (m_Target == null)
            {
                Debug.LogError($"[{nameof(GesturalFloatLogic)}] Target reference is missing.", this);
                enabled = false;
                return;
            }

            if (m_ControllerTransform == null)
            {
                Debug.LogError($"[{nameof(GesturalFloatLogic)}] ControllerTransform reference is missing.", this);
                enabled = false;
                return;
            }

            if (m_Head == null)
            {
                Debug.LogError($"[{nameof(GesturalFloatLogic)}] Head transform reference is missing.", this);
                enabled = false;
                return;
            }

            if (m_XROrigin == null)
            {
                Debug.LogError($"[{nameof(GesturalFloatLogic)}] XROrigin reference is missing.", this);
                enabled = false;
                return;
            }

            if (m_Sensitivity < 0.001f)
            {
                Debug.LogWarning($"[{nameof(GesturalFloatLogic)}] Sensitivity too low, clamping to 0.001.", this);
                m_Sensitivity = 0.001f;
            }

            if (m_MinValue >= m_MaxValue)
            {
                Debug.LogWarning($"[{nameof(GesturalFloatLogic)}] MinValue ({m_MinValue}) must be less than MaxValue ({m_MaxValue}). Resetting to defaults.", this);
                m_MinValue = 0f;
                m_MaxValue = 1.0f;
            }
        }

        private void Update()
        {
            if (!m_IsGestureActive || m_Target == null || !m_Target.enabled)
                return;

            Vector3 displacement = m_ControllerTransform.position - m_StartWorldPosition;
            float delta = Vector3.Dot(displacement, GetGestureAxis());
            float scale = Mathf.Max(m_XROrigin.localScale.x, 0.001f);
            float scaledDeadZone = m_DeadZone * scale;

            if (!m_DeadZoneBroken)
            {
                if (Mathf.Abs(delta) < scaledDeadZone)
                    return;

                m_DeadZoneBroken = true;
                m_DeadZoneSign = Mathf.Sign(delta);
            }

            float adjustedDelta = delta - m_DeadZoneSign * scaledDeadZone;
            float scaledSensitivity = m_Sensitivity * scale;
            float newValue = Mathf.Clamp(m_StartValue + (adjustedDelta / scaledSensitivity), m_MinValue, m_MaxValue);

            m_CurrentValue = newValue;
            m_Target.SetValue(newValue);
            valueChanged?.Invoke(newValue);
        }

        /// <summary>
        /// Begins the gesture, capturing the current controller position and target value.
        /// </summary>
        public void BeginGesture()
        {
            if (m_IsGestureActive || m_Target == null || !m_Target.enabled)
                return;

            m_StartWorldPosition = m_ControllerTransform.position;
            m_StartValue = m_Target.GetValue();
            m_CurrentValue = m_StartValue;
            m_DeadZoneBroken = false;
            m_DeadZoneSign = 0f;
            m_IsGestureActive = true;
            gestureStarted?.Invoke();
        }

        /// <summary>
        /// Ends the gesture.
        /// </summary>
        public void EndGesture()
        {
            if (!m_IsGestureActive)
                return;

            m_IsGestureActive = false;
            gestureStopped?.Invoke();
        }

        private Vector3 GetGestureAxis()
        {
            switch (m_GestureAxis)
            {
                case GestureAxis.Vertical:
                    return m_XROrigin.up;
                case GestureAxis.Horizontal:
                    return m_Head.right;
                case GestureAxis.Depth:
                    return m_Head.forward;
                default:
                    return m_XROrigin.up;
            }
        }
    }
}
