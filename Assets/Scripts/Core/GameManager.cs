using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheTear.AR;
using TheTear.Characters;
using TheTear.Interaction;
using TheTear.Story;
using TheTear.Telemetry;
using TheTear.UI;

namespace TheTear.Core
{
    public class GameManager : MonoBehaviour
    {
        public ARPlacementController arPlacement;
        public SceneRootController sceneRoot;
        public CharacterModeController characterController;
        public ClueManager clueManager;
        public TelemetryRecorder telemetry;
        public TapRaycaster tapRaycaster;
        public HUDController hud;
        public JournalController journal;
        public PauseMenuController pauseMenu;
        public DeductionController deduction;
        public ToastController toast;
        public ErrorPanelController errorPanel;
        public OverlayController overlay;

        private AppState state = AppState.Placement;
        private StoryModel story;
        private bool trackingOk = true;
        private Coroutine trackingRoutine;
        private bool introShown;

        private void Awake()
        {
            Time.timeScale = 1f;
        }

        private void Start()
        {
            StartCoroutine(LoadAndInit());
        }

        private IEnumerator LoadAndInit()
        {
            if (hud != null)
            {
                hud.SetObjective("Loading case...");
            }

            StoryModel loaded = null;
            string loadError = null;
            yield return StoryLoader.LoadStory((model, error) =>
            {
                loaded = model;
                loadError = error;
            });

            if (loaded == null)
            {
                ShowBlockingErrors(new List<string> { "Failed to load story: " + (loadError ?? "Unknown error") });
                yield break;
            }

            story = loaded;
            List<string> issues = StoryValidator.Validate(story);
            if (issues.Count > 0)
            {
                ShowBlockingErrors(issues);
                yield break;
            }

            if (clueManager != null)
            {
                clueManager.Initialize(story, sceneRoot);
                clueManager.OnClueUnlocked += HandleClueUnlocked;
                clueManager.OnClueUnlockBlocked += HandleClueUnlockBlocked;
                clueManager.OnClusterCompleted += HandleClusterCompleted;
                clueManager.OnDeductionAvailabilityChanged += HandleDeductionAvailability;
            }

            if (journal != null)
            {
                journal.Initialize(clueManager, deduction, toast);
            }

            if (pauseMenu != null)
            {
                pauseMenu.Initialize(clueManager);
                pauseMenu.OnPauseChanged += HandlePauseChanged;
            }

            if (deduction != null)
            {
                deduction.Initialize(story, clueManager, telemetry);
                deduction.OnDeductionShown += HandleModalShown;
                deduction.OnDeductionHidden += HandleModalHidden;
            }

            if (hud != null)
            {
                hud.OnMatterPressed += () => characterController?.SetMode(CharacterMode.Matter);
                hud.OnVoidPressed += () => characterController?.SetMode(CharacterMode.Void);
                hud.OnFlowPressed += () => characterController?.SetMode(CharacterMode.Flow);
                hud.OnJournalPressed += () => journal?.Toggle();
                hud.OnRelocatePressed += BeginRelocate;
                hud.OnPausePressed += () => pauseMenu?.Show();
                hud.OnDeductionPressed += () => deduction?.Show();
            }

            if (characterController != null)
            {
                characterController.OnModeChanged += HandleModeChanged;
            }

            if (arPlacement != null)
            {
                arPlacement.OnPlaced += HandlePlaced;
                arPlacement.OnRelocate += HandleRelocate;
                arPlacement.OnTrackingStateChanged += HandleTrackingChanged;
                arPlacement.BeginPlacement();
            }

            if (tapRaycaster != null)
            {
                tapRaycaster.enabled = false;
            }

            if (hud != null)
            {
                hud.SetObjective("Scan for a plane, then tap to place the investigation bubble.");
                if (characterController != null)
                {
                    hud.SetMode(characterController.CurrentMode);
                }
            }

            if (characterController != null)
            {
                ApplyInteractionMask(characterController.CurrentMode);
            }

            CheckLayers();
        }

