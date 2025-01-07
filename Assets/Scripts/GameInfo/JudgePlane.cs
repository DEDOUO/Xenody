using UnityEngine;
using System.Collections.Generic;
using static Utility;
using Newtonsoft.Json;
//using System.Drawing;
//using System.Numerics;
using Params;
using static JudgePlane;
using System.Collections;

//不加这一行代码的话，读取Json会报错（因为JudgePlane实现了IEnumerable接口？）
[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
// 判定面类
public class JudgePlane : IEnumerable<SubJudgePlane>
{
    [JsonProperty("id")]
    // 判定面的唯一标识，按照要求改为数字编号
    public int id;
    [JsonProperty("sub")]
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
        [JsonProperty("Func")]
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
    public float GetPlaneYAxis(float currentTime)
    {
        //float minY = 0f;
        //float maxY = HeightParams.HeightDefault;
        foreach (SubJudgePlane subPlane in subJudgePlaneList)
        {
            if (currentTime >= subPlane.startT && currentTime < subPlane.endT)
            {
                // 根据Y轴变化函数类型计算当前子判定面的Y轴坐标范围
                float YAxisCoordinate = CalculateYAxisPosition(currentTime, subPlane.startT, subPlane.startY, subPlane.endT, subPlane.endY, subPlane.yAxisFunction);
                YAxisCoordinate *= HeightParams.HeightDefault;
                return YAxisCoordinate;
            }
        }
        return 0f;
    }

    // 计算指定子判定面在给定时间下根据不同函数类型的Y轴当前位置
    public static float CalculateYAxisPosition(float currentTime, float startTime, float startVal, float endTime, float endVal, TransFunctionType functionType)
    {
        return CalculatePosition(currentTime, startTime, startVal, endTime, endVal, functionType);
    }

    // 实现IEnumerable<SubJudgePlane>接口要求的GetEnumerator方法，返回一个用于遍历SubJudgePlane元素的枚举器
    public IEnumerator<SubJudgePlane> GetEnumerator()
    {
        return subJudgePlaneList.GetEnumerator();
    }

    // 这个方法是为了兼容非泛型的IEnumerable接口，一般情况下可以简单调用泛型的GetEnumerator方法
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

