using UnityEngine;


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
        // 注意音频采用 Resources 方法读取，需要 Resources 文件夹下的相对路径
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


    // 获取 Cover.jpg 并将其处理为 Sprite 的方法
    public static Sprite GetCoverSprite()
    {
        string coverFilePath = $"{songFolderPath}/Cover.jpg";

        // 加载 Cover.jpg 资源
        Texture2D texture = Resources.Load<Texture2D>(coverFilePath.Substring(coverFilePath.IndexOf("Resources/") + "Resources/".Length).Replace(".jpg", ""));

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