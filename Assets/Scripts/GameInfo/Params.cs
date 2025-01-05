//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace Params
{
    // 将类名修改为更符合规范的单数形式，同时设置为静态类，方便直接访问其属性，无需实例化
    public static class ChartParams
    {
        // 谱面参数
        // X轴坐标从 -2 到 2
        public static float XaxisMin = -2;
        public static float XaxisMax = 2;

        // Y轴坐标从 0 到 1
        public static float YaxisMin = 0;
        public static float YaxisMax = 1;

        //StarHead默认的X轴坐标宽度
        public static float StarHeadXAxis = 0.4f;
    }

    public static class SpeedParams
    {
        // 速度参数
        // 1秒钟Note在世界坐标前进的距离（Z轴）
        public static float NoteSpeedDefault = 50f;
    }

    public static class HeightParams
    {
        // 天线在世界中的实际高度（Y轴）
        public static float HeightDefault = 6f;
    }
    public static class HorizontalParams
    {
        // 判定区大小参数，距离屏幕边缘的水平边距
        public static float HorizontalMargin = 0.1f;
    }
    public static class FinenessParams
    {
        // 模拟Hold/JudgePlane曲面时，用几个斜面代替曲面
        public static int Segment = 8;
    }
    public static class StarArrowParams
    {
        // 箭头的默认缩放比例
        public static float defaultScale = 20.0f;
        // 每单位长度的 SubArrow 数量
        public static int subArrowsPerUnitLength = 8; 
    }
}
