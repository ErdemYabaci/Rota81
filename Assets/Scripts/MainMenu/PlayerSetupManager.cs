using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Player Setup screen: names, colour picks, route selection, and the Ready button.
///
/// Inspector wiring required:
///   player1NameField    — TMP_InputField for player 1's name
///   player2NameField    — TMP_InputField for player 2's name
///   player1ColorButtons — list of colour-swatch Buttons for player 1
///   player2ColorButtons — list of colour-swatch Buttons for player 2
///   readyButton         — the Ready button (disabled until everything is set)
///   backButton          — calls back into MainMenuManager
///   mainMenuManager     — reference to your MainMenuManager
///   routeListContainer  — empty RectTransform (Panel) where route buttons are spawned
/// </summary>
public class PlayerSetupManager : MonoBehaviour
{
    private enum SetupState
    {
        PlayerInfo,
        RouteSelection
    }

    [Header("Player Name Fields")]
    [SerializeField] private TMP_InputField player1NameField;
    [SerializeField] private TMP_InputField player2NameField;

    [Header("Colour Pickers")]
    [SerializeField] private List<Button> player1ColorButtons;
    [SerializeField] private List<Button> player2ColorButtons;

    [Header("Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button backButton;

    [Header("References")]
    [SerializeField] private MainMenuManager mainMenuManager;

    [Header("Route Selection")]
    [Tooltip("Assign an empty Panel / VerticalLayoutGroup here. Route buttons are spawned inside it at runtime.")]
    [SerializeField] private RectTransform routeListContainer;

    // ── Public data ───────────────────────────────────────────────────────────
    public static string Player1Name  { get; private set; }
    public static string Player2Name  { get; private set; }
    public static Color  Player1Color { get; private set; }
    public static Color  Player2Color { get; private set; }

    // ── Internal state ────────────────────────────────────────────────────────
    private bool   player1ColorChosen;
    private bool   player2ColorChosen;
    private string selectedRouteName;

    private Button selectedP1Button;
    private Button selectedP2Button;

    private SetupState _currentState = SetupState.PlayerInfo;
    private GameObject _playerInfoGroup;

    // Route item tracking: routeName → (background Image, border Outline)
    private readonly Dictionary<string, Image> _routeItemImages = new Dictionary<string, Image>();

    private static readonly Color HighlightOutlineColor = Color.white;
    private const float OutlineWidth = 3f;

    // Route item colours
    private static readonly Color ColRouteNormal   = new Color(0.12f, 0.14f, 0.22f, 1f);
    private static readonly Color ColRouteSelected = new Color(0.18f, 0.38f, 0.72f, 1f);

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Colour buttons
        for (int i = 0; i < player1ColorButtons.Count; i++)
        {
            Button btn = player1ColorButtons[i];
            btn.onClick.AddListener(() => OnPlayer1ColorSelected(btn));
        }
        for (int i = 0; i < player2ColorButtons.Count; i++)
        {
            Button btn = player2ColorButtons[i];
            btn.onClick.AddListener(() => OnPlayer2ColorSelected(btn));
        }

        player1NameField.onValueChanged.AddListener(_ => ValidateReady());
        player2NameField.onValueChanged.AddListener(_ => ValidateReady());

        readyButton.onClick.AddListener(OnReady);
        backButton.onClick.AddListener(OnBack);

        readyButton.interactable = false;

        BuildRouteList();
    }

    private void OnEnable()
    {
        ResetPanel();
    }

    private void GroupSetupElements()
    {
        if (_playerInfoGroup != null) return;

        // Resolve the actual Canvas parent of our UI components
        Transform canvasParent = null;
        if (routeListContainer != null)
        {
            if (routeListContainer.parent != null && routeListContainer.parent.name == "Viewport")
                canvasParent = routeListContainer.parent.parent.parent;
            else
                canvasParent = routeListContainer.parent;
        }
        else if (player1NameField != null && player1NameField.transform.parent != null)
        {
            canvasParent = player1NameField.transform.parent.parent;
        }

        if (canvasParent == null)
            canvasParent = transform; // fallback

        _playerInfoGroup = new GameObject("PlayerInfoGroup", typeof(RectTransform));
        _playerInfoGroup.transform.SetParent(canvasParent, false);

        RectTransform rt = _playerInfoGroup.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        // Reparent input fields
        if (player1NameField != null) player1NameField.transform.parent.SetParent(_playerInfoGroup.transform, true);
        if (player2NameField != null) player2NameField.transform.parent.SetParent(_playerInfoGroup.transform, true);

        // Reparent color panels
        if (player1ColorButtons.Count > 0 && player1ColorButtons[0] != null)
            player1ColorButtons[0].transform.parent.SetParent(_playerInfoGroup.transform, true);
        if (player2ColorButtons.Count > 0 && player2ColorButtons[0] != null)
            player2ColorButtons[0].transform.parent.SetParent(_playerInfoGroup.transform, true);

        // Reparent labels and background panels within the canvas setup screen
        List<Transform> childrenToMove = new List<Transform>();
        foreach (Transform child in canvasParent)
        {
            if (child == _playerInfoGroup.transform || child == routeListContainer) continue;
            if (child.gameObject == readyButton.gameObject) continue;
            if (child.gameObject == backButton.gameObject) continue;
            if (child.name == "RouteListScrollView") continue;

            childrenToMove.Add(child);
        }

        foreach (Transform child in childrenToMove)
        {
            child.SetParent(_playerInfoGroup.transform, true);
        }
    }

    private void SetState(SetupState newState)
    {
        GroupSetupElements();
        _currentState = newState;

        Transform canvasParent = _playerInfoGroup != null ? _playerInfoGroup.transform.parent : transform;
        var scrollView = canvasParent.Find("RouteListScrollView")?.gameObject;

        if (_currentState == SetupState.PlayerInfo)
        {
            _playerInfoGroup.SetActive(true);
            if (scrollView != null) scrollView.SetActive(false);

            TMP_Text btnText = readyButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Hazır";

            ValidateReady();
        }
        else if (_currentState == SetupState.RouteSelection)
        {
            _playerInfoGroup.SetActive(false);
            if (scrollView != null) scrollView.SetActive(true);

            TMP_Text btnText = readyButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Başlat";

            // Force dynamic layout rebuilds to prevent ScrollView bounds glitches
            if (routeListContainer != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(routeListContainer);
                var scrollRect = scrollView?.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
                    if (scrollRect.viewport != null)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.viewport);
                }
            }

            ValidateReady();
        }
    }

    // ── Route list ────────────────────────────────────────────────────────────

    private void BuildRouteList()
    {
        if (routeListContainer == null)
        {
            Debug.LogWarning("PlayerSetupManager: routeListContainer not assigned — route list will not appear.");
            return;
        }

        SetupScrollRectProgrammatically();

        // Clear any existing children (e.g. on panel re-enable)
        foreach (Transform child in routeListContainer)
            Destroy(child.gameObject);

        _routeItemImages.Clear();

        // Dynamically ensure scrollability/fitter components on the container
        ContentSizeFitter fitter = routeListContainer.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = routeListContainer.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = routeListContainer.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
        }

        // Spawn special "Rastgele (Random)" button
        SpawnRouteItem("Rastgele (Random)", new string[] { "Sistem sizin için rastgele bir rota seçecektir." });

        foreach (var kvp in RouteDatabase.Routes)
            SpawnRouteItem(kvp.Key, kvp.Value);

        // Force dynamic layout rebuild so sizes register immediately
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(routeListContainer);
    }

    private void SpawnRouteItem(string routeName, string[] stops)
    {
        // ── Root: button + background ─────────────────────────────────────
        GameObject root = new GameObject(routeName + "_RouteItem");
        root.transform.SetParent(routeListContainer, false);

        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(0f, 90f);   // width driven by layout

        // Dynamic height constraints to fix scrolling bounce-back issue
        LayoutElement le = root.AddComponent<LayoutElement>();
        le.preferredHeight = 90f;
        le.minHeight = 90f;

        Image bg = root.AddComponent<Image>();
        bg.color = ColRouteNormal;

        Button btn = root.AddComponent<Button>();

        // Rounded feel via ColorBlock
        ColorBlock cb = btn.colors;
        cb.normalColor      = ColRouteNormal;
        cb.highlightedColor = new Color(0.2f, 0.3f, 0.55f, 1f);
        cb.pressedColor     = new Color(0.12f, 0.22f, 0.5f, 1f);
        cb.selectedColor    = ColRouteSelected;
        btn.colors          = cb;

        // ── Route name label ──────────────────────────────────────────────
        GameObject nameGO = new GameObject("RouteName");
        nameGO.transform.SetParent(root.transform, false);

        TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text      = routeName;
        nameTMP.fontSize  = 20;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color     = Color.white;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.5f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.offsetMin = new Vector2(14f, 0f);
        nameRT.offsetMax = new Vector2(-14f, -4f);

        // ── Stops label ───────────────────────────────────────────────────
        GameObject stopsGO = new GameObject("StopsLabel");
        stopsGO.transform.SetParent(root.transform, false);

        TextMeshProUGUI stopsTMP = stopsGO.AddComponent<TextMeshProUGUI>();
        stopsTMP.text      = string.Join("  →  ", stops);
        stopsTMP.fontSize  = 11;
        stopsTMP.color     = new Color(0.75f, 0.82f, 1f, 1f);
        stopsTMP.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform stopsRT = stopsGO.GetComponent<RectTransform>();
        stopsRT.anchorMin = new Vector2(0f, 0f);
        stopsRT.anchorMax = new Vector2(1f, 0.5f);
        stopsRT.offsetMin = new Vector2(14f, 4f);
        stopsRT.offsetMax = new Vector2(-14f, 0f);

        // ── Selection outline ─────────────────────────────────────────────
        Outline outline = root.AddComponent<Outline>();
        outline.effectColor    = new Color(0.4f, 0.7f, 1f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.enabled        = false;

        _routeItemImages[routeName] = bg;

        // ── Click handler ─────────────────────────────────────────────────
        string capturedName = routeName;
        Outline capturedOutline = outline;
        btn.onClick.AddListener(() => OnRouteSelected(capturedName, bg, capturedOutline));
    }

    private void OnRouteSelected(string routeName, Image bg, Outline outline)
    {
        // Deselect all
        foreach (var kvp in _routeItemImages)
        {
            kvp.Value.color = ColRouteNormal;
            Outline o = kvp.Value.GetComponent<Outline>();
            if (o != null) o.enabled = false;
        }
        // Select this one
        bg.color      = ColRouteSelected;
        outline.enabled = true;

        selectedRouteName = routeName;
        ValidateReady();
    }

    // ── Colour selection ──────────────────────────────────────────────────────

    private void OnPlayer1ColorSelected(Button btn)
    {
        SetSelectedVisual(ref selectedP1Button, btn, player1ColorButtons);
        Player1Color       = btn.GetComponent<Image>().color;
        player1ColorChosen = true;
        ValidateReady();
    }

    private void OnPlayer2ColorSelected(Button btn)
    {
        SetSelectedVisual(ref selectedP2Button, btn, player2ColorButtons);
        Player2Color       = btn.GetComponent<Image>().color;
        player2ColorChosen = true;
        ValidateReady();
    }

    private void SetSelectedVisual(ref Button current, Button next, List<Button> group)
    {
        foreach (Button b in group)
        {
            Outline o = b.GetComponent<Outline>();
            if (o != null) o.enabled = false;
        }

        Outline outline = next.GetComponent<Outline>();
        if (outline == null)
            outline = next.gameObject.AddComponent<Outline>();

        outline.effectColor    = HighlightOutlineColor;
        outline.effectDistance = new Vector2(OutlineWidth, -OutlineWidth);
        outline.enabled        = true;

        current = next;
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private void ValidateReady()
    {
        bool p1Valid = !string.IsNullOrWhiteSpace(player1NameField.text);
        bool p2Valid = !string.IsNullOrWhiteSpace(player2NameField.text);
        bool routeValid = !string.IsNullOrWhiteSpace(selectedRouteName);

        if (_currentState == SetupState.PlayerInfo)
        {
            readyButton.interactable = p1Valid && p2Valid
                                     && player1ColorChosen && player2ColorChosen;
        }
        else if (_currentState == SetupState.RouteSelection)
        {
            readyButton.interactable = routeValid;
        }
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void OnReady()
    {
        if (_currentState == SetupState.PlayerInfo)
        {
            SetState(SetupState.RouteSelection);
        }
        else if (_currentState == SetupState.RouteSelection)
        {
            Player1Name = player1NameField.text.Trim();
            Player2Name = player2NameField.text.Trim();

            PlayerPrefs.SetString("Player1Name", Player1Name);
            PlayerPrefs.SetString("Player2Name", Player2Name);
            PlayerPrefs.Save();

            string actualRoute = selectedRouteName;
            if (actualRoute == "Rastgele (Random)")
            {
                List<string> keys = new List<string>(RouteDatabase.Routes.Keys);
                actualRoute = keys[UnityEngine.Random.Range(0, keys.Count)];
            }

            // Initialise cross-scene game state
            GameState.StartNewGame(actualRoute, RouteDatabase.Routes[actualRoute]);

            // Fade to MapScene
            SceneFader.EnsureExists();
            SceneFader.Instance.FadeOutAndLoad("MapScene");
        }
    }

    private void OnBack()
    {
        if (_currentState == SetupState.RouteSelection)
        {
            SetState(SetupState.PlayerInfo);
        }
        else
        {
            if (mainMenuManager != null)
                mainMenuManager.OnBack();
        }
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    private void ResetPanel()
    {
        player1NameField.text = string.Empty;
        player2NameField.text = string.Empty;
        player1ColorChosen    = false;
        player2ColorChosen    = false;
        selectedRouteName     = null;

        foreach (Button b in player1ColorButtons)
        { Outline o = b.GetComponent<Outline>(); if (o != null) o.enabled = false; }
        foreach (Button b in player2ColorButtons)
        { Outline o = b.GetComponent<Outline>(); if (o != null) o.enabled = false; }

        foreach (var kvp in _routeItemImages)
        {
            kvp.Value.color = ColRouteNormal;
            Outline o = kvp.Value.GetComponent<Outline>();
            if (o != null) o.enabled = false;
        }

        selectedP1Button = null;
        selectedP2Button = null;

        // Put us in the starting state first to resolve canvas parent while unwrapped
        SetState(SetupState.PlayerInfo);

        // Rebuild route list so outlines are fresh
        BuildRouteList();
    }

    private void SetupScrollRectProgrammatically()
    {
        if (routeListContainer == null) return;

        // If already wrapped in a Viewport, keep references but still enforce bounds.
        if (routeListContainer.parent != null && routeListContainer.parent.name == "Viewport")
        {
            Transform scrollView = routeListContainer.parent.parent;
            ScrollRect existing = scrollView != null ? scrollView.GetComponent<ScrollRect>() : null;
            if (existing != null)
            {
                ConfigureRouteScrollRect(existing, scrollView.GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(existing.viewport);
                LayoutRebuilder.ForceRebuildLayoutImmediate(routeListContainer);
            }
            return;
        }

        // 1. Store original properties of container
        Transform originalParent = routeListContainer.parent;
        int originalSiblingIndex = routeListContainer.GetSiblingIndex();
        Vector2 originalAnchoredPos = routeListContainer.anchoredPosition;
        Vector2 originalSizeDelta = routeListContainer.sizeDelta;
        Vector2 originalAnchorMin = routeListContainer.anchorMin;
        Vector2 originalAnchorMax = routeListContainer.anchorMax;
        Vector2 originalPivot = routeListContainer.pivot;

        // 2. Create ScrollView GameObject
        GameObject scrollViewGO = new GameObject("RouteListScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollViewGO.transform.SetParent(originalParent, false);
        scrollViewGO.transform.SetSiblingIndex(originalSiblingIndex);

        RectTransform scrollRT = scrollViewGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = originalAnchorMin;
        scrollRT.anchorMax = originalAnchorMax;
        scrollRT.anchoredPosition = originalAnchoredPos;
        scrollRT.sizeDelta = originalSizeDelta;
        scrollRT.pivot = originalPivot;

        // 3. Create Viewport GameObject
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportGO.transform.SetParent(scrollViewGO.transform, false);

        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;
        viewportRT.anchoredPosition = Vector2.zero;
        viewportRT.pivot = new Vector2(0f, 1f);

        // Make viewport image transparent so it acts as mask only
        Image viewportImg = viewportGO.GetComponent<Image>();
        viewportImg.color = new Color(0f, 0f, 0f, 0f);

        // 4. Reparent the container to Viewport
        routeListContainer.SetParent(viewportRT, false);

        // 5. Configure container layout (must stretch horizontally, align top vertically)
        routeListContainer.anchorMin = new Vector2(0f, 1f);
        routeListContainer.anchorMax = new Vector2(1f, 1f);
        routeListContainer.pivot = new Vector2(0.5f, 1f);
        routeListContainer.anchoredPosition = Vector2.zero;

        // 6. Connect ScrollRect references
        ScrollRect scrollRect = scrollViewGO.GetComponent<ScrollRect>();
        scrollRect.content = routeListContainer;
        scrollRect.viewport = viewportRT;
        ConfigureRouteScrollRect(scrollRect, scrollRT);

        // Rebuild layout values immediately
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRT);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRT);
    }

    private void ConfigureRouteScrollRect(ScrollRect scrollRect, RectTransform scrollRT)
    {
        if (scrollRect == null || scrollRT == null) return;

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 18f;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Keep route list above the bottom buttons so Baslat remains clickable.
        scrollRT.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRT.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRT.pivot = new Vector2(0.5f, 0.5f);
        scrollRT.anchoredPosition = new Vector2(0f, -20f);
        scrollRT.sizeDelta = new Vector2(1120f, 420f);
    }
}
