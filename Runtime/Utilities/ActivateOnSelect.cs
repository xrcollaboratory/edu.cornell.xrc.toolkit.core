using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace XRC.Toolkit.Core
{
    public class ActivateOnSelect : MonoBehaviour
    {
        [SerializeField]
        private XRBaseInteractor m_Interactor;
        
        [SerializeField]
        private GameObject m_ActivatedObject;
        
        // XRBaseInteractor m_Interactor;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_Interactor.selectEntered.AddListener(OnSelectEntered);
            m_Interactor.selectExited.AddListener(OnSelectExited);
        }

        private void OnSelectExited(SelectExitEventArgs arg0)
        {
            m_ActivatedObject.SetActive(false);
        }

        private void OnSelectEntered(SelectEnterEventArgs arg0)
        {
            m_ActivatedObject.SetActive(true);
        }
    }
}
