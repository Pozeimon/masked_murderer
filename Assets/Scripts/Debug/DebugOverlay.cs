using UnityEngine;
using TheTear.Story;
using TheTear.UI;

namespace TheTear.Debugging
{
    public class DebugOverlay : MonoBehaviour
    {
        public ClueManager clueManager;
        public DeductionController deductionController;
        private bool show;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                show = !show;
            }
        }

        private void OnGUI()
        {
            if (!show || clueManager == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 320, Screen.height - 20), "Debug", GUI.skin.window);
            GUILayout.Label("Clues");
            foreach (var clue in clueManager.GetAllClues())
            {
                bool unlocked = clueManager.IsUnlocked(clue.id);
                GUILayout.BeginHorizontal();
                GUILayout.Label((unlocked ? "[x] " : "[ ] ") + clue.id, GUILayout.Width(60));
                GUILayout.Label(clue.title, GUILayout.Width(180));
                if (!unlocked && clueManager.IsEligible(clue.id))
                {
                    if (GUILayout.Button("Unlock", GUILayout.Width(60)))
                    {
                        clueManager.TryUnlockClue(clue.id, "debug");
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (deductionController != null)
            {
                if (GUILayout.Button("Open Deduction"))
                {
                    deductionController.Show();
                }
            }

            GUILayout.EndArea();
        }
#endif
    }
}
