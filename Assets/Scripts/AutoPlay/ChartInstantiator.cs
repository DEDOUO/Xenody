using UnityEngine;
using UnityEditor;
using Params;
using static Utility;
using System.Collections.Generic;
//using UnityEngine.UIElements;
using System;
//using Unity.VisualScripting;
using Note;
using static JudgePlane;
using System.Linq;
//using System.Drawing;
//using UnityEngine.Rendering;



public class ChartInstantiator : MonoBehaviour
{
    private GameObject JudgePlanesParent;
    private GameObject JudgeLinesParent;
    private GameObject ColorLinesParent;
    private GameObject TapsParent;
    private GameObject SlidesParent;
    private GameObject FlicksParent;
    private GameObject FlickArrowsParent;
    private GameObject HoldsParent;
    private GameObject StarsParent;
    private GameObject SubStarsParent;

    private Sprite JudgePlaneSprite;
    private Sprite HoldSprite;
    private Sprite WhiteSprite;

    private GlobalRenderOrderManager renderOrderManager;
    private GameObject videoPlayerContainer;

    private Dictionary<float, List<string>> startTimeToInstanceNames = new Dictionary<float, List<string>>(); // 存储startT到对应实例名列表的映射


    // 新增的公共方法，用于接收各个参数并赋值给对应的私有变量
    public void SetParameters(GameObject judgePlanesParent, GameObject judgeLinesParent, GameObject colorLinesParent, GameObject tapsParent, GameObject slidesParent, GameObject flicksParent, GameObject flickarrowsParent, GameObject holdsParent, GameObject starsParent,
        GameObject subStarsParent,
        Sprite judgePlaneSprite, Sprite holdSprite, GlobalRenderOrderManager globalRenderOrderManager, GameObject animatorContainer)
    {
        JudgePlanesParent = judgePlanesParent;
        JudgeLinesParent = judgeLinesParent;
        ColorLinesParent = colorLinesParent;
        TapsParent = tapsParent;
        SlidesParent = slidesParent;
        FlicksParent = flicksParent;
        FlickArrowsParent = flickarrowsParent;
        HoldsParent = holdsParent;
        StarsParent = starsParent;
        SubStarsParent = subStarsParent;
        JudgePlaneSprite = judgePlaneSprite;
        HoldSprite = holdSprite;
        renderOrderManager = globalRenderOrderManager;
        videoPlayerContainer = animatorContainer;

        WhiteSprite = Resources.Load<Sprite>("Sprites/WhiteSprite");
        if (WhiteSprite == null)
        {
            Debug.LogError("Failed to load sprite: Sprites/WhiteSprite");
        }

    }

