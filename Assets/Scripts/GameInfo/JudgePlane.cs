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
    [JsonProperty("color")]
    // 判定面的唯一标识，按照要求改为数字编号
    public string color;
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
    public JudgePlane(int Id, string uniqueColor)
    {
        id = Id;
        color = uniqueColor;
    }

    // 方法用于向子判定面参数列表中添加子判定面的参数，并检查添加是否合法
    public void AddSubJudgePlane(float startTime, float startY, float endTime, float endY, TransFunctionType yAxisFunction)
    {
        if (subJudgePlaneList.Count > 0 && startTime != subJudgePlaneList[subJudgePlaneList.Count - 1].endT)
        {
            Debug.LogError("添加的子判定面时间戳不连续，无法添加。"+ startTime);
            return;
        }
        if (startY < ChartParams.YaxisMin || startY > ChartParams.YaxisMax || endY < ChartParams.YaxisMin || endY > ChartParams.YaxisMax)
        {
            //Debug.LogError("子判定面的Y轴坐标超出范围，无法添加。");
            //return;
        }
        subJudgePlaneList.Add(new SubJudgePlane(startTime, startY, endTime, endY, yAxisFunction));
    }

    // 获取子判定面列表，方便外部访问（例如序列化时需要）
    public List<SubJudgePlane> GetSubJudgePlaneList()
    {
        return subJudgePlaneList;
    }

    // 根据当前时间更新判定面的整体Y轴坐标，使其各子判定面按设定的函数变化
    public float GetPlaneYAxis(float currentTime)
    {
        //float minY = 0f;
        //float maxY = HeightParams.HeightDefault;
        foreach (SubJudgePlane subPlane in subJudgePlaneList)
        {
            if (currentTime >= subPlane.startT && currentTime <= subPlane.endT)
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


    public bool IsSubJudgePlaneLinear(float startT, float endT)
    {
        // 这里需要根据 judgePlane 的 SubJudgePlane 信息，判断 startT 到 endT 之间是否均为 Linear 函数类型
        // 假设 judgePlane 有一个方法可以获取 SubJudgePlane 列表，并且每个 SubJudgePlane 有一个方法可以获取其函数类型
        foreach (var subJudgePlane in subJudgePlaneList)
        {
            float subStartT = subJudgePlane.startT;
            float subEndT = subJudgePlane.endT;
            if (subStartT <= endT && subEndT >= startT)
            {
                if (subJudgePlane.yAxisFunction != Utility.TransFunctionType.Linear)
                {
                    return false;
                }
            }
        }
        return true;
    }

    // 简单示例方法，获取JudgePlane实例对应的游戏物体
    public GameObject GetJudgePlaneGameObject(GameObject JudgePlanesParent)
    {
        string judgePlaneObjectName = $"JudgePlane{id}";
        foreach (Transform child in JudgePlanesParent.transform)
        {
            if (child.gameObject.name == judgePlaneObjectName)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    //方法用于根据给定的Y轴坐标值改变对应JudgePlane下所有SubJudgePlane实例的透明度
    public void ChangeSubJudgePlaneTransparency(GameObject JudgePlanesParent, float yAxisValue)
    {

        // 计算透明度差值，根据给定的对应关系计算斜率
        float alphaDelta = (AlphaParams.JudgePlaneAlphaMin - AlphaParams.JudgePlaneAlphaMax) / HeightParams.HeightDefault;
        //Debug.Log(alphaDelta);
        // 根据线性关系计算当前透明度值，确保透明度范围在0到1之间
        float currentAlpha = Mathf.Clamp(AlphaParams.JudgePlaneAlphaMax + alphaDelta * yAxisValue, 0, 1);
        //Debug.Log(currentAlpha);
        // 获取JudgePlane对应的游戏物体（假设其命名规则是"JudgePlane + id"，可根据实际调整获取方式）
        GameObject judgePlaneObject = GetJudgePlaneGameObject(JudgePlanesParent);
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

}

