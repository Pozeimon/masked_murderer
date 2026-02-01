using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

namespace MaskedMurderer.Game
{
    public class GameFlowController : MonoBehaviour
    {
        private CaseFile caseFile;
        private string storyText;

        private ARPlaceClueLocation placement;
        private StateMachineTracker stateMachine;
        private ClueManager clueManager;
        private ClueSpawnController spawnController;
        private ClueTapRaycaster tapRaycaster;
        private PrefabLibrary prefabLibrary;
        private Camera arCamera;

        private Canvas uiCanvas;
        private GameObject welcomePanel;
        private GameObject placementHud;
        private GameObject journalPanel;
        private Button startButton;
        private Button beginButton;
        private Button journalButton;
        private Text placementCounterText;
        private Text instructionText;
        private Text toastText;
        private JournalController journalController;
        private bool investigationStarted;
        private Coroutine toastRoutine;
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<GameFlowController>() != null)
            {
                return;
            }

            GameObject go = new GameObject("GameFlowController");
            go.AddComponent<GameFlowController>();
        }

        void Awake()
        {
            placement = FindObjectOfType<ARPlaceClueLocation>();
            stateMachine = FindObjectOfType<StateMachineTracker>();
            if (stateMachine != null && stateMachine.prefabs != null)
            {
                stateMachine.prefabs.Clear();
            }

            clueManager = FindObjectOfType<ClueManager>();
            if (clueManager == null)
            {
                clueManager = gameObject.AddComponent<ClueManager>();
            }

            spawnController = FindObjectOfType<ClueSpawnController>();
            if (spawnController == null)
            {
                spawnController = gameObject.AddComponent<ClueSpawnController>();
            }

            tapRaycaster = FindObjectOfType<ClueTapRaycaster>();
            if (tapRaycaster == null)
            {
                GameObject tapGo = new GameObject("ClueTapRaycaster");
                tapRaycaster = tapGo.AddComponent<ClueTapRaycaster>();
            }
            tapRaycaster.enabled = false;

            prefabLibrary = Resources.Load<PrefabLibrary>("CluePrefabLibrary");
        }

        private IEnumerator Start()
        {
            arCamera = ResolveCamera();
            if (tapRaycaster != null)
            {
                tapRaycaster.SetCamera(arCamera);
            }

            yield return StartCoroutine(LoadData());
            clueManager.Initialize(caseFile);

            Transform sceneRoot = ResolveSceneRoot();
            spawnController.Initialize(placement, sceneRoot, prefabLibrary, clueManager);

            BuildUI();
            DisableLegacyStartButton();

            ButtonLogic.OnUIStartButtonClicked += HandleBeginInvestigation;
            if (clueManager != null)
            {
                clueManager.OnClueUnlocked += HandleClueUnlocked;
                clueManager.OnClueUnlockBlocked += HandleClueBlocked;
            }

            SetStateWelcome();
        }

        private IEnumerator LoadData()
        {
            yield return StartCoroutine(CaseLoader.LoadCaseAsync((data, error) =>
            {
                caseFile = data;
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning(error);
                }
            }));

            yield return StartCoroutine(CaseLoader.LoadStoryAsync((text, error) =>
            {
                storyText = text;
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning(error);
                }
            }));

            if (string.IsNullOrEmpty(storyText))
            {
                storyText = "Story text missing.";
            }
        }

        void OnDestroy()
        {
            ButtonLogic.OnUIStartButtonClicked -= HandleBeginInvestigation;
            if (clueManager != null)
            {
                clueManager.OnClueUnlocked -= HandleClueUnlocked;
                clueManager.OnClueUnlockBlocked -= HandleClueBlocked;
            }
        }

        void Update()
        {
            if (placementHud != null && placementHud.activeSelf)
            {
                int placed = placement != null ? placement.ClueLocations.Count : 0;
                int needed = clueManager != null ? clueManager.TotalClueCount : 0;
                if (needed <= 0)
                {
                    needed = 1;
                }

                if (placementCounterText != null)
                {
                    placementCounterText.text = "Placed " + placed + "/" + needed;
                }

                if (instructionText != null)
                {
                    instructionText.text = placed > 0
                        ? "Tap surfaces to place boundary markers."
                        : "Tap surfaces to place at least 1 boundary marker.";
                }

                if (beginButton != null)
                {
                    beginButton.interactable = placed > 0;
                }
            }
        }

        private void SetStateWelcome()
        {
            if (welcomePanel != null)
            {
                welcomePanel.SetActive(true);
            }
            if (placementHud != null)
            {
                placementHud.SetActive(false);
            }
            if (journalButton != null)
            {
                journalButton.gameObject.SetActive(false);
            }
            if (journalPanel != null)
            {
                journalPanel.SetActive(false);
            }
        }

        private void SetStatePlacement()
        {
            if (welcomePanel != null)
            {
                welcomePanel.SetActive(false);
            }
            if (placementHud != null)
            {
                placementHud.SetActive(true);
            }
        }

        private void HandleStartClicked()
        {
            SetStatePlacement();
        }

        private void HandleBeginInvestigation()
        {
            if (investigationStarted)
            {
                return;
            }

            investigationStarted = true;
            if (placementHud != null)
            {
                placementHud.SetActive(false);
            }
            if (journalButton != null)
            {
                journalButton.gameObject.SetActive(true);
            }

            spawnController.SpawnClues(caseFile);
            if (tapRaycaster != null)
            {
                tapRaycaster.enabled = true;
            }
        }

        private void HandleBeginButtonClicked()
        {
            if (beginButton != null && !beginButton.interactable)
            {
                return;
            }

            ButtonLogic.RaiseBeginInvestigation();
        }

        private void HandleClueUnlocked(ClueDefinition clue)
        {
            if (clue == null)
            {
                return;
            }

            ShowToast("Clue unlocked: " + clue.title + "\n" + clue.GetDescription());
            if (journalController != null)
            {
                journalController.Refresh();
            }
        }

        private void HandleClueBlocked(ClueDefinition clue)
        {
            if (clue == null)
            {
                return;
            }

            ShowToast("Clue locked: " + clue.title);
        }

        private void ToggleJournal()
        {
            if (journalPanel == null)
            {
                return;
            }

            bool next = !journalPanel.activeSelf;
            journalPanel.SetActive(next);
        }

        private void ShowToast(string message)
        {
            if (toastText == null)
            {
                return;
            }

            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }

            toastRoutine = StartCoroutine(ToastRoutine(message));
        }

        private IEnumerator ToastRoutine(string message)
        {
            toastText.gameObject.SetActive(true);
            toastText.text = message;
            yield return new WaitForSeconds(2.5f);
            toastText.gameObject.SetActive(false);
            toastRoutine = null;
        }

        private void BuildUI()
        {
            uiCanvas = CreateCanvas("GameUI");
            Transform canvasRoot = uiCanvas.transform;

            welcomePanel = CreatePanel(canvasRoot, "WelcomePanel", new Color(0f, 0f, 0f, 0.85f), true);

            Text title = CreateText(welcomePanel.transform, "WelcomeTitle", "Murder at the Game Jam", 42, TextAnchor.UpperCenter, Color.white);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 80f), new Vector2(0f, -40f));

            Sprite matterMask = LoadSprite("matter-mask");
            if (matterMask != null)
            {
                Image leftMask = CreateImage(welcomePanel.transform, "MaskLeft", matterMask, Color.white);
                SetRect(leftMask.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, 120f), new Vector2(40f, -40f));
                leftMask.raycastTarget = false;
            }

            Sprite flowMask = LoadSprite("flow-mask");
            if (flowMask != null)
            {
                Image rightMask = CreateImage(welcomePanel.transform, "MaskRight", flowMask, Color.white);
                SetRect(rightMask.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(120f, 120f), new Vector2(-40f, -40f));
                rightMask.raycastTarget = false;
            }

            ScrollRect storyScroll = CreateScrollRect(welcomePanel.transform, "StoryScroll", out Text storyContent);
            SetRect(storyScroll.GetComponent<RectTransform>(), new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), Vector2.zero);
            storyContent.text = string.IsNullOrEmpty(storyText) ? "Story text missing." : storyText;

            startButton = CreateButton(welcomePanel.transform, "WelcomeStartButton", "Start", new Vector2(240f, 72f));
            SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(240f, 72f), new Vector2(0f, 60f));
            startButton.onClick.AddListener(HandleStartClicked);

            placementHud = new GameObject("PlacementHUD", typeof(RectTransform));
            placementHud.transform.SetParent(canvasRoot, false);
            SetRect(placementHud.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            instructionText = CreateText(placementHud.transform, "InstructionText", "Tap surfaces to place boundary markers.", 26, TextAnchor.UpperLeft, Color.white);
            SetRect(instructionText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(700f, 80f), new Vector2(40f, -40f));
            instructionText.raycastTarget = false;

            placementCounterText = CreateText(placementHud.transform, "PlacementCounter", "Placed 0/0", 24, TextAnchor.UpperLeft, Color.white);
            SetRect(placementCounterText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 60f), new Vector2(40f, -90f));
            placementCounterText.raycastTarget = false;

            beginButton = CreateButton(placementHud.transform, "BeginButton", "Begin Investigation", new Vector2(320f, 72f));
            SetRect(beginButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(320f, 72f), new Vector2(-200f, 60f));
            beginButton.onClick.AddListener(HandleBeginButtonClicked);
            beginButton.interactable = false;

            journalButton = CreateButton(canvasRoot, "JournalButton", "Journal", new Vector2(160f, 60f));
            SetRect(journalButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(160f, 60f), new Vector2(-120f, -40f));
            journalButton.onClick.AddListener(ToggleJournal);

            journalPanel = CreatePanel(canvasRoot, "JournalPanel", new Color(0.05f, 0.05f, 0.05f, 0.9f), true);
            SetRect(journalPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 1200f), Vector2.zero);

            Text journalTitle = CreateText(journalPanel.transform, "JournalTitle", "Journal", 34, TextAnchor.UpperCenter, Color.white);
            SetRect(journalTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(600f, 60f), new Vector2(0f, -30f));

            Sprite voidMask = LoadSprite("void-mask");
            if (voidMask != null)
            {
                Image journalMask = CreateImage(journalPanel.transform, "JournalMask", voidMask, new Color(1f, 1f, 1f, 0.6f));
                SetRect(journalMask.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(120f, 120f), new Vector2(40f, 40f));
                journalMask.raycastTarget = false;
            }

            ScrollRect journalScroll = CreateScrollRect(journalPanel.transform, "JournalScroll", out Text journalContent);
            SetRect(journalScroll.GetComponent<RectTransform>(), new Vector2(0.07f, 0.1f), new Vector2(0.93f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Button closeButton = CreateButton(journalPanel.transform, "JournalClose", "Close", new Vector2(160f, 60f));
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(160f, 60f), new Vector2(-120f, 40f));
            closeButton.onClick.AddListener(ToggleJournal);

            journalController = journalPanel.AddComponent<JournalController>();
            journalController.Initialize(caseFile, clueManager, storyText);
            journalController.AssignTextTargets(null, journalContent);

            toastText = CreateText(canvasRoot, "ToastText", "", 24, TextAnchor.MiddleCenter, Color.white);
            SetRect(toastText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 120f), new Vector2(0f, -120f));
            toastText.gameObject.SetActive(false);
            toastText.raycastTarget = false;
        }

        private void DisableLegacyStartButton()
        {
            GameObject legacy = GameObject.Find("StartButton");
            if (legacy != null && legacy.GetComponent<Canvas>() != null)
            {
                legacy.SetActive(false);
            }
        }

        private Transform ResolveSceneRoot()
        {
            XROrigin origin = FindObjectOfType<XROrigin>();
            if (origin != null)
            {
                return origin.transform;
            }
            if (placement != null)
            {
                return placement.transform;
            }
            return transform;
        }

        private Camera ResolveCamera()
        {
            if (Camera.main != null)
            {
                return Camera.main;
            }

            XROrigin origin = FindObjectOfType<XROrigin>();
            if (origin != null && origin.Camera != null)
            {
                return origin.Camera;
            }

            return null;
        }

        private Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private GameObject CreatePanel(Transform parent, string name, Color color, bool raycast)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            panel.layer = 5;
            return panel;
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            Text uiText = go.AddComponent<Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.alignment = anchor;
            uiText.color = color;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            return uiText;
        }

        private Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            return image;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText(go.transform, "Label", label, 24, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            text.raycastTarget = false;

            SetRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero);
            return button;
        }

        private ScrollRect CreateScrollRect(Transform parent, string name, out Text contentText)
        {
            GameObject scrollGo = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            scrollGo.layer = 5;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            viewport.layer = 5;
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.2f);
            viewportImage.raycastTarget = true;
            Mask mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform viewportRt = viewport.GetComponent<RectTransform>();
            SetRect(viewportRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            content.layer = 5;
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);

            contentText = content.AddComponent<Text>();
            contentText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            contentText.fontSize = 22;
            contentText.color = Color.white;
            contentText.alignment = TextAnchor.UpperLeft;
            contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentText.verticalOverflow = VerticalWrapMode.Overflow;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = contentRt;

            return scroll;
        }

        private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPos;
        }

        private Sprite LoadSprite(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            if (spriteCache.TryGetValue(fileName, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>("PrettyStuff/" + fileName);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            spriteCache[fileName] = sprite;
            return sprite;
        }
    }
}