    private void PrepareStartTimeMapping(Chart chart)
    {
        List<KeyValuePair<float, string>> allPairs = new List<KeyValuePair<float, string>>();

        if (chart.taps != null)
        {
            for (int i = 0; i < chart.taps.Count; i++)
            {
                var tap = chart.taps[i];
                string instanceName = $"Tap{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(tap.startT, instanceName));
            }
        }
        if (chart.slides != null)
        {
            for (int i = 0; i < chart.slides.Count; i++)
            {
                var slide = chart.slides[i];
                string instanceName = $"Slide{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(slide.startT, instanceName));
            }
        }
        if (chart.flicks != null)
        {
            for (int i = 0; i < chart.flicks.Count; i++)
            {
                var flick = chart.flicks[i];
                string instanceName = $"Flick{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(flick.startT, instanceName));
            }
        }
        if (chart.holds != null)
        {
            for (int i = 0; i < chart.holds.Count; i++)
            {
                var hold = chart.holds[i];
                float startT = hold.GetFirstSubHoldStartTime();
                string instanceName = $"Hold{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(startT, instanceName));
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

        //输出 startTimeToInstanceNames 所有元素
        //foreach (var startTime in startTimeToInstanceNames.Keys)
        //{
        //    Debug.Log($"Start Time: {startTime}");
        //    foreach (var instanceName in startTimeToInstanceNames[startTime])
        //    {
        //        Debug.Log($"Instance Name: {instanceName}");
        //    }
        //}
    }

    public void InstantiateAll(Chart chart)
    {
        PrepareStartTimeMapping(chart);
        InstantiateJudgePlanes(chart);
        InstantiateJudgeLines(chart);
        InstantiateTaps(chart);
        InstantiateSlides(chart);
        InstantiateFlicks(chart);
        InstantiateHolds(chart);
        InstantiateStarHeads(chart);
        InstantiateSubStars(chart);
    }


    public void InstantiateJudgePlanes(Chart chart)
    {
        if (chart != null && chart.judgePlanes != null)
        {
            foreach (var judgePlane in chart.judgePlanes)
            {
                //Debug.Log(judgePlane.color);
                int RenderQueue = 3000;
                int judgePlaneIndex = judgePlane.id;
                // 创建一个空物体作为JudgePlane实例的父物体，用于统一管理和规范命名
                GameObject judgePlaneParent = new GameObject($"JudgePlane{judgePlaneIndex}");
                judgePlaneParent.transform.position = new Vector3(0, 0, 0);
                // 将judgePlaneParent设置为JudgePlanesParent的子物体
                judgePlaneParent.transform.SetParent(JudgePlanesParent.transform);
                // 继承父物体的图层
                //int parentLayer = JudgePlanesParent.layer;
                judgePlaneParent.layer = JudgePlanesParent.layer;

                // 创建 LeftColorLine 作为左侧亮条合并物体的父物体
                GameObject leftColorLine = new GameObject($"LeftColorLine{judgePlaneIndex}");
                leftColorLine.transform.position = new Vector3(0, 0, 0);
                leftColorLine.transform.SetParent(ColorLinesParent.transform);
                leftColorLine.layer = ColorLinesParent.layer;

                // 创建 RightColorLine 作为右侧亮条合并物体的父物体
                GameObject rightColorLine = new GameObject($"RightColorLine{judgePlaneIndex}");
                rightColorLine.transform.position = new Vector3(0, 0, 0);
                rightColorLine.transform.SetParent(ColorLinesParent.transform);
                rightColorLine.layer = ColorLinesParent.layer;

                int subJudgePlaneIndex = 1;

                foreach (var subJudgePlane in judgePlane.subJudgePlaneList)
                {
                    //List<GameObject> segmentInstances = new List<GameObject>();
                    List<GameObject> judgePlaneInstances = new List<GameObject>();
                    List<GameObject> leftStripInstances = new List<GameObject>();
                    List<GameObject> rightStripInstances = new List<GameObject>();

                    // 根据SubJudgePlane的函数类型来决定如何处理
                    switch (subJudgePlane.yAxisFunction)
                    {
                        case TransFunctionType.Linear:
                            List<GameObject> objectsToCombine = CreateJudgePlaneAndColorLinesQuad(subJudgePlane.startY, subJudgePlane.endY, subJudgePlane.startT, subJudgePlane.endT,
                                JudgePlaneSprite, $"Sub{subJudgePlaneIndex}", judgePlaneParent, leftColorLine, rightColorLine, RenderQueue, judgePlane.color);
                            GameObject subjudgePlaneInstance = objectsToCombine[0];
                            GameObject LeftStripInstance = objectsToCombine[1];
                            GameObject RightStripInstance = objectsToCombine[2];

                            ProcessCombinedInstance(subjudgePlaneInstance, judgePlaneParent, judgePlaneParent.layer);
                            ProcessCombinedInstance(LeftStripInstance, leftColorLine, leftColorLine.layer);
                            ProcessCombinedInstance(RightStripInstance, leftColorLine, leftColorLine.layer);

                            break;

                        case TransFunctionType.Sin:
                        case TransFunctionType.Cos:
                            // 精细度设为8，用于分割时间区间
                            int segments = FinenessParams.Segment;
                            float timeStep = (subJudgePlane.endT - subJudgePlane.startT) / segments;

                            for (int i = 0; i < segments; i++)
                            {
                                float startT = subJudgePlane.startT + i * timeStep;
                                float endT = subJudgePlane.startT + (i + 1) * timeStep;
                                float startY = CalculatePosition(startT, subJudgePlane.startT, subJudgePlane.startY, subJudgePlane.endT, subJudgePlane.endY, subJudgePlane.yAxisFunction);
                                float endY = CalculatePosition(endT, subJudgePlane.startT, subJudgePlane.startY, subJudgePlane.endT, subJudgePlane.endY, subJudgePlane.yAxisFunction);
                                List<GameObject> ObjectsToCombine = CreateJudgePlaneAndColorLinesQuad(startY, endY, startT, endT,
                                    JudgePlaneSprite, $"Sub{subJudgePlaneIndex}_{i + 1}", judgePlaneParent, leftColorLine, rightColorLine, RenderQueue, judgePlane.color);
                                //segmentInstances.AddRange(subObjectsToCombine);
                                judgePlaneInstances.Add(ObjectsToCombine[0]);
                                leftStripInstances.Add(ObjectsToCombine[1]);
                                rightStripInstances.Add(ObjectsToCombine[2]);
                            }

                            // 合并 SubJudgePlane
                            GameObject combinedJudgePlane = CombineInstances(judgePlaneInstances);
                            combinedJudgePlane.name = $"Sub{subJudgePlaneIndex}";
                            ProcessCombinedInstance(combinedJudgePlane, judgePlaneParent, judgePlaneParent.layer);
                            //combinedJudgePlane.transform.SetParent(judgePlaneParent.transform);

                            // 合并左侧亮条
                            GameObject combinedLeftStrip = CombineInstances(leftStripInstances);
                            combinedLeftStrip.name = $"Sub{subJudgePlaneIndex}";
                            ProcessCombinedInstance(combinedLeftStrip, leftColorLine, leftColorLine.layer);
                            //combinedLeftStrip.transform.SetParent(leftColorLine.transform);

                            // 合并右侧亮条
                            GameObject combinedRightStrip = CombineInstances(rightStripInstances);
                            combinedRightStrip.name = $"Sub {subJudgePlaneIndex}";
                            ProcessCombinedInstance(combinedRightStrip, rightColorLine, rightColorLine.layer);
                            //combinedRightStrip.transform.SetParent(rightColorLine.transform);

                            break;
                    }
                    subJudgePlaneIndex++;
                }
            }
        }
    }


    private void ProcessCombinedInstance(GameObject combinedInstance, GameObject judgePlaneParent, int parentLayer)
    {
        // 将合并后的Instance设置为对应的父物体的子物体
        combinedInstance.transform.SetParent(judgePlaneParent.transform);
        // 继承父物体的图层
        combinedInstance.layer = parentLayer;

    }

    public void InstantiateJudgeLines(Chart chart)
    {
        if (chart != null && chart.judgePlanes != null)
        {
            // 通过路径获取JudgeLine预制体的引用，假设与JudgePlane预制体在相同路径下
            GameObject judgeLinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/JudgeLine");
            if (judgeLinePrefab != null)
            {
                foreach (JudgePlane judgePlane in chart.judgePlanes)
                {
                    int judgePlaneIndex = judgePlane.id;
                    //获取第一个SubJudgePlane
                    SubJudgePlane subJudgePlane = judgePlane.subJudgePlaneList[0];
                    // 实例化JudgeLine预制体，命名为JudgeLine1、JudgeLine2等
                    GameObject judgeLineInstance = Instantiate(judgeLinePrefab);
                    judgeLineInstance.name = $"JudgeLine{judgePlaneIndex}";
                    RectTransform judgeLineRectTransform = judgeLineInstance.GetComponent<RectTransform>();
                    judgeLineInstance.transform.SetParent(JudgeLinesParent.transform);
                    // 继承父物体的图层
                    int parentLayer = JudgeLinesParent.layer;
                    judgeLineInstance.layer = parentLayer;

                    //获取初始Y轴坐标并转化为屏幕坐标
                    float YAxisUniform = judgePlane.GetPlaneYAxis(subJudgePlane.startT) / HeightParams.HeightDefault;
                    Vector2 Position = ScalePositionToScreenJudgeLine(new Vector2(0f, YAxisUniform), JudgeLinesParent.GetComponent<RectTransform>());
                    judgeLineRectTransform.anchoredPosition3D = new Vector3(Position.x, Position.y, 0);
                    judgeLineRectTransform.localRotation = Quaternion.Euler(0, 0, 0);
                    judgeLineRectTransform.localScale = new Vector3(1000000, 100, 1);

                }
            }
            else
            {
                Debug.LogError("无法加载JudgeLine预制体！");
            }
        }
    }

    public void InstantiateTaps(Chart chart)
    {
        if (chart != null && chart.taps != null)
        {
            // 假设Tap预制体的加载路径，你需要根据实际情况修改
            GameObject tapPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Tap");
            GameObject tapOutlinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/TapOutline");

            if (tapPrefab != null && tapOutlinePrefab != null)
            {
                float tapXAxisLength = 0; // 先在外层定义变量，初始化为0，后续根据实际情况赋值
                SpriteRenderer spriteRenderer = tapPrefab.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    //获取Tap在X轴的长度（用于缩放）
                    tapXAxisLength = spriteRenderer.sprite.bounds.size.x;
                    //Debug.Log(tapXAxisLength);
                }
                else
                {
                    Debug.LogError($"Tap预制体实例 {tapPrefab.name} 缺少SpriteRenderer组件，无法获取X轴长度进行缩放设置！");
                }

                int tapIndex = 1;
                foreach (var tap in chart.taps)
                {
                    GameObject prefabToInstantiate;
                    // 判断是否有多押情况
                    if (startTimeToInstanceNames.ContainsKey(tap.startT) && startTimeToInstanceNames[tap.startT].Count > 1)
                    {
                        //Debug.Log(tap.startT);
                        prefabToInstantiate = tapOutlinePrefab;
                    }
                    else
                    {
                        prefabToInstantiate = tapPrefab;
                    }

                    // 实例化Tap预制体
                    GameObject tapInstance = Instantiate(prefabToInstantiate);
                    tapInstance.name = $"Tap{tapIndex}"; // 命名
                                                         // 将Tap设置为ChartGameObjects的子物体
                    tapInstance.transform.SetParent(TapsParent.transform);
                    // 继承父物体的图层
                    int parentLayer = TapsParent.layer;
                    tapInstance.layer = parentLayer;

                    // 获取关联的JudgePlane实例
                    JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(tap.associatedPlaneId);
                    if (associatedJudgePlaneObject != null)
                    {
                        // 获取关联JudgePlane在Tap开始时间的Y轴坐标
                        float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(tap.startT);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（封装成方法方便复用，以下是示例方法定义，参数需根据实际情况传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);
                        //Debug.Log(worldUnitToScreenPixelX);

                        // 计算Tap的X轴坐标
                        float startXWorld = worldUnitToScreenPixelX * tap.startX / ChartParams.XaxisMax;

                        // 根据noteSize折算到X轴世界坐标长度，计算每单位noteSize对应的世界坐标长度
                        float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Tap本身在X轴的世界坐标长度和noteSize计算X轴的缩放值
                        float xAxisScale = noteSizeWorldLengthPerUnit / tapXAxisLength * tap.noteSize;

                        // 设置Tap实例的缩放比例（只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        tapInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.ZAxisRate, 1);

                        // 仅在多押时调整_Thickness的值
                        if (prefabToInstantiate == tapOutlinePrefab)
                        {
                            // 获取Tap实例的Material
                            Material tapMaterial = tapInstance.GetComponent<Renderer>().material;
                            if (tapMaterial != null)
                            {
                                // 获取shader中的_Thickness属性ID
                                int thicknessPropertyID = Shader.PropertyToID("_Thickness");
                                // 根据xAxisScale调整_Thickness的值
                                float adjustedThickness = tapMaterial.GetFloat(thicknessPropertyID) / xAxisScale;
                                tapMaterial.SetFloat(thicknessPropertyID, adjustedThickness);
                            }
                            else
                            {
                                Debug.LogError($"Tap实例 {tapInstance.name} 缺少Renderer组件，无法获取Material调整_Thickness！");
                            }
                        }

                        // 设置Tap实例的位置（X、Y、Z轴坐标）
                        float zPositionForStartT = CalculateZAxisPosition(tap.startT);
                        tapInstance.transform.position = new Vector3(-startXWorld, yAxisPosition, zPositionForStartT);
                        //Debug.Log(tapInstance.transform.position);
                    }

                    // 检查点键是否在规定的X轴坐标范围内，如果不在范围，可进行相应处理，比如隐藏或者输出警告等（这里简单示例输出警告）
                    if (!tap.IsInXAxisRange())
                    {
                        Debug.LogWarning($"Tap with startT: {tap.startT} and startX: {tap.startX} is out of X - axis range!");
                    }

                    tapIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载Tap或TapOutline预制体！");
            }
        }
    }

    public void InstantiateSlides(Chart chart)
    {
        if (chart != null && chart.slides != null)
        {
            // 加载常规Slide预制体和多押时的Slide预制体
            GameObject slidePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Slide");
            GameObject slideOutlinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/SlideOutline");

            if (slidePrefab != null && slideOutlinePrefab != null)
            {
                float slideWidth = 0; // 用于存储Slide在X轴方向的宽度（用于后续缩放等操作）
                SpriteRenderer spriteRenderer = slidePrefab.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    // 获取Slide在X轴的宽度（用于缩放等处理）
                    slideWidth = spriteRenderer.sprite.bounds.size.x;
                    //Debug.Log(slideWidth);
                }
                else
                {
                    Debug.LogError($"Slide预制体实例 {slidePrefab.name} 缺少SpriteRenderer组件，无法获取X轴宽度进行相关设置！");
                }

                int slideIndex = 1;
                foreach (var slide in chart.slides)
                {
                    GameObject prefabToInstantiate;
                    // 判断是否有多押情况
                    if (startTimeToInstanceNames.ContainsKey(slide.startT) && startTimeToInstanceNames[slide.startT].Count > 1)
                    {
                        prefabToInstantiate = slideOutlinePrefab;
                    }
                    else
                    {
                        prefabToInstantiate = slidePrefab;
                    }

                    // 实例化Slide预制体
                    GameObject slideInstance = Instantiate(prefabToInstantiate);
                    slideInstance.name = $"Slide{slideIndex}"; // 命名

                    // 将Slide设置为合适的父物体的子物体，这里假设和Taps类似，有个SlidesParent，你可根据实际调整
                    slideInstance.transform.SetParent(SlidesParent.transform);
                    // 继承父物体的图层
                    int parentLayer = SlidesParent.layer;
                    slideInstance.layer = parentLayer;

                    // 获取关联的JudgePlane实例（假设Slide也有关联的JudgePlane，根据实际情况调整获取逻辑）
                    JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(slide.associatedPlaneId);
                    if (associatedJudgePlaneObject != null)
                    {
                        // 获取关联JudgePlane在Slide开始时间的Y轴坐标
                        float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(slide.startT);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（这里可复用已有的相关方法，假设已经有合适的方法定义，参数需根据实际传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);

                        // 计算Slide的X轴坐标（根据Slide相关参数和计算逻辑来确定，示例如下，需按实际调整）
                        float startXWorld = worldUnitToScreenPixelX * slide.startX / ChartParams.XaxisMax;

                        // 根据slideSize等参数折算到X轴世界坐标长度，计算每单位slideSize对应的世界坐标长度（示例逻辑，按实际修改）
                        float slideSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Slide本身在X轴的世界坐标宽度和slideSize等参数计算X轴的缩放值（示例，按需调整）
                        float xAxisScale = slideSizeWorldLengthPerUnit / slideWidth * slide.noteSize;
                        //Debug.Log(xAxisScale);

                        // 设置Slide实例的缩放比例（这里只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        slideInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.ZAxisRate, 1);

                        // 仅在多押时调整_Thickness的值
                        if (prefabToInstantiate == slideOutlinePrefab)
                        {
                            // 获取Slide实例的Material
                            Material slideMaterial = slideInstance.GetComponent<Renderer>().material;
                            if (slideMaterial != null)
                            {
                                // 获取shader中的_Thickness属性ID
                                int thicknessPropertyID = Shader.PropertyToID("_Thickness");
                                // 根据xAxisScale调整_Thickness的值
                                float adjustedThickness = slideMaterial.GetFloat(thicknessPropertyID) / xAxisScale;
                                slideMaterial.SetFloat(thicknessPropertyID, adjustedThickness);
                            }
                            else
                            {
                                Debug.LogError($"Slide实例 {slideInstance.name} 缺少Renderer组件，无法获取Material调整_Thickness！");
                            }
                        }

                        // 设置Slide实例的位置（X、Y、Z轴坐标，示例逻辑，按实际情况调整）
                        float zPositionForStartT = CalculateZAxisPosition(slide.startT);
                        slideInstance.transform.position = new Vector3(-startXWorld, yAxisPosition, zPositionForStartT);
                    }

                    // 检查Slide相关的范围等条件是否满足（这里简单示例输出警告，按实际需求完善检查逻辑）
                    if (!slide.IsInXAxisRange())
                    {
                        Debug.LogWarning($"Slide with startT: {slide.startT} and startX: {slide.startX} is out of valid range!");
                    }

                    slideIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载Slide或SlideOutline预制体！");
            }
        }
    }

