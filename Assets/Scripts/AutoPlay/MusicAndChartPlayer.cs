using UnityEngine;
//using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Params;
using System.Collections;
using UnityEngine.SceneManagement;
using Note;
using static Utility;

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
    private GameObject StarsParent;
    public GameObject SubStarsParent;
    public GameObject MusicSlider;

    private Sprite TapSprite;
    private Sprite SlideSprite;
    private Sprite FlickSprite;
    private Sprite StarHeadSprite;

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
    // 记录当前正在播放音频的星星实例名
    private string currentPlayingStar = null;
    // 记录开始音量衰减的时间
    private float fadeOutStartTime = -1f;
    // 记录当前正在播放的星星音频的初始音量
    //private float initialStarSoundVolume;

    // 新增一个列表用于存储当前正在处理的Hold对应的instanceName
    private List<string> currentHoldInstanceNames = new List<string>();
    //private bool isMusicPlaying; // 用于标识是否已经开始音乐播放阶段
    //private bool isPaused = false;
    private Chart chart; // 用于存储传入的Chart实例，方便在Update里使用
    private int currentIndex = 0;
    private float totalTime;
    //private float AnimationScaleAdjust = 772f / 165f;  // 播放动画时x轴缩放调整
    private float AnimationScaleAdjust = 0.8f;  // 播放动画时x轴缩放调整
    private float HoldAnimationScaleAdjust = 0.7f;  // 播放Hold动画时x轴缩放调整

    // 内部类，用于存储键的相关信息以及判定状态
    private class KeyInfo
    {
        public float startT;
        public bool isJudged;
        public bool isSoundPlayedAtStart;
        public bool isSoundPlayedAtEnd;
        public KeyInfo(float startTime)
        {
            startT = startTime;
            isJudged = false;
            isSoundPlayedAtStart = false;
            isSoundPlayedAtEnd = false;
        }
    }

    // 新增的公共方法，用于接收各个参数并赋值给对应的私有变量，添加了SlidesParent和SlideSoundEffect参数
    public void SetParameters(AudioSource audioSource, GameObject judgePlanesParent, GameObject judgeLinesParent, GameObject colorLinesParent,
        GameObject tapsParent, GameObject slidesParent, GameObject flicksParent, GameObject flickarrowsParent, GameObject holdsParent, GameObject starsParent, GameObject substarsParent,
        AudioSource tapSoundEffect, AudioSource slideSoundEffect, AudioSource flickSoundEffect, AudioSource holdSoundEffect, AudioSource starheadSoundEffect, AudioSource starSoundEffect,
        Chart chart)
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
        StarsParent = starsParent;
        SubStarsParent = substarsParent;
        TapSoundEffect = tapSoundEffect;
        SlideSoundEffect = slideSoundEffect;
        FlickSoundEffect = flickSoundEffect;
        HoldSoundEffect = holdSoundEffect;
        StarHeadSoundEffect = starheadSoundEffect;
        StarSoundEffect = starSoundEffect;
        this.chart = chart;
        totalTime = audioSource.clip.length;
        //Debug.Log(FlickSoundEffect);
    }

    public void PlayMusicAndChart(Chart chart)
    {

        pauseManager = GetComponent<PauseManager>();
        MusicSlider = GameObject.Find("MusicSlider");
        MusicSlider.SetActive(false);
        //Debug.Log(MusicSlider);
        // 提前加工Chart里所有键（Tap和Slide等）的startT与对应实例名的映射关系，并按照startT排序
        PrepareChartMapping(chart);
        subStarInfoDict = Star.InitializeSubStarInfo(chart, SubStarsParent);
        LoadNoteSprites();
        audioSource.Play();
        //isMusicPlaying = true;
        pauseManager.isPaused = false;
        //AddListenerToButton();
        StartCoroutine(UpdatePositionsCoroutine());
    }

    private IEnumerator UpdatePositionsCoroutine()
    {
        //float audioPrevTime = 0f;

        while (true)
        {
            if (audioSource.isPlaying && !pauseManager.isPaused)
            {
                Updateall();
                //audioPrevTime = audioSource.time;
            }
            else if (!audioSource.isPlaying && !pauseManager.isPaused)
            {
                Debug.Log("Music has ended. Loading SongSelect scene...");
                SceneManager.LoadScene("SongSelect");
                // 在这里添加 yield break 来停止协程，因为场景已经切换，协程不需要继续运行
                yield break;
            }

            yield return new WaitForSeconds(FrameParams.updateInterval);
        }
    }

    private void Updateall()
    {
        // 只有当明确进入音乐播放阶段才进行后续判断
        if (audioSource.isPlaying)
        {
            float currentTime = audioSource.time;
            //Debug.Log(currentTime);

            // 将所有已经结束的 JudgePlane（endT 小于 currentTime）设置为 setActive(false)
            for (int i = 0; i < JudgePlanesParent.transform.childCount; i++)
            {
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

            // 使用双指针来维护从上一帧到这一帧的时间，包含哪些note
            while (currentIndex < startTimeToInstanceNames.Count && startTimeToInstanceNames.ElementAt(currentIndex).Key < currentTime)
            {
                List<string> instanceNames = startTimeToInstanceNames.ElementAt(currentIndex).Value;
                foreach (var instanceName in instanceNames)
                {
                    //Debug.Log(instanceName);
                    KeyInfo keyInfo = keyReachedJudgment[instanceName];
                    keyInfo.isJudged = true;
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
                        //TapSoundEffect.Play();
                        //Debug.Log(FlickSoundEffect.clip);
                        //FlickSoundEffect.enabled = true;
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
                }
                currentIndex++;
            }
            //更新星星划动音频的状态
            UpdateStarSoundEffect(currentTime);
            // 统一处理当前所有Hold的状态更新（开始、持续、结束判定）
            UpdateHoldStates(currentTime);
            //更新所有Note位置
            UpdatePositions(currentTime);
            //更新所有Arrow的透明度
            CheckArrowVisibility(SubStarsParent, currentTime, subStarInfoDict);
            //Debug.Log("2. " + audioTime);
        }
    }

    private void Update()
    {
        // 检查鼠标点击
        if (Input.GetMouseButtonDown(0))
        {
            pauseManager.CheckPauseButtonClick();
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
                float elapsedTime = currentTime - fadeOutStartTime;
                if (elapsedTime < SoundParams.starSoundFadeOutTime)
                {
                    // 线性降低音量
                    float newVolume = 1 - elapsedTime / SoundParams.starSoundFadeOutTime;
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

    private void PlayHoldAnimation(string instanceName, float currentTime)
    {
        //hold游戏物体实例
        GameObject holdObject = GameObject.Find(instanceName);
        // 从instanceName中提取数字部分，用于查找对应的Hold
        int holdIndex = GetHoldIndexFromInstanceName(instanceName);
        if (holdIndex >= 0 && holdIndex < chart.holds.Count)
        {
            Hold hold = chart.holds[holdIndex];
            KeyInfo holdKeyInfo = keyReachedJudgment[instanceName];
            float holdStartTime = holdTimes[instanceName][0];
            float holdEndTime = holdTimes[instanceName][1];
            bool isStart = currentTime >= holdStartTime && currentTime < holdEndTime;
            bool isEnd = currentTime >= holdEndTime;
            //Debug.Log($"InstanceName: {instanceName}, currentTime: {currentTime}, isStart: {isStart}, isEnd: {isEnd}");

            // 获取Subhold的X轴左侧和右侧坐标
            float subholdLeftX = hold.GetCurrentSubHoldLeftX(currentTime);
            float subholdRightX = hold.GetCurrentSubHoldRightX(currentTime);
            // 计算X轴坐标均值
            float x = (subholdLeftX + subholdRightX) / 2;

            JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
            float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(hold.GetFirstSubHoldStartTime());
            // 对yAxisPosition进行TransformYCoordinate变换
            yAxisPosition = TransformYCoordinate(yAxisPosition);
            //Debug.Log(yAxisPosition);
            Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
            float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);
            //Debug.Log(worldUnitToScreenPixelX);
            float startXWorld = worldUnitToScreenPixelX * x / ChartParams.XaxisMax;
            float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;
            //Debug.Log(noteSizeWorldLengthPerUnit);

            float x_width = noteSizeWorldLengthPerUnit * HoldAnimationScaleAdjust * (subholdRightX - subholdLeftX);
            //Debug.Log(x_width);

            // 获取所在JudgePlane当时刻坐标
            float y = 0f;
            JudgePlane correspondingJudgePlane = chart.GetCorrespondingJudgePlaneBasedOnTime(currentTime, hold);
            if (correspondingJudgePlane != null)
            {
                y = correspondingJudgePlane.GetPlaneYAxis(currentTime);
                // 对y进行TransformYCoordinate变换
                y = TransformYCoordinate(y);
            }
            //HoldHitEffect的位置（注意挂载在Hold物体下，需要根据父物体坐标折算子物体相对坐标）
            Vector3 holdHitEffectPosition = new Vector3(-startXWorld, y, 0f);

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
            GameObject holdObject = GameObject.Find(instanceName);
            int holdIndex = GetHoldIndexFromInstanceName(instanceName);
            if (holdIndex >= 0 && holdIndex < chart.holds.Count)
            {
                Hold hold = chart.holds[holdIndex];
                KeyInfo holdKeyInfo = keyReachedJudgment[instanceName];
                float holdStartTime = holdTimes[instanceName][0];
                float holdEndTime = holdTimes[instanceName][1];
                bool isStart = currentTime >= holdStartTime && currentTime < holdEndTime;
                bool isEnd = currentTime >= holdEndTime;

                float subholdLeftX = hold.GetCurrentSubHoldLeftX(currentTime);
                float subholdRightX = hold.GetCurrentSubHoldRightX(currentTime);
                float x = (subholdLeftX + subholdRightX) / 2;

                JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
                float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(currentTime);
                // 对yAxisPosition进行TransformYCoordinate变换
                yAxisPosition = TransformYCoordinate(yAxisPosition);
                Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);
                float startXWorld = worldUnitToScreenPixelX * x / ChartParams.XaxisMax;
                float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                float x_width = noteSizeWorldLengthPerUnit * HoldAnimationScaleAdjust * (subholdRightX - subholdLeftX);

                JudgePlane correspondingJudgePlane = chart.GetCorrespondingJudgePlaneBasedOnTime(currentTime, hold);
                float y = correspondingJudgePlane.GetPlaneYAxis(currentTime);
                // 对y进行TransformYCoordinate变换
                y = TransformYCoordinate(y);
                Vector3 holdHitEffectPosition = new Vector3(-startXWorld, y, 0f);

                //判定结束
                if (isEnd && !holdKeyInfo.isSoundPlayedAtEnd)
                {
                    HoldSoundEffect.Play();
                    holdKeyInfo.isSoundPlayedAtEnd = true;
                    // 如果已经结束判定，从列表中移除该instanceName
                    currentHoldInstanceNames.RemoveAt(i);


                    //Debug.Log(holdHitEffectPosition);
                    //结束时的特效缩放由holdEndTime对应X轴宽度决定
                    subholdLeftX = hold.GetCurrentSubHoldLeftX(holdEndTime);
                    subholdRightX = hold.GetCurrentSubHoldRightX(holdEndTime);
                    x = (subholdLeftX + subholdRightX) / 2;
                    startXWorld = worldUnitToScreenPixelX * x / ChartParams.XaxisMax;
                    holdHitEffectPosition = new Vector3(-startXWorld, y, 0f);
                    x_width = noteSizeWorldLengthPerUnit * HoldAnimationScaleAdjust * (subholdRightX - subholdLeftX);


                    GameObject holdHitEffect = GameObject.Find($"HoldHitEffect{holdIndex + 1}");
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
                            PlayAnimation($"HoldHitEffect{holdIndex + 1}", "HoldEffect");

                        }
                    }
                }

                //在判定过程中
                else if (isStart && !isEnd)
                {
                    //Debug.Log(x_width);
                    GameObject holdHitEffect = GameObject.Find($"HoldHitEffect{holdIndex + 1}");
                    if (holdHitEffect != null)
                    {
                        holdHitEffect.transform.position = holdHitEffectPosition;
                        holdHitEffect.transform.localScale = new Vector3(x_width, 1, 1);
                    }
                }
            }
        }
    }

    // 辅助方法，从instanceName中提取数字部分，用于查找对应的Hold索引
    private int GetHoldIndexFromInstanceName(string instanceName)
    {
        if (instanceName.StartsWith("Hold") && int.TryParse(instanceName.Substring(4), out int index))
        {
            return index - 1;
        }
        return -1;
    }

    private void PrepareChartMapping(Chart chart)
    {
        List<KeyValuePair<float, string>> allPairs = new List<KeyValuePair<float, string>>();


        if (chart.judgePlanes != null)
        {
            for (int i = 0; i < chart.judgePlanes.Count; i++)
            {
                var judgePlane = chart.judgePlanes[i];
                float startT = judgePlane.subJudgePlaneList[0].startT;
                float endT = judgePlane.subJudgePlaneList[judgePlane.subJudgePlaneList.Count - 1].endT;
                JudgePlanesStartT.Add(startT);
                JudgePlanesEndT.Add(endT);
                //string instanceName = $"JudgePlane{i + 1}";
                //allPairs.Add(new KeyValuePair<float, string>(startT, instanceName));
                //keyReachedJudgment[instanceName] = new KeyInfo(startT);
            }
        }

        if (chart.taps != null)
        {
            for (int i = 0; i < chart.taps.Count; i++)
            {
                var tap = chart.taps[i];
                string instanceName = $"Tap{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(tap.startT, instanceName));
                keyReachedJudgment[instanceName] = new KeyInfo(tap.startT);
            }
        }
        if (chart.slides != null)
        {
            for (int i = 0; i < chart.slides.Count; i++)
            {
                var slide = chart.slides[i];
                string instanceName = $"Slide{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(slide.startT, instanceName));
                keyReachedJudgment[instanceName] = new KeyInfo(slide.startT);
            }
        }
        if (chart.flicks != null)
        {
            for (int i = 0; i < chart.flicks.Count; i++)
            {
                var flick = chart.flicks[i];
                string instanceName = $"Flick{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(flick.startT, instanceName));
                keyReachedJudgment[instanceName] = new KeyInfo(flick.startT);
            }
        }
        if (chart.holds != null)
        {
            for (int i = 0; i < chart.holds.Count; i++)
            {
                var hold = chart.holds[i];
                float startT = hold.GetFirstSubHoldStartTime();
                float endT = hold.GetLastSubHoldEndTime();
                string instanceName = $"Hold{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(startT, instanceName));
                keyReachedJudgment[instanceName] = new KeyInfo(startT);
                if (!holdTimes.ContainsKey(instanceName))
                {
                    holdTimes[instanceName] = new List<float>();
                }
                // 记录Hold的开始和结束时间
                holdTimes[instanceName].Add(startT);
                holdTimes[instanceName].Add(endT);
            }
        }

        if (chart.stars != null)
        {
            for (int i = 0; i < chart.stars.Count; i++)
            {
                var star = chart.stars[i];
                float startT = star.starHeadT;
                //存储星星头判定时间
                string instanceName = $"StarHead{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(startT, instanceName));
                keyReachedJudgment[instanceName] = new KeyInfo(startT);
                // 记录每个星星的划动开始和结束时间
                float starstartT = star.subStarList[0].starTrackStartT;
                float starendT = star.subStarList[star.subStarList.Count - 1].starTrackEndT;
                starTrackTimes[instanceName] = (starstartT, starendT);
            }
        }

        // 按照startT进行排序
        allPairs = allPairs.OrderBy(pair => pair.Key).ToList();

        foreach (var pair in allPairs)
        {
            float startTime = pair.Key;
            string instanceName = pair.Value;
            if (!startTimeToInstanceNames.ContainsKey(startTime))
            {
                startTimeToInstanceNames[startTime] = new List<string>();
            }
            startTimeToInstanceNames[startTime].Add(instanceName);
            //Debug.Log(startTimeToInstanceNames[startTime]);
        }
    }

    private void UpdatePositions(float currentTime)
    {
        // 计算每帧对应的 Z 轴坐标减少量，假设帧率是固定的（比如 60 帧每秒），然后根据每秒钟变化单位计算每帧变化量
        float audioTimeDelta = currentTime - audioPrevTime;
        float zAxisDecreasePerFrame = SpeedParams.NoteSpeedDefault * audioTimeDelta;

        UpdateJudgePlanesPosition(currentTime, zAxisDecreasePerFrame);
        UpdateJudgeLinesPosition(currentTime, zAxisDecreasePerFrame);
        UpdateNotesPosition(zAxisDecreasePerFrame);

        audioPrevTime = currentTime;
    }

    private void UpdateJudgePlanesPosition(float currentTime, float zAxisDecreasePerFrame)
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
                currentJudgePlane.ChangeSubJudgePlaneTransparency(JudgePlanesParent, YAxis);
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

    private void UpdateJudgeLinesPosition(float currentTime, float zAxisDecreasePerFrame)
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

    //private void UpdateNotesPosition(float zAxisDecreasePerFrame)
    //{
    //    // 处理 Taps 键
    //    if (TapsParent != null)
    //    {
    //        for (int i = 0; i < TapsParent.transform.childCount; i++)
    //        {
    //            Transform childTransform = TapsParent.transform.GetChild(i);
    //            GameObject keyGameObject = childTransform.gameObject;
    //            string instanceName = keyGameObject.name;
    //            KeyInfo keyInfo = keyReachedJudgment[instanceName];
    //            if (!keyInfo.isJudged)
    //            {
    //                Vector3 currentPosition = childTransform.position;
    //                currentPosition.z += zAxisDecreasePerFrame;
    //                childTransform.position = currentPosition;

    //                // 根据 z 轴坐标设置透明度
    //                SetNoteAlpha(keyGameObject, currentPosition.z);
    //            }
    //        }
    //    }

    //    // 处理 Slides 键
    //    if (SlidesParent != null)
    //    {
    //        for (int i = 0; i < SlidesParent.transform.childCount; i++)
    //        {
    //            Transform childTransform = SlidesParent.transform.GetChild(i);
    //            GameObject keyGameObject = childTransform.gameObject;
    //            string instanceName = keyGameObject.name;
    //            KeyInfo keyInfo = keyReachedJudgment[instanceName];
    //            if (!keyInfo.isJudged)
    //            {
    //                Vector3 currentPosition = childTransform.position;
    //                currentPosition.z += zAxisDecreasePerFrame;
    //                childTransform.position = currentPosition;

    //                // 根据 z 轴坐标设置透明度
    //                SetNoteAlpha(keyGameObject, currentPosition.z);
    //            }
    //        }
    //    }

    //    // 处理 Flicks 键
    //    if (FlicksParent != null)
    //    {
    //        for (int i = 0; i < FlicksParent.transform.childCount; i++)
    //        {
    //            Transform childTransform = FlicksParent.transform.GetChild(i);
    //            GameObject keyGameObject = childTransform.gameObject;
    //            string instanceName = keyGameObject.name;
    //            KeyInfo keyInfo = keyReachedJudgment[instanceName];
    //            if (!keyInfo.isJudged)
    //            {
    //                Vector3 currentPosition = childTransform.position;
    //                currentPosition.z += zAxisDecreasePerFrame;
    //                childTransform.position = currentPosition;

    //                //根据 z 轴坐标设置透明度
    //                SetNoteAlpha(keyGameObject, currentPosition.z);
    //            }
    //        }
    //    }

    //    // 处理 FlickArrows 键
    //    if (FlickArrowsParent != null)
    //    {
    //        for (int i = 0; i < FlickArrowsParent.transform.childCount; i++)
    //        {
    //            Transform childTransform = FlickArrowsParent.transform.GetChild(i);
    //            GameObject keyGameObject = childTransform.gameObject;
    //            string instanceName = keyGameObject.name;
    //            Vector3 currentPosition = childTransform.position;
    //            currentPosition.z += zAxisDecreasePerFrame;
    //            childTransform.position = currentPosition;

    //            //根据 z 轴坐标设置透明度
    //            SetNoteAlpha(keyGameObject, currentPosition.z);
    //        }
    //    }

    //    // 处理 Hold 键
    //    if (HoldsParent != null)
    //    {
    //        for (int i = 0; i < HoldsParent.transform.childCount; i++)
    //        {
    //            Transform childTransform = HoldsParent.transform.GetChild(i);
    //            GameObject holdGameObject = childTransform.gameObject;
    //            string instanceName = holdGameObject.name;
    //            // 如果是 HoldHitEffect 物体，则跳过
    //            if (!instanceName.StartsWith("HoldHitEffect"))
    //            {
    //                KeyInfo keyInfo = keyReachedJudgment[instanceName];

    //                // Hold 无论是否判定，位置都要更新
    //                Vector3 currentPosition = childTransform.position;
    //                currentPosition.z += zAxisDecreasePerFrame;
    //                childTransform.position = currentPosition;

    //                //根据 z 轴坐标设置透明度
    //                SetNoteAlpha(holdGameObject, currentPosition.z);
    //            }
    //        }
    //    }

    //    // 处理 StarHead 键
    //    if (StarsParent != null)
    //    {
    //        for (int i = 0; i < StarsParent.transform.childCount; i++)
    //        {
    //            Transform childTransform = StarsParent.transform.GetChild(i);
    //            GameObject starHeadGameObject = childTransform.gameObject;
    //            string instanceName = starHeadGameObject.name;
    //            KeyInfo keyInfo = keyReachedJudgment[instanceName];
    //            if (instanceName.StartsWith("StarHead") && !keyInfo.isJudged)
    //            {
    //                Vector3 currentPosition = childTransform.position;
    //                currentPosition.z += zAxisDecreasePerFrame;
    //                childTransform.position = currentPosition;

    //                //根据 z 轴坐标设置透明度
    //                SetNoteAlpha(starHeadGameObject, currentPosition.z);
    //            }
    //        }
    //    }
    //}
    private void UpdateNotesPosition(float zAxisDecreasePerFrame)
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
                if (instanceName.Contains("HitEffect"))
                {
                    continue;
                }
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                if (!keyInfo.isJudged)
                {
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;

                    // 根据 z 轴坐标设置透明度
                    SetNoteAlpha(keyGameObject, currentPosition.z);
                }
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
                if (instanceName.Contains("HitEffect"))
                {
                    continue;
                }
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                if (!keyInfo.isJudged)
                {
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;

                    // 根据 z 轴坐标设置透明度
                    SetNoteAlpha(keyGameObject, currentPosition.z);
                }
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
                if (instanceName.Contains("HitEffect"))
                {
                    continue;
                }
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                if (!keyInfo.isJudged)
                {
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;

                    //根据 z 轴坐标设置透明度
                    SetNoteAlpha(keyGameObject, currentPosition.z);
                }
            }
        }

        // 处理 FlickArrows 键
        if (FlickArrowsParent != null)
        {
            for (int i = 0; i < FlickArrowsParent.transform.childCount; i++)
            {
                Transform childTransform = FlickArrowsParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;
                string instanceName = keyGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect"))
                {
                    continue;
                }
                Vector3 currentPosition = childTransform.position;
                currentPosition.z += zAxisDecreasePerFrame;
                childTransform.position = currentPosition;

                //根据 z 轴坐标设置透明度
                SetNoteAlpha(keyGameObject, currentPosition.z);
            }
        }

        // 处理 Hold 键
        if (HoldsParent != null)
        {
            for (int i = 0; i < HoldsParent.transform.childCount; i++)
            {
                Transform childTransform = HoldsParent.transform.GetChild(i);
                GameObject holdGameObject = childTransform.gameObject;
                string instanceName = holdGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect"))
                {
                    continue;
                }
                KeyInfo keyInfo = keyReachedJudgment[instanceName];

                // Hold 无论是否判定，位置都要更新
                Vector3 currentPosition = childTransform.position;
                currentPosition.z += zAxisDecreasePerFrame;
                childTransform.position = currentPosition;

                //根据 z 轴坐标设置透明度
                SetNoteAlpha(holdGameObject, currentPosition.z);
            }
        }

        // 处理 StarHead 键
        if (StarsParent != null)
        {
            for (int i = 0; i < StarsParent.transform.childCount; i++)
            {
                Transform childTransform = StarsParent.transform.GetChild(i);
                GameObject starHeadGameObject = childTransform.gameObject;
                string instanceName = starHeadGameObject.name;
                // 如果是 HitEffect 物体，则跳过
                if (instanceName.Contains("HitEffect"))
                {
                    continue;
                }
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                if (instanceName.StartsWith("StarHead") && !keyInfo.isJudged)
                {
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;

                    //根据 z 轴坐标设置透明度
                    SetNoteAlpha(starHeadGameObject, currentPosition.z);
                }
            }
        }
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
        GameObject keyGameObject = GameObject.Find(instanceName);
        if (keyGameObject != null)
        {
            // 复制原物体的坐标和缩放，将z轴坐标改为0
            Vector3 position = keyGameObject.transform.position;
            position.z = 0;
            Vector3 scale = keyGameObject.transform.localScale;

            // 获取原物体的父物体和图层
            Transform parent = keyGameObject.transform.parent;
            int layer = keyGameObject.layer;

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

            // HoldHitEffect不需要调整大小
            if (!instanceName.StartsWith("HoldHitEffect"))
            {
                // 临时调整一下动画效果的缩放，用以跟Note大小匹配
                Vector3 currentScale = newGameObject.transform.localScale;
                currentScale.x *= AnimationScaleAdjust;
                currentScale.y = 1; // 将y轴缩放固定为1
                newGameObject.transform.localScale = currentScale;
            }

            // 判断是否是Flick类型的键，如果是则先删除其所有子物体（如FlickArrow）
            if (instanceName.StartsWith("Flick"))
            {
                string flickNumberPart = instanceName.Substring(5);
                if (int.TryParse(flickNumberPart, out int flickIndex))
                {
                    GameObject gameobject = GameObject.Find($"FlickArrow{flickIndex}");
                    if (gameobject != null)
                    {
                        gameobject.SetActive(false);
                    }
                }
            }
            // 如果是Hold类型的键，则删除对应的Hold物体
            if (instanceName.StartsWith("HoldHitEffect"))
            {
                string holdNumberPart = instanceName.Substring(13);
                if (int.TryParse(holdNumberPart, out int holdIndex))
                {
                    GameObject gameobject = GameObject.Find($"Hold{holdIndex}");
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
        currentIndex = 0;
        while (currentIndex < startTimeToInstanceNames.Count && startTimeToInstanceNames.ElementAt(currentIndex).Key < currentTime)
        {
            List<string> instanceNames = startTimeToInstanceNames.ElementAt(currentIndex).Value;
            foreach (var instanceName in instanceNames)
            {
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                // 设置当前时间前的note为已判定
                keyInfo.isJudged = true;
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
            currentIndex++;
        }

        //Hold只要没到达结束时间就设置为激活
        int holdIndex = 0;
        while (holdIndex < holdTimes.Count)
        {
            string instanceName = holdTimes.ElementAt(holdIndex).Key;
            GameObject NoteInstance = HoldsParent.transform.Find(instanceName).gameObject;
            if (NoteInstance != null)
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
                // 同步子物体的激活状态
                for (int i = 0; i < NoteInstance.transform.childCount; i++)
                {
                    NoteInstance.transform.GetChild(i).gameObject.SetActive(shouldBeActive);
                }

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

        //将所有尚未判定的Note状态设定为已激活
        int notJudgedIndex = currentIndex;
        while (notJudgedIndex < startTimeToInstanceNames.Count && startTimeToInstanceNames.ElementAt(notJudgedIndex).Key >= currentTime)
        {

            List<string> instanceNames = startTimeToInstanceNames.ElementAt(notJudgedIndex).Value;
            foreach (var instanceName in instanceNames)
            {
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                // 设置当前时间后的note为未判定
                keyInfo.isJudged = false;
                // 设置该Note为激活状态
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

                    NoteInstance = FlicksParent.transform.Find(instanceName).gameObject;

                    string numberPart = instanceName.Substring(5);
                    if (int.TryParse(numberPart, out int flickIndex))
                    {
                        GameObject gameobject = FlickArrowsParent.transform.Find($"FlickArrow{flickIndex}").gameObject;
                        if (gameobject != null)
                        {
                            gameobject.SetActive(true);
                        }
                    }
                }
                else if (instanceName.StartsWith("StarHead"))
                {
                    NoteInstance = StarsParent.transform.Find(instanceName).gameObject;
                }
                if (NoteInstance != null)
                {
                    NoteInstance.SetActive(true);
                }
            }
            notJudgedIndex++;
        }

        ResetNotesPositionAndSprite(currentTime);
        //ResetNotesSprite(currentTime);
    }

    private void ResetNotesPositionAndSprite(float currentTime)
    {
        int Index = 0;
        while (Index < startTimeToInstanceNames.Count)
        {
            float startT = startTimeToInstanceNames.ElementAt(Index).Key;
            List<string> instanceNames = startTimeToInstanceNames.ElementAt(Index).Value;
            // 判断是否是多押情况
            bool isMultiTap = instanceNames.Count > 1;

            foreach (var instanceName in instanceNames)
            {
                GameObject NoteInstance = null;
                Sprite sprite = null;
                // 设置该Note的位置和外观
                if (instanceName.StartsWith("Hold"))
                {
                    NoteInstance = HoldsParent.transform.Find(instanceName).gameObject;
                    Vector3 currentPosition = NoteInstance.transform.position;
                    //注意Hold的Z轴坐标计算逻辑不一样
                    currentPosition.z = CalculateZAxisPosition(-currentTime);
                    NoteInstance.transform.position = currentPosition;
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
                                currentPos.z = CalculateZAxisPosition(startT - currentTime);
                                gameobject.transform.position = currentPos;
                                Flick flick = chart.flicks[flickIndex - 1];
                                float flickDirection = flick.flickDirection;

                                // 提前更新Flick位置（AdjustFlickArrowPosition需要用到正确的位置）
                                currentPos = NoteInstance.transform.position;
                                currentPos.z = CalculateZAxisPosition(startT - currentTime) + ChartParams.NoteZAxisOffset;
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
                    Vector3 currentPosition = NoteInstance.transform.position;
                    currentPosition.z = CalculateZAxisPosition(startT - currentTime) + ChartParams.NoteZAxisOffset;
                    NoteInstance.transform.position = currentPosition;
                }

                // 如果是多押情况，将Shader替换为Sprites/Outline
                if (isMultiTap)
                {
                    SpriteRenderer spriteRenderer = NoteInstance.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.material.shader = Shader.Find("Sprites/Outline");
                    }
                }
            }
            Index++;
        }
        // 单独处理JudgePlane的位置和外观
        ResetJudgePlanePositionAndSprite(currentTime);
        audioPrevTime = currentTime;
    }
    private void ResetJudgePlanePositionAndSprite(float currentTime)
    {
        if (chart.judgePlanes != null)
        {
            for (int i = 0; i < chart.judgePlanes.Count; i++)
            {
                var judgePlane = chart.judgePlanes[i];
                string instanceName = $"JudgePlane{i + 1}";
                GameObject NoteInstance = JudgePlanesParent.transform.Find(instanceName).gameObject;
                Vector3 currentPosition = NoteInstance.transform.position;
                //注意JudgePlane的Z轴坐标计算逻辑不一样
                currentPosition.z = CalculateZAxisPosition(-currentTime);
                NoteInstance.transform.position = currentPosition;

                int judgePlaneId = i + 1;
                JudgePlane currentJudgePlane = chart.GetCorrespondingJudgePlane(judgePlaneId);
                float YAxis = currentJudgePlane.GetPlaneYAxis(currentTime);
                // 根据 YAxis 的值，实时改变 currentJudgePlane 下所有 SubJudgePlane 实例的透明度
                currentJudgePlane.ChangeSubJudgePlaneTransparency(JudgePlanesParent, YAxis);

                // 重置对应的 LeftColorLine 和 RightColorLine 的 z 轴位置
                ResetColorLineZPosition($"LeftColorLine{judgePlaneId}", ColorLinesParent, CalculateZAxisPosition(-currentTime));
                ResetColorLineZPosition($"RightColorLine{judgePlaneId}", ColorLinesParent, CalculateZAxisPosition(-currentTime));
            }
        }
    }

    private void ResetColorLineZPosition(string objectName, GameObject parentGameObject, float newZPosition)
    {
        GameObject colorLineObject = parentGameObject.transform.Find(objectName).gameObject;
        if (colorLineObject != null)
        {
            Vector3 currentPosition = colorLineObject.transform.position;
            currentPosition.z = newZPosition;
            colorLineObject.transform.position = currentPosition;
        }
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

