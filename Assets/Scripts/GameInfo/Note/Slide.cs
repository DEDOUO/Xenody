using UnityEngine;
using Newtonsoft.Json;
using Params;
using static Utility;

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
        [JsonProperty("Size")]
        // 新增滑动键的大小
        public float noteSize;
        [JsonProperty("Pid")]
        // 与判定面相关联的标识（通过这个id后续去查找对应的JudgePlane实例）
        public int associatedPlaneId;

        //Y轴坐标
        public float startY;

        // 方法用于检查滑动键是否在规定的X轴坐标范围内（结合新的参数类来判断）
        public bool IsInXAxisRange()
        {
            return Utility.IsInXAxisRange(noteSize, startX);
        }
    }
}

