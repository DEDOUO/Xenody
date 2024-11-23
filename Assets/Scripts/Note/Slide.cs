using UnityEngine;
using Newtonsoft.Json;
using Params;
namespace Note
{
    // 滑动键（Slide）类
    public class Slide
    {
        [JsonProperty("startT")]
        // 滑动键的开始时间
        public float startT;
        [JsonProperty("startX")]
        // 滑动键在X轴的坐标（这里简化为一个坐标，可根据实际情况调整具体含义，比如中心坐标等）
        public float startX;
        [JsonProperty("noteSize")]
        // 新增滑动键的大小
        public float noteSize;
        [JsonProperty("associatedPlane")]
        // 与判定面相关联的引用（在序列化时这里只会记录相关标识，反序列化后需要重新关联，后面会处理）
        public JudgePlane associatedPlane;

        // 方法用于检查滑动键是否在规定的X轴坐标范围内（结合新的参数类来判断）
        public bool IsInXAxisRange()
        {
            float halfNoteSize = noteSize / 2;
            return startX - halfNoteSize >= ChartParams.XaxisMin && startX + halfNoteSize <= ChartParams.XaxisMax;
        }
    }
}

