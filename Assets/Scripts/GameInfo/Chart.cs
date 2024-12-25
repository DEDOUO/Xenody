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

// 谱面文件类
public class Chart
{
    [JsonProperty("judgePlanes")]
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
        var chartData = new
        {
            judgePlanes,
            taps,
            holds,
            slides,
            flicks,
            stars
        };

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
                foreach (var subHold in holdData.subHoldList)
                {
                    newHold.AddSubHold((float)subHold.startT, (float)subHold.startXMin, (float)subHold.startXMax, (float)subHold.endT, (float)subHold.endXMin, (float)subHold.endXMax, (Utility.TransFunctionType)subHold.XLeftFunction, (Utility.TransFunctionType)subHold.XRightFunction);
                }
                newHold.associatedPlaneId = (int)holdData.associatedPlaneId;
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
                newStar.starHeadT = (float)starData.starHeadT;
                foreach (var subStar in starData.subStarList)
                {
                    newStar.AddSubStar((float)subStar.starTrackStartT, (float)subStar.starTrackEndT, (float)subStar.startX, (float)subStar.startY, (float)subStar.endX, (float)subStar.endY, (Utility.TrackFunctionType)subStar.trackFunction);
                }
                chart.stars.Add(newStar);
            }

            return chart;
        }

        Debug.LogError("指定的谱面文件不存在！");
        return null;
    }
}