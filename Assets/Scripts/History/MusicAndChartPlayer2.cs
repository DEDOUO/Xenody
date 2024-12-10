//using UnityEngine;
//using System.Collections;
//using Params;
//using UnityEngine.SceneManagement;
//using System.Collections.Generic;

//public class MusicAndChartPlayer : MonoBehaviour
//{
//    private AudioSource audioSource;
//    private GameObject JudgePlanesParent;
//    private GameObject JudgeLinesParent;
//    private GameObject TapsParent;
//    private GameObject SlidesParent;
//    private AudioSource TapSoundEffect;
//    private Dictionary<GameObject, bool> tapReachedJudgmentLine;
//    private Dictionary<GameObject, bool> slideReachedJudgmentLine;
//    private Chart chart; // 新增，用于存储传入的Chart实例，方便在Update里使用
//    private bool isMusicPlayingStarted; // 新增，用于标识是否已经开始音乐播放阶段

//    // 新增的公共方法，用于接收各个参数并赋值给对应的私有变量
//    public void SetParameters(AudioSource audioSource, GameObject judgePlanesParent, GameObject judgeLinesParent, GameObject tapsParent, GameObject slidesParent, AudioSource tapSoundEffect, Dictionary<GameObject, bool> tapReachedJudgmentLine, Chart chart)
//    {
//        this.audioSource = audioSource;
//        JudgePlanesParent = judgePlanesParent;
//        JudgeLinesParent = judgeLinesParent;
//        TapsParent = tapsParent;
//        SlidesParent = slidesParent;
//        TapSoundEffect = tapSoundEffect;
//        this.tapReachedJudgmentLine = tapReachedJudgmentLine;
//        this.slideReachedJudgmentLine = slideReachedJudgmentLine;
//        this.chart = chart;
//        isMusicPlayingStarted = false; // 初始化时设置为未开始音乐播放
//    }

//    public void PlayMusicAndChart()
//    {
//        audioSource.Play();
//        isMusicPlayingStarted = true; // 调用播放方法时，标记为已开始音乐播放
//    }

//    private void Update()
//    {
//        if (isMusicPlayingStarted) // 只有当明确进入音乐播放阶段才进行后续判断
//        {
//            if (audioSource.isPlaying)
//            {
//                UpdatePositions(chart);
//            }
//            else
//            {
//                // 歌曲播放结束后，跳回选歌场景
//                SceneManager.LoadScene("SongSelect");
//            }
//        }
//    }

//    private void UpdatePositions(Chart chart)
//    {
//        // 计算每帧对应的Z轴坐标减少量，假设帧率是固定的（比如60帧每秒），这里用1.0f/Time.deltaTime来获取帧率，然后根据每秒钟变化单位计算每帧变化量
//        float zAxisDecreasePerFrame = SpeedParams.NoteSpeedDefault * Time.deltaTime;

//        // 如果已经创建了JudgePlanesParent并且音频正在播放，更新其子物体的Z轴坐标
//        if (JudgePlanesParent != null && audioSource.isPlaying)
//        {
//            for (int i = 0; i < JudgePlanesParent.transform.childCount; i++)
//            {
//                Transform childTransform = JudgePlanesParent.transform.GetChild(i);
//                Vector3 currentPosition = childTransform.position;
//                currentPosition.z += zAxisDecreasePerFrame;
//                childTransform.position = currentPosition;
//            }
//        }

//        // 如果已经创建了JudgeLinesParent并且音频正在播放，更新其子物体的Y轴坐标
//        if (JudgeLinesParent != null && audioSource.isPlaying)
//        {
//            for (int i = 0; i < JudgeLinesParent.transform.childCount; i++)
//            {
//                Transform judgeLineTransform = JudgeLinesParent.transform.GetChild(i);
//                if (judgeLineTransform != null)
//                {
//                    // 通过名字获取对应的JudgePlane
//                    JudgePlane correspondingJudgePlane = GetCorrespondingJudgePlane(chart, judgeLineTransform.name);
//                    if (correspondingJudgePlane != null)
//                    {
//                        // 获取当前时间
//                        float currentTime = Time.time;
//                        // 调用JudgePlane的GetPlaneYAxis方法获取当前时间的Y轴坐标，并更新JudgeLine的Y轴坐标
//                        float newYAxis = correspondingJudgePlane.GetPlaneYAxis(currentTime);
//                        Vector3 position = judgeLineTransform.position;
//                        position.y = newYAxis;
//                        judgeLineTransform.position = position;

//                        // 根据newYAxis的值，实时改变correspondingJudgePlane下所有SubJudgePlane实例的透明度
//                        ChangeSubJudgePlaneTransparency(correspondingJudgePlane, newYAxis);
//                    }
//                }
//            }
//        }

//        if (TapsParent != null && audioSource.isPlaying)
//        {
//            for (int i = 0; i < TapsParent.transform.childCount; i++)
//            {
//                Transform childTransform = TapsParent.transform.GetChild(i);
//                GameObject tapInstance = childTransform.gameObject;
//                Vector3 currentPosition = childTransform.position;

//                if (currentPosition.z > 0 && !tapReachedJudgmentLine[tapInstance])
//                {
//                    tapReachedJudgmentLine[tapInstance] = true;
//                    TapSoundEffect.Play();

//                    // 获取或添加Animator组件
//                    Animator animator = tapInstance.GetComponent<Animator>();
//                    if (animator == null)
//                    {
//                        animator = tapInstance.AddComponent<Animator>();
//                    }

