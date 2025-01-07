using System.Collections.Generic;
//using System.Diagnostics;
using System.IO;
//using System.Linq;
//using System.Numerics;
//using System.Xml;
using Newtonsoft.Json;
using UnityEngine;
//using static Utility;
using Note;
using Params;

// 谱面文件类
public class Chart
{
    [JsonProperty("planes")]
    // 判定面实例列表，用于存储所有判定面信息
    public List<JudgePlane> judgePlanes;
    [JsonProperty("taps")]
    // Tap实例列表
    public List<Tap> taps;
    [JsonProperty("holds")]
    // Hold实例列表
    public List<Hold> holds;
    [JsonProperty("slides")]
    // Slide实例列表
    public List<Slide> slides;
    [JsonProperty("flicks")]
    // Flick实例列表
    public List<Flick> flicks;
    [JsonProperty("stars")]
    // Star实例列表
    public List<Star> stars;

    // 导出谱面文件为JSON格式
    public void ExportToJson(string filePath)
    {
        // 创建一个包含所有键型信息的匿名对象，用于序列化
        var chartData = new { judgePlanes, taps, holds, slides, flicks, stars };

        string json = JsonConvert.SerializeObject(chartData, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    // 从JSON文件导入谱面信息
    public static Chart ImportFromJson(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            //Debug.Log(json);
            Chart chartData = JsonConvert.DeserializeObject<Chart>(json);
            //Debug.Log(chartData);

            var chart = new Chart();

            // 反序列化判定面信息
            chart.judgePlanes = new List<JudgePlane>();
            foreach (var judgePlaneData in chartData.judgePlanes)
            {
                var newJudgePlane = new JudgePlane((int)judgePlaneData.id);
                foreach (var subPlane in judgePlaneData.subJudgePlaneList)
                {
                    newJudgePlane.AddSubJudgePlane((float)subPlane.startT, (float)subPlane.startY, (float)subPlane.endT, (float)subPlane.endY, (Utility.TransFunctionType)subPlane.yAxisFunction);
                }
                chart.judgePlanes.Add(newJudgePlane);
            }

            // 反序列化Tap信息
            chart.taps = new List<Tap>();
            foreach (var tap in chartData.taps)
            {
                var newTap = new Tap();
                newTap.startT = (float)tap.startT;
                newTap.startX = (float)tap.startX;
                newTap.noteSize = (float)tap.noteSize;
                newTap.associatedPlaneId = (int)tap.associatedPlaneId;
                chart.taps.Add(newTap);
            }

            // 反序列化Hold信息
            chart.holds = new List<Hold>();
            foreach (var holdData in chartData.holds)
            {
                var newHold = new Hold();
                newHold.associatedPlaneId = (int)holdData.associatedPlaneId;
                newHold.holdId = (int)holdData.holdId;
                foreach (var subHold in holdData.subHoldList)
                {
                    newHold.AddSubHold((float)subHold.startT, (float)subHold.startXMin, (float)subHold.startXMax, (float)subHold.endT, (float)subHold.endXMin, (float)subHold.endXMax, (Utility.TransFunctionType)subHold.XLeftFunction, (Utility.TransFunctionType)subHold.XRightFunction);
                }
                chart.holds.Add(newHold);
            }

            // 反序列化Slide信息
            chart.slides = new List<Slide>();
            foreach (var slide in chartData.slides)
            {
                var newSlide = new Slide();
                newSlide.startT = (float)slide.startT;
                newSlide.startX = (float)slide.startX;
                newSlide.noteSize = (float)slide.noteSize;
                newSlide.associatedPlaneId = (int)slide.associatedPlaneId;
                chart.slides.Add(newSlide);
            }

            // 反序列化Flick信息
            chart.flicks = new List<Flick>();
            foreach (var flick in chartData.flicks)
            {
                var newFlick = new Flick();
                newFlick.startT = (float)flick.startT;
                newFlick.startX = (float)flick.startX;
                newFlick.noteSize = (float)flick.noteSize;
                newFlick.flickDirection = (float)flick.flickDirection;
                newFlick.associatedPlaneId = (int)flick.associatedPlaneId;
                chart.flicks.Add(newFlick);
            }

            // 反序列化Star信息
            chart.stars = new List<Star>();
            foreach (var starData in chartData.stars)
            {
                var newStar = new Star();
                newStar.associatedPlaneId = (int)starData.associatedPlaneId;
                newStar.starId = (int)starData.starId;
                newStar.starHeadT = (float)starData.starHeadT;

                // 找到对应的 JudgePlane
                JudgePlane associatedJudgePlane = null;
                foreach (var judgePlane in chart.judgePlanes)
                {
                    if (judgePlane.id == starData.associatedPlaneId)
                    {
                        associatedJudgePlane = judgePlane;
                        break;
                    }
                }
                if (associatedJudgePlane != null)
                {
                    float startY = associatedJudgePlane.GetPlaneYAxis(newStar.starHeadT) / HeightParams.HeightDefault;
                    //Debug.Log(startY);
                    bool firstSubStar = true;
                    foreach (var subStar in starData.subStarList)
                    {
                        //每个Star的第一个SubStar的Y轴坐标与所在JudgePlane的Y轴坐标保持一致
                        if (firstSubStar)
                        {
                            newStar.AddSubStar((float)subStar.starTrackStartT, (float)subStar.starTrackEndT, (float)subStar.startX, startY, (float)subStar.endX, (float)subStar.endY, (Utility.TrackFunctionType)subStar.trackFunction);
                            firstSubStar = false;
                        }
                        else
                        {
                            newStar.AddSubStar((float)subStar.starTrackStartT, (float)subStar.starTrackEndT, (float)subStar.startX, (float)subStar.startY, (float)subStar.endX, (float)subStar.endY, (Utility.TrackFunctionType)subStar.trackFunction);
                        }
                    }
                }
                chart.stars.Add(newStar);
            }

            return chart;
        }

        Debug.LogError("指定的谱面文件不存在！");
        return null;
    }
}