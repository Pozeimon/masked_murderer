using UnityEngine;

namespace MaskedMurderer.Game
{
    public class InteractableClue : MonoBehaviour
    {
        [SerializeField] private string clueId;
        [SerializeField] private bool hideOnCollect;

        private ClueManager clueManager;
        private CluePulse pulse;

        public void Initialize(ClueManager manager, string id)
        {
            clueManager = manager;
            clueId = id;
            if (pulse == null)
            {
                pulse = GetComponent<CluePulse>();
            }
        }

        public void OnInteract()
        {
            if (clueManager == null || string.IsNullOrEmpty(clueId))
            {
                return;
            }

            bool unlocked = clueManager.TryUnlockClue(clueId, "tap");
            if (pulse == null)
            {
                pulse = GetComponent<CluePulse>();
            }
            if (pulse != null)
            {
                pulse.Pulse();
            }

            if (unlocked && hideOnCollect)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
