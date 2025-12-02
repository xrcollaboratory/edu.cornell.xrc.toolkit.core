using UnityEngine;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Attaches a child object to a parent object at runtime with configurable local transform settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component sets up a parent-child relationship between two GameObjects when the scene starts.
    /// The child object's local position, rotation, and scale are configured based on the serialized values,
    /// and the child's active state is set according to the <c>m_IsActiveOnStart</c> field.
    /// </para>
    /// <para>
    /// This is useful for dynamically attaching objects at runtime that need to maintain specific
    /// transform offsets relative to their parent, such as attaching UI elements or tools to controllers.
    /// </para>
    /// </remarks>
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