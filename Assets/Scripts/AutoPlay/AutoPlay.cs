using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class AutoPlay : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioSource[] audioSources;
    public AudioSource TapSoundEffect;
    public AudioSource SlideSoundEffect;
    public AudioSource FlickSoundEffect;
    //public AudioSource HoldSoundEffect;
    public AudioSource StarHeadSoundEffect;
    public AudioSource StarSoundEffect;

    public GameObject AutoPlayService; // 用于挂载 MusicAndChartLoader 组件的游戏对象
    public GameObject JudgePlanesParent;
    public GameObject JudgeLinesParent;
    public GameObject ColorLinesParent;
    public GameObject TapsParent;
    //public GameObject SlidesParent;
    //public GameObject FlicksParent;
    public GameObject ArrowsParent;
    public GameObject HoldsParent;
    public GameObject HoldOutlinesParent;
    public GameObject StarsParent;
    public GameObject subStarsParent;
    public GameObject JudgeTexturesParent;
    public GameObject MultiHitLinesParent;
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private TMP_Text ComboText;
    [SerializeField] private TMP_Text ScoreText;
    public TMP_Text readyText;

    public Sprite JudgePlaneSprite;
    //public Sprite HoldSprite;
    public GlobalRenderOrderManager renderOrderManager;
    public GameObject AnimatorContainer;

    private MusicAndChartPlayer player;
    private ChartInstantiator instantiator;
    private ScoreManager scoreManager;
    private MusicAndChartLoader loader;
    private Chart chart; // 存储加载的谱面数据

    private void Awake()
    {
        // 查找场景内游戏物体
        AutoPlayService = GameObject.Find("AutoPlayService");
        JudgePlanesParent = GameObject.Find("JudgePlanesParent");
        JudgeLinesParent = GameObject.Find("JudgeLinesParent");
        ColorLinesParent = GameObject.Find("ColorLinesParent");
        TapsParent = GameObject.Find("TapsParent");
        //SlidesParent = GameObject.Find("SlidesParent");
        //FlicksParent = GameObject.Find("FlicksParent");
        ArrowsParent = GameObject.Find("ArrowsParent");
        HoldsParent = GameObject.Find("HoldsParent");
        HoldOutlinesParent = GameObject.Find("HoldOutlinesParent");
        StarsParent = GameObject.Find("StarsParent");
        subStarsParent = GameObject.Find("SubStarsParent");
        JudgeTexturesParent = GameObject.Find("JudgeTexturesParent");
        MultiHitLinesParent = GameObject.Find("MultiHitLinesParent");

        fpsText = GameObject.Find("FPS").GetComponent<TextMeshProUGUI>();
        ComboText = GameObject.Find("ComboText").GetComponent<TextMeshProUGUI>();
        ScoreText = GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        readyText = GameObject.Find("ReadyText").GetComponent<TextMeshProUGUI>();

        // 加载Sprite
        JudgePlaneSprite = Resources.Load<Sprite>("Sprites/TrackBlack2");
        if (JudgePlaneSprite == null)
        {
            Debug.LogError("Failed to load sprite: Sprites/TrackBlack2");
        }
        //HoldSprite = Resources.Load<Sprite>("Sprites/HoldBlueSprite2");
        //if (HoldSprite == null)
        //{
        //    Debug.LogError("Failed to load sprite: Sprites/HoldBlueSprite2");
        //}

        // 查找包含AudioSource的GameObject
        GameObject audioObj = GameObject.Find("AudioService");
        if (audioObj != null)
        {
            audioSource = audioObj.GetComponent<AudioSource>();
        }

        // 获取音效源
        GameObject SoundObj = GameObject.Find("HitsoundService");
        if (SoundObj != null)
        {
            audioSources = SoundObj.GetComponents<AudioSource>();
            TapSoundEffect = audioSources[0];
            SlideSoundEffect = audioSources[1];
            FlickSoundEffect = audioSources[2];
            //Hold与Tap音效一致
            //HoldSoundEffect = audioSources[0];

            StarHeadSoundEffect = audioSources[3];
            StarSoundEffect = audioSources[4];
        }

        // 查找包含GlobalRenderOrderManager的GameObject
        GameObject renderObj = GameObject.Find("GlobalRenderOrderManager");
        if (renderObj != null)
        {
            renderOrderManager = renderObj.GetComponent<GlobalRenderOrderManager>();
        }

        // 查找Animator Container
        AnimatorContainer = GameObject.Find("AnimatorContainer");

        // 获取MusicAndChartPlayer组件
        player = GetComponent<MusicAndChartPlayer>();
        if (player == null)
        {
            Debug.LogError("未找到 MusicAndChartPlayer 组件，请确保该组件已挂载到当前对象！");
        }
    }

    private void Start()
    {
        // 启动协程处理初始化流程
        StartCoroutine(InitializeAutoPlay());
    }

    // 初始化AutoPlay，完成后通知SceneTransitionManager开始真正的场景切换
    private IEnumerator InitializeAutoPlay()
    {
        // 跟据屏幕长宽比确认谱面范围
        AspectRatioManager aspectRatioManager = GetComponent<AspectRatioManager>();
        aspectRatioManager.SetupSpectrumBounds();

        // 加载音乐和谱面文件
        loader = GetComponent<MusicAndChartLoader>();
        loader.Initialize(audioSource);

        // 加载音乐和谱面
        yield return loader.LoadMusicAndChartAsync();

        // 获取加载后的谱面
        chart = loader.GetChart();

        // 实例化谱面内容
        instantiator = GetComponent<ChartInstantiator>();
        instantiator.SetParameters(JudgePlanesParent, JudgeLinesParent, ColorLinesParent, TapsParent, ArrowsParent, HoldsParent, HoldOutlinesParent, StarsParent, subStarsParent, JudgeTexturesParent, MultiHitLinesParent,
            JudgePlaneSprite, renderOrderManager, AnimatorContainer, fpsText);
        instantiator.InstantiateAll(chart);

        // 计算连击和得分列表
        scoreManager = ScoreManager.instance;
        if (scoreManager == null)
        {
            Debug.LogError("未找到ScoreManager单例实例！");
        }
        scoreManager.CalculateAutoPlayScores(chart);

        // 先禁用MusicAndChartPlayer组件
        if (player != null)
        {
            player.enabled = false;

            // 设置参数
            player.SetParameters(audioSource, JudgePlanesParent, JudgeLinesParent, ColorLinesParent, TapsParent, ArrowsParent, HoldsParent, HoldOutlinesParent, StarsParent, subStarsParent, JudgeTexturesParent, MultiHitLinesParent,
                TapSoundEffect, SlideSoundEffect, FlickSoundEffect, StarHeadSoundEffect, StarSoundEffect, chart, fpsText, ComboText, ScoreText);
            player.SetParameters2(instantiator.startTimeToInstanceNames, instantiator.InstanceNamesToGameObject, instantiator.IfSlideDict, instantiator.IfFlickDict, instantiator.holdTimes, instantiator.keyReachedJudgment,
                instantiator.JudgePlanesStartT, instantiator.JudgePlanesEndT, instantiator.subStarInfoDict, instantiator.starTrackTimes, instantiator.GradientColorList, instantiator.ChartStartTime);
            player.SetParameters3(scoreManager.SumComboMap, scoreManager.SumScoreMap, scoreManager.JudgePosMap);

            // 启用组件（但不播放）
            player.enabled = true;
            print("谱面初始化完成！");
        }

        // 通知SceneTransitionManager可以开始真正的场景切换（开门动画）
        if (SceneTransitionManagerDoor.instance != null)
        {
            SceneTransitionManagerDoor.instance.StartRealTransition();
        }
        else
        {
            Debug.LogError("未找到SceneTransitionManager实例，无法启动场景切换！");
        }

        // 等待场景切换完成
        yield return StartCoroutine(WaitForSceneTransitionComplete());

        // 显示Ready提示文字（新增逻辑）
        yield return StartCoroutine(ShowReadyTextEffect());

        // 场景切换完成后，开始播放音乐和谱面
        if (player != null && chart != null)
        {
            player.PlayMusicAndChart(chart);
            //Debug.Log("场景切换完成，开始播放音乐和谱面");
        }
    }

    private IEnumerator ShowReadyTextEffect()
    {
        if (readyText == null)
        {
            readyText = GameObject.Find("ReadyText")?.GetComponent<TMP_Text>();
            if (readyText == null) yield break;
        }

        // 初始状态
        readyText.alpha = 0f;
        readyText.transform.localScale = Vector3.one * 0.8f;

        yield return new WaitForSeconds(0.5f); // 静止0.5秒
        // 第一次闪烁：淡入+放大
        yield return StartCoroutine(AnimateAlphaAndScale(0f, 1f, 0.8f, 1f, 0.3f));
        yield return new WaitForSeconds(0.5f); // 静止0.3秒

        // 第二次闪烁：透明度波动+缩放波动
        yield return StartCoroutine(AnimateAlphaAndScale(1f, 0.5f, 1f, 0.8f, 0.3f));
        yield return StartCoroutine(AnimateAlphaAndScale(0.5f, 1f, 0.8f, 1f, 0.3f));
        yield return new WaitForSeconds(0.5f); // 静止0.3秒

        // 淡出+放大消失
        yield return StartCoroutine(AnimateAlphaAndScale(1f, 0f, 1f, 1.2f, 0.6f));

        // 最终隐藏
        readyText.alpha = 0f;
    }

    // 同时控制透明度和缩放的协程
    IEnumerator AnimateAlphaAndScale(float startAlpha, float endAlpha, float startScale, float endScale, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            float progress = timer / duration;
            readyText.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            readyText.transform.localScale = Vector3.Lerp(
                Vector3.one * startScale,
                Vector3.one * endScale,
                progress
            );
            timer += Time.deltaTime;
            yield return null;
        }

        // 确保最终状态精确
        readyText.alpha = endAlpha;
        readyText.transform.localScale = Vector3.one * endScale;
    }

    // 等待场景切换完成
    private IEnumerator WaitForSceneTransitionComplete()
    {
        // 检查SceneTransitionManager是否存在
        SceneTransitionManagerDoor transitionManager = SceneTransitionManagerDoor.instance;
        if (transitionManager == null)
        {
            Debug.LogWarning("未找到SceneTransitionManager，无法等待场景切换完成");
            yield break;
        }

        // 等待场景切换完成
        while (transitionManager.isTransitioning)
        {
            yield return null;
        }

        //Debug.Log("场景切换完成，准备播放音乐");
    }
}