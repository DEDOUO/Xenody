using UnityEngine;
//using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Params;
using System.Collections;
using UnityEngine.SceneManagement;
using Note;
using static Utility;
using TMPro;
using static GradientColorListUnity;
using static UnityEditor.Experimental.GraphView.GraphView;
using UnityEngine.UIElements;
//using Unity.VisualScripting;
//using DocumentFormat.OpenXml.Wordprocessing;
//using DocumentFormat.OpenXml.Spreadsheet;


public class MusicAndChartPlayer : MonoBehaviour
{
    public PauseManager pauseManager; // 添加对 PauseManager 实例的引用

    public AudioSource audioSource;
    private GameObject JudgePlanesParent;
    private GameObject ColorLinesParent;
    private GameObject JudgeLinesParent;
    private GameObject TapsParent;
    private GameObject SlidesParent;
    private GameObject FlicksParent;
    private GameObject FlickArrowsParent;
    private GameObject HoldsParent;
    private GameObject HoldOutlinesParent;
    private GameObject StarsParent;
    public GameObject SubStarsParent;
    public GameObject JudgeTexturesParent;
    private GameObject MultiHitLinesParent;
    public GameObject MusicSlider;
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private TMP_Text ComboText;
    [SerializeField] private TMP_Text ScoreText;
    
    private Sprite TapSprite;
    private Sprite SlideSprite;
    private Sprite FlickSprite;
    private Sprite StarHeadSprite;
    private Sprite Sync;
    private Sprite Link;
    private Sprite Fuzz;
    private Sprite Null;

    private AudioSource TapSoundEffect;
    private AudioSource SlideSoundEffect;
    private AudioSource FlickSoundEffect;
    private AudioSource HoldSoundEffect;
    private AudioSource StarHeadSoundEffect;
    private AudioSource StarSoundEffect;

    private float audioPrevTime = 0f;
    private Dictionary<float, List<string>> startTimeToInstanceNames = new Dictionary<float, List<string>>(); // 存储startT到对应实例名列表的映射
    private Dictionary<string, List<float>> holdTimes = new Dictionary<string, List<float>>(); // 存储Hold实例名到其开始/结束时间的映射
    private Dictionary<string, KeyInfo> keyReachedJudgment = new Dictionary<string, KeyInfo>(); // 存储实例名到键信息的映射
    private List<float> JudgePlanesStartT = new List<float>(); // 判定面的开始时间（用于JudgeLine出现时间计算）
    private List<float> JudgePlanesEndT = new List<float>(); // 判定面的结束时间（用于JudgeLine结束时间计算）
    //private Dictionary<string, float> judgePlaneEndTimes = new Dictionary<string, float>();
    public Dictionary<(int, int), SubStarInfo> subStarInfoDict = new Dictionary<(int, int), SubStarInfo>();
    // 新增一个字典，用于存储每个星星的划动开始和结束时间
    private Dictionary<string, (float startT, float endT)> starTrackTimes = new Dictionary<string, (float startT, float endT)>();
    private GradientColorListUnity GradientColorList;
    private Dictionary<float, int> comboMap;
    private Dictionary<float, int> scoreMap;
    private Dictionary<float, List<Vector2>> JudgePosMap;
    private int ComboIndex = 0;


    // 记录当前正在播放音频的星星实例名
    private string currentPlayingStar = null;
    // 记录开始音量衰减的时间
    private float fadeOutStartTime = -1f;
    // 记录当前正在播放的星星音频的初始音量
    //private float initialStarSoundVolume;

    // 新增一个列表用于存储当前正在处理的Hold对应的instanceName
    private List<string> currentHoldInstanceNames = new List<string>();
    private Chart chart; // 用于存储传入的Chart实例，方便在Update里使用
    private int ActiveStateIndex = 0;
    private int JudgementIndex = 0;
    private int JudgeTextureIndex = 0;
    private float totalTime;
    //private float AnimationScaleAdjust = 772f / 165f;  // 播放动画时x轴缩放调整
    private float AnimationScaleAdjust = 0.8f;  // 播放动画时x轴缩放调整
    private float HoldAnimationScaleAdjust = 0.7f;  // 播放Hold动画时x轴缩放调整
    public float ChartStartTime = 0f;
    public bool IsPlaying = false;
    public float elapsedTime = 0f; // 记录协程启动后的总 elapsed 时间
    public float accumulatedTime = 0f;  // 积累的时间（用于计算是否到固定帧的更新时间）
    private float startTime; // 协程启动的初始时间
    private float lastUpdateTime; // 上次更新的时间点


    // 新增的公共方法，用于接收各个参数并赋值给对应的私有变量，添加了SlidesParent和SlideSoundEffect参数
    public void SetParameters(AudioSource audioSource, GameObject judgePlanesParent, GameObject judgeLinesParent, GameObject colorLinesParent,
        GameObject tapsParent, GameObject slidesParent, GameObject flicksParent, GameObject flickarrowsParent, GameObject holdsParent, GameObject holdOutlinesParent, 
        GameObject starsParent, GameObject substarsParent, GameObject judgeTexturesParent, GameObject multiHitLinesParent,
        AudioSource tapSoundEffect, AudioSource slideSoundEffect, AudioSource flickSoundEffect, AudioSource holdSoundEffect, AudioSource starheadSoundEffect, AudioSource starSoundEffect,
        Chart chart, TMP_Text FpsText, TMP_Text ComboText, TMP_Text ScoreText)
    {
        this.audioSource = audioSource;
        JudgePlanesParent = judgePlanesParent;
        JudgeLinesParent = judgeLinesParent;
        ColorLinesParent = colorLinesParent;
        TapsParent = tapsParent;
        SlidesParent = slidesParent;
        FlicksParent = flicksParent;
        FlickArrowsParent = flickarrowsParent;
        HoldsParent = holdsParent;
        HoldOutlinesParent = holdOutlinesParent;
        StarsParent = starsParent;
        SubStarsParent = substarsParent;
        JudgeTexturesParent = judgeTexturesParent;
        MultiHitLinesParent = multiHitLinesParent;
        TapSoundEffect = tapSoundEffect;
        SlideSoundEffect = slideSoundEffect;
        FlickSoundEffect = flickSoundEffect;
        HoldSoundEffect = holdSoundEffect;
        StarHeadSoundEffect = starheadSoundEffect;
        StarSoundEffect = starSoundEffect;
        this.chart = chart;
        fpsText = FpsText;
        this.ComboText = ComboText;
        this.ScoreText = ScoreText;
        totalTime = audioSource.clip.length;
        //Debug.Log(FlickSoundEffect);
    }

    public void SetParameters2(Dictionary<float, List<string>> StartTimeToInstanceNames, Dictionary<string, List<float>> HoldTimes, Dictionary<string, KeyInfo> KeyReachedJudgment,
            List<float> judgePlanesStartT, List<float> judgePlanesEndT, Dictionary<(int, int), SubStarInfo> SubStarInfoDict, Dictionary<string, (float startT, float endT)> StarTrackTimes, GradientColorListUnity gradientColorList, float chartStartTime)
    {
        startTimeToInstanceNames = StartTimeToInstanceNames;
        holdTimes = HoldTimes;
        keyReachedJudgment = KeyReachedJudgment;
        JudgePlanesStartT = judgePlanesStartT;
        JudgePlanesEndT = judgePlanesEndT;
        subStarInfoDict = SubStarInfoDict;
        starTrackTimes = StarTrackTimes;
        GradientColorList = gradientColorList;
        ChartStartTime = chartStartTime;
    }

    public void SetParameters3(Dictionary<float, int> comboMap, Dictionary<float, int> scoreMap, Dictionary<float, List<Vector2>> JudgePosMap)
    {
        this.comboMap = comboMap;
        this.scoreMap = scoreMap;
        this.JudgePosMap = JudgePosMap;

        // 打印JudgePosMap的所有内容
        //if (JudgePosMap != null && JudgePosMap.Count > 0)
        //{
        //    Debug.Log($"JudgePosMap 包含 {JudgePosMap.Count} 个时间点：");

        //    foreach (var time in JudgePosMap.Keys)
        //    {
        //        var positions = JudgePosMap[time];
        //        string posString = string.Join(" | ", positions.Select(p => $"({p.x}, {p.y})"));
        //        Debug.Log($"时间 {time}s: {positions.Count} 个坐标 [{posString}]");
        //    }
        //}
        //else
        //{
        //    Debug.Log("JudgePosMap 为空或未初始化");
        //}

    }

    private void Start()
    {
        pauseManager = GetComponent<PauseManager>();
        MusicSlider = GameObject.Find("MusicSlider");
        MusicSlider.SetActive(false);
        pauseManager.isPaused = false;
    }


