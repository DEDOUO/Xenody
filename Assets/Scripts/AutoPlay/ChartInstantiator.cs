using UnityEngine;
using UnityEditor;
using Params;
using static Utility;
using System.Collections.Generic;


public class ChartInstantiator : MonoBehaviour
{
    private GameObject JudgePlanesParent;
    private GameObject JudgeLinesParent;
    private GameObject TapsParent;
    private GameObject SlidesParent;
    private GameObject FlicksParent;
    private GameObject HoldsParent;
    private GameObject StarsParent;
    private Sprite JudgePlaneSprite;
    private Sprite HoldSprite;
    private GlobalRenderOrderManager renderOrderManager;
    private GameObject videoPlayerContainer;
    //private Dictionary<GameObject, bool> tapReachedJudgmentLine = new Dictionary<GameObject, bool>();
    //private Dictionary<GameObject, bool> slideReachedJudgmentLine = new Dictionary<GameObject, bool>(); 

    // 新增的公共方法，用于接收各个参数并赋值给对应的私有变量
    public void SetParameters(GameObject judgePlanesParent, GameObject judgeLinesParent, GameObject tapsParent, GameObject slidesParent, GameObject flicksParent, GameObject holdsParent, GameObject starsParent,
        Sprite judgePlaneSprite, Sprite holdSprite, GlobalRenderOrderManager globalRenderOrderManager, GameObject animatorContainer)
    {
        JudgePlanesParent = judgePlanesParent;
        JudgeLinesParent = judgeLinesParent;
        TapsParent = tapsParent;
        SlidesParent = slidesParent;
        FlicksParent = flicksParent;
        HoldsParent = holdsParent;
        StarsParent = starsParent;
        JudgePlaneSprite = judgePlaneSprite;
        HoldSprite = holdSprite;
        renderOrderManager = globalRenderOrderManager;
        videoPlayerContainer = animatorContainer;
    }

    public void InstantiateJudgePlanesAndJudgeLines(Chart chart)
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
                    GameObject firstSubJudgePlaneInstance = null;

