using System;
using UnityEngine;
using UnityEngine.UI;
using TheTear.Story;
using TheTear.Telemetry;

namespace TheTear.UI
{
    public class DeductionController : MonoBehaviour
    {
        public GameObject panelRoot;
        public Button culpritButton;
        public Component culpritText;
        public Button methodButton;
        public Component methodText;
        public Button motiveButton;
        public Component motiveText;
        public Button submitButton;
        public Button closeButton;
        public Component resultText;

        public event Action OnDeductionShown;
        public event Action OnDeductionHidden;

        private StoryModel story;
        private ClueManager clueManager;
        private TelemetryRecorder telemetry;
        private int culpritIndex;
        private int methodIndex;
        private int motiveIndex;

        private void Awake()
        {
            if (submitButton != null) submitButton.onClick.AddListener(Submit);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (culpritButton != null) culpritButton.onClick.AddListener(CycleCulprit);
            if (methodButton != null) methodButton.onClick.AddListener(CycleMethod);
            if (motiveButton != null) motiveButton.onClick.AddListener(CycleMotive);
        }

        public void Initialize(StoryModel storyModel, ClueManager manager, TelemetryRecorder recorder)
        {
            story = storyModel;
            clueManager = manager;
            telemetry = recorder;
            ResetSelections();
            Hide();
        }

        public void Show()
        {
            if (panelRoot == null)
            {
                return;
            }
            panelRoot.SetActive(true);
            ResetSelections();
            UpdateInteractable();
            UITextHelper.SetText(resultText, string.Empty);
            OnDeductionShown?.Invoke();
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            OnDeductionHidden?.Invoke();
        }

        private void UpdateInteractable()
        {
            if (submitButton != null && clueManager != null)
            {
                submitButton.interactable = clueManager.IsDeductionAvailable;
            }
        }

        private void ResetSelections()
        {
            culpritIndex = 0;
            methodIndex = 0;
            motiveIndex = 0;
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            UITextHelper.SetText(culpritText, GetOption(story != null ? story.culprits : null, culpritIndex));
            UITextHelper.SetText(methodText, GetOption(story != null ? story.methods : null, methodIndex));
            UITextHelper.SetText(motiveText, GetOption(story != null ? story.motives : null, motiveIndex));
        }

        private void CycleCulprit()
        {
            culpritIndex = NextIndex(story != null ? story.culprits : null, culpritIndex);
            UpdateLabels();
        }

        private void CycleMethod()
        {
            methodIndex = NextIndex(story != null ? story.methods : null, methodIndex);
            UpdateLabels();
        }

        private void CycleMotive()
        {
            motiveIndex = NextIndex(story != null ? story.motives : null, motiveIndex);
            UpdateLabels();
        }

        private int NextIndex(string[] options, int current)
        {
            if (options == null || options.Length == 0)
            {
                return 0;
            }
            return (current + 1) % options.Length;
        }

        private string GetOption(string[] options, int index)
        {
            if (options == null || options.Length == 0)
            {
                return string.Empty;
            }
            int safe = Mathf.Clamp(index, 0, options.Length - 1);
            return options[safe];
        }

        private void Submit()
        {
            if (story == null || story.solution == null)
            {
                return;
            }

            string culprit = GetOption(story.culprits, culpritIndex);
            string method = GetOption(story.methods, methodIndex);
            string motive = GetOption(story.motives, motiveIndex);

            bool success = culprit == story.solution.culprit && method == story.solution.method && motive == story.solution.motive;
            UITextHelper.SetText(resultText, success ? "Correct! Case solved." : "Incorrect. Re-evaluate the evidence.");

            if (telemetry != null)
            {
                telemetry.RecordEvent("deduction", success ? "success" : "fail");
            }
        }
    }
}
