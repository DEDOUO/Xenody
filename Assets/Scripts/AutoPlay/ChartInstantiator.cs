using UnityEngine;
using UnityEditor;
using Params;
using static Utility;
using System.Collections.Generic;
using System;
using Note;
using static JudgePlane;
using System.Linq;
using TMPro;

//using DocumentFormat.OpenXml.Spreadsheet;



public class ChartInstantiator : MonoBehaviour
{
    private GameObject JudgePlanesParent;
    private GameObject JudgeLinesParent;
    private GameObject ColorLinesParent;
    private GameObject TapsParent;
    //private GameObject SlidesParent;
    //private GameObject FlicksParent;
    private GameObject ArrowsParent;
    private GameObject HoldsParent;
    private GameObject HoldOutlinesParent;
    private GameObject StarsParent;
    private GameObject SubStarsParent;
    public GameObject JudgeTexturesParent;
    private GameObject MultiHitLinesParent;
    [SerializeField] private TMP_Text fpsText; // 需在Inspector中关联TextMeshPro组件

    private Sprite JudgePlaneSprite;
    private Sprite HoldSprite;
    private Sprite HoldSlideSprite;
    private Sprite WhiteSprite;

    private GlobalRenderOrderManager renderOrderManager;
    private GameObject videoPlayerContainer;

    public Dictionary<float, List<string>> startTimeToInstanceNames = new Dictionary<float, List<string>>(); // 存储startT到对应实例名列表的映射
    public Dictionary<string, GameObject> InstanceNamesToGameObject = new Dictionary<string, GameObject>(); // 存储实例名到游戏物体的映射
    public Dictionary<string, bool> IfSlideDict = new Dictionary<string, bool>(); // 存储实例名到是否为Slide的映射
    public Dictionary<string, bool> IfFlickDict = new Dictionary<string, bool>(); // 存储实例名到是否为Slide的映射

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
    private Camera mainCamera = null;
    private float bottomPixel;
    private float topPixel;

    private GameObject judgeLinePrefab;
    private GameObject tapPrefab;
    private GameObject slidePrefab;
    private GameObject flickArrowPrefab;
    private GameObject starheadPrefab;
    private GameObject StarStartFXPrefab;
    private GameObject StarArrowPrefab;
    private GameObject StarStartArrowPrefab;
    private GameObject multiHitLinePrefab;


    // 新增的公共方法，用于接收各个参数并赋值给对应的私有变量
    public void SetParameters(GameObject judgePlanesParent, GameObject judgeLinesParent, GameObject colorLinesParent, GameObject tapsParent, GameObject arrowsParent,
        GameObject holdsParent, GameObject holdOutlinesParent, GameObject starsParent, GameObject subStarsParent, GameObject judgeTexturesParent, GameObject multiHitLinesParent,
        Sprite judgePlaneSprite, GlobalRenderOrderManager globalRenderOrderManager, GameObject animatorContainer, TMP_Text FpsText)
    {
        JudgePlanesParent = judgePlanesParent;
        JudgeLinesParent = judgeLinesParent;
        ColorLinesParent = colorLinesParent;
        TapsParent = tapsParent;
        ArrowsParent = arrowsParent;
        HoldsParent = holdsParent;
        HoldOutlinesParent = holdOutlinesParent;
        StarsParent = starsParent;
        SubStarsParent = subStarsParent;
        JudgeTexturesParent = judgeTexturesParent;
        MultiHitLinesParent = multiHitLinesParent;
        JudgePlaneSprite = judgePlaneSprite;
        renderOrderManager = globalRenderOrderManager;
        videoPlayerContainer = animatorContainer;
        fpsText = FpsText;

        //WhiteSprite = Resources.Load<Sprite>("Sprites/WhiteSprite");
        //if (WhiteSprite == null)
        //{
        //    Debug.LogError("Failed to load sprite: Sprites/WhiteSprite");
        //}

    }

    private void LoadResources()
    {
        WhiteSprite = Resources.Load<Sprite>("Sprites/WhiteSprite");
        HoldSprite = Resources.Load<Sprite>("Sprites/HoldBlueSprite2");
        HoldSlideSprite = Resources.Load<Sprite>("Sprites/HoldSlideSprite");
        judgeLinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/JudgeLine");
        tapPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Tap3D");
        slidePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Slide3D");
        flickArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/FlickArrow3");
        starheadPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarHead3D");
        StarStartFXPrefab = Resources.Load<GameObject>("Prefabs/External/HitReverse");
        StarArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarArrow");
        StarStartArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarStartArrow");
        multiHitLinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/MultiHitLine");
    }



