using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Params;
//using static Note.Star;
using static Utility;
using Note;
using UnityEngine.XR.OpenXR.Input;



public class ScoreManager : MonoBehaviour
{
    public Dictionary<float, int> comboMap;
    public Dictionary<float, int> SumComboMap;
    public Dictionary<float, float> weightMap;
    public float totalWeight;
    public Dictionary<float, int> SumScoreMap;
    public Dictionary<float, List<Vector2>> JudgePosMap;
    public GameObject JudgeTexturesParent;

    private void Awake()
    {
        // 初始化字典（建议在Awake中，确保早于其他脚本调用）
        comboMap = new Dictionary<float, int>();
        SumComboMap = new Dictionary<float, int>();
        weightMap = new Dictionary<float, float>();
        totalWeight = 0f;
        SumScoreMap = new Dictionary<float, int>();
        JudgePosMap = new Dictionary<float, List<Vector2>>();

}


    public void CalculateAutoPlayScores(Chart chart)
    {
        ChartInstantiator instantiator = GetComponent<ChartInstantiator>();
        JudgeTexturesParent = instantiator.JudgeTexturesParent;
        RectTransform ParentRect = JudgeTexturesParent.GetComponent<RectTransform>();

        // 处理 Tap
        if (chart.taps != null)
        {
            //tapWeight += chart.taps.Count;
            foreach (var tap in chart.taps)
            {
                Vector2 Pos = new Vector2(tap.startX, tap.startY);
                Vector2 PosScreen = ScalePositionToScreenStar(Pos, ParentRect);
                AddToMaps(tap.startT, PosScreen, 1, ScoreParams.TapScoreWeight);
            }
        }

        // 处理 Flick
        if (chart.flicks != null)
        {
            //flickWeight += chart.flicks.Count;
            foreach (var flick in chart.flicks)
            {
                Vector2 Pos = new Vector2(flick.startX, flick.startY);
                Vector2 PosScreen = ScalePositionToScreenStar(Pos, ParentRect);
                AddToMaps(flick.startT, PosScreen, 1, ScoreParams.FlickScoreWeight);
            }
        }

        // 处理 Slide
        if (chart.slides != null)
        {
            //slideWeight += chart.slides.Count;
            foreach (var slide in chart.slides)
            {
                Vector2 Pos = new Vector2(slide.startX, slide.startY);
                Vector2 PosScreen = ScalePositionToScreenStar(Pos, ParentRect);
                AddToMaps(slide.startT, PosScreen, 1, ScoreParams.SlideScoreWeight);
            }
        }

        // 处理 Hold
        if (chart.holds != null)
        {
            foreach (var hold in chart.holds)
            {
                //Debug.Log(hold.holdId);
                foreach (var subHold in hold.subHoldList)
                {
                    
                    float startT = subHold.startT;
                    float endT = subHold.endT;
                    //Debug.Log(startT);
                    float duration = endT - startT;
                    //Debug.Log(duration);
                    int intervals = (int)Math.Round(duration / ChartParams.HoldJudgeTimeInterval);
                    //Debug.Log(subHold.yAxisFunction);

                    Vector2 Pos = new Vector2((subHold.startXMax + subHold.startXMin)/2, subHold.startY);
                    Vector2 PosScreen = ScalePositionToScreenStar(Pos, ParentRect);

                    // 起始点权重
                    //holdWeight += 1;
                    //Debug.Log($"{startT}, {PosScreen}");
                    AddToMaps(startT, PosScreen, 1, ScoreParams.HoldScoreWeight);

                    // 中间点权重
                    for (int i = 1; i <= intervals; i++)
                    {
                        float timePoint = (float)Math.Round(startT + (i * duration / intervals), 3);

                        float x = (CalculatePosition(timePoint, startT, subHold.startXMax, endT, subHold.endXMax, subHold.XRightFunction) + CalculatePosition(timePoint, startT, subHold.startXMin, endT, subHold.endXMin, subHold.XLeftFunction))/2;
                        float y = CalculatePosition(timePoint, startT, subHold.startY, endT, subHold.endY, subHold.yAxisFunction);
                        Vector2 Pos2 = new Vector2(x, y);
                        //Debug.Log(Pos2);
                        Vector2 PosScreen2 = ScalePositionToScreenStar(Pos2, ParentRect);
                        //holdWeight += 1;
                        AddToMaps(timePoint, PosScreen2, 1, ScoreParams.HoldScoreWeight);
                    }
                }
            }
        }

        // 处理 Star
        if (chart.stars != null)
        {
            foreach (var star in chart.stars)
            {
                // 星星头权重
                //starHeadWeight += 1;
                Star.SubStar firstStar = star.subStarList[0];
                Vector2 Pos = new Vector2(firstStar.startX, firstStar.startY);
                Vector2 PosScreen = ScalePositionToScreenStar(Pos, ParentRect);
                AddToMaps(star.starHeadT, PosScreen, 1, ScoreParams.StarHeadScoreWeight);

                // 完整星星权重（使用最后一个子星星的结束时间）
                if (star.subStarList != null && star.subStarList.Count > 0)
                {
                    //starFullWeight += 1;
                    Star.SubStar lastStar = star.subStarList[star.subStarList.Count - 1];
                    Vector2 Pos2 = new Vector2(lastStar.endX, lastStar.endY);
                    Vector2 PosScreen2 = ScalePositionToScreenStar(Pos2, ParentRect);
                    AddToMaps(lastStar.starTrackEndT, PosScreen2, 1, ScoreParams.StarScoreWeight);
                }
            }
        }

        //对JudgePosMap按照时间顺序排序
        if (JudgePosMap == null || JudgePosMap.Count <= 1)
            return; // 无需排序

        // 1. 创建临时有序列表（按 key 升序）
        var sortedEntries = JudgePosMap
            .OrderBy(kv => kv.Key)
            .ToList();

        // 2. 清空原字典
        JudgePosMap.Clear();

        // 3. 按序重新插入元素
        foreach (var entry in sortedEntries)
        {
            JudgePosMap[entry.Key] = entry.Value;
        }


        // 计算总权重
        totalWeight = weightMap.Values.Sum();

        // 避免除零错误
        if (totalWeight <= 0)
        {
            Debug.LogError("Total weight is zero!");
            return;
        }

        // 计算单位权重对应的分数
        double scorePerWeight = ScoreParams.TotalScore / totalWeight;
        //Debug.Log(scorePerWeight);
        // 获取按时间排序的所有时间点
        var sortedTimes = weightMap.Keys.OrderBy(t => t).ToList();
        int accumulatedCombo = 0;
        double accumulatedScore = 0f;

        // 计算每个时间点的累积连击数和分数
        foreach (var time in sortedTimes)
        {
            int currentCombo = comboMap[time];
            accumulatedCombo += currentCombo;

            // 使用Mathf.Round确保分数为整数
            SumComboMap[time] = accumulatedCombo;

            float currentWeight = weightMap[time];
            double currentScore = currentWeight * scorePerWeight;
            accumulatedScore += currentScore;
            //Debug.Log(currentWeight);
            //Debug.Log(accumulatedScore);
            // 使用Mathf.Round确保分数为整数
            SumScoreMap[time] = (int)System.Math.Round(accumulatedScore);
        }

        return;
    }

    // 辅助方法：同时更新密度和得分映射
    private void AddToMaps(float time, Vector2 PosScreen, int Combo, float ScoreWeight)
    {

        //Debug.Log($"{time}, {PosScreen}");

        // 密度映射累加
        if (comboMap.TryGetValue(time, out int c))
        {
            comboMap[time] = c + Combo;
        }
        else
        {
            comboMap[time] = Combo;
        }

        // 得分映射累加
        if (weightMap.TryGetValue(time, out float s))
        {
            weightMap[time] = s + ScoreWeight;
        }
        else
        {
            weightMap[time] = ScoreWeight;
        }

        //判定文本的的位置需要沿y轴向上偏移
        PosScreen.y += JudgeTextureParams.YAxisOffset;


        // 时间映射到判定点坐标列表
        if (!JudgePosMap.TryGetValue(time, out List<Vector2> list))
        {
            list = new List<Vector2>(); // 不存在则创建新列表
            JudgePosMap[time] = list;
        }
        //Debug.Log(list);
        list.Add(PosScreen); // 将新坐标追加到列表
    }

}