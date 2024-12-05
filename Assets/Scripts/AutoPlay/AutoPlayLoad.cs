using UnityEngine;
using UnityEngine.SceneManagement;
//using UnityEngine.UI;
using System.IO;
//using Newtonsoft.Json;
using System.Collections;
//using Note;
using UnityEditor;
//using static Utility;
using Params;
using static Utility;
using Note;
using System.Collections.Generic;
using UnityEngine.Video;


public class AutoPlayLoad : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioSource TapSoundEffect;

    public Chart chart; // 用于存储加载的谱面数据
    public GameObject JudgePlanesParent; 
    public GameObject JudgeLinesParent;
    public GameObject TapsParent;
    public Sprite JudgePlaneSprite;
    public GlobalRenderOrderManager renderOrderManager;
    public GameObject videoPlayerContainer;

    // 动画播放速度
    //private float animationSpeed = 30f;
    // 存储Tap首次到达判定线的状态
    private Dictionary<GameObject, bool> tapReachedJudgmentLine = new Dictionary<GameObject, bool>();


    private void Start()
    {
        LoadMusicAndChart();
        // 启动协程来播放音乐以及后续基于谱面展示相关内容
        StartCoroutine(PlayMusicAndChart());
    }

    private void Update()
    {
        // 计算每帧对应的Z轴坐标减少量，假设帧率是固定的（比如60帧每秒），这里用1.0f / Time.deltaTime来获取帧率，然后根据每秒钟变化单位计算每帧变化量
        float zAxisDecreasePerFrame = SpeedParams.NoteSpeedDefault * Time.deltaTime;


        // 如果已经创建了JudgePlanesParent并且音频正在播放，更新其子物体的Z轴坐标
        if (JudgePlanesParent != null && audioSource.isPlaying)
        {
            for (int i = 0; i < JudgePlanesParent.transform.childCount; i++)
            {
                Transform childTransform = JudgePlanesParent.transform.GetChild(i);
                Vector3 currentPosition = childTransform.position;
                currentPosition.z += zAxisDecreasePerFrame;
                childTransform.position = currentPosition;
            }
        }
        // 如果已经创建了JudgeLinesParent并且音频正在播放，更新其子物体的Y轴坐标
        if (JudgeLinesParent != null && audioSource.isPlaying)
        {
            for (int i = 0; i < JudgeLinesParent.transform.childCount; i++)
            {
                Transform judgeLineTransform = JudgeLinesParent.transform.GetChild(i);
                if (judgeLineTransform != null)
                {
                    // 通过名字获取对应的JudgePlane
                    JudgePlane correspondingJudgePlane = GetCorrespondingJudgePlane(judgeLineTransform.name);
                    if (correspondingJudgePlane != null)
                    {
                        // 获取当前时间
                        float currentTime = Time.time;
                        // 调用JudgePlane的GetPlaneYAxis方法获取当前时间的Y轴坐标，并更新JudgeLine的Y轴坐标
                        float newYAxis = correspondingJudgePlane.GetPlaneYAxis(currentTime);
                        Vector3 position = judgeLineTransform.position;
                        position.y = newYAxis;
                        //Debug.Log(position.y);
                        judgeLineTransform.position = position;

                        // 根据newYAxis的值，实时改变correspondingJudgePlane下所有SubJudgePlane实例的透明度
                        ChangeSubJudgePlaneTransparency(correspondingJudgePlane, newYAxis);
                    }
                }
            }
        }

        if (TapsParent != null && audioSource.isPlaying)
        {
            for (int i = 0; i < TapsParent.transform.childCount; i++)
            {
                Transform childTransform = TapsParent.transform.GetChild(i);
                GameObject tapInstance = childTransform.gameObject;
                Vector3 currentPosition = childTransform.position;

                if (currentPosition.z > 0 && !tapReachedJudgmentLine[tapInstance])
                {
                    tapReachedJudgmentLine[tapInstance] = true;
                    TapSoundEffect.Play();

                    // 获取或添加Animator组件
                    Animator animator = tapInstance.GetComponent<Animator>();
                    if (animator == null)
                    {
                        animator = tapInstance.AddComponent<Animator>();
                    }

                    // 设置Animator的动画控制器（假设你已经创建好了名为TapAnimationController的动画控制器）
                    RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("Animations/TapHitEffectController");
                    if (controller == null)
                    {
                        Debug.LogError("无法加载AnimatorController资源，请检查资源路径和文件是否存在！");
                    }
                    animator.runtimeAnimatorController = controller;

                    // 播放动画
                    animator.Play("TapHitEffect");

                    // 检查动画是否播放完毕（这里使用一个简单的方法来模拟，实际可能需要更精确的判断）
                    StartCoroutine(CheckAnimationEnd(animator, tapInstance));
                }
                //播放动画时，Tap位置不再变化
                if (!tapReachedJudgmentLine[tapInstance])
                { 
                    currentPosition.z += zAxisDecreasePerFrame;
                    childTransform.position = currentPosition;
                }
            }
        }
    }

    // 加载歌曲音频文件和谱面文件
    private void LoadMusicAndChart()
    {
        string musicPath = SongAndChartData.GetMusicFilePath();
        //Debug.LogError(musicPath);
        string chartPath = SongAndChartData.GetChartFilePath();

        // 判断如果没有获取到路径（即直接加载AutoPlay场景时），默认按照第一首歌曲加载
        if (string.IsNullOrEmpty(musicPath) || string.IsNullOrEmpty(chartPath))
        {
            SongAndChartData.SetSelectedSong("Accelerate");
            musicPath = SongAndChartData.GetMusicFilePath();
            chartPath = SongAndChartData.GetChartFilePath();
        }

        audioSource = gameObject.GetComponent<AudioSource>();
        audioSource.clip = Resources.Load<AudioClip>(Path.ChangeExtension(musicPath, null));
        //Debug.LogError(audioSource.clip);


        if (File.Exists(chartPath))
        {
            chart = Chart.ImportFromJson(chartPath);

            // 实例化JudgePlane预制体以及对应的JudgeLine预制体
            InstantiateJudgePlanesAndJudgeLines();
            InstantiateTaps();

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
        while (audioSource.isPlaying){ yield return null; }

        // 歌曲播放结束后，跳回选歌场景
        SceneManager.LoadScene("SongSelect");
    }

    private void InstantiateJudgePlanesAndJudgeLines()
    {
        if (chart != null && chart.judgePlanes != null)
        {
            // 通过路径获取JudgeLine预制体的引用，假设与JudgePlane预制体在相同路径下
            GameObject judgeLinePrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GamePlay/JudgeLine.prefab", typeof(GameObject));
            if (judgeLinePrefab != null)
            {
                foreach (var judgePlane in chart.judgePlanes)
                {
                    int judgePlaneIndex = judgePlane.id;
                    // 创建一个空物体作为JudgePlane实例的父物体，用于统一管理和规范命名
                    GameObject judgePlaneParent = new GameObject($"JudgePlane{judgePlaneIndex}");
                    judgePlaneParent.transform.position = new Vector3(0, 0, 0);
                    // 将judgePlaneParent设置为ChartGameObjects的子物体
                    judgePlaneParent.transform.SetParent(JudgePlanesParent.transform);
                    int subJudgePlaneIndex = 1;

                    foreach (var subJudgePlane in judgePlane.subJudgePlaneList)
                    {
                        // 将StartY和EndY映射为世界坐标并放大到合适范围（0 - HeightParams.HeightDefault，这里假设HeightParams.HeightDefault为6）
                        float startYWorld = subJudgePlane.startY * HeightParams.HeightDefault;
                        float endYWorld = subJudgePlane.endY * HeightParams.HeightDefault;

                        // 根据SubJudgePlane的StartT来设置实例的Z轴位置（这里将变量名修改得更清晰些，叫zPositionForStartT）
                        //float zPositionForStartT = subJudgePlane.startT * SpeedParams.NoteSpeedDefault;

                        float zPositionForStartT = CalculateZAxisPosition(subJudgePlane.startT);

                        // 计算在Z轴方向的长度（之前代码中的height变量，这里改为lengthForZAxis）
                        float lengthForZAxis = (subJudgePlane.endT - subJudgePlane.startT) * SpeedParams.NoteSpeedDefault;

                        // 假设获取到一个目标点的世界坐标
                        Vector3 StartPoint = new Vector3(0, startYWorld, 0);
                        Vector3 EndPoint = new Vector3(0, endYWorld, 0);


                        // 计算StartXWorld和EndXWorld，确保在屏幕左右各留10%的距离
                        float startXWorld = CalculateWorldUnitToScreenPixelXAtPosition(StartPoint);
                        float endXWorld = CalculateWorldUnitToScreenPixelXAtPosition(EndPoint);

                        Vector3 point1 = new Vector3(-startXWorld, startYWorld, zPositionForStartT);
                        Vector3 point2 = new Vector3(startXWorld, startYWorld, zPositionForStartT);
                        Vector3 point3 = new Vector3(endXWorld, endYWorld, zPositionForStartT - lengthForZAxis);
                        Vector3 point4 = new Vector3(-endXWorld, endYWorld, zPositionForStartT - lengthForZAxis);

                        GameObject instance = CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, JudgePlaneSprite, $"Sub{subJudgePlaneIndex}", judgePlaneParent);
                        // 如果为第一个subJudgePlane
                        if (subJudgePlaneIndex == 1)
                        {
                            // 实例化JudgeLine预制体，并将其设置为JudgePlane实例的子物体，命名为JudgeLine1、JudgeLine2等
                            GameObject judgeLineInstance = Instantiate(judgeLinePrefab, instance.transform);
                            judgeLineInstance.name = $"JudgeLine{judgePlaneIndex}";
                            judgeLineInstance.transform.SetParent(JudgeLinesParent.transform);
                            // 初始化JudgeLine实例的位置与JudgePlane下第一个SubJudgePlane的位置一致（这里简单示例，可根据实际需求调整更准确的初始化逻辑）
                            judgeLineInstance.transform.position = instance.transform.position;
                        }

                        subJudgePlaneIndex++;
                    }
                }
            }
            else
            {
                Debug.LogError("无法加载JudgePlane预制体或JudgeLine预制体！");
            }
        }
    }

    private void InstantiateTaps()
    {
        if (chart != null && chart.taps != null)
        {
            // 假设Tap预制体的加载路径，你需要根据实际情况修改
            GameObject tapPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GamePlay/Tap.prefab", typeof(GameObject));
            if (tapPrefab != null)
            {
                float tapXAxisLength = 0; // 先在外层定义变量，初始化为0，后续根据实际情况赋值
                //MeshRenderer meshRenderer = tapPrefab.GetComponent<MeshRenderer>();
                //if (meshRenderer != null)
                //{
                //    //获取Tap在X轴的长度（用于缩放）
                //    tapXAxisLength = meshRenderer.bounds.size.x;
                //    //Debug.Log(tapXAxisLength);
                //}
                //else
                //{
                //    Debug.LogError($"Tap预制体实例 {tapPrefab.name} 缺少MeshRenderer组件，无法获取X轴长度进行缩放设置！");
                //}
                SpriteRenderer spriteRenderer = tapPrefab.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    //获取Tap在X轴的长度（用于缩放）
                    tapXAxisLength = spriteRenderer.sprite.bounds.size.x;
                }
                else
                {
                    Debug.LogError($"Tap预制体实例 {tapPrefab.name} 缺少SpriteRenderer组件，无法获取X轴长度进行缩放设置！");
                }

                int tapIndex = 1;
                foreach (var tap in chart.taps)
                {
                    // 实例化Tap预制体
                    GameObject tapInstance = Instantiate(tapPrefab);
                    tapInstance.name = $"Tap{tapIndex}"; // 命名
                    //Debug.Log(tap.startT);

                    // 将Tap设置为ChartGameObjects的子物体
                    tapInstance.transform.SetParent(TapsParent.transform);

                    // 将Tap实例添加到字典中，并初始化为false
                    tapReachedJudgmentLine[tapInstance] = false;

                    // 获取关联的JudgePlane实例
                    JudgePlane associatedJudgePlaneObject = GetCorrespondingJudgePlane(tap.associatedPlaneId);
                    if (associatedJudgePlaneObject != null)
                    {
                        // 获取关联JudgePlane在Tap开始时间的Y轴坐标
                        float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(tap.startT);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（封装成方法方便复用，以下是示例方法定义，参数需根据实际情况传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint);

                        // 计算Tap的X轴坐标
                        float startXWorld = worldUnitToScreenPixelX * tap.startX / ChartParams.XaxisMax;

                        // 根据noteSize折算到X轴世界坐标长度，计算每单位noteSize对应的世界坐标长度
                        float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;
                        //Debug.Log(noteSizeWorldLengthPerUnit);

                        // 根据Tap本身在X轴的世界坐标长度和noteSize计算X轴的缩放值
                        float xAxisScale = noteSizeWorldLengthPerUnit / tapXAxisLength * tap.noteSize;

                        // 设置Tap实例的缩放比例（只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        tapInstance.transform.localScale = new Vector3(xAxisScale, 1, 1);

                        // 设置Tap实例的位置（X、Y、Z轴坐标）
                        float zPositionForStartT = CalculateZAxisPosition(tap.startT); // 封装Z轴坐标计算方法，参考InstantiateJudgePlanesAndJudgeLines方法里的逻辑
                        tapInstance.transform.position = new Vector3(startXWorld, yAxisPosition, zPositionForStartT);
                        //Debug.Log(tapInstance.transform.position);
                    }

                    // 检查点键是否在规定的X轴坐标范围内，如果不在范围，可进行相应处理，比如隐藏或者输出警告等（这里简单示例输出警告）
                    if (!tap.IsInXAxisRange())
                    {
                        Debug.LogWarning($"Tap with startT: {tap.startT} and startX: {tap.startX} is out of X-axis range!");
                    }

                    tapIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载Tap预制体！");
            }
        }
    }

    // 封装计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度的方法（示例，参数需传入合适的世界坐标点，这里参考InstantiateJudgePlanesAndJudgeLines方法里的逻辑）
    private float CalculateWorldUnitToScreenPixelXAtPosition(Vector3 worldPosition)
    {
        // 获取屏幕宽度
        float screenWidth = Screen.width;
        float targetHorizontalMargin = HorizontalParams.HorizontalMargin; // 目标水平边距，即离屏幕边缘10%的距离，可根据需求调整

        // 计算水平可视范围（考虑边距后的有效宽度）
        float horizontalVisibleRange = screenWidth * (1 - 2 * targetHorizontalMargin);

        Vector3 Point = worldPosition;

        float WorldUnitToScreenPixelX = Utility.CalculateWorldUnitToScreenPixelAtDistance(Point);

        float XWorld = horizontalVisibleRange / 2 / WorldUnitToScreenPixelX;

        return XWorld;
    }


    // 封装计算Z轴坐标的方法（参考InstantiateJudgePlanesAndJudgeLines方法里的逻辑，根据实际情况传入对应的开始时间等参数）
    private float CalculateZAxisPosition(float startTime)
    {
        // 假设存在SpeedParams.NoteSpeedDefault这个速度参数，你需根据实际情况调整
        return -startTime * SpeedParams.NoteSpeedDefault;
    }

    // 通过JudgeLine的名字获取对应的JudgePlane实例（根据名字中的id匹配）
    private JudgePlane GetCorrespondingJudgePlane(string judgeLineName)
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

    //根据judgePlane的id找到对应的JudgePlane
    private JudgePlane GetCorrespondingJudgePlane(int judgePlaneId)
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

    // 方法用于根据给定的Y轴坐标值改变对应JudgePlane下所有SubJudgePlane实例的透明度
    private void ChangeSubJudgePlaneTransparency(JudgePlane judgePlane, float yAxisValue)
    {
        // 计算透明度差值，根据给定的对应关系计算斜率
        float alphaDelta = (0.5f - 0.8f) / 6f;
        // 根据线性关系计算当前透明度值，确保透明度范围在0到1之间
        float currentAlpha = Mathf.Clamp(0.8f + alphaDelta * yAxisValue, 0, 1);
        //Debug.Log(currentAlpha);

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
    // 协程方法用于检查动画是否播放完毕，播放完毕后销毁Tap实例
    private IEnumerator CheckAnimationEnd(Animator animator, GameObject tapInstance)
    {
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        // 动画播放完毕后销毁Tap实例
        Destroy(tapInstance);
    }
}



