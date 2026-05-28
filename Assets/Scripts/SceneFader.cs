using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// DontDestroyOnLoad singleton that fades the screen to black before loading
/// a scene and fades back to clear when the new scene finishes loading.
///
/// Usage:  SceneFader.EnsureExists();
///         SceneFader.Instance.FadeOutAndLoad("SceneName");
///
/// No inspector wiring required — builds its own Canvas and overlay at runtime.
/// Subscribes to SceneManager.sceneLoaded so the fade-in triggers automatically
/// on every scene transition (including the very first one).
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Tooltip("Duration of each fade in seconds.")]
    public float fadeDuration = 0.5f;

    private CanvasGroup _overlay;
    private bool        _busy;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();

        // Subscribe — fires every time any scene finishes loading
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Fade in right now (covers the initial load of whatever scene created us)
        _overlay.alpha = 1f;
        StartCoroutine(FadeIn());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Scene loaded callback ─────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset and fade in every time a new scene finishes loading
        StopAllCoroutines();
        _busy                   = false;
        _overlay.alpha          = 1f;
        _overlay.blocksRaycasts = true;
        StartCoroutine(FadeIn());
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Ensures a SceneFader exists in the scene; creates one if not.</summary>
    public static void EnsureExists()
    {
        if (Instance == null)
            new GameObject("SceneFader").AddComponent<SceneFader>();
    }

    /// <summary>Fades to black then loads the given scene by name.</summary>
    public void FadeOutAndLoad(string sceneName)
    {
        if (_busy) return;
        _busy = true;
        StartCoroutine(FadeOutRoutine(sceneName));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void BuildOverlay()
    {
        // Canvas — always on top
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // Full-screen black image
        GameObject overlayGO = new GameObject("_FadeOverlay");
        overlayGO.transform.SetParent(transform, false);

        Image img = overlayGO.AddComponent<Image>();
        img.color         = Color.black;
        img.raycastTarget = false;

        RectTransform rt = overlayGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _overlay                = overlayGO.AddComponent<CanvasGroup>();
        _overlay.alpha          = 1f;   // opaque until FadeIn clears it
        _overlay.blocksRaycasts = true;
        _overlay.interactable   = false;
    }

    private IEnumerator FadeIn()
    {
        _overlay.alpha = 1f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed        += Time.unscaledDeltaTime;
            _overlay.alpha  = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        _overlay.alpha          = 0f;
        _overlay.blocksRaycasts = false;
        _busy = false;
    }

    private IEnumerator FadeOutRoutine(string sceneName)
    {
        _overlay.blocksRaycasts = true;
        _overlay.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed        += Time.unscaledDeltaTime;
            _overlay.alpha  = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        _overlay.alpha = 1f;
        SceneManager.LoadScene(sceneName);
        // OnSceneLoaded fires next, which kicks off FadeIn for the new scene.
    }
}
