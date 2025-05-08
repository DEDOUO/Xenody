using UnityEngine;
using System.IO;
using ClosedXML.Excel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System;

public static class SongAndChartData
{
    private static string selectedSongName;
    private static string songFolderPath;
    private static string musicFilePath;
    private static string chartFilePath;
    private static string excelFilePath;

    public static void SetSelectedSong(string songName)
    {
        selectedSongName = songName;
        songFolderPath = Path.Combine(Application.streamingAssetsPath, "Songs", selectedSongName);
        musicFilePath = Path.Combine(songFolderPath, "Music.mp3");
        chartFilePath = Path.Combine(songFolderPath, "Chart.json");
        excelFilePath = Path.Combine(songFolderPath, "Chart.xlsx");
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
                    speedRow["startT"] = ParseDoubleValue(row.Cell(speedStartTIndex), 3);
                    speedRow["endT"] = ParseDoubleValue(row.Cell(speedEndTIndex), 3);
                    speedRow["sp"] = ParseDoubleValue(row.Cell(speedSpIndex), 3);
                    speedData.Add(speedRow);
                }
                result["speed"] = speedData;

                // 处理 color 数据
                IXLWorksheet colorSheet = workbook.Worksheet("color");
                List<Dictionary<string, object>> colorData = new List<Dictionary<string, object>>();
                var colorHeaderRow = colorSheet.FirstRowUsed();
                var colorStartTIndex = GetColumnIndex(colorHeaderRow, "startT");
                var colorEndTIndex = GetColumnIndex(colorHeaderRow, "endT");
                var colorLcolorIndex = GetColumnIndex(colorHeaderRow, "Lcolor");
                var colorUcolorIndex = GetColumnIndex(colorHeaderRow, "Ucolor");

