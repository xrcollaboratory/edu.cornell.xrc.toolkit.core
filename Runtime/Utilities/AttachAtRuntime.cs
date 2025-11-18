using UnityEngine;

namespace XRC.Toolkit.Core
{
    public class AttachAtRuntime : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_ParentObject;

        [SerializeField]
        private GameObject m_ChildObject;
    
        [Header("Local transform settings")]
    
        [SerializeField]
        private Vector3 m_LocalPosition = Vector3.zero;
    
        [SerializeField]
        private Quaternion m_LocalRotation = Quaternion.identity;
    
        [SerializeField]
        private Vector3 m_LocalScale = Vector3.one;

        [SerializeField]
        private bool m_IsActiveOnStart = false;

    
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_ChildObject.transform.SetParent(m_ParentObject.transform);
            m_ChildObject.transform.localPosition = m_LocalPosition;
            m_ChildObject.transform.localRotation = m_LocalRotation;
            m_ChildObject.transform.localScale = m_LocalScale;
            m_ChildObject.SetActive(m_IsActiveOnStart);
        }

    }
}