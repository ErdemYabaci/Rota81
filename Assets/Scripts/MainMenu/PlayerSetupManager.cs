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

    // ── Route list ────────────────────────────────────────────────────────────

    private void BuildRouteList()
    {
        if (routeListContainer == null)
        {
            Debug.LogWarning("PlayerSetupManager: routeListContainer not assigned — route list will not appear.");
            return;
        }

        // Clear any existing children (e.g. on panel re-enable)
        foreach (Transform child in routeListContainer)
            Destroy(child.gameObject);

        _routeItemImages.Clear();

        foreach (var kvp in RouteDatabase.Routes)
            SpawnRouteItem(kvp.Key, kvp.Value);
    }

    private void SpawnRouteItem(string routeName, string[] stops)
    {
        // ── Root: button + background ─────────────────────────────────────
        GameObject root = new GameObject(routeName + "_RouteItem");
        root.transform.SetParent(routeListContainer, false);

        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(0f, 90f);   // width driven by layout

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
        bool p1Valid    = !string.IsNullOrWhiteSpace(player1NameField.text);
        bool p2Valid    = !string.IsNullOrWhiteSpace(player2NameField.text);
        bool routeValid = !string.IsNullOrWhiteSpace(selectedRouteName);

        readyButton.interactable = p1Valid && p2Valid
                                 && player1ColorChosen && player2ColorChosen
                                 && routeValid;
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void OnReady()
    {
        Player1Name = player1NameField.text.Trim();
        Player2Name = player2NameField.text.Trim();

        PlayerPrefs.SetString("Player1Name", Player1Name);
        PlayerPrefs.SetString("Player2Name", Player2Name);
        PlayerPrefs.Save();

        // Initialise cross-scene game state
        GameState.StartNewGame(selectedRouteName, RouteDatabase.Routes[selectedRouteName]);

        // Fade to MapScene
        SceneFader.EnsureExists();
        SceneFader.Instance.FadeOutAndLoad("MapScene");
    }

    private void OnBack()
    {
        if (mainMenuManager != null)
            mainMenuManager.OnBack();
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
        readyButton.interactable = false;

        // Rebuild route list so outlines are fresh
        BuildRouteList();
    }
}
