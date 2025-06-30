using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Params;
using System.Linq;

public class SongResult : MonoBehaviour
{
    // 歌曲信息显示组件
    public Image jacketImage;         // 封面图
    public TextMeshProUGUI songNameText;  // 歌曲名
    public TextMeshProUGUI artistNameText; // 作者名
    public Image difficultyImage;       // 难度颜色显示
    public TextMeshProUGUI difficultyText; // 难度文本显示

    // 得分显示组件
    public TextMeshProUGUI scoreText;       // 总分显示
    public TextMeshProUGUI syncText;        // Sync判定数
    public TextMeshProUGUI linkText;        // Link判定数
    public TextMeshProUGUI fuzzText;        // Fuzz判定数
    public TextMeshProUGUI nullText;        // Null判定数
    //public TextMeshProUGUI comboText;       // 最大连击数

    // 场景切换按钮
    public Button retryButton;
    public Button nextButton;

    // 引用ScoreManager获取得分数据
    private ScoreManager scoreManager;

    // 歌曲信息
    private SongInfo selectedSongInfo;
    private int selectedDifficulty;

    void Awake()
    {
        // 初始化组件引用
        //scoreManager = FindObjectOfType<ScoreManager>();
        //scoreManager = GetComponent<ScoreManager>();
        scoreManager = ScoreManager.instance;

        // 设置按钮事件
        retryButton.onClick.AddListener(RetryCurrentSong);
        nextButton.onClick.AddListener(() => StartCoroutine(GoBackToSongSelect()));
    }

    void Start()
    {
        // 从SongAndChartData获取选中的歌曲信息
        selectedSongInfo = SongAndChartData.GetSelectedSongInfo();
        selectedDifficulty = SongAndChartData.selectedDifficulty;

        if (selectedSongInfo == null)
        {
            Debug.LogError("未获取到选中的歌曲信息");
            //StartCoroutine(GoBackToSongSelectWithDelay());
            return;
        }

        // 显示歌曲信息
        LoadAndDisplayJacket(selectedSongInfo);
        UpdateSongAndArtistDisplay(selectedSongInfo);
        UpdateSelectedSongDifficultyDisplay();

        // 显示得分信息（从ScoreManager获取）
        DisplayScoreAndCombo();

        // 播放结果场景背景音乐或音效
        // MusicManager.instance.PlayResultMusic();
    }

    /// <summary>
    /// 加载并显示歌曲封面
    /// </summary>
    private void LoadAndDisplayJacket(SongInfo songInfo)
    {
        if (jacketImage == null) return;

        Sprite coverSprite = SongAndChartData.GetCoverSprite();
        if (coverSprite != null)
        {
            jacketImage.sprite = coverSprite;
        }
        else
        {
            Debug.LogWarning($"未找到歌曲 {songInfo.song.title} 的封面图，使用默认封面");
            // jacketImage.sprite = defaultJacketSprite; // 可设置默认封面
        }
    }

    /// <summary>
    /// 更新歌曲名和作者名显示
    /// </summary>
    private void UpdateSongAndArtistDisplay(SongInfo songInfo)
    {
        if (songNameText == null || artistNameText == null) return;

        songNameText.text = songInfo.song.title;
        artistNameText.text = songInfo.song.artist;

        // 设置字体（参考SongSelect的逻辑）
        SetFontForText(songNameText, songInfo.song.title);
        SetFontForText(artistNameText, songInfo.song.artist);
    }

    /// <summary>
    /// 更新选中歌曲的难度显示
    /// </summary>
    private void UpdateSelectedSongDifficultyDisplay()
    {
        if (difficultyImage == null || difficultyText == null) return;

        // 检查当前难度是否存在谱面
        ChartInfo chartInfo = FindChartInfoByDifficulty(selectedSongInfo, selectedDifficulty);

        if (chartInfo != null)
        {
            // 存在谱面，显示正常颜色和难度
            if (ChartParams.difficultyColorMap.TryGetValue(selectedDifficulty, out Color difficultyColor))
            {
                difficultyImage.color = difficultyColor;
            }
            difficultyText.text = $"{ChartParams.difficultyTextMap[selectedDifficulty]} Lv {chartInfo.level}";
        }
        else
        {
            // 不存在谱面，显示灰色和null
            difficultyImage.color = Color.gray;
            difficultyText.text = $"{ChartParams.difficultyTextMap[selectedDifficulty]} null";
        }
    }