// 根据chart中的SubJudgePlane信息实例化JudgePlane预制体以及对应的JudgeLine预制体
//private void InstantiateJudgePlanesAndJudgeLines()
//{
//    if (chart != null && chart.judgePlanes != null)
//    {
//        // 通过路径获取JudgePlane预制体的引用，这里需要根据你的实际项目结构调整路径
//        GameObject judgePlanePrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GamePlay/JudgePlane.prefab", typeof(GameObject));
//        // 通过路径获取JudgeLine预制体的引用，假设与JudgePlane预制体在相同路径下
//        GameObject judgeLinePrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GamePlay/JudgeLine.prefab", typeof(GameObject));
//        if (judgePlanePrefab != null && judgeLinePrefab != null)
//        {
//            foreach (var judgePlane in chart.judgePlanes)
//            {
//                int judgePlaneIndex = judgePlane.id;
//                // 创建一个空物体作为JudgePlane实例的父物体，用于统一管理和规范命名
//                GameObject judgePlaneParent = new GameObject($"JudgePlane{judgePlaneIndex}");
//                // 将judgePlaneParent设置为ChartGameObjects的子物体
//                judgePlaneParent.transform.SetParent(chartGameObjectsParent.transform);

//                int subJudgePlaneIndex = 1;
//                foreach (var subJudgePlane in judgePlane.subJudgePlaneList)
//                {

