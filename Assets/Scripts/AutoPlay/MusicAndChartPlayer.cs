using UnityEngine;
//using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Params;
using System.Collections;
using UnityEngine.SceneManagement;
//using UnityEngine.Experimental.Rendering.Universal;
//using UnityEngine.Rendering.Universal;
using Note;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class MusicAndChartPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private GameObject JudgePlanesParent;
    private GameObject JudgeLinesParent;
    private GameObject TapsParent;
    private GameObject SlidesParent;
    private GameObject FlicksParent;
    private GameObject HoldsParent;
    private GameObject StarsParent;

    private AudioSource TapSoundEffect;
    private AudioSource SlideSoundEffect;
    private AudioSource FlickSoundEffect;
    private AudioSource HoldSoundEffect;
    private AudioSource StarHeadSoundEffect;

    private float audioPrevTime = 0f;
    private Dictionary<float, List<string>> startTimeToInstanceNames = new Dictionary<float, List<string>>(); // 存储startT到对应实例名列表的映射
    private Dictionary<string, float> holdEndTimes = new Dictionary<string, float>(); // 存储Hold实例名到其结束时间的映射
    private Dictionary<string, KeyInfo> keyReachedJudgment = new Dictionary<string, KeyInfo>(); // 存储实例名到键信息的映射
    private List<float> JudgePlanesStartT = new List<float>(); // 判定面的开始时间（用于JudgeLine出现时间计算
    private Dictionary<string, float> judgePlaneEndTimes = new Dictionary<string, float>();
    public List<Star.SubStar> subStars = new List<Star.SubStar>();

    // 新增一个列表用于存储当前正在处理的Hold对应的instanceName
    private List<string> currentHoldInstanceNames = new List<string>();
    private bool isMusicPlaying; // 用于标识是否已经开始音乐播放阶段
    private bool isPaused = false;
    private Chart chart; // 用于存储传入的Chart实例，方便在Update里使用
    private int currentIndex = 0;
    private float updateInterval = 0.00833333f;  // 更新间隔，固定为120帧


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
    public void SetParameters(AudioSource audioSource, GameObject judgePlanesParent, GameObject judgeLinesParent,
        GameObject tapsParent, GameObject slidesParent, GameObject flicksParent, GameObject holdsParent, GameObject starsParent,
        AudioSource tapSoundEffect, AudioSource slideSoundEffect, AudioSource flickSoundEffect, AudioSource holdSoundEffect, AudioSource starheadSoundEffect,
        Chart chart)
    {
        this.audioSource = audioSource;
        JudgePlanesParent = judgePlanesParent;
        JudgeLinesParent = judgeLinesParent;
        TapsParent = tapsParent;
        SlidesParent = slidesParent;
        FlicksParent = flicksParent;
        HoldsParent = holdsParent;
        StarsParent = starsParent;
        TapSoundEffect = tapSoundEffect;
        SlideSoundEffect = slideSoundEffect;
        FlickSoundEffect = flickSoundEffect;
        HoldSoundEffect = holdSoundEffect;
        StarHeadSoundEffect = starheadSoundEffect;
        this.chart = chart;
        //Debug.Log(FlickSoundEffect);
    }

    public void PlayMusicAndChart(Chart chart)
    {
        // 提前加工Chart里所有键（Tap和Slide等）的startT与对应实例名的映射关系，并按照startT排序
        PrepareChartMapping(chart);
        audioSource.Play();
        isMusicPlaying = true;
        isPaused = false;
        //AddListenerToButton();
        StartCoroutine(UpdatePositionsCoroutine());
    }

    private IEnumerator UpdatePositionsCoroutine()
    {
        //float audioPrevTime = 0f;

        while (true)
        {
            if (audioSource.isPlaying && !isPaused)
            {
                Updateall();
                //audioPrevTime = audioSource.time;
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void Updateall()
    {
        if (isMusicPlaying) // 只有当明确进入音乐播放阶段才进行后续判断
        {
            if (audioSource.isPlaying)
            {
                float currentTime = audioSource.time;
                //Debug.Log("1. " + currentTime);

                // 将所有已经结束的 JudgePlane（endT 小于 currentTime）设置为 setActive(false)
                foreach (var pair in judgePlaneEndTimes)
                {
                    if (pair.Value < currentTime)
                    {
                        var judgePlaneObject = JudgePlanesParent.transform.Find(pair.Key);
                        if (judgePlaneObject != null)
                        {
                            judgePlaneObject.gameObject.SetActive(false);
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
                            TapSoundEffect.Play();
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
                    }
                    currentIndex++;
                }
                // 统一处理当前所有Hold的状态更新（开始、持续、结束判定）
                UpdateHoldStates(currentTime);
                //更新所有Note位置
                UpdatePositions(chart, currentTime);
                //更新所有Arrow的透明度
                Utility.CheckArrowVisibility(chart, currentTime);
                //Debug.Log("2. " + audioTime);
            }
            else
            {
                // 歌曲播放结束后，跳回选歌场景
                SceneManager.LoadScene("SongSelect");
            }
        }
    }

    private void Update()
    {
        // 检查鼠标点击
        if (Input.GetMouseButtonDown(0))
        {
            CheckPauseButtonClick();
        }
    }

    private void CheckPauseButtonClick()
    {
        // 确保 EventSystem 存在
        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem 未找到，请在场景中添加 EventSystem 组件。");
            return;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        // 检查结果列表是否为空
        if (results.Count > 0)
        {
            foreach (RaycastResult result in results)
            {
                if (result.gameObject.CompareTag("PauseButton"))
                {
                    TogglePause();
                    break;
                }
            }
        }
        else
        {
            Debug.Log("Raycast 没有找到任何结果。");
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
            float holdEndTime = holdEndTimes[instanceName];
            bool isStart = currentTime >= holdKeyInfo.startT && currentTime < holdEndTime;
            bool isEnd = currentTime >= holdEndTime;
            //Debug.Log($"InstanceName: {instanceName}, currentTime: {currentTime}, isStart: {isStart}, isEnd: {isEnd}");

            // 获取Subhold的X轴左侧和右侧坐标
            float subholdLeftX = hold.GetCurrentSubHoldLeftX(currentTime);
            float subholdRightX = hold.GetCurrentSubHoldRightX(currentTime);
            // 计算X轴坐标均值
            float x = (subholdLeftX + subholdRightX) / 2;

            JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
            float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(hold.GetFirstSubHoldStartTime());
            //Debug.Log(yAxisPosition);
            Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
            float worldUnitToScreenPixelX = Utility.CalculateWorldUnitToScreenPixelXAtPosition(referencePoint);
            //Debug.Log(worldUnitToScreenPixelX);
            float startXWorld = worldUnitToScreenPixelX * x / ChartParams.XaxisMax;
            float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;
            //Debug.Log(noteSizeWorldLengthPerUnit);

            float x_width = noteSizeWorldLengthPerUnit / 1.18f * (subholdRightX - subholdLeftX);
            //Debug.Log(x_width);

            // 获取所在JudgePlane当时刻坐标
            float y = 0f;
            JudgePlane correspondingJudgePlane = chart.GetCorrespondingJudgePlaneBasedOnTime(currentTime, hold);
            if (correspondingJudgePlane != null)
            {
                y = correspondingJudgePlane.GetPlaneYAxis(currentTime);
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
                //Debug.Log(holdHitEffectPosition);
                GameObject holdHitEffect = new GameObject($"HoldHitEffect{holdIndex + 1}");
                holdHitEffect.transform.parent = HoldsParent.transform;
                holdHitEffect.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                holdHitEffect.transform.position = holdHitEffectPosition;
                holdHitEffect.transform.localScale = new Vector3(x_width, 1, 1);
                holdHitEffect.AddComponent<SpriteRenderer>();

                //动画组件加载和状态设置
                Animator holdAnimator = holdHitEffect.AddComponent<Animator>();
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
                float holdEndTime = holdEndTimes[instanceName];
                bool isStart = currentTime >= holdKeyInfo.startT && currentTime < holdEndTime;
                bool isEnd = currentTime >= holdEndTime;

                float subholdLeftX = hold.GetCurrentSubHoldLeftX(currentTime);
                float subholdRightX = hold.GetCurrentSubHoldRightX(currentTime);
                float x = (subholdLeftX + subholdRightX) / 2;

                JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
                float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(currentTime);
                Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                float worldUnitToScreenPixelX = Utility.CalculateWorldUnitToScreenPixelXAtPosition(referencePoint);
                float startXWorld = worldUnitToScreenPixelX * x / ChartParams.XaxisMax;
                float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                float x_width = noteSizeWorldLengthPerUnit / 1.18f * (subholdRightX - subholdLeftX);

                float y = 0f;
                JudgePlane correspondingJudgePlane = chart.GetCorrespondingJudgePlaneBasedOnTime(currentTime, hold);
                y = correspondingJudgePlane.GetPlaneYAxis(currentTime);
                Vector3 holdHitEffectPosition = new Vector3(-startXWorld, y, 0f);

                if (isEnd && !holdKeyInfo.isSoundPlayedAtEnd)
                {
                    HoldSoundEffect.Play();
                    holdKeyInfo.isSoundPlayedAtEnd = true;
                    // 如果已经结束判定，从列表中移除该instanceName
                    currentHoldInstanceNames.RemoveAt(i);

                    GameObject holdHitEffect = GameObject.Find($"HoldHitEffect{holdIndex + 1}");
                    if (holdHitEffect != null)
                    {
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
                else if (isStart && !isEnd)
                {
                    GameObject holdHitEffect = GameObject.Find($"HoldHitEffect{holdIndex + 1}");
                    holdHitEffect.transform.position = holdHitEffectPosition;
                    holdHitEffect.transform.localScale = new Vector3(x_width, 1, 1);
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
        

        if(chart.judgePlanes != null)
        {
            for (int i = 0; i < chart.judgePlanes.Count; i++)
            {
                var judgePlane = chart.judgePlanes[i];
                float startT = judgePlane.subJudgePlaneList[0].startT;
                float endT = judgePlane.subJudgePlaneList[judgePlane.subJudgePlaneList.Count - 1].endT;
                JudgePlanesStartT.Add(startT);
                string judgePlaneName = $"JudgePlane{i + 1}";
                judgePlaneEndTimes[judgePlaneName] = endT;
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
                holdEndTimes[instanceName] = endT; // 记录Hold的结束时间
            }
        }

        if (chart.stars != null)
        {
            for (int i = 0; i < chart.stars.Count; i++)
            {
                var star = chart.stars[i];
                float startT = star.starHeadT;
                string instanceName = $"StarHead{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(startT, instanceName));
                keyReachedJudgment[instanceName] = new KeyInfo(startT);
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

    private void UpdatePositions(Chart chart, float currentTime)
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
                        // 获取该 JudgeLine 的开始时间
                        float startT = JudgePlanesStartT[i];

                        // 只有当当前时间大于开始时间时，才更新 JudgeLine 的位置
                        if (currentTime > startT)
                        {
                            // 获取当前时间的 Y 轴坐标，转化为屏幕坐标，更新 JudgeLine 的 Y 轴坐标
                            float YAxis = correspondingJudgePlane.GetPlaneYAxis(currentTime);
                            float YAxisUniform = YAxis / HeightParams.HeightDefault;
                            ////Debug.Log(YAxis);
                            Vector2 Position = Utility.ScalePositionToScreen(new Vector2(0f, YAxisUniform), JudgeLinesParent.GetComponent<RectTransform>());
                            judgeLineRectTransform.anchoredPosition = Position;
                            // 根据 YAxis 的值，实时改变 correspondingJudgePlane 下所有 SubJudgePlane 实例的透明度
                            correspondingJudgePlane.ChangeSubJudgePlaneTransparency(JudgePlanesParent, YAxis);
                        }

                        // 判断当前时间与开始时间的关系
                        if (currentTime < startT - ChartParams.JudgeLineAppearTime)
                        {
                            // 当当前时间小于开始时间 - 出现时间时，不进行操作
                            judgeLineRectTransform.gameObject.SetActive(false);
                        }
                        else if (currentTime >= startT - ChartParams.JudgeLineAppearTime && currentTime <= startT)
                        {
                            // 当当前时间介于开始时间 - 出现时间与开始时间之间时，设置 JudgeLine 为 active，且透明度线性地由 0 变为 1
                            judgeLineRectTransform.gameObject.SetActive(true);
                            float t = (currentTime - (startT - ChartParams.JudgeLineAppearTime)) / ChartParams.JudgeLineAppearTime;
                            JudgeLine.SetJudgeLineAlpha(judgeLineRectTransform.gameObject, t);
                        }
                        else if (currentTime >= startT)
                        {
                            // 当当前时间大于开始时间时，设置 JudgeLine 为 active，且透明度为 1
                            judgeLineRectTransform.gameObject.SetActive(true);
                            JudgeLine.SetJudgeLineAlpha(judgeLineRectTransform.gameObject, 1f);
                        }
                    }
                }
            }
        }
    }

    private void UpdateNotesPosition(float zAxisDecreasePerFrame)
    {
        if (TapsParent != null)
        {
            for (int i = 0; i < TapsParent.transform.childCount; i++)
            {
                Transform childTransform = TapsParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;
                string instanceName = keyGameObject.name;
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                if (!keyInfo.isJudged)
                {
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;
                }
                //if (i == 0) { Debug.Log(childTransform.position.z); }
            }
        }

        if (SlidesParent != null)
        {
            for (int i = 0; i < SlidesParent.transform.childCount; i++)
            {
                Transform childTransform = SlidesParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;
                string instanceName = keyGameObject.name;
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                if (!keyInfo.isJudged)
                {
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;
                }
            }
        }

        if (FlicksParent != null)
        {
            for (int i = 0; i < FlicksParent.transform.childCount; i++)
            {
                Transform childTransform = FlicksParent.transform.GetChild(i);
                GameObject keyGameObject = childTransform.gameObject;
                string instanceName = keyGameObject.name;
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                if (!keyInfo.isJudged)
                {
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;
                }
            }
        }

        // 针对Hold键进行额外的位置或属性更新（示例）
        if (HoldsParent != null)
        {
            for (int i = 0; i < HoldsParent.transform.childCount; i++)
            {
                Transform childTransform = HoldsParent.transform.GetChild(i);
                GameObject holdGameObject = childTransform.gameObject;
                string instanceName = holdGameObject.name;
                //如果是HoldHitEffect物体，则跳过
                if (!instanceName.StartsWith("HoldHitEffect"))
                {
                    KeyInfo keyInfo = keyReachedJudgment[instanceName];

                    //注意Hold无论是否判定，位置都要更新
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;

                }
            }
        }
        // 针对StarHead键进行位置更新
        if (StarsParent != null)
        {
            for (int i = 0; i < StarsParent.transform.childCount; i++)
            {
                Transform childTransform = StarsParent.transform.GetChild(i);
                GameObject starHeadGameObject = childTransform.gameObject;
                string instanceName = starHeadGameObject.name;
                KeyInfo keyInfo = keyReachedJudgment[instanceName];
                //如果是HoldHitEffect物体，则跳过
                if (instanceName.StartsWith("StarHead") && !keyInfo.isJudged)
                {
                    Vector3 currentPosition = childTransform.position;
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;
                }
            }
        }
    }

    // 播放动画的方法，设置Animator的动画控制器并播放指定动画
    private void PlayAnimation(string instanceName, string animationName)
    {
        GameObject keyGameObject = GameObject.Find(instanceName);
        if (keyGameObject != null)
        {
            //先修改物体z轴位置为0（保证判定特效在判定线上）
            Vector3 pos = keyGameObject.transform.position;
            pos.z = 0;
            keyGameObject.transform.position = pos;

            Animator animator = keyGameObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = keyGameObject.AddComponent<Animator>();
            }

            //临时调整一下动画效果的缩放，用以跟Note大小匹配
            Vector3 currentScale = keyGameObject.transform.localScale;
            currentScale.x *= 247 / 165f;
            keyGameObject.transform.localScale = currentScale;

            // 判断是否是Flick类型的键，如果是则先删除其所有子物体（如FlickArrow）
            if (instanceName.StartsWith("Flick"))
            {
                // 获取Flick物体下的所有子物体并删除
                for (int i = keyGameObject.transform.childCount - 1; i >= 0; i--)
                {
                    Transform childTransform = keyGameObject.transform.GetChild(i);
                    childTransform.gameObject.SetActive(false);
                }
            }
            //如果是Hold类型的键，则删除对应的Hold物体
            if (instanceName.StartsWith("HoldHitEffect"))
            {
                string numberPart = instanceName.Substring(13);
                if (int.TryParse(numberPart, out int holdIndex))
                {
                    GameObject gameobject = GameObject.Find($"Hold{holdIndex}");
                    gameobject.SetActive(false);
                }
            }

            // 根据不同的动画名称加载对应的动画控制器（这里假设路径和名称是固定的，需按实际调整）
            RuntimeAnimatorController controller = null;
            if (animationName == "TapHitEffect")
            {
                controller = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");
            }
            else if (animationName == "SlideEffect")
            {
                controller = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");
            }
            else if (animationName == "FlickEffect")
            {
                controller = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");
            }
            else if (animationName == "HoldEffect") // Hold开始动画控制器加载
            {
                controller = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");
            }
            else if (animationName == "StarHeadEffect") // Hold开始动画控制器加载
            {
                controller = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");
            }

            if (controller == null)
            {
                Debug.LogError($"无法加载 {animationName} 的AnimatorController资源，请检查资源路径和文件是否存在！");
            }
            animator.runtimeAnimatorController = controller;

            // 正常播放动画
            animator.Play("TapHitEffect");
            // 检查动画是否播放完毕
            StartCoroutine(CheckAnimationEnd(animator, keyGameObject));
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

    // 新增的方法，用于处理暂停和继续播放音乐
    public void TogglePause()
    {
        //Debug.Log("TogglePause");
        if (isPaused)
        {
            audioSource.Play();
            isPaused = false;
            isMusicPlaying = true;
        }
        else
        {
            audioSource.Pause();
            isPaused = true;
            isMusicPlaying = false;
        }
    }

}

