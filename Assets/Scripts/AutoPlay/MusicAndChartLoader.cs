using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

public class MusicAndChartLoader : MonoBehaviour
{
    private AudioSource audioSource;
    private Chart chart;
    private string musicPath;
    private string chartPath;
    private Sprite cover;

    // 新增初始化方法
    public void Initialize(AudioSource audioSource)
    {
        this.audioSource = audioSource;
    }

    public async Task LoadMusicAndChartAsync()
    {
        
        musicPath = SongAndChartData.GetMusicFilePath();

        // 判断如果没有获取到路径（即直接加载 AutoPlay 场景时），默认按照第一首歌曲加载
        if (string.IsNullOrEmpty(musicPath))
        {
            Debug.Log("歌曲路径缺失，加载默认曲");
            SongAndChartData.SetSelectedSong("Accelerate");
            musicPath = SongAndChartData.GetMusicFilePath();
        }
        if (!File.Exists(musicPath))
        {
            Debug.LogError($"音乐文件未找到：{musicPath}");
            return;
        }

        chartPath = SongAndChartData.GetChartFilePath();

        // 检查Chart.json是否存在，若不存在尝试查找并转换Chart.xlsx
        if (!File.Exists(chartPath))
        {
            Dictionary<string, object> chartData = SongAndChartData.GetChartData();
            if (chartData == null)
            {
                Debug.LogError($"未能成功生成或读取 Chart.json 文件，路径：{chartPath}");
                return;
            }
        }

        cover = SongAndChartData.GetCoverSprite();

        // 使用 UnityWebRequest 加载音频文件
        await LoadAudioClipAsync(musicPath);

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

    private async Task LoadAudioClipAsync(string path)
    {
        // 构建 StreamingAssets 路径
        string streamingPath = Path.Combine("file://", Application.streamingAssetsPath, path);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(streamingPath, AudioType.MPEG))
        {
            var operation = www.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
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
        //Debug.Log(chart);
        return chart;
    }

    public AudioSource GetAudioSource()
    {
        return audioSource;
    }

    private void ApplyCoverToJacketImage()
    {
        // 查找 JacketImage 游戏物体
        GameObject jacketImageObject = GameObject.Find("JacketImage");

        if (jacketImageObject != null)
        {
            // 获取 Image 组件
            Image jacketImageComponent = jacketImageObject.GetComponent<Image>();

            if (jacketImageComponent != null)
            {
                // 将封面设置为 Image 组件的源图像
                jacketImageComponent.sprite = cover;
            }
            else
            {
                Debug.LogError("JacketImage 游戏物体上没有找到 Image 组件！");
            }
        }
        else
        {
            Debug.LogError("未找到 JacketImage 游戏物体！");
        }
    }
}