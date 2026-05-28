using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Master controller for the MapScene game loop.
///
/// Inspector wiring required:
///   routeMapManager   — the RouteMapManager in the scene
///   bus1              — BusController for Player 1
///   bus2              — BusController for Player 2
///   winnerPanel       — (optional) root GameObject of the winner overlay
///   winnerLabel       — (optional) TMP_Text inside winnerPanel
///   returnMenuButton  — (optional) Button inside winnerPanel → wires itself
///
/// Everything else (LineRenderer, stop dots) is created at runtime.
/// </summary>
public class MapSceneManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Map References")]
    [SerializeField] private RouteMapManager routeMapManager;
    [SerializeField] private BusController   bus1;
    [SerializeField] private BusController   bus2;

    [Header("Winner UI (optional — assign in Inspector)")]
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private TMP_Text   winnerLabel;
    [SerializeField] private Button     returnMenuButton;

    [Header("Visuals")]
    [Tooltip("How high above the bus-stop position the route line sits.")]
    [SerializeField] private float lineHeightOffset = 0.05f;
    [Tooltip("Scale of each stop-dot sphere.")]
    [SerializeField] private float dotScale = 0.4f;
    [Tooltip("Seconds to wait after buses arrive before fading to GameScene.")]
    [SerializeField] private float arrivalPause = 0.8f;

    // ── Private state ─────────────────────────────────────────────────────────

    private LineRenderer        _line;
    private List<GameObject>    _dots      = new List<GameObject>();
    private List<GameObject>    _dotLabels = new List<GameObject>();

    // Shared material used for all dot renderers (avoids material leaks)
    private Material _dotMaterial;

    // Colours
    private static readonly Color ColLine      = new Color(1f,  0.82f, 0.18f, 0.95f); // gold
    private static readonly Color ColDotVisited= new Color(0.3f, 0.9f, 0.4f,  1f);    // green
    private static readonly Color ColDotCurrent= new Color(1f,  0.75f, 0f,    1f);    // amber
    private static readonly Color ColDotFuture = new Color(0.6f, 0.6f, 0.7f,  0.8f); // grey

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _dotMaterial = new Material(Shader.Find("Standard"));
        _dotMaterial.SetFloat("_Metallic",   0f);
        _dotMaterial.SetFloat("_Smoothness", 0.3f);
    }

    private void Start()
    {
        // Ensure fader exists (handles the fade-in automatically)
        SceneFader.EnsureExists();

        if (winnerPanel != null) winnerPanel.SetActive(false);

        if (returnMenuButton != null)
            returnMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (!GameState.GameInitialized)
        {
            Debug.LogWarning("MapSceneManager: GameState not initialised. " +
                             "Please start the game from the Main Menu.");
            return;
        }

        if (GameState.ReturningFromQuestion)
            HandleReturnFromQuestion();
        else
            InitializeMap();
    }

    // ── Main flow ─────────────────────────────────────────────────────────────

    /// <summary>Called on the very first entry into MapScene for this game.</summary>
    private void InitializeMap()
    {
        DrawRouteVisualization();
        if (GameState.IsFirstTurn)
            SpawnBusesRandomlyOnMap();
        else
            SnapBusesToLastPositions();
            
        StartCoroutine(MoveBusesToNextStops());
    }

    /// <summary>Called when MapScene is re-entered after a GameScene Q&A round.</summary>
    private void HandleReturnFromQuestion()
    {
        GameState.ReturningFromQuestion = false;

        // Advance players who answered correctly
        if (GameState.Player1AnsweredCorrect)
            GameState.Player1StopIndex = Mathf.Min(
                GameState.Player1StopIndex + 1, GameState.RouteStops.Length - 1);

        if (GameState.Player2AnsweredCorrect)
            GameState.Player2StopIndex = Mathf.Min(
                GameState.Player2StopIndex + 1, GameState.RouteStops.Length - 1);

        int lastIndex = GameState.RouteStops.Length - 1;
        bool p1Won = GameState.Player1StopIndex >= lastIndex;
        bool p2Won = GameState.Player2StopIndex >= lastIndex;

        DrawRouteVisualization();

        if (p1Won || p2Won)
        {
            SnapBusesToLastPositions();
            ShowWinner(p1Won, p2Won);
            return;
        }

        // Continue the route
        SnapBusesToLastPositions();
        StartCoroutine(MoveBusesToNextStops());
    }

    // ── Bus movement ──────────────────────────────────────────────────────────

    /// <summary>Instantly teleport each bus to its last visually arrived position.</summary>
    private void SnapBusesToLastPositions()
    {
        SnapBus(bus1, GameState.Player1LastPositionIndex);
        SnapBus(bus2, GameState.Player2LastPositionIndex);
    }

    private void SpawnBusesRandomlyOnMap()
    {
        if (routeMapManager == null) return;

        string[] allProvinces = routeMapManager.GetAllProvinceNames();
        if (allProvinces == null || allProvinces.Length == 0) return;

        string randomProv1 = allProvinces[UnityEngine.Random.Range(0, allProvinces.Length)];
        string randomProv2 = allProvinces[UnityEngine.Random.Range(0, allProvinces.Length)];

        // Ensure different spawning locations if possible
        if (allProvinces.Length > 1)
        {
            while (randomProv2 == randomProv1)
            {
                randomProv2 = allProvinces[UnityEngine.Random.Range(0, allProvinces.Length)];
            }
        }

        ProvinceController prov1 = routeMapManager.GetProvince(randomProv1);
        ProvinceController prov2 = routeMapManager.GetProvince(randomProv2);

        if (prov1 != null && bus1 != null)
            bus1.SetPositionToProvince(prov1);
        if (prov2 != null && bus2 != null)
            bus2.SetPositionToProvince(prov2);
    }

    private void SnapBus(BusController bus, int stopIndex)
    {
        if (bus == null || GameState.RouteStops == null) return;
        stopIndex = Mathf.Clamp(stopIndex, 0, GameState.RouteStops.Length - 1);
        ProvinceController prov = routeMapManager.GetProvince(GameState.RouteStops[stopIndex]);
        if (prov != null) bus.SetPositionToProvince(prov);
    }

    private IEnumerator MoveBusesToNextStops()
    {
        // Short pause so the idle/starting state is visible before movement begins
        yield return new WaitForSeconds(0.3f);

        int len = GameState.RouteStops.Length;

        int p1Target, p2Target;

        if (GameState.IsFirstTurn)
        {
            p1Target = 0;
            p2Target = 0;
        }
        else
        {
            p1Target = GameState.Player1StopIndex;
            p2Target = GameState.Player2StopIndex;
        }

        // Store question cities for GameScene
        GameState.Player1QuestionCity = GameState.RouteStops[p1Target];
        GameState.Player2QuestionCity = GameState.RouteStops[p2Target];

        // Command buses
        ProvinceController prov1 = routeMapManager.GetProvince(GameState.RouteStops[p1Target]);
        ProvinceController prov2 = routeMapManager.GetProvince(GameState.RouteStops[p2Target]);

        if (prov1 != null) bus1.MoveToProvince(prov1);
        if (prov2 != null) bus2.MoveToProvince(prov2);

        // Wait until both are stationary
        yield return new WaitUntil(() => !bus1.IsMoving && !bus2.IsMoving);

        // Record their actual arrived positions
        GameState.Player1LastPositionIndex = p1Target;
        GameState.Player2LastPositionIndex = p2Target;

        // The first turn is now complete
        GameState.IsFirstTurn = false;

        yield return new WaitForSeconds(arrivalPause);

        // Fade to GameScene
        SceneFader.Instance.FadeOutAndLoad("GameScene");
    }

    // ── Route visualisation ───────────────────────────────────────────────────

    private void DrawRouteVisualization()
    {
        if (GameState.RouteStops == null || routeMapManager == null) return;

        EnsureLineRenderer();
        ClearDots();

        int count = GameState.RouteStops.Length;
        Vector3[] positions = new Vector3[count];
        bool[] posValid     = new bool[count];
        List<Vector3> linePoints = new List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            ProvinceController prov = routeMapManager.GetProvince(GameState.RouteStops[i]);
            if (prov != null)
            {
                positions[i] = prov.GetBusStopPosition() + Vector3.up * lineHeightOffset;
                posValid[i]  = true;
                linePoints.Add(positions[i]);
            }
        }

        // ── Line ──
        _line.positionCount = linePoints.Count;
        _line.SetPositions(linePoints.ToArray());

        // ── Dots ──
        int p1 = GameState.Player1StopIndex;
        int p2 = GameState.Player2StopIndex;
        int maxVisited = Mathf.Max(p1, p2);

        for (int i = 0; i < count; i++)
        {
            if (!posValid[i]) continue;

            Color dotColor;
            if (i < maxVisited)          dotColor = ColDotVisited;
            else if (i == maxVisited)    dotColor = ColDotCurrent;
            else                         dotColor = ColDotFuture;

            SpawnDot(positions[i], dotColor, GameState.RouteStops[i], i);
        }
    }

    private void EnsureLineRenderer()
    {
        if (_line != null) return;

        _line = gameObject.GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>();

        _line.useWorldSpace    = true;
        _line.startWidth       = 0.18f;
        _line.endWidth         = 0.18f;
        _line.numCornerVertices = 4;
        _line.numCapVertices    = 4;

        // Use Sprites/Default so gradient colour works without extra shader setup
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = ColLine;
        _line.material = lineMat;

        _line.startColor = ColLine;
        _line.endColor   = new Color(1f, 0.45f, 0.1f, 0.95f); // orange at end
    }

    private void SpawnDot(Vector3 worldPos, Color color, string cityName, int stopIndex)
    {
        // Sphere primitive
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = $"RouteDot_{stopIndex}_{cityName}";
        dot.transform.position   = worldPos;
        dot.transform.localScale = Vector3.one * dotScale;

        // Remove physics collider — purely visual
        Collider col = dot.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Apply colour via shared material instance
        Material mat = new Material(_dotMaterial);
        mat.color = color;
        dot.GetComponent<Renderer>().material = mat;

        _dots.Add(dot);

        // ── City name label (WorldSpace canvas) ──
        GameObject labelRoot = new GameObject($"Label_{stopIndex}_{cityName}");
        labelRoot.transform.position = worldPos + Vector3.up * (dotScale * 0.75f);

        Canvas c = labelRoot.AddComponent<Canvas>();
        c.renderMode   = RenderMode.WorldSpace;
        c.sortingOrder = 10;

        RectTransform crt = labelRoot.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(2.5f, 0.6f);
        crt.localScale = Vector3.one * 0.01f;   // shrink to world units

        // TMP text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(labelRoot.transform, false);

        TMP_Text tmp = textGO.AddComponent<TextMeshPro>();
        tmp.text      = cityName;
        tmp.fontSize  = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        _dotLabels.Add(labelRoot);
    }

    private void ClearDots()
    {
        foreach (GameObject d in _dots)       if (d) Destroy(d);
        foreach (GameObject l in _dotLabels)  if (l) Destroy(l);
        _dots.Clear();
        _dotLabels.Clear();
    }

    // ── Winner screen ─────────────────────────────────────────────────────────

    private void ShowWinner(bool p1Won, bool p2Won)
    {
        string text;
        if (p1Won && p2Won)
            text = "🎉 Berabere!";
        else if (p1Won)
            text = $"🎉 {PlayerSetupManager.Player1Name} kazandı!";
        else
            text = $"🎉 {PlayerSetupManager.Player2Name} kazandı!";

        if (winnerPanel != null)
        {
            winnerPanel.SetActive(true);
            if (winnerLabel != null) winnerLabel.text = text;
        }
        else
        {
            // Fallback: create a simple overlay at runtime
            BuildFallbackWinnerPanel(text);
        }
    }

    private void BuildFallbackWinnerPanel(string text)
    {
        GameObject canvasGO = new GameObject("WinnerCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Dark semi-transparent background
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.78f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

        // Label
        GameObject labelGO = new GameObject("WinnerLabel");
        labelGO.transform.SetParent(canvasGO.transform, false);
        TMP_Text label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text      = text;
        label.fontSize  = 64;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color     = new Color(1f, 0.85f, 0.1f, 1f);
        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin  = new Vector2(0f, 0.55f);
        lrt.anchorMax  = new Vector2(1f, 0.75f);
        lrt.offsetMin  = Vector2.zero;
        lrt.offsetMax  = Vector2.zero;

        // Return button
        GameObject btnGO = new GameObject("ReturnBtn");
        btnGO.transform.SetParent(canvasGO.transform, false);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.55f, 0.95f, 1f);
        Button btn   = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(ReturnToMainMenu);
        RectTransform brt = btnGO.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.3f, 0.38f);
        brt.anchorMax = new Vector2(0.7f, 0.48f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        GameObject btnLabel = new GameObject("BtnLabel");
        btnLabel.transform.SetParent(btnGO.transform, false);
        TMP_Text bl = btnLabel.AddComponent<TextMeshProUGUI>();
        bl.text      = "Ana Menüye Dön";
        bl.fontSize  = 28;
        bl.fontStyle = FontStyles.Bold;
        bl.alignment = TextAlignmentOptions.Center;
        bl.color     = Color.white;
        RectTransform blrt = btnLabel.GetComponent<RectTransform>();
        blrt.anchorMin = Vector2.zero; blrt.anchorMax = Vector2.one;
        blrt.offsetMin = Vector2.zero; blrt.offsetMax = Vector2.zero;
    }

    private void ReturnToMainMenu()
    {
        GameState.GameInitialized = false;
        SceneFader.EnsureExists();
        SceneFader.Instance.FadeOutAndLoad("MainMenu");
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        ClearDots();
        if (_dotMaterial != null) Destroy(_dotMaterial);
    }
}
