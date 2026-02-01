using UnityEngine;
using TheTear.Story;

namespace TheTear.Interaction
{
    public class InteractableClue : MonoBehaviour, IInteractable
    {
        [SerializeField] private string clueId;
        private ClueManager clueManager;

        public void Initialize(ClueManager manager, string id)
        {
            clueManager = manager;
            clueId = id;
        }

        public void OnInteract()
        {
            if (clueManager != null)
            {
                clueManager.TryUnlockClue(clueId, "tap");
            }
        }
    }
}
