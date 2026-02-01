using UnityEngine;
using UnityEngine.EventSystems;

namespace MaskedMurderer.Game
{
    public class ClueTapRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask hitMask = ~0;

        public void SetCamera(Camera cam)
        {
            targetCamera = cam;
        }

        void Update()
        {
            if (targetCamera == null)
            {
                return;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began)
                {
                    return;
                }

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }

                TryRaycast(touch.position);
            }
            else if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                TryRaycast(Input.mousePosition);
            }
        }

        private void TryRaycast(Vector2 screenPosition)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f, hitMask, QueryTriggerInteraction.Ignore))
            {
                InteractableClue interactable = hit.collider.GetComponentInParent<InteractableClue>();
                if (interactable != null)
                {
                    interactable.OnInteract();
                }
            }
        }
    }
}
