using Unity.XR.CoreUtils;
using UnityEngine;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Mirrors another transform's world scale onto this GameObject every frame. Useful
    /// when a separate system (for example XRC Grab Move) drives scale changes on the
    /// player rig and unrelated objects — UI panels, auxiliary props — need to follow
    /// that scale even though they aren't parented under the rig.
    /// </summary>
    /// <remarks>
    /// The source's <see cref="Transform.lossyScale"/> is converted to a local scale on
    /// this transform by dividing out the parent's world scale, so the target's world
    /// scale ends up matching the source regardless of either object's parent hierarchy.
    /// </remarks>
    public class FollowScale : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Transform whose world scale this GameObject should match each frame. If left empty, the scene's XROrigin component is located once at Start and its transform is used.")]
        private Transform m_Source;

        private void Start()
        {
            if (m_Source != null)
                return;

            var origin = FindFirstObjectByType<XROrigin>();
            if (origin != null)
                m_Source = origin.transform;
        }

        private void LateUpdate()
        {
            if (m_Source == null)
                return;

            var sourceWorld = m_Source.lossyScale;
            var parent = transform.parent;
            if (parent == null)
            {
                transform.localScale = sourceWorld;
                return;
            }

            var parentWorld = parent.lossyScale;
            transform.localScale = new Vector3(
                SafeDivide(sourceWorld.x, parentWorld.x),
                SafeDivide(sourceWorld.y, parentWorld.y),
                SafeDivide(sourceWorld.z, parentWorld.z));
        }

        private static float SafeDivide(float numerator, float denominator)
            => Mathf.Approximately(denominator, 0f) ? numerator : numerator / denominator;
    }
}
