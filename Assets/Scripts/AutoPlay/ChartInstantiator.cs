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
using TMPro;
using DocumentFormat.OpenXml.InkML;
//using DocumentFormat.OpenXml.Wordprocessing;
//using static KeyInfo;
//using DocumentFormat.OpenXml.Math;
//using DocumentFormat.OpenXml.Presentation;
//using Unity.VisualScripting;



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
    private GameObject HoldOutlinesParent;
    private GameObject StarsParent;
    private GameObject SubStarsParent;
    public GameObject JudgeTexturesParent;
    private GameObject MultiHitLinesParent;
    [SerializeField] private TMP_Text fpsText; // 需在Inspector中关联TextMeshPro组件

    private Sprite JudgePlaneSprite;
    private Sprite HoldSprite;
    private Sprite WhiteSprite;

    private GlobalRenderOrderManager renderOrderManager;
    private GameObject videoPlayerContainer;

    public Dictionary<float, List<string>> startTimeToInstanceNames = new Dictionary<float, List<string>>(); // 存储startT到对应实例名列表的映射
    public Dictionary<string, List<float>> holdTimes = new Dictionary<string, List<float>>(); // 存储Hold实例名到其开始/结束时间的映射
    public Dictionary<string, KeyInfo> keyReachedJudgment = new Dictionary<string, KeyInfo>(); // 存储实例名到键信息的映射
    public List<float> JudgePlanesStartT = new List<float>(); // 判定面的开始时间（用于JudgeLine出现时间计算）
    public List<float> JudgePlanesEndT = new List<float>(); // 判定面的结束时间（用于JudgeLine结束时间计算）
    public Dictionary<(int, int), SubStarInfo> subStarInfoDict = new Dictionary<(int, int), SubStarInfo>();
    // 新增一个字典，用于存储每个星星的划动开始和结束时间
    public Dictionary<string, (float startT, float endT)> starTrackTimes = new Dictionary<string, (float startT, float endT)>();
    public GradientColorListUnity GradientColorList;
    private List<KeyValuePair<float, string>> MultiHitDict = new List<KeyValuePair<float, string>>();
    private Dictionary<float, List<string>> MultiHitPairs = new Dictionary<float, List<string>>();
    private Dictionary<float, List<Vector3>> MultiHitPairsCoord = new Dictionary<float, List<Vector3>>();
    private float FirstNoteStartTime = 0f;
    public float ChartStartTime = 0f;

    
    // 新增的公共方法，用于接收各个参数并赋值给对应的私有变量
    public void SetParameters(GameObject judgePlanesParent, GameObject judgeLinesParent, GameObject colorLinesParent, GameObject tapsParent, GameObject slidesParent, GameObject flicksParent, GameObject flickarrowsParent,
        GameObject holdsParent, GameObject holdOutlinesParent, GameObject starsParent, GameObject subStarsParent, GameObject judgeTexturesParent, GameObject multiHitLinesParent,
        Sprite judgePlaneSprite, Sprite holdSprite, GlobalRenderOrderManager globalRenderOrderManager, GameObject animatorContainer, TMP_Text FpsText)
    {
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
        SubStarsParent = subStarsParent;
        JudgeTexturesParent = judgeTexturesParent;
        MultiHitLinesParent = multiHitLinesParent;
        JudgePlaneSprite = judgePlaneSprite;
        HoldSprite = holdSprite;
        renderOrderManager = globalRenderOrderManager;
        videoPlayerContainer = animatorContainer;
        fpsText = FpsText;

        WhiteSprite = Resources.Load<Sprite>("Sprites/WhiteSprite");
        if (WhiteSprite == null)
        {
            Debug.LogError("Failed to load sprite: Sprites/WhiteSprite");
        }

    }

    private void PrepareStartTimeMapping(Chart chart)
    {
        //根据摄像头范围，更新FPS文本的位置
        //float screenHeight = Screen.height;
        //Vector2 fpsTextPos = fpsText.gameObject.GetComponent<RectTransform>().position;
        //Debug.Log(fpsTextPos);
        //fpsTextPos.y += (screenHeight - AspectRatioManager.croppedScreenHeight)/2;
        //fpsText.gameObject.GetComponent<RectTransform>().position = fpsTextPos;
        //Debug.Log(fpsText.gameObject.GetComponent<RectTransform>().position);


        //Debug.Log("start..");
        List<KeyValuePair<float, string>> allPairs = new List<KeyValuePair<float, string>>();
        //List<KeyValuePair<float, string>> MultiHitPairs = new List<KeyValuePair<float, string>>();

        if (chart.judgePlanes != null)
        {
            for (int i = 0; i < chart.judgePlanes.Count; i++)
            {
                var judgePlane = chart.judgePlanes[i];
                string instanceName = $"JudgePlane{i + 1}";
                float startT = judgePlane.subJudgePlaneList[0].startT;
                float endT = judgePlane.subJudgePlaneList[judgePlane.subJudgePlaneList.Count - 1].endT;
                JudgePlanesStartT.Add(startT);
                JudgePlanesEndT.Add(endT);
                allPairs.Add(new KeyValuePair<float, string>(startT, instanceName));
            }
        }

        if (chart.taps != null)
        {
            for (int i = 0; i < chart.taps.Count; i++)
            {
                var tap = chart.taps[i];
                string instanceName = $"Tap{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(tap.startT, instanceName));
                MultiHitDict.Add(new KeyValuePair<float, string>(tap.startT, instanceName));
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
                // Slide不计入多押
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
                MultiHitDict.Add(new KeyValuePair<float, string>(flick.startT, instanceName));
                foreach (var hold in chart.holds)
                {
                    //如果Flick是Hold的伴生Flick（在Hold的开头或结尾处）
                    if (
                        (flick.startT == hold.GetFirstSubHoldStartTime() & flick.startX == hold.GetFirstSubHoldStartX() & flick.associatedPlaneId == hold.associatedPlaneId) |
                        (flick.startT == hold.GetLastSubHoldEndTime() & flick.startX == hold.GetLastSubHoldEndX() & flick.associatedPlaneId == hold.associatedPlaneId)
                        )
                    {
                        //则从多押列表里删除该Flick
                        MultiHitDict.RemoveAll(pair =>
                            pair.Key == flick.startT &&
                            pair.Value == $"Flick{i + 1}"
                        );
                        break;
                    }
                }
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
                MultiHitDict.Add(new KeyValuePair<float, string>(startT, instanceName));
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
                string instanceName = $"StarHead{i + 1}";
                allPairs.Add(new KeyValuePair<float, string>(startT, instanceName));
                MultiHitDict.Add(new KeyValuePair<float, string>(startT, instanceName));
                keyReachedJudgment[instanceName] = new KeyInfo(startT);
                // 记录每个星星的划动开始和结束时间
                float starstartT = star.subStarList[0].starTrackStartT;
                float starendT = star.subStarList[star.subStarList.Count - 1].starTrackEndT;
                starTrackTimes[instanceName] = (starstartT, starendT);
            }
        }

        //把多押原始字典映射为时间到实例名的多押字典
        MultiHitPairs = MultiHitDict.GroupBy(pair => pair.Key)
                                        .Where(group => group.Count() > 1)
                                        .ToDictionary(
                                            group => group.Key,
                                            group => group.Select(pair => pair.Value).ToList()
                                        );

        // 按照startT进行排序
        allPairs = allPairs.OrderBy(pair => pair.Key).ToList();

        // 新增：从allPairs中筛选排除所有JudgePlane实例
        List<KeyValuePair<float, string>> nonJudgePlanePairs = allPairs
            .Where(pair => !pair.Value.StartsWith("JudgePlane")) // 按实例名前缀筛选
            .ToList();
        //第一个Note的判定时间（用于计算谱面开始偏移时间）
        FirstNoteStartTime = nonJudgePlanePairs[0].Key;
        //谱面开始偏移时间（第一个Note时值为0时，最多可偏移2s；第一个Note时值≥2s时不偏移）
        ChartStartTime = Math.Max(ChartParams.ChartStartTimeOffset - FirstNoteStartTime, 0f);
        //Debug.Log($"首个Note时间：{FirstNoteStartTime}");
        Debug.Log($"谱面偏移时间：{ChartStartTime}");

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

        // 调用 ConvertToUnityList 方法完成颜色转换
        GradientColorList = GradientColorListUnity.ConvertToUnityList(chart.gradientColorList);

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
        InstantiateMultiHitLines();

        subStarInfoDict = Star.InitializeSubStarInfo(chart, SubStarsParent);
    }


    public void InstantiateJudgePlanes(Chart chart)
    {
        if (chart != null && chart.judgePlanes != null)
        {
            foreach (var judgePlane in chart.judgePlanes)
            {
                //Debug.Log(judgePlane.id);
                int RenderQueue = 3000;
                int judgePlaneIndex = judgePlane.id;

                int subJudgePlaneIndex = 1;

                Color planecolor = Color.black;

                List<GameObject> judgePlaneInstances = new List<GameObject>();
                List<GameObject> leftStripInstances = new List<GameObject>();
                List<GameObject> rightStripInstances = new List<GameObject>();

                foreach (var subJudgePlane in judgePlane.subJudgePlaneList)
                {

                    if (subJudgePlaneIndex == 1)
                    {
                        planecolor = GradientColorList.GetColorAtTimeAndY(subJudgePlane.startT, subJudgePlane.startY);
                    };

                    // 根据SubJudgePlane的函数类型来决定如何处理
                    switch (subJudgePlane.yAxisFunction)
                    {
                        case TransFunctionType.Linear:
                            List<GameObject> objectsToCombine = CreateJudgePlaneAndColorLinesQuad(subJudgePlane.startY, subJudgePlane.endY, subJudgePlane.startT, subJudgePlane.endT,
                                JudgePlaneSprite, $"Sub{subJudgePlaneIndex}", JudgePlanesParent, ColorLinesParent, RenderQueue, planecolor, chart.speedList);

                            judgePlaneInstances.Add(objectsToCombine[0]);
                            leftStripInstances.Add(objectsToCombine[1]);
                            rightStripInstances.Add(objectsToCombine[2]);

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
                                    JudgePlaneSprite, $"Sub{subJudgePlaneIndex}_{i + 1}", JudgePlanesParent, ColorLinesParent, RenderQueue, planecolor, chart.speedList);

                                judgePlaneInstances.Add(ObjectsToCombine[0]);
                                leftStripInstances.Add(ObjectsToCombine[1]);
                                rightStripInstances.Add(ObjectsToCombine[2]);
                            }

                            break;
                    }

                    subJudgePlaneIndex++;
                }

                // 合并 SubJudgePlane
                GameObject combinedJudgePlane = CombineInstances(judgePlaneInstances, JudgePlanesParent.transform);
                combinedJudgePlane.name = $"JudgePlane{judgePlaneIndex}";
                ProcessCombinedInstance(combinedJudgePlane, JudgePlanesParent, JudgePlanesParent.layer);

                // 合并左侧亮条
                GameObject combinedLeftStrip = CombineInstances(leftStripInstances, ColorLinesParent.transform);
                combinedLeftStrip.name = $"LeftColorLine{judgePlaneIndex}";
                ProcessCombinedInstance(combinedLeftStrip, ColorLinesParent, ColorLinesParent.layer);

                // 合并右侧亮条
                GameObject combinedRightStrip = CombineInstances(rightStripInstances, ColorLinesParent.transform);
                combinedRightStrip.name = $"RightColorLine{judgePlaneIndex}";
                ProcessCombinedInstance(combinedRightStrip, ColorLinesParent, ColorLinesParent.layer);

            }
        }
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
                    float YAxisUniform = judgePlane.GetPlaneYAxis(subJudgePlane.startT);
                    //Debug.Log(YAxisUniform);
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
            // 假设Tap3D预制体的加载路径，你需要根据实际情况修改
            GameObject tapPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Tap3D");
            //GameObject tapOutlinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Tap3DOutline");

            if (tapPrefab != null)
            {
                float tapXAxisLength = 0; // 先在外层定义变量，初始化为0，后续根据实际情况赋值
                MeshFilter meshFilter = tapPrefab.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    // 获取Tap在X轴的长度（用于缩放），使用sharedMesh替代mesh
                    tapXAxisLength = meshFilter.sharedMesh.bounds.size.x;
                    //Debug.Log(tapXAxisLength);
                }
                else
                {
                    Debug.LogError($"Tap3D预制体实例 {tapPrefab.name} 缺少MeshFilter组件，无法获取X轴长度进行缩放设置！");
                }

                int tapIndex = 1;
                foreach (var tap in chart.taps)
                {

                    // 实例化Tap预制体
                    GameObject tapInstance = Instantiate(tapPrefab);
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
                        tap.startY = yAxisPosition;
                        // 注意Outline初始化为最初的颜色
                        Color planecolor = GradientColorList.GetColorAtTimeAndY(0f, yAxisPosition);
                        float yPos = TransformYCoordinate(yAxisPosition);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（封装成方法方便复用，以下是示例方法定义，参数需根据实际情况传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yPos, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);
                        //Debug.Log(worldUnitToScreenPixelX);

                        // 计算Tap的X轴坐标
                        float startXWorld = worldUnitToScreenPixelX * tap.startX / ChartParams.XaxisMax;

                        // 根据noteSize折算到X轴世界坐标长度，计算每单位noteSize对应的世界坐标长度
                        float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Tap本身在X轴的世界坐标长度和noteSize计算X轴的缩放值
                        float xAxisScale = noteSizeWorldLengthPerUnit / tapXAxisLength * tap.noteSize;

                        // 设置Tap实例的缩放比例（只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        tapInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.NoteThickness, 1);

                        // 设置Tap实例的位置（X、Y、Z轴坐标），同时考虑Z轴偏移量
                        float zPositionForStartT = CalculateZAxisPosition(tap.startT, ChartStartTime, chart.speedList);

                        tapInstance.transform.position = new Vector3(-startXWorld, yPos, zPositionForStartT + ChartParams.NoteZAxisOffset);
                        //Debug.Log(tapInstance.transform.position);

                        // 获取MyOutline组件并设置属性
                        MyOutline outlineComponent = tapInstance.GetComponent<MyOutline>();
                        if (outlineComponent != null)
                        {
                            outlineComponent.OutlineColor = planecolor;
                            outlineComponent.OutlineWidth = ChartParams.OutlineWidth;
                        }
                        else
                        {
                            Debug.Log($"Tap实例 {tapInstance.name} 缺少MyOutline组件！");
                        }
                    }

                    // 检查点键是否在规定的X轴坐标范围内，如果不在范围，可进行相应处理，比如隐藏或者输出警告等（这里简单示例输出警告）
                    if (!tap.IsInXAxisRange())
                    {
                        Debug.LogWarning($"Tap with startT: {tap.startT} and startX: {tap.startX} is out of X - axis range!");
                    }

                    // 检查时间点和实例名是否都在 MultiHitPairs 中
                    if (MultiHitPairs.ContainsKey(tap.startT) &&
                        MultiHitPairs.TryGetValue(tap.startT, out List<string> namesAtTime) &&
                        namesAtTime.Contains(tapInstance.name))
                    {
                        if (!MultiHitPairsCoord.ContainsKey(tap.startT))
                        {
                            MultiHitPairsCoord[tap.startT] = new List<Vector3>();
                        }
                        //添加Note坐标至MultiHitPairsCoord
                        MultiHitPairsCoord[tap.startT].Add(tapInstance.transform.position);
                    }

                    //未到达渲染时间的设置为非激活
                    if (tap.startT > ChartParams.NoteRenderTimeOffset)
                    {
                        tapInstance.SetActive(false);
                    }

                    tapIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载Tap3D或Tap3DOutline预制体！");
            }
        }
    }

    public void InstantiateSlides(Chart chart)
    {
        if (chart != null && chart.slides != null)
        {
            // 加载常规Slide预制体和多押时的Slide预制体
            GameObject slidePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Slide3D");
            //GameObject slideOutlinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Slide3DOutline");

            if (slidePrefab != null)
            {
                float slideXAxisLength = 0; // 用于存储Slide在X轴方向的长度（用于后续缩放等操作）
                MeshFilter meshFilter = slidePrefab.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    // 获取Slide在X轴的长度（用于缩放等处理）
                    slideXAxisLength = meshFilter.sharedMesh.bounds.size.x;
                }
                else
                {
                    Debug.LogError($"Slide3D预制体实例 {slidePrefab.name} 缺少MeshFilter组件，无法获取X轴长度进行相关设置！");
                }

                int slideIndex = 1;
                foreach (var slide in chart.slides)
                {

                    // 实例化Slide预制体
                    GameObject slideInstance = Instantiate(slidePrefab);
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
                        slide.startY = yAxisPosition;
                        // 注意Outline初始化为最初的颜色
                        Color planecolor = GradientColorList.GetColorAtTimeAndY(0f, yAxisPosition);
                        float yPos = TransformYCoordinate(yAxisPosition);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（这里可复用已有的相关方法，假设已经有合适的方法定义，参数需根据实际传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yPos, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);

                        // 计算Slide的X轴坐标（根据Slide相关参数和计算逻辑来确定，示例如下，需按实际调整）
                        float startXWorld = worldUnitToScreenPixelX * slide.startX / ChartParams.XaxisMax;

                        // 根据slideSize等参数折算到X轴世界坐标长度，计算每单位slideSize对应的世界坐标长度（示例逻辑，按实际修改）
                        float slideSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Slide本身在X轴的世界坐标长度和slideSize等参数计算X轴的缩放值（示例，按需调整）
                        float xAxisScale = slideSizeWorldLengthPerUnit / slideXAxisLength * slide.noteSize;

                        // 设置Slide实例的缩放比例（这里只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        slideInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.NoteThickness, 1);

                        // 设置Slide实例的位置（X、Y、Z轴坐标，示例逻辑，按实际情况调整）
                        float zPositionForStartT = CalculateZAxisPosition(slide.startT, ChartStartTime, chart.speedList);
                        slideInstance.transform.position = new Vector3(-startXWorld, yPos, zPositionForStartT + ChartParams.NoteZAxisOffset);

                        // 获取MyOutline组件并设置属性
                        MyOutline outlineComponent = slideInstance.GetComponent<MyOutline>();
                        if (outlineComponent != null)
                        {
                            outlineComponent.OutlineColor = planecolor;
                            outlineComponent.OutlineWidth = ChartParams.OutlineWidth;
                        }
                        else
                        {
                            Debug.Log($"Slide实例 {slideInstance.name} 缺少MyOutline组件！");
                        }
                    }

                    // 检查Slide相关的范围等条件是否满足（这里简单示例输出警告，按实际需求完善检查逻辑）
                    if (!slide.IsInXAxisRange())
                    {
                        Debug.LogWarning($"Slide with startT: {slide.startT} and startX: {slide.startX} is out of valid range!");
                    }

                    //未到达渲染时间的设置为非激活
                    if (slide.startT > ChartParams.NoteRenderTimeOffset)
                    {
                        slideInstance.SetActive(false);
                    }

                    slideIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载Slide3D或Slide3DOutline预制体！");
            }
        }
    }

    public void InstantiateFlicks(Chart chart)
    {
        if (chart != null && chart.flicks != null)
        {
            // 加载常规Flick预制体、多押时的Flick预制体和FlickArrow预制体
            GameObject flickPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Flick3D");
            //GameObject flickOutlinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Flick3DOutline");
            GameObject flickArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/FlickArrow");

            if (flickPrefab != null && flickArrowPrefab != null)
            {
                float flickXAxisLength = 0; // 用于存储Flick在X轴方向的长度（用于后续缩放等操作）
                MeshFilter meshFilter = flickPrefab.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    // 获取Flick在X轴的长度（用于缩放等处理）
                    flickXAxisLength = meshFilter.sharedMesh.bounds.size.x;
                }
                else
                {
                    Debug.LogError($"Flick3D预制体实例 {flickPrefab.name} 缺少MeshFilter组件，无法获取X轴长度进行相关设置！");
                }

                int flickIndex = 1;
                foreach (var flick in chart.flicks)
                {

                    // 实例化Flick预制体
                    GameObject flickInstance = Instantiate(flickPrefab);
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
                        flick.startY = yAxisPosition;
                        // 注意Outline初始化为最初的颜色
                        Color planecolor = GradientColorList.GetColorAtTimeAndY(0f, yAxisPosition);
                        float yPos = TransformYCoordinate(yAxisPosition);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（这里可复用已有的相关方法，假设已经有合适的方法定义，参数需根据实际传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yPos, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);

                        // 计算Flick的X轴坐标（根据Flick相关参数和计算逻辑来确定，示例如下，需按实际调整）
                        float startXWorld = worldUnitToScreenPixelX * flick.startX / ChartParams.XaxisMax;

                        // 根据flickSize等参数折算到X轴世界坐标长度，计算每单位flickSize对应的世界坐标长度（示例逻辑，按实际修改）
                        float flickSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Flick本身在X轴的世界坐标长度和flickSize等参数计算X轴的缩放值（示例，按需调整）
                        float xAxisScale = flickSizeWorldLengthPerUnit / flickXAxisLength * flick.noteSize;

                        // 设置Flick实例的缩放比例（这里只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        flickInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.NoteThickness, 1);

                        // 设置Flick实例的位置（X、Y、Z轴坐标，示例逻辑，按实际情况调整）
                        float zPositionForStartT = CalculateZAxisPosition(flick.startT, ChartStartTime, chart.speedList);
                        flickInstance.transform.position = new Vector3(-startXWorld, yPos, zPositionForStartT + ChartParams.NoteZAxisOffset);

                        Vector3 leftMiddleWorldPos = GetLeftMiddleWorldPosition(flickInstance);

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

                        // 获取MyOutline组件并设置属性
                        MyOutline outlineComponent = flickInstance.GetComponent<MyOutline>();
                        if (outlineComponent != null)
                        {
                            outlineComponent.OutlineColor = planecolor;
                            outlineComponent.OutlineWidth = ChartParams.OutlineWidth;
                        }
                        else
                        {
                            Debug.Log($"Flick实例 {flickInstance.name} 缺少MyOutline组件！");
                        }

                        //未到达渲染时间的设置为非激活
                        if (flick.startT > ChartParams.NoteRenderTimeOffset)
                        {
                            flickInstance.SetActive(false);
                            flickArrowInstance.SetActive(false);
                        }
                    }

                    // 检查Flick相关的范围等条件是否满足（这里简单示例输出警告，按实际需求完善检查逻辑）
                    if (!flick.IsInXAxisRange())
                    {
                        Debug.LogWarning($"Flick with startT: {flick.startT} and startX: {flick.startX} is out of valid range!");
                    }

                    // 检查时间点和实例名是否都在 MultiHitPairs 中
                    if (MultiHitPairs.ContainsKey(flick.startT) &&
                        MultiHitPairs.TryGetValue(flick.startT, out List<string> namesAtTime) &&
                        namesAtTime.Contains(flickInstance.name))
                    {
                        if (!MultiHitPairsCoord.ContainsKey(flick.startT))
                        {
                            MultiHitPairsCoord[flick.startT] = new List<Vector3>();
                        }
                        //添加Note坐标至MultiHitPairsCoord
                        MultiHitPairsCoord[flick.startT].Add(flickInstance.transform.position);
                    }

                    flickIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载Flick3D、Flick3DOutline或FlickArrow预制体！");
            }
        }
    }

    public void InstantiateStarHeads(Chart chart)
    {
        if (chart != null && chart.stars != null)
        {
            // 加载常规StarHead预制体和多押时的StarHead预制体
            GameObject starheadPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarHead3D");
            GameObject StarStartFXPrefab = Resources.Load<GameObject>("Prefabs/External/HitReverse");
            //GameObject starheadOutlinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarHead3DOutline");

            if (starheadPrefab != null & StarStartFXPrefab != null)
            {
                float starheadXAxisLength = 0; // 先在外层定义变量，初始化为0，后续根据实际情况赋值
                MeshFilter meshFilter = starheadPrefab.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    // 获取StarHead在X轴的长度（用于缩放）
                    starheadXAxisLength = meshFilter.sharedMesh.bounds.size.x;
                }
                else
                {
                    Debug.LogError($"StarHead3D预制体实例 {starheadPrefab.name} 缺少MeshFilter组件，无法获取X轴长度进行缩放设置！");
                }

                int starIndex = 1;
                foreach (var star in chart.stars)
                {

                    // 实例化StarHead预制体
                    GameObject starheadInstance = Instantiate(starheadPrefab);
                    starheadInstance.name = $"StarHead{starIndex}"; // 命名
                    // 将StarHead设置为ChartGameObjects的子物体
                    starheadInstance.transform.SetParent(StarsParent.transform);
                    // 继承父物体的图层
                    int parentLayer = StarsParent.layer;
                    starheadInstance.layer = parentLayer;

                    // 实例化Star启动特效
                    GameObject starStartFXInstance = Instantiate(StarStartFXPrefab);
                    starStartFXInstance.name = $"StarStartFX{starIndex}"; // 命名
                    // 将StarHead设置为ChartGameObjects的子物体
                    starStartFXInstance.transform.SetParent(SubStarsParent.transform);
                    // 继承父物体的图层
                    int Layer = SubStarsParent.layer;
                    starStartFXInstance.layer = Layer;

                    // 获取开始的X轴和Y轴坐标
                    JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(star.associatedPlaneId);
                    Vector2 firstStarCoodinate = star.GetFirstSubStarCoordinates();
                    float xAxisPosition = firstStarCoodinate.x;
                    float yAxisPosition = firstStarCoodinate.y;
                    star.startY = yAxisPosition;
                    // 注意Outline初始化为最初的颜色
                    Color planecolor = GradientColorList.GetColorAtTimeAndY(0f, yAxisPosition);
                    float yPos = TransformYCoordinate(yAxisPosition);

                    // 获取判定区下边缘和上边缘在屏幕空间中的像素坐标
                    //float bottomPixel = AspectRatioManager.croppedScreenHeight * HorizontalParams.VerticalMarginBottom;
                    //float topPixel = AspectRatioManager.croppedScreenHeight * HorizontalParams.VerticalMarginCeiling;

                    // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围
                    Vector3 referencePoint = new Vector3(0, yPos, 0);
                    float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);

                    // 计算X轴坐标
                    float startXWorld = worldUnitToScreenPixelX * xAxisPosition / ChartParams.XaxisMax;
                    // 根据noteSize折算到X轴世界坐标长度，计算每单位noteSize对应的世界坐标长度
                    float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;
                    // 根据StarHead本身在X轴的世界坐标长度和noteSize计算X轴的缩放值
                    float xAxisScale = noteSizeWorldLengthPerUnit / starheadXAxisLength * ChartParams.StarHeadXAxis;

                    // 设置StarHead实例的缩放比例
                    starheadInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.NoteThickness, 1);

                    // 设置StarHead实例的位置（X、Y、Z轴坐标）
                    float zPositionForStartT = CalculateZAxisPosition(star.starHeadT, ChartStartTime, chart.speedList);
                    starheadInstance.transform.position = new Vector3(-startXWorld, yPos, zPositionForStartT + ChartParams.NoteZAxisOffset);

                    //先把启动特效设置为未激活
                    starStartFXInstance.transform.position = new Vector3(-startXWorld, yPos, 0f);
                    starStartFXInstance.SetActive(false);

                    // 获取MyOutline组件并设置属性
                    MyOutline outlineComponent = starheadInstance.GetComponent<MyOutline>();
                    if (outlineComponent != null)
                    {
                        outlineComponent.OutlineColor = planecolor;
                        outlineComponent.OutlineWidth = ChartParams.OutlineWidth;
                    }
                    else
                    {
                        Debug.Log($"StarHead实例 {starheadInstance.name} 缺少MyOutline组件！");
                    }

                    // 检查点键是否在规定的X轴坐标范围内，如果不在范围，可进行相应处理，比如隐藏或者输出警告等（这里简单示例输出警告）
                    //if (!star.IsInAxisRange())
                    //{
                    //    Debug.LogWarning($"Star with starHeadT: {star.starHeadT} is out of Axis range!");
                    //}

                    // 检查时间点和实例名是否都在 MultiHitPairs 中
                    if (MultiHitPairs.ContainsKey(star.starHeadT) &&
                        MultiHitPairs.TryGetValue(star.starHeadT, out List<string> namesAtTime) &&
                        namesAtTime.Contains(starheadInstance.name))
                    {
                        if (!MultiHitPairsCoord.ContainsKey(star.starHeadT))
                        {
                            MultiHitPairsCoord[star.starHeadT] = new List<Vector3>();
                        }
                        //添加Note坐标至MultiHitPairsCoord
                        MultiHitPairsCoord[star.starHeadT].Add(starheadInstance.transform.position);
                    }

                    //未到达渲染时间的设置为非激活
                    if (star.starHeadT > ChartParams.NoteRenderTimeOffset)
                    {
                        starheadInstance.SetActive(false);
                    }

                    starIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载StarHead3D或HitReverse预制体！");
            }
        }
    }

    public void InstantiateHolds(Chart chart)
    {
        if (chart != null && chart.holds != null)
        {
            int holdIndex = 1;
            int RenderQueue = 3000;
            foreach (var hold in chart.holds)
            {

                float HoldstartT = hold.GetFirstSubHoldStartTime();
                // 创建一个空物体作为 hold 实例的父物体，用于统一管理和规范命名
                JudgePlane associatedJudgePlaneObject = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
                Color planecolor = Color.black;

                if (associatedJudgePlaneObject != null)
                {
                    string shaderName = "MaskMaterial"; // 默认使用 MaskMaterial 作为 shader

                    List<GameObject> subHoldInstances = new List<GameObject>();
                    List<GameObject> OutlineInstances = new List<GameObject>();
                    int subHoldIndex = 1;
                    foreach (var subHold in hold.subHoldList)
                    {

                        float startY = associatedJudgePlaneObject.GetPlaneYAxis(subHold.startT);
                        float endY = associatedJudgePlaneObject.GetPlaneYAxis(subHold.endT);

                        subHold.startY = startY;
                        subHold.endY = endY;
                        subHold.yAxisFunction = associatedJudgePlaneObject.GetPlaneYAxisFunction(subHold.startT);

                        // 根据摄像机角度修正y轴坐标，使y轴坐标在摄像机视角下是线性变换的
                        float startYWorld = TransformYCoordinate(startY);
                        float endYWorld = TransformYCoordinate(endY);

                        if (subHoldIndex == 1)
                        {
                            planecolor = GradientColorList.GetColorAtTimeAndY(subHold.startT, startY);
                        };

                        // 检查 SubHold 所在的 SubJudgePlane 是否为 Linear
                        bool isSubJudgePlaneLinear = associatedJudgePlaneObject.IsSubJudgePlaneLinear(subHold.startT, subHold.endT);

                        if (subHold.Jagnum == 0)
                        {
                            // Jagnum = 0 时，采用之前的代码
                            if (subHold.XLeftFunction == TransFunctionType.Linear && subHold.XRightFunction == TransFunctionType.Linear && isSubJudgePlaneLinear)
                            {
                                float startXMinWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld, 0), HorizontalParams.HorizontalMargin) * subHold.startXMin / ChartParams.XaxisMax;
                                float startXMaxWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld, 0), HorizontalParams.HorizontalMargin) * subHold.startXMax / ChartParams.XaxisMax;
                                float endXMinWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld, 0), HorizontalParams.HorizontalMargin) * subHold.endXMin / ChartParams.XaxisMax;
                                float endXMaxWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld, 0), HorizontalParams.HorizontalMargin) * subHold.endXMax / ChartParams.XaxisMax;

                                // 根据 startT 和 endT 计算 Z 轴位置
                                float zPositionForStartT = CalculateZAxisPosition(subHold.startT, ChartStartTime, chart.speedList);
                                float zPositionForEndT = CalculateZAxisPosition(subHold.endT, ChartStartTime, chart.speedList);

                                // 一次性生成整个 SubHold 及两侧色条
                                List<GameObject> subHoldAndColorLines = CreateHoldQuadWithColorLines(startXMinWorld, startXMaxWorld, endXMinWorld, endXMaxWorld,
                                    startYWorld, endYWorld, zPositionForStartT, zPositionForEndT, HoldSprite, WhiteSprite, $"SubHold{subHoldIndex}", HoldsParent, HoldOutlinesParent, RenderQueue, shaderName, planecolor);
                                //subHoldInstances.AddRange(subHoldAndColorLines);
                                subHoldInstances.Add(subHoldAndColorLines[0]);
                                OutlineInstances.Add(subHoldAndColorLines[1]);
                                OutlineInstances.Add(subHoldAndColorLines[2]);
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

                                    // 根据摄像机角度修正y轴坐标，使y轴坐标在摄像机视角下是线性变换的
                                    float startYWorld_Inner = TransformYCoordinate(startY_Inner);
                                    float endYWorld_Inner = TransformYCoordinate(endY_Inner);

                                    float startXMin = CalculatePosition(startT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                                    float startXMax = CalculatePosition(startT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);
                                    float endXMin = CalculatePosition(endT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                                    float endXMax = CalculatePosition(endT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);

                                    float startXMinWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * startXMin / ChartParams.XaxisMax;
                                    float startXMaxWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * startXMax / ChartParams.XaxisMax;
                                    float endXMinWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * endXMin / ChartParams.XaxisMax;
                                    float endXMaxWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * endXMax / ChartParams.XaxisMax;

                                    // 根据 startT 和 endT 计算 Z 轴位置
                                    float zPositionForStartT_Inner = CalculateZAxisPosition(startT, ChartStartTime, chart.speedList);
                                    float zPositionForEndT_Inner = CalculateZAxisPosition(endT, ChartStartTime, chart.speedList);

                                    // 生成细分的 SubHold 及两侧色条
                                    List<GameObject> segmentAndColorLines = CreateHoldQuadWithColorLines(startXMinWorld_Inner, startXMaxWorld_Inner, endXMinWorld_Inner, endXMaxWorld_Inner,
                                        startYWorld_Inner, endYWorld_Inner, zPositionForStartT_Inner, zPositionForEndT_Inner, HoldSprite, WhiteSprite, $"SubHold{subHoldIndex}_{i + 1}", HoldsParent, HoldOutlinesParent, RenderQueue, shaderName, planecolor);
                                    //segmentInstances.AddRange(segmentAndColorLines);
                                    subHoldInstances.Add(segmentAndColorLines[0]);
                                    OutlineInstances.Add(segmentAndColorLines[1]);
                                    OutlineInstances.Add(segmentAndColorLines[2]);
                                }
                            }
                        }
                        else
                        {
                            // Jagnum > 0 时，创建锯齿状的 SubHold
                            int totalSegments = subHold.Jagnum * 2;
                            float timeStep = (subHold.endT - subHold.startT) / totalSegments;

                            List<GameObject> segmentInstances = new List<GameObject>();

                            for (int i = 0; i < totalSegments; i++)
                            {
                                float startT = subHold.startT + i * timeStep;
                                float endT = subHold.startT + (i + 1) * timeStep;

                                float startY_Inner = associatedJudgePlaneObject.GetPlaneYAxis(startT);
                                float endY_Inner = associatedJudgePlaneObject.GetPlaneYAxis(endT);

                                // 根据摄像机角度修正y轴坐标，使y轴坐标在摄像机视角下是线性变换的
                                float startYWorld_Inner = TransformYCoordinate(startY_Inner);
                                float endYWorld_Inner = TransformYCoordinate(endY_Inner);

                                float startXMin = CalculatePosition(startT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                                float startXMax = CalculatePosition(startT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);
                                float endXMin = CalculatePosition(endT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                                float endXMax = CalculatePosition(endT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);

                                float middleXMin, middleXMax;
                                if (i % 2 == 0)
                                {
                                    // 偶数段，由 100% 宽到 50% 宽
                                    float width = (endXMax - endXMin) / 2;
                                    float center = (endXMin + endXMax) / 2;
                                    middleXMin = center - width / 2;
                                    middleXMax = center + width / 2;
                                }
                                else
                                {
                                    // 奇数段，由 50% 宽到 100% 宽
                                    float width = (startXMax - startXMin) / 2;
                                    float center = (startXMin + startXMax) / 2;
                                    middleXMin = center - width / 2;
                                    middleXMax = center + width / 2;
                                }

                                float startXMinWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * startXMin / ChartParams.XaxisMax;
                                float startXMaxWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * startXMax / ChartParams.XaxisMax;
                                float endXMinWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * endXMin / ChartParams.XaxisMax;
                                float endXMaxWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * endXMax / ChartParams.XaxisMax;
                                float middleXMinWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * middleXMin / ChartParams.XaxisMax;
                                float middleXMaxWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld_Inner, 0), HorizontalParams.HorizontalMargin) * middleXMax / ChartParams.XaxisMax;

                                // 根据 startT 和 endT 计算 Z 轴位置
                                float zPositionForStartT_Inner = CalculateZAxisPosition(startT, ChartStartTime, chart.speedList);
                                float zPositionForEndT_Inner = CalculateZAxisPosition(endT, ChartStartTime, chart.speedList);

                                if (i % 2 == 0)
                                {
                                    // 偶数段
                                    List<GameObject> segmentAndColorLines = CreateHoldQuadWithColorLines(startXMinWorld_Inner, startXMaxWorld_Inner, middleXMinWorld_Inner, middleXMaxWorld_Inner,
                                        startYWorld_Inner, endYWorld_Inner, zPositionForStartT_Inner, zPositionForEndT_Inner, HoldSprite, WhiteSprite, $"SubHold{subHoldIndex}_{i + 1}", HoldsParent, HoldOutlinesParent, RenderQueue, shaderName, planecolor);
                                    //segmentInstances.AddRange(segmentAndColorLines);
                                    subHoldInstances.Add(segmentAndColorLines[0]);
                                    OutlineInstances.Add(segmentAndColorLines[1]);
                                    OutlineInstances.Add(segmentAndColorLines[2]);
                                }
                                else
                                {
                                    // 奇数段
                                    List<GameObject> segmentAndColorLines = CreateHoldQuadWithColorLines(middleXMinWorld_Inner, middleXMaxWorld_Inner, endXMinWorld_Inner, endXMaxWorld_Inner,
                                        startYWorld_Inner, endYWorld_Inner, zPositionForStartT_Inner, zPositionForEndT_Inner, HoldSprite, WhiteSprite, $"SubHold{subHoldIndex}_{i + 1}", HoldsParent, HoldOutlinesParent, RenderQueue, shaderName, planecolor);
                                    //segmentInstances.AddRange(segmentAndColorLines);
                                    subHoldInstances.Add(segmentAndColorLines[0]);
                                    OutlineInstances.Add(segmentAndColorLines[1]);
                                    OutlineInstances.Add(segmentAndColorLines[2]);
                                }
                            }
                        }

                        subHoldIndex++;
                    }

                    // 合并所有的 SubHold 为一个整个的 Hold
                    GameObject combinedHold = CombineInstances(subHoldInstances, HoldsParent.transform);
                    combinedHold.name = $"Hold{holdIndex}";
                    ProcessCombinedInstance(combinedHold, HoldsParent, HoldsParent.layer);

                    // 初始化 StartWhiteRect 和 EndWhiteRect
                    float startY1 = associatedJudgePlaneObject.GetPlaneYAxis(hold.subHoldList[0].startT);
                    float startY2 = associatedJudgePlaneObject.GetPlaneYAxis(hold.subHoldList[0].startT + OutlineParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault);
                    float startYWorld1 = TransformYCoordinate(startY1);
                    float startYWorld2 = TransformYCoordinate(startY2);

                    float endY1 = associatedJudgePlaneObject.GetPlaneYAxis(hold.subHoldList.Last().endT - OutlineParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault);
                    float endY2 = associatedJudgePlaneObject.GetPlaneYAxis(hold.subHoldList.Last().endT);
                    float endYWorld1 = TransformYCoordinate(endY1);
                    float endYWorld2 = TransformYCoordinate(endY2);

                    // 计算开始部分的四个顶点位置
                    float startXMin1 = CalculatePosition(hold.subHoldList[0].startT, hold.subHoldList[0].startT, hold.subHoldList[0].startXMin, hold.subHoldList[0].endT, hold.subHoldList[0].endXMin, hold.subHoldList[0].XLeftFunction);
                    float startXMax1 = CalculatePosition(hold.subHoldList[0].startT, hold.subHoldList[0].startT, hold.subHoldList[0].startXMax, hold.subHoldList[0].endT, hold.subHoldList[0].endXMax, hold.subHoldList[0].XRightFunction);
                    float startXMin2 = CalculatePosition(hold.subHoldList[0].startT + OutlineParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, hold.subHoldList[0].startT, hold.subHoldList[0].startXMin, hold.subHoldList[0].endT, hold.subHoldList[0].endXMin, hold.subHoldList[0].XLeftFunction);
                    float startXMax2 = CalculatePosition(hold.subHoldList[0].startT + OutlineParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, hold.subHoldList[0].startT, hold.subHoldList[0].startXMax, hold.subHoldList[0].endT, hold.subHoldList[0].endXMax, hold.subHoldList[0].XRightFunction);

                    float startXMinWorld1 = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld1, 0), HorizontalParams.HorizontalMargin) * startXMin1 / ChartParams.XaxisMax;
                    float startXMaxWorld1 = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld1, 0), HorizontalParams.HorizontalMargin) * startXMax1 / ChartParams.XaxisMax;
                    float startXMinWorld2 = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld2, 0), HorizontalParams.HorizontalMargin) * startXMin2 / ChartParams.XaxisMax;
                    float startXMaxWorld2 = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startYWorld2, 0), HorizontalParams.HorizontalMargin) * startXMax2 / ChartParams.XaxisMax;

                    float startZ1 = CalculateZAxisPosition(hold.subHoldList[0].startT, ChartStartTime, chart.speedList);
                    float startZ2 = CalculateZAxisPosition(hold.subHoldList[0].startT + OutlineParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, ChartStartTime, chart.speedList);

                    // 计算结束部分的四个顶点位置
                    float endXMin1 = CalculatePosition(hold.subHoldList.Last().endT - OutlineParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, hold.subHoldList.Last().startT, hold.subHoldList.Last().startXMin, hold.subHoldList.Last().endT, hold.subHoldList.Last().endXMin, hold.subHoldList.Last().XLeftFunction);
                    float endXMax1 = CalculatePosition(hold.subHoldList.Last().endT - OutlineParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, hold.subHoldList.Last().startT, hold.subHoldList.Last().startXMax, hold.subHoldList.Last().endT, hold.subHoldList.Last().endXMax, hold.subHoldList.Last().XRightFunction);
                    float endXMin2 = CalculatePosition(hold.subHoldList.Last().endT, hold.subHoldList.Last().startT, hold.subHoldList.Last().startXMin, hold.subHoldList.Last().endT, hold.subHoldList.Last().endXMin, hold.subHoldList.Last().XLeftFunction);
                    float endXMax2 = CalculatePosition(hold.subHoldList.Last().endT, hold.subHoldList.Last().startT, hold.subHoldList.Last().startXMax, hold.subHoldList.Last().endT, hold.subHoldList.Last().endXMax, hold.subHoldList.Last().XRightFunction);

                    float endXMinWorld1 = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld1, 0), HorizontalParams.HorizontalMargin) * endXMin1 / ChartParams.XaxisMax;
                    float endXMaxWorld1 = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld1, 0), HorizontalParams.HorizontalMargin) * endXMax1 / ChartParams.XaxisMax;
                    float endXMinWorld2 = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld2, 0), HorizontalParams.HorizontalMargin) * endXMin2 / ChartParams.XaxisMax;
                    float endXMaxWorld2 = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endYWorld2, 0), HorizontalParams.HorizontalMargin) * endXMax2 / ChartParams.XaxisMax;

                    float endZ1 = CalculateZAxisPosition(hold.subHoldList.Last().endT - OutlineParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, ChartStartTime, chart.speedList);
                    float endZ2 = CalculateZAxisPosition(hold.subHoldList.Last().endT, ChartStartTime, chart.speedList);

                    // 增加渲染队列值，确保白色矩形显示在 Hold 之上
                    //int whiteRectRenderQueue = RenderQueue + 1;

                    // 创建开头的白色矩形及两侧色条
                    GameObject startOutline = CreateHoldStartAndEndOutlineQuad(startXMinWorld1 - OutlineParams.HoldColorLineWidth, startXMaxWorld1 + OutlineParams.HoldColorLineWidth,
                        startXMinWorld2 - OutlineParams.HoldColorLineWidth, startXMaxWorld2 + OutlineParams.HoldColorLineWidth,
                        startYWorld1, startYWorld2, startZ1, startZ2, WhiteSprite, $"StartWhiteRect{holdIndex}", HoldsParent, RenderQueue, shaderName, planecolor);

                    // 创建结尾的白色矩形及两侧色条
                    GameObject endOutline = CreateHoldStartAndEndOutlineQuad(endXMinWorld1 - OutlineParams.HoldColorLineWidth, endXMaxWorld1 + OutlineParams.HoldColorLineWidth,
                        endXMinWorld2 - OutlineParams.HoldColorLineWidth, endXMaxWorld2 + OutlineParams.HoldColorLineWidth,
                        endYWorld1, endYWorld2, endZ1, endZ2, WhiteSprite, $"EndWhiteRect{holdIndex}", HoldsParent, RenderQueue, shaderName, planecolor);

                    // 合并 Hold 周围一圈的所有亮条（包括 StartWhiteRect 和 EndWhiteRect）
                    //List<GameObject> allOutlineInstances = new List<GameObject>();
                    OutlineInstances.Add(startOutline);
                    OutlineInstances.Add(endOutline);

                    GameObject combinedHoldOutline = CombineInstances(OutlineInstances, HoldOutlinesParent.transform);
                    combinedHoldOutline.name = $"HoldOutline{holdIndex}";
                    ProcessCombinedInstance(combinedHoldOutline, HoldOutlinesParent, HoldOutlinesParent.layer);

                    //注意Hold的实际世界坐标与其他Note不一致，需要获取Hold对应的标准坐标
                    float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(HoldstartT);
                    float yPos = TransformYCoordinate(yAxisPosition);

                    // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（这里可复用已有的相关方法，假设已经有合适的方法定义，参数需根据实际传入合适的世界坐标点）
                    Vector3 referencePoint = new Vector3(0, yPos, 0);
                    float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);

                    // 计算X轴坐标
                    float HoldstartX = hold.GetFirstSubHoldStartX();
                    float startXWorld = worldUnitToScreenPixelX * HoldstartX / ChartParams.XaxisMax;

                    // Hold实例的标准位置
                    float zPosForStartT = CalculateZAxisPosition(HoldstartT, ChartStartTime, chart.speedList);
                    Vector3 HoldPos = new Vector3(-startXWorld, yPos, zPosForStartT);

                    // 检查时间点和实例名是否都在 MultiHitPairs 中
                    if (MultiHitPairs.ContainsKey(HoldstartT) &&
                        MultiHitPairs.TryGetValue(HoldstartT, out List<string> namesAtTime) &&
                        namesAtTime.Contains(combinedHold.name))
                    {
                        if (!MultiHitPairsCoord.ContainsKey(HoldstartT))
                        {
                            MultiHitPairsCoord[HoldstartT] = new List<Vector3>();
                        }
                        //添加Note坐标至MultiHitPairsCoord
                        MultiHitPairsCoord[HoldstartT].Add(HoldPos);
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
                // 用于存储上一个SubStar的起始信息
                float prevStartT = 0;
                float prevStartX = 0;
                float prevStartY = 0;
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
                    // 四舍五入
                    int numArrows = (int)MathF.Floor(curveLength * StarArrowParams.subArrowsPerUnitLength + 0.5f);
                    //Debug.Log(numArrows);
                    float rateStep = 1.0f / (float)(numArrows - 1);

                    if (subStarIndex > 1)
                    {
                        // 如果不是第一个SubStar，使用上一个SubStar的结束位置作为当前SubStar的起始位置
                        subStar.starTrackStartT = prevStartT;
                        subStar.startX = prevStartX;
                        subStar.startY = prevStartY;
                    }

                    (float startT, float startX, float startY) = InitiateStarArrows(subStar, starIndex, subStarIndex, numArrows, rateStep);
                    prevStartT = startT;
                    prevStartX = startX;
                    prevStartY = startY;
                    subStarIndex++;
                }
                starIndex++;
            }
        }
    }

    public (float endT, float endX, float endY) InitiateStarArrows(Star.SubStar subStar, int starIndex, int subStarIndex, int numArrows, float rateStep)
    {
        //Debug.Log($"{starIndex}  {subStarIndex}");
        GameObject StarArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarArrow");
        GameObject StarStartArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarStartArrow");
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
            Vector2 subStarStartScreen = ScalePositionToScreenStar(subStarStart, SubStarsParentRect);
            //Debug.Log(subStarStart);
            //Debug.Log(subStarStartScreen);

            switch (subStar.trackFunction)
            {

                // 如果是线性，则根据起始位置和终止位置初始化
                case TrackFunctionType.Linear:
                    Vector2 subStarEnd = new Vector2(subStar.endX, subStar.endY);
                    Vector2 subStarEndScreen = ScalePositionToScreenStar(subStarEnd, SubStarsParentRect);
                    if (subStarIndex == 1)
                    {
                        // 当 subStarIndex 为 1 时，初始化第一个箭头；否则忽略第一个箭头
                        //计算箭头位置
                        //{ Debug.Log(currentRate + "   " + subStarStartScreen + "   " + subStarEndScreen); }
                        Vector2 position = CalculateSubArrowPositionLinear(currentRate, subStarStartScreen, subStarEndScreen);
                        //Debug.Log(position);
                        float rotation = CalculateSubArrowRotationLinear(currentRate, subStarStartScreen, subStarEndScreen);
                        // 初始化箭头
                        GameObject arrow = Instantiate(StarStartArrowPrefab);
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
                        Vector2 position = CalculateSubArrowPositionLinear(currentRate, subStarStartScreen, subStarEndScreen);
                        float rotation = CalculateSubArrowRotationLinear(currentRate, subStarStartScreen, subStarEndScreen);
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

                    // 这里暂时先返回传入的subStar的起始信息，后续需补充正确的计算逻辑
                    return (subStar.starTrackEndT, subStar.endX, subStar.endY);

                // 如果是圆弧，则首先需要计算终止位置
                case TrackFunctionType.CWC:
                case TrackFunctionType.CCWC:


                    Vector2 substarEndScreen = CauculateEndScreenStar(subStarStartScreen, SubStarsParentRect, subStar);
                    //Debug.Log(substarEndScreen);
                    Vector2 substarEnd = ScreenPositionToScaleStar(substarEndScreen, SubStarsParentRect);
                    //Debug.Log(substarEnd);
                    subStar.endX = substarEnd.x;
                    subStar.endY = substarEnd.y;

                    if (subStarIndex == 1)
                    {
                        // 当 subStarIndex 为 1 时，初始化第一个箭头；否则忽略第一个箭头
                        //计算箭头位置
                        //{ Debug.Log(currentRate + "   " + subStarStartScreen + "   " + subStarEndScreen); }
                        Vector2 position = CalculateSubArrowPositionCircle(currentRate, subStarStartScreen, SubStarsParentRect, subStar);
                        float rotation = CalculateSubArrowRotationCircle(currentRate, subStarStartScreen, subStar);
                        // 初始化箭头
                        GameObject arrow = Instantiate(StarStartArrowPrefab);
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
                        Vector2 position = CalculateSubArrowPositionCircle(currentRate, subStarStartScreen, SubStarsParentRect, subStar);
                        float rotation = CalculateSubArrowRotationCircle(currentRate, subStarStartScreen, subStar);
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

                    // 这里暂时先返回传入的subStar的起始信息，后续需补充正确的计算逻辑
                    return (subStar.starTrackEndT, substarEnd.x, substarEnd.y);
            }
        }
        // 如果预制体未找到，返回默认值
        return (0, 0, 0);
    }

    public void InstantiateMultiHitLines()
    {
        GameObject multiHitLinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/MultiHitLine");
        if (multiHitLinePrefab == null)
        {
            Debug.LogError("MultiHitLine预制体未找到！");
            return;
        }

        // 生成所有y轴坐标不同的坐标对（组合而非排列，避免重复连线）
        int Index = 1;

        // 遍历所有多押时间点的坐标列表
        foreach (var timeGroup in MultiHitPairsCoord)
        {
            float startT = timeGroup.Key;
            List<Vector3> coordinates = timeGroup.Value;
            if (coordinates.Count < 2) continue; // 至少需要2个坐标才能连线

            for (int i = 0; i < coordinates.Count; i++)
            {
                for (int j = i + 1; j < coordinates.Count; j++)
                {
                    Vector3 pointA = coordinates[i];
                    Vector3 pointB = coordinates[j];
                    if (pointA.y != pointB.y) // 仅当y轴坐标不同时连线
                    {
                        CreateMultiHitLine(multiHitLinePrefab, pointA, pointB, startT, Index);
                        Index += 1;
                    }
                }
            }
        }


    }

    /// <summary>
    /// 根据两个坐标点实例化多押判定线，并设置位置、旋转和缩放
    /// </summary>
    private void CreateMultiHitLine(GameObject prefab, Vector3 startPos, Vector3 endPos, float startT, int Index)
    {
        // 实例化线条
        GameObject lineInstance = Instantiate(prefab);
        lineInstance.name = $"MultiHitLine_{Index}";



        // 设置父节点和图层
        lineInstance.transform.SetParent(MultiHitLinesParent.transform);
        lineInstance.layer = MultiHitLinesParent.layer;

        // 计算线条的中点作为位置
        Vector3 midPoint = (startPos + endPos) / 2;
        // 摄像机是俯视角，观感更好
        midPoint.y = midPoint.y - 0.1f;
        lineInstance.transform.position = midPoint;

        // 排除两点重合的情况
        Vector3 direction = endPos - startPos;
        if (direction.magnitude < 0.001f)
        {
            Destroy(lineInstance);
            return;
        }

        // 设置旋转：绕Y轴旋转180度，使正面朝向负Z轴（摄像机方向）
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineInstance.transform.rotation = Quaternion.Euler(0, 180f, -angle); // 新增绕Y轴180度旋转

        // 计算线条长度（x-y平面的距离）
        float lineLength = direction.magnitude;

        // 获取Sprite的原始宽度（考虑pixelsPerUnit）
        float baseSpriteWidth = GetSpriteOriginalWidth(prefab);

        // 计算正确的缩放比例：目标长度 / 精灵原始宽度（加长一点点，观感更好）
        float scaleFactor = lineLength / baseSpriteWidth + 10f;

        // 设置x轴缩放（假设Sprite的x轴为长度方向）
        lineInstance.transform.localScale = new Vector3(
            scaleFactor,
            lineInstance.transform.localScale.y,
            lineInstance.transform.localScale.z
        );

        //未到达渲染时间的设置为非激活
        if (startT > ChartParams.NoteRenderTimeOffset)
        {
            lineInstance.SetActive(false);
        }

        // 添加到时间-实例映射
        if (!startTimeToInstanceNames.ContainsKey(startT))
        {
            startTimeToInstanceNames[startT] = new List<string>();
        }
        startTimeToInstanceNames[startT].Add(lineInstance.name);

        keyReachedJudgment[lineInstance.name] = new KeyInfo(startT);

    }

    // 获取预制体的原始x轴长度（模型导入时的实际大小，不受缩放影响）
    private float GetSpriteOriginalWidth(GameObject prefab)
    {
        SpriteRenderer spriteRenderer = prefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogError($"预制体 {prefab.name} 缺少SpriteRenderer组件或Sprite为空！");
            return 1f; // 默认返回1
        }

        // 获取Sprite的原始尺寸（像素）并转换为Unity单位
        Sprite sprite = spriteRenderer.sprite;
        float pixelsPerUnit = sprite.pixelsPerUnit;
        float originalWidthInPixels = sprite.rect.width;

        // 计算Unity世界中的实际宽度
        return originalWidthInPixels / pixelsPerUnit;
    }

    private List<GameObject> CreateJudgePlaneAndColorLinesQuad(float startY, float endY, float startT, float endT, Sprite sprite, string objectName,
        GameObject judgePlaneParent, GameObject ColorLinesParent, int RenderQueue, Color planecolor, List<Speed> speedList)
    {
        //Debug.Log(startT);
        // 只有当startT为0时，startT往前推ChartStartTime
        if (Math.Abs(startT - 0f) <= 0.001)
        {
            startT -= ChartStartTime;
        }
        //Debug.Log(startT);
        //Debug.Log(planecolor);

        // 根据摄像机角度修正y轴坐标，使y轴坐标在摄像机视角下是线性变换的
        float startYWorld = TransformYCoordinate(startY);
        float endYWorld = TransformYCoordinate(endY);

        // 根据SubJudgePlane的StartT来设置实例的Z轴位置（这里将变量名修改得更清晰些，叫zPositionForStartT）
        float zPositionForStartT = CalculateZAxisPosition(startT, ChartStartTime, speedList);
        //Debug.Log(zPositionForStartT);
        float zPositionForEndT = CalculateZAxisPosition(endT, ChartStartTime, speedList);

        // 计算在Z轴方向的长度（之前代码中的height变量，这里改为lengthForZAxis）
        //float lengthForZAxis = (endT - startT) * SpeedParams.NoteSpeedDefault;

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
        Vector3 point3 = new Vector3(endXWorld, endYWorld, zPositionForEndT);
        Vector3 point4 = new Vector3(-endXWorld, endYWorld, zPositionForEndT);

        // 创建JudgePlane实例，使用Sprite的颜色，不再额外赋予灰色
        GameObject judgePlaneInstance = CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, sprite, objectName, judgePlaneParent, RenderQueue, 1f, "MaskMaterial");

        // 创建左侧亮条实例
        Vector3 leftPoint1 = new Vector3(-startXWorldPlus, startYWorld, zPositionForStartT);
        Vector3 leftPoint2 = new Vector3(-startXWorld, startYWorld, zPositionForStartT);
        Vector3 leftPoint3 = new Vector3(-endXWorld, endYWorld, zPositionForEndT);
        Vector3 leftPoint4 = new Vector3(-endXWorldPlus, endYWorld, zPositionForEndT);

        //string leftObjectName = $"LeftStrip_{objectName}";
        GameObject leftStripInstance = CreateQuadFromPoints.CreateQuad(leftPoint1, leftPoint2, leftPoint3, leftPoint4, sprite, objectName, ColorLinesParent, RenderQueue, 1f, "MaskMaterialColorLine");
        SetSpriteColor(leftStripInstance, planecolor);


        // 创建右侧亮条实例
        Vector3 rightPoint1 = new Vector3(startXWorldPlus, startYWorld, zPositionForStartT);
        Vector3 rightPoint2 = new Vector3(startXWorld, startYWorld, zPositionForStartT);
        Vector3 rightPoint3 = new Vector3(endXWorld, endYWorld, zPositionForEndT);
        Vector3 rightPoint4 = new Vector3(endXWorldPlus, endYWorld, zPositionForEndT);

        //string rightObjectName = $"RightStrip_{objectName}";
        GameObject rightStripInstance = CreateQuadFromPoints.CreateQuad(rightPoint1, rightPoint2, rightPoint3, rightPoint4, sprite, objectName, ColorLinesParent, RenderQueue, 1f, "MaskMaterialColorLine");
        SetSpriteColor(rightStripInstance, planecolor);

        List<GameObject> instances = new List<GameObject>
        {
            judgePlaneInstance,
            leftStripInstance,
            rightStripInstance
        };
        return instances;
    }


    private GameObject CreateHoldStartAndEndOutlineQuad(float startXMinWorld, float startXMaxWorld, float endXMinWorld, float endXMaxWorld,
        float startY, float endY, float zPositionForStartT, float zPositionForEndT, Sprite sprite, string objectName, GameObject parentObject, int RenderQueue, string shaderName, Color color)
    {

        float AlphaOutline = 1f;
        // 注意四边形顶点顺序
        Vector3 point1 = new Vector3(-startXMinWorld, startY, zPositionForStartT);
        Vector3 point2 = new Vector3(-endXMinWorld, endY, zPositionForEndT);
        Vector3 point3 = new Vector3(-endXMaxWorld, endY, zPositionForEndT);
        Vector3 point4 = new Vector3(-startXMaxWorld, startY, zPositionForStartT);

        GameObject holdInstance = CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, sprite, objectName, parentObject, RenderQueue, AlphaOutline, shaderName);
        SetSpriteColor(holdInstance, color);

        return holdInstance;
    }

    private List<GameObject> CreateHoldQuadWithColorLines(float startXMinWorld, float startXMaxWorld, float endXMinWorld, float endXMaxWorld,
        float startY, float endY, float zPositionForStartT, float zPositionForEndT, Sprite spritehold, Sprite spritecolor, string objectName, GameObject parentObject, GameObject colorLineParentObject, int RenderQueue, string shaderName, Color color)
    {
        float AlphaHold = 0.8f;
        float AlphaOutline = 1f;

        List<GameObject> instances = new List<GameObject>();

        // 注意四边形顶点顺序
        Vector3 point1 = new Vector3(-startXMinWorld, startY, zPositionForStartT);
        Vector3 point2 = new Vector3(-endXMinWorld, endY, zPositionForEndT);
        Vector3 point3 = new Vector3(-endXMaxWorld, endY, zPositionForEndT);
        Vector3 point4 = new Vector3(-startXMaxWorld, startY, zPositionForStartT);

        // 创建 Hold 实例
        GameObject holdInstance = CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, spritehold, objectName, parentObject, RenderQueue, AlphaHold, shaderName);
        instances.Add(holdInstance);

        // 创建左侧亮条实例
        float leftStartXMinWorld = -startXMinWorld + OutlineParams.HoldColorLineWidth;
        float leftEndXMinWorld = -endXMinWorld + OutlineParams.HoldColorLineWidth;
        Vector3 leftPoint1 = new Vector3(leftStartXMinWorld, startY, zPositionForStartT);
        Vector3 leftPoint2 = new Vector3(-startXMinWorld, startY, zPositionForStartT);
        Vector3 leftPoint3 = new Vector3(-endXMinWorld, endY, zPositionForEndT);
        Vector3 leftPoint4 = new Vector3(leftEndXMinWorld, endY, zPositionForEndT);

        GameObject leftStripInstance = CreateQuadFromPoints.CreateQuad(leftPoint1, leftPoint2, leftPoint3, leftPoint4, spritecolor, $"Left_{objectName}", colorLineParentObject, RenderQueue, AlphaOutline, "MaskMaterialColorLine");
        SetSpriteColor(leftStripInstance, color);

        instances.Add(leftStripInstance);

        // 创建右侧亮条实例
        float rightStartXMaxWorld = -startXMaxWorld - OutlineParams.HoldColorLineWidth;
        float rightEndXMaxWorld = -endXMaxWorld - OutlineParams.HoldColorLineWidth;
        Vector3 rightPoint1 = new Vector3(-startXMaxWorld, startY, zPositionForStartT);
        Vector3 rightPoint2 = new Vector3(rightStartXMaxWorld, startY, zPositionForStartT);
        Vector3 rightPoint3 = new Vector3(rightEndXMaxWorld, endY, zPositionForEndT);
        Vector3 rightPoint4 = new Vector3(-endXMaxWorld, endY, zPositionForEndT);

        GameObject rightStripInstance = CreateQuadFromPoints.CreateQuad(rightPoint1, rightPoint2, rightPoint3, rightPoint4, spritecolor, $"Right_{objectName}", colorLineParentObject, RenderQueue, AlphaOutline, "MaskMaterialColorLine");
        SetSpriteColor(rightStripInstance, color);

        instances.Add(rightStripInstance);

        return instances;
    }

}
