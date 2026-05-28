/// <summary>
/// Static cross-scene data bag.
/// All fields are set before a scene transition and read on the other side.
/// No MonoBehaviour, no DontDestroyOnLoad — plain static state is enough.
/// </summary>
public static class GameState
{
    // ── Route ─────────────────────────────────────────────────────────────────
    public static string   SelectedRouteName = "";
    public static string[] RouteStops        = null;   // ordered city names

    // ── Per-player confirmed position ─────────────────────────────────────────
    // Index into RouteStops.  0 = starting city.
    // Advance only when the player answers correctly.
    public static int Player1StopIndex = 0;
    public static int Player2StopIndex = 0;

    // ── City for current Q&A round (set by MapSceneManager before fading) ─────
    public static string Player1QuestionCity = "";
    public static string Player2QuestionCity = "";

    // ── Q&A results (set by GameManager, read by MapSceneManager) ────────────
    public static bool Player1AnsweredCorrect = false;
    public static bool Player2AnsweredCorrect = false;

    // ── Flow flags ────────────────────────────────────────────────────────────
    /// <summary>True after GameScene stores results; MapSceneManager clears it.</summary>
    public static bool ReturningFromQuestion = false;

    /// <summary>False until a new game is properly started from MainMenu.</summary>
    public static bool GameInitialized = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Call once from PlayerSetupManager.OnReady before loading MapScene.
    /// </summary>
    public static void StartNewGame(string routeName, string[] stops)
    {
        SelectedRouteName        = routeName;
        RouteStops               = stops;
        Player1StopIndex         = 0;
        Player2StopIndex         = 0;
        Player1QuestionCity      = "";
        Player2QuestionCity      = "";
        Player1AnsweredCorrect   = false;
        Player2AnsweredCorrect   = false;
        ReturningFromQuestion    = false;
        GameInitialized          = true;
    }
}