    public void InstantiateFlicks(Chart chart)
    {
        if (chart != null && chart.flicks != null)
        {
            // 加载常规Flick预制体、多押时的Flick预制体和FlickArrow预制体
            GameObject flickPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Flick");
            GameObject flickOutlinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/FlickOutline");
            GameObject flickArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/FlickArrow");

            if (flickPrefab != null && flickOutlinePrefab != null && flickArrowPrefab != null)
            {
                float flickWidth = 0; // 用于存储Flick在X轴方向的宽度（用于后续缩放等操作）
                SpriteRenderer spriteRenderer = flickPrefab.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    // 获取Flick在X轴的宽度（用于缩放等处理）
                    flickWidth = spriteRenderer.sprite.bounds.size.x;
                }
                else
                {
                    Debug.LogError($"Flick预制体实例 {flickPrefab.name} 缺少SpriteRenderer组件，无法获取X轴宽度进行相关设置！");
                }

                int flickIndex = 1;
                foreach (var flick in chart.flicks)
                {
                    GameObject prefabToInstantiate;
                    // 判断是否有多押情况
                    if (startTimeToInstanceNames.ContainsKey(flick.startT) && startTimeToInstanceNames[flick.startT].Count > 1)
                    {
                        prefabToInstantiate = flickOutlinePrefab;
                    }
                    else
                    {
                        prefabToInstantiate = flickPrefab;
                    }

                    // 实例化Flick预制体
                    GameObject flickInstance = Instantiate(prefabToInstantiate);
                    flickInstance.name = $"Flick{flickIndex}"; // 命名

                    // 将Flick设置为合适的父物体的子物体，这里假设和Taps、Slides类似，有个FlicksParent，你可根据实际调整
                    flickInstance.transform.SetParent(FlicksParent.transform);
                    // 继承父物体的图层
                    int parentLayer = FlicksParent.layer;
                    flickInstance.layer = parentLayer;

                    // 获取关联的JudgePlane实例（假设Flick也有关联的JudgePlane，根据实际情况调整获取逻辑）
                    JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(flick.associatedPlaneId);
                    if (associatedJudgePlaneObject != null)
                    {
                        // 获取关联JudgePlane在Flick开始时间的Y轴坐标
                        float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(flick.startT);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（这里可复用已有的相关方法，假设已经有合适的方法定义，参数需根据实际传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);

                        // 计算Flick的X轴坐标（根据Flick相关参数和计算逻辑来确定，示例如下，需按实际调整）
                        float startXWorld = worldUnitToScreenPixelX * flick.startX / ChartParams.XaxisMax;

                        // 根据flickSize等参数折算到X轴世界坐标长度，计算每单位flickSize对应的世界坐标长度（示例逻辑，按实际修改）
                        float flickSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Flick本身在X轴的世界坐标宽度和flickSize等参数计算X轴的缩放值（示例，按需调整）
                        float xAxisScale = flickSizeWorldLengthPerUnit / flickWidth * flick.noteSize;

                        // 设置Flick实例的缩放比例（这里只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        flickInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.ZAxisRate, 1);

                        // 仅在多押时调整_Thickness的值
                        if (prefabToInstantiate == flickOutlinePrefab)
                        {
                            // 获取Flick实例的Material
                            Material flickMaterial = flickInstance.GetComponent<Renderer>().material;
                            if (flickMaterial != null)
                            {
                                // 获取shader中的_Thickness属性ID
                                int thicknessPropertyID = Shader.PropertyToID("_Thickness");
                                // 根据xAxisScale调整_Thickness的值
                                float adjustedThickness = flickMaterial.GetFloat(thicknessPropertyID) / xAxisScale;
                                flickMaterial.SetFloat(thicknessPropertyID, adjustedThickness);
                            }
                            else
                            {
                                Debug.LogError($"Flick实例 {flickInstance.name} 缺少Renderer组件，无法获取Material调整_Thickness！");
                            }
                        }

                        // 设置Flick实例的位置（X、Y、Z轴坐标，示例逻辑，按实际情况调整）
                        float zPositionForStartT = CalculateZAxisPosition(flick.startT);
                        flickInstance.transform.position = new Vector3(-startXWorld, yAxisPosition, zPositionForStartT);

                        Vector3 leftMiddleWorldPos = GetLeftMiddleWorldPosition(flickInstance);
                        //Debug.Log(leftMiddleWorldPos);

                        // 实例化Flick箭头预制体
                        GameObject flickArrowInstance = Instantiate(flickArrowPrefab);
                        flickArrowInstance.name = $"FlickArrow{flickIndex}";

                        // 设置Flick箭头实例的父物体为FlickArrowsParent
                        flickArrowInstance.transform.SetParent(FlickArrowsParent.transform);
                        // 只手动设置子物体的世界位置为父物体的世界位置
                        flickArrowInstance.transform.position = flickInstance.transform.position;

                        // 继承父物体的图层
                        int parentLayer2 = flickInstance.layer;
                        flickArrowInstance.layer = parentLayer2;

                        //调整FlickArrow的位置（针对横划情况）
                        AdjustFlickArrowPosition(flickArrowInstance, flickInstance, flick.flickDirection);
                    }

                    // 检查Flick相关的范围等条件是否满足（这里简单示例输出警告，按实际需求完善检查逻辑）
                    if (!flick.IsInXAxisRange())
                    {
                        Debug.LogWarning($"Flick with startT: {flick.startT} and startX: {flick.startX} is out of valid range!");
                    }

                    flickIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载Flick、FlickOutline或FlickArrow预制体！");
            }
        }
    }

    public void InstantiateStarHeads(Chart chart)
    {
        if (chart != null && chart.stars != null)
        {
            // 加载常规StarHead预制体和多押时的StarHead预制体
            GameObject starheadPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarHead");
            GameObject starheadOutlinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarHeadOutline");

            if (starheadPrefab != null && starheadOutlinePrefab != null)
            {
                float starheadXAxisLength = 0; // 先在外层定义变量，初始化为0，后续根据实际情况赋值
                SpriteRenderer spriteRenderer = starheadPrefab.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    // 获取StarHead在X轴的长度（用于缩放）
                    starheadXAxisLength = spriteRenderer.sprite.bounds.size.x;
                }
                else
                {
                    Debug.LogError($"starhead预制体实例 {starheadPrefab.name} 缺少SpriteRenderer组件，无法获取X轴长度进行缩放设置！");
                }

                int starIndex = 1;
                foreach (var star in chart.stars)
                {
                    GameObject prefabToInstantiate;
                    // 判断是否有多押情况
                    if (startTimeToInstanceNames.ContainsKey(star.starHeadT) && startTimeToInstanceNames[star.starHeadT].Count > 1)
                    {
                        prefabToInstantiate = starheadOutlinePrefab;
                    }
                    else
                    {
                        prefabToInstantiate = starheadPrefab;
                    }

                    // 实例化StarHead预制体
                    GameObject starheadInstance = Instantiate(prefabToInstantiate);
                    starheadInstance.name = $"StarHead{starIndex}"; // 命名
                                                                    // 将StarHead设置为ChartGameObjects的子物体
                    starheadInstance.transform.SetParent(StarsParent.transform);
                    // 继承父物体的图层
                    int parentLayer = StarsParent.layer;
                    starheadInstance.layer = parentLayer;

                    // 获取开始的X轴和Y轴坐标
                    Vector2 firstStarCoodinate = star.GetFirstSubStarCoordinates();
                    float xAxisPosition = firstStarCoodinate.x;
                    float yAxisPosition = firstStarCoodinate.y;
                    yAxisPosition *= HeightParams.HeightDefault;
                    // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围
                    Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                    float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);

                    // 计算X轴坐标
                    float startXWorld = worldUnitToScreenPixelX * xAxisPosition / ChartParams.XaxisMax;
                    // 根据noteSize折算到X轴世界坐标长度，计算每单位noteSize对应的世界坐标长度
                    float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;
                    // 根据StarHead本身在X轴的世界坐标长度和noteSize计算X轴的缩放值
                    float xAxisScale = noteSizeWorldLengthPerUnit / starheadXAxisLength * ChartParams.StarHeadXAxis;

                    // 设置StarHead实例的缩放比例
                    starheadInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.ZAxisRate, 1);

                    // 仅在多押时调整_Thickness的值
                    if (prefabToInstantiate == starheadOutlinePrefab)
                    {
                        // 获取StarHead实例的Material
                        Material starheadMaterial = starheadInstance.GetComponent<Renderer>().material;
                        if (starheadMaterial != null)
                        {
                            // 获取shader中的_Thickness属性ID
                            int thicknessPropertyID = Shader.PropertyToID("_Thickness");
                            // 根据xAxisScale调整_Thickness的值
                            float adjustedThickness = starheadMaterial.GetFloat(thicknessPropertyID) / xAxisScale;
                            starheadMaterial.SetFloat(thicknessPropertyID, adjustedThickness);
                        }
                        else
                        {
                            Debug.LogError($"StarHead实例 {starheadInstance.name} 缺少Renderer组件，无法获取Material调整_Thickness！");
                        }
                    }

                    // 设置StarHead实例的位置（X、Y、Z轴坐标）
                    float zPositionForStartT = CalculateZAxisPosition(star.starHeadT);
                    starheadInstance.transform.position = new Vector3(-startXWorld, yAxisPosition, zPositionForStartT);

                    // 检查点键是否在规定的X轴坐标范围内，如果不在范围，可进行相应处理，比如隐藏或者输出警告等（这里简单示例输出警告）
                    //if (!star.IsInAxisRange())
                    //{
                    //    Debug.LogWarning($"Star with starHeadT: {star.starHeadT} is out of Axis range!");
                    //}

                    starIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载StarHead或StarHeadOutline预制体！");
            }
        }
    }

    public void InstantiateHolds(Chart chart)
    {
        if (chart != null && chart.holds != null)
        {
            int holdIndex = 1;
            int RenderQueue = 2000;
            foreach (var hold in chart.holds)
            {
                // 创建一个空物体作为 hold 实例的父物体，用于统一管理和规范命名
                GameObject holdParent = new GameObject($"Hold{holdIndex}");
                holdParent.transform.position = new Vector3(0, 0, 0);
                // 将 holdParent 设置为 ChartGameObjects 的子物体
                holdParent.transform.SetParent(HoldsParent.transform);
                // 继承父物体的图层
                int parentLayer = HoldsParent.layer;
                holdParent.layer = parentLayer;

                JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
                if (associatedJudgePlaneObject != null)
                {
                    string shaderName = "MaskMaterial"; // 默认使用 MaskMaterial 作为 shader

                    List<GameObject> subHoldInstances = new List<GameObject>();
                    int subHoldIndex = 1;
                    foreach (var subHold in hold.subHoldList)
                    {
                        float startY = associatedJudgePlaneObject.GetPlaneYAxis(subHold.startT);
                        float endY = associatedJudgePlaneObject.GetPlaneYAxis(subHold.endT);

                        // 检查 SubHold 所在的 SubJudgePlane 是否为 Linear
                        bool isSubJudgePlaneLinear = associatedJudgePlaneObject.IsSubJudgePlaneLinear(subHold.startT, subHold.endT);

                        // 只有当两侧变化函数均为 Linear，且所在的 SubJudgePlane 为 Linear 时，才能一次性初始化
                        if (subHold.XLeftFunction == TransFunctionType.Linear && subHold.XRightFunction == TransFunctionType.Linear && isSubJudgePlaneLinear)
                        {
                            float startXMinWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startY, 0), HorizontalParams.HorizontalMargin) * subHold.startXMin / ChartParams.XaxisMax;
                            float startXMaxWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startY, 0), HorizontalParams.HorizontalMargin) * subHold.startXMax / ChartParams.XaxisMax;
                            float endXMinWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endY, 0), HorizontalParams.HorizontalMargin) * subHold.endXMin / ChartParams.XaxisMax;
                            float endXMaxWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endY, 0), HorizontalParams.HorizontalMargin) * subHold.endXMax / ChartParams.XaxisMax;

                            // 根据 startT 和 endT 计算 Z 轴位置
                            float zPositionForStartT = CalculateZAxisPosition(subHold.startT);
                            float zPositionForEndT = CalculateZAxisPosition(subHold.endT);

                            // 一次性生成整个 SubHold
                            GameObject subHoldInstance = CreateHoldQuad(startXMinWorld, startXMaxWorld, endXMinWorld, endXMaxWorld,
                                startY, endY, zPositionForStartT, zPositionForEndT, HoldSprite, $"SubHold{subHoldIndex}", holdParent, RenderQueue, shaderName);
                            subHoldInstances.Add(subHoldInstance);
                        }
                        else
                        {
                            // 精细度设为 8，用于分割时间区间（可根据实际需求调整精细度）
                            int segments = FinenessParams.Segment;
                            float timeStep = (subHold.endT - subHold.startT) / segments;
                            // 用于存储细分的 Instance，以便后续合并
                            List<GameObject> segmentInstances = new List<GameObject>();

                            for (int i = 0; i < segments; i++)
                            {
                                float startT = subHold.startT + i * timeStep;
                                float endT = subHold.startT + (i + 1) * timeStep;
                                float startY_Inner = associatedJudgePlaneObject.GetPlaneYAxis(startT);
                                float endY_Inner = associatedJudgePlaneObject.GetPlaneYAxis(endT);

                                float startXMin = CalculatePosition(startT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                                float startXMax = CalculatePosition(startT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);
                                float endXMin = CalculatePosition(endT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                                float endXMax = CalculatePosition(endT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);

                                float startXMinWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startY_Inner, 0), HorizontalParams.HorizontalMargin) * startXMin / ChartParams.XaxisMax;
                                float startXMaxWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startY_Inner, 0), HorizontalParams.HorizontalMargin) * startXMax / ChartParams.XaxisMax;
                                float endXMinWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endY_Inner, 0), HorizontalParams.HorizontalMargin) * endXMin / ChartParams.XaxisMax;
                                float endXMaxWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endY_Inner, 0), HorizontalParams.HorizontalMargin) * endXMax / ChartParams.XaxisMax;

                                // 根据 startT 和 endT 计算 Z 轴位置
                                float zPositionForStartT_Inner = CalculateZAxisPosition(startT);
                                float zPositionForEndT_Inner = CalculateZAxisPosition(endT);

                                GameObject instance = CreateHoldQuad(startXMinWorld_Inner, startXMaxWorld_Inner, endXMinWorld_Inner, endXMaxWorld_Inner,
                                    startY_Inner, endY_Inner, zPositionForStartT_Inner, zPositionForEndT_Inner, HoldSprite, $"SubHold{subHoldIndex}_{i + 1}", holdParent, RenderQueue, shaderName);
                                segmentInstances.Add(instance);
                            }

                            // 合并细分的 Instance 为一个新的 GameObject
                            GameObject combinedSubHold = CombineInstances(segmentInstances);
                            combinedSubHold.name = $"SubHold{subHoldIndex}";
                            subHoldInstances.Add(combinedSubHold);
                        }
                        subHoldIndex++;
                    }

                    // 合并所有的 SubHold 为一个整个的 Hold
                    GameObject combinedHold = CombineInstances(subHoldInstances);
                    combinedHold.name = $"Hold{holdIndex}";
                    combinedHold.transform.SetParent(holdParent.transform);
                    int parentLayer2 = holdParent.layer;
                    combinedHold.layer = parentLayer2;

                    if (hold.subHoldList.Count > 0)
                    {
                        var firstSubHold = hold.subHoldList[0];
                        // 判断是否有多押情况，只根据第一个 SubHold 的 startT 来判断
                        bool isMultiHold = startTimeToInstanceNames.ContainsKey(firstSubHold.startT) && startTimeToInstanceNames[firstSubHold.startT].Count > 1;
                        if (isMultiHold)
                        {
                            // 获取 Hold 的开头和结尾 Z 轴位置
                            float startZ = CalculateZAxisPosition(firstSubHold.startT);
                            float endZ = CalculateZAxisPosition(hold.subHoldList.Last().endT);

                            // 往后推移一点点距离，这里假设推移 0.1，可根据实际情况调整
                            float offsetZ = -OutlineParams.HoldOutlineDefault;

                            // 开头的两个点
                            float startXMin = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, associatedJudgePlaneObject.GetPlaneYAxis(firstSubHold.startT), 0), HorizontalParams.HorizontalMargin) * firstSubHold.startXMin / ChartParams.XaxisMax;
                            float startXMax = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, associatedJudgePlaneObject.GetPlaneYAxis(firstSubHold.startT), 0), HorizontalParams.HorizontalMargin) * firstSubHold.startXMax / ChartParams.XaxisMax;
                            float startY = associatedJudgePlaneObject.GetPlaneYAxis(firstSubHold.startT);

                            // 结尾的两个点
                            float endXMin = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, associatedJudgePlaneObject.GetPlaneYAxis(hold.subHoldList.Last().endT), 0), HorizontalParams.HorizontalMargin) * hold.subHoldList.Last().endXMin / ChartParams.XaxisMax;
                            float endXMax = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, associatedJudgePlaneObject.GetPlaneYAxis(hold.subHoldList.Last().endT), 0), HorizontalParams.HorizontalMargin) * hold.subHoldList.Last().endXMax / ChartParams.XaxisMax;
                            float endY = associatedJudgePlaneObject.GetPlaneYAxis(hold.subHoldList.Last().endT);

                            // 增加渲染队列值，确保白色矩形显示在 Hold 之上
                            int whiteRectRenderQueue = RenderQueue + 1;

                            // 创建开头的白色矩形
                            GameObject startWhiteRect = CreateHoldQuad(startXMin, startXMax, startXMin, startXMax,
                                startY, startY, startZ, startZ + offsetZ, WhiteSprite, $"StartWhiteRect{holdIndex}", holdParent, whiteRectRenderQueue, shaderName);

                            // 创建结尾的白色矩形
                            GameObject endWhiteRect = CreateHoldQuad(endXMin, endXMax, endXMin, endXMax,
                                endY, endY, endZ - offsetZ, endZ, WhiteSprite, $"EndWhiteRect{holdIndex}", holdParent, whiteRectRenderQueue, shaderName);

                            // 将白色矩形与原来的 Hold 合并
                            //List<GameObject> allInstances = new List<GameObject> { combinedHold, startWhiteRect, endWhiteRect };
                            //GameObject finalCombinedHold = CombineInstances(allInstances);
                            //finalCombinedHold.name = $"Hold{holdIndex}";
                            //finalCombinedHold.transform.SetParent(holdParent.transform);
                            //finalCombinedHold.layer = parentLayer2;

                        }
                    }
                }
                holdIndex++;
            }
        }
    }


    public void InstantiateSubStars(Chart chart)
    {
        if (chart != null && chart.stars != null)
        {
            int starIndex = 1;
            foreach (var star in chart.stars)
            {
                int subStarIndex = 1;
                foreach (var subStar in star.subStarList)
                {
                    // 检查子星星是否在规定的 X 轴坐标范围内，如果不在范围，可进行相应处理，比如隐藏或者输出警告等（这里简单示例输出警告）
                    //if (!subStar.IsInAxisRange())
                    //{
                    //    Debug.LogWarning($"SubStar from {subStar.starTrackStartT} to {subStar.starTrackEndT} is out of Axis range!");
                    //}

                    // 计算 subStar 的曲线长度
                    float curveLength = CalculateSubStarCurveLength(subStar);
                    //Debug.Log(curveLength);
                    // 根据曲线长度和每单位长度的 SubArrow 数量，计算需要初始化的 SubArrow 数量
                    //四舍五入
                    int numArrows = (int)MathF.Floor(curveLength * StarArrowParams.subArrowsPerUnitLength + 0.5f);
                    //Debug.Log(numArrows);
                    float rateStep = 1.0f / (float)(numArrows - 1);

                    InitiateStarArrows(subStar, starIndex, subStarIndex, numArrows, rateStep);
                    subStarIndex++;
                }
                starIndex++;
            }
        }
    }

    public void InitiateStarArrows(Star.SubStar subStar, int starIndex, int subStarIndex, float numArrows, float rateStep)
    {
        GameObject StarArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarArrow");
        if (StarArrowPrefab != null)
        {
            float currentRate = 0.0f;

            RectTransform SubStarsParentRect = SubStarsParent.GetComponent<RectTransform>();
            GameObject subStarArrowContainer = new GameObject($"Star{starIndex}SubStar{subStarIndex}Arrows", typeof(RectTransform));
            RectTransform subStarArrowContainerRectTransform = subStarArrowContainer.GetComponent<RectTransform>();
            subStarArrowContainerRectTransform.SetParent(SubStarsParentRect);
            // 继承父物体的图层
            int parentLayer = SubStarsParent.layer;
            subStarArrowContainer.layer = parentLayer;

            // 将 subStarArrowContainer 的位置、旋转和缩放设置为默认值
            subStarArrowContainerRectTransform.anchoredPosition3D = Vector3.zero;
            subStarArrowContainerRectTransform.localRotation = Quaternion.identity;
            subStarArrowContainerRectTransform.localScale = Vector3.one;
            subStarArrowContainerRectTransform.sizeDelta = SubStarsParentRect.sizeDelta;

            //先将subStar的起点和终点转换为画布上的坐标
            Vector2 subStarStart = new Vector2(subStar.startX, subStar.startY);
            Vector2 subStarEnd = new Vector2(subStar.endX, subStar.endY);
            //Debug.Log(subStarStart);
            //Debug.Log(subStarEnd);
            Vector2 subStarStartScreen = ScalePositionToScreenStar(subStarStart, SubStarsParentRect);
            Vector2 subStarEndScreen = ScalePositionToScreenStar(subStarEnd, SubStarsParentRect);
            //Debug.Log(subStarStartScreen);
            //Debug.Log(subStarEndScreen);


            if (subStarIndex == 1)
            {
                // 当 subStarIndex 为 1 时，初始化第一个箭头；否则忽略第一个箭头
                //计算箭头位置
                //if (starIndex == 16)
                //{ Debug.Log(currentRate + "   " + subStarStartScreen + "   " + subStarEndScreen); }
                Vector2 position = CalculateSubArrowPosition(currentRate, subStarStartScreen, subStarEndScreen, subStar.trackFunction);
                //Debug.Log(position);
                float rotation = CalculateSubArrowRotation(currentRate, subStarStartScreen, subStarEndScreen, subStar.trackFunction);
                // 初始化箭头
                GameObject arrow = Instantiate(StarArrowPrefab);
                arrow.name = $"Star{starIndex}SubStar{subStarIndex}Arrow{0 + 1}";
                RectTransform arrowRectTransform = arrow.GetComponent<RectTransform>();
                arrowRectTransform.SetParent(subStarArrowContainerRectTransform);
                // 继承父物体的图层
                int parentLayer2 = subStarArrowContainer.layer;
                arrow.layer = parentLayer2;

                arrowRectTransform.anchoredPosition3D = new Vector3(position.x, position.y, 0);
                arrowRectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
                arrowRectTransform.localScale = Vector3.one * StarArrowParams.defaultScale;
                arrow.SetActive(false);
                //arrowInstances.Add(arrow);
            }

            currentRate += rateStep;
            for (int i = 1; i < numArrows; i++)
            {
                //计算箭头位置
                //if (starIndex == 16)
                //{ Debug.Log(currentRate + "   " + subStarStartScreen + "   " + subStarEndScreen); }
                Vector2 position = CalculateSubArrowPosition(currentRate, subStarStartScreen, subStarEndScreen, subStar.trackFunction);
                float rotation = CalculateSubArrowRotation(currentRate, subStarStartScreen, subStarEndScreen, subStar.trackFunction);
                // 初始化箭头
                GameObject arrow = Instantiate(StarArrowPrefab);
                arrow.name = $"Star{starIndex}SubStar{subStarIndex}Arrow{i + 1}";
                RectTransform arrowRectTransform = arrow.GetComponent<RectTransform>();
                arrowRectTransform.SetParent(subStarArrowContainerRectTransform);
                // 继承父物体的图层
                int parentLayer2 = subStarArrowContainer.layer;
                arrow.layer = parentLayer2;

                arrowRectTransform.anchoredPosition3D = new Vector3(position.x, position.y, 0);
                arrowRectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
                arrowRectTransform.localScale = Vector3.one * StarArrowParams.defaultScale;
                arrow.SetActive(false);
                //arrowInstances.Add(arrow);
                currentRate += rateStep;
            }
        }
    }

    private List<GameObject> CreateJudgePlaneAndColorLinesQuad(float startY, float endY, float startT, float endT, Sprite sprite, string objectName,
        GameObject judgePlaneParent, GameObject leftColorLine, GameObject rightColorLine, int RenderQueue, string colorHex)
    {
        // 根据摄像机角度修正y轴坐标，使y轴坐标在摄像机视角下是线性变换的
        float startYWorld = TransformYCoordinate(startY);
        float endYWorld = TransformYCoordinate(endY);

        // 根据SubJudgePlane的StartT来设置实例的Z轴位置（这里将变量名修改得更清晰些，叫zPositionForStartT）
        float zPositionForStartT = CalculateZAxisPosition(startT);

        // 计算在Z轴方向的长度（之前代码中的height变量，这里改为lengthForZAxis）
        float lengthForZAxis = (endT - startT) * SpeedParams.NoteSpeedDefault;

        // 假设获取到一个目标点的世界坐标
        Vector3 StartPoint = new Vector3(0, startYWorld, 0);
        Vector3 EndPoint = new Vector3(0, endYWorld, 0);

        // 计算StartXWorld和EndXWorld，确保在屏幕左右各留10%的距离
        float startXWorld = CalculateWorldUnitToScreenPixelXAtPosition(StartPoint, HorizontalParams.HorizontalMargin);
        float endXWorld = CalculateWorldUnitToScreenPixelXAtPosition(EndPoint, HorizontalParams.HorizontalMargin);

        float startXWorldPlus = CalculateWorldUnitToScreenPixelXAtPosition(StartPoint, HorizontalParams.PlusHorizontalMargin);
        float endXWorldPlus = CalculateWorldUnitToScreenPixelXAtPosition(EndPoint, HorizontalParams.PlusHorizontalMargin);

        Vector3 point1 = new Vector3(-startXWorld, startYWorld, zPositionForStartT);
        Vector3 point2 = new Vector3(startXWorld, startYWorld, zPositionForStartT);
        Vector3 point3 = new Vector3(endXWorld, endYWorld, zPositionForStartT - lengthForZAxis);
        Vector3 point4 = new Vector3(-endXWorld, endYWorld, zPositionForStartT - lengthForZAxis);

        // 创建JudgePlane实例，使用Sprite的颜色，不再额外赋予灰色
        GameObject judgePlaneInstance = CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, sprite, objectName, judgePlaneParent, RenderQueue, 1f, "MaskMaterial");
        // 为JudgePlane实例创建独立的材质实例
        MeshRenderer judgePlaneRenderer = judgePlaneInstance.GetComponent<MeshRenderer>();
        if (judgePlaneRenderer != null)
        {
            judgePlaneRenderer.material = new Material(judgePlaneRenderer.material);
        }

        // 创建左侧亮条实例
        Vector3 leftPoint1 = new Vector3(-startXWorldPlus, startYWorld, zPositionForStartT);
        Vector3 leftPoint2 = new Vector3(-startXWorld, startYWorld, zPositionForStartT);
        Vector3 leftPoint3 = new Vector3(-endXWorld, endYWorld, zPositionForStartT - lengthForZAxis);
        Vector3 leftPoint4 = new Vector3(-endXWorldPlus, endYWorld, zPositionForStartT - lengthForZAxis);

        //string leftObjectName = $"LeftStrip_{objectName}";
        GameObject leftStripInstance = CreateQuadFromPoints.CreateQuad(leftPoint1, leftPoint2, leftPoint3, leftPoint4, sprite, objectName, leftColorLine, RenderQueue, 1f, "MaskMaterialColorLine");
        Color leftColor;
        if (!string.IsNullOrEmpty(colorHex))
        {
            leftColor = HexToColor(colorHex);
        }
        else
        {
            //默认颜色为灰色
            leftColor = new Color(51f / 255f, 51f / 255f, 51f / 255f);
        }
        SetSpriteColor(leftStripInstance, leftColor);

        // 为左侧亮条实例创建独立的材质实例
        MeshRenderer leftStripRenderer = leftStripInstance.GetComponent<MeshRenderer>();
        if (leftStripRenderer != null)
        {
            leftStripRenderer.material = new Material(leftStripRenderer.material);
        }

        // 创建右侧亮条实例
        Vector3 rightPoint1 = new Vector3(startXWorldPlus, startYWorld, zPositionForStartT);
        Vector3 rightPoint2 = new Vector3(startXWorld, startYWorld, zPositionForStartT);
        Vector3 rightPoint3 = new Vector3(endXWorld, endYWorld, zPositionForStartT - lengthForZAxis);
        Vector3 rightPoint4 = new Vector3(endXWorldPlus, endYWorld, zPositionForStartT - lengthForZAxis);

        //string rightObjectName = $"RightStrip_{objectName}";
        GameObject rightStripInstance = CreateQuadFromPoints.CreateQuad(rightPoint1, rightPoint2, rightPoint3, rightPoint4, sprite, objectName, rightColorLine, RenderQueue, 1f, "MaskMaterialColorLine");
        Color rightColor;
        if (!string.IsNullOrEmpty(colorHex))
        {
            rightColor = HexToColor(colorHex);
        }
        else
        {
            //默认颜色为灰色
            rightColor = new Color(51f / 255f, 51f / 255f, 51f / 255f);
        }
        SetSpriteColor(rightStripInstance, rightColor);

        // 为右侧亮条实例创建独立的材质实例
        MeshRenderer rightStripRenderer = rightStripInstance.GetComponent<MeshRenderer>();
        if (rightStripRenderer != null)
        {
            rightStripRenderer.material = new Material(rightStripRenderer.material);
        }

        List<GameObject> instances = new List<GameObject>
        {
            judgePlaneInstance,
            leftStripInstance,
            rightStripInstance
        };
        return instances;
    }


    private void SetSpriteColor(GameObject obj, Color color)
    {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // 实例化材质
            Material instanceMaterial = new Material(renderer.material);
            instanceMaterial.color = color;
            renderer.material = instanceMaterial;
            //Debug.Log(renderer.material.color);
        }
    }

    private GameObject CreateHoldQuad(float startXMinWorld, float startXMaxWorld, float endXMinWorld, float endXMaxWorld,
    float startY, float endY, float zPositionForStartT, float zPositionForEndT, Sprite sprite, string objectName, GameObject parentObject, int RenderQueue, string shaderName)
    {

        //注意四边形顶点顺序
        Vector3 point1 = new Vector3(-startXMinWorld, startY, zPositionForStartT);
        Vector3 point2 = new Vector3(-endXMinWorld, endY, zPositionForEndT);
        Vector3 point3 = new Vector3(-endXMaxWorld, endY, zPositionForEndT);
        Vector3 point4 = new Vector3(-startXMaxWorld, startY, zPositionForStartT);

        // 假设CreateQuadFromPoints.CreateQuad方法直接返回游戏物体实例
        return CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, sprite, objectName, parentObject, RenderQueue, 0.7f, shaderName);
    }

}
