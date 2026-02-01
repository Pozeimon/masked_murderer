using UnityEngine;

public class DestroyClueLocationObjectOnEvent : MonoBehaviour
{
    
    [SerializeField] GameObject ClueLocation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ButtonLogic.OnUIStartButtonClicked += SelfDestruct;
    }

    void SelfDestruct()
    {
        Destroy(ClueLocation);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
