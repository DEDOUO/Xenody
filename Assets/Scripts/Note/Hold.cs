using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Params;

namespace Note
{
    // 持续按键（Hold）类，代表整个Hold，由多个子Hold组成
    public class Hold
    {
        [JsonProperty("subHoldList")]
        // 用于存储子Hold参数的列表
        public List<SubHold> subHoldList = new();
        [JsonProperty("associatedPlane")]
        // 与判定面相关联的标识（通过这个id后续去查找对应的JudgePlane实例）
        public int associatedPlaneId;

        // 内部类，用于表示子Hold的参数结构
        public class SubHold
        {
            [JsonProperty("startT")]
            public float startT;
            [JsonProperty("startXMin")]
            public float startXMin;
            [JsonProperty("startXMax")]
            public float startXMax;
            [JsonProperty("endT")]
            public float endT;
            [JsonProperty("endXMin")]
            public float endXMin;
            [JsonProperty("endXMax")]
            public float endXMax;
            [JsonProperty("XLeftFunction")]
            public Utility.TransFunctionType XLeftFunction;
            [JsonProperty("XRightFunction")]
            public Utility.TransFunctionType XRightFunction;

            public SubHold(float startTime, float startXMinVal, float startXMaxVal, float endTime, float endXMinVal,
                           float endXMaxVal, Utility.TransFunctionType XLeftFunc, Utility.TransFunctionType XRightFunc)
            {
                startT = startTime;
                startXMin = startXMinVal;
                startXMax = startXMaxVal;
                endT = endTime;
                endXMin = endXMinVal;
                endXMax = endXMaxVal;
                XLeftFunction = XLeftFunc;
                XRightFunction = XRightFunc;
            }
            public bool IsInXAxisRange()
            {
                //可能要修改逻辑，需要确保每个子HoldX轴坐标都位于起止坐标轴之间
                return startXMin >= ChartParams.XaxisMin && endXMin >= ChartParams.XaxisMin && startXMax <= ChartParams.XaxisMax && endXMax <= ChartParams.XaxisMax;
            }

        }

        // 方法用于向子Hold参数列表中添加子Hold的参数，并检查添加是否合法
        public void AddSubHold(float startTime, float startXMin, float startXMax, float endTime, float endXMin,
                               float endXMax, Utility.TransFunctionType startXFunction, Utility.TransFunctionType endXFunction)
        {
            if (subHoldList.Count > 0)
            {
                var lastSubHold = subHoldList[subHoldList.Count - 1];
                if (startTime != lastSubHold.endT)
                {
                    Debug.LogError("添加的子Hold时间戳不连续，无法添加。");
                    return;
                }
                if (startXMax < lastSubHold.endXMin || startXMin > lastSubHold.endXMax)
                {
                    Debug.LogError("添加的子Hold在X轴上与上一个子Hold无重叠，无法添加。");
                    return;
                }
            }
            subHoldList.Add(new SubHold(startTime, startXMin, startXMax, endTime, endXMin, endXMax, startXFunction, endXFunction));
        }

        // 获取子Hold列表，方便外部访问（例如序列化时需要）
        public List<SubHold> GetSubHoldList()
        {
            return subHoldList;
        }
        public bool IsInXAxisRange()
        {
            foreach (SubHold subhold in subHoldList)
            {
                if (!subhold.IsInXAxisRange())
                {
                    return false;
                }
            }
            return true;
        }
    }
}