                    foreach (var subJudgePlane in judgePlane.subJudgePlaneList)
                    {
                        // 根据SubJudgePlane的函数类型来决定如何处理
                        switch (subJudgePlane.yAxisFunction)
                        {
                            case TransFunctionType.Linear:
                                firstSubJudgePlaneInstance = CreateJudgePlaneQuad(subJudgePlane.startY, subJudgePlane.endY, subJudgePlane.startT, subJudgePlane.endT, 
                                    JudgePlaneSprite, $"Sub{subJudgePlaneIndex}", judgePlaneParent);
                                break;
                            case TransFunctionType.Sin:
                            case TransFunctionType.Cos:
                                // 精细度设为8，用于分割时间区间
                                int segments = FinenessParams.Segment;
                                float timeStep = (subJudgePlane.endT - subJudgePlane.startT) / segments;
                                // 用于存储细分的Instance，以便后续合并
                                List<GameObject> segmentInstances = new List<GameObject>();

                                for (int i = 0; i < segments; i++)
                                {
                                    float startT = subJudgePlane.startT + i * timeStep;
                                    float endT = subJudgePlane.startT + (i + 1) * timeStep;
                                    float startY = CalculatePosition(startT, subJudgePlane.startT, subJudgePlane.startY, subJudgePlane.endT, subJudgePlane.endY, subJudgePlane.yAxisFunction);
                                    float endY = CalculatePosition(endT, subJudgePlane.startT, subJudgePlane.startY, subJudgePlane.endT, subJudgePlane.endY, subJudgePlane.yAxisFunction);
                                    GameObject instance = CreateJudgePlaneQuad(startY, endY, startT, endT, JudgePlaneSprite, $"Sub{subJudgePlaneIndex}_{i + 1}", judgePlaneParent);
                                    if (i == 0)
                                    {
                                        firstSubJudgePlaneInstance = instance;
                                    }
                                    segmentInstances.Add(instance);
                                }

                                // 合并细分的Instance为一个新的GameObject
                                GameObject combinedInstance = CombineInstances(segmentInstances);
                                combinedInstance.name = $"Sub{subJudgePlaneIndex}";
                                // 将合并后的Instance设置为对应的父物体的子物体
                                combinedInstance.transform.SetParent(judgePlaneParent.transform);

                                // 删除合并前的实例
                                foreach (GameObject segmentInstance in segmentInstances)
                                {
                                    Destroy(segmentInstance);
                                }

                                break;
                        }
                        // 如果为第一个subJudgePlane
                        if (subJudgePlaneIndex == 1)
                        {
                            // 实例化JudgeLine预制体，命名为JudgeLine1、JudgeLine2等
                            GameObject judgeLineInstance = Instantiate(judgeLinePrefab, firstSubJudgePlaneInstance.transform);
                            judgeLineInstance.name = $"JudgeLine{judgePlaneIndex}";
                            judgeLineInstance.transform.SetParent(JudgeLinesParent.transform);
                            // 初始化JudgeLine实例的位置与JudgePlane下第一个SubJudgePlane的位置一致（这里简单示例，可根据实际需求调整更准确的初始化逻辑）
                            judgeLineInstance.transform.position = firstSubJudgePlaneInstance.transform.position;
                            //Debug.Log(firstSubJudgePlaneInstance.transform.position.z);
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

    private GameObject CombineInstances(List<GameObject> instances)
    {
        GameObject combined = new GameObject("CombinedInstance");

        // 用于存储所有顶点、三角形索引和UV坐标
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        int vertexOffset = 0;
        foreach (GameObject instance in instances)
        {
            MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.mesh;
                // 添加顶点
                vertices.AddRange(mesh.vertices);
                // 处理三角形索引，添加偏移量
                for (int i = 0; i < mesh.triangles.Length; i++)
                {
                    triangles.Add(mesh.triangles[i] + vertexOffset);
                }
                // 添加UV坐标（如果有的话）
                if (mesh.uv.Length > 0)
                {
                    uv.AddRange(mesh.uv);
                }
                vertexOffset += mesh.vertices.Length;
            }
        }

        // 创建新的网格
        Mesh combinedMesh = new Mesh();
        combinedMesh.vertices = vertices.ToArray();
        combinedMesh.triangles = triangles.ToArray();
        if (uv.Count > 0)
        {
            combinedMesh.uv = uv.ToArray();
        }
        combinedMesh.RecalculateNormals();

        // 添加网格组件和渲染器组件
        MeshFilter combinedMeshFilter = combined.AddComponent<MeshFilter>();
        combinedMeshFilter.mesh = combinedMesh;
        MeshRenderer combinedRenderer = combined.AddComponent<MeshRenderer>();
        combinedRenderer.material = instances[0].GetComponentInChildren<MeshRenderer>().material;

        return combined;
    }

    private GameObject CreateJudgePlaneQuad(float startY, float endY, float startT, float endT, Sprite sprite, string objectName, GameObject parentObject)
    {
        // 将StartY和EndY映射为世界坐标并放大到合适范围（0 - HeightParams.HeightDefault，这里假设HeightParams.HeightDefault为6）
        float startYWorld = startY * HeightParams.HeightDefault;
        float endYWorld = endY * HeightParams.HeightDefault;

        // 根据SubJudgePlane的StartT来设置实例的Z轴位置（这里将变量名修改得更清晰些，叫zPositionForStartT）
        float zPositionForStartT = CalculateZAxisPosition(startT);

        // 计算在Z轴方向的长度（之前代码中的height变量，这里改为lengthForZAxis）
        float lengthForZAxis = (endT - startT) * SpeedParams.NoteSpeedDefault;

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

        GameObject instance = CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, sprite, objectName, parentObject);
        return instance;
    }

