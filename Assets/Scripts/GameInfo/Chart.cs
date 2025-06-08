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
//using Params;

public class Chart
{
    [JsonProperty("speed")]
    // 判定面实例列表，用于存储所有判定面信息
    public List<Speed> speedList;
    [JsonProperty("color")]
    // 判定面实例列表，用于存储所有判定面信息
    public List<GradientColor> gradientColorList;
    [JsonProperty("plane")]
    // 判定面实例列表，用于存储所有判定面信息
    public List<JudgePlane> judgePlanes;
    [JsonProperty("tap")]
    // Tap实例列表
    public List<Tap> taps;
    [JsonProperty("hold")]
    // Hold实例列表
    public List<Hold> holds;
    [JsonProperty("slide")]
    // Slide实例列表
    public List<Slide> slides;
    [JsonProperty("flick")]
    // Flick实例列表
    public List<Flick> flicks;
    [JsonProperty("star")]
    // Star实例列表
    public List<Star> stars;

    // 导出谱面文件为JSON格式
    public void ExportToJson(string filePath)
    {
        // 创建一个包含所有键型信息的匿名对象，用于序列化
        var chartData = new { speedList, gradientColorList, judgePlanes, taps, holds, slides, flicks, stars };

        string json = JsonConvert.SerializeObject(chartData, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    // 从JSON文件导入谱面信息
    public static Chart ImportFromJson(string filePath)
    {
        if (File.Exists(filePath))
        {
            //Debug.Log(filePath);
            string json = File.ReadAllText(filePath);
            //Debug.Log(json);
            Chart chart = JsonConvert.DeserializeObject<Chart>(json);
            return chart;
        }

        Debug.LogError("指定的谱面文件不存在！");
        return null;
    }

    public JudgePlane GetCorrespondingJudgePlane(string judgeLineName)
    {
        int judgePlaneId;
        if (int.TryParse(judgeLineName.Replace("JudgeLine", ""), out judgePlaneId))
        {
            if (judgePlanes != null)
            {
                foreach (var judgePlane in judgePlanes)
                {
                    if (judgePlane.id == judgePlaneId)
                    {
                        return judgePlane;
                    }
                }
            }
        }
        return null;
    }

    public JudgePlane GetCorrespondingJudgePlane(int judgePlaneId)
    {
        if (judgePlanes != null)
        {
            foreach (var judgePlane in judgePlanes)
            {
                if (judgePlane.id == judgePlaneId)
                {
                    return judgePlane;
                }
            }
        }
        return null;
    }


}