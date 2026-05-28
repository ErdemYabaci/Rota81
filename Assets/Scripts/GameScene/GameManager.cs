using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Orchestrates one Q&A round in GameScene, then returns to MapScene.
///
/// Changes from the original:
///  - Questions come from GameState.Player1QuestionCity / Player2QuestionCity
///    so each player answers about the city their bus just reached.
///  - Only ONE round is played per visit; after it resolves the results are
///    stored in GameState and SceneFader loads MapScene.
///
/// Inspector wiring (unchanged):
///   player1Panel / player2Panel  — the two PlayerPanel components
///   player1Header / player2Header — header Images coloured from setup
///   resultOverlay / resultLabel  — brief between-panels overlay
///   nextRoundDelay               — seconds to show result before returning
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Player Panels")]
    [SerializeField] private PlayerPanel player1Panel;
    [SerializeField] private PlayerPanel player2Panel;

    [Header("Header Images (for runtime colour)")]
    [SerializeField] private Image player1Header;
    [SerializeField] private Image player2Header;

    [Header("Between-round Overlay")]
    [SerializeField] private GameObject resultOverlay;
    [SerializeField] private TMP_Text   resultLabel;
    [SerializeField] private float      nextRoundDelay = 3f;

    // Track who answered first this round
    private bool _roundResolving;
    private int  _answersThisRound;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        // Ensure SceneFader is alive in this scene too
        SceneFader.EnsureExists();

        // Set player names
        string p1Name = string.IsNullOrEmpty(PlayerSetupManager.Player1Name)
                        ? "Oyuncu 1" : PlayerSetupManager.Player1Name;
        string p2Name = string.IsNullOrEmpty(PlayerSetupManager.Player2Name)
                        ? "Oyuncu 2" : PlayerSetupManager.Player2Name;

        player1Panel.SetPlayerName(p1Name);
        player2Panel.SetPlayerName(p2Name);

        // Apply player colours to headers
        if (player1Header != null)
        {
            player1Header.color = PlayerSetupManager.Player1Color * 0.8f;
            var avatar1 = player1Header.transform.Find("AvatarCircle");
            if (avatar1 != null)
                avatar1.GetComponent<Image>().color = PlayerSetupManager.Player1Color;
        }

        if (player2Header != null)
        {
            player2Header.color = PlayerSetupManager.Player2Color * 0.8f;
            var avatar2 = player2Header.transform.Find("AvatarCircle");
            if (avatar2 != null)
                avatar2.GetComponent<Image>().color = PlayerSetupManager.Player2Color;
        }

        resultOverlay.SetActive(false);

        player1Panel.OnAnswered += OnPlayerAnswered;
        player2Panel.OnAnswered += OnPlayerAnswered;

        StartRound();
    }

    private void OnDestroy()
    {
        player1Panel.OnAnswered -= OnPlayerAnswered;
        player2Panel.OnAnswered -= OnPlayerAnswered;
    }

    // ── Round logic ───────────────────────────────────────────────────────────

    private void StartRound()
    {
        _roundResolving   = false;
        _answersThisRound = 0;

        // Each player answers about their own current destination city
        string city1 = GameState.Player1QuestionCity;
        string city2 = GameState.Player2QuestionCity;

        // Fallback: if GameState is empty (e.g. testing directly in editor)
        if (string.IsNullOrEmpty(city1))
            city1 = QuestionLoader.Instance != null
                    ? QuestionLoader.Instance.GetRandomProvinceName()
                    : "Samsun";

        if (string.IsNullOrEmpty(city2))
            city2 = QuestionLoader.Instance != null
                    ? QuestionLoader.Instance.GetRandomProvinceName()
                    : "Trabzon";

        QuestionData q1 = null;
        QuestionData q2 = null;

        if (QuestionLoader.Instance != null)
        {
            // Try to get distinct questions for each player
            q1 = QuestionLoader.Instance.GetRandomQuestion(city1);
            q2 = QuestionLoader.Instance.GetRandomQuestion(city2);

            // If cities are the same and we got the same question, retry once
            if (city1 == city2 && q1 != null && q2 != null && q1.id == q2.id)
                q2 = QuestionLoader.Instance.GetRandomQuestion(city2);

            // Last-resort fallbacks
            if (q1 == null) q1 = QuestionLoader.Instance.GetRandomQuestion(
                                     QuestionLoader.Instance.GetRandomProvinceName());
            if (q2 == null) q2 = q1;
        }

        player1Panel.LoadQuestion(q1, city1);
        player2Panel.LoadQuestion(q2, city2);
    }

    private void OnPlayerAnswered(PlayerPanel panel, bool correct)
    {
        if (_roundResolving) return;

        _answersThisRound++;

        if (_answersThisRound == 1)
        {
            PlayerPanel opponent = (panel == player1Panel) ? player2Panel : player1Panel;
            panel.LockAnswers(waitingForOpponent: true);
            if (!opponent.HasAnswered) opponent.StartCountdown();
        }

        if (player1Panel.HasAnswered && player2Panel.HasAnswered)
        {
            _roundResolving = true;
            StartCoroutine(ResolveRound());
        }
    }

    private IEnumerator ResolveRound()
    {
        player1Panel.StopCountdown();
        player2Panel.StopCountdown();
        player1Panel.RevealResult();
        player2Panel.RevealResult();

        // Store results so MapSceneManager can advance stop indices
        GameState.Player1AnsweredCorrect = player1Panel.AnsweredCorrect;
        GameState.Player2AnsweredCorrect = player2Panel.AnsweredCorrect;
        GameState.ReturningFromQuestion  = true;

        // Build result summary
        string p1Name  = PlayerSetupManager.Player1Name;
        string p2Name  = PlayerSetupManager.Player2Name;
        string city1   = GameState.Player1QuestionCity;
        string city2   = GameState.Player2QuestionCity;

        string summary =
            $"{p1Name} ({city1}): {(player1Panel.AnsweredCorrect ? "✓ Doğru" : "✗ Yanlış")}   " +
            $"{p2Name} ({city2}): {(player2Panel.AnsweredCorrect ? "✓ Doğru" : "✗ Yanlış")}";

        resultLabel.text = summary;
        resultOverlay.SetActive(true);

        yield return new WaitForSeconds(nextRoundDelay);

        resultOverlay.SetActive(false);

        // Return to MapScene (SceneFader handles the black fade)
        SceneFader.Instance.FadeOutAndLoad("MapScene");
    }
}