    /// <summary>
    /// 显示得分和连击数
    /// </summary>
    private void DisplayScoreAndCombo()
    {
        if (scoreManager == null)
        {
            Debug.LogError("未找到ScoreManager组件");
            //SetDefaultAutoPlayScores();
            return;
        }

        try
        {
            // 从ScoreManager获取最后得分和连击数
            int finalScore = 0;
            int maxCombo = 0;

            if (scoreManager.SumScoreMap.Count > 0)
            {
                // 获取字典中最后一个时间点的得分（按时间排序后的最后一个）
                float lastTime = scoreManager.SumScoreMap.Keys.Max();
                finalScore = scoreManager.SumScoreMap[lastTime];
            }

            if (scoreManager.SumComboMap.Count > 0)
            {
                float lastTime = scoreManager.SumComboMap.Keys.Max();
                maxCombo = scoreManager.SumComboMap[lastTime];
            }

            // 显示得分和连击数
            scoreText.text = $"{finalScore}";
            //comboText.text = $"Max Combo: {maxCombo}";

            // 由于是AutoPlay，假设所有判定都是最佳
            syncText.text = $"Sync: {maxCombo}";
            linkText.text = "Link: 0";
            fuzzText.text = "Fuzz: 0";
            nullText.text = "Null: 0";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"获取得分数据出错: {e.Message}");
            //SetDefaultAutoPlayScores();
        }
    }

    /// <summary>
    /// 设置AutoPlay的默认得分（当数据获取失败时）
    /// </summary>
    //private void SetDefaultAutoPlayScores()
    //{
    //    scoreText.text = "1000000";
    //    //comboText.text = "Max Combo: 900";
    //    syncText.text = "Sync: 900";
    //    linkText.text = "Link: 0";
    //    fuzzText.text = "Fuzz: 0";
    //    nullText.text = "Null: 0";
    //}

    /// <summary>
    /// 重试当前歌曲
    /// </summary>
    private void RetryCurrentSong()
    {
        // 播放按钮音效
        // AudioManager.instance.PlayButtonClickSound();

        // 切换回AutoPlay场景
        SceneTransitionManagerDoor.instance.StartTransitionToScene("AutoPlay");
    }

    /// <summary>
    /// 返回选歌场景
    /// </summary>
    private IEnumerator GoBackToSongSelect()
    {
        if (SceneTransitionManagerFade.instance == null)
        {
            // 通过物体名称查找
            GameObject managerObj = GameObject.Find("SceneTransitionManager");
            if (managerObj != null)
            {
                SceneTransitionManagerFade manager = managerObj.GetComponent<SceneTransitionManagerFade>();
                if (manager != null)
                {
                    SceneTransitionManagerFade.instance = manager;
                    Debug.Log("通过物体名称找到 SceneTransitionManagerFade");
                    StartCoroutine(manager.TransitionToScene("SongSelect"));
                }
                else
                {
                    Debug.Log("物体存在但缺少组件");
                    SceneManager.LoadScene("SongSelect");
                }
            }
            else
            {
                Debug.Log("未找到 SceneTransitionManager 物体");
                SceneManager.LoadScene("SongSelect");
            }
        }
        else
        {
            //Debug.Log(1);
            StartCoroutine(SceneTransitionManagerFade.instance.TransitionToScene("SongSelect"));
        }
        yield break;
    }



    /// <summary>
    /// 设置文本的字体（参考SongSelect的逻辑）
    /// </summary>
    private void SetFontForText(TextMeshProUGUI textComponent, string text)
    {
        if (textComponent == null || string.IsNullOrEmpty(text)) return;

        TMP_FontAsset fontAsset = GetFontBasedOnCharacters(text);
        if (fontAsset != null)
        {
            textComponent.font = fontAsset;

            // 根据字体类型设置字号
            if (IsNotoSansFont(fontAsset))
            {
                textComponent.fontSize = 35;
            }
            else
            {
                textComponent.fontSize = 50;
            }
        }
    }

    /// <summary>
    /// 根据文本内容选择合适的字体
    /// </summary>
    private TMP_FontAsset GetFontBasedOnCharacters(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        if (ContainsChinese(input))
        {
            return Resources.Load<TMP_FontAsset>("Fonts/NotoSansSC-Regular SDF");
        }
        else if (ContainsKorean(input))
        {
            return Resources.Load<TMP_FontAsset>("Fonts/NotoSansKR-Regular SDF");
        }
        else if (ContainsJapanese(input))
        {
            return Resources.Load<TMP_FontAsset>("Fonts/NotoSansJP-Regular SDF");
        }
        return Resources.Load<TMP_FontAsset>("Fonts/Jupiter SDF");
    }

    // 以下字体检测方法与SongSelect一致
    private bool IsCJKUnifiedIdeograph(char c)
    {
        return (c >= 0x4E00 && c <= 0x9FFF) ||
               (c >= 0x3400 && c <= 0x4DBF) ||
               (c >= 0x20000 && c <= 0x2A6DF) ||
               (c >= 0x2A700 && c <= 0x2B73F) ||
               (c >= 0x2B740 && c <= 0x2B81F) ||
               (c >= 0x2B820 && c <= 0x2CEAF) ||
               (c >= 0xF900 && c <= 0xFAFF) ||
               (c >= 0x2F800 && c <= 0x2FA1F);
    }

    private bool ContainsChinese(string input)
    {
        foreach (char c in input)
        {
            if (IsCJKUnifiedIdeograph(c))
            {
                return true;
            }
        }
        return false;
    }

    private bool ContainsKorean(string input)
    {
        foreach (char c in input)
        {
            if ((c >= 0xAC00 && c <= 0xD7AF) ||
                (c >= 0x1100 && c <= 0x11FF) ||
                (c >= 0x3130 && c <= 0x318F) ||
                (c >= 0xA960 && c <= 0xA97F) ||
                (c >= 0xD7B0 && c <= 0xD7FF))
            {
                return true;
            }
        }
        return false;
    }

    private bool ContainsJapanese(string input)
    {
        foreach (char c in input)
        {
            if ((c >= 0x3040 && c <= 0x309F) ||
                (c >= 0x30A0 && c <= 0x30FF) ||
                (c >= 0x31F0 && c <= 0x31FF))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsNotoSansFont(TMP_FontAsset fontAsset)
    {
        return fontAsset != null && (
            fontAsset.name.Contains("NotoSansKR-Regular") ||
            fontAsset.name.Contains("NotoSansJP-Regular") ||
            fontAsset.name.Contains("NotoSansSC-Regular")
        );
    }

    /// <summary>
    /// 根据难度级别查找对应的ChartInfo
    /// </summary>
    private ChartInfo FindChartInfoByDifficulty(SongInfo songInfo, int difficultyLevel)
    {
        if (songInfo == null || songInfo.charts == null) return null;

        foreach (var chart in songInfo.charts)
        {
            if (chart.difficulty == difficultyLevel)
            {
                return chart;
            }
        }
        return null;
    }
}