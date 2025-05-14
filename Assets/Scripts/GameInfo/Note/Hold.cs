using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Params;

namespace Note
{
    // 持续按键（Hold）类，代表整个Hold，由多个子Hold组成
    public class Hold
    {
        [JsonProperty("Pid")]
        // 与判定面相关联的标识（通过这个id后续去查找对应的JudgePlane实例）
        public int associatedPlaneId;
        [JsonProperty("id")]
        // Hold的id，用于分辨同一时间戳的不同Hold
        public int holdId;
        [JsonProperty("sub")]
        // 用于存储子Hold参数的列表
        public List<SubHold> subHoldList = new();
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
            [JsonProperty("LFunc")]
            public Utility.TransFunctionType XLeftFunction;
            [JsonProperty("RFunc")]
            public Utility.TransFunctionType XRightFunction;
            [JsonProperty("Jagnum")]
            public int Jagnum;

            public SubHold(float startTime, float startXMinVal, float startXMaxVal, float endTime, float endXMinVal,
                           float endXMaxVal, Utility.TransFunctionType XLeftFunc, Utility.TransFunctionType XRightFunc, int jagnum)
            {
                startT = startTime;
                startXMin = startXMinVal;
                startXMax = startXMaxVal;
                endT = endTime;
                endXMin = endXMinVal;
                endXMax = endXMaxVal;
                XLeftFunction = XLeftFunc;
                XRightFunction = XRightFunc;
                Jagnum = jagnum;
            }
            public bool IsInXAxisRange()
            {
                //可能要修改逻辑，需要确保每个子HoldX轴坐标都位于起止坐标轴之间
                return startXMin >= ChartParams.XaxisMin && endXMin >= ChartParams.XaxisMin && startXMax <= ChartParams.XaxisMax && endXMax <= ChartParams.XaxisMax;
            }

        }

        // 方法用于向子Hold参数列表中添加子Hold的参数，并检查添加是否合法
        public void AddSubHold(float startTime, float startXMin, float startXMax, float endTime, float endXMin,
                               float endXMax, Utility.TransFunctionType startXFunction, Utility.TransFunctionType endXFunction, int jagnum)
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
            subHoldList.Add(new SubHold(startTime, startXMin, startXMax, endTime, endXMin, endXMax, startXFunction, endXFunction, jagnum));
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

        public float GetFirstSubHoldStartTime()
        {
            if (subHoldList.Count > 0)
            {
                return subHoldList[0].startT;
            }
            Debug.LogError("Hold中没有子Hold，无法获取第一个SubHold的startT。");
            return -1; // 返回一个表示错误的值，可根据实际情况调整
        }

        public float GetFirstSubHoldStartX()
        {
            if (subHoldList.Count > 0)
            {
                return (subHoldList[0].startXMin + subHoldList[0].startXMax)/2;
            }
            Debug.LogError("Hold中没有子Hold，无法获取第一个SubHold的startT。");
            return -1; // 返回一个表示错误的值，可根据实际情况调整
        }


        public float GetLastSubHoldEndTime()
        {
            if (subHoldList.Count > 0)
            {
                return subHoldList[subHoldList.Count - 1].endT;
            }
            Debug.LogError("Hold中没有子Hold，无法获取最后一个SubHold的endT。");
            return -1; // 返回一个表示错误的值，可根据实际情况调整
        }

        public float GetLastSubHoldEndX()
        {
            if (subHoldList.Count > 0)
            {
                return (subHoldList[subHoldList.Count - 1].endXMin + subHoldList[subHoldList.Count - 1].endXMax) / 2;
            }
            Debug.LogError("Hold中没有子Hold，无法获取最后一个SubHold的startT。");
            return -1; // 返回一个表示错误的值，可根据实际情况调整
        }

        // 根据currentTime获取当前SubHold的左侧X轴坐标
        public float GetCurrentSubHoldLeftX(float currentTime)
        {
            foreach (SubHold subHold in subHoldList)
            {
                if (currentTime >= subHold.startT && currentTime <= subHold.endT)
                {
                    return Utility.CalculatePosition(currentTime, subHold.startT, subHold.startXMin, subHold.endT, subHold.endXMin, subHold.XLeftFunction);
                }
            }
            // 如果没有找到对应的SubHold，根据你的需求返回合适的值，这里返回0
            return 0;
        }

        // 根据currentTime获取当前SubHold的右侧X轴坐标
        public float GetCurrentSubHoldRightX(float currentTime)
        {
            foreach (SubHold subHold in subHoldList)
            {
                if (currentTime >= subHold.startT && currentTime <= subHold.endT)
                {
                    return Utility.CalculatePosition(currentTime, subHold.startT, subHold.startXMax, subHold.endT, subHold.endXMax, subHold.XRightFunction);
                }
            }
            // 如果没有找到对应的SubHold，根据你的需求返回合适的值，这里返回0
            return 0;
        }

    }
}
