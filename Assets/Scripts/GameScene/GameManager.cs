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
        player1Panel.SetPlayerName(PlayerSetupManager.Player1Name);
        player2Panel.SetPlayerName(PlayerSetupManager.Player2Name);

        // Apply chosen colours to each panel's header (optional — wire header Image refs
        // to GameManager if you want to tint them at runtime)

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

        // Pick a random city and a question for each player
        // Both players get a question about the SAME city (same province, different question)
        string city = QuestionLoader.Instance.GetRandomProvinceName();

        QuestionData q1 = QuestionLoader.Instance.GetRandomQuestion(city);
        QuestionData q2 = QuestionLoader.Instance.GetRandomQuestion(city);

        // Fallback: if the province ran out of unique questions pick a second one for P2
        if (q2 == null || q2.id == q1.id)
        {
            string city2 = QuestionLoader.Instance.GetRandomProvinceName();
            q2 = QuestionLoader.Instance.GetRandomQuestion(city2);
            player1Panel.LoadQuestion(q1, city);
            player2Panel.LoadQuestion(q2, city2);
        }
        else
        {
            player1Panel.LoadQuestion(q1, city);
            player2Panel.LoadQuestion(q2, city);
        }
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
