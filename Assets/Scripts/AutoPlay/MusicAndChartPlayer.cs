using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Params;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;
using Note;

public class MusicAndChartPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private GameObject JudgePlanesParent;
    private GameObject JudgeLinesParent;
    private GameObject TapsParent;
    private GameObject SlidesParent;
    private GameObject FlicksParent;
    private GameObject HoldsParent;

    private AudioSource TapSoundEffect;
    private AudioSource SlideSoundEffect;
    private AudioSource FlickSoundEffect;
    private AudioSource HoldSoundEffect;

    private Dictionary<float, List<string>> startTimeToInstanceNames = new Dictionary<float, List<string>>(); // 存储startT到对应实例名列表的映射
    private Dictionary<string, float> holdEndTimes = new Dictionary<string, float>(); // 存储Hold实例名到其结束时间的映射
    private Dictionary<string, KeyInfo> keyReachedJudgment = new Dictionary<string, KeyInfo>(); // 存储实例名到键信息的映射
    private Dictionary<string, GameObject> holdHitEffectDictionary = new Dictionary<string, GameObject>();
    // 新增一个列表用于存储当前正在处理的Hold对应的instanceName
    private List<string> currentHoldInstanceNames = new List<string>();
    private bool isMusicPlayingStarted; // 用于标识是否已经开始音乐播放阶段
    private Chart chart; // 用于存储传入的Chart实例，方便在Update里使用
    private int currentIndex = 0;

    private Animator holdAnimator;
    private int holdStartHash;
    private int holdEndHash;

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
        GameObject tapsParent, GameObject slidesParent, GameObject flicksParent, GameObject holdsParent,
        AudioSource tapSoundEffect, AudioSource slideSoundEffect, AudioSource flickSoundEffect, AudioSource holdSoundEffect, Chart chart)
    {
        this.audioSource = audioSource;
        JudgePlanesParent = judgePlanesParent;
        JudgeLinesParent = judgeLinesParent;
        TapsParent = tapsParent;
        SlidesParent = slidesParent;
        FlicksParent = flicksParent;
        HoldsParent = holdsParent;
        TapSoundEffect = tapSoundEffect;
        SlideSoundEffect = slideSoundEffect;
        FlickSoundEffect = flickSoundEffect;
        HoldSoundEffect = holdSoundEffect;
        this.chart = chart;
    }

    public void PlayMusicAndChart(Chart chart)
    {
        // 提前加工Chart里所有键（Tap和Slide等）的startT与对应实例名的映射关系，并按照startT排序
        PrepareChartMapping(chart);
        audioSource.Play();
        isMusicPlayingStarted = true;
    }

    private void Update()
    {
        if (isMusicPlayingStarted) // 只有当明确进入音乐播放阶段才进行后续判断
        {
            if (audioSource.isPlaying)
            {
                float currentTime = Time.time;
                // 未知原因，时间为0时就执行了UpdatePositions(chart)导致Z轴坐标偏后了，先额外加个判断，之后再查问题……
                if (currentTime > 0)
                {
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
                                //Debug.Log(Time.time);
                                PlayAnimation(instanceName, "FlickEffect"); // 假设动画名称为FlickEffect，按实际修改
                            }
                            else if (instanceName.StartsWith("Hold"))
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
                                    float x_width = (subholdRightX - subholdLeftX)*1.5f;

                                    // 获取所在JudgePlane当时刻坐标
                                    float y = 0f;
                                    JudgePlane correspondingJudgePlane = GetCorrespondingJudgePlaneBasedOnTime(currentTime, chart, hold);
                                    if (correspondingJudgePlane != null)
                                    {
                                        y = correspondingJudgePlane.GetPlaneYAxis(currentTime);
                                    }
                                    //HoldHitEffect的位置（注意挂载在Hold物体下，需要根据父物体坐标折算子物体相对坐标））
                                    Vector3 holdHitEffectPosition = new Vector3(-x, y, 0f);
                                    if (isStart && !holdKeyInfo.isSoundPlayedAtStart) // 仅在开始判定且还没播放过开始音效时播放
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
                                        holdHitEffect.AddComponent< SpriteRenderer > ();

                                        //动画组件加载和状态设置
                                        Animator holdAnimator = holdHitEffect.AddComponent<Animator>();
                                        holdAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");
                                        int holdStartHash = Animator.StringToHash($"HoldStart");
                                        //设置状态为HoldStart
                                        holdAnimator.SetTrigger(holdStartHash);

                                        // 挂载PingPongAnimationStateController脚本
                                        //PingPongAnimationStateController pingPongScript = holdHitEffect.AddComponent<PingPongAnimationStateController>();
                                        //PlayAnimation($"HoldHitEffect{holdIndex + 1}", "HoldEffect");
                                    }
                                }
                            }
                        }
                        currentIndex++;
                    }
                    // 统一处理当前所有Hold的状态更新（开始、持续、结束判定）
                    UpdateHoldStates(currentTime);
                    UpdatePositions(chart);
                }
            }
            else
            {
                // 歌曲播放结束后，跳回选歌场景
                SceneManager.LoadScene("SongSelect");
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

                // 获取当前光效所在世界坐标
                float subholdLeftX = hold.GetCurrentSubHoldLeftX(currentTime);
                float subholdRightX = hold.GetCurrentSubHoldRightX(currentTime);
                float x = (subholdLeftX + subholdRightX) / 2;
                float x_width = (subholdRightX - subholdLeftX) * 1.5f;

                float y = 0f;
                JudgePlane correspondingJudgePlane = GetCorrespondingJudgePlaneBasedOnTime(currentTime, chart, hold);
                if (correspondingJudgePlane != null)
                {
                    y = correspondingJudgePlane.GetPlaneYAxis(currentTime);
                }
                Vector3 holdHitEffectPosition = new Vector3(-x, y, 0f);

                if (isEnd && !holdKeyInfo.isSoundPlayedAtEnd)
                {
                    HoldSoundEffect.Play();
                    holdKeyInfo.isSoundPlayedAtEnd = true;
                    // 如果已经结束判定，从列表中移除该instanceName
                    currentHoldInstanceNames.RemoveAt(i);

                    //int holdEndHash = Animator.StringToHash($"HoldEnd_{holdIndex}");
                    //animator.SetTrigger(holdEndHash);
                    //StartCoroutine(CheckAnimationEnd(animator, holdHitEffect));
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
                            //Debug.Log(1);

                        }
                    }


                }
                else if (isStart && !isEnd)
                {
                    //Debug.Log(holdHitEffectPosition);
                    GameObject holdHitEffect = GameObject.Find($"HoldHitEffect{holdIndex + 1}");
                    holdHitEffect.transform.position = holdHitEffectPosition;
                    holdHitEffect.transform.localScale = new Vector3(x_width, 1, 1);
                    //UpdateHoldLight(holdKeyInfo, instanceName, holdObject, holdHitEffectPosition);
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

    private JudgePlane GetCorrespondingJudgePlaneBasedOnTime(float currentTime, Chart chart, Hold hold)
    {
        JudgePlane associatedJudgePlaneObject = GetCorrespondingJudgePlane(chart, hold.associatedPlaneId);
        if (associatedJudgePlaneObject != null)
        {
            return associatedJudgePlaneObject;
        }
        return null;
    }

    private void PrepareChartMapping(Chart chart)
    {
        List<KeyValuePair<float, string>> allPairs = new List<KeyValuePair<float, string>>();
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
        }
    }

    private void UpdatePositions(Chart chart)
    {
        //Debug.Log($"当前帧率: {1.0f / Time.deltaTime}");
        // 计算每帧对应的Z轴坐标减少量，假设帧率是固定的（比如60帧每秒），这里用1.0f/Time.deltaTime来获取帧率，然后根据每秒钟变化单位计算每帧变化量
        float zAxisDecreasePerFrame = SpeedParams.NoteSpeedDefault * Time.deltaTime;

        // 如果已经创建了JudgePlanesParent并且音频正在播放，更新其子物体的Z轴坐标
        if (JudgePlanesParent != null)
        {
            for (int i = 0; i < JudgePlanesParent.transform.childCount; i++)
            {
                Transform childTransform = JudgePlanesParent.transform.GetChild(i);
                Vector3 currentPosition = childTransform.position;
                currentPosition.z += zAxisDecreasePerFrame;
                childTransform.position = currentPosition;
                //Debug.Log(Time.time + "___" + childTransform.position.z);
            }
        }

        // 如果已经创建了JudgeLinesParent并且音频正在播放，更新其子物体的Y轴坐标
        if (JudgeLinesParent != null)
        {
            for (int i = 0; i < JudgeLinesParent.transform.childCount; i++)
            {
                Transform judgeLineTransform = JudgeLinesParent.transform.GetChild(i);
                if (judgeLineTransform != null)
                {
                    // 通过名字获取对应的JudgePlane
                    JudgePlane correspondingJudgePlane = GetCorrespondingJudgePlane(chart, judgeLineTransform.name);
                    if (correspondingJudgePlane != null)
                    {
                        // 获取当前时间
                        float currentTime = Time.time;
                        // 调用JudgePlane的GetPlaneYAxis方法获取当前时间的Y轴坐标，并更新JudgeLine的Y轴坐标
                        float newYAxis = correspondingJudgePlane.GetPlaneYAxis(currentTime);
                        Vector3 position = judgeLineTransform.position;
                        position.y = newYAxis;
                        judgeLineTransform.position = position;

                        // 根据newYAxis的值，实时改变correspondingJudgePlane下所有SubJudgePlane实例的透明度
                        ChangeSubJudgePlaneTransparency(correspondingJudgePlane, newYAxis);
                    }
                }
            }
        }

        // 更新Tap和Slide的位置，统一处理，不再区分两个字典
        UpdateKeysPosition(zAxisDecreasePerFrame);
    }

    private void UpdateKeysPosition(float zAxisDecreasePerFrame)
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
                if (! instanceName.StartsWith("HoldHitEffect"))
                { 
                    KeyInfo keyInfo = keyReachedJudgment[instanceName];

                    //注意Hold无论是否判定，位置都要更新
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
            Animator animator = keyGameObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = keyGameObject.AddComponent<Animator>();
            }

            //临时调整一下动画效果的缩放，用以跟Note大小匹配
            Vector3 currentScale = keyGameObject.transform.localScale;
            currentScale.x *= 1.5f;
            keyGameObject.transform.localScale = currentScale;

            // 判断是否是Flick类型的键，如果是则先删除其所有子物体（如FlickArrow）
            if (instanceName.StartsWith("Flick"))
            {
                // 获取Flick物体下的所有子物体并删除
                for (int i = keyGameObject.transform.childCount - 1; i >= 0; i--)
                {
                    Transform childTransform = keyGameObject.transform.GetChild(i);
                    Destroy(childTransform.gameObject);
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

            if (controller == null)
            {
                Debug.LogError($"无法加载 {animationName} 的AnimatorController资源，请检查资源路径和文件是否存在！");
            }
            animator.runtimeAnimatorController = controller;
            //animator.Play(animationName);
            //animator.Play("TapHitEffect");

            // 针对Hold开始判定时动画在第1帧和第6帧之间反复播放
            //if (animationName != "HoldEffect")
            //{
                // 对于其他类型的键，正常播放动画
                animator.Play("TapHitEffect");
                // 检查动画是否播放完毕（这里使用一个简单的方法来模拟，实际可能需要更精确的判断）
                StartCoroutine(CheckAnimationEnd(animator, keyGameObject));
            //}

            //SpriteRenderer spriteRenderer = keyGameObject.GetComponent<SpriteRenderer>();
            ////获取Tap在X轴的长度（用于缩放）
            //float tapXAxisLength = spriteRenderer.sprite.bounds.size.x;
            //Debug.Log(tapXAxisLength);

            
        }
    }
    // 协程方法用于控制动画帧在第1帧和第6帧之间循环
    //private IEnumerator LoopAnimationFrames(Animator animator, int startFrame, int endFrame)
    //{
    //    // 获取动画的帧率（假设动画剪辑已经设置好帧率）
    //    float frameRate = animator.GetCurrentAnimatorClipInfo(0)[0].clip.frameRate;
    //    // 计算第1帧和第6帧对应的时间
    //    float startFrameTime = (float)startFrame / frameRate;
    //    float endFrameTime = (float)endFrame / frameRate;

    //    while (true)
    //    {
    //        // 设置动画时间到第1帧时间
    //        animator.Play(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name, 0, startFrameTime);
    //        // 等待直到动画时间到达第6帧时间
    //        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < endFrameTime)
    //        {
    //            yield return null;
    //        }
    //    }
    //}

    // 协程方法用于检查动画是否播放完毕，播放完毕后销毁对应的游戏物体（这里统一处理，根据实际情况可调整）
    private IEnumerator CheckAnimationEnd(Animator animator, GameObject keyGameObject)
    {
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        // 动画播放完毕后销毁对应的游戏物体（可根据实际需求决定是否销毁等操作）
        Destroy(keyGameObject);
    }

    // 通过JudgeLine的名字获取对应的JudgePlane实例（根据名字中的id匹配）
    private JudgePlane GetCorrespondingJudgePlane(Chart chart, string judgeLineName)
    {
        int judgePlaneId;
        if (int.TryParse(judgeLineName.Replace("JudgeLine", ""), out judgePlaneId))
        {
            if (chart != null && chart.judgePlanes != null)
            {
                foreach (var judgePlane in chart.judgePlanes)
                {
                    if (judgePlane.id == judgePlaneId)
                    {
                        return judgePlane;
                    }
                }
            }
        }
        return null;
    }
    private JudgePlane GetCorrespondingJudgePlane(Chart chart, int judgePlaneId)
    {
        if (chart != null && chart.judgePlanes != null)
        {
            foreach (var judgePlane in chart.judgePlanes)
            {
                if (judgePlane.id == judgePlaneId)
                {
                    return judgePlane;
                }
            }
        }
        return null;
    }

    //方法用于根据给定的Y轴坐标值改变对应JudgePlane下所有SubJudgePlane实例的透明度
    private void ChangeSubJudgePlaneTransparency(JudgePlane judgePlane, float yAxisValue)
    {
        // 计算透明度差值，根据给定的对应关系计算斜率
        float alphaDelta = (0.5f - 0.8f) / 6f;
        // 根据线性关系计算当前透明度值，确保透明度范围在0到1之间
        float currentAlpha = Mathf.Clamp(0.8f + alphaDelta * yAxisValue, 0, 1);

        // 获取JudgePlane对应的游戏物体（假设其命名规则是"JudgePlane + id"，可根据实际调整获取方式）
        GameObject judgePlaneObject = GetJudgePlaneGameObject(judgePlane);
        if (judgePlaneObject != null)
        {
            // 直接遍历JudgePlane游戏物体下的所有子物体
            foreach (Transform child in judgePlaneObject.transform)
            {
                // 获取物体的MeshRenderer组件（前提是子物体有这个组件用于渲染）
                MeshRenderer meshRenderer = child.gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material.SetFloat("_Opacity", currentAlpha);
                }
            }
        }
    }

    // 简单示例方法，获取JudgePlane实例对应的游戏物体，你可根据实际情况调整获取方式
    private GameObject GetJudgePlaneGameObject(JudgePlane judgePlane)
    {
        string judgePlaneObjectName = $"JudgePlane{judgePlane.id}";
        foreach (Transform child in JudgePlanesParent.transform)
        {
            if (child.gameObject.name == judgePlaneObjectName)
            {
                return child.gameObject;
            }
        }
        return null;
    }
}

