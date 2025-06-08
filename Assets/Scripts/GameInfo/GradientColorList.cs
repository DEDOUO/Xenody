using Newtonsoft.Json;
using UnityEngine;
using static Utility;
using System.Collections.Generic;
using System.Linq;

public class GradientColor
{
    [JsonProperty("startT")]
    public float startT;
    [JsonProperty("endT")]
    public float endT;
    // 从startT到endT的速度（相对基准速度的倍率）
    [JsonProperty("StartLcolor")]
    public string Startlowercolor;
    [JsonProperty("StartUcolor")]
    public string Startuppercolor;
    [JsonProperty("EndLcolor")]
    public string Endlowercolor;
    [JsonProperty("EndUcolor")]
    public string Enduppercolor;
}

public class GradientColorListUnity
{

    public List<GradientColorUnity> colors;
    public class GradientColorUnity
    {
        public float startT;
        public float endT;
        public Color Startlowercolor;
        public Color Startuppercolor;
        public Color Endlowercolor;
        public Color Enduppercolor;
        //是否需要进行时间插值（开始时间和结束时间颜色是否一致，一致就不要插值）
        public bool isTimeInterpolationNeeded;
    }

    public GradientColorListUnity()
    {
        colors = new List<GradientColorUnity>();
    }

    public Color GetColorAtTimeAndY(float t, float y)
    {

        foreach (var color in colors)
        {

            // 判断t是否在当前时间窗口内
            if (t <  0 || (t >= color.startT && t <= color.endT))
            {
                float timeRate = (t - color.startT) / (color.endT - color.startT);

                Color lowerColor, upperColor;
                if (color.isTimeInterpolationNeeded)
                {
                    // 计算底端和顶端的时间插值颜色
                    lowerColor = Color.Lerp(color.Startlowercolor, color.Endlowercolor, timeRate);
                    upperColor = Color.Lerp(color.Startuppercolor, color.Enduppercolor, timeRate);
                }
                else
                {
                    // 时间窗内颜色固定，使用起始颜色
                    lowerColor = color.Startlowercolor;
                    upperColor = color.Startuppercolor;
                }

                // 判断底端和顶端颜色是否一致
                if (lowerColor == upperColor)
                {
                    // 颜色一致时直接使用底端颜色
                    return lowerColor;
                }
                else
                {
                    // 根据y轴坐标计算最终颜色
                    return GetColorByY(lowerColor, upperColor, y);
                }
            }
        }

        // 默认返回黑色（可自定义默认逻辑）
        return Color.black;
    }

    // 分离y轴颜色计算逻辑
    private Color GetColorByY(Color lowerColor, Color upperColor, float y)
    {
        if (y <= 0)
        {
            return lowerColor;
        }
        else if (y >= 0 && y <= 1)
        {
            return Color.Lerp(lowerColor, upperColor, y);
        }
        else if (y > 1 && y <= 1.5)
        {
            float alpha = Mathf.Max(upperColor.a - (y - 1) * 1f, 0);
            Color result = upperColor;
            result.a = alpha;
            return result;
        }
        else
        {
            // 超出范围时使用顶端颜色（透明度-0.5）
            float alpha = Mathf.Max(upperColor.a - 0.5f, 0);
            Color result = upperColor;
            result.a = alpha;
            return result;
        }
    }

    public static GradientColorListUnity ConvertToUnityList(List<GradientColor> list)
    {
        GradientColorListUnity unityList = new GradientColorListUnity();
        foreach (var gradientColor in list)
        {
            // 解析颜色（确保End颜色存在时使用End颜色，否则使用Start颜色）
            Color startLowerColor = HexToColor(gradientColor.Startlowercolor);
            Color startUpperColor = HexToColor(gradientColor.Startuppercolor);
            Color endLowerColor = string.IsNullOrEmpty(gradientColor.Endlowercolor)
                ? startLowerColor
                : HexToColor(gradientColor.Endlowercolor);
            Color endUpperColor = string.IsNullOrEmpty(gradientColor.Enduppercolor)
                ? startUpperColor
                : HexToColor(gradientColor.Enduppercolor);

            // 计算是否需要时间插值
            bool isTimeInterpolation = !(endLowerColor == startLowerColor && endUpperColor == startUpperColor);

            unityList.colors.Add(new GradientColorUnity
            {
                startT = gradientColor.startT,
                endT = gradientColor.endT,
                Startlowercolor = startLowerColor,
                Startuppercolor = startUpperColor,
                Endlowercolor = endLowerColor,
                Enduppercolor = endUpperColor,
                isTimeInterpolationNeeded = isTimeInterpolation
            });
        }
        return unityList;
    }

    //public static Color HexToColorNew(string start, string end)
    //{
    //    if (end == null)
    //    {
    //        return HexToColor(start);
    //    }
    //    else 
    //    { 
    //        return HexToColor(end);
    //    }

    //}


}