    public void PlayMusicAndChart(Chart chart)
    {
        //GradientColorList = ConvertToUnityList(chart.gradientColorList);
        
        LoadNoteSprites();
        IsPlaying = true;

        startTime = Time.time; // 记录启动时间
        lastUpdateTime = startTime; // 初始化上次更新时间

        //elapsedTime = 0f; // 重置时间计数器
        elapsedTime = -ChartStartTime; // 重置时间计数器
        audioPrevTime = - ChartStartTime;
        // 启动更新位置的协程
        StartCoroutine(UpdatePositionsCoroutine());
        // 启动等待音频播放的协程
        StartCoroutine(WaitAndPlayAudio(ChartStartTime));

    }


    private IEnumerator WaitAndPlayAudio(float delay)
    {
        yield return new WaitUntil(() => Time.time >= startTime + delay);
        if (IsPlaying && !pauseManager.isPaused)
        { audioSource.Play(); }
    }


    public IEnumerator UpdatePositionsCoroutine()
    {
        //float accumulatedTime = 0f; // 累积的未处理时间
        float targetFrameTime = FrameParams.updateInterval; // 目标帧间隔

        while (true)
        {
            //Debug.Log(audioSource.isPlaying);
            //Debug.Log(audioSource.clip.length - audioSource.time);
            //Debug.Log(pauseManager.isPaused);

            // 如果音乐播放完了
            if (!audioSource.isPlaying && audioSource.time >= audioSource.clip.length - 0.01f && !pauseManager.isPaused)
            {
                SceneManager.LoadScene("SongSelect");
                yield break;
            }

            // 以固定间隔执行Updateall，处理累积的时间
            while (accumulatedTime >= targetFrameTime && IsPlaying && !pauseManager.isPaused)
            {
                // 执行游戏逻辑更新
                // 更新elapsedTime（基于固定间隔，而非实际帧时间）
                elapsedTime += targetFrameTime;
                accumulatedTime -= targetFrameTime;
                Updateall(false);
            }

            // 计算从上一帧到现在的真实时间
            if (IsPlaying && !pauseManager.isPaused)
            {
                float deltaTime = Time.deltaTime;
                accumulatedTime += deltaTime;
            }

            // 等待下一帧
            yield return null;
        }
    }


    //当从暂停状态恢复播放时，IfResumePlay=1，此时所有Note重新计算位置和颜色
    private void Updateall(bool IfResumePlay)
    {
        float currentTime = elapsedTime;

        //将已经结束的JudgePlane设置为非激活
        UpdateJudgePlaneActiveState(currentTime);

        //更新Note的激活状态
        UpdateNotesActiveState(currentTime);

        //对到达判定线的Note播放判定动画
        UpdateNotesJudgementAnimation(currentTime);

        //更新星星划动音频的状态
        UpdateStarSoundEffect(currentTime);

        // 统一处理当前所有Hold的状态更新（开始、持续、结束判定）
        UpdateHoldStates(currentTime);

        //更新所有Note位置和颜色
        UpdatePositionsAndColor(currentTime, chart.speedList, IfResumePlay);

        //更新所有Arrow的透明度（以及更新启动Arrow位置）
        CheckArrowVisibility(currentTime, subStarInfoDict, SubStarsParent);

        // 更新连击数和分数
        UpdateComboAndScore(currentTime, IfResumePlay);

        //Debug.Log("2. " + audioTime);
    }

    private void Update()
    {
        float fps = 1.0f / Time.deltaTime;
        //Debug.Log($"FPS: {fps:F1}");
        // 更新显示
        if (fpsText != null)
        {
            fpsText.text = $"FPS: {fps:F1}";
        }

        // 检查鼠标点击
        if (Input.GetMouseButtonDown(0))
        {
            pauseManager.CheckPauseButtonClick();
        }
    }

    private void UpdateJudgePlaneActiveState(float currentTime)
    {
        // 将所有已经结束的 JudgePlane（endT 小于 currentTime）设置为 setActive(false)
        for (int i = 0; i < JudgePlanesParent.transform.childCount; i++)
        {
            //Debug.Log(i);
            float endT = JudgePlanesEndT[i];
            if (endT < currentTime)
            {
                // 隐藏 JudgePlane
                var judgePlaneObject = JudgePlanesParent.transform.GetChild(i);
                if (judgePlaneObject != null)
                {
                    judgePlaneObject.gameObject.SetActive(false);
                }

                // 隐藏对应的 LeftColorLine
                if (ColorLinesParent.transform.childCount > i * 2)
                {
                    var leftColorLineObject = ColorLinesParent.transform.GetChild(i * 2);
                    if (leftColorLineObject != null)
                    {
                        leftColorLineObject.gameObject.SetActive(false);
                    }
                }

                // 隐藏对应的 RightColorLine
                if (ColorLinesParent.transform.childCount > i * 2 + 1)
                {
                    var rightColorLineObject = ColorLinesParent.transform.GetChild(i * 2 + 1);
                    if (rightColorLineObject != null)
                    {
                        rightColorLineObject.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void UpdateNotesActiveState(float currentTime)
    {
        //Debug.Log(ActiveStateIndex);
        // 从未进入激活范围到进入激活范围，包含哪些Note
        while (ActiveStateIndex < startTimeToInstanceNames.Count && startTimeToInstanceNames.ElementAt(ActiveStateIndex).Key < currentTime + ChartParams.NoteRenderTimeOffset)
        {
            float startT = startTimeToInstanceNames.ElementAt(ActiveStateIndex).Key;
            //Debug.Log(startT);
            List<string> instanceNames = startTimeToInstanceNames.ElementAt(ActiveStateIndex).Value;
            foreach (var instanceName in instanceNames)
            {
                if (instanceName.StartsWith("JudgePlane"))
                {
                    GameObject Instance = JudgePlanesParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetJudgePlanePosition(currentTime, Instance.transform);
                }
                else if (instanceName.StartsWith("Tap"))
                {
                    GameObject Instance = TapsParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);
                }
                else if (instanceName.StartsWith("Slide"))
                {
                    GameObject Instance = SlidesParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);
                }
                else if (instanceName.StartsWith("Flick"))
                {
                    GameObject Instance = FlicksParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);

                    //对应的FlickArrow
                    string numberPart = instanceName.Substring(5);
                    if (int.TryParse(numberPart, out int flickIndex))
                    {
                        GameObject gameobject = FlickArrowsParent.transform.Find($"FlickArrow{flickIndex}").gameObject;
                        gameobject.SetActive(true);
                        ResetNotePosition(gameobject.transform, currentTime, startT);
                    }
                }
                else if (instanceName.StartsWith("Hold"))
                {
                    GameObject Instance = HoldsParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetHoldPosition(Instance.transform, currentTime);
                }
                else if (instanceName.StartsWith("StarHead"))
                {
                    GameObject Instance = StarsParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);
                }
                else if (instanceName.StartsWith("MultiHitLine"))
                {
                    GameObject Instance = MultiHitLinesParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);
                }
            }
            ActiveStateIndex++;
        }
    }

    private void UpdateNotesJudgementAnimation(float currentTime)
    {
        // 使用双指针来维护从上一帧到这一帧的时间，包含哪些note
        while (JudgementIndex < startTimeToInstanceNames.Count && startTimeToInstanceNames.ElementAt(JudgementIndex).Key < currentTime)
        {
            List<string> instanceNames = startTimeToInstanceNames.ElementAt(JudgementIndex).Value;
            foreach (var instanceName in instanceNames)
            {
                //Debug.Log(instanceName);
                if (!instanceName.StartsWith("JudgePlane"))
                {
                    KeyInfo keyInfo = keyReachedJudgment[instanceName];
                    keyInfo.isJudged = true;
                }

                // 根据是Tap、Slide还是Flick等不同类型的键，播放对应的音效和动画（这里简单示例，实际可能需要更精确判断类型的逻辑）
                if (instanceName.StartsWith("Tap"))
                {
                    TapSoundEffect.Play();
                    //FlickSoundEffect.Play();
                    PlayAnimation(instanceName, "TapHitEffect");
                }
                else if (instanceName.StartsWith("Slide"))
                {
                    SlideSoundEffect.Play();
                    PlayAnimation(instanceName, "SlideEffect");
                }
                else if (instanceName.StartsWith("Flick"))
                {
                    FlickSoundEffect.Play();
                    PlayAnimation(instanceName, "FlickEffect");
                }
                else if (instanceName.StartsWith("Hold"))
                {
                    PlayHoldAnimation(instanceName, currentTime);
                }
                else if (instanceName.StartsWith("StarHead"))
                {
                    StarHeadSoundEffect.Play();
                    PlayAnimation(instanceName, "StarHeadEffect");

                }
                else if (instanceName.StartsWith("Stars"))
                {
                    StarSoundEffect.Play();
                }
                else if (instanceName.StartsWith("MultiHitLine"))
                {
                    GameObject keyGameObject = MultiHitLinesParent.transform.Find(instanceName).gameObject;
                    keyGameObject.SetActive(false);
                    //Destroy(keyGameObject);
                }
            }
            JudgementIndex++;
        }

        while (JudgeTextureIndex < JudgePosMap.Count && JudgePosMap.ElementAt(JudgeTextureIndex).Key < currentTime)
        {
            List<Vector2> Positions = JudgePosMap.ElementAt(JudgeTextureIndex).Value;
            foreach (Vector2 Pos in Positions)
            {
                // 创建一个新的挂载SpriteRenderer的游戏物体
                string newName = "JudgeTexture";
                GameObject newGameObject = new GameObject(newName, typeof(RectTransform), typeof(SpriteRenderer));
                SpriteRenderer spriteRenderer = newGameObject.GetComponent<SpriteRenderer>();
                spriteRenderer.sprite = Sync;

                Color SpriteColor = spriteRenderer.color; // 初始颜色 (Alpha=1)
                Color NewColor = new Color(SpriteColor.r, SpriteColor.g, SpriteColor.b, JudgeTextureParams.StartAlpla);
                spriteRenderer.color = NewColor;


                RectTransform newGameObjectRectTransform = newGameObject.GetComponent<RectTransform>();

                // 设置新物体的父物体
                newGameObjectRectTransform.SetParent(JudgeTexturesParent.GetComponent<RectTransform>(), false);
                // false 表示不保留世界坐标，适配 UI 层级的位置计算

                // 设置位置：UI 元素用 anchoredPosition 来设置位置更合适
                newGameObjectRectTransform.anchoredPosition = new Vector2(Pos.x, Pos.y);
                newGameObjectRectTransform.localScale = Vector3.one * JudgeTextureParams.Scale;

                // 继承父物体的图层
                int parentLayer = JudgeTexturesParent.layer;
                newGameObject.layer = parentLayer;

                // 启动动画协程
                StartCoroutine(AnimateAndDestroy(newGameObject));
            }
            JudgeTextureIndex++;
        }

    }



