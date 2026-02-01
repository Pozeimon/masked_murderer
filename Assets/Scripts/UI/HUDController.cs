using System;
using UnityEngine;
using UnityEngine.UI;
using TheTear.Characters;

namespace TheTear.UI
{
    public class HUDController : MonoBehaviour
    {
        public Button matterButton;
        public Button voidButton;
        public Button flowButton;
        public Button journalButton;
        public Button relocateButton;
        public Button pauseButton;
        public Button deductionButton;
        public Component objectiveText;
        public GameObject trackingBanner;
        public Component trackingText;
        public Color activeButtonColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        public Color inactiveButtonColor = new Color(0.22f, 0.22f, 0.22f, 0.9f);
        public Color disabledButtonColor = new Color(0.12f, 0.12f, 0.12f, 0.45f);

        public event Action OnMatterPressed;
        public event Action OnVoidPressed;
        public event Action OnFlowPressed;
        public event Action OnJournalPressed;
        public event Action OnRelocatePressed;
        public event Action OnPausePressed;
        public event Action OnDeductionPressed;

        private bool voidFlowAvailable = true;

        private void Awake()
        {
            if (matterButton != null) matterButton.onClick.AddListener(() => OnMatterPressed?.Invoke());
            if (voidButton != null) voidButton.onClick.AddListener(() => OnVoidPressed?.Invoke());
            if (flowButton != null) flowButton.onClick.AddListener(() => OnFlowPressed?.Invoke());
            if (journalButton != null) journalButton.onClick.AddListener(() => OnJournalPressed?.Invoke());
            if (relocateButton != null) relocateButton.onClick.AddListener(() => OnRelocatePressed?.Invoke());
            if (pauseButton != null) pauseButton.onClick.AddListener(() => OnPausePressed?.Invoke());
            if (deductionButton != null) deductionButton.onClick.AddListener(() => OnDeductionPressed?.Invoke());
        }

        public void SetObjective(string text)
        {
            UITextHelper.SetText(objectiveText, text);
        }

        public void SetTrackingBanner(bool show)
        {
            if (trackingBanner != null)
            {
                trackingBanner.SetActive(show);
            }
            if (show)
            {
                UITextHelper.SetText(trackingText, "Tracking limited - move device slowly.");
            }
        }

        public void SetVoidFlowInteractable(bool layersReady)
        {
            voidFlowAvailable = layersReady;
            if (voidButton != null) voidButton.interactable = layersReady;
            if (flowButton != null) flowButton.interactable = layersReady;
            ApplyModeVisuals(null);
        }

        public void SetDeductionInteractable(bool ready)
        {
            if (deductionButton != null) deductionButton.interactable = ready;
        }

        public void SetMode(CharacterMode mode)
        {
            ApplyModeVisuals(mode);
        }

        private void ApplyModeVisuals(CharacterMode? mode)
        {
            if (mode.HasValue)
            {
                SetButtonState(matterButton, mode.Value == CharacterMode.Matter, true);
                SetButtonState(voidButton, mode.Value == CharacterMode.Void, voidFlowAvailable);
                SetButtonState(flowButton, mode.Value == CharacterMode.Flow, voidFlowAvailable);
            }
            else
            {
                SetButtonState(voidButton, false, voidFlowAvailable);
                SetButtonState(flowButton, false, voidFlowAvailable);
            }
        }

        private void SetButtonState(Button button, bool active, bool available)
        {
            if (button == null)
            {
                return;
            }

            bool interactable = available && !active;
            button.interactable = interactable;

            var image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            if (!available)
            {
                image.color = disabledButtonColor;
            }
            else
            {
                image.color = active ? activeButtonColor : inactiveButtonColor;
            }
        }
    }
}
