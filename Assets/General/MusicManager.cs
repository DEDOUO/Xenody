using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;
    private AudioSource audioSource;

    // 音乐配置：场景名 → Resources 中的音乐文件名
    public Dictionary<string, string> sceneMusicMap = new Dictionary<string, string>()
    {
        { "SongSelect", "SceneMusic/SongSelectScene" }
        // 可添加其他场景的音乐配置
    };

    // 当前播放的音乐名称
    private string currentMusicName = string.Empty;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true; // 设置音乐循环播放

            // 注册场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Debug.Log($"销毁重复的 MusicManager: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    // 场景加载完成后触发
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Debug.Log($"场景已加载: {scene.name}");

        // 如果场景有配置音乐，播放它
        if (sceneMusicMap.TryGetValue(scene.name, out string musicName))
        {
            //Debug.Log($"为场景 {scene.name} 播放音乐: {musicName}");
            PlayMusic(musicName);
        }
        else
        {
            // 如果场景没有配置音乐，停止当前音乐
            //Debug.Log($"场景 {scene.name} 没有配置音乐，停止当前播放");
            StopMusic();
        }
    }

    // 播放音乐（从 Resources 加载）
    public void PlayMusic(string musicName)
    {
        // 如果是相同的音乐且正在播放，不重复加载
        if (musicName == currentMusicName && audioSource.isPlaying)
        {
            Debug.Log($"音乐 {musicName} 已在播放，无需重复加载");
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>(musicName);
        if (clip != null)
        {
            currentMusicName = musicName;
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"成功播放音乐: {musicName}");
        }
        else
        {
            Debug.LogError($"音乐文件未找到: {musicName}");
        }
    }

    // 停止当前音乐
    public void StopMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            currentMusicName = string.Empty;
            //Debug.Log("音乐已停止");
        }
    }

    // 暂停音乐（可选）
    public void PauseMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            //Debug.Log("音乐已暂停");
        }
    }

    // 恢复音乐（可选）
    public void ResumeMusic()
    {
        if (!audioSource.isPlaying && !string.IsNullOrEmpty(currentMusicName))
        {
            audioSource.Play();
            //Debug.Log("音乐已恢复");
        }
    }

    // 设置音乐音量（可选）
    public void SetVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
        //Debug.Log($"音乐音量已设置为: {audioSource.volume}");
    }

    // 获取当前音乐状态
    public bool IsMusicPlaying()
    {
        return audioSource.isPlaying;
    }
}