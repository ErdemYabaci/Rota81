using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Run via menu: Tools → Build GameScene UI
/// Opens GameScene, rebuilds the Canvas with the full split-screen layout,
/// wires all PlayerPanel + GameManager references, then saves.
/// </summary>
public static class SceneBuilder
{
    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color P1Header  = new Color(0.18f, 0.38f, 0.62f, 1f);   // dark blue
    static readonly Color P2Header  = new Color(0.52f, 0.22f, 0.12f, 1f);   // dark brown
    static readonly Color PanelBg   = new Color(0.95f, 0.95f, 0.93f, 1f);   // off-white
    static readonly Color OrangeBar = new Color(0.94f, 0.62f, 0.15f, 1f);

    // ── Entry point ──────────────────────────────────────────────────────────
    [MenuItem("Tools/Build GameScene UI")]
    public static void BuildGameScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");

        // Remove old Canvas; keep Camera, Light, EventSystem
        var oldCanvas = Object.FindFirstObjectByType<Canvas>();
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);

        // ── Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Panels
        var p1 = BuildPanel(canvasGO.transform, "Player1Panel",
                            new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                            P1Header, "A", "Ahmet");

        var p2 = BuildPanel(canvasGO.transform, "Player2Panel",
                            new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                            P2Header, "Z", "Zeynep");

        // ── Divider
        BuildDivider(canvasGO.transform);

        // ── Result overlay
        var (overlayGO, resultLabel) = BuildResultOverlay(canvasGO.transform);

        // ── GameManager GO
        var gmGO = new GameObject("GameManager");
        var gm   = gmGO.AddComponent<GameManager>();
        var gmSO = new SerializedObject(gm);
        gmSO.FindProperty("player1Panel")  .objectReferenceValue = p1;
        gmSO.FindProperty("player2Panel")  .objectReferenceValue = p2;
        gmSO.FindProperty("resultOverlay") .objectReferenceValue = overlayGO;
        gmSO.FindProperty("resultLabel")   .objectReferenceValue = resultLabel;
        gmSO.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] GameScene built and saved.");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Panel builder
    // ════════════════════════════════════════════════════════════════════════
    static PlayerPanel BuildPanel(Transform canvasT, string panelName,
                                  Vector2 anchorMin, Vector2 anchorMax,
                                  Color headerColor, string initial, string playerName)
    {
        // Root
        var panelGO = NewRect(canvasT, panelName);
        Anchor(panelGO, anchorMin, anchorMax);
        var panelImg   = panelGO.AddComponent<Image>();
        panelImg.color = PanelBg;

        var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding             = new RectOffset(10, 10, 10, 10);
        vlg.spacing             = 8f;
        vlg.childControlWidth   = true;
        vlg.childControlHeight  = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── Header
        var headerGO  = NewImg(panelGO.transform, "Header", headerColor);
        LE(headerGO, ph: 70);

        // AvatarCircle
        var avatarGO  = NewImg(headerGO.transform, "AvatarCircle",
                               Lighten(headerColor, 0.12f));
        var avatarRT  = avatarGO.GetComponent<RectTransform>();
        avatarRT.anchorMin        = new Vector2(0f, 0.5f);
        avatarRT.anchorMax        = new Vector2(0f, 0.5f);
        avatarRT.pivot            = new Vector2(0f, 0.5f);
        avatarRT.anchoredPosition = new Vector2(10f, 0f);
        avatarRT.sizeDelta        = new Vector2(44f, 44f);
        avatarGO.GetComponent<Image>().sprite =
            Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

        // AvatarInitial
        var initGO = NewTMP(avatarGO.transform, "AvatarInitial", initial, 18f,
                            Color.white, TextAlignmentOptions.Center);
        Stretch(initGO.GetComponent<RectTransform>());

        // PlayerNameLabel
        var nameGO = NewTMP(headerGO.transform, "PlayerNameLabel", playerName, 18f,
                            Color.white, TextAlignmentOptions.MidlineLeft);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = nameRT.anchorMax = new Vector2(0f, 0.5f);
        nameRT.pivot            = new Vector2(0f, 0.5f);
        nameRT.anchoredPosition = new Vector2(64f, 0f);
        nameRT.sizeDelta        = new Vector2(200f, 40f);

        // ScoreLabel
        var scoreGO = NewTMP(headerGO.transform, "ScoreLabel", "Puan: 0", 16f,
                             Color.white, TextAlignmentOptions.MidlineRight);
        var scoreRT = scoreGO.GetComponent<RectTransform>();
        scoreRT.anchorMin = scoreRT.anchorMax = new Vector2(1f, 0.5f);
        scoreRT.pivot            = new Vector2(1f, 0.5f);
        scoreRT.anchoredPosition = new Vector2(-10f, 0f);
        scoreRT.sizeDelta        = new Vector2(130f, 40f);

        // ── CityLabel
        var cityGO = NewTMP(panelGO.transform, "CityLabel", "Şehir: —", 14f,
                            headerColor, TextAlignmentOptions.Center);
        LE(cityGO, ph: 30);

        // ── QuestionBox
        var qBoxGO = NewImg(panelGO.transform, "QuestionBox", Color.white);
        LE(qBoxGO, ph: 120);
        var qVLG = qBoxGO.AddComponent<VerticalLayoutGroup>();
        qVLG.padding            = new RectOffset(10, 10, 10, 10);
        qVLG.spacing            = 4f;
        qVLG.childControlWidth  = true;
        qVLG.childControlHeight = false;
        qVLG.childForceExpandWidth  = true;
        qVLG.childForceExpandHeight = false;

        var qLabelGO = NewTMP(qBoxGO.transform, "QuestionLabel", "", 15f,
                              headerColor, TextAlignmentOptions.Center);
        LE(qLabelGO, fh: 1f);
        qLabelGO.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        var catGO = NewTMP(qBoxGO.transform, "CategoryLabel", "cografya · orta", 11f,
                           Color.gray, TextAlignmentOptions.MidlineLeft);
        LE(catGO, ph: 20);

        // ── Answer Buttons
        string[] letters = { "A", "B", "C", "D" };
        var answerButtons = new Button[4];
        var answerLabels  = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var (btnGO, btnTMP) = NewButton(panelGO.transform,
                                            $"AnswerButton_{letters[i]}",
                                            $"{letters[i]}  Seçenek {i + 1}");
            LE(btnGO, ph: 50);
            answerButtons[i] = btnGO.GetComponent<Button>();
            answerLabels[i]  = btnTMP;
        }

        // ── StatusLabel
        var statusGO = NewTMP(panelGO.transform, "StatusLabel", "", 12f,
                              Color.gray, TextAlignmentOptions.Center);
        LE(statusGO, ph: 28);

        // ── StatsLabel
        var statsGO = NewTMP(panelGO.transform, "StatsLabel",
                             "Doğru: 0 · Yanlış: 0 · Toplam: 0", 12f,
                             Color.gray, TextAlignmentOptions.Center);
        LE(statsGO, ph: 28);

        // ── TimerGroup  (ignore layout so VLG skips it)
        var timerGroupGO = NewRect(panelGO.transform, "TimerGroup");
        var tgLE = timerGroupGO.AddComponent<LayoutElement>();
        tgLE.ignoreLayout = true;
        var tgRT = timerGroupGO.GetComponent<RectTransform>();
        tgRT.anchorMin        = new Vector2(0f, 0f);
        tgRT.anchorMax        = new Vector2(1f, 0f);
        tgRT.pivot            = new Vector2(0.5f, 0f);
        tgRT.offsetMin        = new Vector2(10f, 46f);
        tgRT.offsetMax        = new Vector2(-10f, 74f);   // 28 px tall

        var timerBarGO = NewImg(timerGroupGO.transform, "TimerBar", OrangeBar);
        Stretch(timerBarGO.GetComponent<RectTransform>());
        var timerBarImg       = timerBarGO.GetComponent<Image>();
        timerBarImg.type      = Image.Type.Filled;
        timerBarImg.fillMethod = Image.FillMethod.Horizontal;
        timerBarImg.fillOrigin = 0;
        timerBarImg.fillAmount = 1f;

        var timerLabelGO = NewTMP(timerGroupGO.transform, "TimerLabel", "Süre: 15s",
                                  13f, Color.black, TextAlignmentOptions.Center);
        Stretch(timerLabelGO.GetComponent<RectTransform>());

        timerGroupGO.SetActive(false);

        // ── Wire PlayerPanel
        var pp   = panelGO.AddComponent<PlayerPanel>();
        var ppSO = new SerializedObject(pp);
        ppSO.FindProperty("playerNameLabel").objectReferenceValue =
            nameGO.GetComponent<TextMeshProUGUI>();
        ppSO.FindProperty("scoreLabel").objectReferenceValue =
            scoreGO.GetComponent<TextMeshProUGUI>();
        ppSO.FindProperty("cityLabel").objectReferenceValue =
            cityGO.GetComponent<TextMeshProUGUI>();
        ppSO.FindProperty("questionLabel").objectReferenceValue =
            qLabelGO.GetComponent<TextMeshProUGUI>();
        ppSO.FindProperty("categoryLabel").objectReferenceValue =
            catGO.GetComponent<TextMeshProUGUI>();
        ppSO.FindProperty("statusLabel").objectReferenceValue =
            statusGO.GetComponent<TextMeshProUGUI>();
        ppSO.FindProperty("statsLabel").objectReferenceValue =
            statsGO.GetComponent<TextMeshProUGUI>();
        ppSO.FindProperty("timerBar").objectReferenceValue =
            timerBarImg;
        ppSO.FindProperty("timerLabel").objectReferenceValue =
            timerLabelGO.GetComponent<TextMeshProUGUI>();

        var abProp = ppSO.FindProperty("answerButtons");
        abProp.arraySize = 4;
        var alProp = ppSO.FindProperty("answerLabels");
        alProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            abProp.GetArrayElementAtIndex(i).objectReferenceValue = answerButtons[i];
            alProp.GetArrayElementAtIndex(i).objectReferenceValue = answerLabels[i];
        }
        ppSO.ApplyModifiedProperties();

        return pp;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Divider & Result Overlay
    // ════════════════════════════════════════════════════════════════════════
    static void BuildDivider(Transform parent)
    {
        var go = NewImg(parent, "Divider", new Color(0.7f, 0.7f, 0.7f, 1f));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(2f, 0f);
    }

    static (GameObject overlay, TMP_Text label) BuildResultOverlay(Transform parent)
    {
        var go = NewImg(parent, "ResultOverlay", new Color(0f, 0f, 0f, 0.71f));
        Stretch(go.GetComponent<RectTransform>());

        var labelGO = NewTMP(go.transform, "ResultLabel", "", 28f,
                             Color.white, TextAlignmentOptions.Center);
        Stretch(labelGO.GetComponent<RectTransform>());

        go.SetActive(false);
        return (go, labelGO.GetComponent<TMP_Text>());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════
    static GameObject NewRect(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject NewImg(Transform parent, string name, Color color)
    {
        var go  = NewRect(parent, name);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject NewTMP(Transform parent, string name, string text,
                             float size, Color color, TextAlignmentOptions align)
    {
        var go  = NewRect(parent, name);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = align;
        return go;
    }

    static (GameObject go, TMP_Text label) NewButton(Transform parent,
                                                      string name, string text)
    {
        var go  = NewRect(parent, name);
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGO  = NewRect(go.transform, "Text (TMP)");
        var textRT  = textGO.GetComponent<RectTransform>();
        Stretch(textRT);
        textRT.offsetMin = new Vector2(12f, 0f);
        textRT.offsetMax = new Vector2(-12f, 0f);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 15f;
        tmp.color     = new Color(0.15f, 0.15f, 0.15f, 1f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        return (go, tmp);
    }

    static void LE(GameObject go, float ph = -1f, float fh = -1f)
    {
        var le = go.AddComponent<LayoutElement>();
        if (ph >= 0f) le.preferredHeight = ph;
        if (fh >= 0f) le.flexibleHeight  = fh;
    }

    static void Anchor(GameObject go, Vector2 min, Vector2 max)
    {
        var rt        = go.GetComponent<RectTransform>();
        rt.anchorMin  = min;
        rt.anchorMax  = max;
        rt.pivot      = new Vector2(0.5f, 0.5f);
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Color Lighten(Color c, float amount) =>
        new Color(Mathf.Clamp01(c.r + amount),
                  Mathf.Clamp01(c.g + amount),
                  Mathf.Clamp01(c.b + amount), c.a);
}
