﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace Params
{
    // 将类名修改为更符合规范的单数形式，同时设置为静态类，方便直接访问其属性，无需实例化

    public static class AspectRatioParams
    {
        // 谱面长宽比参数（应该不用动吧……）
        public static float AspectRatioDefault = 16f / 9f;
    }

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
        public static float StarHeadXAxis = 0.8f;
        //Star默认的出现时间
        public static float StarAppearTime = 0.4f;

        //JudgeLine默认的出现时间
        public static float JudgeLineAppearTime = 0.4f;

        // 所有Note默认厚度缩放比例
        public static float ZAxisRate = 0.6f;
        public static float NoteThickness = 0.3f;

        // 所有Note默认开始出现Z轴坐标
        public static float NoteZAxisAppearPos = -100f;

        // 所有Note默认完全变为不透明Z轴坐标
        public static float NoteZAxisOpaquePos = -50f;

        // 所有3DNote默认Z轴偏移坐标（为NoteZ轴宽度的一半）
        public static float NoteZAxisOffset = -0.55f;

        public static float OutlineWidth = 0.15f;
    }

    public static class SpeedParams
    {
        // 速度参数
        // 1秒钟Note在世界坐标前进的距离（Z轴）
        public static float NoteSpeedDefault = 80f;
    }

    public static class OutlineParams
    {
        // 描边参数
        // Hold描边宽度
        //public static float HoldOutlineDefault = 0.4f;
        public static float HoldColorLineWidth = 0.2f;
    }

    //public static class HeightParams
    //{
    //    // 天线在世界中的实际高度（Y轴）
    //    public static float HeightDefault = 4.5f;
    //}

    public static class HorizontalParams
    {
        // 判定区大小参数，谱面左右距离判定区（固定为16:9）左/右边缘的水平边距
        public static float HorizontalMargin = 0.1f;

        // 判定区大小参数，谱面下方距离判定区（固定为16:9）下边缘的垂直边距
        public static float VerticalMarginBottom = 0.08f;
        // 判定区大小参数，谱面上方（Y轴谱面坐标=1）距离判定区（固定为16:9）下边缘的垂直边距
        public static float VerticalMarginCeiling = 0.45f;

        // 判定区大小参数，加上两侧亮条后，距离屏幕边缘的水平边距（两侧各留1%亮条宽度，也就是8%）
        public static float PlusHorizontalMargin = 0.08f;
    }
    public static class FinenessParams
    {
        // 模拟Hold/JudgePlane曲面时，用几个斜面代替曲面
        public static int Segment = 10;
    }
    public static class StarArrowParams
    {
        // 箭头的默认缩放比例
        public static float defaultScale = 30.0f;
        // 每单位长度的 SubArrow 数量
        public static int subArrowsPerUnitLength = 6;
    }
    public static class AlphaParams
    {
        // JudgePlane的最小和最大透明度
        public static float JudgePlaneAlphaMin = 0.5f;
        public static float JudgePlaneAlphaMax = 0.9f;
    }

    public static class SoundParams
    {
        // 星星划动结束后，音频音量线性地衰减为0的时间
        public static float starSoundFadeOutTime = 0.5f;
    }
    public static class FrameParams
    {
        // 控制Note位置更新和判定的帧率，默认120帧
        public static float updateInterval = 0.00833333f;
    }
}
