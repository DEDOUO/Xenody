using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using Newtonsoft.Json;
using static Utility;
using Unity.VisualScripting;
using System.Collections;

public class SongSelect : MonoBehaviour
{

    private float horizontalOffset = 280f;
    private float verticalOffset = 110f;
    private float spacing = 15f;
    private float buttonWidth = 400f;
    private float buttonHeight = 60f;
    private float difficultyObjectOffset = 50f; // 难度物体水平偏移量（按钮右侧）
    private float difficultyObjectWidth = 100f; 
    private float difficultyObjectHeight = 50f; 

    public ScrollRect songListScrollView;
    private List<string> songList = new List<string>();
    private List<SongInfo> allSongInfo = new List<SongInfo>();
    private List<GameObject> songButtons = new List<GameObject>();
    private List<GameObject> songDifficultyObjects = new List<GameObject>(); // 新增：存储歌曲难度物体的引用

    private int currentDifficulty = 1;
    public TextMeshProUGUI difficultyLabel;
    public Button difficultySwitchL;
    public Button difficultySwitchR;
    public Image difficultyLabelImage;
    public Image difficultySwitchLImage;
    public Image difficultySwitchRImage;

    public AudioClip difficultyClickSound;
    public AudioClip songButtonClickSound;
    public AudioSource audioSource;
    //public AudioSource SongSelected;

    // 新增：歌曲难度预制体引用
    //public GameObject songDifficultyPrefab; // 拖拽赋值 SongDifficulty 预制体

    private Dictionary<int, string> difficultyTextMap = new Dictionary<int, string>()
    {
        { 1, "Initialize" },
        { 2, "Operation" },
        { 3, "Overburn" },
        { 4, "Meltdown" }
    };

    private Dictionary<int, Color> difficultyColorMap = new Dictionary<int, Color>()
    {
        { 1, new Color(98f / 255f, 190f / 255f, 119f / 255f) },
        { 2, new Color(190f / 255f, 187f / 255f, 98f / 255f) },
        { 3, new Color(190f / 255f, 116f / 255f, 98f / 255f) },
        { 4, new Color(153f / 255f, 0f / 255f, 181f / 255f) }
    };


    private void Start()
    {
        PopulateSongList();
        UpdateDifficultyUI();

        difficultySwitchL.onClick.AddListener(() => { DecreaseDifficulty(); PlayDifficultySound(); });
        difficultySwitchR.onClick.AddListener(() => { IncreaseDifficulty(); PlayDifficultySound(); });
    }