    private void UpdateStarSoundEffect(float currentTime)
    {
        // 检查当前播放的星星音频是否结束
        if (currentPlayingStar != null && starTrackTimes.TryGetValue(currentPlayingStar, out var trackTimes))
        {
            if (currentTime >= trackTimes.endT)
            {
                if (fadeOutStartTime < 0)
                {
                    // 开始音量衰减
                    fadeOutStartTime = currentTime;
                }

                // 计算音量衰减进度
                float ElapsedTime = currentTime - fadeOutStartTime;
                if (ElapsedTime < SoundParams.starSoundFadeOutTime)
                {
                    // 线性降低音量
                    float newVolume = 1 - ElapsedTime / SoundParams.starSoundFadeOutTime;
                    StarSoundEffect.volume = newVolume;
                }
                else
                {
                    // 音量降为 0 后停止播放
                    StarSoundEffect.Stop();
                    StarSoundEffect.volume = 1f; // 恢复初始音量
                    currentPlayingStar = null;
                    fadeOutStartTime = -1f;
                }
            }
        }

        // 检查是否有新的星星开始划动，覆盖之前的音频
        foreach (var pair in starTrackTimes)
        {
            string instanceName = pair.Key;
            var trackTime = pair.Value;
            if (currentTime >= trackTime.startT && currentTime < trackTime.endT)
            {
                if (currentPlayingStar == null || starTrackTimes[currentPlayingStar].startT < trackTime.startT)
                {
                    StarSoundEffect.Stop();
                    StarSoundEffect.volume = 1f; // 恢复初始音量
                    StarSoundEffect.Play();
                    currentPlayingStar = instanceName;
                    fadeOutStartTime = -1f; // 重置音量衰减开始时间
                }
            }
        }
    }

    private void UpdateComboAndScore(float currentTime, bool IfResumePlay)
    {
        // 如果是暂停重新播放，则重置ComboIndex
        if (IfResumePlay) { ComboIndex = 0; }

        //只有当ComboIndex没越界时，更新Combo和Score
        if (ComboIndex <= comboMap.Count - 1)
        { 
            float JudgeTime = comboMap.ElementAt(ComboIndex).Key;

            while (ComboIndex <= comboMap.Count-1 && JudgeTime < currentTime) 
            {
                int Combo = comboMap.ElementAt(ComboIndex).Value;
                if (Combo >= ScoreParams.MinCombo)
                {
                    ComboText.text = $"Combo {comboMap.ElementAt(ComboIndex).Value}";
                }
                else 
                {
                    ComboText.text = "";
                }

                ScoreText.text = $"{scoreMap.ElementAt(ComboIndex).Value}";

                ComboIndex += 1;
                // 防止索引越界：提前检查下一个索引是否有效
                if (ComboIndex < comboMap.Count)
                {
                    JudgeTime = comboMap.ElementAt(ComboIndex).Key;
                }
                else
                {
                    break; // 到达集合末尾时退出循环
                }
            }

            //int JudgeIndex = ComboIndex - 1;


        }

    }

