using System;
//using System.Numerics;
using UnityEngine;
using Note;
//using static Utility;

public class Utility
{
    // 定义X轴/Y轴坐标变化函数的枚举类型
    //适用JudgePlane和Hold类
    public enum TransFunctionType { Linear, Sin, Cos }

    // 定义星星坐标变化函数的枚举类型
    //适用Star类
    public enum TrackFunctionType { Linear, Sin, Cos, Circular }

    /// <summary>
    /// 根据给定的时间、起始值、结束值以及坐标变化函数类型来计算相应的位置值。
    /// 可以用于处理如游戏中物体在某个时间段内按照不同函数规律进行位置变化的情况。
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="startTime">起始时间</param>
    /// <param name="startVal">起始值（如起始坐标等）</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="endVal">结束值（如结束坐标等）</param>
    /// <param name="functionType">坐标变化函数类型（线性、正弦、余弦）</param>
    /// <returns>计算得到的位置值</returns>
    /// 
    public static float CalculatePosition(float currentTime, float startTime, float startVal, float endTime, float endVal, TransFunctionType functionType)
    {
        if (startTime > endTime)
        {
            throw new ArgumentException("起始时间不能大于结束时间", "startTime");
        }

        if (!Enum.IsDefined(typeof(TransFunctionType), functionType))
        {
            throw new ArgumentException("传入的坐标变化函数类型无效", "functionType");
        }

        float result = 0f;

        switch (functionType)
        {
            case TransFunctionType.Linear:
                // 线性函数计算当前位置
                result = startVal + ((endVal - startVal) * ((currentTime - startTime) / (endTime - startTime)));
                break;
            case TransFunctionType.Sin:
                // 正弦函数计算当前位置，这里假设周期为整个子判定面持续时间
                float period = endTime - startTime;
                result = startVal + ((endVal - startVal) / 2) * (1 + Mathf.Sin((2 * Mathf.PI * (currentTime - startTime)) / period));
                break;
            case TransFunctionType.Cos:
                // 余弦函数计算当前位置，同样假设周期为整个子判定面持续时间
                period = endTime - startTime;
                result = startVal + ((endVal - startVal) / 2) * (1 + Mathf.Cos((2 * Mathf.PI * (currentTime - startTime)) / period));
                break;
        }

        return result;
    }

    // 根据给定时间和子星星参数计算当前子星星的位置
    public static Vector2 CalculatePosition(float currentTime, Star.SubStar subStar)
    {
        Vector2 result = Vector2.zero;

        switch (subStar.trackFunction)
        {
            case TrackFunctionType.Linear:
                // 线性函数计算当前位置
                result.x = subStar.startX + ((subStar.endX - subStar.startX) * ((currentTime - subStar.starTrackStartT) / (subStar.starTrackEndT - subStar.starTrackStartT)));
                result.y = subStar.startY + ((subStar.endY - subStar.startY) * ((currentTime - subStar.starTrackStartT) / (subStar.starTrackEndT - subStar.starTrackStartT)));
                break;
            case TrackFunctionType.Sin:
                // 正弦函数计算当前位置，这里假设周期为整个子星星轨道持续时间
                float periodX = subStar.starTrackEndT - subStar.starTrackStartT;
                float periodY = subStar.starTrackEndT - subStar.starTrackStartT;
                result.x = subStar.startX + ((subStar.endX - subStar.startX) / 2) * (1 + Mathf.Sin((2 * Mathf.PI * (currentTime - subStar.starTrackStartT)) / periodX));
                result.y = subStar.startY + ((subStar.endY - subStar.startY) / 2) * (1 + Mathf.Sin((2 * Mathf.PI * (currentTime - subStar.starTrackStartT)) / periodY));
                break;
            case TrackFunctionType.Cos:
                // 余弦函数计算当前位置，同样假设周期为整个子星星轨道持续时间
                periodX = subStar.starTrackEndT - subStar.starTrackStartT;
                periodY = subStar.starTrackEndT - subStar.starTrackStartT;
                result.x = subStar.startX + ((subStar.endX - subStar.startX) / 2) * (1 + Mathf.Cos((2 * Mathf.PI * (currentTime - subStar.starTrackStartT)) / periodX));
                result.y = subStar.startY + ((subStar.endY - subStar.startY) / 2) * (1 + Mathf.Cos((2 * Mathf.PI * (currentTime - subStar.starTrackStartT)) / periodY));
                break;
            case TrackFunctionType.Circular:
                // 圆形轨道函数计算当前位置（简单示例，假设绕圆心顺时针匀速运动，圆心为起始位置，可根据实际调整完善）
                float angle = ((currentTime - subStar.starTrackStartT) / (subStar.starTrackEndT - subStar.starTrackStartT)) * 2 * Mathf.PI;
                result.x = subStar.startX + Mathf.Cos(angle) * (subStar.endX - subStar.startX);
                result.y = subStar.startY + Mathf.Sin(angle) * (subStar.endY - subStar.startY);
                break;
        }

        return result;
    }
}