    private void PopulateSongList()
    {
        Debug.Log($"获取歌曲列表...");


        string songsFolderPath = Path.Combine(Application.streamingAssetsPath, "Songs");

        if (Directory.Exists(songsFolderPath))
        {
            string[] songFolderNames = Directory.GetDirectories(songsFolderPath);
            foreach (string songFolder in songFolderNames)
            {
                string folderName = Path.GetFileName(songFolder);
                string infoJsonPath = Path.Combine(songFolder, "info.json");

                if (File.Exists(infoJsonPath))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(infoJsonPath);
                        SongInfo songInfo = JsonConvert.DeserializeObject<SongInfo>(jsonContent);

                        if (songInfo != null && songInfo.song != null && songInfo.song.id > 0)
                        {
                            songInfo.folderName = folderName;
                            allSongInfo.Add(songInfo);
                        }
                        else
                        {
                            Debug.LogWarning($"无效的歌曲信息: {songFolder}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"解析JSON文件出错: {infoJsonPath}\n{e.Message}");
                    }
                }
            }

            allSongInfo.Sort((x, y) => x.song.id.CompareTo(y.song.id));

            GameObject buttonPrefab = Resources.Load<GameObject>("Prefabs/Editor/Buttons/SF Button My");
            if (buttonPrefab == null)
            {
                Debug.LogError("未找到按钮预制体: SF Button My");
                return;
            }

            //GameObject songDifficultyPrefab = Resources.Load<GameObject>("Prefabs/Editor/Buttons/SongDifficulty");
            //// 确保预制体存在
            //if (songDifficultyPrefab == null)
            //{
            //    Debug.LogError("未找到歌曲难度预制体: SongDifficulty");
            //    return;
            //}

            songList.Clear();
            songButtons.Clear();
            songDifficultyObjects.Clear(); // 清空难度物体列表

            foreach (var songInfo in allSongInfo)
            {
                string songName = songInfo.song.title;
                songList.Add(songName);
                SongAndChartData.AddSongInfo(songName, songInfo);

                // 创建歌曲按钮
                GameObject buttonObj = Instantiate(buttonPrefab, songListScrollView.content);
                buttonObj.name = $"Song{songList.Count}";
                buttonObj.transform.localScale = Vector3.one;

                Button button = buttonObj.GetComponent<Button>();
                if (button == null)
                {
                    Debug.LogError($"预制体不包含Button组件: {buttonObj.name}");
                    continue;
                }

                Image buttonImage = buttonObj.GetComponentInChildren<Image>();
                if (buttonImage == null)
                {
                    Debug.LogError($"预制体中未找到Image组件: {buttonObj.name}");
                    continue;
                }

                // 设置初始颜色
                if (difficultyColorMap.TryGetValue(currentDifficulty, out Color buttonColor))
                {
                    buttonImage.color = buttonColor;
                }
                else
                {
                    buttonImage.color = Color.white;
                }

                TextMeshProUGUI textComponent = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent == null)
                {
                    Debug.LogError($"预制体中未找到TextMeshProUGUI组件: {buttonObj.name}");
                    continue;
                }

                textComponent.text = songName;
                textComponent.alignment = TextAlignmentOptions.Center;

                TMP_FontAsset fontAsset = GetFontBasedOnCharacters(songName);
                textComponent.font = fontAsset;

                if (IsNotoSansFont(fontAsset))
                {
                    textComponent.fontSize = 35;
                }
                else
                {
                    textComponent.fontSize = 50;
                }

                textComponent.color = Color.white;

                button.onClick.AddListener(() =>
                {
                    // 查找当前歌曲对应的ChartInfo，确认难度是否存在
                    ChartInfo chartInfo = FindChartInfoByDifficulty(songInfo, currentDifficulty);

                    if (chartInfo != null)
                    {
                        SongAndChartData.SetSelectedSong(songName, songInfo.folderName, currentDifficulty);
                        PlaySongButtonSound();
                        // 难度存在，调用带难度参数的SetSelectedSong
                        //SceneManager.LoadScene("AutoPlay");
                    }
                    else
                    {
                        // 难度不存在，显示提示
                        Debug.Log($"歌曲 {songName} 不存在难度 {currentDifficulty} 的谱面");
                        ShowToast("谱面不存在！"); // 显示提示（需自行实现该方法）
                    }
                    
                });

                RectTransform buttonRectTransform = buttonObj.GetComponent<RectTransform>();
                buttonRectTransform.anchorMin = new Vector2(0f, 1f);
                buttonRectTransform.anchorMax = new Vector2(0f, 1f);
                buttonRectTransform.pivot = new Vector2(0.5f, 0.5f);
                buttonRectTransform.anchoredPosition3D = new Vector3(horizontalOffset, -verticalOffset, 0f);
                buttonRectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);

                verticalOffset += buttonHeight + spacing;
                songButtons.Add(buttonObj);

                // 创建歌曲难度物体（右侧），传入songInfo
                CreateSongDifficultyObject(buttonRectTransform, buttonColor, songInfo);
            }
        }
        else
        {
            Debug.LogError("Songs文件夹不存在，请检查路径是否正确！");
        }
    }
    // 在SongSelect类中添加或修改此方法
    private void ShowToast(string message)
    {
        // 查找场景中的ErrorText物体
        //GameObject errorTextObj = GameObject.Find("ErrorText");
        GameObject Parent = GameObject.Find("Panel");
        GameObject errorTextObj = Parent.transform.Find("ErrorText").GameObject();

        if (errorTextObj != null)
        {
            // 获取TextMeshProUGUI组件
            TextMeshProUGUI errorText = errorTextObj.GetComponentInChildren<TextMeshProUGUI>();

            if (errorText != null)
            {
                // 设置文本内容和颜色
                errorText.text = message;
                errorText.color = Color.white;

                // 确保物体是激活状态
                errorTextObj.SetActive(true);

                // 启动动画协程
                StartCoroutine(AnimateAndDeactivate(errorTextObj));
            }
            else
            {
                Debug.LogError("ErrorText物体不包含TextMeshProUGUI组件");
            }
        }
        else
        {
            Debug.LogError("未找到ErrorText物体");
            //Debug.Log(message); // 作为备用，在控制台输出消息
        }
    }

    // 在CreateSongDifficultyObject方法中增加了SongInfo参数
    private void CreateSongDifficultyObject(RectTransform buttonRect, Color buttonColor, SongInfo songInfo)
    {
        GameObject songDifficultyPrefab = Resources.Load<GameObject>("Prefabs/Editor/Buttons/SongDifficulty");
        // 确保预制体存在
        if (songDifficultyPrefab == null)
        {
            Debug.LogError("未找到歌曲难度预制体: SongDifficulty");
            return;
        }

        GameObject difficultyObj = Instantiate(songDifficultyPrefab, songListScrollView.content);
        difficultyObj.name = "SongDifficulty_" + songButtons.Count;
        difficultyObj.transform.localScale = Vector3.one;

        RectTransform difficultyRect = difficultyObj.GetComponent<RectTransform>();
        difficultyRect.anchorMin = new Vector2(0f, 1f);
        difficultyRect.anchorMax = new Vector2(0f, 1f);
        difficultyRect.pivot = new Vector2(0.5f, 0.5f);
        difficultyRect.anchoredPosition3D = new Vector3(
            buttonRect.anchoredPosition3D.x + buttonRect.sizeDelta.x / 2 + difficultyObjectOffset,
            buttonRect.anchoredPosition3D.y, 0f);
        difficultyRect.sizeDelta = new Vector2(difficultyObjectWidth, difficultyObjectHeight);

        Image difficultyImage = difficultyObj.GetComponentInChildren<Image>();
        TextMeshProUGUI difficultyText = difficultyObj.GetComponentInChildren<TextMeshProUGUI>();

        // 查找对应难度的ChartInfo
        ChartInfo chartInfo = FindChartInfoByDifficulty(songInfo, currentDifficulty);

        if (chartInfo != null)
        {
            // 找到对应难度，显示等级并使用正常颜色
            if (difficultyImage != null) difficultyImage.color = buttonColor;
            if (difficultyText != null) difficultyText.text = $"Lv {chartInfo.level}";
        }
        else
        {
            // 未找到对应难度，显示null并使用灰色
            if (difficultyImage != null) difficultyImage.color = Color.gray;
            if (difficultyText != null) difficultyText.text = "null";
        }

        songDifficultyObjects.Add(difficultyObj);
    }

    // 新增：根据难度级别查找对应的ChartInfo
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

    private void UpdateDifficultyUI()
    {
        if (difficultyTextMap.TryGetValue(currentDifficulty, out string difficultyText))
        {
            difficultyLabel.text = difficultyText;
        }
        else
        {
            difficultyLabel.text = "Unknown";
        }

        SetDifficultyButtonColors();
        UpdateSongButtonColors();
    }

    private void SetDifficultyButtonColors()
    {
        if (difficultyColorMap.TryGetValue(currentDifficulty, out Color targetColor))
        {
            difficultyLabelImage.color = targetColor;
            difficultySwitchLImage.color = targetColor;
            difficultySwitchRImage.color = targetColor;
        }
        else
        {
            difficultyLabelImage.color = Color.blue;
            difficultySwitchLImage.color = Color.blue;
            difficultySwitchRImage.color = Color.blue;
        }
    }


    // 修改UpdateSongButtonColors方法，增加ChartInfo检查
    private void UpdateSongButtonColors()
    {
        if (difficultyColorMap.TryGetValue(currentDifficulty, out Color buttonColor))
        {
            for (int i = 0; i < songButtons.Count && i < songDifficultyObjects.Count && i < allSongInfo.Count; i++)
            {
                Image buttonImage = songButtons[i].GetComponentInChildren<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = buttonColor;
                }

                Image difficultyImage = songDifficultyObjects[i].GetComponentInChildren<Image>();
                TextMeshProUGUI difficultyText = songDifficultyObjects[i].GetComponentInChildren<TextMeshProUGUI>();

                ChartInfo chartInfo = FindChartInfoByDifficulty(allSongInfo[i], currentDifficulty);
                if (chartInfo != null)
                {
                    if (difficultyImage != null) difficultyImage.color = buttonColor;
                    if (difficultyText != null) difficultyText.text = $"Lv {chartInfo.level}";
                }
                else
                {
                    if (difficultyImage != null) difficultyImage.color = Color.gray;
                    if (difficultyText != null) difficultyText.text = "null";
                }
            }
        }
    }

    private void DecreaseDifficulty()
    {
        if (currentDifficulty > 1)
        {
            currentDifficulty--;
            UpdateDifficultyUI();
        }
    }

    private void IncreaseDifficulty()
    {
        if (currentDifficulty < 4)
        {
            currentDifficulty++;
            UpdateDifficultyUI();
        }
    }

    private void PlayDifficultySound()
    {
        if (audioSource && difficultyClickSound)
        {
            audioSource.PlayOneShot(difficultyClickSound);
        }
    }

    //private void PlaySongButtonSound()
    //{
    //    //Debug.Log("PlaySongButtonSound");
    //    if (audioSource && songButtonClickSound)
    //    {
    //        //Debug.Log("PlaySongButtonSound");
    //        audioSource.PlayOneShot(songButtonClickSound);
    //    }
    //}

    private void PlaySongButtonSound()
    {
        if (audioSource && songButtonClickSound)
        {
            audioSource.clip = songButtonClickSound;
            audioSource.Play();

            // 启动协程等待音效播放完成
            StartCoroutine(WaitForSoundAndLoadScene());
        }
        else
        {
            // 没有音效，直接加载场景
            SceneManager.LoadScene("AutoPlay");
        }
    }

    private IEnumerator WaitForSoundAndLoadScene()
    {
        // 等待音效播放完成
        yield return new WaitForSeconds(songButtonClickSound.length);

        // 销毁临时音频源
        if (audioSource != null)
        {
            Destroy(audioSource);
        }

        // 加载场景
        SceneManager.LoadScene("AutoPlay");
    }

    // 字体相关方法保持不变...
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
}