using UnityEngine;

namespace XRC.Toolkit.Core.Samples
{
    /// <summary>
    /// Passes the scale from the provided transform to the game object's shader.
    /// This is used when Grab Move manipulates the scale of the XR Origin game object and the shader needs that scale value to render consistently from the camera's perspective, as it moves with XR Origin.
    /// </summary>
    public class SetShaderScale : MonoBehaviour
    {
        [SerializeField]
        private Transform m_ScaleTransform;
    
        private Material m_Material;

        void Start()
        {
            m_Material = GetComponent<Renderer>().material;
        }

        // Update is called once per frame
        void Update()
        {
            var scale = m_ScaleTransform.localScale.x;
            m_Material.SetFloat("_Scale", scale);
        }
    }
}