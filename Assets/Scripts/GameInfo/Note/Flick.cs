using UnityEngine;
using Newtonsoft.Json;
using Params;
//using System.Numerics;

namespace Note
{

    // 划键（Flick）类
    public class Flick
    {
        [JsonProperty("startT")]
        // 划键的开始时间
        public float startT;
        [JsonProperty("startX")]
        // 划键在X轴的坐标（这里简化为一个坐标，可根据实际情况调整具体含义，比如中心坐标等）
        public float startX;
        [JsonProperty("Size")]
        // 新增划键的大小
        public float noteSize;
        [JsonProperty("Dir")]
        // 用于记录划键操作的滑动方向（设置为0-1之间的值）
        public float flickDirection;
        [JsonProperty("Pid")]
        // 与判定面相关联的标识（通过这个id后续去查找对应的JudgePlane实例）
        public int associatedPlaneId;

        //Y轴坐标
        public float startY;


        // 检测玩家是否在正确的时间点击并向正确方向滑动了划键对应的判定区（仅针对移动端触摸检测）
        public bool IsFlickedCorrectly()
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                // 获取点击位置的世界坐标
                Vector3 clickPosition = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);

                // 判断点击位置是否在划键的坐标范围内以及是否在关联的判定面上
                //if (GetComponent<Collider>().bounds.Contains(clickPosition) && clickPosition.y >= associatedPlane.startY && clickPosition.y <= associatedPlane.endY)
                //{
                //    // 记录点击开始时的触摸位置
                //    Vector2 startTouchPosition = Input.GetTouch(0).position;
                //    // 等待触摸移动阶段，获取滑动后的触摸位置
                //    while (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
                //    {
                //        Vector2 endTouchPosition = Input.GetTouch(0).position;
                //        // 计算滑动方向向量
                //        flickDirectionVector = (endTouchPosition - startTouchPosition).normalized;
                //    }

                //    return Time.time >= startT && Time.time <= startT + 0.1f;
                //}
            }

            return false;
        }
        // 方法用于检查划键是否在规定的X轴坐标范围内（结合新的参数类来判断）
        public bool IsInXAxisRange()
        {
            return Utility.IsInXAxisRange(noteSize, startX);
        }
        public bool IsInFlickRange()
        {
            return flickDirection >= 0 && flickDirection <= 1;
        }

    }
}
