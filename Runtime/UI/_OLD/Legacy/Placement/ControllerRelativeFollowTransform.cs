using UnityEngine;

namespace XRC.Toolkit.Core.Legacy
{
    /// <summary>
    /// Keeps a transform following the left-hand controller with a stable local offset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ControllerRelativeFollowTransform : MonoBehaviour
    {
        [SerializeField] private Transform m_FollowTarget;
        [SerializeField] private bool m_AutoFindLeftController = true;
        [SerializeField] private Vector3 m_FollowOffset = new(0.12f, 0.02f, 0.22f);
        [SerializeField] private Vector3 m_RotationOffsetEuler;

        private void Awake()
        {
            AutoWireReferences();
        }

        private void OnEnable()
        {
            AutoWireReferences();

            if (!Application.isPlaying)
                return;

            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            if (!TryGetDesiredPose(out var desiredPosition, out var desiredRotation))
                return;

            transform.SetPositionAndRotation(desiredPosition, desiredRotation);
        }

        public bool SnapToTarget()
        {
            if (!TryGetDesiredPose(out var position, out var rotation))
                return false;

            transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        private void AutoWireReferences() { }

        private bool TryGetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation)
        {
            desiredPosition = transform.position;
            desiredRotation = transform.rotation;

            var followTarget = ResolveFollowTarget();
            if (followTarget == null)
                return false;

            desiredPosition = followTarget.TransformPoint(m_FollowOffset);
            desiredRotation = followTarget.rotation *
                              Quaternion.Euler(m_RotationOffsetEuler);
            return true;
        }

        private Transform ResolveFollowTarget()
        {
            if (m_FollowTarget != null)
                return m_FollowTarget;

            if (!m_AutoFindLeftController)
                return null;

            m_FollowTarget = FindBestLeftControllerCandidate();
            return m_FollowTarget;
        }

        private static Transform FindBestLeftControllerCandidate()
        {
            var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Transform best = null;
            var bestScore = int.MinValue;

            foreach (var candidate in transforms)
            {
                if (candidate == null)
                    continue;

                var score = ScoreCandidate(candidate.name);
                if (score <= bestScore)
                    continue;

                best = candidate;
                bestScore = score;
            }

            return best;
        }

        private static int ScoreCandidate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return int.MinValue;

            name = name.ToLowerInvariant();

            var hasLeft = name.Contains("left");
            var hasInteractor = name.Contains("interactor");
            var hasDirect = name.Contains("direct");
            var hasController = name.Contains("controller");
            var hasHand = name.Contains("hand");

            if (!hasLeft || (!hasInteractor && !hasController && !hasHand))
                return int.MinValue;

            var score = 0;
            if (hasLeft)
                score += 100;
            if (hasInteractor)
                score += 40;
            if (hasDirect)
                score += 30;
            if (hasController)
                score += 20;
            if (hasHand)
                score += 10;

            return score;
        }
    }
}