//                    // 实例化JudgePlane预制体，并将其设置为JudgePlane父物体的子物体
//                    GameObject instance = Instantiate(judgePlanePrefab, judgePlaneParent.transform);

//                    // 设置实例的名称为SubJudgePlane对应的规范名称
//                    instance.name = $"Sub{subJudgePlaneIndex}";

//                    // 将当前实例设置为对应的JudgePlane父物体的子物体（这是添加的关键步骤）
//                    instance.transform.SetParent(judgePlaneParent.transform);

//                    // 将StartY和EndY映射为世界坐标并放大到合适范围（0 - HeightParams.HeightDefault，这里假设HeightParams.HeightDefault为6）
//                    float startYWorld = subJudgePlane.startY * HeightParams.HeightDefault;
//                    float endYWorld = subJudgePlane.endY * HeightParams.HeightDefault;

//                    // 根据SubJudgePlane的StartT来设置实例的Z轴位置（这里将变量名修改得更清晰些，叫zPositionForStartT）
//                    float zPositionForStartT = subJudgePlane.startT * SpeedParams.NoteSpeedDefault;
//                    instance.transform.position = new Vector3(instance.transform.position.x, startYWorld, -zPositionForStartT);

//                    // 获取实例的Sprite Renderer组件，用于后续调整平铺模式下的高度参数
//                    SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
//                    if (spriteRenderer != null)
//                    {
//                        // 计算在Z轴方向的长度（之前代码中的height变量，这里改为lengthForZAxis）
//                        float lengthForZAxis = (subJudgePlane.endT - subJudgePlane.startT) * SpeedParams.NoteSpeedDefault;
//                        // 计算高度方向的长度（EndY - StartY），注意方向（正负）
//                        float lengthForYAxis = endYWorld - startYWorld;