        private void CheckLayers()
        {
            if (characterController == null)
            {
                return;
            }

            string message;
            bool ok = characterController.ValidateLayers(out message);
            if (hud != null)
            {
                hud.SetVoidFlowInteractable(ok);
                if (characterController != null)
                {
                    hud.SetMode(characterController.CurrentMode);
                }
            }

            if (!ok && errorPanel != null)
            {
                errorPanel.ShowErrors(new List<string> { message }, true);
            }
        }

        private void HandlePlaced()
        {
            state = AppState.Investigating;

            if (clueManager != null)
            {
                clueManager.SpawnClues();
            }

            if (!ValidateEvidence())
            {
                return;
            }

            if (tapRaycaster != null)
            {
                tapRaycaster.enabled = true;
            }

            if (hud != null)
            {
                hud.SetObjective("Investigate the evidence. Switch modes to reveal hidden clues.");
            }

            if (toast != null)
            {
                toast.Show("Anchor placed. Tap evidence to unlock clues.");
            }

            if (!introShown && story != null && !string.IsNullOrEmpty(story.introText))
            {
                introShown = true;
                if (toast != null)
                {
                    toast.Show(story.introText, 5f);
                }
            }
        }

        private void BeginRelocate()
        {
            if (arPlacement != null)
            {
                arPlacement.BeginPlacement();
            }
            if (tapRaycaster != null)
            {
                tapRaycaster.enabled = false;
            }
            if (telemetry != null)
            {
                telemetry.RecordEvent("relocate", "begin");
            }
            if (toast != null)
            {
                toast.Show("Relocate mode: tap a plane to move the investigation bubble.");
            }
        }

        private void HandleRelocate()
        {
            if (telemetry != null)
            {
                telemetry.RecordEvent("relocate", "placed");
            }
            if (tapRaycaster != null)
            {
                tapRaycaster.enabled = true;
            }
            if (toast != null)
            {
                toast.Show("Investigation bubble moved.");
            }
        }

        private void HandleClueUnlocked(ClueData clue)
        {
            if (toast != null)
            {
                string description = !string.IsNullOrEmpty(clue.description) ? clue.description : clue.summary;
                if (!string.IsNullOrEmpty(description))
                {
                    toast.Show(clue.title + " - " + description);
                }
                else
                {
                    toast.Show("Clue unlocked: " + clue.title);
                }
            }
            if (telemetry != null)
            {
                telemetry.RecordEvent("clue_unlock", clue.id);
            }
            if (journal != null)
            {
                journal.Refresh();
            }
            if (pauseMenu != null)
            {
                pauseMenu.Refresh();
            }
        }

        private void HandleClueUnlockBlocked(ClueData clue)
        {
            if (toast != null)
            {
                toast.Show("Something is missing.");
            }
        }

        private void HandleClusterCompleted(ClusterData cluster)
        {
            if (toast == null || cluster == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(cluster.completionText))
            {
                toast.Show(cluster.completionText, 4f);
            }
            else
            {
                string title = !string.IsNullOrEmpty(cluster.title) ? cluster.title : cluster.name;
                if (!string.IsNullOrEmpty(title))
                {
                    toast.Show("Cluster complete: " + title, 3f);
                }
            }
        }

        private void HandleDeductionAvailability(bool available)
        {
            if (hud != null)
            {
                hud.SetDeductionInteractable(available);
            }
            if (available && toast != null)
            {
                toast.Show("Deduction ready. Review the board when ready.");
            }
        }

        private void HandlePauseChanged(bool isPaused)
        {
            if (tapRaycaster != null)
            {
                tapRaycaster.enabled = !isPaused;
            }
        }

        private void HandleModalShown()
        {
            if (tapRaycaster != null)
            {
                tapRaycaster.enabled = false;
            }
        }

        private void HandleModalHidden()
        {
            if (state == AppState.Investigating && arPlacement != null && !arPlacement.IsPlacementActive)
            {
                if (tapRaycaster != null)
                {
                    tapRaycaster.enabled = true;
                }
            }
        }

