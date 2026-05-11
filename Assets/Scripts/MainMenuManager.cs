using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the main menu panels: Main Menu, How to Play, and Settings.
/// Attach this to a "MainMenuManager" GameObject in your scene.
///
/// Inspector wiring required:
///   mainMenuPanel       — the root panel with the 4 buttons
///   howToPlayPanel      — the How to Play panel
///   settingsPanel       — the Settings panel
///   playerSetupPanel    — the Player Setup panel (lives on PlayerSetupManager)
///   volumeSlider        — Settings volume slider
///   sfxSlider           — Settings SFX slider
///   fullscreenToggle    — Settings fullscreen toggle
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject playerSetupPanel;   // owned by PlayerSetupManager

    [Header("Settings Controls")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle fullscreenToggle;

    // ---------------------------------------------------------------
    // Unity lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        // Safety: make sure only the main menu is visible on start
        ShowPanel(mainMenuPanel);
    }

    private void Start()
    {
        // Load saved settings so controls reflect stored values
        if (volumeSlider != null)
            volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);

        if (sfxSlider != null)
            sfxSlider.value = PlayerPrefs.GetFloat("SFX", 1f);

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
    }

    // ---------------------------------------------------------------
    // Main Menu buttons  (wire these in the Inspector's OnClick events)
    // ---------------------------------------------------------------

    public void OnStartGame()
    {
        ShowPanel(playerSetupPanel);
    }

    public void OnHowToPlay()
    {
        ShowPanel(howToPlayPanel);
    }

    public void OnSettings()
    {
        ShowPanel(settingsPanel);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------
    // Back buttons  (wire each panel's Back button to these)
    // ---------------------------------------------------------------

    public void OnHowToPlayBack()
    {
        ShowPanel(mainMenuPanel);
    }

    public void OnSettingsBack()
    {
        SaveSettings();
        ShowPanel(mainMenuPanel);
    }

    // Called by PlayerSetupManager when its Back button is pressed
    public void OnPlayerSetupBack()
    {
        ShowPanel(mainMenuPanel);
    }

    // ---------------------------------------------------------------
    // Settings persistence  (called automatically by Slider/Toggle
    //   OnValueChanged events, or on Back)
    // ---------------------------------------------------------------

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
    }

    public void OnSfxChanged(float value)
    {
        // Hook your SFX mixer here; storing the value for now
        PlayerPrefs.SetFloat("SFX", value);
    }

    public void OnFullscreenChanged(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Deactivates every managed panel then activates the requested one.
    /// </summary>
    private void ShowPanel(GameObject targetPanel)
    {
        mainMenuPanel.SetActive(false);
        howToPlayPanel.SetActive(false);
        settingsPanel.SetActive(false);
        playerSetupPanel.SetActive(false);

        if (targetPanel != null)
            targetPanel.SetActive(true);
    }
}
