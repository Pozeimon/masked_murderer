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
        if (UIStartButton != null)
        {
            UIStartButton.onClick.AddListener(OnStartButtonClicked);
        }
    }

    void OnStartButtonClicked()
    {
        RaiseBeginInvestigation();
        if (UIStartButton != null)
        {
            UIStartButton.gameObject.SetActive(false);
        }
    }

    public static void RaiseBeginInvestigation()
    {
        OnUIStartButtonClicked?.Invoke();
    }
}
