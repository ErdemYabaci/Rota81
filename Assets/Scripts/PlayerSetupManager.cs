using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the Player Setup screen: names, colour picks, and the Ready button.
/// Attach this to a "PlayerSetupManager" GameObject in your scene.
///
/// Inspector wiring required:
///   player1NameField    — TMP_InputField for player 1's name
///   player2NameField    — TMP_InputField for player 2's name
///   player1ColorButtons — list of colour-swatch Buttons for player 1
///   player2ColorButtons — list of colour-swatch Buttons for player 2
///   readyButton         — the Ready button (disabled until both players are set)
///   backButton          — calls back into MainMenuManager
///   mainMenuManager     — reference to your MainMenuManager
///   gameSceneName       — name of the scene to load when Ready is pressed
///
/// Each colour Button's Image component determines the colour displayed.
/// Give every Button in player1ColorButtons / player2ColorButtons an Image
/// with the colour you want — no extra data needed.
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
    [SerializeField] private string gameSceneName = "GameScene";

    // ---------------------------------------------------------------
    // Public data — read these from another script after Ready
    // ---------------------------------------------------------------
    public static string Player1Name  { get; private set; }
    public static string Player2Name  { get; private set; }
    public static Color  Player1Color { get; private set; }
    public static Color  Player2Color { get; private set; }

    // ---------------------------------------------------------------
    // Internal state
    // ---------------------------------------------------------------
    private bool player1ColorChosen;
    private bool player2ColorChosen;

    // Visual feedback: the currently selected button gets a highlight border
    private Button selectedP1Button;
    private Button selectedP2Button;

    private static readonly Color HighlightOutlineColor = Color.white;
    private const float OutlineWidth = 3f;

    // ---------------------------------------------------------------
    // Unity lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        // Wire colour buttons at runtime so the prefab stays data-driven
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
    }

    private void OnEnable()
    {
        // Reset the panel every time it becomes visible
        ResetPanel();
    }

    // ---------------------------------------------------------------
    // Colour selection
    // ---------------------------------------------------------------

    private void OnPlayer1ColorSelected(Button btn)
    {
        SetSelectedVisual(ref selectedP1Button, btn, player1ColorButtons);
        Player1Color      = btn.GetComponent<Image>().color;
        player1ColorChosen = true;
        ValidateReady();
    }

    private void OnPlayer2ColorSelected(Button btn)
    {
        SetSelectedVisual(ref selectedP2Button, btn, player2ColorButtons);
        Player2Color      = btn.GetComponent<Image>().color;
        player2ColorChosen = true;
        ValidateReady();
    }

    /// <summary>
    /// Adds a simple outline to the newly selected button and removes it
    /// from the previously selected one.  Uses the Outline component if
    /// present; otherwise adds one dynamically.
    /// </summary>
    private void SetSelectedVisual(ref Button current, Button next, List<Button> group)
    {
        // Clear old selection in the group
        foreach (Button b in group)
        {
            Outline o = b.GetComponent<Outline>();
            if (o != null) o.enabled = false;
        }

        // Highlight the new one
        Outline outline = next.GetComponent<Outline>();
        if (outline == null)
            outline = next.gameObject.AddComponent<Outline>();

        outline.effectColor    = HighlightOutlineColor;
        outline.effectDistance = new Vector2(OutlineWidth, -OutlineWidth);
        outline.enabled        = true;

        current = next;
    }

    // ---------------------------------------------------------------
    // Validation
    // ---------------------------------------------------------------

    private void ValidateReady()
    {
        bool p1Valid = !string.IsNullOrWhiteSpace(player1NameField.text);
        bool p2Valid = !string.IsNullOrWhiteSpace(player2NameField.text);
        readyButton.interactable = p1Valid && p2Valid && player1ColorChosen && player2ColorChosen;
    }

    // ---------------------------------------------------------------
    // Button handlers
    // ---------------------------------------------------------------

    private void OnReady()
    {
        // Persist the chosen names so other scenes can read them
        Player1Name = player1NameField.text.Trim();
        Player2Name = player2NameField.text.Trim();

        // Also save to PlayerPrefs if you want cross-session persistence
        PlayerPrefs.SetString("Player1Name", Player1Name);
        PlayerPrefs.SetString("Player2Name", Player2Name);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameSceneName);
    }

    private void OnBack()
    {
        if (mainMenuManager != null)
            mainMenuManager.OnPlayerSetupBack();
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private void ResetPanel()
    {
        player1NameField.text = string.Empty;
        player2NameField.text = string.Empty;
        player1ColorChosen    = false;
        player2ColorChosen    = false;

        // Clear all selection highlights
        foreach (Button b in player1ColorButtons)
        {
            Outline o = b.GetComponent<Outline>();
            if (o != null) o.enabled = false;
        }
        foreach (Button b in player2ColorButtons)
        {
            Outline o = b.GetComponent<Outline>();
            if (o != null) o.enabled = false;
        }

        selectedP1Button = null;
        selectedP2Button = null;
        readyButton.interactable = false;
    }
}
