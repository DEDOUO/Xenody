using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;
    private AudioSource audioSource;

    // 场景音乐配置：场景名 → Resources 中的音乐文件名（不带扩展名）
    public Dictionary<string, string> sceneMusicMap = new Dictionary<string, string>()
    {
        { "SongSelect", "SceneMusic/SongSelectScene" },
        { "SongResult", "SceneMusic/SongResultScene" }
        // 可添加其他场景的音乐配置
    };

    // 当前播放的音乐信息
    private string currentMusicName = string.Empty;
    private Coroutine fadeCoroutine;
    private bool isPreviewing = false;
    private float previewStartTime = 0f;
    private float previewEndTime = 0f;
    private string previewClipPath = string.Empty;
    private AudioClip originalClip = null;

    // 音频缓存（仅用于歌曲预览）
    private Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();

    // 淡入淡出参数
    private float fadeDuration = 0.5f;         // 常规淡入淡出时间
    private float previewVolume = 1f;          // 预览音量
    private float sceneMusicVolume = 1f;       // 场景音乐音量
    private float loopFadeDuration = 0.5f;     // 循环时的淡入淡出时间

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
            //Debug.Log($"销毁重复的 MusicManager: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    // 场景加载完成后触发
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果场景有配置音乐，播放它
        if (sceneMusicMap.TryGetValue(scene.name, out string musicName))
        {
            PlayMusic(musicName);
        }
        else
        {
            // 新增：明确停止音乐并应用淡出效果
            FadeOutAndStop();
        }
    }

    // 淡出并停止音乐（新增方法）
    private void FadeOutAndStop()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOut(() => {
            audioSource.Stop();
            currentMusicName = string.Empty;
        }));
    }

    // 播放场景音乐（从 Resources 加载）
    public void PlayMusic(string musicName)
    {
        // 如果正在预览，先停止预览
        if (isPreviewing)
        {
            StopPreview();
        }

        // 如果是相同的音乐且正在播放，不重复加载
        if (musicName == currentMusicName && audioSource.isPlaying)
        {
            Debug.Log($"音乐 {musicName} 已在播放，无需重复加载");
            return;
        }

        // 从 Resources 加载场景音乐
        AudioClip clip = Resources.Load<AudioClip>(musicName);
        if (clip != null)
        {
            PlaySceneMusic(clip, musicName);
        }
        else
        {
            Debug.LogError($"场景音乐文件未找到: {musicName} (Resources路径)");
        }
    }

    // 播放场景音乐
    private void PlaySceneMusic(AudioClip clip, string musicName)
    {
        // 停止当前淡入淡出
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 应用淡入效果
        if (audioSource.isPlaying)
        {
            fadeCoroutine = StartCoroutine(FadeOutAndPlay(clip, musicName));
        }
        else
        {
            currentMusicName = musicName;
            audioSource.clip = clip;
            audioSource.volume = 0f;
            audioSource.Play();
            fadeCoroutine = StartCoroutine(FadeIn(sceneMusicVolume));
        }
    }

    // 播放歌曲预览（从 StreamingAssets 加载）
    public void PlaySongPreview(string clipPath, float startTime, float endTime)
    {
        // 如果是同一首歌且正在预览，不重复操作
        if (isPreviewing && clipPath == previewClipPath)
        {
            return;
        }

        // 保存当前场景音乐信息
        if (!isPreviewing && audioSource.isPlaying)
        {
            originalClip = audioSource.clip;
        }

        // 停止当前预览（如果有）
        if (isPreviewing)
        {
            StopPreview();
        }

        previewClipPath = clipPath;
        previewStartTime = startTime;
        previewEndTime = endTime;
        isPreviewing = true;

        // 检查缓存
        if (audioClipCache.TryGetValue(clipPath, out AudioClip cachedClip))
        {
            PlayPreviewClip(cachedClip);
        }
        else
        {
            // 加载并播放预览
            StartCoroutine(LoadAudioClipAsync(clipPath, (clip) => {
                if (clip != null)
                {
                    // 缓存音频
                    audioClipCache[clipPath] = clip;
                    PlayPreviewClip(clip);
                }
                else
                {
                    Debug.LogError($"预览音频文件未找到: {clipPath} (StreamingAssets路径)");
                    isPreviewing = false;
                }
            }));
        }
    }

    // 播放已加载的预览片段
    private void PlayPreviewClip(AudioClip clip)
    {
        // 停止当前淡入淡出
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 应用淡入效果
        if (audioSource.isPlaying)
        {
            fadeCoroutine = StartCoroutine(FadeOutAndPlayPreview(clip));
        }
        else
        {
            audioSource.clip = clip;
            audioSource.time = previewStartTime;
            audioSource.volume = 0f;
            audioSource.loop = false; // 不使用原生循环，使用自定义循环逻辑
            audioSource.Play();
            fadeCoroutine = StartCoroutine(FadeIn(previewVolume));

            // 注册预览循环事件
            StartCoroutine(ManagePreviewLoop());
        }
    }

    // 管理预览循环
    private IEnumerator ManagePreviewLoop()
    {
        // 确保音频有效
        if (audioSource.clip == null)
        {
            Debug.LogWarning("预览音频为空，无法循环");
            yield break;
        }

        // 计算实际预览时长
        float previewDuration = previewEndTime - previewStartTime;

        // 确保预览时长有效
        if (previewDuration <= 0 || previewEndTime > audioSource.clip.length)
        {
            previewDuration = audioSource.clip.length - previewStartTime;
            Debug.Log($"调整预览时长为: {previewDuration}s");
        }

        // 预览循环逻辑
        while (isPreviewing)
        {
            // 计算距离预览结束的剩余时间
            float timeRemaining = previewEndTime - audioSource.time;

            // 如果接近预览结束，开始淡出
            if (timeRemaining <= loopFadeDuration)
            {
                // 停止当前淡入淡出
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                // 淡出
                fadeCoroutine = StartCoroutine(LoopFadeOut(() => {
                    // 重置到预览开始位置
                    audioSource.time = previewStartTime;

                    // 确保音频处于播放状态
                    audioSource.Play();

                    // 淡入
                    fadeCoroutine = StartCoroutine(LoopFadeIn(previewVolume));
                }));

                // 等待整个淡入淡出过程完成
                yield return new WaitForSeconds(loopFadeDuration * 2);
            }

            yield return null;
        }
    }

    // 循环时的淡出效果（使用loopFadeDuration）
    private IEnumerator LoopFadeOut(System.Action onComplete = null)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < loopFadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / loopFadeDuration);
            yield return null;
        }

        audioSource.volume = 0f;

        if (onComplete != null)
        {
            onComplete();
        }
    }

    // 循环时的淡入效果（使用loopFadeDuration）
    private IEnumerator LoopFadeIn(float targetVolume)
    {
        // 确保从预览开始时间播放
        audioSource.time = previewStartTime;

        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < loopFadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / loopFadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    // 预加载音频（从 StreamingAssets 加载）
    public void PreloadAudio(string clipPath)
    {
        // 检查缓存
        if (audioClipCache.ContainsKey(clipPath))
        {
            return;
        }

        // 开始异步加载
        StartCoroutine(LoadAudioClipAsync(clipPath, (clip) => {
            if (clip != null)
            {
                // 缓存音频
                audioClipCache[clipPath] = clip;
                Debug.Log($"成功预加载: {clipPath}");
            }
            else
            {
                Debug.LogError($"预加载失败: {clipPath}");
            }
        }));
    }

    // 使用 UnityWebRequest 异步加载音频（从 StreamingAssets）
    private IEnumerator LoadAudioClipAsync(string path, System.Action<AudioClip> callback)
    {
        // 构建 StreamingAssets 路径
        string streamingPath = Path.Combine("file://", Application.streamingAssetsPath, path);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(streamingPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"加载音频失败：{www.error}，路径: {streamingPath}");
                callback(null);
            }
            else
            {
                callback(DownloadHandlerAudioClip.GetContent(www));
            }
        }
    }

    // 停止预览
    public void StopPreview()
    {
        if (!isPreviewing) return;

        isPreviewing = false;

        // 停止当前淡入淡出
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 应用淡出效果
        fadeCoroutine = StartCoroutine(FadeOut(() => {
            // 如果有原始场景音乐，恢复播放
            if (originalClip != null)
            {
                audioSource.clip = originalClip;
                audioSource.loop = true;
                audioSource.volume = 0f;
                audioSource.Play();
                fadeCoroutine = StartCoroutine(FadeIn(sceneMusicVolume));
            }

            originalClip = null;
        }));
    }

    // 淡入效果（常规）
    private IEnumerator FadeIn(float targetVolume)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    // 淡出效果（常规）
    private IEnumerator FadeOut(System.Action onComplete = null)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }

        audioSource.volume = 0f;

        if (onComplete != null)
        {
            onComplete();
        }
    }

    // 淡出并播放新音乐
    private IEnumerator FadeOutAndPlay(AudioClip newClip, string newMusicName)
    {
        yield return StartCoroutine(FadeOut());

        currentMusicName = newMusicName;
        audioSource.clip = newClip;
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.Play();

        fadeCoroutine = StartCoroutine(FadeIn(sceneMusicVolume));
    }

    // 淡出并播放预览
    private IEnumerator FadeOutAndPlayPreview(AudioClip previewClip)
    {
        yield return StartCoroutine(FadeOut());

        audioSource.clip = previewClip;
        audioSource.time = previewStartTime;
        audioSource.loop = false;
        audioSource.volume = 0f;
        audioSource.Play();

        fadeCoroutine = StartCoroutine(FadeIn(previewVolume));

        // 注册预览循环事件
        StartCoroutine(ManagePreviewLoop());
    }

    // 停止当前音乐
    public void StopMusic()
    {
        if (isPreviewing)
        {
            StopPreview();
        }
        else if (audioSource.isPlaying)
        {
            // 应用淡出效果
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeOut(() => {
                currentMusicName = string.Empty;
            }));
        }
    }

    // 清空音频缓存
    public void ClearAudioCache()
    {
        foreach (var clip in audioClipCache.Values)
        {
            if (clip != null)
            {
                Destroy(clip);
            }
        }
        audioClipCache.Clear();
    }
}