        private void HandleTrackingChanged(bool trackingOkNow)
        {
            trackingOk = trackingOkNow;
            if (trackingRoutine != null)
            {
                StopCoroutine(trackingRoutine);
            }
            trackingRoutine = StartCoroutine(ApplyTrackingBannerDelayed(trackingOkNow));
        }

        private IEnumerator ApplyTrackingBannerDelayed(bool isTrackingOk)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            if (trackingOk != isTrackingOk)
            {
                yield break;
            }
            if (hud != null)
            {
                hud.SetTrackingBanner(!isTrackingOk);
            }
        }

        private void ShowBlockingErrors(List<string> issues)
        {
            state = AppState.Error;
            if (tapRaycaster != null)
            {
                tapRaycaster.enabled = false;
            }
            if (errorPanel != null)
            {
                errorPanel.ShowErrors(issues, false);
            }
            if (hud != null)
            {
                hud.SetObjective("Fix story data errors to proceed.");
            }
        }

        private void HandleModeChanged(CharacterMode mode)
        {
            if (hud != null)
            {
                hud.SetMode(mode);
            }

            ApplyInteractionMask(mode);
        }

        private void ApplyInteractionMask(CharacterMode mode)
        {
            if (tapRaycaster == null)
            {
                return;
            }

            int defaultLayer = LayerMask.NameToLayer("Default");
            int voidLayer = LayerMask.NameToLayer("Void");
            int flowLayer = LayerMask.NameToLayer("Flow");

            int mask = defaultLayer >= 0 ? (1 << defaultLayer) : 0;
            if (mode == CharacterMode.Void && voidLayer >= 0)
            {
                mask |= 1 << voidLayer;
            }
            if (mode == CharacterMode.Flow && flowLayer >= 0)
            {
                mask |= 1 << flowLayer;
            }

            tapRaycaster.interactionMask = mask;
        }

        private bool ValidateEvidence()
        {
            if (sceneRoot == null)
            {
                return true;
            }

            List<string> issues = new List<string>();
            Transform cameraTransform = arPlacement != null && arPlacement.arCamera != null ? arPlacement.arCamera.transform : null;

            if (cameraTransform != null && sceneRoot.transform.IsChildOf(cameraTransform))
            {
                issues.Add("SceneRoot is parented under the AR Camera. Move it under XROrigin instead.");
            }

            foreach (Transform child in sceneRoot.transform)
            {
                ValidateEvidenceRoot(child, cameraTransform, issues);
            }

            if (issues.Count == 0)
            {
                return true;
            }

            foreach (var issue in issues)
            {
                Debug.LogError(issue);
            }

            state = AppState.Error;
            if (tapRaycaster != null)
            {
                tapRaycaster.enabled = false;
            }
            if (errorPanel != null)
            {
                errorPanel.ShowErrors(issues, false);
            }
            if (hud != null)
            {
                hud.SetObjective("Evidence configuration error. Fix and restart.");
            }
            return false;
        }

        private void ValidateEvidenceRoot(Transform root, Transform cameraTransform, List<string> issues)
        {
            if (root == null)
            {
                return;
            }

            if (root.GetComponentInChildren<RectTransform>(true) != null)
            {
                issues.Add("Evidence '" + root.name + "' includes UI RectTransform components.");
            }

            if (root.GetComponentInParent<Canvas>() != null)
            {
                issues.Add("Evidence '" + root.name + "' is parented under a Canvas.");
            }

            if (cameraTransform != null && root.IsChildOf(cameraTransform))
            {
                issues.Add("Evidence '" + root.name + "' is parented under the AR Camera.");
            }

            if (root.GetComponentInChildren<Collider>(true) == null)
            {
                issues.Add("Evidence '" + root.name + "' has no Collider.");
            }

            if (root.GetComponentInChildren<Renderer>(true) == null)
            {
                issues.Add("Evidence '" + root.name + "' has no Renderer.");
            }
        }
    }
}
