using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Params;
using System.Collections;
using UnityEngine.SceneManagement;

public class MusicAndChartPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private GameObject JudgePlanesParent;
    private GameObject JudgeLinesParent;
    private GameObject TapsParent;
    private GameObject SlidesParent;
    private GameObject FlicksParent;
    private AudioSource TapSoundEffect;
    private AudioSource SlideSoundEffect;
    private AudioSource FlickSoundEffect;
    private Dictionary<float, List<string>> startTimeToInstanceNames = new Dictionary<float, List<string>>(); // 存储startT到对应实例名列表的映射
    private Dictionary<string, KeyInfo> keyReachedJudgment = new Dictionary<string, KeyInfo>(); // 存储实例名到键信息的映射
    private bool isMusicPlayingStarted; // 用于标识是否已经开始音乐播放阶段
    private Chart chart; // 用于存储传入的Chart实例，方便在Update里使用
    private int currentIndex = 0;

    // 内部类，用于存储键的相关信息以及判定状态
    private class KeyInfo
    {
        public float startT;
        public bool isJudged;
        public KeyInfo(float startTime)
        {
            startT = startTime;
            isJudged = false;
        }
    }

    // 新增的公共方法，用于接收各个参数并赋值给对应的私有变量，添加了SlidesParent和SlideSoundEffect参数
    public void SetParameters(AudioSource audioSource, GameObject judgePlanesParent, GameObject judgeLinesParent, 
        GameObject tapsParent, GameObject slidesParent, GameObject flicksParent, 
        AudioSource tapSoundEffect, AudioSource slideSoundEffect, AudioSource flickSoundEffect, Chart chart)
    {
        this.audioSource = audioSource;
        JudgePlanesParent = judgePlanesParent;
        JudgeLinesParent = judgeLinesParent;
        TapsParent = tapsParent;
        SlidesParent = slidesParent;
        FlicksParent = flicksParent;
        TapSoundEffect = tapSoundEffect;
        SlideSoundEffect = slideSoundEffect;
        FlickSoundEffect = flickSoundEffect;
        this.chart = chart;
    }

    public void PlayMusicAndChart(Chart chart)
    {
        
        // 提前加工Chart里所有键（Tap和Slide等）的startT与对应实例名的映射关系，并按照startT排序
        PrepareStartTimeToInstanceNamesMapping(chart);
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
                        }
                        currentIndex++;
                    }
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


    private void PrepareStartTimeToInstanceNamesMapping(Chart chart)
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

            if (controller == null)
            {
                Debug.LogError($"无法加载 {animationName} 的AnimatorController资源，请检查资源路径和文件是否存在！");
            }
            animator.runtimeAnimatorController = controller;
            //animator.Play(animationName);
            animator.Play("TapHitEffect");

            // 检查动画是否播放完毕（这里使用一个简单的方法来模拟，实际可能需要更精确的判断）
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