//                        // 根据勾股定理计算斜边长度，即物体实际的长度
//                        float actualLength = Mathf.Sqrt(lengthForZAxis * lengthForZAxis + lengthForYAxis * lengthForYAxis);

//                        // 计算旋转角度（使用反正切函数，根据高和底边来计算，注意角度正负处理）
//                        float angle = Mathf.Atan2(lengthForYAxis, lengthForZAxis) * Mathf.Rad2Deg;

//                        // 设置实例的旋转信息
//                        instance.transform.rotation = Quaternion.Euler(instance.transform.rotation.eulerAngles.x + angle, instance.transform.rotation.eulerAngles.y, instance.transform.rotation.eulerAngles.z);

//                        // 设置Sprite Renderer组件在平铺模式下的高度参数，使用计算出的实际长度
//                        spriteRenderer.size = new Vector2(spriteRenderer.size.x, actualLength);
//                    }
//                    else
//                    {
//                        Debug.LogError("实例缺少SpriteRenderer组件，无法设置平铺模式下的高度参数！");
//                    }

//                    //如果为第一个subJudgePlane
//                    if (subJudgePlaneIndex == 1)
//                    {
//                        // 实例化JudgeLine预制体，并将其设置为JudgePlane实例的子物体，命名为JudgeLine1、JudgeLine2等
//                        GameObject judgeLineInstance = Instantiate(judgeLinePrefab, instance.transform);
//                        judgeLineInstance.name = $"JudgeLine{judgePlaneIndex}";
//                        judgeLineInstance.transform.SetParent(JudgeLinesParent.transform);
//                        // 初始化JudgeLine实例的位置与JudgePlane下第一个SubJudgePlane的位置一致（这里简单示例，可根据实际需求调整更准确的初始化逻辑）
//                        judgeLineInstance.transform.position = instance.transform.position;
//                    }

//                    subJudgePlaneIndex++;
//                }
//            }
//        }
//        else
//        {
//            Debug.LogError("无法加载JudgePlane预制体或JudgeLine预制体！");
//        }
//    }
//}