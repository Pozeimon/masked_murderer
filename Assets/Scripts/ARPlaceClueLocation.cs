using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

public class ARPlaceClueLocation : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    public GameObject StateMachine;
    bool isPlacing = false;
    public List<Vector3> ClueLocations = new List<Vector3>();
    public List<Quaternion> ClueRotations = new List<Quaternion>();

    // Update is called once per frame
    void Update()
    {
        if(StateMachine.GetComponent<StateMachineTracker>().currentState != "Start") return;
        if(!raycastManager) return;

        //int touchCount = InputSystem.EnhancedTouch.Touch.activeTouches.Count;
        //int touchCount = Input.touchCount;
        //touchPhase =  InputSystem.EnhancedTouch.Touch.activeTouches[i]
        if((Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began
        || Input.GetMouseButtonDown(0)) && !isPlacing)
        {
            isPlacing = true;
            if (Input.touchCount > 0)
            {
                // Touch
                PlaceObject(Input.GetTouch(0).position);
            } else
            {
                // Mouse click
                PlaceObject(Input.mousePosition);
                //PlaceObject(Mouse.current.position.ReadValue());
            }
        }
    }

    void PlaceObject(Vector2 touchPosition)
    {
        var rayHits = new List<ARRaycastHit>();
        raycastManager.Raycast(touchPosition, rayHits, TrackableType.AllTypes);
        if (rayHits.Count > 0)
        {
            Vector3 hitPosePosition = rayHits[0].pose.position;
            Quaternion hitPoseRotation = rayHits[0].pose.rotation;
            // rayHits[0].GameObject.tag

            ClueLocations.Add(hitPosePosition);
            ClueRotations.Add(hitPoseRotation);
            Instantiate(raycastManager.raycastPrefab,
                        hitPosePosition, hitPoseRotation);
        }

        StartCoroutine(SetIsPlacingFalseWithDelay());
    }
    IEnumerator SetIsPlacingFalseWithDelay()
    {
        yield return new WaitForSeconds(0.25f);
        isPlacing = false;
    }
}
