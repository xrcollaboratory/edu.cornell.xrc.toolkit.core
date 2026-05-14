using UnityEngine;
using UnityEngine.InputSystem;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Input component for gestural float control.
    /// Hold the assigned button to activate the gesture; release to end it.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="GesturalFloatLogic"/> on the same GameObject.
    /// </remarks>
    [RequireComponent(typeof(GesturalFloatLogic))]
    public class GesturalFloatInput : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Button action to hold for the gesture (e.g. X button).")]
        private InputActionProperty m_GestureAction;

        private GesturalFloatLogic m_Logic;

        private void Awake()
        {
            m_Logic = GetComponent<GesturalFloatLogic>();

            if (m_GestureAction.action == null)
            {
                Debug.LogError($"[{nameof(GesturalFloatInput)}] Gesture action is not assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (m_GestureAction.action == null)
                return;

            m_GestureAction.action.Enable();
            m_GestureAction.action.started += OnGestureStarted;
            m_GestureAction.action.canceled += OnGestureCanceled;
        }

        private void OnDisable()
        {
            if (m_GestureAction.action == null)
                return;

            m_GestureAction.action.started -= OnGestureStarted;
            m_GestureAction.action.canceled -= OnGestureCanceled;
            m_GestureAction.action.Disable();
        }

        private void OnGestureStarted(InputAction.CallbackContext context)
        {
            m_Logic.BeginGesture();
        }

        private void OnGestureCanceled(InputAction.CallbackContext context)
        {
            m_Logic.EndGesture();
        }
    }
}
