using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Params;
using static Utility;
using Note;
using UnityEngine.XR.OpenXR.Input;

public class ScoreManager : MonoBehaviour
{
    // 单例模式实现
    public static ScoreManager instance;

    public Dictionary<float, int> comboMap;
    public Dictionary<float, int> SumComboMap;
    public Dictionary<float, float> weightMap;
    public float totalWeight;
    public Dictionary<float, int> SumScoreMap;
    public Dictionary<float, List<Vector2>> JudgePosMap;
    public GameObject JudgeTexturesParent;

    private void Awake()
    {
        // 单例模式初始化
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 防止场景切换时被销毁
        }
        else
        {
            // 如果已有实例，销毁当前实例
            Destroy(gameObject);
            return;
        }

        // 初始化字典（建议在Awake中，确保早于其他脚本调用）
        InitializeDictionaries();
    }

    // 新增字典初始化方法，方便重复调用
    private void InitializeDictionaries()
    {
        comboMap = new Dictionary<float, int>();
        SumComboMap = new Dictionary<float, int>();
        weightMap = new Dictionary<float, float>();
        totalWeight = 0f;
        SumScoreMap = new Dictionary<float, int>();
        JudgePosMap = new Dictionary<float, List<Vector2>>();
    }

    public void CalculateAutoPlayScores(Chart chart)
    {
        // 每次调用先重新初始化相关字典数据
        InitializeDictionaries();

        // 检查Chart是否为空
        if (chart == null)
        {
            Debug.LogError("Chart数据为空，无法计算得分！");
            return;
        }

        // 通过场景查找ChartInstantiator组件（推荐方案）
        ChartInstantiator instantiator = FindObjectOfType<ChartInstantiator>();
        if (instantiator == null)
        {
            Debug.LogError("未找到ChartInstantiator组件，无法计算得分！");
            return;
        }

        // 获取并检查JudgeTexturesParent
        JudgeTexturesParent = instantiator.JudgeTexturesParent;
        if (JudgeTexturesParent == null)
        {
            Debug.LogError("JudgeTexturesParent为空，无法计算得分！");
            return;
        }

        // 获取并检查RectTransform
        RectTransform ParentRect = JudgeTexturesParent.GetComponent<RectTransform>();
        if (ParentRect == null)
        {
            Debug.LogError("JudgeTexturesParent缺少RectTransform组件！");
            return;
        }

        // 处理 Tap
        if (chart.taps != null)
        {
            foreach (var tap in chart.taps)
            {
                Vector2 Pos = new Vector2(tap.startX, tap.startY);
                Vector2 PosScreen = ScalePositionToScreen(Pos, ParentRect);
                AddToMaps(tap.startT, PosScreen, 1, ScoreParams.TapScoreWeight);
            }
        }

        // 处理 Flick
        if (chart.flicks != null)
        {
            foreach (var flick in chart.flicks)
            {
                Vector2 Pos = new Vector2(flick.startX, flick.startY);
                Vector2 PosScreen = ScalePositionToScreen(Pos, ParentRect);
                AddToMaps(flick.startT, PosScreen, 1, ScoreParams.FlickScoreWeight);
            }
        }

        // 处理 Slide
        if (chart.slides != null)
        {
            foreach (var slide in chart.slides)
            {
                Vector2 Pos = new Vector2(slide.startX, slide.startY);
                Vector2 PosScreen = ScalePositionToScreen(Pos, ParentRect);
                AddToMaps(slide.startT, PosScreen, 1, ScoreParams.SlideScoreWeight);
            }
        }

        // 处理 Hold
        if (chart.holds != null)
        {
            foreach (var hold in chart.holds)
            {
                // 检查subHoldList是否为空
                if (hold.subHoldList == null || hold.subHoldList.Count == 0)
                {
                    Debug.LogWarning($"Hold ID: {hold.holdId} 的subHoldList为空，跳过处理");
                    continue;
                }

                foreach (var subHold in hold.subHoldList)
                {
                    // 检查subHold数据完整性
                    if (subHold.startT >= subHold.endT)
                    {
                        Debug.LogWarning($"Hold ID: {hold.holdId} 的subHold时间无效: startT={subHold.startT}, endT={subHold.endT}");
                        continue;
                    }

                    float startT = subHold.startT;
                    float endT = subHold.endT;
                    float duration = endT - startT;

                    // 确保间隔数至少为1
                    int intervals = Mathf.Max(1, (int)Math.Round(duration / ChartParams.HoldJudgeTimeInterval));

                    Vector2 Pos = new Vector2((subHold.startXMax + subHold.startXMin) / 2, subHold.startY);
                    Vector2 PosScreen = ScalePositionToScreen(Pos, ParentRect);

                    // 起始点权重
                    AddToMaps(startT, PosScreen, 1, ScoreParams.HoldScoreWeight);

                    // 中间点权重
                    for (int i = 1; i <= intervals; i++)
                    {
                        float timePoint = (float)Math.Round(startT + (i * duration / intervals), 3);

                        try
                        {
                            float x = (CalculatePosition(timePoint, startT, subHold.startXMax, endT, subHold.endXMax, subHold.XRightFunction) +
                                      CalculatePosition(timePoint, startT, subHold.startXMin, endT, subHold.endXMin, subHold.XLeftFunction)) / 2;
                            float y = CalculatePosition(timePoint, startT, subHold.startY, endT, subHold.endY, subHold.yAxisFunction);
                            Vector2 Pos2 = new Vector2(x, y);
                            Vector2 PosScreen2 = ScalePositionToScreen(Pos2, ParentRect);
                            AddToMaps(timePoint, PosScreen2, 1, ScoreParams.HoldScoreWeight);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"计算Hold中间点时出错 (Hold ID: {hold.holdId}, 时间点: {timePoint}): {e.Message}");
                        }
                    }
                }
            }
        }

        // 处理 Star
        if (chart.stars != null)
        {
            foreach (var star in chart.stars)
            {
                // 检查subStarList是否有效
                if (star.subStarList == null || star.subStarList.Count == 0)
                {
                    Debug.LogWarning($"Star的subStarList为空，跳过处理");
                    continue;
                }

                // 星星头权重
                try
                {
                    Star.SubStar firstStar = star.subStarList[0];
                    Vector2 Pos = new Vector2(firstStar.startX, firstStar.startY);
                    Vector2 PosScreen = ScalePositionToScreen(Pos, ParentRect);
                    AddToMaps(star.starHeadT, PosScreen, 1, ScoreParams.StarHeadScoreWeight);
                }
                catch (Exception e)
                {
                    Debug.LogError($"处理Star头部时出错: {e.Message}");
                    continue;
                }

                // 完整星星权重
                try
                {
                    Star.SubStar lastStar = star.subStarList[star.subStarList.Count - 1];
                    Vector2 Pos2 = new Vector2(lastStar.endX, lastStar.endY);
                    Vector2 PosScreen2 = ScalePositionToScreen(Pos2, ParentRect);
                    AddToMaps(lastStar.starTrackEndT, PosScreen2, 1, ScoreParams.StarScoreWeight);
                }
                catch (Exception e)
                {
                    Debug.LogError($"处理Star尾部时出错: {e.Message}");
                }
            }
        }

        // 对JudgePosMap按照时间顺序排序
        if (JudgePosMap == null || JudgePosMap.Count <= 1)
            return; // 无需排序

        try
        {
            // 创建临时有序列表（按 key 升序）
            var sortedEntries = JudgePosMap
               .OrderBy(kv => kv.Key)
               .ToList();

            // 清空原字典
            JudgePosMap.Clear();

            // 按序重新插入元素
            foreach (var entry in sortedEntries)
            {
                JudgePosMap[entry.Key] = entry.Value;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"排序JudgePosMap时出错: {e.Message}");
            return;
        }

        // 计算总权重
        try
        {
            totalWeight = weightMap.Values.Sum();
        }
        catch (Exception e)
        {
            Debug.LogError($"计算总权重时出错: {e.Message}");
            return;
        }

        // 避免除零错误
        if (totalWeight <= 0)
        {
            Debug.LogError("Total weight is zero!");
            return;
        }

        // 计算单位权重对应的分数
        double scorePerWeight = ScoreParams.TotalScore / totalWeight;

        try
        {
            // 获取按时间排序的所有时间点
            var sortedTimes = weightMap.Keys.OrderBy(t => t).ToList();
            int accumulatedCombo = 0;
            double accumulatedScore = 0f;

            // 计算每个时间点的累积连击数和分数
            foreach (var time in sortedTimes)
            {
                // 确保comboMap包含当前时间点
                if (!comboMap.ContainsKey(time))
                {
                    Debug.LogWarning($"comboMap不包含时间点: {time}，使用默认值0");
                    continue;
                }

                int currentCombo = comboMap[time];
                accumulatedCombo += currentCombo;
                SumComboMap[time] = accumulatedCombo;

                float currentWeight = weightMap[time];
                double currentScore = currentWeight * scorePerWeight;
                accumulatedScore += currentScore;
                SumScoreMap[time] = (int)System.Math.Round(accumulatedScore);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"计算分数时出错: {e.Message}");
        }
    }

    // 辅助方法：同时更新密度和得分映射
    private void AddToMaps(float time, Vector2 PosScreen, int Combo, float ScoreWeight)
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

        // 判定文本的的位置需要沿y轴向上偏移
        PosScreen.y += JudgeTextureParams.YAxisOffset;

        // 时间映射到判定点坐标列表
        if (!JudgePosMap.TryGetValue(time, out List<Vector2> list))
        {
            list = new List<Vector2>(); // 不存在则创建新列表
            JudgePosMap[time] = list;
        }
        list.Add(PosScreen); // 将新坐标追加到列表
    }
}