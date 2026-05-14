using UnityEngine;
using UnityEngine.UIElements;

namespace XRC.Toolkit.Core
{
    /// <summary>
    /// Declares metadata for a scene-authored UI panel shown by <see cref="NavigationRailController"/>.
    /// Attach to the panel root GameObject that also owns a UIDocument.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class UIPanelDefinition : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Object _icon;

        private UIDocument _panelDocument;

        public UnityEngine.Object Icon => _icon;
        public UIDocument PanelDocument => _panelDocument != null ? _panelDocument : (_panelDocument = GetComponent<UIDocument>());
        public VisualElement Root => PanelDocument?.rootVisualElement;

        public void ApplyIcon(Image target)
        {
            if (target == null)
                return;

            switch (_icon)
            {
                case VectorImage vectorImage:
                    target.vectorImage = vectorImage;
                    target.sprite = null;
                    target.image = null;
                    break;
                case Sprite sprite:
                    target.vectorImage = null;
                    target.sprite = sprite;
                    target.image = null;
                    break;
                case Texture2D texture:
                    target.vectorImage = null;
                    target.sprite = null;
                    target.image = texture;
                    break;
                default:
                    target.vectorImage = null;
                    target.sprite = null;
                    target.image = null;
                    break;
            }
        }

    }
}
