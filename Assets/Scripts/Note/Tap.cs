using UnityEngine;
using Newtonsoft.Json;
using Params;

namespace Note
{
    // 判定面上的点键类
    public class Tap
    {
        [JsonProperty("startT")]
        // 点键的开始时间
        public float startT;
        [JsonProperty("startX")]
        // 点键自身的X轴坐标
        public float startX;
        [JsonProperty("noteSize")]
        // 点键的大小（可用于碰撞检测范围等设置，根据实际需求调整）
        public float noteSize;
        [JsonProperty("associatedPlane")]
        // 与判定面相关联的引用（在序列化时这里只会记录相关标识，反序列化后需要重新关联，后面会处理）
        public JudgePlane associatedPlane;

        // 方法用于检查点键是否在规定的X轴坐标范围内（避免越界）
        public bool IsInXAxisRange()
        {
            float halfNoteSize = noteSize / 2;
            return startX - halfNoteSize >= ChartParams.XaxisMin && startX + halfNoteSize <= ChartParams.XaxisMax;
        }
    }
}
