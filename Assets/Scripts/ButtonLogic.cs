using System;
using UnityEngine;
using UnityEngine.UI;

public class ButtonLogic : MonoBehaviour
{
    [SerializeField] private Button UIStartButton;
    public GameObject StateMachine;
    
    [SerializeField] string currentMask;
    [SerializeField] string currentState;
    
    public static event Action OnUIStartButtonClicked;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UIStartButton.onClick.AddListener(OnStartButtonClicked);
    }

    void OnStartButtonClicked()
    {
        OnUIStartButtonClicked?.Invoke();
        UIStartButton.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        currentMask = StateMachine.GetComponent<StateMachineTracker>().currentMask;
        Debug.Log(currentMask);
        currentState = StateMachine.GetComponent<StateMachineTracker>().currentState;
        Debug.Log(currentState);
    }
}
