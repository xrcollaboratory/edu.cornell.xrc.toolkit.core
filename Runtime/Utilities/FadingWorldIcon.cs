using System.Collections;
using UnityEngine;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Generic transient floating icon that briefly appears head-locked in front of the user
    /// and fades out. Triggered by calling <see cref="Appear"/> from any system that wants a
    /// short visual notification (for example a play/pause cue wired to a playback toggle).
    /// </summary>
    /// <remarks>
    /// Renders through a <see cref="UnityEngine.SpriteRenderer"/> on the same GameObject — the
    /// sprite and material are configured directly on the SpriteRenderer in the inspector.
    /// To pierce 3D occlusion, assign an overlay-capable sprite material (sprite shader with
    /// <c>ZTest Always</c>) on the SpriteRenderer; this component does not modify the shader.
    ///
    /// Pose is recomputed from <see cref="m_Head"/> inside <see cref="Application.onBeforeRender"/>
    /// so the icon stays in lockstep with the XR camera's render-time pose, avoiding the
    /// reprojection strobe that occurs when head-locked visuals are written from <c>Update</c>
    /// or <c>LateUpdate</c>.
    /// </remarks>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FadingWorldIcon : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Head transform the icon is locked to. Required — typically the XR camera transform.")]
        private Transform m_Head;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Distance in metres in front of the head where the icon appears.")]
        private float m_Distance = 1.0f;

        [SerializeField]
        [Tooltip("Additional offset in head-local space (x = right, y = up, z = forward) added on top of Distance.")]
        private Vector3 m_HeadOffset = Vector3.zero;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Duration in seconds of the fade-in.")]
        private float m_FadeInDuration = 0.1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Duration in seconds the icon stays at full opacity before fading out.")]
        private float m_HoldDuration = 0.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Duration in seconds of the fade-out.")]
        private float m_FadeOutDuration = 0.4f;

        private SpriteRenderer m_Sprite;
        private Coroutine m_Routine;
        private bool m_IsVisible;

        private void Awake()
        {
            m_Sprite = GetComponent<SpriteRenderer>();
            m_Sprite.enabled = false;
            SetAlpha(0f);

            if (m_Head == null)
            {
                Debug.LogError($"[{nameof(FadingWorldIcon)}] Head transform is not assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        /// <summary>
        /// Shows the icon, runs the fade-in / hold / fade-out cycle, then hides it.
        /// Calling again while a fade is in progress restarts the cycle from the fade-in.
        /// </summary>
        public void Appear()
        {
            if (m_Head == null)
                return;

            if (m_Routine != null)
                StopCoroutine(m_Routine);

            m_IsVisible = true;
            m_Sprite.enabled = true;
            UpdatePose();
            m_Routine = StartCoroutine(FadeRoutine());
        }

        [BeforeRenderOrder(100)]
        private void OnBeforeRender()
        {
            if (!m_IsVisible || m_Head == null)
                return;

            UpdatePose();
        }

        private void UpdatePose()
        {
            Vector3 localOffset = m_HeadOffset + Vector3.forward * m_Distance;
            transform.position = m_Head.position + m_Head.rotation * localOffset;
            transform.rotation = m_Head.rotation;
        }

        private IEnumerator FadeRoutine()
        {
            yield return Lerp(0f, 1f, m_FadeInDuration);

            float held = 0f;
            while (held < m_HoldDuration)
            {
                held += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return Lerp(1f, 0f, m_FadeOutDuration);

            m_Sprite.enabled = false;
            m_IsVisible = false;
            m_Routine = null;
        }

        private IEnumerator Lerp(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            Color c = m_Sprite.color;
            c.a = alpha;
            m_Sprite.color = c;
        }
    }
}
