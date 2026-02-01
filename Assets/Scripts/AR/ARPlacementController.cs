using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace TheTear.AR
{
    public class ARPlacementController : MonoBehaviour
    {
        public ARRaycastManager raycastManager;
        public ARPlaneManager planeManager;
        public Camera arCamera;
        public SceneRootController sceneRoot;
        public Transform reticle;

        public bool IsPlacementActive => placementActive;

        public event Action OnPlaced;
        public event Action OnRelocate;
        public event Action<bool> OnTrackingStateChanged;
        public event Action OnFallbackPlaced;

        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
        private bool placementActive = true;
        private bool hasPlaced = false;

        private void OnEnable()
        {
            ARSession.stateChanged += OnARSessionStateChanged;
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= OnARSessionStateChanged;
        }

        private void Update()
        {
            if (!placementActive || raycastManager == null)
            {
                return;
            }

            UpdateReticle();

            if (!TryGetTap(out Vector2 screenPos))
            {
                return;
            }

            if (raycastManager.Raycast(screenPos, Hits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = AlignPoseToCameraYaw(Hits[0].pose);
                PlaceAtPose(pose);
            }
        }

        public void BeginPlacement()
        {
            placementActive = true;
            SetPlaneVisualization(true);
            if (reticle != null)
            {
                reticle.gameObject.SetActive(false);
            }
        }

        private void PlaceAtPose(Pose pose)
        {
            if (sceneRoot == null)
            {
                return;
            }

            if (!sceneRoot.gameObject.activeSelf)
            {
                sceneRoot.SetActive(true);
            }

            sceneRoot.transform.SetPositionAndRotation(pose.position, pose.rotation);
            placementActive = false;
            SetPlaneVisualization(false);
            if (reticle != null)
            {
                reticle.gameObject.SetActive(false);
            }

            if (!hasPlaced)
            {
                hasPlaced = true;
                OnPlaced?.Invoke();
            }
            else
            {
                OnRelocate?.Invoke();
            }
        }

        private bool TryGetTap(out Vector2 screenPos)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    screenPos = touch.position.ReadValue();
                    return true;
                }
            }

            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                return true;
            }
#endif

            if (Input.touchCount > 0)
            {
                UnityEngine.Touch touch = Input.GetTouch(0);
                if (touch.phase == UnityEngine.TouchPhase.Began)
                {
                    screenPos = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                return true;
            }

            screenPos = default;
            return false;
        }

        private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            bool trackingOk = args.state == ARSessionState.SessionTracking;
            OnTrackingStateChanged?.Invoke(trackingOk);
        }

        private void UpdateReticle()
        {
            if (reticle == null || arCamera == null || raycastManager == null)
            {
                return;
            }

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (raycastManager.Raycast(screenCenter, Hits, TrackableType.PlaneWithinPolygon))
            {
                Pose pose = AlignPoseToCameraYaw(Hits[0].pose);
                reticle.SetPositionAndRotation(pose.position, pose.rotation);
                if (!reticle.gameObject.activeSelf)
                {
                    reticle.gameObject.SetActive(true);
                }
            }
            else if (reticle.gameObject.activeSelf)
            {
                reticle.gameObject.SetActive(false);
            }
        }

        private Pose AlignPoseToCameraYaw(Pose pose)
        {
            if (arCamera == null)
            {
                return pose;
            }

            Vector3 forward = Vector3.ProjectOnPlane(arCamera.transform.forward, pose.up).normalized;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up).normalized;
            }
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(forward, pose.up);
            return new Pose(pose.position, rotation);
        }

        private void SetPlaneVisualization(bool active)
        {
            if (planeManager == null)
            {
                return;
            }

            planeManager.enabled = active;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(active);
            }
        }
    }
}
