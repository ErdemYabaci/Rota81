using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls one player's half of the split screen.
/// Attach one instance to each player panel root.
///
/// Inspector wiring:
///   playerNameLabel   — TMP label showing the player's name
///   scoreLabel        — TMP label for current score (e.g. "Puan: 3")
///   cityLabel         — TMP label showing city name
///   questionLabel     — TMP label for the question text
///   categoryLabel     — TMP label for category + difficulty
///   answerButtons     — 4 Buttons (A B C D)
///   answerLabels      — 4 TMP labels on those buttons
///   statusLabel       — TMP label for "locked / waiting / time's up" feedback
///   statsLabel        — TMP label for correct/wrong totals
///   timerBar          — Image whose fillAmount drives the countdown (set Image Type = Filled)
///   timerLabel        — TMP label showing remaining seconds
///   timerDuration     — seconds the opponent gets after this player answers first
/// </summary>
public class PlayerPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameLabel;
    [SerializeField] private TMP_Text scoreLabel;
    [SerializeField] private TMP_Text cityLabel;
    [SerializeField] private TMP_Text questionLabel;
    [SerializeField] private TMP_Text categoryLabel;
    [SerializeField] private Button[] answerButtons;   // exactly 4
    [SerializeField] private TMP_Text[] answerLabels;  // exactly 4
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text statsLabel;

    [Header("Timer (shown when opponent answers first)")]
    [SerializeField] private Image    timerBar;
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private float    timerDuration = 15f;

    // ── State ────────────────────────────────────────────────────────────────
    public bool  HasAnswered    { get; private set; }
    public bool  AnsweredCorrect { get; private set; }
    public int   Score          { get; private set; }
    public int   Correct        { get; private set; }
    public int   Wrong          { get; private set; }

    private QuestionData _currentQuestion;
    private Coroutine    _timerCoroutine;

    // Fired when this player submits an answer (correct: bool)
    public event Action<PlayerPanel, bool> OnAnswered;

    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly Color ColDefault  = new Color(1f,    1f,    1f,    1f);
    private static readonly Color ColSelected = new Color(0.88f, 0.94f, 1f,    1f);
    private static readonly Color ColCorrect  = new Color(0.87f, 0.97f, 0.82f, 1f);
    private static readonly Color ColWrong    = new Color(0.99f, 0.9f,  0.9f,  1f);
    private static readonly Color ColLocked   = new Color(0.93f, 0.93f, 0.93f, 1f);

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetPlayerName(string playerName)
    {
        playerNameLabel.text = playerName;
    }

    /// <summary>Load a new question and reset all visual state.</summary>
    public void LoadQuestion(QuestionData q, string cityName)
    {
        _currentQuestion = q;
        HasAnswered       = false;
        AnsweredCorrect   = false;

        cityLabel.text     = $"Şehir: {cityName}";
        questionLabel.text = q.question;
        categoryLabel.text = $"{q.category} · {q.difficulty}";
        statusLabel.text   = string.Empty;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerLabels[i].text                       = $"{OptionLetter(i)}  {q.options[i]}";
            answerButtons[i].GetComponent<Image>().color = ColDefault;
            answerButtons[i].interactable              = true;

            int captured = i;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(captured));
        }

        HideTimer();
        StopCountdown();
    }

    /// <summary>Lock all buttons (called when this player answers first and we
    /// are waiting for the opponent, OR called by GameManager when round ends).</summary>
    public void LockAnswers(bool waitingForOpponent = false)
    {
        foreach (Button b in answerButtons) b.interactable = false;
        if (waitingForOpponent)
            statusLabel.text = "Cevap kilitlendi — rakip bekleniyor";
    }

    /// <summary>Start the countdown timer on this panel (opponent answered first).</summary>
    public void StartCountdown()
    {
        StopCountdown();
        ShowTimer();
        _timerCoroutine = StartCoroutine(CountdownRoutine());
    }

    public void StopCountdown()
    {
        if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
    }

    /// <summary>Reveal correct/wrong highlights after the round resolves.</summary>
    public void RevealResult()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            Image img = answerButtons[i].GetComponent<Image>();
            if (i == _currentQuestion.answerIndex)
                img.color = ColCorrect;
            else if (!answerButtons[i].interactable && i != _currentQuestion.answerIndex && HasAnswered)
                img.color = ColWrong;
        }

        if (HasAnswered)
        {
            statusLabel.text = AnsweredCorrect ? "Doğru!" : $"Yanlış — doğru: {_currentQuestion.answer}";
        }
        else
        {
            statusLabel.text = "Süre doldu — yanlış sayıldı";
        }

        RefreshStats();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void OnAnswerSelected(int index)
    {
        if (HasAnswered) return;

        HasAnswered     = true;
        AnsweredCorrect = (index == _currentQuestion.answerIndex);

        // Visual feedback — highlight chosen button
        answerButtons[index].GetComponent<Image>().color = ColSelected;

        if (AnsweredCorrect) { Score += 10; Correct++; }
        else                 { Wrong++; }

        scoreLabel.text = $"Puan: {Score}";
        LockAnswers();
        StopCountdown();
        HideTimer();

        OnAnswered?.Invoke(this, AnsweredCorrect);
    }

    private IEnumerator CountdownRoutine()
    {
        float elapsed = 0f;
        while (elapsed < timerDuration)
        {
            elapsed += Time.deltaTime;
            float remaining = timerDuration - elapsed;
            timerBar.fillAmount  = remaining / timerDuration;
            timerLabel.text      = $"Süre: {Mathf.CeilToInt(remaining)}s";
            yield return null;
        }

        // Time ran out — treat as wrong
        HasAnswered     = true;
        AnsweredCorrect = false;
        Wrong++;

        LockAnswers();
        HideTimer();
        OnAnswered?.Invoke(this, false);
    }

    private void ShowTimer() { if (timerBar)  timerBar.transform.parent.gameObject.SetActive(true); }
    private void HideTimer() { if (timerBar)  timerBar.transform.parent.gameObject.SetActive(false); }

    private void RefreshStats()
    {
        statsLabel.text = $"Doğru: {Correct} · Yanlış: {Wrong} · Toplam: {Correct + Wrong}";
    }

    private static string OptionLetter(int i) => ((char)('A' + i)).ToString();
}
