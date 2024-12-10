using UnityEngine;
using System.IO;
//using UnityEngine.SceneManagement;
//using System.Collections;
//using System.Collections.Generic;
//using Params;
//using Note;
//using UnityEditor;
//using static Utility;
//using static JudgePlane;

public class MusicAndChartLoader
{
    private AudioSource audioSource;
    private Chart chart;
    private string musicPath;
    private string chartPath;

    public MusicAndChartLoader(AudioSource audioSource)
    {
        this.audioSource = audioSource;
    }

    public void LoadMusicAndChart()
    {
        musicPath = SongAndChartData.GetMusicFilePath();
        chartPath = SongAndChartData.GetChartFilePath();

        // 判断如果没有获取到路径（即直接加载AutoPlay场景时），默认按照第一首歌曲加载
        if (string.IsNullOrEmpty(musicPath) || string.IsNullOrEmpty(chartPath))
        {
            SongAndChartData.SetSelectedSong("Accelerate");
            musicPath = SongAndChartData.GetMusicFilePath();
            chartPath = SongAndChartData.GetChartFilePath();
        }

        audioSource.clip = Resources.Load<AudioClip>(Path.ChangeExtension(musicPath, null));


        if (File.Exists(chartPath))
        {
            chart = Chart.ImportFromJson(chartPath);
        }
        else
        {
            Debug.LogError("谱面文件不存在！");
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
}