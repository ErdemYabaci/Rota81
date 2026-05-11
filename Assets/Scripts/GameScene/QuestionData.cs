using System.Collections.Generic;

// ── Data models ─────────────────────────────────────────────────────────────
// These match the JSON schema exactly so JsonUtility can deserialise them.
// Place this file anywhere in your Assets folder — no MonoBehaviour needed.

[System.Serializable]
public class QuestionData
{
    public string   id;
    public string   category;
    public string   difficulty;
    public string   question;
    public string[] options;
    public int      answerIndex;
    public string   answer;
    public string   explanation;
}

[System.Serializable]
public class ProvinceData
{
    public string         plateCode;
    public string         name;
    public int            questionCount;
    public List<QuestionData> questions;
}

[System.Serializable]
public class QuestionDatabase
{
    public List<ProvinceData> provinces;
}
