using UnityEngine;
using System.Collections.Generic;
using static Utility;
using Newtonsoft.Json;
//using System.Drawing;
//using System.Numerics;
using Params;

// 判定面类
public class JudgePlane
{
    [JsonProperty("id")]
    // 判定面的唯一标识，按照要求改为数字编号
    public int id;
    [JsonProperty("subJudgePlanes")]
    // 用于存储子判定面参数的列表
    public List<SubJudgePlane> subJudgePlaneList = new List<SubJudgePlane>();

    // 内部类，用于表示子判定面的参数结构
    public class SubJudgePlane
    {
        [JsonProperty("startT")]
        public float startT;
        [JsonProperty("startY")]
        public float startY;
        [JsonProperty("endT")]
        public float endT;
        [JsonProperty("endY")]
        public float endY;
        [JsonProperty("yAxisFunction")]
        public TransFunctionType yAxisFunction;

        public SubJudgePlane(float startTime, float startYVal, float endTime, float endYVal, TransFunctionType func)
        {
            startT = startTime;
            startY = startYVal;
            endT = endTime;
            endY = endYVal;
            yAxisFunction = func;
        }
    }

    // 构造函数，用于初始化时设置唯一标识
    public JudgePlane(int uniqueId)
    {
        id = uniqueId;
    }

    // 方法用于向子判定面参数列表中添加子判定面的参数，并检查添加是否合法
    public void AddSubJudgePlane(float startTime, float startY, float endTime, float endY, TransFunctionType yAxisFunction)
    {
        if (subJudgePlaneList.Count > 0 && startTime != subJudgePlaneList[subJudgePlaneList.Count - 1].endT)
        {
            Debug.LogError("添加的子判定面时间戳不连续，无法添加。");
            return;
        }
        if (startY < ChartParams.YaxisMin || startY > ChartParams.YaxisMax || endY < ChartParams.YaxisMin || endY > ChartParams.YaxisMax)
        {
            Debug.LogError("子判定面的Y轴坐标超出范围，无法添加。");
            return;
        }
        subJudgePlaneList.Add(new SubJudgePlane(startTime, startY, endTime, endY, yAxisFunction));
    }

    // 获取子判定面列表，方便外部访问（例如序列化时需要）
    public List<SubJudgePlane> GetSubJudgePlaneList()
    {
        return subJudgePlaneList;
    }

    // 根据给定的参数列表初始化判定面实例，创建可视化的平面游戏对象并设置其属性
    //public void Initialize()
    //{
    //    // 创建一个平面游戏对象来表示判定面
    //    planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
    //    planeRenderer = planeObject.GetComponent<MeshRenderer>();

    //    // 设置平面的一些属性，比如颜色等（这里简单设置为半透明灰色，可根据美术需求调整）
    //    planeRenderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    //    // 初始时先将平面的位置和缩放设置为默认值，后续根据子判定面参数更新
    //    planeObject.transform.position = Vector3.zero;
    //    planeObject.transform.localScale = Vector3.one;
    //}

    // 根据当前时间更新判定面的整体Y轴坐标，使其各子判定面按设定的函数变化
    public void UpdatePlaneYAxis(float currentTime)
    {
        float minY = Mathf.Infinity;
        float maxY = -Mathf.Infinity;

        foreach (SubJudgePlane subPlane in subJudgePlaneList)
        {
            if (currentTime >= subPlane.startT && currentTime <= subPlane.endT)
            {
                // 根据Y轴变化函数类型计算当前子判定面的Y轴坐标范围
                float subStartY = CalculateYAxisPosition(currentTime, subPlane.startT, subPlane.startY, subPlane.endT, subPlane.endY, subPlane.yAxisFunction);
                float subEndY = CalculateYAxisPosition(currentTime, subPlane.startT, subPlane.startY + 1f, subPlane.endT, subPlane.endY + 1f, subPlane.yAxisFunction);
                minY = Mathf.Min(minY, subStartY);
                maxY = Mathf.Max(maxY, subEndY);
            }
        }

        // 更新平面的位置和缩放以反映Y轴坐标的变化
        //planeObject.transform.position = new Vector3(0f, (minY + maxY) / 2f, 0f);
        //planeObject.transform.localScale = new Vector3(10f, Mathf.Abs(maxY - minY), 10f);
    }

    // 计算指定子判定面在给定时间下根据不同函数类型的Y轴当前位置
    public static float CalculateYAxisPosition(float currentTime, float startTime, float startVal, float endTime, float endVal, Utility.TransFunctionType functionType)
    {
        return CalculatePosition(currentTime, startTime, startVal, endTime, endVal, functionType);
    }
}

