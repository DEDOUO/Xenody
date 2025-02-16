using UnityEngine;
using System.IO;

// 用于管理歌曲和谱面文件路径等相关数据的类
public static class SongAndChartData
{
    private static string selectedSongName; // 选择的歌曲名称
    private static string songFolderPath;   // 歌曲文件夹的完整路径
    private static string musicFilePath;   // 音乐文件的完整路径
    private static string chartFilePath;  // 谱面文件的完整路径

    // 设置选择的歌曲名称，并初始化对应的文件路径
    public static void SetSelectedSong(string songName)
    {
        selectedSongName = songName;

        // 更新路径为 StreamingAssets 文件夹下的路径
        songFolderPath = Path.Combine(Application.streamingAssetsPath, "Songs", selectedSongName);
        musicFilePath = Path.Combine(songFolderPath, "Music.mp3");
        chartFilePath = Path.Combine(songFolderPath, "Chart.json");
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

    // 获取 Cover.jpg 并将其处理为 Sprite 的方法
    public static Sprite GetCoverSprite()
    {
        string coverFilePath = Path.Combine(songFolderPath, "Cover.jpg");

        // 检查文件是否存在
        if (!File.Exists(coverFilePath))
        {
            Debug.LogError($"Cover file not found at {coverFilePath}");
            return null;
        }

        // 从 StreamingAssets 加载 Cover.jpg
        byte[] fileData = File.ReadAllBytes(coverFilePath);
        Texture2D texture = new Texture2D(2, 2); // 创建一个空的 Texture2D
        texture.LoadImage(fileData); // 加载图片数据到 Texture2D

        if (texture == null)
        {
            Debug.LogError($"Failed to load texture from {coverFilePath}");
            return null;
        }

        // 将 Texture2D 转换为 Sprite
        Sprite coverSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        return coverSprite;
    }
}