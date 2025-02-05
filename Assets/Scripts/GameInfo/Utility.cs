using System;
//using System.Numerics;
using UnityEngine;
using Note;
using Params;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static Note.Star;
using System.Collections.Generic;
//using static Utility;


public class Utility : MonoBehaviour
{
    // 定义X轴/Y轴坐标变化函数的枚举类型
    //适用JudgePlane和Hold类
    public enum TransFunctionType { Linear, Sin, Cos }
    // 正弦函数(开头不平滑，结尾平滑)
    // 余弦函数(开头平滑，结尾不平滑)

    // 定义星星坐标变化函数的枚举类型
    //适用Star类
    //public enum TrackFunctionType { Linear, Sin, Cos, Circular }
    //星星暂时只支持线性和圆形（椭圆型）
    //sin和cos涉及曲线计算的部分，不能用初等函数表示，只能通过数值计算，暂时忽略
    public enum TrackFunctionType { Linear, UpperCir, LowerCir }

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
        float TimePeriod = endTime - startTime;
        float ValPeriod = endVal - startVal;
        float currentTimeStandard = (currentTime - startTime) / TimePeriod;

        switch (functionType)
        {
            case TransFunctionType.Linear:
                // 线性函数计算当前位置
                result = startVal + ValPeriod * currentTimeStandard;
                break;
            case TransFunctionType.Sin:
                // 正弦函数(开头不平滑，结尾平滑)
                //Debug.Log(1 / 2 * Mathf.PI * currentTimeStandard);
                //Debug.Log(Mathf.Sin(1 / 2 * Mathf.PI * currentTimeStandard));
                result = startVal + ValPeriod * Mathf.Sin(0.5f * Mathf.PI * currentTimeStandard);
                //Debug.Log(result);
                break;
            case TransFunctionType.Cos:
                // 余弦函数(开头平滑，结尾不平滑)
                result = startVal + ValPeriod * (1 - Mathf.Cos(0.5f * Mathf.PI * currentTimeStandard));
                break;
        }

