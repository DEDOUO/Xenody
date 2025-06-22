using Newtonsoft.Json;

public class SongInfo
{
    [JsonProperty("song")]
    public SongData song;

    [JsonProperty("folderName")]
    public string folderName;

    [JsonProperty("charts")]
    public ChartInfo[] charts;
}

public class SongData
{
    [JsonProperty("id")]
    public int id;

    [JsonProperty("title")]
    public string title;

    [JsonProperty("artist")]
    public string artist;

    [JsonProperty("bpm")]
    public float bpm;

    [JsonProperty("time")]
    public string time;

    // 新增：预览时间范围
    [JsonProperty("previewStart")]
    public float previewStart;

    [JsonProperty("previewEnd")]
    public float previewEnd;
}


public class ChartInfo
{
    [JsonProperty("difficulty")]
    public int difficulty;

    [JsonProperty("level")]
    public string level;

    [JsonProperty("prec")]
    public float prec;

    [JsonProperty("charter")]
    public string charter;
}