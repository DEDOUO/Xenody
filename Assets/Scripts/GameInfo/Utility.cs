using System;
//using System.Numerics;
using UnityEngine;
using Note;
using Params;
//using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static Note.Star;
using System.Collections.Generic;
using static GradientColorListUnity;
using System.Linq;
//using DocumentFormat.OpenXml.InkML;
//using System.Text.RegularExpressions;
//using UnityEngine.Windows;
//using Unity.VisualScripting;



public class Utility : MonoBehaviour
{
    // 定义X轴/Y轴坐标变化函数的枚举类型
    //适用JudgePlane和Hold类
    public enum TransFunctionType { Linear, Sin, Cos }
    // 正弦函数(开头不平滑，结尾平滑)
    // 余弦函数(开头平滑，结尾不平滑)

    // 定义星星坐标变化函数的枚举类型
    //适用Star类
    //星星暂时只支持线性和圆形（椭圆型）
    //sin和cos涉及曲线计算的部分，不能用初等函数表示，只能通过数值计算，暂时忽略
    //Linear:线性
    //CWC：顺时针圆  CCWC：逆时针圆
    public enum TrackFunctionType { Linear, CWC, CCWC }


    /// 根据给定的时间、起始值、结束值以及坐标变化函数类型来计算相应的位置值。
    /// 可以用于处理如游戏中物体在某个时间段内按照不同函数规律进行位置变化的情况。
    public static float CalculatePosition(float currentTime, float startTime, float startVal, float endTime, float endVal, TransFunctionType functionType)
    {
        if (startTime > endTime)
        {
            throw new ArgumentException($"起始时间不能大于结束时间:{startTime}");
        }

        if (!Enum.IsDefined(typeof(TransFunctionType), functionType))
        {
            throw new ArgumentException($"传入的坐标变化函数类型无效:{functionType}");
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
        else if (theta >= Mathf.PI / 2 && theta <= Mathf.PI + 0.01f)
        {
            // 第二象限，将 t 调整到第二象限
            return Mathf.PI + t;
        }
        else if (theta >= -Mathf.PI - 0.01f && theta < -Mathf.PI / 2)
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

    public static Vector2 CalculateSubArrowPositionCircle(float currentRate, Vector2 subStarStartScreen, RectTransform canvas, SubStar subStar)
    {

        Vector2 result = Vector2.zero;

        float canvasWidth = canvas.sizeDelta.x;
        //float canvasHeight = canvas.sizeDelta.y;
        float croppedcanvasWidth = AspectRatioManager.croppedScreenWidth / Screen.width * canvasWidth;
        float screenXRange = croppedcanvasWidth * (1 - 2 * HorizontalParams.HorizontalMargin);
        //注意以X轴坐标单位为基准
        float UnitXPxiel = screenXRange / ChartParams.XaxisMax / 2;

        // 将圆弧半径转换为画布上的坐标长度
        float radiusInCanvas = subStar.Radius * UnitXPxiel;
        //Debug.Log(radiusInCanvas);
        // 将角度从 0 - 1 范围转换为 0 - 360 度
        // 注意这里角度要乘以当前rate
        float angleDegree = subStar.Angle * 360f * currentRate;

        // 将旋转角度从 0 - 1 范围转换为 0 - 360 度
        float rotationDegree = subStar.Rotation * 360f;
        //将角度改为弧度
        float angleRadian = angleDegree * Mathf.Deg2Rad;
        float rotationRadian = rotationDegree * Mathf.Deg2Rad;

        // 起始点坐标
        float startX = subStarStartScreen.x;
        float startY = subStarStartScreen.y;

        // 计算圆心坐标
        float centerX, centerY, endX, endY;

        //float cosTheta = Mathf.Cos(rotationRadian);
        //float sinTheta = Mathf.Sin(rotationRadian);

        if (subStar.trackFunction == TrackFunctionType.CWC) // 顺时针圆弧
        {
            // 圆点中心在初始点下方
            // 将圆心绕起始点逆时针旋转 rotationDegree+90 度
            centerX = startX - radiusInCanvas * Mathf.Cos(rotationRadian + 0.5f * Mathf.PI);
            centerY = startY - radiusInCanvas * Mathf.Sin(rotationRadian + 0.5f * Mathf.PI);
            // 终点为圆心从 rotationDegree-90 度开始，转 angleRadian
            endX = centerX + radiusInCanvas * Mathf.Cos(rotationRadian + 0.5f * Mathf.PI - angleRadian);
            endY = centerY + radiusInCanvas * Mathf.Sin(rotationRadian + 0.5f * Mathf.PI - angleRadian);
        }
        else if (subStar.trackFunction == TrackFunctionType.CCWC) // 逆时针圆弧
        {
            // 圆点中心在初始点下方
            // 将圆心绕起始点逆时针旋转 rotationDegree-90 度
            centerX = startX - radiusInCanvas * Mathf.Cos(rotationRadian - 0.5f * Mathf.PI);
            centerY = startY - radiusInCanvas * Mathf.Sin(rotationRadian - 0.5f * Mathf.PI);
            // 终点为圆心从 rotationDegree+90 度开始，转 -angleRadian
            endX = centerX + radiusInCanvas * Mathf.Cos(rotationRadian - 0.5f * Mathf.PI + angleRadian);
            endY = centerY + radiusInCanvas * Mathf.Sin(rotationRadian - 0.5f * Mathf.PI + angleRadian);
        }
        else
        {
            // 未知的轨迹函数类型，返回0,0作为默认值
            endX = 0f;
            endY = 0f;
        }
        result.x = endX;
        result.y = endY;
        return result;
    }

    public static Vector2 CalculateSubArrowPositionLinear(float currentRate, Vector2 subStarStartScreen, Vector2 subStarEndScreen)
    {
        float x1 = subStarStartScreen.x;
        float y1 = subStarStartScreen.y;
        float x2 = subStarEndScreen.x;
        float y2 = subStarEndScreen.y;

        Vector2 result = Vector2.zero;
        result.x = x1 + ((x2 - x1) * currentRate);
        result.y = y1 + ((y2 - y1) * currentRate);

        return result;
    }


    public static float CalculateSubArrowRotationCircle(float currentRate, Vector2 subStarStartScreen, SubStar subStar)
    {


        // 将角度从 0 - 1 范围转换为 0 - 360 度
        // 注意这里角度要乘以当前rate
        float angleDegree = subStar.Angle * 360f * currentRate;

        // 将旋转角度从 0 - 1 范围转换为 0 - 360 度
        float rotationDegree = subStar.Rotation * 360f;
        //将角度改为弧度
        float angleRadian = angleDegree * Mathf.Deg2Rad;
        float rotationRadian = rotationDegree * Mathf.Deg2Rad;

        float theta = rotationRadian;


        if (subStar.trackFunction == TrackFunctionType.CWC) // 顺时针圆弧
        {
            theta -= angleRadian;
        }
        else if (subStar.trackFunction == TrackFunctionType.CCWC) // 逆时针圆弧
        {
            theta += angleRadian;
        }

        theta -= 0.5f * Mathf.PI;
        theta *= Mathf.Rad2Deg;

        return theta;
    }

    public static float CalculateSubArrowRotationLinear(float currentRate, Vector2 subStarStartScreen, Vector2 subStarEndScreen)
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

        return theta;
    }


    public static float CalculateSubStarCurveLength(SubStar subStar)
    {
        float length = 0.0f;
        switch (subStar.trackFunction)
        {
            case TrackFunctionType.Linear:
                // 计算直线长度
                length = Vector2.Distance(new Vector2(subStar.startX, subStar.startY), new Vector2(subStar.endX, subStar.endY));
                break;
            case TrackFunctionType.CWC:
            case TrackFunctionType.CCWC:
                // 计算圆弧长度
                length = subStar.Radius * subStar.Angle * 360f * Mathf.Deg2Rad;
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


    public static Vector2 ScalePositionToScreenStar(Vector2 position, RectTransform canvas)
    {
        // 注意这里获取的是画布的长宽，而不是屏幕的长宽
        float canvasWidth = canvas.sizeDelta.x;
        float canvasHeight = canvas.sizeDelta.y;

        // 以下Y轴坐标计算逻辑与 ScalePositionToScreenJudgeLine 保持一致
        float croppedcanvasHeight = AspectRatioManager.croppedScreenHeight / Screen.height * canvasHeight;
        float croppedcanvasWidth = AspectRatioManager.croppedScreenWidth / Screen.width * canvasWidth;
        float screenXRange = croppedcanvasWidth * (1 - 2 * HorizontalParams.HorizontalMargin);

        float bottomPixel = AspectRatioManager.croppedScreenHeight * (1 - HorizontalParams.VerticalMarginBottom);
        float topPixel = AspectRatioManager.croppedScreenHeight * (1 - HorizontalParams.VerticalMarginCeiling);

        float ScreenYBottom = bottomPixel * canvasHeight / Screen.height;
        //Debug.Log(ScreenYBottom);
        float ScreenYCeiling = topPixel * canvasHeight / Screen.height;

        float scaledY = (croppedcanvasHeight / 2) - ((position.y / ChartParams.YaxisMax) * (ScreenYCeiling - ScreenYBottom) + ScreenYBottom);
        float scaledX = position.x / ChartParams.XaxisMax * screenXRange / 2;

        return new Vector2(scaledX, scaledY);
    }

    public static Vector2 ScreenPositionToScaleStar(Vector2 position, RectTransform canvas)
    {
        // 获取画布的长宽
        float canvasWidth = canvas.sizeDelta.x;
        float canvasHeight = canvas.sizeDelta.y;

        // 计算裁剪后的画布尺寸（新增逻辑）
        float croppedcanvasHeight = AspectRatioManager.croppedScreenHeight / Screen.height * canvasHeight;
        float croppedcanvasWidth = AspectRatioManager.croppedScreenWidth / Screen.width * canvasWidth;
        float screenXRange = croppedcanvasWidth * (1 - 2 * HorizontalParams.HorizontalMargin);

        // 计算垂直边界像素（Y轴逻辑未变）
        float bottomPixel = AspectRatioManager.croppedScreenHeight * (1 - HorizontalParams.VerticalMarginBottom);
        float topPixel = AspectRatioManager.croppedScreenHeight * (1 - HorizontalParams.VerticalMarginCeiling);
        float ScreenYBottom = bottomPixel * canvasHeight / Screen.height;
        float ScreenYCeiling = topPixel * canvasHeight / Screen.height;

        // 反向计算X轴坐标（关键修改点）
        float originalX = (position.x * 2 / screenXRange) * ChartParams.XaxisMax;

        // 反向计算Y轴坐标（保持不变）
        float yDiff = (croppedcanvasHeight / 2) - position.y - ScreenYBottom;
        float originalY = (yDiff / (ScreenYCeiling - ScreenYBottom)) * ChartParams.YaxisMax;

        return new Vector2(originalX, originalY);
    }

    public static Vector2 CauculateEndScreenStar(Vector2 subStarStartScreen, RectTransform canvas, SubStar subStar)
    {
        return CalculateSubArrowPositionCircle(1f, subStarStartScreen, canvas, subStar);
    }


    // JudgeLine的X轴坐标一直为0，简化计算
    // JudgeLine的Y轴坐标需要额外修正；JudgePlane的Y轴坐标线性变化时，其实对应JudgeLine的Y轴坐标不是线性变化，需要修正（注意摄像机有一定倾角）
    public static Vector2 ScalePositionToScreenJudgeLine(Vector2 position, RectTransform canvas)
    {
        float canvasHeight = canvas.sizeDelta.y;
        float croppedcanvasHeight = AspectRatioManager.croppedScreenHeight / Screen.height * canvasHeight;

        float bottomPixel = AspectRatioManager.croppedScreenHeight * (1 - HorizontalParams.VerticalMarginBottom);
        float topPixel = AspectRatioManager.croppedScreenHeight * (1 - HorizontalParams.VerticalMarginCeiling);

        float ScreenYBottom = bottomPixel * canvasHeight / Screen.height;
        //Debug.Log(ScreenYBottom);
        float ScreenYCeiling = topPixel * canvasHeight / Screen.height;

        float scaledY = (croppedcanvasHeight / 2) - ((position.y / ChartParams.YaxisMax) * (ScreenYCeiling - ScreenYBottom) + ScreenYBottom);
        return new Vector2(0, scaledY);
    }

    public static float CalculateWorldUnitToScreenPixelXAtPosition(Vector3 worldPosition, float targetHorizontalMargin)
    {
        //AspectRatioManager aspectRatioManager = GetComponent<AspectRatioManager>();
        // 获取屏幕宽度
        float screenWidth = AspectRatioManager.croppedScreenWidth;

        // 计算水平可视范围（考虑边距后的有效宽度）
        float horizontalVisibleRange = screenWidth * (1 - 2 * targetHorizontalMargin);

        Vector3 Point = worldPosition;

        float WorldUnitToScreenPixelX = CalculateWorldUnitToScreenPixelAtDistance(Point);

        float XWorld = horizontalVisibleRange / 2 / WorldUnitToScreenPixelX;

        return XWorld;
    }

    public static void CheckArrowVisibility(float currentTime, Dictionary<(int, int), SubStarInfo> subStarInfoDict, GameObject SubStarsParent)
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
            float starstartT = info.starstartT;
            float starendT = info.starendT;
            bool is_firstsubStar = info.is_firstsubStar;
            GameObject StartArrow = info.StartArrow;
            Star star = info.star;
            int id = info.id;

            //更新启动Arrow的位置
            if (is_firstsubStar & StartArrow != null)
            {
                UpdateStartArrowPos(StartArrow, currentTime, star, SubStarArrowParent);
            }


            //预留个100ms的检测窗口，提前0.4秒播放星星开始提示
            if (currentTime >= starTrackStartT - 0.4f && currentTime <= starTrackStartT - 0.35f)
            {
                if (is_firstsubStar & StartArrow != null)
                {
                    // 如果还没播放对应的启动特效，播放
                    string targetName = $"StarStartFX{id}";
                    Transform targetTransform = SubStarsParent.transform.Find(targetName);
                    if (targetTransform != null)
                    {
                        // 激活物体并播放粒子系统
                        targetTransform.gameObject.SetActive(true);

                        // 获取并播放粒子系统
                        ParticleSystem particleSystem = targetTransform.GetComponent<ParticleSystem>();
                        if (particleSystem != null & !particleSystem.isPlaying)
                        {
                            particleSystem.Play();
                        }
                    }

                }
            }

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
            // 当时间处于 starTrackStartT 和 starTrackEndT 之间时，按箭头顺序，将该 substar 下所有非启动 arrow 的透明度由 1 线性地变为 0
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

                        // 注意跳过启动Arrow
                        if (!is_firstsubStar | k != 0)
                        {
                            SetArrowAlpha(arrows[k], alpha);
                        }
                    }
                }
            }
            else if (currentTime > starTrackEndT)
            {
                // 对于不包含启动Arrow的SubStar，直接将实例设为非激活
                if (!is_firstsubStar)
                {
                    SubStarArrowParent.SetActive(false);
                }
                // 对于包含启动Arrow的SubStar，整个个星星结束了才设为非激活
                else
                {
                    if (currentTime > starendT)
                    {
                        SubStarArrowParent.SetActive(false);
                    }
                    else
                    {
                        for (int k = 1; k < arrows.Count; k++)
                        {
                            SetArrowAlpha(arrows[k], 0.0f);
                        }
                    }

                }
            }
            else if (currentTime < starHeadT - ChartParams.StarAppearTime)
            {
                SubStarArrowParent.SetActive(false);
            }
        }
    }

    public static void UpdateStartArrowPos(GameObject StartArrow, float currentTime, Star star, GameObject SubStarsParent)
    {
        int subStarIndex = 1;

        RectTransform SubStarsParentRect = SubStarsParent.GetComponent<RectTransform>();
        RectTransform arrowRectTransform = StartArrow.GetComponent<RectTransform>();
        Vector2 position = Vector2.zero;
        float rotation = 0f;

        foreach (var subStar in star.subStarList)
        {
            float starTrackStartT = subStar.starTrackStartT;
            float starTrackEndT = subStar.starTrackEndT;

            //如果currentTime小于第一个Substar的开始时间，则初始化subStar位置
            if (subStarIndex == 1 & currentTime < starTrackStartT)
            {

                Vector2 subStarStart = new Vector2(subStar.startX, subStar.startY);
                Vector2 subStarStartScreen = ScalePositionToScreenStar(subStarStart, SubStarsParentRect);

                switch (subStar.trackFunction)
                {

                    // 如果是线性，则根据起始位置和终止位置初始化
                    case TrackFunctionType.Linear:

                        Vector2 subStarEnd = new Vector2(subStar.endX, subStar.endY);
                        Vector2 subStarEndScreen = ScalePositionToScreenStar(subStarEnd, SubStarsParentRect);

                        position = CalculateSubArrowPositionLinear(0f, subStarStartScreen, subStarEndScreen);
                        rotation = CalculateSubArrowRotationLinear(0f, subStarStartScreen, subStarEndScreen);

                        arrowRectTransform.anchoredPosition3D = new Vector3(position.x, position.y, 0);
                        arrowRectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
                        return;

                    // 如果是圆弧，则首先需要计算终止位置
                    case TrackFunctionType.CWC:
                    case TrackFunctionType.CCWC:

                        Vector2 substarEndScreen = CauculateEndScreenStar(subStarStartScreen, SubStarsParentRect, subStar);
                        Vector2 substarEnd = ScreenPositionToScaleStar(substarEndScreen, SubStarsParentRect);
                        position = CalculateSubArrowPositionCircle(0f, subStarStartScreen, SubStarsParentRect, subStar);
                        rotation = CalculateSubArrowRotationCircle(0f, subStarStartScreen, subStar);


                        arrowRectTransform.anchoredPosition3D = new Vector3(position.x, position.y, 0);
                        arrowRectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
                        return;
                }
            }

            //找到现在时间位于哪个SubStar
            if (currentTime >= starTrackStartT & currentTime <= starTrackEndT)
            {
                float currentRate = (currentTime - starTrackStartT) / (starTrackEndT - starTrackStartT);

                Vector2 subStarStart = new Vector2(subStar.startX, subStar.startY);
                Vector2 subStarStartScreen = ScalePositionToScreenStar(subStarStart, SubStarsParentRect);

                switch (subStar.trackFunction)
                {

                    // 如果是线性，则根据起始位置和终止位置初始化
                    case TrackFunctionType.Linear:

                        Vector2 subStarEnd = new Vector2(subStar.endX, subStar.endY);
                        Vector2 subStarEndScreen = ScalePositionToScreenStar(subStarEnd, SubStarsParentRect);

                        position = CalculateSubArrowPositionLinear(currentRate, subStarStartScreen, subStarEndScreen);
                        rotation = CalculateSubArrowRotationLinear(currentRate, subStarStartScreen, subStarEndScreen);

                        arrowRectTransform.anchoredPosition3D = new Vector3(position.x, position.y, 0);
                        arrowRectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
                        return;

                    // 如果是圆弧，则首先需要计算终止位置
                    case TrackFunctionType.CWC:
                    case TrackFunctionType.CCWC:

                        Vector2 substarEndScreen = CauculateEndScreenStar(subStarStartScreen, SubStarsParentRect, subStar);
                        Vector2 substarEnd = ScreenPositionToScaleStar(substarEndScreen, SubStarsParentRect);
                        position = CalculateSubArrowPositionCircle(currentRate, subStarStartScreen, SubStarsParentRect, subStar);
                        rotation = CalculateSubArrowRotationCircle(currentRate, subStarStartScreen, subStar);

                        arrowRectTransform.anchoredPosition3D = new Vector3(position.x, position.y, 0);
                        arrowRectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
                        return;
                }
            }
            subStarIndex += 1;
        }
        return;
    }

    public static GameObject CombineInstances(List<GameObject> instances, Transform parent)
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

                //Vector3[] Vertices = mesh.vertices;
                //for (int i = 0; i < Vertices.Length; i++)
                //{
                //    if (float.IsNaN(Vertices[i].x) || float.IsNaN(Vertices[i].y) || float.IsNaN(Vertices[i].z))
                //    {
                //        Debug.LogError($"Invalid vertex at{instance.name}, index {i}: {Vertices[i]}");
                //    }
                //}

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

        // 先从父物体移除所有子物体
        foreach (GameObject segmentInstance in instances)
        {
            segmentInstance.transform.SetParent(null);
        }

        // 设置合并后的物体为父物体的子物体
        combined.transform.SetParent(parent);

        // 销毁合并前的实例
        foreach (GameObject segmentInstance in instances)
        {
            UnityEngine.Object.Destroy(segmentInstance);
        }

        return combined;
    }

    // 辅助方法，从instanceName中提取数字部分，用于查找对应的Hold索引
    public static int GetHoldIndexFromInstanceName(string instanceName)
    {
        if (instanceName.StartsWith("Hold") && int.TryParse(instanceName.Substring(4), out int index))
        {
            return index - 1;
        }
        return -1;
    }


    public static float CalculateZAxisPosition(float startTime, float Offset, List<Speed> speedList)
    {
        if (speedList == null || speedList.Count == 0)
        {
            return -startTime * SpeedParams.NoteSpeedDefault;
        }

        // 对速度列表按 startT 排序
        speedList = speedList.OrderBy(s => s.startT).ToList();

        float totalDistance = 0;

        float FirstSpeed = speedList[0].sp;
        //对于startTime <0（对应音乐播放前的偏移），需要正确计算
        if (startTime < 0) 
        {
            // 减去偏移量(0s以前用speed列表的第一个speed
            totalDistance += startTime * FirstSpeed;
        }

        // 处理 startTime 之前的所有速度段
        foreach (var speed in speedList)
        {
            // 如果这一段还没到startTime
            if (speed.endT <= startTime)
            {
                //则加上这一段的所有长度
                totalDistance += (speed.endT - speed.startT) * speed.sp;
            }
            // 如果startTime在这一段里
            else if (speed.startT <= startTime)
            {
                //则加上这一段截至startTime的长度
                totalDistance += (startTime - speed.startT) * speed.sp;
            }
            else
            {
                break;
            }
        }

        //加上偏移量
        totalDistance += Offset;

        return -totalDistance * SpeedParams.NoteSpeedDefault;
    }


    public static void AdjustFlickArrowPosition(GameObject flickarrow, GameObject flick, float flickDirection)
    {
        Vector3 leftMiddleWorldPos = GetLeftMiddleWorldPosition(flick);
        Vector3 rightMiddleWorldPos = GetRightMiddleWorldPosition(flick);
        //Debug.Log(leftMiddleWorldPos);
        //Debug.Log(rightMiddleWorldPos);

        // 假设flickDirection是0-1之间的值，转换为0-360度的角度，根据实际调整
        float arrowRotationAngle = flickDirection * 360;
        //针对非横划（90度或270度），箭头应显示在水平平面内
        if (Math.Abs(arrowRotationAngle - 90) <= 1 | Math.Abs(arrowRotationAngle - 270) <= 1)
        {
            bool ifleft = Math.Abs(arrowRotationAngle - 90) <= 1;
            flickarrow.transform.rotation = Quaternion.Euler(-90, 0, arrowRotationAngle);
            // 设置Flick箭头实例的位置，使其在Flick的合适位置上
            if (ifleft)
            {
                //以Flick左侧坐标为锚点，往右平移
                Vector3 Pos = leftMiddleWorldPos;
                Vector3 PositionAdjust = new Vector3(0.8f, 0, 0);
                Vector3 newPosition = Pos + PositionAdjust;
                flickarrow.transform.position = newPosition;
            }
            else
            {
                //以Flick右侧坐标为锚点，往左平移
                Vector3 Pos = rightMiddleWorldPos;
                Vector3 PositionAdjust = new Vector3(-0.8f, 0, 0);
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

    public static float TransformYCoordinate(float startY)
    {
        Camera mainCamera = Camera.main;

        // 获取判定区下边缘和上边缘在屏幕空间中的像素坐标
        float bottomPixel = AspectRatioManager.croppedScreenHeight * (1 - HorizontalParams.VerticalMarginBottom) + (Screen.height - AspectRatioManager.croppedScreenHeight) / 2f;
        float topPixel = AspectRatioManager.croppedScreenHeight * (1 - HorizontalParams.VerticalMarginCeiling) + (Screen.height - AspectRatioManager.croppedScreenHeight) / 2f;

        // 计算 startY 对应的屏幕像素坐标
        float screenPixelY = bottomPixel + startY * (topPixel - bottomPixel);

        // 手动指定 z 轴的值为一个较大的值
        Vector3 screenPoint = new Vector3(Screen.width / 2, Screen.height - screenPixelY, 100);
        //Debug.Log(screenPoint);

        // 通过 ScreenToWorldPoint 得到射线上的一个点
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenPoint);
        //Debug.Log(worldPoint);

        // 计算射线
        Ray ray = new Ray(mainCamera.transform.position, (worldPoint - mainCamera.transform.position).normalized);

        // 计算射线与 z = 0 平面的交点
        Plane zZeroPlane = new Plane(Vector3.forward, Vector3.zero);
        float distance;
        if (zZeroPlane.Raycast(ray, out distance))
        {
            Vector3 intersectionPoint = ray.GetPoint(distance);
            //Debug.Log(intersectionPoint);
            return intersectionPoint.y;
        }
        else
        {
            Debug.LogError("射线未与 z = 0 平面相交");
            return 0f;
        }
    }

    public static Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");
        if (hex.Length == 6)
        {
            hex = hex + "FF"; // 不包含透明度时，在末尾添加默认不透明值
        }
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        byte a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

        // 将 Color32 转换为 Color
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    public static void ProcessCombinedInstance(GameObject combinedInstance, GameObject judgePlaneParent, int parentLayer)
    {
        // 将合并后的Instance设置为对应的父物体的子物体
        combinedInstance.transform.SetParent(judgePlaneParent.transform);
        // 继承父物体的图层
        combinedInstance.layer = parentLayer;

    }

    // 将 GradientColor 列表实例转换为 GradientColorListUnity 实例
    public static GradientColorListUnity ConvertToUnityList(List<GradientColor> list)
    {
        GradientColorListUnity unityList = new GradientColorListUnity();
        foreach (var gradientColor in list)
        {
            unityList.colors.Add(new GradientColorUnity
            {
                startT = gradientColor.startT,
                endT = gradientColor.endT,
                lowercolor = HexToColor(gradientColor.lowercolor),
                uppercolor = HexToColor(gradientColor.uppercolor)
            });
        }
        return unityList;
    }

    public static void SetSpriteColor(GameObject obj, Color color)
    {
        MyOutline outline = obj.GetComponent<MyOutline>();
        if (outline != null)
        {
            outline.OutlineColor = color;
        }
        else
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
    }


    public Dictionary<float, int> CalculateNoteDensity(Chart chart)
    {
        var densityMap = new Dictionary<float, int>();

        // 处理 Tap
        if (chart.taps != null)
        {
            foreach (var tap in chart.taps)
            {
                AddOrIncrement(densityMap, tap.startT, 1);
            }
        }

        // 处理 Slide
        if (chart.slides != null)
        {
            foreach (var slide in chart.slides)
            {
                AddOrIncrement(densityMap, slide.startT, 1);
            }
        }

        // 处理 Flick
        if (chart.flicks != null)
        {
            foreach (var flick in chart.flicks)
            {
                AddOrIncrement(densityMap, flick.startT, 1);
            }
        }

        // 处理 Hold
        if (chart.holds != null)
        {
            foreach (var hold in chart.holds)
            {
                foreach (var subHold in hold.subHoldList)
                {
                    float startT = subHold.startT;
                    float endT = subHold.endT;
                    float duration = endT - startT;
                    int intervals = (int)Math.Round(duration / 50f);

                    // 添加起始点
                    AddOrIncrement(densityMap, startT, 1);

                    // 添加中间点
                    for (int i = 1; i <= intervals; i++)
                    {
                        float timePoint = startT + (i * duration / intervals);
                        AddOrIncrement(densityMap, timePoint, 1);
                    }
                }
            }
        }

        // 处理 Star
        if (chart.stars != null)
        {
            foreach (var star in chart.stars)
            {
                // 星星头物量
                AddOrIncrement(densityMap, star.starHeadT, 1);

                // 完整星星物量（使用第一个子星星的开始时间）
                if (star.subStarList != null && star.subStarList.Count > 0)
                {
                    AddOrIncrement(densityMap, star.subStarList[0].starTrackStartT, 1);
                }
            }
        }

        return densityMap;
    }

    // 辅助方法：添加或增加字典中的值
    private void AddOrIncrement(Dictionary<float, int> dict, float key, int value)
    {
        if (dict.TryGetValue(key, out int existingValue))
        {
            dict[key] = existingValue + value;
        }
        else
        {
            dict[key] = value;
        }
    }


    

}