//                    // 设置Animator的动画控制器（假设你已经创建好了名为TapAnimationController的动画控制器）
//                    RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");
//                    if (controller == null)
//                    {
//                        Debug.LogError("无法加载AnimatorController资源，请检查资源路径和文件是否存在！");
//                    }
//                    animator.runtimeAnimatorController = controller;

//                    // 播放动画
//                    animator.Play("TapHitEffect");

//                    // 检查动画是否播放完毕（这里使用一个简单的方法来模拟，实际可能需要更精确的判断）
//                    StartCoroutine(CheckAnimationEnd(animator, tapInstance));
//                }
//                //播放动画时，Tap位置不再变化
//                if (!tapReachedJudgmentLine[tapInstance])
//                {
//                    currentPosition.z += zAxisDecreasePerFrame;
//                    childTransform.position = currentPosition;
//                }
//            }
//        }

//        // 新增的Slides更新逻辑部分
//        if (SlidesParent != null && audioSource.isPlaying)
//        {
//            for (int i = 0; i < SlidesParent.transform.childCount; i++)
//            {
//                Transform slideTransform = SlidesParent.transform.GetChild(i);
//                GameObject slideInstance = slideTransform.gameObject;
//                Vector3 currentPosition = slideTransform.position;

//                if (currentPosition.z > 0 && !slideReachedJudgmentLine[slideInstance])
//                {
//                    // 标记Slide到达判定线（这里假设也有类似的判定逻辑，你可根据实际调整）
//                    slideReachedJudgmentLine[slideInstance] = true;
//                    SlideSoundEffect.Play();

//                    // 获取或添加Animator组件（如果Slide也有动画效果的话，按实际需求添加逻辑）
//                    Animator animator = slideInstance.GetComponent<Animator>();
//                    if (animator == null)
//                    {
//                        animator = slideInstance.AddComponent<Animator>();
//                    }
//                    // 设置Animator的动画控制器（假设Slide有对应的动画控制器，需按实际路径和名称修改）
//                    RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("Animations/SlideAnimationController");
//                    if (controller == null)
//                    {
//                        Debug.LogError("无法加载Slide的AnimatorController资源，请检查资源路径和文件是否存在！");
//                    }
//                    animator.runtimeAnimatorController = controller;

//                    // 播放动画（假设动画名称为SlideEffect之类的，按实际修改）
//                    animator.Play("SlideEffect");

//                    // 检查动画是否播放完毕（这里同样使用简单模拟方式，实际可能需更精确判断）
//                    StartCoroutine(CheckAnimationEnd(animator, slideInstance));
//                }
//                // 如果Slide还未到达判定线，更新其位置（这里的位置更新计算按照Slide的特点来，示例如下，可根据实际调整）
//                if (!slideReachedJudgmentLine[slideInstance])
//                {
//                    currentPosition.z += zAxisDecreasePerFrame;
//                    slideTransform.position = currentPosition;
//                }
//            }
//        }
//    }

//    // 通过JudgeLine的名字获取对应的JudgePlane实例（根据名字中的id匹配）
//    private JudgePlane GetCorrespondingJudgePlane(Chart chart, string judgeLineName)
//    {
//        int judgePlaneId;
//        if (int.TryParse(judgeLineName.Replace("JudgeLine", ""), out judgePlaneId))
//        {
//            if (chart != null && chart.judgePlanes != null)
//            {
//                foreach (var judgePlane in chart.judgePlanes)
//                {
//                    if (judgePlane.id == judgePlaneId)
//                    {
//                        return judgePlane;
//                    }
//                }
//            }
//        }
//        return null;
//    }

//    //方法用于根据给定的Y轴坐标值改变对应JudgePlane下所有SubJudgePlane实例的透明度
//    private void ChangeSubJudgePlaneTransparency(JudgePlane judgePlane, float yAxisValue)
//    {
//        // 计算透明度差值，根据给定的对应关系计算斜率
//        float alphaDelta = (0.5f - 0.8f) / 6f;
//        // 根据线性关系计算当前透明度值，确保透明度范围在0到1之间
//        float currentAlpha = Mathf.Clamp(0.8f + alphaDelta * yAxisValue, 0, 1);

//        // 获取JudgePlane对应的游戏物体（假设其命名规则是"JudgePlane + id"，可根据实际调整获取方式）
//        GameObject judgePlaneObject = GetJudgePlaneGameObject(judgePlane);
//        if (judgePlaneObject != null)
//        {
//            // 直接遍历JudgePlane游戏物体下的所有子物体
//            foreach (Transform child in judgePlaneObject.transform)
//            {
//                // 获取物体的MeshRenderer组件（前提是子物体有这个组件用于渲染）
//                MeshRenderer meshRenderer = child.gameObject.GetComponent<MeshRenderer>();
//                if (meshRenderer != null)
//                {
//                    meshRenderer.material.SetFloat("_Opacity", currentAlpha);
//                }
//            }
//        }
//    }

//    // 协程方法用于检查动画是否播放完毕，播放完毕后销毁Tap实例
//    private IEnumerator CheckAnimationEnd(Animator animator, GameObject tapInstance)
//    {
//        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
//        {
//            yield return null;
//        }

//        // 动画播放完毕后销毁Tap实例
//        Destroy(tapInstance);
//    }

//    // 简单示例方法，获取JudgePlane实例对应的游戏物体，你可根据实际情况调整获取方式
//    private GameObject GetJudgePlaneGameObject(JudgePlane judgePlane)
//    {
//        string judgePlaneObjectName = $"JudgePlane{judgePlane.id}";
//        foreach (Transform child in JudgePlanesParent.transform)
//        {
//            if (child.gameObject.name == judgePlaneObjectName)
//            {
//                return child.gameObject;
//            }
//        }
//        return null;
//    }
//}