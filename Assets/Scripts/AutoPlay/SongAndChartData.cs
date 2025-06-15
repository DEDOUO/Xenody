using UnityEngine;
using System.IO;
using ClosedXML.Excel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System;
//using DocumentFormat.OpenXml.Drawing.Charts;
//using DocumentFormat.OpenXml.Drawing.Diagrams;
//using DocumentFormat.OpenXml.Spreadsheet;

public static class SongAndChartData
{
    private static Dictionary<string, SongInfo> songInfoDict = new Dictionary<string, SongInfo>();
    private static Dictionary<int, SongInfo> songIdDict = new Dictionary<int, SongInfo>(); // 新增：按ID索引
    public static int selectedDifficulty = 1; // 新增：当前选择的难度

    private static string selectedSongName;
    private static string selectedFolderName; // 新增：选中的文件夹名
    private static string songFolderPath;
    private static string musicFilePath;
    private static string chartFilePath;
    private static string excelFilePath;

    // 修改SetSelectedSong方法，添加难度参数
    public static void SetSelectedSong(string songName, string folderName, int difficulty)
    {
        selectedSongName = songName;
        selectedFolderName = folderName;
        selectedDifficulty = difficulty; // 保存选择的难度

        // 构建歌曲文件夹路径
        songFolderPath = Path.Combine(Application.streamingAssetsPath, "Songs", folderName);

        // 构建谱面文件路径（按难度命名：Chart+难度+.json）
        musicFilePath = Path.Combine(songFolderPath, "Music.mp3");
        chartFilePath = Path.Combine(songFolderPath, $"Chart{difficulty}.json");
        excelFilePath = Path.Combine(songFolderPath, $"Chart{difficulty}.xlsx");
        

        // 检查谱面文件是否存在
        if (!File.Exists(chartFilePath) & !File.Exists(excelFilePath))
        {
            // 谱面不存在，清空选择状态并提示
            selectedSongName = null;
            selectedFolderName = null;
            selectedDifficulty = 1;
            Debug.LogError($"谱面不存在: {chartFilePath}");
            //ShowToast("谱面不存在！"); // 显示提示（需自行实现该方法）
        }
    }

    // 添加歌曲信息（自动按ID索引）
    public static void AddSongInfo(string songName, SongInfo info)
    {
        if (!songInfoDict.ContainsKey(songName))
        {
            songInfoDict.Add(songName, info);
            if (info.song != null && !songIdDict.ContainsKey(info.song.id))
            {
                songIdDict.Add(info.song.id, info);
            }
        }
    }

    // 按ID获取歌曲信息
    public static SongInfo GetSongInfoById(int id)
    {
        if (songIdDict.TryGetValue(id, out SongInfo info))
        {
            return info;
        }
        return null;
    }

    // 获取当前选中的歌曲信息
    public static SongInfo GetSelectedSongInfo()
    {
        if (string.IsNullOrEmpty(selectedSongName) || !songInfoDict.ContainsKey(selectedSongName))
        {
            Debug.LogError("未选择有效歌曲或歌曲信息不存在");
            return null;
        }

        return songInfoDict[selectedSongName];
    }

    // 获取所有歌曲信息
    public static Dictionary<string, SongInfo> GetAllSongInfo()
    {
        return songInfoDict;
    }

    public static string GetMusicFilePath()
    {
        return musicFilePath;
    }

    public static string GetChartFilePath()
    {
        return chartFilePath;
    }

    public static string GetExcelFilePath()
    {
        return excelFilePath;
    }