        return result;
    }

    // 根据给定时间和子星星参数计算当前子星星的位置
    public static Vector2 CalculateSubStarPosition(float currentRate, Star.SubStar subStar)
    {
        Vector2 result = Vector2.zero;
        Vector2 offset = Vector2.zero;
        float a = 0f;
        float b = 0f;
        float theta = 0f;
        float k = 0f;
        float t = 0f;
        switch (subStar.trackFunction)
        {
            case TrackFunctionType.Linear:
                // 线性函数计算当前位置
                result.x = subStar.startX + ((subStar.endX - subStar.startX) * currentRate);
                result.y = subStar.startY + ((subStar.endY - subStar.startY) * currentRate);
                break;
            case TrackFunctionType.UpperCir:
                // UpperCir指向上凸起的圆弧（详见说明）
                a = Mathf.Abs(subStar.endX - subStar.startX);
                b = Mathf.Abs(subStar.endY - subStar.startY);
                if (subStar.startX < subStar.endX && subStar.startY < subStar.endY)
                {
                    theta = Mathf.PI - currentRate * 0.5f * Mathf.PI;
                    offset.x = subStar.endX;
                    offset.y = subStar.startY;
                }
                if (subStar.startX < subStar.endX && subStar.startY > subStar.endY)
                {
                    theta = 0.5f * Mathf.PI - currentRate * 0.5f * Mathf.PI;
                    offset.x = subStar.startX;
                    offset.y = subStar.endY;
                }
                if (subStar.startX > subStar.endX && subStar.startY < subStar.endY)
                {
                    theta = currentRate * 0.5f * Mathf.PI;
                    offset.x = subStar.endX;
                    offset.y = subStar.startY;
                }
                if (subStar.startX > subStar.endX && subStar.startY > subStar.endY)
                {
                    theta = 0.5f * Mathf.PI + currentRate * 0.5f * Mathf.PI;
                    offset.x = subStar.startX;
                    offset.y = subStar.endY;
                }
                k = Mathf.Tan(theta);
                t = Mathf.Atan(k * a / b);
                result.x = offset.x + a * Mathf.Cos(t);
                result.y = offset.y + b * Mathf.Sin(t);
                break;
            case TrackFunctionType.LowerCir:
                // LowerCir指向下凸起的圆弧（详见说明）
                a = Mathf.Abs(subStar.endX - subStar.startX);
                b = Mathf.Abs(subStar.endY - subStar.startY);
                if (subStar.startX < subStar.endX && subStar.startY < subStar.endY)
                {
                    theta = 1.5f * Mathf.PI + currentRate * 0.5f * Mathf.PI;
                    offset.x = subStar.startX;
                    offset.y = subStar.endY;
                }
                if (subStar.startX < subStar.endX && subStar.startY > subStar.endY)
                {
                    theta = Mathf.PI + currentRate * 0.5f * Mathf.PI;
                    offset.x = subStar.endX;
                    offset.y = subStar.startY;
                }
                if (subStar.startX > subStar.endX && subStar.startY < subStar.endY)
                {
                    theta = 1.5f * Mathf.PI - currentRate * 0.5f * Mathf.PI;
                    offset.x = subStar.startX;
                    offset.y = subStar.endY;
                }
                if (subStar.startX > subStar.endX && subStar.startY > subStar.endY)
                {
                    theta = -currentRate * 0.5f * Mathf.PI;
                    offset.x = subStar.endX;
                    offset.y = subStar.startY;
                }
                k = Mathf.Tan(theta);
                t = Mathf.Atan(k * a / b);
                result.x = offset.x + a * Mathf.Cos(t);
                result.y = offset.y + b * Mathf.Sin(t);
                break;
        }
        return result;
    }

    public static float ConvertAngle(float theta, float a, float b)
    {
        float k = Mathf.Tan(theta);
        float t = Mathf.Atan(k * a / b);

        // 判断 theta 所在的象限并进行调整
        if (theta >= 0 && theta < Mathf.PI / 2)
        {
            // 第一象限，t 本身就在正确区间，无需调整
            return t;
        }
        else if (theta >= Mathf.PI / 2 && theta <= Mathf.PI+0.01f)
        {
            // 第二象限，将 t 调整到第二象限
            return Mathf.PI + t;
        }
        else if (theta >= -Mathf.PI-0.01f && theta < -Mathf.PI / 2)
        {
            // 第三象限，将 t 调整到第三象限
            return -Mathf.PI + t;
        }
        else if (theta >= -Mathf.PI / 2 && theta < 0)
        {
            // 第四象限，t 本身就在正确区间，无需调整
            return t;
        }

        return t;
    }

    public static Vector2 CalculateSubArrowPosition(float currentRate, Vector2 subStarStartScreen, Vector2 subStarEndScreen, TrackFunctionType trackFunction)
    {
        float x1 = subStarStartScreen.x;
        float y1 = subStarStartScreen.y;
        float x2 = subStarEndScreen.x;
        float y2 = subStarEndScreen.y;

        Vector2 result = Vector2.zero;
        Vector2 offset = Vector2.zero;
        float a = 0f;
        float b = 0f;
        float theta = 0f;
        //float k = 0f;
        float t = 0f;
        switch (trackFunction)
        {
            case TrackFunctionType.Linear:
                // 线性函数计算当前位置
                result.x = x1 + ((x2 - x1) * currentRate);
                result.y = y1 + ((y2 - y1) * currentRate);
                break;
            case TrackFunctionType.UpperCir:
                // UpperCir指向上凸起的圆弧（详见说明）
                a = Mathf.Abs(x2 - x1);
                b = Mathf.Abs(y2 - y1);
                if (x1 < x2 && y1 < y2)
                {
                    theta = Mathf.PI - currentRate * 0.5f * Mathf.PI;
                    offset.x = x2;
                    offset.y = y1;
                }
                if (x1 < x2 && y1 > y2)
                {
                    theta = 0.5f * Mathf.PI - currentRate * 0.5f * Mathf.PI;
                    offset.x = x1;
                    offset.y = y2;
                    //Debug.Log(theta);
                    //Debug.Log(offset);
                }
                if (x1 > x2 && y1 < y2)
                {
                    theta = currentRate * 0.5f * Mathf.PI;
                    offset.x = x2;
                    offset.y = y1;
                }
                if (x1 > x2 && y1 > y2)
                {
                    theta = 0.5f * Mathf.PI + currentRate * 0.5f * Mathf.PI;
                    offset.x = x1;
                    offset.y = y2;
                }
                //先判断theta是否为π/2或3/2π（无正切值）
                if (Mathf.Abs(Mathf.Cos(theta)) < 0.001f)
                {
                    result.x = offset.x;
                    result.y = offset.y + b;
                    break;
                }
                t = ConvertAngle(theta, a, b);
                result.x = offset.x + a * Mathf.Cos(t);
                result.y = offset.y + b * Mathf.Sin(t);
                //Debug.Log(result);
                break;
            case TrackFunctionType.LowerCir:
                // LowerCir指向下凸起的圆弧（详见说明）
                a = Mathf.Abs(x2 - x1);
                b = Mathf.Abs(y2 - y1);
                if (x1 < x2 && y1 < y2)
                {
                    theta = -0.5f * Mathf.PI + currentRate * 0.5f * Mathf.PI;
                    offset.x = x1;
                    offset.y = y2;
                }
                if (x1 < x2 && y1 > y2)
                {
                    theta = -Mathf.PI + currentRate * 0.5f * Mathf.PI;
                    offset.x = x2;
                    offset.y = y1;
                    //Debug.Log(theta);
                    //Debug.Log(offset);
                }
                if (x1 > x2 && y1 < y2)
                {
                    theta = -0.5f * Mathf.PI - currentRate * 0.5f * Mathf.PI;
                    offset.x = x1;
                    offset.y = y2;
                }
                if (x1 > x2 && y1 > y2)
                {
                    theta = -currentRate * 0.5f * Mathf.PI;
                    offset.x = x2;
                    offset.y = y1;
                }
                //先判断theta是否为π/2或3/2π（无正切值）
                if (Mathf.Abs(Mathf.Cos(theta)) < 0.001f)
                {
                    result.x = offset.x;
                    result.y = offset.y - b;
                    break;
                }
                t = ConvertAngle(theta, a, b);
                result.x = offset.x + a * Mathf.Cos(t);
                result.y = offset.y + b * Mathf.Sin(t);
                break;
        }
        return result;
    }

    public static float CalculateSubArrowRotation(float currentRate, Vector2 subStarStartScreen, Vector2 subStarEndScreen, TrackFunctionType trackFunction)
    {
        //注意Camera在画布背面，Arrow旋转好像需要镜像？
        float x1 = subStarStartScreen.x;
        float y1 = subStarStartScreen.y;
        float x2 = subStarEndScreen.x;
        float y2 = subStarEndScreen.y;

        //float result = 0f;
        float theta = 0f;

        //先处理x轴坐标相同的情况
        if (x1 == x2)
        {
            if (y1 < y2)
            {
                return 0f;
            }
            else 
            {
                return 180f;
            }
        }

        switch (trackFunction)
        {
            case TrackFunctionType.Linear:
                // 线性函数计算当前位置
                if (x1 <= x2 && y1 <= y2)
                {
                    theta = Mathf.Atan((y2 - y1) / (x2 - x1));
                }
                else if (x1 <= x2 && y1 > y2)
                {
                    theta = Mathf.Atan((y2 - y1) / (x2 - x1));
                }
                else if (x1 > x2 && y1 <= y2)
                {
                    theta = Mathf.Atan((y2 - y1) / (x2 - x1)) + Mathf.PI;
                }
                else if (x1 > x2 && y1 > y2)
                {
                    theta = Mathf.Atan((y2 - y1) / (x2 - x1)) + Mathf.PI;
                }
                theta -= 0.5f * Mathf.PI;
                theta *= Mathf.Rad2Deg;
                break;
            case TrackFunctionType.UpperCir:
                // UpperCir指向上凸起的圆弧（详见说明）
                if (x1 <= x2 && y1 <= y2)
                {
                    theta = currentRate * 0.5f * Mathf.PI;
                }
                else if (x1 <= x2 && y1 > y2)
                {
                    theta = 0.5f * Mathf.PI + currentRate * 0.5f * Mathf.PI;
                }
                else if (x1 > x2 && y1 <= y2)
                {
                    theta = -currentRate * 0.5f * Mathf.PI;
                }
                else if (x1 > x2 && y1 > y2)
                {
                    theta = -0.5f * Mathf.PI - currentRate * 0.5f * Mathf.PI;
                }
                theta *= -Mathf.Rad2Deg;
                break;
            case TrackFunctionType.LowerCir:
                // LowerCir指向下凸起的圆弧（详见说明）
                if (x1 <= x2 && y1 <= y2)
                {
                    theta = 0.5f * Mathf.PI - currentRate * 0.5f * Mathf.PI;
                }
                if (x1 <= x2 && y1 > y2)
                {
                    theta =  Mathf.PI - currentRate * 0.5f * Mathf.PI;
                }
                if (x1 > x2 && y1 <= y2)
                {
                    theta = -0.5f * Mathf.PI + currentRate * 0.5f * Mathf.PI;
                }
                if (x1 > x2 && y1 > y2)
                {
                    theta = -Mathf.PI + currentRate * 0.5f * Mathf.PI;
                }
                theta *= -Mathf.Rad2Deg;
                break;
        }
        return theta;
    }

    public static float CalculateSubStarCurveLength(Note.Star.SubStar subStar)
    {
        float length = 0.0f;
        switch (subStar.trackFunction)
        {
            case TrackFunctionType.Linear:
                // 计算直线长度
                length = Vector2.Distance(new Vector2(subStar.startX, subStar.startY), new Vector2(subStar.endX, subStar.endY));
                break;
            case TrackFunctionType.UpperCir:
            case TrackFunctionType.LowerCir:
                // 对于圆弧，这里使用简单的椭圆周长近似公式：π * (3 * (a + b) - Mathf.Sqrt((3 * a + b) * (a + 3 * b)))
                float a = Mathf.Abs(subStar.endX - subStar.startX);
                float b = Mathf.Abs(subStar.endY - subStar.startY);
                length = Mathf.PI * (3 * (a + b) - Mathf.Sqrt((3 * a + b) * (a + 3 * b))) / 4;
                break;
            default:
                // 对于其他未定义的情况，可以添加相应的处理或抛出异常
                break;
        }
        return length;
    }

    //计算与相机在特定距离下，世界坐标单位长度对应的屏幕像素长度
    public static float CalculateWorldUnitToScreenPixelAtDistance(Vector3 targetPoint)
    {
        // 获取摄像机组件（假设场景中只有一个主摄像机，可根据实际情况调整获取方式）
        Camera mainCamera = Camera.main;

        // 注意Camera视野轴设置为垂直，需要计算水平场视角
        float verticalFOV = mainCamera.fieldOfView;
        float aspectRatio = mainCamera.aspect;
        // 通过三角函数关系计算水平视场角
        float horizontalFOV = 2 * Mathf.Atan(Mathf.Tan(verticalFOV * Mathf.Deg2Rad / 2) * aspectRatio) * Mathf.Rad2Deg;

        //Debug.Log("水平视场角为: " + horizontalFOV + "度");
        float screenWidth = Screen.width; // 屏幕宽度，单位是像素
        //float screenHeight = Screen.height; // 屏幕高度，单位是像素

        // 根据摄像机的视野角度（fieldOfView）和近裁剪平面距离（nearClipPlane）计算视野的水平半角（单位：弧度）
        float halfFieldOfViewInRadians = horizontalFOV * Mathf.Deg2Rad / 2;

        float distanceToCamera = Vector3.Distance(targetPoint, mainCamera.transform.position);

        float VisibleRangeAtTargetDepth = 2 * distanceToCamera * Mathf.Tan(halfFieldOfViewInRadians);
        //Debug.Log(VisibleRangeAtTargetDepth);
        float worldUnitToScreenPixelXAtTarget = screenWidth / VisibleRangeAtTargetDepth;

        return worldUnitToScreenPixelXAtTarget;
    }

    public static float CalculateYAxisPixel(Vector3 targetPoint)
    {
        // 获取摄像机组件（假设场景中只有一个主摄像机，可根据实际情况调整获取方式）
        Camera mainCamera = Camera.main;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPoint);
        return screenPosition.y;
    }


    public static bool IsInXAxisRange(float noteSize, float startX)
    {
        float halfNoteSize = noteSize / 2;
        return startX - halfNoteSize >= ChartParams.XaxisMin - 0.01f && startX + halfNoteSize <= ChartParams.XaxisMax + 0.01f;
    }

    public static Vector2 ScalePositionToScreen(Vector2 position, RectTransform canvas)
    {
        //注意这里获取的是画布的长宽，而不是屏幕的长宽
        float screenWidth = canvas.sizeDelta.x;
        float screenHeight = canvas.sizeDelta.y;
        //Debug.Log(screenHeight);

        float screenXMin = screenWidth * HorizontalParams.HorizontalMargin;
        float screenXMax = screenWidth * (1 - HorizontalParams.HorizontalMargin);
        float screenXRange = screenXMax - screenXMin;

        Vector3 worldYBottom = new Vector3(0, 0, 0);
        Vector3 worldYCeiling = new Vector3(0, HeightParams.HeightDefault, 0);
        //这里的计算逻辑我没想明白，但是最终计算结果是正确的，后续需要再检查
        float ScreenYBottom = CalculateYAxisPixel(worldYBottom) * screenHeight / Screen.height;
        float ScreenYCeiling = CalculateYAxisPixel(worldYCeiling) * screenHeight / Screen.height;
        //Debug.Log(ScreenYCeiling);
        //Debug.Log(ScreenYBottom);

        float scaledX = position.x / ChartParams.XaxisMax * screenXRange / 2;
        float scaledY = ((position.y / ChartParams.YaxisMax) * (ScreenYCeiling - ScreenYBottom) + ScreenYBottom) - (screenHeight / 2);


        return new Vector2(scaledX, scaledY);
    }

    public static float CalculateWorldUnitToScreenPixelXAtPosition(Vector3 worldPosition)
    {
        // 获取屏幕宽度
        float screenWidth = Screen.width;
        float targetHorizontalMargin = HorizontalParams.HorizontalMargin; // 目标水平边距，即离屏幕边缘10%的距离，可根据需求调整

        // 计算水平可视范围（考虑边距后的有效宽度）
        float horizontalVisibleRange = screenWidth * (1 - 2 * targetHorizontalMargin);

        Vector3 Point = worldPosition;

        float WorldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelAtDistance(Point);

        float XWorld = horizontalVisibleRange / 2 / WorldUnitToScreenPixelX;

        return XWorld;
    }


    public static void CheckArrowVisibility(GameObject SubStarsParent, float currentTime, Dictionary<(int, int), SubStarInfo> subStarInfoDict)
    {
        foreach (var infoPair in subStarInfoDict)
        {
            SubStarInfo info = infoPair.Value;
            List<GameObject> arrows = info.arrows;
            GameObject SubStarArrowParent = info.SubStarArrowParent;
            float totalTime = info.totalTime;
            float arrowTimeInterval = info.arrowTimeInterval;
            float starTrackStartT = info.starTrackStartT;
            float starTrackEndT = info.starTrackEndT;
            float starHeadT = info.starHeadT;

            // 当时间处于 starHeadT - StarAppearTime 和 starHeadT 之间时，将该 substar 下对应 Arrow 均设置为可见，该 substar 下所有 arrow 的透明度由 0 线性地变为 1
            if (currentTime >= starHeadT - ChartParams.StarAppearTime && currentTime <= starHeadT)
            {
                SubStarArrowParent.SetActive(true);
                if (arrows.Count != 0)
                {
                    float t = (currentTime - (starHeadT - ChartParams.StarAppearTime)) / ChartParams.StarAppearTime;
                    foreach (var arrow in arrows)
                    {
                        arrow.SetActive(true);
                        SetArrowAlpha(arrow, t);
                    }
                }
            }
            // 当时间处于 starTrackStartT 和 starTrackEndT 之间时，按箭头顺序，将该 substar 下所有 arrow 的透明度由 1 线性地变为 0
            else if (currentTime >= starTrackStartT && currentTime <= starTrackEndT)
            {
                SubStarArrowParent.SetActive(true);
                if (arrows.Count != 0)
                {
                    int arrowIndex = Mathf.FloorToInt((currentTime - starTrackStartT) / arrowTimeInterval);
                    arrowIndex = Mathf.Clamp(arrowIndex, 0, arrows.Count - 1);

                    for (int k = 0; k < arrows.Count; k++)
                    {
                        float alpha = 1.0f;
                        if (k <= arrowIndex)
                        {
                            if (k == arrowIndex)
                            {
                                float timeInInterval = (currentTime - (starTrackStartT + k * arrowTimeInterval)) / arrowTimeInterval;
                                alpha = 1.0f - timeInInterval;
                            }
                            else
                            {
                                alpha = 0.0f;
                            }
                        }
                        SetArrowAlpha(arrows[k], alpha);
                    }
                }
            }
            else if (currentTime > starTrackEndT)
            {
                if (arrows.Count != 0)
                {
                    SubStarArrowParent.SetActive(false);
                }
            }
            else if (currentTime < starHeadT - ChartParams.StarAppearTime)
            {
                if (arrows.Count != 0)
                {
                    SubStarArrowParent.SetActive(false);
                }
            }
        }
    }



    //public static void CheckArrowVisibility(Chart chart, GameObject SubStarsParent, float currentTime)
    //{
    //    if (chart.stars != null)
    //    {
    //        for (int i = 0; i < chart.stars.Count; i++)
    //        {
    //            var star = chart.stars[i];
    //            float starHeadT = star.starHeadT;
    //            for (int j = 0; j < star.subStarList.Count; j++)
    //            {
    //                var subStar = star.subStarList[j];
    //                int subStarIndex = j;

    //                string instanceName = $"Star{i + 1}SubStar{j + 1}Arrows";
    //                //Debug.Log(instanceName);
    //                GameObject SubStarArrowParent = SubStarsParent.transform.Find(instanceName).gameObject;
    //                //Debug.Log(SubStarArrowParent);
    //                List<GameObject> arrows = new List<GameObject>();

    //                if (SubStarArrowParent != null)
    //                {
    //                    for (int k = 0; k < SubStarArrowParent.transform.childCount; k++)
    //                    {
    //                        GameObject arrow = SubStarArrowParent.transform.GetChild(k).gameObject;
    //                        arrows.Add(arrow);
    //                    }

    //                    if (currentTime >= starHeadT - ChartParams.StarAppearTime && currentTime < starHeadT)
    //                    {
    //                        SubStarArrowParent.SetActive(true);
    //                        // 当时间处于 starHeadT - StarAppearTime 和 substar.startT 之间时，将该 substar 下对应 Arrow 均设置为可见，该 substar 下所有 arrow 的透明度由 0 线性地变为 1
    //                        if (arrows.Count != 0)
    //                        {
    //                            float t = (currentTime - (starHeadT - ChartParams.StarAppearTime)) / ChartParams.StarAppearTime;
    //                            foreach (var arrow in arrows)
    //                            {
    //                                arrow.SetActive(true);
    //                                SetArrowAlpha(arrow, t);
    //                            }
    //                        }
    //                    }
    //                    else if (currentTime >= subStar.starTrackStartT && currentTime < subStar.starTrackEndT)
    //                    {
    //                        SubStarArrowParent.SetActive(true);
    //                        if (arrows.Count != 0)
    //                        {
    //                            // 计算时间间隔和每个箭头的时间间隔
    //                            float totalTime = subStar.starTrackEndT - subStar.starTrackStartT;
    //                            float arrowTimeInterval = totalTime / arrows.Count;

    //                            // 计算当前时间所在的箭头索引
    //                            int arrowIndex = Mathf.FloorToInt((currentTime - subStar.starTrackStartT) / arrowTimeInterval);

    //                            // 确保箭头索引不越界
    //                            arrowIndex = Mathf.Clamp(arrowIndex, 0, arrows.Count - 1);

    //                            // 遍历箭头，根据时间设置透明度
    //                            for (int k = 0; k < arrows.Count; k++)
    //                            {
    //                                float alpha = 1.0f;
    //                                if (k <= arrowIndex)
    //                                {
    //                                    // 对于当前箭头及之前的箭头，根据时间线性改变透明度
    //                                    if (k == arrowIndex)
    //                                    {
    //                                        float timeInInterval = (currentTime - (subStar.starTrackStartT + k * arrowTimeInterval)) / arrowTimeInterval;
    //                                        alpha = 1.0f - timeInInterval;
    //                                    }
    //                                    else
    //                                    {
    //                                        alpha = 0.0f;
    //                                    }
    //                                }
    //                                SetArrowAlpha(arrows[k], alpha);
    //                            }
    //                        }
    //                    }
    //                    else if (currentTime >= subStar.starTrackEndT)
    //                    {
    //                        if (arrows.Count != 0)
    //                        {
    //                            // 当时间大于 starTrackEndT 时，将 substar 下的 arrow 实例均设为非激活
    //                            SubStarArrowParent.SetActive(false);
    //                        }
    //                    }
    //                    else if (currentTime < starHeadT - ChartParams.StarAppearTime)
    //                    {
    //                        if (arrows.Count != 0)
    //                        {
    //                            // 当时间小于 starHeadT - ChartParams.StarAppearTime 时，将 substar 下的 arrow 实例均设为非激活
    //                            SubStarArrowParent.SetActive(false);
    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    Debug.Log(instanceName + "未找到！");
    //                }
    //            }
    //        }
    //    }
    //}

    public static GameObject CombineInstances(List<GameObject> instances)
    {
        GameObject combined = new GameObject("CombinedInstance");

        // 用于存储所有顶点、三角形索引和UV坐标
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        int vertexOffset = 0;
        foreach (GameObject instance in instances)
        {
            MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.mesh;
                // 添加顶点
                vertices.AddRange(mesh.vertices);
                // 处理三角形索引，添加偏移量
                for (int i = 0; i < mesh.triangles.Length; i++)
                {
                    triangles.Add(mesh.triangles[i] + vertexOffset);
                }
                // 添加UV坐标（如果有的话）
                if (mesh.uv.Length > 0)
                {
                    uv.AddRange(mesh.uv);
                }
                vertexOffset += mesh.vertices.Length;
            }
        }

        // 创建新的网格
        Mesh combinedMesh = new Mesh();
        combinedMesh.vertices = vertices.ToArray();
        combinedMesh.triangles = triangles.ToArray();
        if (uv.Count > 0)
        {
            combinedMesh.uv = uv.ToArray();
        }
        combinedMesh.RecalculateNormals();

        // 添加网格组件和渲染器组件
        MeshFilter combinedMeshFilter = combined.AddComponent<MeshFilter>();
        combinedMeshFilter.mesh = combinedMesh;
        MeshRenderer combinedRenderer = combined.AddComponent<MeshRenderer>();
        combinedRenderer.material = instances[0].GetComponentInChildren<MeshRenderer>().material;

        return combined;
    }
    public static float CalculateZAxisPosition(float startTime)
    {
        // 假设存在SpeedParams.NoteSpeedDefault这个速度参数，你需根据实际情况调整
        return -startTime * SpeedParams.NoteSpeedDefault;
    }

    // 设置锚点的辅助方法
    public static void SetAnchor(GameObject obj, Vector2 anchor)
    {
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
        }
    }

    public static void AdjustFlickArrowPosition(GameObject flickarrow, GameObject flick, float flickDirection)
    {
        Vector3 leftMiddleWorldPos = GetLeftMiddleWorldPosition(flick);
        Vector3 rightMiddleWorldPos = GetRightMiddleWorldPosition(flick);
        //Debug.Log(leftMiddleWorldPos);
        //Debug.Log(rightMiddleWorldPos);

        float arrowRotationAngle = flickDirection * 360; // 假设flickDirection是0-1之间的值，转换为0-360度的角度，根据实际调整
        if (Math.Abs(arrowRotationAngle - 90) <= 1 | Math.Abs(arrowRotationAngle - 270) <= 1)
        {
            bool ifleft = Math.Abs(arrowRotationAngle - 90) <= 1;
            flickarrow.transform.rotation = Quaternion.Euler(-90, 0, arrowRotationAngle);
            // 设置Flick箭头实例的位置，使其在Flick的合适位置上
            if (ifleft)
            {
                //以Flick左侧坐标为锚点，往右平移
                Vector3 Pos = leftMiddleWorldPos;
                Vector3 PositionAdjust = new Vector3(1.2f, 0, 0);
                Vector3 newPosition = Pos + PositionAdjust;
                flickarrow.transform.position = newPosition;
            }
            else
            {
                //以Flick右侧坐标为锚点，往左平移
                Vector3 Pos = rightMiddleWorldPos;
                Vector3 PositionAdjust = new Vector3(-1.2f, 0, 0);
                Vector3 newPosition = Pos + PositionAdjust;
                flickarrow.transform.position = newPosition;
            }
            // 根据Flick的缩放比例同步缩放箭头（仅针对X轴缩放，即缩放箭头宽度）
            flickarrow.transform.localScale = new Vector3(1f, 0.8f, 1);
        }
        //针对非横划（90度或270度），箭头应显示在竖直平面内
        else
        {
            flickarrow.transform.rotation = Quaternion.Euler(0, 0, arrowRotationAngle);
            // 根据Flick的缩放比例同步缩放箭头（仅针对X轴缩放，即缩放箭头宽度）
            flickarrow.transform.localScale = new Vector3(1f, 0.8f, 1);
        }
    }

    // 获取矩形左侧中间的世界坐标
    public static Vector3 GetLeftMiddleWorldPosition(GameObject obj)
    {
        // 获取物体的 Transform 组件
        Transform transform = obj.transform;

        // 获取物体的 Renderer 组件，用于获取边界信息
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("物体缺少 Renderer 组件！");
            return Vector3.zero;
        }

        // 获取物体的局部边界
        Bounds localBounds = renderer.bounds;

        // 计算左侧中间的局部坐标
        Vector3 leftMiddlePos = new Vector3(localBounds.min.x, localBounds.center.y, localBounds.center.z);
        //Debug.Log(leftMiddleLocalPos);

        // 将局部坐标转换为世界坐标
        //Vector3 leftMiddleWorldPos = transform.TransformPoint(leftMiddleLocalPos);

        return leftMiddlePos;
    }

    // 获取矩形右侧中间的世界坐标
    public static Vector3 GetRightMiddleWorldPosition(GameObject obj)
    {
        // 获取物体的 Transform 组件
        Transform transform = obj.transform;

        // 获取物体的 Renderer 组件，用于获取边界信息
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("物体缺少 Renderer 组件！");
            return Vector3.zero;
        }

        // 获取物体的局部边界
        Bounds localBounds = renderer.bounds;

        // 计算右侧中间的局部坐标
        Vector3 rightMiddlePos = new Vector3(localBounds.max.x, localBounds.center.y, localBounds.center.z);

        // 将局部坐标转换为世界坐标
        //Vector3 rightMiddleWorldPos = transform.TransformPoint(rightMiddleLocalPos);

        return rightMiddlePos;
    }

}


