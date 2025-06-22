using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Params;
using TMPro;

public class MusicAndChartLoader : MonoBehaviour
{
    private AudioSource audioSource;
    private Chart chart;
    private string musicPath;
    private string chartPath;
    private Sprite cover;
    private static int selectedDifficulty = 1; // 新增：当前选择的难度

    // 新增初始化方法
    public void Initialize(AudioSource audioSource)
    {
        this.audioSource = audioSource;
    }

    // 修改：返回 IEnumerator 而非 Task
    public IEnumerator LoadMusicAndChartAsync()
    {
        selectedDifficulty = SongAndChartData.selectedDifficulty;
        musicPath = SongAndChartData.GetMusicFilePath();

        // 判断如果没有获取到路径（即直接加载 AutoPlay 场景时），默认按照第一首歌曲加载
        if (string.IsNullOrEmpty(musicPath))
        {
            Debug.Log("歌曲路径缺失，加载默认曲");
            SongAndChartData.SetSelectedSong("Accelerate", "1-Accelerate", 4);
            musicPath = SongAndChartData.GetMusicFilePath();
        }
        if (!File.Exists(musicPath))
        {
            Debug.LogError($"音乐文件未找到：{musicPath}");
            yield break;
        }

        chartPath = SongAndChartData.GetChartFilePath();

        // 检查Chart.xlsx是否存在，若存在尝试将其转换为Chart.json（如果已有Chart.json，就覆盖原来的）
        string excelPath = SongAndChartData.GetExcelFilePath();
        if (File.Exists(excelPath))
        {
            SongAndChartData.ConvertExcelToJson();
        }
        else
        {
            // 若Chart.xlsx不存在，检查Chart.json是否存在
            if (!File.Exists(chartPath))
            {
                Debug.LogError("谱面文件不存在！");
                yield break;
            }
        }

        cover = SongAndChartData.GetCoverSprite();

        // 使用 UnityWebRequest 加载音频文件
        yield return StartCoroutine(LoadAudioClipAsync(musicPath));

        if (File.Exists(chartPath))
        {
            chart = Chart.ImportFromJson(chartPath);
            if (chart == null)
            {
                Debug.LogError("谱面文件解析失败！");
            }
        }
        else
        {
            Debug.LogError("谱面文件不存在！");
        }

        // 应用封面到 JacketImage 游戏物体的 Image 组件
        ApplyCoverToJacketImage();
    }

    // 修改：返回 IEnumerator
    private IEnumerator LoadAudioClipAsync(string path)
    {
        // 构建 StreamingAssets 路径
        string streamingPath = Path.Combine("file://", Application.streamingAssetsPath, path);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(streamingPath, AudioType.MPEG))
        {
            var operation = www.SendWebRequest();
            while (!operation.isDone)
            {
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"加载音频失败：{www.error}");
            }
            else
            {
                if (audioSource != null)
                {
                    audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                }
                else
                {
                    Debug.LogError("audioSource 为 null，无法设置音频剪辑！");
                }
            }
        }
    }

    public Chart GetChart()
    {
        return chart;
    }

    public AudioSource GetAudioSource()
    {
        return audioSource;
    }

    private void ApplyCoverToJacketImage()
    {
        // 1. 设置封面图（保持不变）
        GameObject jacketImageObject = GameObject.Find("JacketCover");
        if (jacketImageObject != null)
        {
            Image jacketImageComponent = jacketImageObject.GetComponent<Image>();
            if (jacketImageComponent != null)
            {
                jacketImageComponent.sprite = cover;
            }
            else
            {
                Debug.LogError("JacketCover 游戏物体上没有找到 Image 组件！");
            }
        }
        else
        {
            Debug.LogError("未找到 JacketCover 游戏物体！");
        }

        // 2. 设置难度背景图颜色（保持不变）
        GameObject diffImageObject = GameObject.Find("DiffImage");
        if (diffImageObject != null)
        {
            Image diffImageComponent = diffImageObject.GetComponent<Image>();
            if (diffImageComponent != null)
            {
                if (ChartParams.difficultyColorMap.TryGetValue(selectedDifficulty, out Color difficultyColor))
                {
                    diffImageComponent.color = difficultyColor;
                }
                else
                {
                    Debug.LogError($"未找到难度 {selectedDifficulty} 对应的颜色！");
                    diffImageComponent.color = Color.white;
                }
            }
            else
            {
                Debug.LogError("DiffImage 游戏物体上没有找到 Image 组件！");
            }
        }
        else
        {
            Debug.LogError("未找到 DiffImage 游戏物体！");
        }

        // 3. 设置难度文本（修改后：从DifficultyText游戏物体获取文本）
        GameObject difficultyTextObject = GameObject.Find("DifficultyText");
        if (difficultyTextObject != null)
        {
            TextMeshProUGUI difficultyTextComponent = difficultyTextObject.GetComponent<TextMeshProUGUI>();
            if (difficultyTextComponent != null)
            {
                GameObject diffTextObject = GameObject.Find("DiffText");
                if (diffTextObject != null)
                {
                    TextMeshProUGUI diffTextComponent = diffTextObject.GetComponent<TextMeshProUGUI>();
                    if (diffTextComponent != null)
                    {
                        // 将DifficultyText的文本复制到DiffText
                        diffTextComponent.text = difficultyTextComponent.text;
                    }
                    else
                    {
                        Debug.LogError("DiffText 游戏物体上没有找到 TextMeshProUGUI 组件！");
                    }
                }
                else
                {
                    Debug.LogError("未找到 DiffText 游戏物体！");
                }
            }
            else
            {
                Debug.LogError("DifficultyText 游戏物体上没有找到 TextMeshProUGUI 组件！");
            }
        }
        else
        {
            Debug.LogError("未找到 DifficultyText 游戏物体！");
        }
    }
}