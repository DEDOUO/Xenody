using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
//using static Utility;
using Params;
//using System.Numerics;

namespace Note
{
    // 星星（Star）类
    public class Star
    {
        [JsonProperty("Pid")]
        // 与判定面相关联的标识（通过这个id后续去查找对应的JudgePlane实例）
        public int associatedPlaneId;
        [JsonProperty("id")]
        // 星星的id，用于分辨同一时间戳的不同星星
        public int starId;
        [JsonProperty("headT")]
        // 星星头（Star Head）的激活时间
        public float starHeadT;
        [JsonProperty("sub")]
        // 用于存储子星星（subStar）参数的列表
        public List<SubStar> subStarList = new List<SubStar>();

        // 内部类，用于表示子星星（subStar）的参数结构
        public class SubStar
        {
            [JsonProperty("startT")]
            public float starTrackStartT;
            [JsonProperty("endT")]
            public float starTrackEndT;
            [JsonProperty("startX")] // 原startPosition的X坐标分量
            public float startX;
            [JsonProperty("startY")] // 原startPosition的Y坐标分量
            public float startY;
            [JsonProperty("endX")] // 原endPosition的X坐标分量
            public float endX;
            [JsonProperty("endY")] // 原endPosition的Y坐标分量
            public float endY;
            [JsonProperty("Func")]
            public Utility.TrackFunctionType trackFunction;

            public SubStar(float startT, float endT, float startXVal, float startYVal, float endXVal, float endYVal, Utility.TrackFunctionType func)
            {
                starTrackStartT = startT;
                starTrackEndT = endT;
                startX = startXVal;
                startY = startYVal;
                endX = endXVal;
                endY = endYVal;
                trackFunction = func;
            }

            // 方法用于检查子星星坐标是否在规定的X轴和Y轴坐标范围内，适配新的坐标存储方式
            public bool IsInAxisRange()
            {
                return startX >= ChartParams.XaxisMin && startX <= ChartParams.XaxisMax
                       && startY >= ChartParams.YaxisMin && startY <= ChartParams.YaxisMax
                       && endX >= ChartParams.XaxisMin && endX <= ChartParams.XaxisMax
                       && endY >= ChartParams.YaxisMin && endY <= ChartParams.XaxisMax;
            }

        }

        // 方法用于向子星星参数列表中添加子星星的参数，并检查添加是否合法，适配新的坐标传入方式
        public void AddSubStar(float starTrackStartT, float starTrackEndT, float startX, float startY, float endX, float endY, Utility.TrackFunctionType trackFunction)
        {
            if (subStarList.Count > 0)
            {
                var lastSubStar = subStarList[subStarList.Count - 1];
                if (starTrackStartT != lastSubStar.starTrackEndT)
                {
                    Debug.LogError("添加的子星星时间戳不连续，无法添加。");
                    return;
                }
                // 检查起始坐标与上一个子星星结束坐标是否一致，适配新的坐标存储方式
                if (startX != lastSubStar.endX || startY != lastSubStar.endY)
                {
                    Debug.LogError("添加的子星星起始坐标与上一个子星星结束坐标不一致，无法添加。");
                    return;
                }
            }

            var newSubStar = new SubStar(starTrackStartT, starTrackEndT, startX, startY, endX, endY, trackFunction);
            if (!newSubStar.IsInAxisRange())
            {
                Debug.LogError("添加的子星星坐标超出范围，无法添加。");
                return;
            }

            subStarList.Add(newSubStar);
        }

        // 获取子星星列表，方便外部访问（例如序列化时需要）
        public List<SubStar> GetSubStarList()
        {
            return subStarList;
        }

        // 新增的方法：获取第一个subStar的X轴和Y轴坐标
        public Vector2 GetFirstSubStarCoordinates()
        {
            if (subStarList.Count > 0)
            {
                var firstSubStar = subStarList[0];
                return new Vector2(firstSubStar.startX, firstSubStar.startY);
            }
            else
            {
                // 如果没有子星星，返回零向量
                return Vector2.zero;
            }
        }
        public bool IsInAxisRange()
        {
            foreach (var subStar in subStarList)
            {
                if (!subStar.IsInAxisRange())
                {
                    return false;
                }
            }
            return true;
        }
        public static void SetArrowAlpha(GameObject arrow, float alpha)
        {
            SpriteRenderer arrowSpriteRenderer = arrow.GetComponent<SpriteRenderer>();
            if (arrowSpriteRenderer != null)
            {
                Color color = arrowSpriteRenderer.color;
                color.a = alpha;
                arrowSpriteRenderer.color = color;
            }
        }

    }
}
