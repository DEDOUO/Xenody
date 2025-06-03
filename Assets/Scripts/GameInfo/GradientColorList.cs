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
    [JsonProperty("Lcolor")]
    public string lowercolor;
    [JsonProperty("Ucolor")]
    public string uppercolor;
}

public class GradientColorListUnity
{

    public List<GradientColorUnity> colors;
    public class GradientColorUnity
    {
        public float startT;
        public float endT;
        public Color lowercolor;
        public Color uppercolor;
    }

    public GradientColorListUnity()
    {
        colors = new List<GradientColorUnity>();
    }

    // 新增方法，用于返回在时间戳t和y轴坐标y的颜色
    public Color GetColorAtTimeAndY(float t, float y)
    {
        foreach (var color in colors)
        {
            // 这里需要包含t<=0，即音乐还没开始播放时的状态
            if ( t<= 0 | (t >= color.startT && t <= color.endT))
            {
                if (y <= 0)
                {
                    // 当 y 小于等于 0 时，返回 lowercolor
                    return color.lowercolor;
                }
                else if (y >= 0 && y <= 1)
                {
                    // 当 y 在 0 到 1 之间时，返回 lowercolor 到 uppercolor 的渐变色
                    return Color.Lerp(color.lowercolor, color.uppercolor, y);
                }
                else if (y > 1 && y <= 1.5)
                {
                    // 当 y 在 1 到 1.5 之间时，返回 uppercolor 并线性减少透明度
                    float alpha = Mathf.Max(color.uppercolor.a - (y - 1) * 1f, 0);
                    Color result = color.uppercolor;
                    result.a = alpha;
                    return result;
                }
            }
        }
        // 如果 t 不在范围内，可根据实际情况处理，这里简单返回黑色
        return Color.black;
    }



}