using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class MusicAndChartLoader
{
    private AudioSource audioSource;
    private Chart chart;
    private string musicPath;
    private string chartPath;
    private Sprite cover;


    public MusicAndChartLoader(AudioSource audioSource)
    {
        this.audioSource = audioSource;
    }


    public void LoadMusicAndChart()
    {
        // 判断如果没有获取到路径（即直接加载 AutoPlay 场景时），默认按照第一首歌曲加载
        if (string.IsNullOrEmpty(musicPath) || string.IsNullOrEmpty(chartPath))
        {
            SongAndChartData.SetSelectedSong("Accelerate");
        }


        musicPath = SongAndChartData.GetMusicFilePath();
        chartPath = SongAndChartData.GetChartFilePath();
        cover = SongAndChartData.GetCoverSprite();

        audioSource.clip = Resources.Load<AudioClip>(Path.ChangeExtension(musicPath, null));

        if (File.Exists(chartPath))
        {
            chart = Chart.ImportFromJson(chartPath);
        }
        else
        {
            Debug.LogError("谱面文件不存在！");
        }

        // 应用封面到 JacketImage 游戏物体的 Image 组件
        ApplyCoverToJacketImage();
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