    private void PlayHoldAnimation(string instanceName, float currentTime)
    {
        //hold游戏物体实例
        // 从instanceName中提取数字部分，用于查找对应的Hold
        int holdIndex = GetHoldIndexFromInstanceName(instanceName);
        if (holdIndex >= 0 && holdIndex < chart.holds.Count)
        {
            Hold hold = chart.holds[holdIndex];
            KeyInfo holdKeyInfo = keyReachedJudgment[instanceName];
            float holdStartTime = holdTimes[instanceName][0];
            float holdEndTime = holdTimes[instanceName][1];
            bool isStart = currentTime >= holdStartTime && currentTime < holdEndTime;
            //bool isEnd = currentTime >= holdEndTime;
            //Debug.Log($"InstanceName: {instanceName}, currentTime: {currentTime}, isStart: {isStart}, isEnd: {isEnd}");

            // 获取Subhold的X轴左侧和右侧坐标
            float subholdLeftX = hold.GetCurrentSubHoldLeftX(currentTime);
            float subholdRightX = hold.GetCurrentSubHoldRightX(currentTime);
            // 计算X轴坐标均值
            float x = (subholdLeftX + subholdRightX) / 2;

            JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
            float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(hold.GetFirstSubHoldStartTime());
            // 对yAxisPosition进行TransformYCoordinate变换
            float yPos = TransformYCoordinate(yAxisPosition);
            Vector3 referencePoint = new Vector3(0, yPos, 0);

            float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);
            //Debug.Log(worldUnitToScreenPixelX);
            float startXWorld = worldUnitToScreenPixelX * x / ChartParams.XaxisMax;
            float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;
            //Debug.Log(noteSizeWorldLengthPerUnit);

            float x_width = noteSizeWorldLengthPerUnit * HoldAnimationScaleAdjust * (subholdRightX - subholdLeftX);
            //Debug.Log(x_width);


            // HoldHitEffect的位置
            Vector3 holdHitEffectPosition = new Vector3(-startXWorld, yPos, 0f);

            // 仅在开始判定且还没播放过开始音效时播放
            if (isStart && !holdKeyInfo.isSoundPlayedAtStart)
            {
                HoldSoundEffect.Play();
                holdKeyInfo.isSoundPlayedAtStart = true; // 标记已播放开始音效

                // 将当前Hold的instanceName添加到列表中，后续统一处理状态更新
                currentHoldInstanceNames.Add(instanceName);

                string holdHitEffectName = $"HoldHitEffect{holdIndex + 1}";
                Transform holdHitEffectTransform = HoldsParent.transform.Find(holdHitEffectName);
                GameObject holdHitEffect;

                Animator holdAnimator = null;
                if (holdHitEffectTransform != null)
                {
                    // 如果找到该游戏物体，设置其为激活状态
                    holdHitEffect = holdHitEffectTransform.gameObject;
                    holdHitEffect.SetActive(true);
                    holdAnimator = holdHitEffect.GetComponent<Animator>();
                }
                else
                {
                    // 如果未找到，则创建新的游戏物体
                    holdHitEffect = new GameObject(holdHitEffectName);
                    holdHitEffect.transform.parent = HoldsParent.transform;
                    holdHitEffect.AddComponent<SpriteRenderer>();
                    holdAnimator = holdHitEffect.AddComponent<Animator>();
                }

                holdHitEffect.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                holdHitEffect.transform.position = holdHitEffectPosition;
                holdHitEffect.transform.localScale = new Vector3(x_width, 1, 1);
                //设置Hold判定动画图层（确保位于JudgeLine之上）
                holdHitEffect.layer = 10;

                //动画组件状态设置
                holdAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");
                int holdStartHash = Animator.StringToHash($"HoldStart");
                //设置状态为HoldStart
                holdAnimator.SetTrigger(holdStartHash);

            }
        }
    }

    private void UpdateHoldStates(float currentTime)
    {
        for (int i = currentHoldInstanceNames.Count - 1; i >= 0; i--)
        {
            string instanceName = currentHoldInstanceNames[i];
            GameObject holdObject = HoldsParent.transform.Find(instanceName).gameObject;
            int holdIndex = GetHoldIndexFromInstanceName(instanceName);
            GameObject holdHitEffect = HoldsParent.transform.Find($"HoldHitEffect{holdIndex + 1}").gameObject;
            if (holdIndex >= 0 && holdIndex < chart.holds.Count)
            {
                Hold hold = chart.holds[holdIndex];
                KeyInfo holdKeyInfo = keyReachedJudgment[instanceName];
                float holdStartTime = holdTimes[instanceName][0];
                float holdEndTime = holdTimes[instanceName][1];
                bool isStart = currentTime >= holdStartTime && currentTime < holdEndTime;
                bool isEnd = currentTime >= holdEndTime;
                //Debug.Log($"InstanceName: {instanceName}, currentTime: {currentTime}, Start: {holdStartTime}, End: {holdEndTime}");

                float subholdLeftX = hold.GetCurrentSubHoldLeftX(currentTime);
                float subholdRightX = hold.GetCurrentSubHoldRightX(currentTime);
                float x = (subholdLeftX + subholdRightX) / 2;

                JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
                float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(currentTime);
                // 对yAxisPosition进行TransformYCoordinate变换
                float yPos = TransformYCoordinate(yAxisPosition);
                Vector3 referencePoint = new Vector3(0, yPos, 0);
                float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);
                float startXWorld = worldUnitToScreenPixelX * x / ChartParams.XaxisMax;
                float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                float x_width = noteSizeWorldLengthPerUnit * HoldAnimationScaleAdjust * (subholdRightX - subholdLeftX);

                Vector3 holdHitEffectPosition = new Vector3(-startXWorld, yPos, 0f);

                //判定结束
                if (isEnd && !holdKeyInfo.isSoundPlayedAtEnd)
                {
                    //Debug.Log($"{instanceName}判定已结束，当前时间：{currentTime}");
                    //先将Hold物体和对应的Outline物体设置为非激活
                    holdObject.SetActive(false);

                    // 获取 HoldOutlinesParent 下对应的物体
                    string outlineInstanceName = "HoldOutline" + instanceName.Substring(4);
                    GameObject outlineGameObject = HoldOutlinesParent.transform.Find(outlineInstanceName).gameObject;
                    if (outlineGameObject != null)
                    {
                        outlineGameObject.SetActive(false);
                    }

                    HoldSoundEffect.Play();
                    holdKeyInfo.isSoundPlayedAtEnd = true;
                    // 如果已经结束判定，从列表中移除该instanceName
                    currentHoldInstanceNames.RemoveAt(i);

                    //Debug.Log(holdHitEffectPosition);
                    //结束时的特效缩放由holdEndTime对应X轴宽度决定
                    subholdLeftX = hold.GetCurrentSubHoldLeftX(holdEndTime);
                    subholdRightX = hold.GetCurrentSubHoldRightX(holdEndTime);
                    x = (subholdLeftX + subholdRightX) / 2;
                    
                    yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(holdEndTime);
                    // 对yAxisPosition进行TransformYCoordinate变换
                    yPos = TransformYCoordinate(yAxisPosition);
                    referencePoint = new Vector3(0, yPos, 0);
                    worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);
                    startXWorld = worldUnitToScreenPixelX * x / ChartParams.XaxisMax;
                    noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;
                    holdHitEffectPosition = new Vector3(-startXWorld, yPos, 0f);
                    x_width = noteSizeWorldLengthPerUnit * HoldAnimationScaleAdjust * (subholdRightX - subholdLeftX);

                    if (holdHitEffect != null)
                    {
                        holdHitEffect.transform.position = holdHitEffectPosition;
                        holdHitEffect.transform.localScale = new Vector3(x_width, 1, 1);
                        Animator animator = holdHitEffect.GetComponent<Animator>();

                        if (animator != null)
                        {
                            int holdEndHash = Animator.StringToHash($"HoldEnd");
                            //Debug.Log("holdEndHash: " + holdEndHash);
                            // 设置动画状态为HoldEnd
                            animator.SetTrigger(holdEndHash);
                            //Destroy( holdHitEffect );

                            // 正常播放动画
                            animator.Play("TapHitEffect");
                            // 检查动画是否播放完毕
                            StartCoroutine(CheckAnimationEnd(animator, holdHitEffect));

                        }
                    }
                }

                //在判定过程中
                else if (isStart && !isEnd)
                {
                    if (holdHitEffect != null)
                    {
                        holdHitEffect.transform.position = holdHitEffectPosition;
                        holdHitEffect.transform.localScale = new Vector3(x_width, 1, 1);
                    }
                }
            }
        }
    }

    private void UpdatePositionsAndColor(float currentTime, List<Speed> speedList, bool IfResumePlay)
    {
        // 计算每帧对应的 Z 轴坐标减少量
        //float audioTimeDelta = currentTime - audioPrevTime;
        float totalDistance = 0;

        // 处理 startTime 之前的所有速度段
        //注意查找每个速度段与audioPrevTime到currentTime区间是否有交集
        float FirstSpeed = speedList[0].sp;

        //如果这个时候音乐还没播放，就按照第一个速度
        if (currentTime <= 0) 
        {
            totalDistance += (currentTime - audioPrevTime) * FirstSpeed;
        }
        else 
        { 
            foreach (var speed in speedList)
            {
                //如果全包含
                if (speed.startT <= audioPrevTime & speed.endT >= currentTime)
                {
                    totalDistance += (currentTime - audioPrevTime) * speed.sp;
                }
                //包含前半段
                else if (speed.endT > audioPrevTime & speed.endT <= currentTime)
                {
                    totalDistance += (speed.endT - audioPrevTime) * speed.sp;
                }
                //包含后半段
                else if (speed.startT >= audioPrevTime & speed.startT < currentTime)
                {
                    totalDistance += (currentTime - speed.startT) * speed.sp;
                }
            }
        }
        //Debug.Log(totalDistance);
        float zAxisDecreasePerFrame = SpeedParams.NoteSpeedDefault * totalDistance;
        //Debug.Log(zAxisDecreasePerFrame);


        UpdateJudgePlanesPosition(currentTime, zAxisDecreasePerFrame, IfResumePlay);
        UpdateJudgeLinesPosition(currentTime);
        UpdateNotesPosition(currentTime, zAxisDecreasePerFrame, IfResumePlay);
        //更新所有Note描边颜色
        UpdateColors(currentTime, IfResumePlay);

        audioPrevTime = currentTime;
    }

    private void UpdateColors(float currentTime, bool IfUpdateAll)
    {
        // 处理JudgePlane
        for (int i = 0; i < JudgePlanesParent.transform.childCount; i++)
        {
            float endT = JudgePlanesEndT[i];
            //只更新还没消失的JudgePlane的色条颜色
            if (endT > currentTime)
            {
                //Debug.Log(i + 1);
                JudgePlane currentJudgePlane = chart.GetCorrespondingJudgePlane(i + 1);
                Color planecolor = GradientColorList.GetColorAtTimeAndY(currentTime, currentJudgePlane.GetPlaneYAxis(currentTime));

                // 设置LeftColorLine颜色
                if (ColorLinesParent.transform.childCount > i * 2)
                {
                    var leftColorLineObject = ColorLinesParent.transform.GetChild(i * 2).gameObject;
                    if (leftColorLineObject != null && leftColorLineObject.activeSelf)
                    {
                        SetSpriteColor(leftColorLineObject, planecolor);
                    }
                }

                // 设置RightColorLine颜色
                if (ColorLinesParent.transform.childCount > i * 2 + 1)
                {
                    var rightColorLineObject = ColorLinesParent.transform.GetChild(i * 2 + 1).gameObject;
                    if (rightColorLineObject != null && rightColorLineObject.activeSelf)
                    {
                        SetSpriteColor(rightColorLineObject, planecolor);
                    }
                }
            }
        }

        // 处理 HoldOutline
        if (HoldOutlinesParent != null)
        {
            for (int i = 0; i < HoldOutlinesParent.transform.childCount; i++)
            {
                GameObject keyGameObject = HoldOutlinesParent.transform.GetChild(i).gameObject;
                if (!keyGameObject.activeSelf) continue;

                //string instanceName = keyGameObject.name;
                Hold hold = chart.holds[i];
                JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
                float currentY = associatedJudgePlaneObject.GetPlaneYAxis(currentTime);
                Color holdcolor = GradientColorList.GetColorAtTimeAndY(currentTime, currentY);
                SetSpriteColor(keyGameObject, holdcolor);
            }
        }

        //如果更新所有其他Note的颜色（暂停结束，恢复播放时使用）
        if (IfUpdateAll)
        {
            UpdateOtherNoteColors(currentTime);
        }
        else
        {
            //否则，如果正好处于颜色突变时间，则更新所有其他Note的Outline颜色
            for (int j = 0; j < GradientColorList.colors.Count; j++)
            {
                GradientColorUnity color = GradientColorList.colors[j];
                if (color.startT > audioPrevTime && color.startT <= currentTime)
                {
                    //Debug.Log("换颜色！");
                    UpdateOtherNoteColors(currentTime);
                }
                //当前所处时段为需要随时间变化的时段时，仅变化处于激活状态的Note
                else if (currentTime >= color.startT && currentTime <= color.endT && color.isTimeInterpolationNeeded)
                {
                    UpdateOtherActiveNoteColors(currentTime);
                }
            }
        }
    }

    private void UpdateOtherNoteColors(float currentTime)
    {

        // 处理 Taps 键
        if (TapsParent != null)
        {
            for (int i = 0; i < TapsParent.transform.childCount; i++)
            {
                Transform childTransform = TapsParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;

                string instanceName = keyGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect")) { continue; }
                Tap tap = chart.taps[i];
                Color tapcolor = GradientColorList.GetColorAtTimeAndY(currentTime, tap.startY);
                //Debug.Log(tapcolor);
                SetSpriteColor(keyGameObject, tapcolor);
            }
        }

        // 处理 Slides 键
        if (SlidesParent != null)
        {
            for (int i = 0; i < SlidesParent.transform.childCount; i++)
            {
                Transform childTransform = SlidesParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;

                string instanceName = keyGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect")) { continue; }
                Slide slide = chart.slides[i];
                Color slidecolor = GradientColorList.GetColorAtTimeAndY(currentTime, slide.startY);
                SetSpriteColor(keyGameObject, slidecolor);
            }
        }

        // 处理 Flicks 键
        if (FlicksParent != null)
        {
            for (int i = 0; i < FlicksParent.transform.childCount; i++)
            {
                Transform childTransform = FlicksParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;

                string instanceName = keyGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect")) { continue; }
                Flick flick = chart.flicks[i];
                Color flickcolor = GradientColorList.GetColorAtTimeAndY(currentTime, flick.startY);
                SetSpriteColor(keyGameObject, flickcolor);
            }
        }

        // 处理 StarHead 键
        if (StarsParent != null)
        {
            for (int i = 0; i < StarsParent.transform.childCount; i++)
            {
                Transform childTransform = StarsParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;

                string instanceName = keyGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect")) { continue; }
                Star star = chart.stars[i];
                Color starcolor = GradientColorList.GetColorAtTimeAndY(currentTime, star.startY);
                SetSpriteColor(keyGameObject, starcolor);
            }
        }
    }

    private void UpdateOtherActiveNoteColors(float currentTime)
    {

        // 处理 Taps 键
        if (TapsParent != null)
        {
            for (int i = 0; i < TapsParent.transform.childCount; i++)
            {
                Transform childTransform = TapsParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;
                if (!keyGameObject.activeSelf) continue;

                string instanceName = keyGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect")) { continue; }
                Tap tap = chart.taps[i];
                Color tapcolor = GradientColorList.GetColorAtTimeAndY(currentTime, tap.startY);
                //Debug.Log(tapcolor);
                SetSpriteColor(keyGameObject, tapcolor);
            }
        }

        // 处理 Slides 键
        if (SlidesParent != null)
        {
            for (int i = 0; i < SlidesParent.transform.childCount; i++)
            {
                Transform childTransform = SlidesParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;
                if (!keyGameObject.activeSelf) continue;

                string instanceName = keyGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect")) { continue; }
                Slide slide = chart.slides[i];
                Color slidecolor = GradientColorList.GetColorAtTimeAndY(currentTime, slide.startY);
                SetSpriteColor(keyGameObject, slidecolor);
            }
        }

        // 处理 Flicks 键
        if (FlicksParent != null)
        {
            for (int i = 0; i < FlicksParent.transform.childCount; i++)
            {
                Transform childTransform = FlicksParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;
                if (!keyGameObject.activeSelf) continue;

                string instanceName = keyGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect")) { continue; }
                Flick flick = chart.flicks[i];
                Color flickcolor = GradientColorList.GetColorAtTimeAndY(currentTime, flick.startY);
                SetSpriteColor(keyGameObject, flickcolor);
            }
        }

        // 处理 StarHead 键
        if (StarsParent != null)
        {
            for (int i = 0; i < StarsParent.transform.childCount; i++)
            {
                Transform childTransform = StarsParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;
                if (!keyGameObject.activeSelf) continue;

                string instanceName = keyGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect")) { continue; }
                Star star = chart.stars[i];
                Color starcolor = GradientColorList.GetColorAtTimeAndY(currentTime, star.startY);
                SetSpriteColor(keyGameObject, starcolor);
            }
        }
    }

    private void UpdateJudgePlanesPosition(float currentTime, float zAxisDecreasePerFrame, bool IfResumePlay)
    {
        //如果恢复播放，则重置JudgePlane的位置
        if (IfResumePlay)
        {
            //float ZPos = -CalculateZAxisPosition(currentTime, ChartStartTime, chart.speedList);
            for (int i = 0; i < JudgePlanesParent.transform.childCount; i++)
            {
                Transform childTransform = JudgePlanesParent.transform.GetChild(i);
                ResetJudgePlanePosition(currentTime, childTransform);
            }

        }
        //否则只更新JudgePlane的位置
        else
        {
            // 如果已经创建了 JudgePlanesParent 并且音频正在播放，更新其子物体的 Z 轴坐标
            if (JudgePlanesParent != null)
            {
                for (int i = 0; i < JudgePlanesParent.transform.childCount; i++)
                {
                    Transform childTransform = JudgePlanesParent.transform.GetChild(i);
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;

                    // 获取子物体的名称
                    string childName = childTransform.gameObject.name;
                    // 假设子物体名称为 "JudgePlaneX"，提取 X 作为 judgePlaneId
                    int judgePlaneId = int.Parse(childName.Substring("JudgePlane".Length));
                    JudgePlane currentJudgePlane = chart.GetCorrespondingJudgePlane(judgePlaneId);
                    float YAxis = currentJudgePlane.GetPlaneYAxis(currentTime);
                    // 根据 YAxis 的值，实时改变 currentJudgePlane 下所有 SubJudgePlane 实例的透明度
                    currentJudgePlane.ChangeJudgePlaneTransparency(JudgePlanesParent, YAxis);
                }
            }

            // 更新 ColorLinesParent 的子对象的位置
            if (ColorLinesParent != null)
            {
                for (int i = 0; i < ColorLinesParent.transform.childCount; i++)
                {
                    Transform childTransform = ColorLinesParent.transform.GetChild(i);
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;
                }
            }
        }
    }

    private void ResetJudgePlanePosition(float currentTime, Transform childTransform)
    {
        float ZPos = -CalculateZAxisPosition(currentTime, ChartStartTime, chart.speedList);
        Vector3 currentPosition = childTransform.position;
        currentPosition.z = ZPos;
        childTransform.position = currentPosition;

        // 获取子物体的名称
        string childName = childTransform.gameObject.name;
        // 假设子物体名称为 "JudgePlaneX"，提取 X 作为 judgePlaneId
        int judgePlaneId = int.Parse(childName.Substring("JudgePlane".Length));
        JudgePlane currentJudgePlane = chart.GetCorrespondingJudgePlane(judgePlaneId);
        float YAxis = currentJudgePlane.GetPlaneYAxis(currentTime);
        // 根据 YAxis 的值，实时改变 currentJudgePlane 下所有 SubJudgePlane 实例的透明度
        currentJudgePlane.ChangeJudgePlaneTransparency(JudgePlanesParent, YAxis);

        
        // 更新 ColorLinesParent 的子对象的位置
        Transform LeftColorLineTransform = ColorLinesParent.transform.Find($"LeftColorLine{judgePlaneId}");
        Vector3 LeftcurrentPos = LeftColorLineTransform.position;
        LeftcurrentPos.z = ZPos;
        LeftColorLineTransform.position = LeftcurrentPos;

        // 更新 ColorLinesParent 的子对象的位置
        Transform RightColorLineTransform = ColorLinesParent.transform.Find($"RightColorLine{judgePlaneId}");
        Vector3 RightcurrentPos = RightColorLineTransform.position;
        RightcurrentPos.z = ZPos;
        RightColorLineTransform.position = RightcurrentPos;
    }


    private void UpdateJudgeLinesPosition(float currentTime)
    {
        // 如果已经创建了 JudgeLinesParent 并且音频正在播放，更新其子物体的 Y 轴坐标
        if (JudgeLinesParent != null)
        {
            for (int i = 0; i < JudgeLinesParent.transform.childCount; i++)
            {
                RectTransform judgeLineRectTransform = JudgeLinesParent.transform.GetChild(i) as RectTransform;
                if (judgeLineRectTransform != null)
                {
                    // 通过名字获取对应的 JudgePlane
                    JudgePlane correspondingJudgePlane = chart.GetCorrespondingJudgePlane(judgeLineRectTransform.name);
                    if (correspondingJudgePlane != null)
                    {
                        // 获取该 JudgeLine 的开始时间和结束时间
                        float startT = JudgePlanesStartT[i];
                        float endT = JudgePlanesEndT[i];

                        // 判断当前时间与开始时间的关系
                        // 当当前时间小于开始时间 - 出现时间时，不进行操作
                        if (currentTime < startT - ChartParams.JudgeLineAppearTime)
                        {
                            judgeLineRectTransform.gameObject.SetActive(false);
                        }
                        // 当当前时间介于开始时间 - 出现时间与开始时间之间时，设置 JudgeLine 为 active，且透明度线性地由 0 变为 1
                        else if (currentTime >= startT - ChartParams.JudgeLineAppearTime && currentTime <= startT)
                        {
                            judgeLineRectTransform.gameObject.SetActive(true);
                            float t = (currentTime - (startT - ChartParams.JudgeLineAppearTime)) / ChartParams.JudgeLineAppearTime;
                            JudgeLine.SetJudgeLineAlpha(judgeLineRectTransform.gameObject, t);
                        }
                        // 当前时间大于开始时间，小于结束时间时，更新 JudgeLine 的位置，并设置对应JudgePlane透明度
                        else if (currentTime > startT && currentTime <= endT)
                        {
                            judgeLineRectTransform.gameObject.SetActive(true);
                            // 获取当前时间的 Y 轴坐标
                            float YAxis = correspondingJudgePlane.GetPlaneYAxis(currentTime);

                            // 获取判定区下边缘和上边缘在屏幕空间中的像素坐标
                            //float bottomPixel = AspectRatioManager.croppedScreenHeight * (1-HorizontalParams.VerticalMarginBottom);
                            //float topPixel = AspectRatioManager.croppedScreenHeight * (1-HorizontalParams.VerticalMarginCeiling);

                            // 计算 YAxis 对应的归一化坐标
                            //float YAxisUniform = (YAxis - bottomPixel) / (topPixel - bottomPixel);

                            Vector2 Position = ScalePositionToScreenJudgeLine(new Vector2(0f, YAxis), JudgeLinesParent.GetComponent<RectTransform>());
                            judgeLineRectTransform.anchoredPosition = Position;
                            // 设置 JudgeLine 透明度为 1
                            JudgeLine.SetJudgeLineAlpha(judgeLineRectTransform.gameObject, 1f);
                        }
                        // 当当前时间介于结束时间与结束时间+出现时间之间时，设置 JudgeLine 为 active，且透明度线性地由 1 变为 0
                        else if (currentTime > endT && currentTime <= endT + ChartParams.JudgeLineAppearTime)
                        {
                            judgeLineRectTransform.gameObject.SetActive(true);
                            float t = 1 - (currentTime - endT) / ChartParams.JudgeLineAppearTime;
                            JudgeLine.SetJudgeLineAlpha(judgeLineRectTransform.gameObject, t);
                        }
                        // 当当前时间超过结束时间时，设置 JudgeLine 为 false
                        else if (currentTime > endT + ChartParams.JudgeLineAppearTime)
                        {
                            judgeLineRectTransform.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    private void UpdateNotesPosition(float currentTime, float zAxisDecreasePerFrame, bool IfResumePlay)
    {
        //如果恢复播放，则重置所有Note的位置
        if (IfResumePlay)
        {
            int Index = 0;
            while (Index < startTimeToInstanceNames.Count)
            {
                float startT = startTimeToInstanceNames.ElementAt(Index).Key;
                List<string> instanceNames = startTimeToInstanceNames.ElementAt(Index).Value;

                foreach (var instanceName in instanceNames)
                {
                    GameObject NoteInstance = null;
                    Sprite sprite = null;
                    if (!instanceName.StartsWith("JudgePlane"))
                    {
                        // 设置该Note的位置和外观
                        if (instanceName.StartsWith("Hold"))
                        {
                            NoteInstance = HoldsParent.transform.Find(instanceName).gameObject;
                            ResetHoldPosition(NoteInstance.transform, currentTime);

                        }
                        //对于非JudgePlane和非Hold的其他游戏物体，统一处理位置和外观
                        else
                        {
                            if (instanceName.StartsWith("Tap"))
                            {
                                NoteInstance = TapsParent.transform.Find(instanceName).gameObject;
                                sprite = TapSprite;
                            }
                            else if (instanceName.StartsWith("Slide"))
                            {
                                NoteInstance = SlidesParent.transform.Find(instanceName).gameObject;
                                sprite = SlideSprite;
                            }
                            else if (instanceName.StartsWith("Flick"))
                            {
                                NoteInstance = FlicksParent.transform.Find(instanceName).gameObject;
                                sprite = FlickSprite;

                                //重置FlickArrow的位置
                                string numberPart = instanceName.Substring(5);
                                if (int.TryParse(numberPart, out int flickIndex))
                                {
                                    GameObject gameobject = FlickArrowsParent.transform.Find($"FlickArrow{flickIndex}").gameObject;
                                    if (gameobject != null)
                                    {
                                        // 更新FlickArrow位置
                                        Vector3 currentPos = gameobject.transform.position;
                                        currentPos.z = CalculateZAxisPosition(startT, ChartStartTime, chart.speedList) - CalculateZAxisPosition(currentTime, ChartStartTime, chart.speedList);
                                        gameobject.transform.position = currentPos;
                                        Flick flick = chart.flicks[flickIndex - 1];
                                        float flickDirection = flick.flickDirection;

                                        // 提前更新Flick位置（AdjustFlickArrowPosition需要用到正确的位置）
                                        currentPos = NoteInstance.transform.position;
                                        currentPos.z = CalculateZAxisPosition(startT, ChartStartTime, chart.speedList) - CalculateZAxisPosition(currentTime, ChartStartTime, chart.speedList) + ChartParams.NoteZAxisOffset;
                                        NoteInstance.transform.position = currentPos;

                                        // 针对横划键，需要额外调整位置
                                        AdjustFlickArrowPosition(gameobject, NoteInstance, flickDirection);
                                    }
                                }
                            }
                            else if (instanceName.StartsWith("StarHead"))
                            {
                                NoteInstance = StarsParent.transform.Find(instanceName).gameObject;
                                sprite = StarHeadSprite;
                            }
                            else if (instanceName.StartsWith("MultiHitLine"))
                            {
                                //Debug.Log(instanceName);
                                NoteInstance = MultiHitLinesParent.transform.Find(instanceName).gameObject;
                                sprite = null;
                            }

                            // 删除Note的Animator组件（如果有）
                            Animator animator = NoteInstance.GetComponent<Animator>();
                            if (animator != null)
                            {
                                Destroy(animator);
                                // 将Note的Sprite重置为初始Sprite
                                if (sprite != null)
                                {
                                    // 获取SpriteRenderer组件
                                    SpriteRenderer spriteRenderer = NoteInstance.GetComponent<SpriteRenderer>();

                                    if (spriteRenderer != null)
                                    {
                                        // 将加载的Sprite赋值给SpriteRenderer
                                        spriteRenderer.sprite = sprite;
                                    }

                                    // Note在播放动画时，x轴缩放进行了一步放缩，需要再放缩回去
                                    Vector3 currentScale = NoteInstance.transform.localScale;
                                    currentScale.x /= AnimationScaleAdjust;
                                    NoteInstance.transform.localScale = currentScale;
                                }
                            }
                            // 更新Note位置
                            //Vector3 currentPosition = NoteInstance.transform.position;
                            //currentPosition.z = CalculateZAxisPosition(startT, ChartStartTime, chart.speedList) - CalculateZAxisPosition(currentTime, ChartStartTime, chart.speedList) + ChartParams.NoteZAxisOffset;
                            //NoteInstance.transform.position = currentPosition;

                            ResetNotePosition(NoteInstance.transform, currentTime, startT);

                            NoteInstance.SetActive(true);
                            if (startT > currentTime + ChartParams.NoteRenderTimeOffset)
                            {
                                NoteInstance.SetActive(false);
                            }
                        }
                    }

                }
                Index++;
            }
        }
        // 否则只更新所有激活状态的Note的位置
        else
        {
            UpdateActiveNotesInParent(TapsParent, zAxisDecreasePerFrame);
            UpdateActiveNotesInParent(SlidesParent, zAxisDecreasePerFrame);
            UpdateActiveNotesInParent(FlicksParent, zAxisDecreasePerFrame);
            UpdateActiveNotesInParent(FlickArrowsParent, zAxisDecreasePerFrame);
            UpdateActiveNotesInParent(HoldsParent, zAxisDecreasePerFrame);
            UpdateActiveNotesInParent(HoldOutlinesParent, zAxisDecreasePerFrame);
            UpdateActiveNotesInParent(StarsParent, zAxisDecreasePerFrame);
            UpdateActiveNotesInParent(MultiHitLinesParent, zAxisDecreasePerFrame);
        }

    }

    // 辅助方法：更新父对象下所有激活状态的子对象位置
    private void UpdateActiveNotesInParent(GameObject parent, float zAxisDecreasePerFrame)
    {
        if (parent == null) return;

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform childTransform = parent.transform.GetChild(i);
            if (childTransform.gameObject.activeSelf) // 只更新激活状态的对象
            {
                UpdateNotePosition(childTransform, zAxisDecreasePerFrame);
            }
        }
    }

    private void ResetNotePosition(Transform childTransform, float currentTime, float startT)
    {
        GameObject keyGameObject = childTransform.gameObject;
        string instanceName = keyGameObject.name;
        // 如果是 HitEffect 物体，则跳过
        if (instanceName.Contains("HitEffect"))
        {
            return;
        }

        Vector3 currentPosition = childTransform.position;
        currentPosition.z = CalculateZAxisPosition(startT, ChartStartTime, chart.speedList) - CalculateZAxisPosition(currentTime, ChartStartTime, chart.speedList) + ChartParams.NoteZAxisOffset;
        childTransform.position = currentPosition;

        //根据 z 轴坐标设置透明度
        SetNoteAlpha(keyGameObject, currentPosition.z);

    }

    private void ResetHoldPosition(Transform childTransform, float currentTime)
    {

        GameObject NoteInstance = childTransform.gameObject;
        string instanceName = NoteInstance.name;
        Vector3 currentPosition = NoteInstance.transform.position;
        //注意Hold的Z轴坐标计算逻辑不一样
        currentPosition.z = -CalculateZAxisPosition(currentTime, ChartStartTime, chart.speedList);
        NoteInstance.transform.position = currentPosition;

        // 获取 HoldOutlinesParent 下对应的物体
        string outlineInstanceName = "HoldOutline" + instanceName.Substring(4);
        GameObject outlineGameObject = HoldOutlinesParent.transform.Find(outlineInstanceName).gameObject;
        if (outlineGameObject != null)
        {
            Vector3 outlinePosition = outlineGameObject.transform.position;
            outlinePosition.z = currentPosition.z;
            outlineGameObject.transform.position = outlinePosition;
        }
        else
        {
            Debug.LogWarning($"在 HoldOutlinesParent 下未找到对应的物体 {outlineInstanceName}");
        }

    }


    private void UpdateNotePosition(Transform childTransform, float zAxisDecreasePerFrame)
    {
        GameObject keyGameObject = childTransform.gameObject;
        string instanceName = keyGameObject.name;
        // 如果是 HitEffect 物体，则跳过
        if (instanceName.Contains("HitEffect"))
        {
            return;
        }

        Vector3 currentPosition = childTransform.position;
        currentPosition.z += zAxisDecreasePerFrame;
        childTransform.position = currentPosition;

        //根据 z 轴坐标设置透明度
        SetNoteAlpha(keyGameObject, currentPosition.z);


    }



    // 根据 z 轴坐标设置 Note 的透明度
    private void SetNoteAlpha(GameObject noteObject, float zPosition)
    {
        SpriteRenderer spriteRenderer = noteObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color currentColor = spriteRenderer.color;
            float alphaValue;
            if (zPosition < ChartParams.NoteZAxisAppearPos)
            {
                // 完全透明
                alphaValue = 0f;
            }
            else if (zPosition < ChartParams.NoteZAxisOpaquePos)
            {
                // 从透明线性地变为不透明
                float t = (zPosition - ChartParams.NoteZAxisAppearPos) / (ChartParams.NoteZAxisOpaquePos - ChartParams.NoteZAxisAppearPos);
                alphaValue = t;
                // 打印物体名称和对应的透明度
                //Debug.Log($"物体名称: {noteObject.name}, 透明度: {alphaValue}");
            }
            else
            {
                // 完全不透明
                alphaValue = 1f;
            }
            currentColor.a = alphaValue;
            spriteRenderer.color = currentColor;
        }
    }

    // 播放动画的方法，设置Animator的动画控制器并播放指定动画
    private void PlayAnimation(string instanceName, string animationName)
    {
        GameObject keyGameObject = null;
        if (instanceName.StartsWith("Tap"))
        {
            keyGameObject = TapsParent.transform.Find(instanceName).gameObject;
        }
        else if (instanceName.StartsWith("Slide"))
        {
            keyGameObject = SlidesParent.transform.Find(instanceName).gameObject;
        }
        else if (instanceName.StartsWith("Flick"))
        {
            keyGameObject = FlicksParent.transform.Find(instanceName).gameObject;
        }
        else if (instanceName.StartsWith("StarHead"))
        {
            keyGameObject = StarsParent.transform.Find(instanceName).gameObject;
        }

        if (keyGameObject != null)
        {
            // 复制原物体的坐标和缩放，将z轴坐标改为0
            Vector3 position = keyGameObject.transform.position;
            position.z = 0;
            Vector3 scale = keyGameObject.transform.localScale;

            // 获取原物体的父物体和图层
            Transform parent = keyGameObject.transform.parent;
            //int layer = keyGameObject.layer;
            //这里统一用JudgeLine图层（10）
            int layer = 10;

            // 提取原物体名称中的数字部分
            string numberPart = "";
            int startIndex = 0;
            for (int i = 0; i < instanceName.Length; i++)
            {
                if (char.IsDigit(instanceName[i]))
                {
                    startIndex = i;
                    numberPart = instanceName.Substring(startIndex);
                    break;
                }
            }
            string baseName = instanceName.Substring(0, startIndex);

            // 创建一个新的挂载SpriteRenderer的游戏物体
            string newName = baseName + "HitEffect" + numberPart;
            GameObject newGameObject = new GameObject(newName);
            SpriteRenderer spriteRenderer = newGameObject.AddComponent<SpriteRenderer>();

            // 设置新物体的父物体、位置、缩放和图层
            newGameObject.transform.SetParent(parent);
            newGameObject.transform.position = position;
            newGameObject.transform.localScale = scale;
            newGameObject.layer = layer;

            // 设为非激活状态
            keyGameObject.SetActive(false);

            Animator animator = newGameObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = newGameObject.AddComponent<Animator>();
            }


            // 临时调整一下动画效果的缩放，用以跟Note大小匹配
            Vector3 currentScale = newGameObject.transform.localScale;
            currentScale.x *= AnimationScaleAdjust;
            currentScale.y = 1; // 将y轴缩放固定为1
            newGameObject.transform.localScale = currentScale;

            // 判断是否是Flick类型的键，如果是则先删除其所有子物体（如FlickArrow）
            if (instanceName.StartsWith("Flick"))
            {
                string flickNumberPart = instanceName.Substring(5);
                if (int.TryParse(flickNumberPart, out int flickIndex))
                {
                    GameObject gameobject = FlickArrowsParent.transform.Find($"FlickArrow{flickIndex}").gameObject;
                    if (gameobject != null)
                    {
                        gameobject.SetActive(false);
                    }
                }
            }

            // 在播放动画前将物体的Shader替换为Sprites/Default
            if (spriteRenderer != null)
            {
                spriteRenderer.material.shader = Shader.Find("Sprites/Default");
            }

            // 根据不同的动画名称加载对应的动画控制器（这里假设路径和名称是固定的，需按实际调整）
            RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");

            if (controller == null)
            {
                Debug.LogError($"无法加载 {animationName} 的AnimatorController资源，请检查资源路径和文件是否存在！");
            }
            animator.runtimeAnimatorController = controller;

            // 正常播放动画
            animator.Play("TapHitEffect");
            // 检查动画是否播放完毕
            StartCoroutine(CheckAnimationEnd(animator, newGameObject));
        }
    }

    // 协程方法用于检查动画是否播放完毕，播放完毕后销毁对应的游戏物体（这里统一处理，根据实际情况可调整）
    private IEnumerator CheckAnimationEnd(Animator animator, GameObject keyGameObject)
    {
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        keyGameObject.SetActive(false);
    }

    public void ResetAllNotes(float currentTime)
    {
        //Debug.Log($"更新所有Note位置：{currentTime}");
        //重置当前播放Star音效对象为null
        currentPlayingStar = null;

        //重置所有JudgePlane的激活状态
        for (int i = 0; i < JudgePlanesParent.transform.childCount; i++)
        {
            float endT = JudgePlanesEndT[i];
            if (endT >= currentTime)
            {
                // 激活 JudgePlane
                var judgePlaneObject = JudgePlanesParent.transform.GetChild(i);
                if (judgePlaneObject != null)
                {
                    judgePlaneObject.gameObject.SetActive(true);
                }

                // 激活对应的 LeftColorLine
                if (ColorLinesParent.transform.childCount > i * 2)
                {
                    var leftColorLineObject = ColorLinesParent.transform.GetChild(i * 2);
                    if (leftColorLineObject != null)
                    {
                        leftColorLineObject.gameObject.SetActive(true);
                    }
                }

                // 激活对应的 RightColorLine
                if (ColorLinesParent.transform.childCount > i * 2 + 1)
                {
                    var rightColorLineObject = ColorLinesParent.transform.GetChild(i * 2 + 1);
                    if (rightColorLineObject != null)
                    {
                        rightColorLineObject.gameObject.SetActive(true);
                    }
                }
            }
        }


        //重置所有Note的判定和激活状态
        //将所有已经判定的Note状态设定为未激活
        JudgementIndex = 0;
        while (JudgementIndex < startTimeToInstanceNames.Count && startTimeToInstanceNames.ElementAt(JudgementIndex).Key < currentTime)
        {
            List<string> instanceNames = startTimeToInstanceNames.ElementAt(JudgementIndex).Value;
            foreach (var instanceName in instanceNames)
            {
                if (!instanceName.StartsWith("JudgePlane"))
                {
                    KeyInfo keyInfo = keyReachedJudgment[instanceName];
                    keyInfo.isJudged = true;
                }
                // 设置该Note为非激活状态
                GameObject NoteInstance = null;
                if (instanceName.StartsWith("Tap"))
                {
                    NoteInstance = TapsParent.transform.Find(instanceName).gameObject;
                }
                else if (instanceName.StartsWith("Slide"))
                {
                    NoteInstance = SlidesParent.transform.Find(instanceName).gameObject;
                }
                else if (instanceName.StartsWith("Flick"))
                {
                    //Transform flickTransform = FlicksParent.transform.Find(instanceName);

                    NoteInstance = FlicksParent.transform.Find(instanceName).gameObject;

                    string numberPart = instanceName.Substring(5);
                    if (int.TryParse(numberPart, out int flickIndex))
                    {
                        GameObject gameobject = FlickArrowsParent.transform.Find($"FlickArrow{flickIndex}").gameObject;
                        if (gameobject != null)
                        {
                            gameobject.SetActive(false);
                        }
                    }

                }
                else if (instanceName.StartsWith("StarHead"))
                {
                    NoteInstance = StarsParent.transform.Find(instanceName).gameObject;
                }
                if (NoteInstance != null)
                {
                    NoteInstance.SetActive(false);
                }

            }
            JudgementIndex++;
        }

        //将所有未判定的note的keyInfo设置为未判定
        int notJudgedIndex = JudgementIndex;
        while (notJudgedIndex < startTimeToInstanceNames.Count && startTimeToInstanceNames.ElementAt(notJudgedIndex).Key >= currentTime)
        {
            List<string> instanceNames = startTimeToInstanceNames.ElementAt(notJudgedIndex).Value;
            foreach (var instanceName in instanceNames)
            {
                if (!instanceName.StartsWith("JudgePlane"))
                {
                    KeyInfo keyInfo = keyReachedJudgment[instanceName];
                    keyInfo.isJudged = false;
                }
                notJudgedIndex++;
            }
        }

        //Hold只要没到达结束时间就设置为激活
        int holdIndex = 0;
        while (holdIndex < holdTimes.Count)
        {
            string instanceName = holdTimes.ElementAt(holdIndex).Key;
            GameObject NoteInstance = HoldsParent.transform.Find(instanceName).gameObject;
            // 获取 HoldOutlinesParent 下对应的物体
            string outlineInstanceName = "HoldOutline" + instanceName.Substring(4);
            GameObject outlineGameObject = HoldOutlinesParent.transform.Find(outlineInstanceName).gameObject;

            if (NoteInstance != null & outlineGameObject != null)
            {
                float startT = holdTimes.ElementAt(holdIndex).Value[0];
                float endT = holdTimes.ElementAt(holdIndex).Value[1];
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                //如果未开始判定
                bool shouldBeActive;
                if (currentTime <= startT)
                {
                    keyInfo.isJudged = false;
                    keyInfo.isSoundPlayedAtStart = false;
                    keyInfo.isSoundPlayedAtEnd = false;
                    shouldBeActive = true;
                }
                //如果处于判定中
                else if (currentTime > startT && currentTime < endT)
                {
                    keyInfo.isJudged = true;
                    keyInfo.isSoundPlayedAtStart = true;
                    keyInfo.isSoundPlayedAtEnd = false;
                    shouldBeActive = true;
                }
                //如果结束判定
                else if (currentTime >= endT)
                {
                    keyInfo.isJudged = true;
                    keyInfo.isSoundPlayedAtStart = true;
                    keyInfo.isSoundPlayedAtEnd = true;
                    shouldBeActive = false;
                }
                else
                {
                    shouldBeActive = false;
                }

                NoteInstance.SetActive(shouldBeActive);
                outlineGameObject.SetActive(shouldBeActive);
                //// 同步子物体的激活状态
                //for (int i = 0; i < NoteInstance.transform.childCount; i++)
                //{
                //    NoteInstance.transform.GetChild(i).gameObject.SetActive(shouldBeActive);
                //}

                //将所有HoldHitEffect效果设置为非激活
                Transform holdHitEffectTransform = HoldsParent.transform.Find($"HoldHitEffect{holdIndex}");
                if (holdHitEffectTransform != null)
                {
                    GameObject gameobject = holdHitEffectTransform.gameObject;
                    gameobject.SetActive(false);
                }
            }

            holdIndex++;
        }

        //将所有尚未判定且位于判定区的Note状态设定为已激活
        ActiveStateIndex = 0;
        while (ActiveStateIndex < startTimeToInstanceNames.Count && startTimeToInstanceNames.ElementAt(ActiveStateIndex).Key < currentTime + ChartParams.NoteRenderTimeOffset)
        {
            float startT = startTimeToInstanceNames.ElementAt(ActiveStateIndex).Key;
            //Debug.Log(startT);
            List<string> instanceNames = startTimeToInstanceNames.ElementAt(ActiveStateIndex).Value;
            foreach (var instanceName in instanceNames)
            {
                if (instanceName.StartsWith("JudgePlane"))
                {
                    GameObject Instance = JudgePlanesParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetJudgePlanePosition(currentTime, Instance.transform);
                }
                else if (instanceName.StartsWith("Tap"))
                {
                    GameObject Instance = TapsParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);
                }
                else if (instanceName.StartsWith("Slide"))
                {
                    GameObject Instance = SlidesParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);
                }
                else if (instanceName.StartsWith("Flick"))
                {
                    GameObject Instance = FlicksParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);

                    //对应的FlickArrow
                    string numberPart = instanceName.Substring(5);
                    if (int.TryParse(numberPart, out int flickIndex))
                    {
                        GameObject gameobject = FlickArrowsParent.transform.Find($"FlickArrow{flickIndex}").gameObject;
                        gameobject.SetActive(true);
                        ResetNotePosition(gameobject.transform, currentTime, startT);
                    }
                }
                else if (instanceName.StartsWith("Hold"))
                {
                    GameObject Instance = HoldsParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetHoldPosition(Instance.transform, currentTime);
                }
                else if (instanceName.StartsWith("StarHead"))
                {
                    GameObject Instance = StarsParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);
                }
                else if (instanceName.StartsWith("MultiHitLine"))
                {
                    GameObject Instance = MultiHitLinesParent.transform.Find(instanceName).gameObject;
                    Instance.SetActive(true);
                    ResetNotePosition(Instance.transform, currentTime, startT);
                }
            }
            ActiveStateIndex++;
        }


        //重置判定文本编号
        JudgeTextureIndex = 0;
        while (JudgeTextureIndex < JudgePosMap.Count && JudgePosMap.ElementAt(JudgeTextureIndex).Key < currentTime)
        { JudgeTextureIndex += 1; }


        //更新所有谱面元素位置和颜色状态
        Updateall(true);

    }

    private void LoadNoteSprites()
    {
        // 加载 Tap 音符的 Sprite
        TapSprite = Resources.Load<Sprite>("Textures/Gameplay/Note/TapNote");
        // 加载 Slide 音符的 Sprite
        SlideSprite = Resources.Load<Sprite>("Textures/Gameplay/Note/SlideNote");
        // 加载 Flick 音符的 Sprite
        FlickSprite = Resources.Load<Sprite>("Textures/Gameplay/Note/FlickNote");
        // 加载 StarHead 音符的 Sprite
        StarHeadSprite = Resources.Load<Sprite>("Textures/Gameplay/Note/StarHead");

        // 加载判定文字的Sprite
        Sync = Resources.Load<Sprite>("Textures/Gameplay/Particles/TextSync");
        Link = Resources.Load<Sprite>("Textures/Gameplay/Particles/TextLink");
        Fuzz = Resources.Load<Sprite>("Textures/Gameplay/Particles/TextFuzz");
        Null = Resources.Load<Sprite>("Textures/Gameplay/Particles/TextNull");

        // 检查是否成功加载
        if (TapSprite == null)
        {
            Debug.LogError("Failed to load TapNote sprite.");
        }
        if (SlideSprite == null)
        {
            Debug.LogError("Failed to load SlideNote sprite.");
        }
        if (FlickSprite == null)
        {
            Debug.LogError("Failed to load FlickNote sprite.");
        }
        if (StarHeadSprite == null)
        {
            Debug.LogError("Failed to load StarHead sprite.");
        }
    }

}

