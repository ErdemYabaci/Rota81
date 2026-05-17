using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Orchestrates the game loop: picks cities, loads questions into both panels,
/// handles the "first to answer → start opponent timer" logic, and resolves rounds.
///
/// Attach to a single "GameManager" GameObject in GameScene.
///
/// Inspector wiring:
///   player1Panel / player2Panel  — the two PlayerPanel components
///   resultOverlay                — a full-screen panel shown briefly between rounds
///   resultLabel                  — TMP text inside the overlay
///   nextRoundDelay               — seconds to show the result before the next question
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
        // Set player names from the setup screen
        string p1Name = string.IsNullOrEmpty(PlayerSetupManager.Player1Name)
                        ? "Oyuncu 1" : PlayerSetupManager.Player1Name;
        string p2Name = string.IsNullOrEmpty(PlayerSetupManager.Player2Name)
                        ? "Oyuncu 2" : PlayerSetupManager.Player2Name;

        player1Panel.SetPlayerName(p1Name);
        player2Panel.SetPlayerName(p2Name);

        // Apply the colour chosen in the setup screen to each header
        if (player1Header != null) player1Header.color = PlayerSetupManager.Player1Color;
        if (player2Header != null) player2Header.color = PlayerSetupManager.Player2Color;

        resultOverlay.SetActive(false);

        // Subscribe to answer events
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

        // Both players get questions from the SAME city.
        // Try up to 10 cities to find one with at least 2 distinct questions.
        string city = string.Empty;
        QuestionData q1 = null;
        QuestionData q2 = null;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            city = QuestionLoader.Instance.GetRandomProvinceName();
            q1   = QuestionLoader.Instance.GetRandomQuestion(city);
            q2   = QuestionLoader.Instance.GetRandomQuestion(city);

            // Accept if both questions exist and are different
            if (q1 != null && q2 != null && q1.id != q2.id)
                break;

            // This city had only one question left — reset it and keep trying
            QuestionLoader.Instance.ResetProvince(city);
        }

        // Last-resort: use the same question for both (shouldn't normally happen)
        if (q1 == null) q1 = QuestionLoader.Instance.GetRandomQuestion(
                                 QuestionLoader.Instance.GetRandomProvinceName());
        if (q2 == null) q2 = q1;

        player1Panel.LoadQuestion(q1, city);
        player2Panel.LoadQuestion(q2, city);
    }

    private void OnPlayerAnswered(PlayerPanel panel, bool correct)
    {
        if (_roundResolving) return;

        _answersThisRound++;

        if (_answersThisRound == 1)
        {
            // First answer — lock answerer's panel, start timer on the other
            PlayerPanel opponent = (panel == player1Panel) ? player2Panel : player1Panel;

            panel.LockAnswers(waitingForOpponent: true);

            if (!opponent.HasAnswered)
                opponent.StartCountdown();
        }

        // Both answered (or opponent timed out triggers another OnAnswered)
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

        // Build summary text
        string p1Name = PlayerSetupManager.Player1Name;
        string p2Name = PlayerSetupManager.Player2Name;
        string summary =
            $"{p1Name}: {(player1Panel.AnsweredCorrect ? "Doğru ✓" : "Yanlış ✗")}   " +
            $"{p2Name}: {(player2Panel.AnsweredCorrect ? "Doğru ✓" : "Yanlış ✗")}\n" +
            $"Toplam — {p1Name}: {player1Panel.Score}  |  {p2Name}: {player2Panel.Score}";

        resultLabel.text = summary;
        resultOverlay.SetActive(true);

        yield return new WaitForSeconds(nextRoundDelay);

        resultOverlay.SetActive(false);
        StartRound();
    }
}
