using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads questions.json from Resources and serves random, non-repeating
/// questions per province.  One instance lives in the scene as a singleton.
///
/// Place your JSON file at:  Assets/Resources/questions.json
/// </summary>
public class QuestionLoader : MonoBehaviour
{
    public static QuestionLoader Instance { get; private set; }

    /// <summary>
    /// Drag the question JSON TextAsset here in the Inspector (wired automatically
    /// by SceneBuilder). If left empty, falls back to Resources.Load("questions").
    /// </summary>
    [SerializeField] private TextAsset jsonFile;

    private QuestionDatabase _db;

    // Tracks which question indices have been used per province name
    private Dictionary<string, List<int>> _usedIndices = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        // Prefer the directly-wired asset; fall back to Resources/questions.json
        TextAsset json = jsonFile;
        if (json == null)
            json = Resources.Load<TextAsset>("questions");

        if (json == null)
        {
            Debug.LogError("QuestionLoader: no JSON asset wired and questions.json not found in Resources/");
            return;
        }
        _db = JsonUtility.FromJson<QuestionDatabase>(json.text);
        Debug.Log($"QuestionLoader: loaded {_db.provinces.Count} provinces.");
    }

    /// <summary>
    /// Returns a random question for the given province name that hasn't been
    /// asked yet this session.  Returns null when all questions are exhausted
    /// (caller should pick a new province or reset).
    /// </summary>
    public QuestionData GetRandomQuestion(string provinceName)
    {
        ProvinceData province = _db.provinces.Find(p => p.name == provinceName);
        if (province == null)
        {
            Debug.LogWarning($"QuestionLoader: province '{provinceName}' not found.");
            return null;
        }

        if (!_usedIndices.ContainsKey(provinceName))
            _usedIndices[provinceName] = new List<int>();

        List<int> used = _usedIndices[provinceName];

        if (used.Count >= province.questions.Count)
        {
            Debug.Log($"QuestionLoader: all questions used for {provinceName}, resetting.");
            used.Clear();
        }

        // Build candidate list
        List<int> candidates = new();
        for (int i = 0; i < province.questions.Count; i++)
            if (!used.Contains(i)) candidates.Add(i);

        int pick = candidates[Random.Range(0, candidates.Count)];
        used.Add(pick);
        return province.questions[pick];
    }

    /// <summary>Returns a random province name from the database.</summary>
    public string GetRandomProvinceName()
    {
        if (_db == null || _db.provinces.Count == 0) return string.Empty;
        return _db.provinces[Random.Range(0, _db.provinces.Count)].name;
    }

    /// <summary>Resets used-question history for a province (e.g. new game).</summary>
    public void ResetProvince(string provinceName) =>
        _usedIndices.Remove(provinceName);

    public void ResetAll() => _usedIndices.Clear();
}
