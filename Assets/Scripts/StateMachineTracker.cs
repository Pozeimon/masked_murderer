using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachineTracker : MonoBehaviour
{
    public List<GameObject> prefabs;
    public GameObject ARPlaceClueLocation;
    public string currentMask = "None"; // None/Red/Green/Blue
    public string currentState = "Start"; // Start/Ongoing/End

    public List<int> randomList;
    public int amountOfClues;
    int amountOfLocations;

    void getIndexToPlaceFrom()
    {
        randomList = new List<int>();
        NewNumber();
    }
    private void NewNumber()
    {
        for (int i = 0; i < amountOfClues; i++)
        {
        int MyNumber = UnityEngine.Random.Range(0, amountOfLocations);
        if (!randomList.Contains(MyNumber))
            {
                randomList.Add(MyNumber);
            }
        else
            {
                i--;
            }
        }
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ButtonLogic.OnUIStartButtonClicked += MoveToGameState;
    }

    void MoveToGameState()
    {
        currentMask = "Blue";
        currentState = "Ongoing";
        amountOfClues = prefabs.Count;
        amountOfLocations = ARPlaceClueLocation.GetComponent<ARPlaceClueLocation>().ClueLocations.Count;
        getIndexToPlaceFrom();

        for (int index = 0; index < amountOfClues; index ++)
        {
            Instantiate(prefabs[index], ARPlaceClueLocation.GetComponent<ARPlaceClueLocation>().ClueLocations[randomList[index]],
                                ARPlaceClueLocation.GetComponent<ARPlaceClueLocation>().ClueRotations[randomList[index]]);
        }
    }

    
    // Update is called once per frame
    void Update()
    {
        
    }
}
