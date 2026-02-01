using UnityEngine;
using UnityEngine.EventSystems;

namespace TheTear.Interaction
{
    public class TapRaycaster : MonoBehaviour
    {
        public Camera arCamera;
        public float maxDistance = 10f;
        public LayerMask interactionMask = ~0;

        private void Update()
        {
            if (arCamera == null)
            {
                return;
            }

            if (!TryGetTap(out Vector2 screenPos, out int pointerId))
            {
                return;
            }

            if (IsPointerOverUI(pointerId))
            {
                return;
            }

            Ray ray = arCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactionMask))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    interactable.OnInteract();
                }
            }
        }

        private bool IsPointerOverUI(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            if (pointerId >= 0)
            {
                return EventSystem.current.IsPointerOverGameObject(pointerId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }

        private bool TryGetTap(out Vector2 screenPos, out int pointerId)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    screenPos = touch.position.ReadValue();
                    pointerId = touch.touchId.ReadValue();
                    return true;
                }
            }

            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                pointerId = -1;
                return true;
            }
#endif

            if (Input.touchCount > 0)
            {
                UnityEngine.Touch touch = Input.GetTouch(0);
                if (touch.phase == UnityEngine.TouchPhase.Began)
                {
                    screenPos = touch.position;
                    pointerId = touch.fingerId;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                pointerId = -1;
                return true;
            }

            screenPos = default;
            pointerId = -1;
            return false;
        }
    }
}