//private void CreateHoldLight(KeyInfo holdKeyInfo, string instanceName, GameObject holdGameObject, Vector3 holdHitEffectPosition)
//{
//    if (holdGameObject != null)
//    {
//        // 创建HoldHitEffect子物体
//        GameObject holdHitEffect = new GameObject("HoldHitEffect");
//        holdHitEffect.transform.parent = holdGameObject.transform;

//        // 创建一个新的Light组件（3D光源）并挂载到HoldHitEffect上
//        Light holdLight = holdHitEffect.AddComponent<Light>();
//        holdLight.color = Color.blue;
//        holdLight.intensity = 1f;
//        holdLight.range = 0.1f;

//        // 将创建的HoldHitEffect添加到字典中
//        holdHitEffectDictionary[instanceName] = holdHitEffect;

//        holdHitEffect.transform.position = holdHitEffectPosition;
//    }
//}

//private void UpdateHoldLight(KeyInfo holdKeyInfo, string instanceName, GameObject holdGameObject, Vector3 holdHitEffectPosition)
//{
//    if (holdHitEffectDictionary.TryGetValue(instanceName, out GameObject holdHitEffect) && holdHitEffect != null && holdHitEffect.GetComponent<Light>() != null)
//    {
//        Light holdLight = holdHitEffect.GetComponent<Light>();

//        holdHitEffect.transform.position = holdHitEffectPosition;
//    }
//}

//private void DestroyPreviousLight(KeyInfo holdKeyInfo, string instanceName)
//{
//    if (holdHitEffectDictionary.TryGetValue(instanceName, out GameObject holdHitEffect) && holdHitEffect != null)
//    {
//        Destroy(holdHitEffect);
//        holdHitEffectDictionary.Remove(instanceName);
//    }
//}