    private void PrepareStartTimeMapping(Chart chart)
    {

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
                // 如果是slide，则不加入多押字典
                if (!tap.IfSlide)
                { 
                    MultiHitDict.Add(new KeyValuePair<float, string>(tap.startT, instanceName));
                }
                keyReachedJudgment[instanceName] = new KeyInfo(tap.startT);
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
        mainCamera = Camera.main;
        bottomPixel = AspectRatioManager.croppedScreenHeight* (1 - HorizontalParams.VerticalMarginBottom) + (Screen.height - AspectRatioManager.croppedScreenHeight) / 2f;
        topPixel = AspectRatioManager.croppedScreenHeight* (1 - HorizontalParams.VerticalMarginCeiling) + (Screen.height - AspectRatioManager.croppedScreenHeight) / 2f;

        LoadResources();
        PrepareStartTimeMapping(chart);
        InstantiateJudgePlanes(chart);
        InstantiateJudgeLines(chart);
        InstantiateTaps(chart);
        //InstantiateSlides(chart);
        //InstantiateFlicks(chart);
        InstantiateHolds(chart);
        InstantiateStarHeads(chart);
        InstantiateSubStars(chart);
        InstantiateMultiHitLines();

        subStarInfoDict = Star.InitializeSubStarInfo(chart, SubStarsParent);
        //print("谱面初始化完成！");
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
                //float FirstStartT = 0f;
                float FirstStartY = 0f;

                foreach (var subJudgePlane in judgePlane.subJudgePlaneList)
                {

                    if (subJudgePlaneIndex == 1)
                    {
                        planecolor = GradientColorList.GetColorAtTimeAndY(subJudgePlane.startT, subJudgePlane.startY);
                        //FirstStartT = subJudgePlane.startT;
                        FirstStartY = subJudgePlane.startY;
                    };

                    //无论是线性还是非线性，都分段生成
                    // 精细度设为8，用于分割时间区间
                    int segments = FinenessParams.Segment;
                    float timeStep = (subJudgePlane.endT - subJudgePlane.startT) / segments;

                    for (int i = 0; i < segments; i++)
                    {
                        float startT = subJudgePlane.startT + i * timeStep;
                        float endT = subJudgePlane.startT + (i + 1) * timeStep;
                        float startY = CalculatePosition(startT, subJudgePlane.startT, subJudgePlane.startY, subJudgePlane.endT, subJudgePlane.endY, subJudgePlane.yAxisFunction);
                        float endY = CalculatePosition(endT, subJudgePlane.startT, subJudgePlane.startY, subJudgePlane.endT, subJudgePlane.endY, subJudgePlane.yAxisFunction);

                        //float startY = judgePlane.GetPlaneYAxis(startT);
                        //float endY = judgePlane.GetPlaneYAxis(endT);

                        List<GameObject> ObjectsToCombine = CreateJudgePlaneAndColorLinesQuad(startY, endY, startT, endT,
                            JudgePlaneSprite, $"Sub{subJudgePlaneIndex}_{i + 1}", JudgePlanesParent, ColorLinesParent, RenderQueue, planecolor, chart.speedList);

                        judgePlaneInstances.Add(ObjectsToCombine[0]);
                        leftStripInstances.Add(ObjectsToCombine[1]);
                        rightStripInstances.Add(ObjectsToCombine[2]);
                    }

                    subJudgePlaneIndex++;
                }

                //float ZPos = -CalculateZAxisPosition(FirstStartT, ChartStartTime, chart.speedList);

                // 合并 SubJudgePlane
                GameObject combinedJudgePlane = CombineInstances(judgePlaneInstances, JudgePlanesParent.transform);
                combinedJudgePlane.name = $"JudgePlane{judgePlaneIndex}";
                ProcessCombinedInstance(combinedJudgePlane, JudgePlanesParent, JudgePlanesParent.layer);
                //建立实例名到游戏物体的映射
                InstanceNamesToGameObject[$"JudgePlane{judgePlaneIndex}"] = combinedJudgePlane;

                // 合并左侧亮条
                GameObject combinedLeftStrip = CombineInstances(leftStripInstances, ColorLinesParent.transform);
                combinedLeftStrip.name = $"LeftColorLine{judgePlaneIndex}";
                ProcessCombinedInstance(combinedLeftStrip, ColorLinesParent, ColorLinesParent.layer);
                //建立实例名到游戏物体的映射
                //InstanceNamesToGameObject[$"LeftColorLine{judgePlaneIndex}"] = combinedLeftStrip;

                // 合并右侧亮条
                GameObject combinedRightStrip = CombineInstances(rightStripInstances, ColorLinesParent.transform);
                combinedRightStrip.name = $"RightColorLine{judgePlaneIndex}";
                ProcessCombinedInstance(combinedRightStrip, ColorLinesParent, ColorLinesParent.layer);
                //建立实例名到游戏物体的映射
                //InstanceNamesToGameObject[$"RightColorLine{judgePlaneIndex}"] = combinedRightStrip;

                // 根据 YAxis 的值，实时改变 currentJudgePlane 下所有 SubJudgePlane 实例的透明度
                judgePlane.ChangeJudgePlaneTransparency(JudgePlanesParent, FirstStartY);

            }
        }
    }


    public void InstantiateJudgeLines(Chart chart)
    {
        if (chart != null && chart.judgePlanes != null)
        {
            // 通过路径获取JudgeLine预制体的引用，假设与JudgePlane预制体在相同路径下
            //GameObject judgeLinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/JudgeLine");
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
                    Vector2 Position = ScalePositionToScreen(new Vector2(0f, YAxisUniform), JudgeLinesParent.GetComponent<RectTransform>());
                    judgeLineRectTransform.anchoredPosition3D = new Vector3(Position.x, Position.y, 0);
                    judgeLineRectTransform.localRotation = Quaternion.Euler(0, 0, 0);
                    judgeLineRectTransform.localScale = new Vector3(1000000, 100, 1);

                    //建立实例名到游戏物体的映射
                    InstanceNamesToGameObject[$"JudgeLine{judgePlaneIndex}"] = judgeLineInstance;

                    //如果是从0时刻开始的JudgeLine，就不设为非激活了
                    if (subJudgePlane.startT > 0)
                    {
                        judgeLineInstance.SetActive(false);
                    }
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
            //GameObject tapPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Tap3D");
            //GameObject slidePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/Slide3D");
            //GameObject flickArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/FlickArrow");

            if (tapPrefab != null & slidePrefab != null & flickArrowPrefab != null)
            {
                float tapXAxisLength = 0; // 先在外层定义变量，初始化为0，后续根据实际情况赋值
                MeshFilter meshFilter = tapPrefab.GetComponent<MeshFilter>();
                if (meshFilter != null)
                { tapXAxisLength = meshFilter.sharedMesh.bounds.size.x; }
                else
                { Debug.LogError($"Tap3D预制体实例 {tapPrefab.name} 缺少MeshFilter组件，无法获取X轴长度进行缩放设置！"); }

                int tapIndex = 1;
                foreach (var tap in chart.taps)
                {

                    // 实例化Tap预制体
                    //根据是否为Slide区分外观
                    GameObject tapInstance = null;
                    if (!tap.IfSlide)
                    { 
                        tapInstance = Instantiate(tapPrefab);
                        IfSlideDict[$"Tap{tapIndex}"] = false;
                    }
                    else
                    { 
                        tapInstance = Instantiate(slidePrefab);
                        IfSlideDict[$"Tap{tapIndex}"] = true;
                    }
                    
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
                        float yPos = TransformYCoordinate(mainCamera, yAxisPosition, bottomPixel, topPixel);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（封装成方法方便复用，以下是示例方法定义，参数需根据实际情况传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yPos, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);

                        // 根据noteSize折算到X轴世界坐标长度，计算每单位noteSize对应的世界坐标长度
                        float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Tap本身在X轴的世界坐标长度和noteSize计算X轴的缩放值
                        float xAxisScale = noteSizeWorldLengthPerUnit / tapXAxisLength * tap.noteSize;

                        // 设置Tap实例的缩放比例（只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        tapInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.NoteThickness, 1);

                        // 设置Tap实例的位置（X、Y、Z轴坐标），同时考虑Z轴偏移量
                        float zPositionForStartT = CalculateZAxisPosition(tap.startT, ChartStartTime, chart.speedList);

                        // 关键：将2D UI坐标转换为3D世界坐标
                        RectTransform SubStarsParentRect = SubStarsParent.GetComponent<RectTransform>();
                        Vector2 position = ScalePositionToScreen(new Vector2 (tap.startX, tap.startY), SubStarsParentRect);
                        Vector3 worldPosition = ConvertUIPositionToWorldPosition(position, SubStarsParentRect, mainCamera);
                        // 设置3D物体的Transform坐标
                        tapInstance.transform.position = new Vector3(worldPosition.x, worldPosition.y, zPositionForStartT + ChartParams.NoteZAxisOffset);

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

                        //建立实例名到游戏物体的映射
                        InstanceNamesToGameObject[$"Tap{tapIndex}"] = tapInstance;
                    }

                    // 检查点键是否在规定的X轴坐标范围内，如果不在范围，可进行相应处理，比如隐藏或者输出警告等（这里简单示例输出警告）
                    if (!tap.IsInXAxisRange())
                    {
                        Debug.LogWarning($"Tap with startT: {tap.startT} and startX: {tap.startX} is out of X - axis range!");
                    }

                    Vector3 leftMiddleWorldPos = GetLeftMiddleWorldPosition(tapInstance);

                    //如果包含Flick方向，则初始化箭头
                    if (tap.flickDirection.HasValue) 
                    { 
                        // 实例化Flick箭头预制体
                        GameObject flickArrowInstance = Instantiate(flickArrowPrefab);
                        flickArrowInstance.name = $"Arrow{tapIndex}";

                        // 设置Flick箭头实例的父物体为FlickArrowsParent
                        flickArrowInstance.transform.SetParent(ArrowsParent.transform);
                        // 只手动设置子物体的世界位置为父物体的世界位置
                        flickArrowInstance.transform.position = tapInstance.transform.position;

                        // 继承父物体的图层
                        int parentLayer2 = tapInstance.layer;
                        flickArrowInstance.layer = parentLayer2;

                        //调整FlickArrow的位置（针对横划情况）
                        AdjustFlickArrowPosition(flickArrowInstance, tap.flickDirection.Value);

                        //建立实例名到游戏物体的映射
                        InstanceNamesToGameObject[$"Arrow{tapIndex}"] = flickArrowInstance;

                        IfFlickDict[$"Tap{tapIndex}"] = true;
                    }
                    else
                    {
                        IfFlickDict[$"Tap{tapIndex}"] = false;
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

    public void InstantiateStarHeads(Chart chart)
    {
        if (chart != null && chart.stars != null)
        {
            // 加载常规StarHead预制体和多押时的StarHead预制体
            //GameObject starheadPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarHead3D");
            //GameObject StarStartFXPrefab = Resources.Load<GameObject>("Prefabs/External/HitReverse");
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
                    JudgePlane associatedJudgePlane = chart.GetCorrespondingJudgePlane(star.associatedPlaneId);
                    //Vector2 firstStarCoodinate = star.GetFirstSubStarCoordinates();
                    float xAxisPosition = star.subStarList[0].startX;
                    float yAxisPosition = associatedJudgePlane.GetPlaneYAxis(star.starHeadT);
                    //将第一个SubStar的startY强制绑定为StarHead属于的JudgePlane的Y轴坐标
                    star.subStarList[0].startY = yAxisPosition;
                    star.startY = yAxisPosition;
                    // 注意Outline初始化为最初的颜色
                    Color planecolor = GradientColorList.GetColorAtTimeAndY(0f, yAxisPosition);
                    float yPos = TransformYCoordinate(mainCamera, yAxisPosition, bottomPixel, topPixel);

                    // 获取判定区下边缘和上边缘在屏幕空间中的像素坐标
                    //float bottomPixel = AspectRatioManager.croppedScreenHeight * HorizontalParams.VerticalMarginBottom;
                    //float topPixel = AspectRatioManager.croppedScreenHeight * HorizontalParams.VerticalMarginCeiling;

                    // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围

                    RectTransform SubStarsParentRect = SubStarsParent.GetComponent<RectTransform>();
                    Vector2 subStarStart = new Vector2(xAxisPosition, yAxisPosition);
                    Vector2 position = ScalePositionToScreen(subStarStart, SubStarsParentRect);
                    //Debug.Log(position);
                    //RectTransform arrowRectTransform = starStartFXInstance.GetComponent<RectTransform>();
                    //arrowRectTransform.anchoredPosition3D = new Vector3(position.x, position.y, 0);

                    Vector3 referencePoint = new Vector3(0, yPos, 0);
                    float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint, HorizontalParams.HorizontalMargin);

                    // 计算X轴坐标
                    //float startXWorld = worldUnitToScreenPixelX * xAxisPosition / ChartParams.XaxisMax;
                    // 根据noteSize折算到X轴世界坐标长度，计算每单位noteSize对应的世界坐标长度
                    float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;
                    // 根据StarHead本身在X轴的世界坐标长度和noteSize计算X轴的缩放值
                    float xAxisScale = noteSizeWorldLengthPerUnit / starheadXAxisLength * ChartParams.StarHeadXAxis;

                    // 设置StarHead实例的缩放比例
                    starheadInstance.transform.localScale = new Vector3(xAxisScale, ChartParams.NoteThickness, 1);

                    // 设置StarHead实例的位置（X、Y、Z轴坐标）
                    float zPositionForStartT = CalculateZAxisPosition(star.starHeadT, ChartStartTime, chart.speedList);

                    // 关键：将2D UI坐标转换为3D世界坐标
                    Vector3 worldPosition = ConvertUIPositionToWorldPosition(position,SubStarsParentRect,mainCamera);

                    // 设置3D物体的Transform坐标
                    starheadInstance.transform.position = new Vector3(worldPosition.x, worldPosition.y, zPositionForStartT + ChartParams.NoteZAxisOffset);
                    //Debug.Log(starheadInstance.name);
                    //Debug.Log(starheadInstance.transform.position);
                    starStartFXInstance.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
                    //Debug.Log(starStartFXInstance.transform.position);

                    //先把启动特效设置为未激活
                    //starStartFXInstance.transform.position = new Vector3(-startXWorld, yPos, 0f);
                    //Debug.Log(starStartFXInstance.transform.position);
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

                    //建立实例名到游戏物体的映射
                    InstanceNamesToGameObject[$"StarHead{starIndex}"] = starheadInstance;

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
        if (chart == null || chart.holds == null)
            return;

        int holdIndex = 1;
        int renderQueue = 3000; // 渲染队列，统一命名为renderQueue，使用小写驼峰

        foreach (var hold in chart.holds)
        {
            float holdStartT = hold.GetFirstSubHoldStartTime();
            float holdEndT = hold.GetLastSubHoldEndTime();
            Sprite holdSprite;
            if (!hold.IfSlide)
            {
                holdSprite = HoldSprite;
                IfSlideDict[$"Hold{holdIndex}"] = false;
            }
            else
            {
                holdSprite = HoldSlideSprite;
                IfSlideDict[$"Hold{holdIndex}"] = true;
            }

            JudgePlane associatedJudgePlane = chart.GetCorrespondingJudgePlane(hold.associatedPlaneId);
            Color planeColor = Color.black;

            if (associatedJudgePlane != null)
            {
                string shaderName = "MaskMaterial";
                List<GameObject> subHoldInstances = new List<GameObject>();
                List<GameObject> outlineInstances = new List<GameObject>();
                int subHoldIndex = 1;

                RectTransform subStarsParentRect = SubStarsParent.GetComponent<RectTransform>(); // 优化变量名

                foreach (var subHold in hold.subHoldList)
                {
                    float startY = associatedJudgePlane.GetPlaneYAxis(subHold.startT);
                    float endY = associatedJudgePlane.GetPlaneYAxis(subHold.endT);

                    subHold.startY = startY;
                    subHold.endY = endY;
                    subHold.yAxisFunction = associatedJudgePlane.GetPlaneYAxisFunction(subHold.startT);

                    #region 计算屏幕坐标和世界坐标（优化版）
                    // 起始点坐标转换
                    Vector2 startMinScreenPos = ScalePositionToScreen(new Vector2(subHold.startXMin, startY), subStarsParentRect);
                    //Debug.Log(subHold.startXMin);
                    Vector3 startMinWorldPos = ConvertUIPositionToWorldPosition(startMinScreenPos, subStarsParentRect, mainCamera);
                    float startXMinWorld = startMinWorldPos.x;
                    //Debug.Log(startXMinWorld);
                    float startYWorld = startMinWorldPos.y;

                    Vector2 startMaxScreenPos = ScalePositionToScreen(new Vector2(subHold.startXMax, startY), subStarsParentRect);
                    Vector3 startMaxWorldPos = ConvertUIPositionToWorldPosition(startMaxScreenPos, subStarsParentRect, mainCamera);
                    float startXMaxWorld = startMaxWorldPos.x;

                    // 结束点坐标转换
                    Vector2 endMinScreenPos = ScalePositionToScreen(new Vector2(subHold.endXMin, endY), subStarsParentRect);
                    Vector3 endMinWorldPos = ConvertUIPositionToWorldPosition(endMinScreenPos, subStarsParentRect, mainCamera);
                    float endXMinWorld = endMinWorldPos.x;
                    float endYWorld = endMinWorldPos.y;

                    Vector2 endMaxScreenPos = ScalePositionToScreen(new Vector2(subHold.endXMax, endY), subStarsParentRect);
                    Vector3 endMaxWorldPos = ConvertUIPositionToWorldPosition(endMaxScreenPos, subStarsParentRect, mainCamera);
                    float endXMaxWorld = endMaxWorldPos.x;
                    #endregion

                    if (subHoldIndex == 1)
                    {
                        planeColor = GradientColorList.GetColorAtTimeAndY(subHold.startT, startY);
                    }

                    bool isSubJudgePlaneLinear = associatedJudgePlane.IsSubJudgePlaneLinear(subHold.startT, subHold.endT);

                    if (subHold.Jagnum == 0)
                    {
                        //无论是线性还是非线性，都分段生成
                        int segments = FinenessParams.Segment;
                        float timeStep = (subHold.endT - subHold.startT) / segments;

                        for (int i = 0; i < segments; i++)
                        {
                            float segmentStartT = subHold.startT + i * timeStep;
                            float segmentEndT = subHold.startT + (i + 1) * timeStep;

                            float segmentStartY = associatedJudgePlane.GetPlaneYAxis(segmentStartT);
                            float segmentEndY = associatedJudgePlane.GetPlaneYAxis(segmentEndT);

                            #region 分段坐标计算
                            // 计算当前段起始X坐标
                            float segStartXMin = CalculatePosition(segmentStartT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                            float segStartXMax = CalculatePosition(segmentStartT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);
                            // 计算当前段结束X坐标
                            float segEndXMin = CalculatePosition(segmentEndT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                            float segEndXMax = CalculatePosition(segmentEndT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);

                            // 转换为屏幕坐标和世界坐标
                            Vector2 segStartMinScreen = ScalePositionToScreen(new Vector2(segStartXMin, segmentStartY), subStarsParentRect);
                            Vector3 segStartMinWorld = ConvertUIPositionToWorldPosition(segStartMinScreen, subStarsParentRect, mainCamera);

                            Vector2 segStartMaxScreen = ScalePositionToScreen(new Vector2(segStartXMax, segmentStartY), subStarsParentRect);
                            Vector3 segStartMaxWorld = ConvertUIPositionToWorldPosition(segStartMaxScreen, subStarsParentRect, mainCamera);

                            Vector2 segEndMinScreen = ScalePositionToScreen(new Vector2(segEndXMin, segmentEndY), subStarsParentRect);
                            Vector3 segEndMinWorld = ConvertUIPositionToWorldPosition(segEndMinScreen, subStarsParentRect, mainCamera);

                            Vector2 segEndMaxScreen = ScalePositionToScreen(new Vector2(segEndXMax, segmentEndY), subStarsParentRect);
                            Vector3 segEndMaxWorld = ConvertUIPositionToWorldPosition(segEndMaxScreen, subStarsParentRect, mainCamera);
                            #endregion

                            float zSegStart = CalculateZAxisPosition(segmentStartT, ChartStartTime, chart.speedList);
                            float zSegEnd = CalculateZAxisPosition(segmentEndT, ChartStartTime, chart.speedList);

                            List<GameObject> segmentObjects = CreateHoldQuadWithColorLines(
                                segStartMinWorld.x, segStartMaxWorld.x, segEndMinWorld.x, segEndMaxWorld.x,
                                segStartMinWorld.y, segEndMinWorld.y, zSegStart, zSegEnd,
                                holdSprite, WhiteSprite,
                                $"SubHold{subHoldIndex}_{i + 1}",
                                HoldsParent, HoldOutlinesParent,
                                renderQueue, shaderName, planeColor
                            );

                            subHoldInstances.Add(segmentObjects[0]);
                            outlineInstances.Add(segmentObjects[1]);
                            outlineInstances.Add(segmentObjects[2]);
                        }
                    }
                    else
                    {
                        int totalSegments = subHold.Jagnum * 2;
                        float timeStep = (subHold.endT - subHold.startT) / totalSegments;

                        for (int i = 0; i < totalSegments; i++)
                        {
                            float segmentStartT = subHold.startT + i * timeStep;
                            float segmentEndT = subHold.startT + (i + 1) * timeStep;

                            float segmentStartY = associatedJudgePlane.GetPlaneYAxis(segmentStartT);
                            float segmentEndY = associatedJudgePlane.GetPlaneYAxis(segmentEndT);

                            #region 锯齿状坐标计算
                            // 计算当前段起始和结束X坐标
                            float segStartXMin = CalculatePosition(segmentStartT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                            float segStartXMax = CalculatePosition(segmentStartT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);
                            float segEndXMin = CalculatePosition(segmentEndT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                            float segEndXMax = CalculatePosition(segmentEndT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);

                            // 计算中间点X坐标（锯齿效果）
                            float middleXMin, middleXMax;
                            if (i % 2 == 0)
                            {
                                float width = (segEndXMax - segEndXMin) / 2;
                                float center = (segEndXMin + segEndXMax) / 2;
                                middleXMin = center - width / 2;
                                middleXMax = center + width / 2;
                            }
                            else
                            {
                                float width = (segStartXMax - segStartXMin) / 2;
                                float center = (segStartXMin + segStartXMax) / 2;
                                middleXMin = center - width / 2;
                                middleXMax = center + width / 2;
                            }

                            // 转换为屏幕坐标和世界坐标
                            Vector2 segStartMinScreen = ScalePositionToScreen(new Vector2(segStartXMin, segmentStartY), subStarsParentRect);
                            Vector3 segStartMinWorld = ConvertUIPositionToWorldPosition(segStartMinScreen, subStarsParentRect, mainCamera);

                            Vector2 segStartMaxScreen = ScalePositionToScreen(new Vector2(segStartXMax, segmentStartY), subStarsParentRect);
                            Vector3 segStartMaxWorld = ConvertUIPositionToWorldPosition(segStartMaxScreen, subStarsParentRect, mainCamera);

                            Vector2 segEndMinScreen = ScalePositionToScreen(new Vector2(segEndXMin, segmentEndY), subStarsParentRect);
                            Vector3 segEndMinWorld = ConvertUIPositionToWorldPosition(segEndMinScreen, subStarsParentRect, mainCamera);

                            Vector2 segEndMaxScreen = ScalePositionToScreen(new Vector2(segEndXMax, segmentEndY), subStarsParentRect);
                            Vector3 segEndMaxWorld = ConvertUIPositionToWorldPosition(segEndMaxScreen, subStarsParentRect, mainCamera);
                            #endregion

                            float zSegStart = CalculateZAxisPosition(segmentStartT, ChartStartTime, chart.speedList);
                            float zSegEnd = CalculateZAxisPosition(segmentEndT, ChartStartTime, chart.speedList);

                            List<GameObject> segmentObjects;
                            if (i % 2 == 0)
                            {
                                Vector2 middleMinScreen = ScalePositionToScreen(new Vector2(middleXMin, segmentEndY), subStarsParentRect);
                                Vector3 middleMinWorld = ConvertUIPositionToWorldPosition(middleMinScreen, subStarsParentRect, mainCamera);

                                Vector2 middleMaxScreen = ScalePositionToScreen(new Vector2(middleXMax, segmentEndY), subStarsParentRect);
                                Vector3 middleMaxWorld = ConvertUIPositionToWorldPosition(middleMaxScreen, subStarsParentRect, mainCamera);

                                segmentObjects = CreateHoldQuadWithColorLines(
                                    segStartMinWorld.x, segStartMaxWorld.x, middleMinWorld.x, middleMaxWorld.x,
                                    segStartMinWorld.y, middleMinWorld.y, zSegStart, zSegEnd,
                                    holdSprite, WhiteSprite,
                                    $"SubHold{subHoldIndex}_{i + 1}",
                                    HoldsParent, HoldOutlinesParent,
                                    renderQueue, shaderName, planeColor
                                );
                            }
                            else
                            {
                                Vector2 middleMinScreen = ScalePositionToScreen(new Vector2(middleXMin, segmentStartY), subStarsParentRect);
                                Vector3 middleMinWorld = ConvertUIPositionToWorldPosition(middleMinScreen, subStarsParentRect, mainCamera);

                                Vector2 middleMaxScreen = ScalePositionToScreen(new Vector2(middleXMax, segmentStartY), subStarsParentRect);
                                Vector3 middleMaxWorld = ConvertUIPositionToWorldPosition(middleMaxScreen, subStarsParentRect, mainCamera);

                                segmentObjects = CreateHoldQuadWithColorLines(
                                    middleMinWorld.x, middleMaxWorld.x, segEndMinWorld.x, segEndMaxWorld.x,
                                    middleMinWorld.y, segEndMinWorld.y, zSegStart, zSegEnd,
                                    holdSprite, WhiteSprite,
                                    $"SubHold{subHoldIndex}_{i + 1}",
                                    HoldsParent, HoldOutlinesParent,
                                    renderQueue, shaderName, planeColor
                                );
                            }

                            subHoldInstances.Add(segmentObjects[0]);
                            outlineInstances.Add(segmentObjects[1]);
                            outlineInstances.Add(segmentObjects[2]);
                        }
                    }

                    subHoldIndex++;
                }

                // 合并SubHold实例
                GameObject combinedHold = CombineInstances(subHoldInstances, HoldsParent.transform);
                combinedHold.name = $"Hold{holdIndex}";
                ProcessCombinedInstance(combinedHold, HoldsParent, HoldsParent.layer);

                //建立实例名到游戏物体的映射
                InstanceNamesToGameObject[$"Hold{holdIndex}"] = combinedHold;

                #region 计算StartWhiteRect和EndWhiteRect坐标
                // 开始部分坐标
                float startY1 = associatedJudgePlane.GetPlaneYAxis(hold.subHoldList[0].startT);
                float startY2 = associatedJudgePlane.GetPlaneYAxis(hold.subHoldList[0].startT + ChartParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault);

                Vector2 startY1Screen = ScalePositionToScreen(new Vector2(0, startY1), subStarsParentRect);
                Vector3 startY1World = ConvertUIPositionToWorldPosition(startY1Screen, subStarsParentRect, mainCamera);
                float startY1WorldVal = startY1World.y;

                Vector2 startY2Screen = ScalePositionToScreen(new Vector2(0, startY2), subStarsParentRect);
                Vector3 startY2World = ConvertUIPositionToWorldPosition(startY2Screen, subStarsParentRect, mainCamera);
                float startY2WorldVal = startY2World.y;

                // 结束部分坐标
                float endY1 = associatedJudgePlane.GetPlaneYAxis(hold.subHoldList.Last().endT - ChartParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault);
                float endY2 = associatedJudgePlane.GetPlaneYAxis(hold.subHoldList.Last().endT);

                Vector2 endY1Screen = ScalePositionToScreen(new Vector2(0, endY1), subStarsParentRect);
                Vector3 endY1World = ConvertUIPositionToWorldPosition(endY1Screen, subStarsParentRect, mainCamera);
                float endY1WorldVal = endY1World.y;

                Vector2 endY2Screen = ScalePositionToScreen(new Vector2(0, endY2), subStarsParentRect);
                Vector3 endY2World = ConvertUIPositionToWorldPosition(endY2Screen, subStarsParentRect, mainCamera);
                float endY2WorldVal = endY2World.y;

                // 计算StartWhiteRect的四个顶点X坐标
                float startXMin1 = CalculatePosition(hold.subHoldList[0].startT, hold.subHoldList[0].startT, hold.subHoldList[0].startXMin, hold.subHoldList[0].endT, hold.subHoldList[0].endXMin, hold.subHoldList[0].XLeftFunction);
                float startXMax1 = CalculatePosition(hold.subHoldList[0].startT, hold.subHoldList[0].startT, hold.subHoldList[0].startXMax, hold.subHoldList[0].endT, hold.subHoldList[0].endXMax, hold.subHoldList[0].XRightFunction);
                float startXMin2 = CalculatePosition(hold.subHoldList[0].startT + ChartParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, hold.subHoldList[0].startT, hold.subHoldList[0].startXMin, hold.subHoldList[0].endT, hold.subHoldList[0].endXMin, hold.subHoldList[0].XLeftFunction);
                float startXMax2 = CalculatePosition(hold.subHoldList[0].startT + ChartParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, hold.subHoldList[0].startT, hold.subHoldList[0].startXMax, hold.subHoldList[0].endT, hold.subHoldList[0].endXMax, hold.subHoldList[0].XRightFunction);

                Vector2 startXMin1Screen = ScalePositionToScreen(new Vector2(startXMin1, startY1), subStarsParentRect);
                Vector3 startXMin1World = ConvertUIPositionToWorldPosition(startXMin1Screen, subStarsParentRect, mainCamera);
                float startXMin1WorldVal = startXMin1World.x;

                Vector2 startXMax1Screen = ScalePositionToScreen(new Vector2(startXMax1, startY1), subStarsParentRect);
                Vector3 startXMax1World = ConvertUIPositionToWorldPosition(startXMax1Screen, subStarsParentRect, mainCamera);
                float startXMax1WorldVal = startXMax1World.x;

                Vector2 startXMin2Screen = ScalePositionToScreen(new Vector2(startXMin2, startY2), subStarsParentRect);
                Vector3 startXMin2World = ConvertUIPositionToWorldPosition(startXMin2Screen, subStarsParentRect, mainCamera);
                float startXMin2WorldVal = startXMin2World.x;

                Vector2 startXMax2Screen = ScalePositionToScreen(new Vector2(startXMax2, startY2), subStarsParentRect);
                Vector3 startXMax2World = ConvertUIPositionToWorldPosition(startXMax2Screen, subStarsParentRect, mainCamera);
                float startXMax2WorldVal = startXMax2World.x;

                // 计算EndWhiteRect的四个顶点X坐标
                float endXMin1 = CalculatePosition(hold.subHoldList.Last().endT - ChartParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault,
                    hold.subHoldList.Last().startT, hold.subHoldList.Last().startXMin,
                    hold.subHoldList.Last().endT, hold.subHoldList.Last().endXMin,
                    hold.subHoldList.Last().XLeftFunction);
                float endXMax1 = CalculatePosition(hold.subHoldList.Last().endT - ChartParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault,
                    hold.subHoldList.Last().startT, hold.subHoldList.Last().startXMax,
                    hold.subHoldList.Last().endT, hold.subHoldList.Last().endXMax,
                    hold.subHoldList.Last().XRightFunction);
                float endXMin2 = CalculatePosition(hold.subHoldList.Last().endT,
                    hold.subHoldList.Last().startT, hold.subHoldList.Last().startXMin,
                    hold.subHoldList.Last().endT, hold.subHoldList.Last().endXMin,
                    hold.subHoldList.Last().XLeftFunction);
                float endXMax2 = CalculatePosition(hold.subHoldList.Last().endT,
                    hold.subHoldList.Last().startT, hold.subHoldList.Last().startXMax,
                    hold.subHoldList.Last().endT, hold.subHoldList.Last().endXMax,
                    hold.subHoldList.Last().XRightFunction);

                Vector2 endXMin1Screen = ScalePositionToScreen(new Vector2(endXMin1, endY1), subStarsParentRect);
                Vector3 endXMin1World = ConvertUIPositionToWorldPosition(endXMin1Screen, subStarsParentRect, mainCamera);
                float endXMin1WorldVal = endXMin1World.x;

                Vector2 endXMax1Screen = ScalePositionToScreen(new Vector2(endXMax1, endY1), subStarsParentRect);
                Vector3 endXMax1World = ConvertUIPositionToWorldPosition(endXMax1Screen, subStarsParentRect, mainCamera);
                float endXMax1WorldVal = endXMax1World.x;

                Vector2 endXMin2Screen = ScalePositionToScreen(new Vector2(endXMin2, endY2), subStarsParentRect);
                Vector3 endXMin2World = ConvertUIPositionToWorldPosition(endXMin2Screen, subStarsParentRect, mainCamera);
                float endXMin2WorldVal = endXMin2World.x;

                Vector2 endXMax2Screen = ScalePositionToScreen(new Vector2(endXMax2, endY2), subStarsParentRect);
                Vector3 endXMax2World = ConvertUIPositionToWorldPosition(endXMax2Screen, subStarsParentRect, mainCamera);
                float endXMax2WorldVal = endXMax2World.x;
                #endregion

                // 创建开头的白色矩形及两侧色条
                GameObject startOutline = CreateHoldStartAndEndOutlineQuad(
                    startXMin1WorldVal + ChartParams.HoldColorLineWidth, startXMax1WorldVal - ChartParams.HoldColorLineWidth,
                    startXMin2WorldVal + ChartParams.HoldColorLineWidth, startXMax2WorldVal - ChartParams.HoldColorLineWidth,
                    startY1WorldVal, startY2WorldVal,
                    CalculateZAxisPosition(hold.subHoldList[0].startT, ChartStartTime, chart.speedList),
                    CalculateZAxisPosition(hold.subHoldList[0].startT + ChartParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, ChartStartTime, chart.speedList),
                    WhiteSprite, $"StartWhiteRect{holdIndex}", HoldsParent, renderQueue, shaderName, planeColor
                );

                // 创建结尾的白色矩形及两侧色条
                GameObject endOutline = CreateHoldStartAndEndOutlineQuad(
                    endXMin1WorldVal + ChartParams.HoldColorLineWidth, endXMax1WorldVal - ChartParams.HoldColorLineWidth,
                    endXMin2WorldVal + ChartParams.HoldColorLineWidth, endXMax2WorldVal - ChartParams.HoldColorLineWidth,
                    endY1WorldVal, endY2WorldVal,
                    CalculateZAxisPosition(hold.subHoldList.Last().endT - ChartParams.HoldColorLineWidth / SpeedParams.NoteSpeedDefault, ChartStartTime, chart.speedList),
                    CalculateZAxisPosition(hold.subHoldList.Last().endT, ChartStartTime, chart.speedList),
                    WhiteSprite, $"EndWhiteRect{holdIndex}", HoldsParent, renderQueue, shaderName, planeColor
                );

                // 合并Hold周围的所有亮条
                outlineInstances.Add(startOutline);
                outlineInstances.Add(endOutline);

                GameObject combinedHoldOutline = CombineInstances(outlineInstances, HoldOutlinesParent.transform);
                combinedHoldOutline.name = $"HoldOutline{holdIndex}";
                ProcessCombinedInstance(combinedHoldOutline, HoldOutlinesParent, HoldOutlinesParent.layer);

                //建立实例名到游戏物体的映射
                InstanceNamesToGameObject[$"HoldOutline{holdIndex}"] = combinedHoldOutline;

                #region 计算Hold标准位置
                float holdStartX = hold.GetFirstSubHoldStartX();
                float holdStartY = associatedJudgePlane.GetPlaneYAxis(holdStartT);

                // 起始点坐标转换
                Vector2 HoldScreenPos = ScalePositionToScreen(new Vector2(holdStartX, holdStartY), subStarsParentRect);
                //Debug.Log(subHold.startXMin);
                Vector3 HoldWorldPos = ConvertUIPositionToWorldPosition(HoldScreenPos, subStarsParentRect, mainCamera);

                float zPosForStartT = CalculateZAxisPosition(holdStartT, ChartStartTime, chart.speedList);
                Vector3 holdPos = new Vector3(HoldWorldPos.x, HoldWorldPos.y, zPosForStartT);

                float holdEndX = hold.GetLastSubHoldEndX();
                float holdEndY = associatedJudgePlane.GetPlaneYAxis(holdEndT);

                // 起始点坐标转换
                Vector2 HoldScreenPosEnd = ScalePositionToScreen(new Vector2(holdEndX, holdEndY), subStarsParentRect);
                //Debug.Log(subHold.startXMin);
                Vector3 HoldWorldPosEnd = ConvertUIPositionToWorldPosition(HoldScreenPosEnd, subStarsParentRect, mainCamera);

                float zPosForEndT = CalculateZAxisPosition(holdEndT, ChartStartTime, chart.speedList);
                Vector3 holdPosEnd = new Vector3(HoldWorldPosEnd.x, HoldWorldPosEnd.y, zPosForEndT);
                #endregion

                //如果包含Flick方向，则初始化箭头
                if (hold.startDirection.HasValue)
                {
                    // 实例化Flick箭头预制体
                    GameObject flickArrowInstance = Instantiate(flickArrowPrefab);
                    flickArrowInstance.name = $"HoldStartArrow{holdIndex}";

                    // 设置Flick箭头实例的父物体为FlickArrowsParent
                    flickArrowInstance.transform.SetParent(ArrowsParent.transform);
                    // 只手动设置子物体的世界位置为父物体的世界位置
                    flickArrowInstance.transform.position = holdPos;

                    // 设置为Flick的图层
                    int parentLayer2 = 9;
                    flickArrowInstance.layer = parentLayer2;

                    //调整FlickArrow的位置（针对横划情况）
                    AdjustFlickArrowPosition(flickArrowInstance, hold.startDirection.Value);

                    //建立实例名到游戏物体的映射
                    InstanceNamesToGameObject[$"HoldStartArrow{holdIndex}"] = flickArrowInstance;

                    IfFlickDict[$"HoldStart{holdIndex}"] = true;
                }
                else
                {
                    IfFlickDict[$"HoldStart{holdIndex}"] = false;
                }
                if (hold.endDirection.HasValue)
                {
                    // 实例化Flick箭头预制体
                    GameObject flickArrowInstance = Instantiate(flickArrowPrefab);
                    flickArrowInstance.name = $"HoldEndArrow{holdIndex}";

                    // 设置Flick箭头实例的父物体为FlickArrowsParent
                    flickArrowInstance.transform.SetParent(ArrowsParent.transform);
                    // 只手动设置子物体的世界位置为父物体的世界位置
                    flickArrowInstance.transform.position = holdPosEnd;
                    //Debug.Log(flickArrowInstance.transform.position);

                    // 设置为Flick的图层
                    int parentLayer2 = 9;
                    flickArrowInstance.layer = parentLayer2;

                    //调整FlickArrow的位置（针对横划情况）
                    AdjustFlickArrowPosition(flickArrowInstance, hold.endDirection.Value);

                    //建立实例名到游戏物体的映射
                    InstanceNamesToGameObject[$"HoldEndArrow{holdIndex}"] = flickArrowInstance;

                    IfFlickDict[$"HoldEnd{holdIndex}"] = true;
                }
                else
                {
                    IfFlickDict[$"HoldEnd{holdIndex}"] = false;
                }

                // 检查时间点和实例名是否在MultiHitPairs中
                if (MultiHitPairs.ContainsKey(holdStartT) &&
                    MultiHitPairs.TryGetValue(holdStartT, out List<string> namesAtTime) &&
                    namesAtTime.Contains(combinedHold.name))
                {
                    if (!MultiHitPairsCoord.ContainsKey(holdStartT))
                    {
                        MultiHitPairsCoord[holdStartT] = new List<Vector3>();
                    }
                    MultiHitPairsCoord[holdStartT].Add(holdPos);
                }
            }

            holdIndex++;
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
                    // 计算 subStar 的曲线长度
                    float curveLength = CalculateSubStarCurveLength(subStar);

                    // 根据曲线长度和每单位长度的 SubArrow 数量，计算需要初始化的 SubArrow 数量
                    // 四舍五入
                    int numArrows = (int)MathF.Floor(curveLength * StarArrowParams.subArrowsPerUnitLength + 0.5f);
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
        //GameObject StarArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarArrow");
        //GameObject StarStartArrowPrefab = Resources.Load<GameObject>("Prefabs/GamePlay/StarStartArrow");
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
            Vector2 subStarStartScreen = ScalePositionToScreen(subStarStart, SubStarsParentRect);

            switch (subStar.trackFunction)
            {

                // 如果是线性，则根据起始位置和终止位置初始化
                case TrackFunctionType.Linear:
                    Vector2 subStarEnd = new Vector2(subStar.endX, subStar.endY);
                    Vector2 subStarEndScreen = ScalePositionToScreen(subStarEnd, SubStarsParentRect);
                    if (subStarIndex == 1)
                    {
                        // 当 subStarIndex 为 1 时，初始化第一个箭头；否则忽略第一个箭头
                        //计算箭头位置
                        Vector2 position = CalculateSubArrowPositionLinear(currentRate, subStarStartScreen, subStarEndScreen);
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
                        currentRate += rateStep;
                    }

                    // 这里暂时先返回传入的subStar的起始信息，后续需补充正确的计算逻辑
                    return (subStar.starTrackEndT, subStar.endX, subStar.endY);

                // 如果是圆弧，则首先需要计算终止位置
                case TrackFunctionType.CWC:
                case TrackFunctionType.CCWC:

                    Vector2 substarEndScreen = CauculateEndScreenStar(subStarStartScreen, SubStarsParentRect, subStar);
                    Vector2 substarEnd = ScreenPositionToScale(substarEndScreen, SubStarsParentRect);
                    subStar.endX = substarEnd.x;
                    subStar.endY = substarEnd.y;

                    if (subStarIndex == 1)
                    {
                        // 当 subStarIndex 为 1 时，初始化第一个箭头；否则忽略第一个箭头
                        //计算箭头位置
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
                    }

                    currentRate += rateStep;
                    for (int i = 1; i < numArrows; i++)
                    {
                        //计算箭头位置
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
        //GameObject multiHitLinePrefab = Resources.Load<GameObject>("Prefabs/GamePlay/MultiHitLine");
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
                    if (Mathf.Abs(pointA.y - pointB.y) > 1) // 仅当y轴坐标相差较大时连线
                    {
                        CreateMultiHitLine(multiHitLinePrefab, pointA, pointB, startT, Index);
                        Index += 1;
                    }
                }
            }
        }


    }

    // 根据两个坐标点实例化多押判定线，并设置位置、旋转和缩放
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

        //建立实例名到游戏物体的映射
        InstanceNamesToGameObject[$"MultiHitLine_{Index}"] = lineInstance;
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
        // 只有当startT为0时，startT往前推ChartStartTime
        if (Math.Abs(startT - 0f) <= 0.001)
        {
            startT -= ChartStartTime;
        }

        // 根据SubJudgePlane的StartT来设置实例的Z轴位置（这里将变量名修改得更清晰些，叫zPositionForStartT）
        float zPositionForStartT = CalculateZAxisPosition(startT, ChartStartTime, speedList);
        float zPositionForEndT = CalculateZAxisPosition(endT, ChartStartTime, speedList);

        RectTransform SubStarsParentRect = SubStarsParent.GetComponent<RectTransform>();

        // 创建JudgePlane实例
        Vector2 LeftStart = ScalePositionToScreen(new Vector2(-ChartParams.XaxisMax, startY), SubStarsParentRect);
        Vector2 RightStart = ScalePositionToScreen(new Vector2(ChartParams.XaxisMax, startY), SubStarsParentRect);
        Vector2 LeftEnd = ScalePositionToScreen(new Vector2(-ChartParams.XaxisMax, endY), SubStarsParentRect);
        Vector2 RightEnd = ScalePositionToScreen(new Vector2(ChartParams.XaxisMax, endY), SubStarsParentRect);

        Vector2 LeftEdgeStart = ScalePositionToScreen(new Vector2(-ChartParams.XaxisMax- HorizontalParams.ColorLineWidth, startY), SubStarsParentRect);
        Vector2 RightEdgeStart = ScalePositionToScreen(new Vector2(ChartParams.XaxisMax+ HorizontalParams.ColorLineWidth, startY), SubStarsParentRect);
        Vector2 LeftEdgeEnd = ScalePositionToScreen(new Vector2(-ChartParams.XaxisMax- HorizontalParams.ColorLineWidth, endY), SubStarsParentRect);
        Vector2 RightEdgeEnd = ScalePositionToScreen(new Vector2(ChartParams.XaxisMax+ HorizontalParams.ColorLineWidth, endY), SubStarsParentRect);

        Vector3 LeftStartworldPos = ConvertUIPositionToWorldPosition(LeftStart, SubStarsParentRect, mainCamera);
        Vector3 RightStartworldPos = ConvertUIPositionToWorldPosition(RightStart, SubStarsParentRect, mainCamera);
        Vector3 LeftEndworldPos = ConvertUIPositionToWorldPosition(LeftEnd, SubStarsParentRect, mainCamera);
        Vector3 RightEndworldPos = ConvertUIPositionToWorldPosition(RightEnd, SubStarsParentRect, mainCamera);

        Vector3 LeftEdgeStartworldPos = ConvertUIPositionToWorldPosition(LeftEdgeStart, SubStarsParentRect, mainCamera);
        Vector3 RightEdgeStartworldPos = ConvertUIPositionToWorldPosition(RightEdgeStart, SubStarsParentRect, mainCamera);
        Vector3 LeftEdgeEndworldPos = ConvertUIPositionToWorldPosition(LeftEdgeEnd, SubStarsParentRect, mainCamera);
        Vector3 RightEdgeEndworldPos = ConvertUIPositionToWorldPosition(RightEdgeEnd, SubStarsParentRect, mainCamera);

        Vector3 point1 = new Vector3(LeftStartworldPos.x, LeftStartworldPos.y, zPositionForStartT);
        Vector3 point2 = new Vector3(RightStartworldPos.x, RightStartworldPos.y, zPositionForStartT);
        Vector3 point3 = new Vector3(RightEndworldPos.x, RightEndworldPos.y, zPositionForEndT); 
        Vector3 point4 = new Vector3(LeftEndworldPos.x, LeftEndworldPos.y, zPositionForEndT);

        // 使用Sprite的颜色，不再额外赋予灰色
        GameObject judgePlaneInstance = CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, sprite, objectName, judgePlaneParent, RenderQueue, 1f, "MaskMaterial");

        // 创建左侧亮条实例
        Vector3 leftPoint1 = new Vector3(LeftStartworldPos.x, LeftStartworldPos.y, zPositionForStartT);
        Vector3 leftPoint2 = new Vector3(LeftEdgeStartworldPos.x, LeftEdgeStartworldPos.y, zPositionForStartT);
        Vector3 leftPoint3 = new Vector3(LeftEdgeEndworldPos.x, LeftEdgeEndworldPos.y, zPositionForEndT);
        Vector3 leftPoint4 = new Vector3(LeftEndworldPos.x, LeftEndworldPos.y, zPositionForEndT);

        GameObject leftStripInstance = CreateQuadFromPoints.CreateQuad(leftPoint1, leftPoint2, leftPoint3, leftPoint4, sprite, objectName, ColorLinesParent, RenderQueue, 1f, "MaskMaterialColorLine");
        SetSpriteColor(leftStripInstance, planecolor);


        // 创建右侧亮条实例
        Vector3 rightPoint1 = new Vector3(RightStartworldPos.x, RightStartworldPos.y, zPositionForStartT);
        Vector3 rightPoint2 = new Vector3(RightEdgeStartworldPos.x, RightEdgeStartworldPos.y, zPositionForStartT);
        Vector3 rightPoint3 = new Vector3(RightEdgeEndworldPos.x, RightEdgeEndworldPos.y, zPositionForEndT);
        Vector3 rightPoint4 = new Vector3(RightEndworldPos.x, RightEndworldPos.y, zPositionForEndT);

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
        Vector3 point1 = new Vector3(startXMinWorld, startY, zPositionForStartT);
        Vector3 point2 = new Vector3(endXMinWorld, endY, zPositionForEndT);
        Vector3 point3 = new Vector3(endXMaxWorld, endY, zPositionForEndT);
        Vector3 point4 = new Vector3(startXMaxWorld, startY, zPositionForStartT);

        GameObject holdInstance = CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, sprite, objectName, parentObject, RenderQueue, AlphaOutline, shaderName);
        SetSpriteColor(holdInstance, color);

        return holdInstance;
    }

    private List<GameObject> CreateHoldQuadWithColorLines(float startXMinWorld, float startXMaxWorld, float endXMinWorld, float endXMaxWorld,
    float startY, float endY, float zPositionForStartT, float zPositionForEndT, Sprite spritehold, Sprite spritecolor, string objectName, GameObject parentObject, GameObject colorLineParentObject, int RenderQueue, string shaderName, Color color)
    {
        float AlphaHold = 0.7f;
        float AlphaOutline = 1f;

        List<GameObject> instances = new List<GameObject>();

        // 注意四边形顶点顺序
        Vector3 point1 = new Vector3(startXMinWorld, startY, zPositionForStartT);
        Vector3 point2 = new Vector3(endXMinWorld, endY, zPositionForEndT);
        Vector3 point3 = new Vector3(endXMaxWorld, endY, zPositionForEndT);
        Vector3 point4 = new Vector3(startXMaxWorld, startY, zPositionForStartT);

        // 创建 Hold 实例
        GameObject holdInstance = CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, spritehold, objectName, parentObject, RenderQueue, AlphaHold, shaderName);
        instances.Add(holdInstance);

        // 创建左侧亮条实例
        float leftStartXMinWorld = startXMinWorld + ChartParams.HoldColorLineWidth;
        float leftEndXMinWorld = endXMinWorld + ChartParams.HoldColorLineWidth;
        Vector3 leftPoint1 = new Vector3(leftStartXMinWorld, startY, zPositionForStartT);
        Vector3 leftPoint2 = new Vector3(startXMinWorld, startY, zPositionForStartT);
        Vector3 leftPoint3 = new Vector3(endXMinWorld, endY, zPositionForEndT);
        Vector3 leftPoint4 = new Vector3(leftEndXMinWorld, endY, zPositionForEndT);

        GameObject leftStripInstance = CreateQuadFromPoints.CreateQuad(leftPoint1, leftPoint2, leftPoint3, leftPoint4, spritecolor, $"Left_{objectName}", colorLineParentObject, RenderQueue, AlphaOutline, "MaskMaterialColorLine");
        SetSpriteColor(leftStripInstance, color);

        instances.Add(leftStripInstance);

        // 创建右侧亮条实例
        float rightStartXMaxWorld = startXMaxWorld - ChartParams.HoldColorLineWidth;
        float rightEndXMaxWorld = endXMaxWorld - ChartParams.HoldColorLineWidth;
        Vector3 rightPoint1 = new Vector3(startXMaxWorld, startY, zPositionForStartT);
        Vector3 rightPoint2 = new Vector3(rightStartXMaxWorld, startY, zPositionForStartT);
        Vector3 rightPoint3 = new Vector3(rightEndXMaxWorld, endY, zPositionForEndT);
        Vector3 rightPoint4 = new Vector3(endXMaxWorld, endY, zPositionForEndT);

        GameObject rightStripInstance = CreateQuadFromPoints.CreateQuad(rightPoint1, rightPoint2, rightPoint3, rightPoint4, spritecolor, $"Right_{objectName}", colorLineParentObject, RenderQueue, AlphaOutline, "MaskMaterialColorLine");
        SetSpriteColor(rightStripInstance, color);

        instances.Add(rightStripInstance);

        return instances;
    }

}