                var colorDataRows = colorSheet.RowsUsed().Skip(1);
                foreach (var row in colorDataRows)
                {
                    Dictionary<string, object> colorRow = new Dictionary<string, object>();
                    colorRow["startT"] = ParseDoubleValue(row.Cell(colorStartTIndex), 3);
                    colorRow["endT"] = ParseDoubleValue(row.Cell(colorEndTIndex), 3);
                    colorRow["Lcolor"] = ParseStringValue(row.Cell(colorLcolorIndex));
                    colorRow["Ucolor"] = ParseStringValue(row.Cell(colorUcolorIndex));
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
                            subRow["startT"] = ParseDoubleValue(row.Cell(startTIndex), 3);
                            subRow["startY"] = ParseDoubleValue(row.Cell(startYIndex), 3);
                            subRow["endT"] = ParseDoubleValue(row.Cell(endTIndex), 3);
                            subRow["endY"] = ParseDoubleValue(row.Cell(endYIndex), 3);
                            subRow["Func"] = ParseStringValue(row.Cell(funcIndex));
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
                    tap["startT"] = ParseDoubleValue(row.Cell(tapStartTIndex), 3);
                    tap["startX"] = ParseDoubleValue(row.Cell(tapStartXIndex), 3);
                    tap["Size"] = ParseDoubleValue(row.Cell(tapSizeIndex), 3);
                    tap["Pid"] = ParseIntValue(row.Cell(tapPidIndex), "tap");
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
                        hold["Pid"] = ParseIntValue(group.First().Cell(holdPidIndex), "hold") ?? 0;
                        hold["id"] = group.Key.Value;
                        List<Dictionary<string, object>> subData = new List<Dictionary<string, object>>();
                        foreach (var row in group)
                        {
                            Dictionary<string, object> subRow = new Dictionary<string, object>();
                            subRow["startT"] = ParseDoubleValue(row.Cell(holdStartTIndex), 3);
                            subRow["startXMin"] = ParseDoubleValue(row.Cell(holdStartXMinIndex), 3);
                            subRow["startXMax"] = ParseDoubleValue(row.Cell(holdStartXMaxIndex), 3);
                            subRow["endT"] = ParseDoubleValue(row.Cell(holdEndTIndex), 3);
                            subRow["endXMin"] = ParseDoubleValue(row.Cell(holdEndXMinIndex), 3);
                            subRow["endXMax"] = ParseDoubleValue(row.Cell(holdEndXMaxIndex), 3);
                            subRow["LFunc"] = ParseStringValue(row.Cell(holdLFuncIndex));
                            subRow["RFunc"] = ParseStringValue(row.Cell(holdRFuncIndex));
                            subRow["Jagnum"] = ParseIntValue(row.Cell(holdJagnumIndex), "hold") ?? 0;
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
                    slide["startT"] = ParseDoubleValue(row.Cell(slideStartTIndex), 3);
                    slide["startX"] = ParseDoubleValue(row.Cell(slideStartXIndex), 3);
                    slide["Size"] = ParseDoubleValue(row.Cell(slideSizeIndex), 3);
                    slide["Pid"] = ParseIntValue(row.Cell(slidePidIndex), "slide");
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
                    flick["startT"] = ParseDoubleValue(row.Cell(flickStartTIndex), 3);
                    flick["startX"] = ParseDoubleValue(row.Cell(flickStartXIndex), 3);
                    flick["Size"] = ParseDoubleValue(row.Cell(flickSizeIndex), 3);
                    flick["Dir"] = ParseStringValue(row.Cell(flickDirIndex));
                    flick["Pid"] = ParseIntValue(row.Cell(flickPidIndex), "flick");
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

                var starDataRows = starSheet.RowsUsed().Skip(1);
                var validStarRows = starDataRows.Where(row => !string.IsNullOrEmpty(row.Cell(starIdIndex).GetValue<string>()?.Trim()));
                foreach (var group in validStarRows.GroupBy(row => ParseIntValue(row.Cell(starIdIndex), "star")))
                {
                    if (group.Key.HasValue)
                    {
                        Dictionary<string, object> star = new Dictionary<string, object>();
                        star["Pid"] = ParseIntValue(group.First().Cell(starPidIndex), "star") ?? 0;
                        star["id"] = group.Key.Value;
                        star["headT"] = ParseDoubleValue(group.First().Cell(starHeadTIndex), 3);
                        List<Dictionary<string, object>> subData = new List<Dictionary<string, object>>();
                        foreach (var row in group)
                        {
                            Dictionary<string, object> subRow = new Dictionary<string, object>();
                            subRow["startT"] = ParseDoubleValue(row.Cell(starStartTIndex), 3);
                            subRow["endT"] = ParseDoubleValue(row.Cell(starEndTIndex), 3);
                            subRow["startX"] = ParseDoubleValue(row.Cell(starStartXIndex), 3);
                            subRow["startY"] = ParseDoubleValue(row.Cell(starStartYIndex), 3);
                            subRow["endX"] = ParseDoubleValue(row.Cell(starEndXIndex), 3);
                            subRow["endY"] = ParseDoubleValue(row.Cell(starEndYIndex), 3);
                            subRow["Func"] = ParseStringValue(row.Cell(starFuncIndex));
                            subData.Add(subRow);
                        }
                        star["sub"] = subData;
                        starData.Add(star);
                    }
                }
                result["star"] = starData;

                string jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
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
            Debug.LogError($"在 {sheetName} 工作表中，单元格 {cell.Address} 的值为空，无法转换为整数。");
            return null;
        }
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        Debug.LogError($"在 {sheetName} 工作表中，无法将单元格 {cell.Address} 的值 {value} 转换为整数。");
        return null;
    }

    private static double ParseDoubleValue(IXLCell cell, int decimalPlaces)
    {
        string value = cell.GetValue<string>()?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError($"单元格 {cell.Address} 的值为空，无法转换为双精度浮点数。");
            return 0;
        }
        if (double.TryParse(value, out double result))
        {
            return Math.Round(result, decimalPlaces);
        }
        Debug.LogError($"无法将单元格 {cell.Address} 的值 {value} 转换为双精度浮点数。");
        return 0;
    }

    private static string ParseStringValue(IXLCell cell)
    {
        return cell.GetValue<string>()?.Trim();
    }
}