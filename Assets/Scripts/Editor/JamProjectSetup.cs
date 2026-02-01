using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TheTear.AR;
using TheTear.Characters;
using TheTear.Core;
using TheTear.Debugging;
using TheTear.Factory;
using TheTear.Interaction;
using TheTear.Story;
using TheTear.Telemetry;
using TheTear.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace TheTear.Editor
{
    public static class JamProjectSetup
    {
        [MenuItem("Jam/Generate Scene")]
        public static void GenerateScene()
        {
            DisableInteractionSimulatorAutoload();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            bool useTmp = TmpEssentialsAvailable();
            PrefabLibrary prefabLibrary = GetOrCreatePrefabLibrary();

            // AR foundation objects
            GameObject arSessionGo = new GameObject("ARSession", typeof(ARSession));
            GameObject arManagersGo = new GameObject("ARManagers", typeof(ARRaycastManager), typeof(ARPlaneManager));
            GameObject arCameraGo = new GameObject("ARCamera", typeof(Camera), typeof(AudioListener), typeof(ARCameraManager), typeof(ARCameraBackground));
            arCameraGo.tag = "MainCamera";

            GameObject xrOriginGo = TryCreateXROrigin(out GameObject cameraOffset);
            if (xrOriginGo != null && cameraOffset != null)
            {
                arCameraGo.transform.SetParent(cameraOffset.transform, false);
                arManagersGo.transform.SetParent(xrOriginGo.transform, false);
                ConfigureXROriginCamera(xrOriginGo, arCameraGo, cameraOffset);
            }

            TryAddComponent(arCameraGo, "UnityEngine.XR.ARFoundation.ARPoseDriver, Unity.XR.ARFoundation");

            // Scene root
            GameObject sceneRootGo = new GameObject("SceneRoot", typeof(SceneRootController));
            if (xrOriginGo != null)
            {
                sceneRootGo.transform.SetParent(xrOriginGo.transform, false);
            }
            sceneRootGo.SetActive(false);
            var sceneRoot = sceneRootGo.GetComponent<SceneRootController>();

            // Game manager and core components
            GameObject gmGo = new GameObject("GameManager");
            var gameManager = gmGo.AddComponent<GameManager>();
            var placement = gmGo.AddComponent<ARPlacementController>();
            var character = gmGo.AddComponent<TheTear.Characters.CharacterModeController>();
            var clueManager = gmGo.AddComponent<ClueManager>();
            var telemetry = gmGo.AddComponent<TelemetryRecorder>();
            var debugOverlay = gmGo.AddComponent<DebugOverlay>();

            var tapRaycaster = arCameraGo.AddComponent<TapRaycaster>();

            GameObject reticleGo = CreatePlacementReticle();
            if (reticleGo != null && xrOriginGo != null)
            {
                reticleGo.transform.SetParent(xrOriginGo.transform, false);
            }

            // UI
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var inputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            var legacy = eventSystemGo.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                UnityEngine.Object.DestroyImmediate(legacy);
            }
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif

            GameObject overlaysGo = CreateUIRoot("VisualOverlays", canvasGo.transform);
            GameObject hudGo = CreateUIRoot("HUD", canvasGo.transform);
            GameObject modalsGo = CreateUIRoot("Modals", canvasGo.transform);

            // Overlays
            var matterTint = CreateFullscreenImage("MatterTint", overlaysGo.transform, new Color(0.2f, 0.25f, 0.3f, 1f));
            var voidTint = CreateFullscreenImage("VoidTint", overlaysGo.transform, new Color(0.1f, 0.35f, 0.5f, 1f));
            var flowTint = CreateFullscreenImage("FlowTint", overlaysGo.transform, new Color(0.7f, 0.4f, 0.1f, 1f));
            var fadeFlash = CreateFullscreenImage("FadeFlash", overlaysGo.transform, new Color(0.95f, 0.95f, 0.95f, 1f));

            CanvasGroup matterGroup = matterTint.gameObject.AddComponent<CanvasGroup>();
            CanvasGroup voidGroup = voidTint.gameObject.AddComponent<CanvasGroup>();
            CanvasGroup flowGroup = flowTint.gameObject.AddComponent<CanvasGroup>();
            CanvasGroup flashGroup = fadeFlash.gameObject.AddComponent<CanvasGroup>();

            var overlayController = overlaysGo.AddComponent<OverlayController>();
            overlayController.matterGroup = matterGroup;
            overlayController.voidGroup = voidGroup;
            overlayController.flowGroup = flowGroup;
            overlayController.flashGroup = flashGroup;

            // HUD elements
            var hudController = hudGo.AddComponent<HUDController>();

            Button matterButton = CreateButton(hudGo.transform, "MatterButton", "MATTER", useTmp, new Vector2(180, 60), new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 90), out Component unusedMatterLabel);
            Button voidButton = CreateButton(hudGo.transform, "VoidButton", "VOID", useTmp, new Vector2(180, 60), new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 160), out Component unusedVoidLabel);
            Button flowButton = CreateButton(hudGo.transform, "FlowButton", "FLOW", useTmp, new Vector2(180, 60), new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 230), out Component unusedFlowLabel);

            Button journalButton = CreateButton(hudGo.transform, "JournalButton", "Journal", useTmp, new Vector2(180, 60), new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -90), out Component unusedJournalLabel);
            Button deductionButton = CreateButton(hudGo.transform, "DeductionButton", "Deduction", useTmp, new Vector2(180, 60), new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -160), out Component unusedDeductionLabel);
            Button relocateButton = CreateButton(hudGo.transform, "RelocateButton", "Relocate", useTmp, new Vector2(180, 60), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, -90), out Component unusedRelocateLabel);
            Button pauseButton = CreateButton(hudGo.transform, "PauseButton", "Pause", useTmp, new Vector2(180, 60), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, -160), out Component unusedPauseLabel);

            Component objectiveText = CreateText(hudGo.transform, "ObjectiveText", "Objective", useTmp, 26, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(800, 80), new Vector2(0, -30));

            GameObject trackingBanner = CreatePanel(hudGo.transform, "TrackingBanner", new Color(0.8f, 0.2f, 0.2f, 0.85f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(800, 50), new Vector2(0, -90));
            Component trackingText = CreateText(trackingBanner.transform, "TrackingText", "Tracking limited", useTmp, 22, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(780, 40), Vector2.zero);
            trackingBanner.SetActive(false);

            GameObject toastGo = new GameObject("ToastText", typeof(RectTransform), typeof(CanvasGroup));
            toastGo.transform.SetParent(hudGo.transform, false);
            var toastRect = toastGo.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0);
            toastRect.anchorMax = new Vector2(0.5f, 0);
            toastRect.sizeDelta = new Vector2(900, 80);
            toastRect.anchoredPosition = new Vector2(0, 80);
            Component toastText = CreateText(toastGo.transform, "ToastLabel", "", useTmp, 24, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 80), Vector2.zero);
            var toastController = toastGo.AddComponent<ToastController>();
            toastController.textComponent = toastText;
            toastController.canvasGroup = toastGo.GetComponent<CanvasGroup>();
            toastGo.SetActive(false);

            // Journal panel
            GameObject journalPanel = CreatePanel(hudGo.transform, "JournalPanel", new Color(0.1f, 0.1f, 0.1f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 1200), Vector2.zero);
            Component journalTitle = CreateText(journalPanel.transform, "JournalTitle", "Journal", useTmp, 30, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(860, 60), new Vector2(0, -30));
            Component clusterText = CreateText(journalPanel.transform, "ClusterText", "", useTmp, 22, TextAnchor.UpperLeft, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(860, 120), new Vector2(30, -100));
            Component clueListText = CreateText(journalPanel.transform, "ClueListText", "", useTmp, 20, TextAnchor.UpperLeft, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(860, 780), new Vector2(30, -220));

            Button journalUnlock = CreateButton(journalPanel.transform, "JournalUnlockButton", "Unlock Eligible", useTmp, new Vector2(220, 60), new Vector2(0, 0), new Vector2(0, 0), new Vector2(140, 60), out Component unusedJournalUnlockLabel);
            Button journalDeduction = CreateButton(journalPanel.transform, "JournalDeductionButton", "Open Deduction", useTmp, new Vector2(220, 60), new Vector2(0, 0), new Vector2(0, 0), new Vector2(380, 60), out Component unusedJournalDeductionLabel);
            Button journalClose = CreateButton(journalPanel.transform, "JournalCloseButton", "Close", useTmp, new Vector2(200, 60), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-140, 60), out Component unusedJournalCloseLabel);

            journalPanel.SetActive(false);
            var journalController = journalPanel.AddComponent<JournalController>();
            journalController.panelRoot = journalPanel;
            journalController.titleText = journalTitle;
            journalController.clusterText = clusterText;
            journalController.clueListText = clueListText;
            journalController.closeButton = journalClose;
            journalController.unlockButton = journalUnlock;
            journalController.deductionButton = journalDeduction;
            journalController.toast = toastController;

            // Pause modal
            GameObject pauseModal = CreateModal(modalsGo.transform, "PauseModal", new Color(0, 0, 0, 0.65f));
            GameObject pausePanel = CreatePanel(pauseModal.transform, "PausePanel", new Color(0.15f, 0.15f, 0.15f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 1200), Vector2.zero);
            Component pauseTitle = CreateText(pausePanel.transform, "PauseTitle", "Paused", useTmp, 30, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(860, 60), new Vector2(0, -30));
            Component charDesc = CreateText(pausePanel.transform, "CharacterDesc", "", useTmp, 20, TextAnchor.UpperLeft, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(860, 380), new Vector2(30, -100));
            Component pauseClues = CreateText(pausePanel.transform, "PauseClues", "", useTmp, 20, TextAnchor.UpperLeft, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(860, 420), new Vector2(30, -500));

            Button resumeButton = CreateButton(pausePanel.transform, "ResumeButton", "Resume", useTmp, new Vector2(200, 60), new Vector2(0, 0), new Vector2(0, 0), new Vector2(140, 60), out Component unusedResumeLabel);
            Button quitButton = CreateButton(pausePanel.transform, "QuitButton", "Quit", useTmp, new Vector2(200, 60), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-140, 60), out Component unusedQuitLabel);

            pauseModal.SetActive(false);
            var pauseController = pauseModal.AddComponent<PauseMenuController>();
            pauseController.panelRoot = pauseModal;
            pauseController.resumeButton = resumeButton;
            pauseController.quitButton = quitButton;
            pauseController.characterText = charDesc;
            pauseController.clueListText = pauseClues;

            // Deduction modal
            GameObject deductionModal = CreateModal(modalsGo.transform, "DeductionModal", new Color(0, 0, 0, 0.65f));
            GameObject deductionPanel = CreatePanel(deductionModal.transform, "DeductionPanel", new Color(0.15f, 0.15f, 0.15f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 900), Vector2.zero);
            Component deductionTitle = CreateText(deductionPanel.transform, "DeductionTitle", "Deduction Board", useTmp, 30, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(860, 60), new Vector2(0, -30));

            Button culpritButton = CreateButton(deductionPanel.transform, "CulpritButton", "Culprit", useTmp, new Vector2(700, 60), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), out Component culpritLabel);
            Button methodButton = CreateButton(deductionPanel.transform, "MethodButton", "Method", useTmp, new Vector2(700, 60), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -230), out Component methodLabel);
            Button motiveButton = CreateButton(deductionPanel.transform, "MotiveButton", "Motive", useTmp, new Vector2(700, 60), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -320), out Component motiveLabel);

            Button submitButton = CreateButton(deductionPanel.transform, "SubmitDeduction", "Submit", useTmp, new Vector2(200, 60), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-120, 80), out Component unusedSubmitLabel);
            Button closeDeduction = CreateButton(deductionPanel.transform, "CloseDeduction", "Close", useTmp, new Vector2(200, 60), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(120, 80), out Component unusedCloseLabel);
            Component resultText = CreateText(deductionPanel.transform, "ResultText", "", useTmp, 24, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800, 80), new Vector2(0, -30));

            deductionModal.SetActive(false);
            var deductionController = deductionModal.AddComponent<DeductionController>();
            deductionController.panelRoot = deductionModal;
            deductionController.culpritButton = culpritButton;
            deductionController.culpritText = culpritLabel;
            deductionController.methodButton = methodButton;
            deductionController.methodText = methodLabel;
            deductionController.motiveButton = motiveButton;
            deductionController.motiveText = motiveLabel;
            deductionController.submitButton = submitButton;
            deductionController.closeButton = closeDeduction;
            deductionController.resultText = resultText;

            // Error modal
            GameObject errorModal = CreateModal(modalsGo.transform, "ErrorModal", new Color(0, 0, 0, 0.75f));
            GameObject errorPanel = CreatePanel(errorModal.transform, "ErrorPanel", new Color(0.2f, 0.1f, 0.1f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 600), Vector2.zero);
            Component errorText = CreateText(errorPanel.transform, "ErrorText", "", useTmp, 22, TextAnchor.UpperLeft, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(860, 420), new Vector2(30, -60));
            Button errorClose = CreateButton(errorPanel.transform, "ErrorClose", "Close", useTmp, new Vector2(200, 60), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), out Component unusedErrorLabel);
            errorModal.SetActive(false);
            var errorController = errorModal.AddComponent<ErrorPanelController>();
            errorController.panelRoot = errorModal;
            errorController.errorText = errorText;
            errorController.closeButton = errorClose;

            // Wire references
            placement.raycastManager = arManagersGo.GetComponent<ARRaycastManager>();
            placement.planeManager = arManagersGo.GetComponent<ARPlaneManager>();
            placement.arCamera = arCameraGo.GetComponent<Camera>();
            placement.sceneRoot = sceneRoot;
            placement.reticle = reticleGo != null ? reticleGo.transform : null;

            character.arCamera = arCameraGo.GetComponent<Camera>();
            character.overlayController = overlayController;
            character.telemetry = telemetry;
            character.errorPanel = errorController;

            tapRaycaster.arCamera = arCameraGo.GetComponent<Camera>();

            gameManager.arPlacement = placement;
            gameManager.sceneRoot = sceneRoot;
            gameManager.characterController = character;
            gameManager.clueManager = clueManager;
            gameManager.telemetry = telemetry;
            gameManager.tapRaycaster = tapRaycaster;
            gameManager.hud = hudController;
            gameManager.journal = journalController;
            gameManager.pauseMenu = pauseController;
            gameManager.deduction = deductionController;
            gameManager.toast = toastController;
            gameManager.errorPanel = errorController;
            gameManager.overlay = overlayController;

            hudController.matterButton = matterButton;
            hudController.voidButton = voidButton;
            hudController.flowButton = flowButton;
            hudController.journalButton = journalButton;
            hudController.relocateButton = relocateButton;
            hudController.pauseButton = pauseButton;
            hudController.deductionButton = deductionButton;
            hudController.objectiveText = objectiveText;
            hudController.trackingBanner = trackingBanner;
            hudController.trackingText = trackingText;

            debugOverlay.clueManager = clueManager;
            debugOverlay.deductionController = deductionController;

            clueManager.prefabLibrary = prefabLibrary;

            // Save scene
            string scenePath = "Assets/Scenes/Main.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            // Validate story
            ValidateStoryInEditor();
            CheckLayers();

            Debug.Log("Jam scene generated. Remember to import TMP Essentials (if using TMP), enable ARCore, and set layers Void/Flow.");
        }

        [MenuItem("Jam/Validate Project")]
        public static void ValidateProject()
        {
            DisableInteractionSimulatorAutoload();
            ValidateStoryInEditor();
            CheckLayers();
        }

        private static void ValidateStoryInEditor()
        {
            StoryModel story = StoryLoader.LoadStoryBlocking(out string error);
            if (story == null)
            {
                Debug.LogError("Story load failed: " + error);
                return;
            }

            var issues = StoryValidator.Validate(story);
            if (issues.Count > 0)
            {
                foreach (var issue in issues)
                {
                    Debug.LogError(issue);
                }
            }
            else
            {
                Debug.Log("Story validation passed.");
            }
        }

        private static void CheckLayers()
        {
            int voidLayer = LayerMask.NameToLayer("Void");
            int flowLayer = LayerMask.NameToLayer("Flow");
            if (voidLayer < 0 || flowLayer < 0)
            {
                Debug.LogError("Missing layers for VOID/FLOW. Add in Edit > Project Settings > Tags and Layers: set Layer 6 = \"Void\" and Layer 7 = \"Flow\".");
            }
        }

        private static void DisableInteractionSimulatorAutoload()
        {
            Type settingsType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings, Unity.XR.Interaction.Toolkit");
            if (settingsType == null)
            {
                return;
            }

            object settings = null;
            PropertyInfo instanceProp = settingsType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (instanceProp != null)
            {
                settings = instanceProp.GetValue(null, null);
            }

            if (settings == null)
            {
                MethodInfo getInstance = settingsType.GetMethod("GetInstanceOrLoadOnly", BindingFlags.Static | BindingFlags.NonPublic);
                if (getInstance != null)
                {
                    settings = getInstance.Invoke(null, null);
                }
            }

            if (settings == null)
            {
                return;
            }

            var unityObj = settings as UnityEngine.Object;
            if (unityObj == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(unityObj);
            SerializedProperty autoProp = serialized.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab");
            if (autoProp != null)
            {
                autoProp.boolValue = false;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(unityObj);
            AssetDatabase.SaveAssets();
        }

        private static GameObject TryCreateXROrigin(out GameObject cameraOffset)
        {
            cameraOffset = null;
            Type xrOriginType = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
            if (xrOriginType == null)
            {
                return null;
            }

            GameObject xrOriginGo = new GameObject("XROrigin");
            Component xrOrigin = xrOriginGo.AddComponent(xrOriginType);

            cameraOffset = new GameObject("CameraOffset");
            cameraOffset.transform.SetParent(xrOriginGo.transform, false);

            SetProperty(xrOrigin, "CameraFloorOffsetObject", cameraOffset);
            SetProperty(xrOrigin, "CameraYOffsetObject", cameraOffset);

            return xrOriginGo;
        }

        private static void ConfigureXROriginCamera(GameObject xrOriginGo, GameObject cameraGo, GameObject cameraOffset)
        {
            if (xrOriginGo == null || cameraGo == null)
            {
                return;
            }

            Component xrOrigin = xrOriginGo.GetComponent(Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils"));
            if (xrOrigin != null)
            {
                SetProperty(xrOrigin, "Camera", cameraGo.GetComponent<Camera>());
                if (cameraOffset != null)
                {
                    SetProperty(xrOrigin, "CameraFloorOffsetObject", cameraOffset);
                    SetProperty(xrOrigin, "CameraYOffsetObject", cameraOffset);
                }
            }
        }

        private static void TryAddComponent(GameObject go, string typeName)
        {
            if (go == null || string.IsNullOrEmpty(typeName))
            {
                return;
            }
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                return;
            }
            if (go.GetComponent(type) == null)
            {
                go.AddComponent(type);
            }
        }

        private static GameObject CreatePlacementReticle()
        {
            GameObject reticle = new GameObject("PlacementReticle");
            var line = reticle.AddComponent<LineRenderer>();
            line.loop = true;
            line.positionCount = 36;
            line.useWorldSpace = false;
            line.widthMultiplier = 0.01f;

            float radius = 0.15f;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = (float)i / (line.positionCount - 1) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            Material mat = CreateUnlitMaterial(new Color(0.1f, 0.8f, 0.9f, 0.9f));
            if (mat != null)
            {
                line.material = mat;
            }

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "CenterDot";
            dot.transform.SetParent(reticle.transform, false);
            dot.transform.localScale = new Vector3(0.04f, 0.01f, 0.04f);
            if (mat != null)
            {
                var renderer = dot.GetComponent<Renderer>();
                renderer.material = mat;
            }
            UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());

            reticle.SetActive(false);
            return reticle;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                return null;
            }
            Material mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private static GameObject CreateUIRoot(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            return go;
        }

        private static Image CreateFullscreenImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return go;
        }

        private static GameObject CreateModal(Transform parent, string name, Color background)
        {
            GameObject modal = new GameObject(name, typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(parent, false);
            var rect = modal.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            var image = modal.GetComponent<Image>();
            image.color = background;
            image.raycastTarget = true;
            return modal;
        }

        private static Button CreateButton(Transform parent, string name, string label, bool useTmp, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, out Component labelComponent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            Component text = CreateText(go.transform, "Label", label, useTmp, 22, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetRaycastTarget(text, false);
            labelComponent = text;
            return go.GetComponent<Button>();
        }

        private static Component CreateText(Transform parent, string name, string value, bool useTmp, int fontSize, TextAnchor anchor, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            if (useTmp)
            {
                Component tmp = TryAddTMP(go);
                if (tmp != null)
                {
                    SetProperty(tmp, "text", value);
                    SetProperty(tmp, "fontSize", fontSize);
                    SetProperty(tmp, "color", color);
                    SetTmpAlignment(tmp, anchor);
                    SetRaycastTarget(tmp, false);
                    return tmp;
                }
            }

            var text = go.AddComponent<Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
            return text;
        }

        

        private static Component TryAddTMP(GameObject go)
        {
            Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType == null)
            {
                return null;
            }
            return go.AddComponent(tmpType) as Component;
        }

        private static bool TmpEssentialsAvailable()
        {
            Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Type settingsType = Type.GetType("TMPro.TMP_Settings, Unity.TextMeshPro");
            if (tmpType == null || settingsType == null)
            {
                return false;
            }

            PropertyInfo instanceProp = settingsType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            object settings = instanceProp != null ? instanceProp.GetValue(null, null) : null;
            if (settings == null)
            {
                return false;
            }

            PropertyInfo fontProp = settingsType.GetProperty("defaultFontAsset", BindingFlags.Public | BindingFlags.Instance);
            object fontAsset = fontProp != null ? fontProp.GetValue(settings, null) : null;
            return fontAsset != null;
        }

        private static void SetTmpAlignment(Component tmp, TextAnchor anchor)
        {
            Type alignType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
            if (alignType == null)
            {
                return;
            }

            string alignName = anchor == TextAnchor.UpperLeft ? "TopLeft" : "Center";
            object enumVal = Enum.Parse(alignType, alignName);
            SetProperty(tmp, "alignment", enumVal);
        }

        private static void SetProperty(Component component, string propertyName, object value)
        {
            if (component == null)
            {
                return;
            }
            PropertyInfo prop = component.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                prop.SetValue(component, value, null);
            }
        }

        private static void SetRaycastTarget(Component component, bool value)
        {
            if (component == null)
            {
                return;
            }
            PropertyInfo prop = component.GetType().GetProperty("raycastTarget", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                prop.SetValue(component, value, null);
            }
        }

        private static PrefabLibrary GetOrCreatePrefabLibrary()
        {
            const string folder = "Assets/Art";
            const string assetPath = "Assets/Art/PrefabLibrary.asset";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Art");
            }

            PrefabLibrary library = AssetDatabase.LoadAssetAtPath<PrefabLibrary>(assetPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<PrefabLibrary>();
                AssetDatabase.CreateAsset(library, assetPath);
                AssetDatabase.SaveAssets();
            }

            return library;
        }
    }
}
