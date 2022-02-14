using UnityEngine;
using UnityEngine.EventSystems;

namespace NoZoom {
    public class WindowZoomBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public bool hasPointer = false;

        public void OnPointerEnter(PointerEventData _eventData) {
            hasPointer = true;
        }

        public void OnPointerExit(PointerEventData _eventData) {
            hasPointer = false;
        }

        public static WindowZoomBlocker MakeWindowZoomBlocker(GameObject gameObject) {
            return gameObject.AddComponent<WindowZoomBlocker>();
        }
    }
}
