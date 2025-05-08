//using UnityEngine;
//using System.Collections.Generic;
//using static Utility;
using Newtonsoft.Json;
//using Params;
//using System.Collections;

// 判定面类
public class Speed
{
    [JsonProperty("startT")]
    public float startT;
    [JsonProperty("endT")]
    public float endT;
    // 从startT到endT的速度（相对基准速度的倍率）
    [JsonProperty("sp")]
    public float sp;

}

