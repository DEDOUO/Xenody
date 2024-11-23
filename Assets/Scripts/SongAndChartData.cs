//using System;
//using UnityEngine;

// 用于管理歌曲和谱面文件路径等相关数据的类
public static class SongAndChartData
{
    private static string selectedSongName;
    private static string songFolderPath;
    private static string songFolderRelativePath;
    private static string musicFilePath;
    private static string chartFilePath;

    // 设置选择的歌曲名称，并初始化对应的文件路径
    public static void SetSelectedSong(string songName)
    {
        selectedSongName = songName;
        songFolderPath = $"Assets/Resources/Songs/{selectedSongName}";
        songFolderRelativePath = $"Songs/{selectedSongName}";
        //注意音频采用Resources方法读取，需要Resources文件夹下的相对路径
        musicFilePath = $"{songFolderRelativePath}/Music.mp3";
        chartFilePath = $"{songFolderPath}/Chart.json";
    }

    // 获取歌曲音频文件的路径
    public static string GetMusicFilePath()
    {
        return musicFilePath;
    }

    // 获取谱面文件的路径
    public static string GetChartFilePath()
    {
        return chartFilePath;
    }
}