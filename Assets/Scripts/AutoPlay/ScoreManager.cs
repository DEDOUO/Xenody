using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Params;


public class ScoreManager : MonoBehaviour
{
    public Dictionary<float, int> comboMap;
    public Dictionary<float, int> SumComboMap;
    public Dictionary<float, float> weightMap;
    public float totalWeight;
    public Dictionary<float, int> SumScoreMap;

    private void Awake()
    {
        // 初始化字典（建议在Awake中，确保早于其他脚本调用）
        comboMap = new Dictionary<float, int>();
        SumComboMap = new Dictionary<float, int>();
        weightMap = new Dictionary<float, float>();
        totalWeight = 0f;
        SumScoreMap = new Dictionary<float, int>();
    }


    public void CalculateAutoPlayScores(Chart chart)
    {

        // 处理 Tap
        if (chart.taps != null)
        {
            //tapWeight += chart.taps.Count;
            foreach (var tap in chart.taps)
            {
                AddToMaps(tap.startT, 1, ScoreParams.TapScoreWeight);
            }
        }

        // 处理 Flick
        if (chart.flicks != null)
        {
            //flickWeight += chart.flicks.Count;
            foreach (var flick in chart.flicks)
            {
                AddToMaps(flick.startT, 1, ScoreParams.FlickScoreWeight);
            }
        }

        // 处理 Slide
        if (chart.slides != null)
        {
            //slideWeight += chart.slides.Count;
            foreach (var slide in chart.slides)
            {
                AddToMaps(slide.startT, 1, ScoreParams.SlideScoreWeight);
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
                    float duration = subHold.endT - subHold.startT;
                    //Debug.Log(duration);
                    int intervals = (int)Math.Round(duration / ChartParams.HoldJudgeTimeInterval);
                    //Debug.Log(intervals);

                    // 起始点权重
                    //holdWeight += 1;
                    AddToMaps(startT, 1, ScoreParams.HoldScoreWeight);

                    // 中间点权重
                    for (int i = 1; i <= intervals; i++)
                    {
                        float timePoint = (float)Math.Round(startT + (i * duration / intervals), 3);
                        //holdWeight += 1;
                        AddToMaps(timePoint, 1, ScoreParams.HoldScoreWeight);
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
                AddToMaps(star.starHeadT, 1, ScoreParams.StarHeadScoreWeight);

                // 完整星星权重（使用最后一个子星星的结束时间）
                if (star.subStarList != null && star.subStarList.Count > 0)
                {
                    //starFullWeight += 1;
                    AddToMaps(star.subStarList[star.subStarList.Count-1].starTrackEndT, 1, ScoreParams.StarScoreWeight);
                }
            }
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
    private void AddToMaps(float time, int Combo, float ScoreWeight)
    {
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
    }

}