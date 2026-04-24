using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRHandsSetup : MonoBehaviour
{
    [SerializeField]
    private GameObject m_LeftHand;

    [SerializeField]
    private GameObject m_RightHand;

    [SerializeField]
    private XRInputModalityManager m_XRInputModalityManager;

    [SerializeField]
    [Tooltip("If assigned, reparents this GameObject under the target transform on start.")]
    private Transform m_ParentObject;

    void Start()
    {
        if (m_ParentObject == null)
        {
            var cameraOffset = GameObject.Find("Camera Offset");
            if (cameraOffset != null)
            {
                m_ParentObject = cameraOffset.transform;
            }
        }

        if (m_ParentObject != null)
        {
            transform.SetParent(m_ParentObject);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        if (m_XRInputModalityManager == null)
        {
            m_XRInputModalityManager = FindAnyObjectByType<XRInputModalityManager>();
        }

        if (m_XRInputModalityManager == null)
        {
            Debug.LogWarning($"[{nameof(XRHandsSetup)}] No XRInputModalityManager found. Hand injection skipped.", this);
            return;
        }

        m_XRInputModalityManager.leftHand = m_LeftHand;
        m_XRInputModalityManager.rightHand = m_RightHand;
    }
}