    public static Sprite GetCoverSprite()
    {
        string coverFilePath = Path.Combine(songFolderPath, "Cover.jpg");
        if (!File.Exists(coverFilePath))
        {
            Debug.LogError($"Cover file not found at {coverFilePath}");
            return null;
        }
        byte[] fileData = File.ReadAllBytes(coverFilePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        if (texture == null)
        {
            Debug.LogError($"Failed to load texture from {coverFilePath}");
            return null;
        }
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    public static void ConvertExcelToJson()
    {
        try
        {
            using (XLWorkbook workbook = new XLWorkbook(excelFilePath))
            {
                Dictionary<string, object> result = new Dictionary<string, object>();

                // 处理 speed 数据
                IXLWorksheet speedSheet = workbook.Worksheet("speed");
                List<Dictionary<string, object>> speedData = new List<Dictionary<string, object>>();
                var speedHeaderRow = speedSheet.FirstRowUsed();
                var speedStartTIndex = GetColumnIndex(speedHeaderRow, "startT");
                var speedEndTIndex = GetColumnIndex(speedHeaderRow, "endT");
                var speedSpIndex = GetColumnIndex(speedHeaderRow, "sp");

                var speedDataRows = speedSheet.RowsUsed().Skip(1);
                foreach (var row in speedDataRows)
                {
                    Dictionary<string, object> speedRow = new Dictionary<string, object>();

                    double? startT = ParseDoubleValue(row.Cell(speedStartTIndex), 3);
                    if (startT.HasValue) speedRow["startT"] = startT.Value;

                    double? endT = ParseDoubleValue(row.Cell(speedEndTIndex), 3);
                    if (endT.HasValue) speedRow["endT"] = endT.Value;

                    double? sp = ParseDoubleValue(row.Cell(speedSpIndex), 3);
                    if (sp.HasValue) speedRow["sp"] = sp.Value;

                    speedData.Add(speedRow);
                }
                result["speed"] = speedData;

                // 处理 color 数据
                IXLWorksheet colorSheet = workbook.Worksheet("color");
                List<Dictionary<string, object>> colorData = new List<Dictionary<string, object>>();
                var colorHeaderRow = colorSheet.FirstRowUsed();
                var colorStartTIndex = GetColumnIndex(colorHeaderRow, "startT");
                var colorEndTIndex = GetColumnIndex(colorHeaderRow, "endT");
                var startLcolorIndex = GetColumnIndex(colorHeaderRow, "StartLcolor"); // 新增列
                var startUcolorIndex = GetColumnIndex(colorHeaderRow, "StartUcolor"); // 新增列
                var endLcolorIndex = GetColumnIndex(colorHeaderRow, "EndLcolor");     // 新增列
                var endUcolorIndex = GetColumnIndex(colorHeaderRow, "EndUcolor");     // 新增列

                var colorDataRows = colorSheet.RowsUsed().Skip(1);
                foreach (var row in colorDataRows)
                {
                    Dictionary<string, object> colorRow = new Dictionary<string, object>();

                    double? startT = ParseDoubleValue(row.Cell(colorStartTIndex), 3);
                    if (startT.HasValue) colorRow["startT"] = startT.Value;

                    double? endT = ParseDoubleValue(row.Cell(colorEndTIndex), 3);
                    if (endT.HasValue) colorRow["endT"] = endT.Value;

                    // 解析颜色字段（4个新增字段）
                    string startLcolor = ParseStringValue(row.Cell(startLcolorIndex));
                    if (!string.IsNullOrEmpty(startLcolor))
                        colorRow["StartLcolor"] = startLcolor; // 映射到JsonProperty名称

                    string startUcolor = ParseStringValue(row.Cell(startUcolorIndex));
                    if (!string.IsNullOrEmpty(startUcolor))
                        colorRow["StartUcolor"] = startUcolor;

                    string endLcolor = ParseStringValue(row.Cell(endLcolorIndex));
                    if (!string.IsNullOrEmpty(endLcolor))
                        colorRow["EndLcolor"] = endLcolor;

                    string endUcolor = ParseStringValue(row.Cell(endUcolorIndex));
                    if (!string.IsNullOrEmpty(endUcolor))
                        colorRow["EndUcolor"] = endUcolor;

                    colorData.Add(colorRow);
                }
                result["color"] = colorData;

                // 处理 plane 数据
                IXLWorksheet planeSheet = workbook.Worksheet("plane");
                List<Dictionary<string, object>> planeData = new List<Dictionary<string, object>>();
                var planeHeaderRow = planeSheet.FirstRowUsed();
                var startTIndex = GetColumnIndex(planeHeaderRow, "startT");
                var startYIndex = GetColumnIndex(planeHeaderRow, "startY");
                var endTIndex = GetColumnIndex(planeHeaderRow, "endT");
                var endYIndex = GetColumnIndex(planeHeaderRow, "endY");
                var funcIndex = GetColumnIndex(planeHeaderRow, "Func");
                var idIndex = GetColumnIndex(planeHeaderRow, "id");

                var dataRows = planeSheet.RowsUsed().Skip(1);
                var validRows = dataRows.Where(row => !string.IsNullOrEmpty(row.Cell(idIndex).GetValue<string>()?.Trim()));
                foreach (var group in validRows.GroupBy(row => ParseIntValue(row.Cell(idIndex), "plane")))
                {
                    if (group.Key.HasValue)
                    {
                        Dictionary<string, object> plane = new Dictionary<string, object>();
                        plane["id"] = group.Key.Value;
                        List<Dictionary<string, object>> subData = new List<Dictionary<string, object>>();
                        foreach (var row in group)
                        {
                            Dictionary<string, object> subRow = new Dictionary<string, object>();

                            double? startT = ParseDoubleValue(row.Cell(startTIndex), 3);
                            if (startT.HasValue) subRow["startT"] = startT.Value;

                            double? startY = ParseDoubleValue(row.Cell(startYIndex), 3);
                            if (startY.HasValue) subRow["startY"] = startY.Value;

                            double? endT = ParseDoubleValue(row.Cell(endTIndex), 3);
                            if (endT.HasValue) subRow["endT"] = endT.Value;

                            double? endY = ParseDoubleValue(row.Cell(endYIndex), 3);
                            if (endY.HasValue) subRow["endY"] = endY.Value;

                            string func = ParseStringValue(row.Cell(funcIndex));
                            if (!string.IsNullOrEmpty(func)) subRow["Func"] = func;

                            subData.Add(subRow);
                        }
                        plane["sub"] = subData;
                        planeData.Add(plane);
                    }
                }
                result["plane"] = planeData;

                // 处理 tap 数据
                IXLWorksheet tapSheet = workbook.Worksheet("tap");
                List<Dictionary<string, object>> tapData = new List<Dictionary<string, object>>();
                var tapHeaderRow = tapSheet.FirstRowUsed();
                var tapStartTIndex = GetColumnIndex(tapHeaderRow, "startT");
                var tapStartXIndex = GetColumnIndex(tapHeaderRow, "startX");
                var tapSizeIndex = GetColumnIndex(tapHeaderRow, "Size");
                var tapPidIndex = GetColumnIndex(tapHeaderRow, "Pid");

                var tapDataRows = tapSheet.RowsUsed().Skip(1);
                foreach (var row in tapDataRows)
                {
                    Dictionary<string, object> tap = new Dictionary<string, object>();

                    double? startT = ParseDoubleValue(row.Cell(tapStartTIndex), 3);
                    if (startT.HasValue) tap["startT"] = startT.Value;

                    double? startX = ParseDoubleValue(row.Cell(tapStartXIndex), 3);
                    if (startX.HasValue) tap["startX"] = startX.Value;

                    double? size = ParseDoubleValue(row.Cell(tapSizeIndex), 3);
                    if (size.HasValue) tap["Size"] = size.Value;

                    int? pid = ParseIntValue(row.Cell(tapPidIndex), "tap");
                    if (pid.HasValue) tap["Pid"] = pid.Value;

                    tapData.Add(tap);
                }
                result["tap"] = tapData;

                // 处理 hold 数据
                IXLWorksheet holdSheet = workbook.Worksheet("hold");
                List<Dictionary<string, object>> holdData = new List<Dictionary<string, object>>();
                var holdHeaderRow = holdSheet.FirstRowUsed();
                var holdStartTIndex = GetColumnIndex(holdHeaderRow, "startT");
                var holdStartXMinIndex = GetColumnIndex(holdHeaderRow, "startXMin");
                var holdStartXMaxIndex = GetColumnIndex(holdHeaderRow, "startXMax");
                var holdEndTIndex = GetColumnIndex(holdHeaderRow, "endT");
                var holdEndXMinIndex = GetColumnIndex(holdHeaderRow, "endXMin");
                var holdEndXMaxIndex = GetColumnIndex(holdHeaderRow, "endXMax");
                var holdLFuncIndex = GetColumnIndex(holdHeaderRow, "LFunc");
                var holdRFuncIndex = GetColumnIndex(holdHeaderRow, "RFunc");
                var holdIdIndex = GetColumnIndex(holdHeaderRow, "id");
                var holdPidIndex = GetColumnIndex(holdHeaderRow, "Pid");
                var holdJagnumIndex = GetColumnIndex(holdHeaderRow, "Jagnum");

                var holdDataRows = holdSheet.RowsUsed().Skip(1);
                var validHoldRows = holdDataRows.Where(row => !string.IsNullOrEmpty(row.Cell(holdIdIndex).GetValue<string>()?.Trim()));
                foreach (var group in validHoldRows.GroupBy(row => ParseIntValue(row.Cell(holdIdIndex), "hold")))
                {
                    if (group.Key.HasValue)
                    {
                        Dictionary<string, object> hold = new Dictionary<string, object>();

                        int? pid = ParseIntValue(group.First().Cell(holdPidIndex), "hold");
                        if (pid.HasValue) hold["Pid"] = pid.Value;

                        hold["id"] = group.Key.Value;
                        List<Dictionary<string, object>> subData = new List<Dictionary<string, object>>();
                        foreach (var row in group)
                        {
                            Dictionary<string, object> subRow = new Dictionary<string, object>();

                            double? startT = ParseDoubleValue(row.Cell(holdStartTIndex), 3);
                            if (startT.HasValue) subRow["startT"] = startT.Value;

                            double? startXMin = ParseDoubleValue(row.Cell(holdStartXMinIndex), 3);
                            if (startXMin.HasValue) subRow["startXMin"] = startXMin.Value;

                            double? startXMax = ParseDoubleValue(row.Cell(holdStartXMaxIndex), 3);
                            if (startXMax.HasValue) subRow["startXMax"] = startXMax.Value;

                            double? endT = ParseDoubleValue(row.Cell(holdEndTIndex), 3);
                            if (endT.HasValue) subRow["endT"] = endT.Value;

                            double? endXMin = ParseDoubleValue(row.Cell(holdEndXMinIndex), 3);
                            if (endXMin.HasValue) subRow["endXMin"] = endXMin.Value;

                            double? endXMax = ParseDoubleValue(row.Cell(holdEndXMaxIndex), 3);
                            if (endXMax.HasValue) subRow["endXMax"] = endXMax.Value;

                            string lFunc = ParseStringValue(row.Cell(holdLFuncIndex));
                            if (!string.IsNullOrEmpty(lFunc)) subRow["LFunc"] = lFunc;

                            string rFunc = ParseStringValue(row.Cell(holdRFuncIndex));
                            if (!string.IsNullOrEmpty(rFunc)) subRow["RFunc"] = rFunc;

                            int? jagNum = ParseIntValue(row.Cell(holdJagnumIndex), "hold");
                            if (jagNum.HasValue) subRow["Jagnum"] = jagNum.Value;

                            subData.Add(subRow);
                        }
                        hold["sub"] = subData;
                        holdData.Add(hold);
                    }
                }
                result["hold"] = holdData;

                // 处理 slide 数据
                IXLWorksheet slideSheet = workbook.Worksheet("slide");
                List<Dictionary<string, object>> slideData = new List<Dictionary<string, object>>();
                var slideHeaderRow = slideSheet.FirstRowUsed();
                var slideStartTIndex = GetColumnIndex(slideHeaderRow, "startT");
                var slideStartXIndex = GetColumnIndex(slideHeaderRow, "startX");
                var slideSizeIndex = GetColumnIndex(slideHeaderRow, "Size");
                var slidePidIndex = GetColumnIndex(slideHeaderRow, "Pid");

                var slideDataRows = slideSheet.RowsUsed().Skip(1);
                foreach (var row in slideDataRows)
                {
                    Dictionary<string, object> slide = new Dictionary<string, object>();

                    double? startT = ParseDoubleValue(row.Cell(slideStartTIndex), 3);
                    if (startT.HasValue) slide["startT"] = startT.Value;

                    double? startX = ParseDoubleValue(row.Cell(slideStartXIndex), 3);
                    if (startX.HasValue) slide["startX"] = startX.Value;

                    double? size = ParseDoubleValue(row.Cell(slideSizeIndex), 3);
                    if (size.HasValue) slide["Size"] = size.Value;

                    int? pid = ParseIntValue(row.Cell(slidePidIndex), "slide");
                    if (pid.HasValue) slide["Pid"] = pid.Value;

                    slideData.Add(slide);
                }
                result["slide"] = slideData;

                // 处理 flick 数据
                IXLWorksheet flickSheet = workbook.Worksheet("flick");
                List<Dictionary<string, object>> flickData = new List<Dictionary<string, object>>();
                var flickHeaderRow = flickSheet.FirstRowUsed();
                var flickStartTIndex = GetColumnIndex(flickHeaderRow, "startT");
                var flickStartXIndex = GetColumnIndex(flickHeaderRow, "startX");
                var flickSizeIndex = GetColumnIndex(flickHeaderRow, "Size");
                var flickDirIndex = GetColumnIndex(flickHeaderRow, "Dir");
                var flickPidIndex = GetColumnIndex(flickHeaderRow, "Pid");

                var flickDataRows = flickSheet.RowsUsed().Skip(1);
                foreach (var row in flickDataRows)
                {
                    Dictionary<string, object> flick = new Dictionary<string, object>();

                    double? startT = ParseDoubleValue(row.Cell(flickStartTIndex), 3);
                    if (startT.HasValue) flick["startT"] = startT.Value;

                    double? startX = ParseDoubleValue(row.Cell(flickStartXIndex), 3);
                    if (startX.HasValue) flick["startX"] = startX.Value;

                    double? size = ParseDoubleValue(row.Cell(flickSizeIndex), 3);
                    if (size.HasValue) flick["Size"] = size.Value;

                    string dir = ParseStringValue(row.Cell(flickDirIndex));
                    if (!string.IsNullOrEmpty(dir)) flick["Dir"] = dir;

                    int? pid = ParseIntValue(row.Cell(flickPidIndex), "flick");
                    if (pid.HasValue) flick["Pid"] = pid.Value;

                    flickData.Add(flick);
                }
                result["flick"] = flickData;

                // 处理 star 数据
                IXLWorksheet starSheet = workbook.Worksheet("star");
                List<Dictionary<string, object>> starData = new List<Dictionary<string, object>>();
                var starHeaderRow = starSheet.FirstRowUsed();
                var starStartTIndex = GetColumnIndex(starHeaderRow, "startT");
                var starEndTIndex = GetColumnIndex(starHeaderRow, "endT");
                var starStartXIndex = GetColumnIndex(starHeaderRow, "startX");
                var starStartYIndex = GetColumnIndex(starHeaderRow, "startY");
                var starEndXIndex = GetColumnIndex(starHeaderRow, "endX");
                var starEndYIndex = GetColumnIndex(starHeaderRow, "endY");
                var starFuncIndex = GetColumnIndex(starHeaderRow, "Func");
                var starIdIndex = GetColumnIndex(starHeaderRow, "id");
                var starPidIndex = GetColumnIndex(starHeaderRow, "Pid");
                var starHeadTIndex = GetColumnIndex(starHeaderRow, "headT");
                var RadIndex = GetColumnIndex(starHeaderRow, "Rad");
                var AngleIndex = GetColumnIndex(starHeaderRow, "Angle");
                var RotIndex = GetColumnIndex(starHeaderRow, "Rot");

                var starDataRows = starSheet.RowsUsed().Skip(1);
                var validStarRows = starDataRows.Where(row => !string.IsNullOrEmpty(row.Cell(starIdIndex).GetValue<string>()?.Trim()));
                foreach (var group in validStarRows.GroupBy(row => ParseIntValue(row.Cell(starIdIndex), "star")))
                {
                    if (group.Key.HasValue)
                    {
                        Dictionary<string, object> star = new Dictionary<string, object>();

                        int? pid = ParseIntValue(group.First().Cell(starPidIndex), "star");
                        if (pid.HasValue) star["Pid"] = pid.Value;

                        star["id"] = group.Key.Value;

                        double? headT = ParseDoubleValue(group.First().Cell(starHeadTIndex), 3);
                        if (headT.HasValue) star["headT"] = headT.Value;

                        List<Dictionary<string, object>> subData = new List<Dictionary<string, object>>();
                        foreach (var row in group)
                        {
                            Dictionary<string, object> subRow = new Dictionary<string, object>();

                            double? startT = ParseDoubleValue(row.Cell(starStartTIndex), 3);
                            if (startT.HasValue) subRow["startT"] = startT.Value;

                            double? endT = ParseDoubleValue(row.Cell(starEndTIndex), 3);
                            if (endT.HasValue) subRow["endT"] = endT.Value;

                            double? startX = ParseDoubleValue(row.Cell(starStartXIndex), 3);
                            if (startX.HasValue) subRow["startX"] = startX.Value;

                            double? startY = ParseDoubleValue(row.Cell(starStartYIndex), 3);
                            if (startY.HasValue) subRow["startY"] = startY.Value;

                            double? endX = ParseDoubleValue(row.Cell(starEndXIndex), 3);
                            if (endX.HasValue) subRow["endX"] = endX.Value;

                            double? endY = ParseDoubleValue(row.Cell(starEndYIndex), 3);
                            if (endY.HasValue) subRow["endY"] = endY.Value;

                            string func = ParseStringValue(row.Cell(starFuncIndex));
                            if (!string.IsNullOrEmpty(func)) subRow["Func"] = func;

                            double? rad = ParseDoubleValue(row.Cell(RadIndex), 3);
                            if (rad.HasValue) subRow["Rad"] = rad.Value;

                            double? angle = ParseDoubleValue(row.Cell(AngleIndex), 3);
                            if (angle.HasValue) subRow["Angle"] = angle.Value;

                            double? rot = ParseDoubleValue(row.Cell(RotIndex), 3);
                            if (rot.HasValue) subRow["Rot"] = rot.Value;

                            subData.Add(subRow);
                        }
                        star["sub"] = subData;
                        starData.Add(star);
                    }
                }
                result["star"] = starData;

                // 修复JSON序列化时的Formatting命名冲突
                string jsonData = JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(chartFilePath, jsonData);
                Debug.Log($"已将Excel谱面文件转为json格式");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error converting Excel to JSON: {e.Message}");
        }
    }

    private static int GetColumnIndex(IXLRow headerRow, string columnName)
    {
        for (int i = 1; i <= headerRow.LastCellUsed().Address.ColumnNumber; i++)
        {
            if (headerRow.Cell(i).GetValue<string>()?.Trim() == columnName)
            {
                return i;
            }
        }
        throw new ArgumentException($"Column {columnName} not found in the header row.");
    }
    private static int? ParseIntValue(IXLCell cell, string sheetName)
    {
        string value = cell.GetValue<string>()?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            // 移除错误日志，直接返回null
            return null;
        }
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        Debug.LogError($"在 {sheetName} 工作表中，无法将单元格 {cell.Address} 的值 {value} 转换为整数。");
        return null;
    }

    private static double? ParseDoubleValue(IXLCell cell, int decimalPlaces)
    {
        string value = cell.GetValue<string>()?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            // 移除错误日志，直接返回null
            return null;
        }
        if (double.TryParse(value, out double result))
        {
            return Math.Round(result, decimalPlaces);
        }
        Debug.LogError($"无法将单元格 {cell.Address} 的值 {value} 转换为双精度浮点数。");
        return null;
    }

    private static string ParseStringValue(IXLCell cell)
    {
        string value = cell.GetValue<string>()?.Trim();
        // 空字符串返回null，不输出日志
        return string.IsNullOrEmpty(value) ? null : value;
    }

}