    public void InstantiateTaps(Chart chart)
    {
        if (chart != null && chart.taps != null)
        {
            // 假设Tap预制体的加载路径，你需要根据实际情况修改
            GameObject tapPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GamePlay/Tap.prefab", typeof(GameObject));
            if (tapPrefab != null)
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
                    // 实例化Tap预制体
                    GameObject tapInstance = Instantiate(tapPrefab);
                    tapInstance.name = $"Tap{tapIndex}"; // 命名
                    // 将Tap设置为ChartGameObjects的子物体
                    tapInstance.transform.SetParent(TapsParent.transform);

                    // 获取关联的JudgePlane实例
                    JudgePlane associatedJudgePlaneObject = GetCorrespondingJudgePlane(chart, tap.associatedPlaneId);
                    if (associatedJudgePlaneObject != null)
                    {
                        // 获取关联JudgePlane在Tap开始时间的Y轴坐标
                        float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(tap.startT);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（封装成方法方便复用，以下是示例方法定义，参数需根据实际情况传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint);
                        //Debug.Log(worldUnitToScreenPixelX);

                        // 计算Tap的X轴坐标
                        float startXWorld = worldUnitToScreenPixelX * tap.startX / ChartParams.XaxisMax;

                        // 根据noteSize折算到X轴世界坐标长度，计算每单位noteSize对应的世界坐标长度
                        float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Tap本身在X轴的世界坐标长度和noteSize计算X轴的缩放值
                        float xAxisScale = noteSizeWorldLengthPerUnit / tapXAxisLength * tap.noteSize;

                        // 设置Tap实例的缩放比例（只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        tapInstance.transform.localScale = new Vector3(xAxisScale, 1, 1);

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
                Debug.LogError("无法加载Tap预制体！");
            }
        }
    }
    public void InstantiateSlides(Chart chart)
    {
        if (chart != null && chart.slides != null)
        {
            // 假设Slide预制体的加载路径，你需要根据实际情况修改
            GameObject slidePrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GamePlay/Slide.prefab", typeof(GameObject));
            if (slidePrefab != null)
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
                    // 实例化Slide预制体
                    GameObject slideInstance = Instantiate(slidePrefab);
                    slideInstance.name = $"Slide{slideIndex}"; // 命名

                    // 将Slide设置为合适的父物体的子物体，这里假设和Taps类似，有个SlidesParent，你可根据实际调整
                    slideInstance.transform.SetParent(SlidesParent.transform);

                    // 获取关联的JudgePlane实例（假设Slide也有关联的JudgePlane，根据实际情况调整获取逻辑）
                    JudgePlane associatedJudgePlaneObject = GetCorrespondingJudgePlane(chart, slide.associatedPlaneId);
                    if (associatedJudgePlaneObject != null)
                    {
                        // 获取关联JudgePlane在Slide开始时间的Y轴坐标
                        float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(slide.startT);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（这里可复用已有的相关方法，假设已经有合适的方法定义，参数需根据实际传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint);

                        // 计算Slide的X轴坐标（根据Slide相关参数和计算逻辑来确定，示例如下，需按实际调整）
                        float startXWorld = worldUnitToScreenPixelX * slide.startX / ChartParams.XaxisMax;

                        // 根据slideSize等参数折算到X轴世界坐标长度，计算每单位slideSize对应的世界坐标长度（示例逻辑，按实际修改）
                        float slideSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Slide本身在X轴的世界坐标宽度和slideSize等参数计算X轴的缩放值（示例，按需调整）
                        float xAxisScale = slideSizeWorldLengthPerUnit / slideWidth * slide.noteSize;
                        //Debug.Log(xAxisScale);

                        // 设置Slide实例的缩放比例（这里只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        slideInstance.transform.localScale = new Vector3(xAxisScale, 1, 1);

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
                Debug.LogError("无法加载Slide预制体！");
            }
        }
    }

    public void InstantiateFlicks(Chart chart)
    {
        if (chart != null && chart.flicks != null)
        {
            // 假设Flick预制体的加载路径，你需要根据实际情况修改
            GameObject flickPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GamePlay/Flick.prefab", typeof(GameObject));
            GameObject flickArrowPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GamePlay/FlickArrow.prefab", typeof(GameObject));
            if (flickPrefab != null && flickArrowPrefab != null)
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
                    // 实例化Flick预制体
                    GameObject flickInstance = Instantiate(flickPrefab);
                    flickInstance.name = $"Flick{flickIndex}"; // 命名

                    // 将Flick设置为合适的父物体的子物体，这里假设和Taps、Slides类似，有个FlicksParent，你可根据实际调整
                    flickInstance.transform.SetParent(FlicksParent.transform);

                    // 获取关联的JudgePlane实例（假设Flick也有关联的JudgePlane，根据实际情况调整获取逻辑）
                    JudgePlane associatedJudgePlaneObject = GetCorrespondingJudgePlane(chart, flick.associatedPlaneId);
                    if (associatedJudgePlaneObject != null)
                    {
                        // 获取关联JudgePlane在Flick开始时间的Y轴坐标
                        float yAxisPosition = associatedJudgePlaneObject.GetPlaneYAxis(flick.startT);

                        // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围（这里可复用已有的相关方法，假设已经有合适的方法定义，参数需根据实际传入合适的世界坐标点）
                        Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                        float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint);

                        // 计算Flick的X轴坐标（根据Flick相关参数和计算逻辑来确定，示例如下，需按实际调整）
                        float startXWorld = worldUnitToScreenPixelX * flick.startX / ChartParams.XaxisMax;

                        // 根据flickSize等参数折算到X轴世界坐标长度，计算每单位flickSize对应的世界坐标长度（示例逻辑，按实际修改）
                        float flickSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;

                        // 根据Flick本身在X轴的世界坐标宽度和flickSize等参数计算X轴的缩放值（示例，按需调整）
                        float xAxisScale = flickSizeWorldLengthPerUnit / flickWidth * flick.noteSize;

                        // 设置Flick实例的缩放比例（这里只修改X轴缩放，保持Y、Z轴缩放为1，可根据实际需求改变）
                        flickInstance.transform.localScale = new Vector3(xAxisScale, 1, 1);

                        // 设置Flick实例的位置（X、Y、Z轴坐标，示例逻辑，按实际情况调整）
                        float zPositionForStartT = CalculateZAxisPosition(flick.startT);
                        flickInstance.transform.position = new Vector3(-startXWorld, yAxisPosition, zPositionForStartT);

                        // 实例化Flick箭头预制体
                        GameObject flickArrowInstance = Instantiate(flickArrowPrefab);
                        flickArrowInstance.name = $"FlickArrow{flickIndex}";

                        // 设置Flick箭头实例的父物体为当前Flick实例，使其覆盖在Flick上
                        flickArrowInstance.transform.SetParent(flickInstance.transform);

                        // 根据flickDirection设置箭头的旋转角度，将其方向与定义的方向一致（这里假设flickDirection表示角度相关的值，需根据实际含义调整转换逻辑）
                        //Debug.Log(flick.flickDirection);
                        float arrowRotationAngle = flick.flickDirection * 360; // 假设flickDirection是0-1之间的值，转换为0-360度的角度，根据实际调整
                        //Debug.Log(arrowRotationAngle);
                        flickArrowInstance.transform.localRotation = Quaternion.Euler(0, 0, arrowRotationAngle);

                        // 设置Flick箭头实例的位置，使其在Flick的合适位置上（比如中心位置等，根据实际调整）
                        flickArrowInstance.transform.localPosition = new Vector3(0, 0.3f, 0); // 示例，可根据实际调整

                        // 根据Flick的缩放比例同步缩放箭头（仅针对Y轴缩放，即缩放箭头长度）
                        flickArrowInstance.transform.localScale = new Vector3(0.6f, xAxisScale, 1);
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
                Debug.LogError("无法加载Flick预制体！");
            }
        }
    }

    public void InstantiateHolds(Chart chart)
    {
        if (chart != null && chart.holds != null)
        {
            int holdIndex = 1;
            // 创建一个空物体作为hold实例的父物体，用于统一管理和规范命名
            GameObject holdParent = new GameObject($"Hold{holdIndex}");
            holdParent.transform.position = new Vector3(0, 0, 0);
            // 将holdParent设置为ChartGameObjects的子物体
            holdParent.transform.SetParent(HoldsParent.transform);

            foreach (var hold in chart.holds)
            {
                JudgePlane associatedJudgePlaneObject = GetCorrespondingJudgePlane(chart, hold.associatedPlaneId);
                if (associatedJudgePlaneObject != null)
                {
                    int subHoldIndex = 1;
                    foreach (var subHold in hold.subHoldList)
                    {
                        float startY = associatedJudgePlaneObject.GetPlaneYAxis(subHold.startT);
                        float endY = associatedJudgePlaneObject.GetPlaneYAxis(subHold.endT);

                        // 根据SubHold的函数类型来决定如何处理
                        switch (subHold.XLeftFunction, subHold.XRightFunction)
                        {
                            //只有当两侧变化函数均为Linear时，才能一次性初始化
                            case (TransFunctionType.Linear, TransFunctionType.Linear):
                                float startXMinWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startY, 0)) * subHold.startXMin / ChartParams.XaxisMax;
                                float startXMaxWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startY, 0)) * subHold.startXMax / ChartParams.XaxisMax;
                                float endXMinWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endY, 0)) * subHold.endXMin / ChartParams.XaxisMax;
                                float endXMaxWorld = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endY, 0)) * subHold.endXMax / ChartParams.XaxisMax;

                                // 根据startT和endT计算Z轴位置
                                float zPositionForStartT = CalculateZAxisPosition(subHold.startT);
                                float zPositionForEndT = CalculateZAxisPosition(subHold.endT);

                                // 一次性生成整个SubHold
                                GameObject subHoldInstance = CreateHoldQuad(startXMinWorld, startXMaxWorld, endXMinWorld, endXMaxWorld, 
                                    startY, endY, zPositionForStartT, zPositionForEndT, HoldSprite, $"SubHold{subHoldIndex}", holdParent);
                                break;
                            //否则要分割成多个子块，生成后拼接
                            default:
                                // 精细度设为8，用于分割时间区间（可根据实际需求调整精细度）
                                int segments = FinenessParams.Segment;
                                float timeStep = (subHold.endT - subHold.startT) / segments;
                                // 用于存储细分的Instance，以便后续合并
                                List<GameObject> segmentInstances = new List<GameObject>();

                                for (int i = 0; i < segments; i++)
                                {
                                    float startT = subHold.startT + i * timeStep;
                                    float endT = subHold.startT + (i + 1) * timeStep;
                                    float startY_Inner = associatedJudgePlaneObject.GetPlaneYAxis(startT);
                                    float endY_Inner = associatedJudgePlaneObject.GetPlaneYAxis(startT);

                                    float startXMin = CalculatePosition(startT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                                    float startXMax = CalculatePosition(startT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);
                                    float endXMin = CalculatePosition(endT, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                                    float endXMax = CalculatePosition(endT, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);

                                    //Debug.Log("startXMin: " + startXMin + ", endXMin: " + endXMin + ", startXMax: " + startXMax + ", endXMax: " + endXMax);
                                    float startXMinWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startY_Inner, 0)) * startXMin / ChartParams.XaxisMax;
                                    float startXMaxWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, startY_Inner, 0)) * startXMax / ChartParams.XaxisMax;
                                    float endXMinWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endY_Inner, 0)) * endXMin / ChartParams.XaxisMax;
                                    float endXMaxWorld_Inner = CalculateWorldUnitToScreenPixelXAtPosition(new Vector3(0, endY_Inner, 0)) * endXMax / ChartParams.XaxisMax;

                                    //Debug.Log("startXMin: " + startXMinWorld_Inner + ", endXMin: " + endXMinWorld_Inner + ", startXMax: " + startXMaxWorld_Inner + ", endXMax: " + endXMaxWorld_Inner);

                                    // 根据startT和endT计算Z轴位置
                                    float zPositionForStartT_Inner = CalculateZAxisPosition(startT);
                                    float zPositionForEndT_Inner = CalculateZAxisPosition(endT);
                                    //Debug.Log("zPositionForStartT: " + zPositionForStartT_Inner + ", zPositionForEndT: " + zPositionForEndT_Inner);

                                    GameObject instance = CreateHoldQuad(startXMinWorld_Inner, startXMaxWorld_Inner, endXMinWorld_Inner, endXMaxWorld_Inner,
                                        startY_Inner, endY_Inner, zPositionForStartT_Inner, zPositionForEndT_Inner, HoldSprite, $"SubHold{subHoldIndex}_{i + 1}", holdParent);
                                    segmentInstances.Add(instance);
                                }

                                // 合并细分的Instance为一个新的GameObject
                                GameObject combinedInstance = CombineInstances(segmentInstances);
                                combinedInstance.name = $"SubHold{subHoldIndex}";
                                // 将合并后的Instance设置为对应的父物体的子物体
                                combinedInstance.transform.SetParent(holdParent.transform);

                                // 删除合并前的实例
                                foreach (GameObject segmentInstance in segmentInstances)
                                {
                                    Destroy(segmentInstance);
                                }
                                break;
                        }
                        subHoldIndex++;
                    }
                }
                holdIndex++;
            }
        }
    }

    public void InstantiateStarHeads(Chart chart)
    {
        if (chart != null && chart.stars != null)
        {
            // 假设Tap预制体的加载路径，你需要根据实际情况修改
            GameObject starheadPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GamePlay/StarHead.prefab", typeof(GameObject));
            if (starheadPrefab != null)
            {
                float starheadXAxisLength = 0; // 先在外层定义变量，初始化为0，后续根据实际情况赋值
                SpriteRenderer spriteRenderer = starheadPrefab.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    //获取Tap在X轴的长度（用于缩放）
                    starheadXAxisLength = spriteRenderer.sprite.bounds.size.x;
                }
                else
                {
                    Debug.LogError($"starhead预制体实例 {starheadPrefab.name} 缺少SpriteRenderer组件，无法获取X轴长度进行缩放设置！");
                }

                int starIndex = 1;
                foreach (var star in chart.stars)
                {
                    // 实例化starhead预制体
                    GameObject starheadInstance = Instantiate(starheadPrefab);
                    starheadInstance.name = $"StarHead{starIndex}"; // 命名
                    // 将starhead设置为ChartGameObjects的子物体
                    starheadInstance.transform.SetParent(StarsParent.transform);

                    // 获取开始的X轴和Y轴坐标
                    Vector2 firstStarCoodinate = star.GetFirstSubStarCoordinates();
                    float xAxisPosition = firstStarCoodinate.x;
                    float yAxisPosition = firstStarCoodinate.y;
                    yAxisPosition  *= HeightParams.HeightDefault;
                    // 计算水平方向上在世界坐标中的单位长度对应的屏幕像素长度以及水平可视范围
                    Vector3 referencePoint = new Vector3(0, yAxisPosition, 0);
                    float worldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelXAtPosition(referencePoint);

                    // 计算X轴坐标
                    float startXWorld = worldUnitToScreenPixelX * xAxisPosition / ChartParams.XaxisMax;
                    // 根据noteSize折算到X轴世界坐标长度，计算每单位noteSize对应的世界坐标长度
                    float noteSizeWorldLengthPerUnit = worldUnitToScreenPixelX / ChartParams.XaxisMax;
                    // 根据StarHead本身在X轴的世界坐标长度和noteSize计算X轴的缩放值
                    //默认StarHead对应的X轴键宽为0.8
                    float xAxisScale = noteSizeWorldLengthPerUnit / starheadXAxisLength * ChartParams.StarHeadXAxis;

                    // 设置StarHead实例的缩放比例
                    starheadInstance.transform.localScale = new Vector3(xAxisScale, xAxisScale, 1);

                    // 设置StarHead实例的位置（X、Y、Z轴坐标）
                    float zPositionForStartT = CalculateZAxisPosition(star.starHeadT);
                    starheadInstance.transform.position = new Vector3(-startXWorld, yAxisPosition, zPositionForStartT);

                    // 检查点键是否在规定的X轴坐标范围内，如果不在范围，可进行相应处理，比如隐藏或者输出警告等（这里简单示例输出警告）
                    if (!star.IsInAxisRange())
                    {
                        Debug.LogWarning($"Star with starHeadT: {star.starHeadT} is out of Axis range!");
                    }

                    starIndex++;
                }
            }
            else
            {
                Debug.LogError("无法加载star预制体！");
            }
        }
    }

    private GameObject CreateHoldQuad(float startXMinWorld, float startXMaxWorld, float endXMinWorld, float endXMaxWorld, 
        float startY, float endY, float zPositionForStartT, float zPositionForEndT, Sprite sprite, string objectName, GameObject parentObject)
    {
        //Hold的Y轴坐标需要上移一点，以显示在JudgePlane上方
        //startY += 0.05f;
        //endY += 0.05f;
        //注意四边形顶点顺序
        Vector3 point1 = new Vector3(-startXMinWorld, startY, zPositionForStartT);
        Vector3 point2 = new Vector3(-endXMinWorld, endY, zPositionForEndT);
        Vector3 point3 = new Vector3(-endXMaxWorld, endY, zPositionForEndT);
        Vector3 point4 = new Vector3(-startXMaxWorld, startY, zPositionForStartT);

        // 假设CreateQuadFromPoints.CreateQuad方法直接返回游戏物体实例
        return CreateQuadFromPoints.CreateQuad(point1, point2, point3, point4, sprite, objectName, parentObject);
    }

    // 封装计算Z轴坐标的方法（参考InstantiateJudgePlanesAndJudgeLines方法里的逻辑，根据实际情况传入对应的开始时间等参数）
    private float CalculateZAxisPosition(float startTime)
    {
        // 假设存在SpeedParams.NoteSpeedDefault这个速度参数，你需根据实际情况调整
        return -startTime * SpeedParams.NoteSpeedDefault;
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
    //根据judgePlane的id找到对应的JudgePlane
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
}