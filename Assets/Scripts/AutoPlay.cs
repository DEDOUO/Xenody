using UnityEngine;
using UnityEngine.SceneManagement;
//using UnityEngine.UI;
using System.IO;
using Newtonsoft.Json;
using System.Collections;
using Note; 

public class PlaySceneScript : MonoBehaviour
{
    private AudioSource audioSource;
    private Chart chart; // 用于存储加载的谱面数据

    private void Start()
    {
        LoadMusicAndChart();
        StartCoroutine(PlayMusicAndChart());
    }

    // 加载歌曲音频文件和谱面文件
    private void LoadMusicAndChart()
    {
        string musicPath = SongAndChartData.GetMusicFilePath();
        //Debug.LogError(musicPath);
        string chartPath = SongAndChartData.GetChartFilePath();

        audioSource = gameObject.GetComponent<AudioSource>();
        audioSource.clip = Resources.Load<AudioClip>(Path.ChangeExtension(musicPath, null));
        //Debug.LogError(audioSource.clip);


        if (File.Exists(chartPath))
        {
            string json = File.ReadAllText(chartPath);
            chart = JsonConvert.DeserializeObject<Chart>(json);
        }
        else
        {
            Debug.LogError("谱面文件不存在！");
        }
    }

    // 协程方法，用于播放歌曲并根据谱面数据展示谱面内容（这里暂时只打印相关信息，后续需完善展示逻辑）
    private IEnumerator PlayMusicAndChart()
    {
        audioSource.Play();
        while (audioSource.isPlaying)
        {
            // 这里可以根据谱面数据（chart变量中的内容）来展示对应的谱面元素，比如音符下落等效果
            // 目前先简单打印一些信息示例，后续要结合Unity的图形渲染等功能完善
            //foreach (var tap in chart.taps)
            //{
            //    Debug.Log($"Tap出现时间: {tap.startT}，位置: {tap.startX}");
            //}

            yield return null;
        }

        // 歌曲播放结束后，跳回选歌场景
        //SceneManager.LoadScene("SongSelect");
    }
}