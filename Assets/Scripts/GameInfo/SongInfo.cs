using Newtonsoft.Json;

public class SongInfo
{
    [JsonProperty("song")]
    public SongData song;

    [JsonProperty("folderName")] // 新增：文件夹名
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
