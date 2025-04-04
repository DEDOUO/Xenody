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

    public static void SetSelectedSong(string songName)
    {
        selectedSongName = songName;
        songFolderPath = Path.Combine(Application.streamingAssetsPath, "Songs", selectedSongName);
        musicFilePath = Path.Combine(songFolderPath, "Music.mp3");
        chartFilePath = Path.Combine(songFolderPath, "Chart.json");
    }

    public static string GetMusicFilePath()
    {
        return musicFilePath;
    }

    public static string GetChartFilePath()
    {
        return chartFilePath;
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

    public static Dictionary<string, object> GetChartData()
    {
        if (!File.Exists(chartFilePath))
        {
            string excelPath = Path.Combine(songFolderPath, "Chart.xlsx");
            if (File.Exists(excelPath))
            {
                try
                {
                    using (XLWorkbook workbook = new XLWorkbook(excelPath))
                    {
                        Dictionary<string, object> result = new Dictionary<string, object>();

                        IXLWorksheet planesSheet = workbook.Worksheet("planes");
                        IXLWorksheet tapsSheet = workbook.Worksheet("taps");
                        IXLWorksheet holdsSheet = workbook.Worksheet("holds");
                        IXLWorksheet slidesSheet = workbook.Worksheet("slides");
                        IXLWorksheet flicksSheet = workbook.Worksheet("flicks");
                        IXLWorksheet starsSheet = workbook.Worksheet("stars");

                        // 处理 planes 数据
                        List<Dictionary<string, object>> planesData = new List<Dictionary<string, object>>();
                        var headerRow = planesSheet.FirstRowUsed();
                        var startTIndex = GetColumnIndex(headerRow, "startT");
                        var startYIndex = GetColumnIndex(headerRow, "startY");
                        var endTIndex = GetColumnIndex(headerRow, "endT");
                        var endYIndex = GetColumnIndex(headerRow, "endY");
                        var funcIndex = GetColumnIndex(headerRow, "Func");
                        var idIndex = GetColumnIndex(headerRow, "id");
                        var colorIndex = GetColumnIndex(headerRow, "color");

                        var dataRows = planesSheet.RowsUsed().Skip(1);
                        var validRows = dataRows.Where(row => !string.IsNullOrEmpty(row.Cell(idIndex).GetValue<string>()?.Trim()));
                        foreach (var group in validRows.GroupBy(row => ParseIntValue(row.Cell(idIndex), "planes")))
                        {
                            if (group.Key.HasValue)
                            {
                                Dictionary<string, object> plane = new Dictionary<string, object>();
                                plane["id"] = group.Key.Value;
                                plane["color"] = ParseStringValue(group.First().Cell(colorIndex));
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
                                planesData.Add(plane);
                            }
                        }
                        result["planes"] = planesData;

                        // 处理 taps 数据
                        List<Dictionary<string, object>> tapsData = new List<Dictionary<string, object>>();
                        var tapsHeaderRow = tapsSheet.FirstRowUsed();
                        var tapsStartTIndex = GetColumnIndex(tapsHeaderRow, "startT");
                        var tapsStartXIndex = GetColumnIndex(tapsHeaderRow, "startX");
                        var tapsSizeIndex = GetColumnIndex(tapsHeaderRow, "Size");
                        var tapsPidIndex = GetColumnIndex(tapsHeaderRow, "Pid");

                        var tapsDataRows = tapsSheet.RowsUsed().Skip(1);
                        foreach (var row in tapsDataRows)
                        {
                            Dictionary<string, object> tap = new Dictionary<string, object>();
                            tap["startT"] = ParseDoubleValue(row.Cell(tapsStartTIndex), 3);
                            tap["startX"] = ParseDoubleValue(row.Cell(tapsStartXIndex), 3);
                            tap["Size"] = ParseDoubleValue(row.Cell(tapsSizeIndex), 3);
                            tap["Pid"] = ParseIntValue(row.Cell(tapsPidIndex), "taps");
                            tapsData.Add(tap);
                        }
                        result["taps"] = tapsData;

                        // 处理 holds 数据
                        List<Dictionary<string, object>> holdsData = new List<Dictionary<string, object>>();
                        var holdsHeaderRow = holdsSheet.FirstRowUsed();
                        var holdsStartTIndex = GetColumnIndex(holdsHeaderRow, "startT");
                        var holdsStartXMinIndex = GetColumnIndex(holdsHeaderRow, "startXMin");
                        var holdsStartXMaxIndex = GetColumnIndex(holdsHeaderRow, "startXMax");
                        var holdsEndTIndex = GetColumnIndex(holdsHeaderRow, "endT");
                        var holdsEndXMinIndex = GetColumnIndex(holdsHeaderRow, "endXMin");
                        var holdsEndXMaxIndex = GetColumnIndex(holdsHeaderRow, "endXMax");
                        var holdsLFuncIndex = GetColumnIndex(holdsHeaderRow, "LFunc");
                        var holdsRFuncIndex = GetColumnIndex(holdsHeaderRow, "RFunc");
                        var holdsIdIndex = GetColumnIndex(holdsHeaderRow, "id");
                        var holdsPidIndex = GetColumnIndex(holdsHeaderRow, "Pid");

                        var holdsDataRows = holdsSheet.RowsUsed().Skip(1);
                        var validHoldsRows = holdsDataRows.Where(row => !string.IsNullOrEmpty(row.Cell(holdsIdIndex).GetValue<string>()?.Trim()));
                        foreach (var group in validHoldsRows.GroupBy(row => ParseIntValue(row.Cell(holdsIdIndex), "holds")))
                        {
                            if (group.Key.HasValue)
                            {
                                Dictionary<string, object> hold = new Dictionary<string, object>();
                                hold["Pid"] = ParseIntValue(group.First().Cell(holdsPidIndex), "holds") ?? 0;
                                hold["id"] = group.Key.Value;
                                List<Dictionary<string, object>> subData = new List<Dictionary<string, object>>();
                                foreach (var row in group)
                                {
                                    Dictionary<string, object> subRow = new Dictionary<string, object>();
                                    subRow["startT"] = ParseDoubleValue(row.Cell(holdsStartTIndex), 3);
                                    subRow["startXMin"] = ParseDoubleValue(row.Cell(holdsStartXMinIndex), 3);
                                    subRow["startXMax"] = ParseDoubleValue(row.Cell(holdsStartXMaxIndex), 3);
                                    subRow["endT"] = ParseDoubleValue(row.Cell(holdsEndTIndex), 3);
                                    subRow["endXMin"] = ParseDoubleValue(row.Cell(holdsEndXMinIndex), 3);
                                    subRow["endXMax"] = ParseDoubleValue(row.Cell(holdsEndXMaxIndex), 3);
                                    subRow["LFunc"] = ParseStringValue(row.Cell(holdsLFuncIndex));
                                    subRow["RFunc"] = ParseStringValue(row.Cell(holdsRFuncIndex));
                                    subData.Add(subRow);
                                }
                                hold["sub"] = subData;
                                holdsData.Add(hold);
                            }
                        }
                        result["holds"] = holdsData;

                        // 处理 slides 数据
                        List<Dictionary<string, object>> slidesData = new List<Dictionary<string, object>>();
                        var slidesHeaderRow = slidesSheet.FirstRowUsed();
                        var slidesStartTIndex = GetColumnIndex(slidesHeaderRow, "startT");
                        var slidesStartXIndex = GetColumnIndex(slidesHeaderRow, "startX");
                        var slidesSizeIndex = GetColumnIndex(slidesHeaderRow, "Size");
                        var slidesPidIndex = GetColumnIndex(slidesHeaderRow, "Pid");

                        var slidesDataRows = slidesSheet.RowsUsed().Skip(1);
                        foreach (var row in slidesDataRows)
                        {
                            Dictionary<string, object> slide = new Dictionary<string, object>();
                            slide["startT"] = ParseDoubleValue(row.Cell(slidesStartTIndex), 3);
                            slide["startX"] = ParseDoubleValue(row.Cell(slidesStartXIndex), 3);
                            slide["Size"] = ParseDoubleValue(row.Cell(slidesSizeIndex), 3);
                            slide["Pid"] = ParseIntValue(row.Cell(slidesPidIndex), "slides");
                            slidesData.Add(slide);
                        }
                        result["slides"] = slidesData;

                        // 处理 flicks 数据
                        List<Dictionary<string, object>> flicksData = new List<Dictionary<string, object>>();
                        var flicksHeaderRow = flicksSheet.FirstRowUsed();
                        var flicksStartTIndex = GetColumnIndex(flicksHeaderRow, "startT");
                        var flicksStartXIndex = GetColumnIndex(flicksHeaderRow, "startX");
                        var flicksSizeIndex = GetColumnIndex(flicksHeaderRow, "Size");
                        var flicksDirIndex = GetColumnIndex(flicksHeaderRow, "Dir");
                        var flicksPidIndex = GetColumnIndex(flicksHeaderRow, "Pid");

                        var flicksDataRows = flicksSheet.RowsUsed().Skip(1);
                        foreach (var row in flicksDataRows)
                        {
                            Dictionary<string, object> flick = new Dictionary<string, object>();
                            flick["startT"] = ParseDoubleValue(row.Cell(flicksStartTIndex), 3);
                            flick["startX"] = ParseDoubleValue(row.Cell(flicksStartXIndex), 3);
                            flick["Size"] = ParseDoubleValue(row.Cell(flicksSizeIndex), 3);
                            flick["Dir"] = ParseStringValue(row.Cell(flicksDirIndex));
                            flick["Pid"] = ParseIntValue(row.Cell(flicksPidIndex), "flicks");
                            flicksData.Add(flick);
                        }
                        result["flicks"] = flicksData;

                        // 处理 stars 数据
                        List<Dictionary<string, object>> starsData = new List<Dictionary<string, object>>();
                        var starsHeaderRow = starsSheet.FirstRowUsed();
                        var starsStartTIndex = GetColumnIndex(starsHeaderRow, "startT");
                        var starsEndTIndex = GetColumnIndex(starsHeaderRow, "endT");
                        var starsStartXIndex = GetColumnIndex(starsHeaderRow, "startX");
                        var starsStartYIndex = GetColumnIndex(starsHeaderRow, "startY");
                        var starsEndXIndex = GetColumnIndex(starsHeaderRow, "endX");
                        var starsEndYIndex = GetColumnIndex(starsHeaderRow, "endY");
                        var starsFuncIndex = GetColumnIndex(starsHeaderRow, "Func");
                        var starsIdIndex = GetColumnIndex(starsHeaderRow, "id");
                        var starsPidIndex = GetColumnIndex(starsHeaderRow, "Pid");
                        var starsHeadTIndex = GetColumnIndex(starsHeaderRow, "headT");

                        var starsDataRows = starsSheet.RowsUsed().Skip(1);
                        var validStarsRows = starsDataRows.Where(row => !string.IsNullOrEmpty(row.Cell(starsIdIndex).GetValue<string>()?.Trim()));
                        foreach (var group in validStarsRows.GroupBy(row => ParseIntValue(row.Cell(starsIdIndex), "stars")))
                        {
                            if (group.Key.HasValue)
                            {
                                Dictionary<string, object> star = new Dictionary<string, object>();
                                star["Pid"] = ParseIntValue(group.First().Cell(starsPidIndex), "stars") ?? 0;
                                star["id"] = group.Key.Value;
                                star["headT"] = ParseDoubleValue(group.First().Cell(starsHeadTIndex), 3);
                                List<Dictionary<string, object>> subData = new List<Dictionary<string, object>>();
                                foreach (var row in group)
                                {
                                    Dictionary<string, object> subRow = new Dictionary<string, object>();
                                    subRow["startT"] = ParseDoubleValue(row.Cell(starsStartTIndex), 3);
                                    subRow["endT"] = ParseDoubleValue(row.Cell(starsEndTIndex), 3);
                                    subRow["startX"] = ParseDoubleValue(row.Cell(starsStartXIndex), 3);
                                    subRow["startY"] = ParseDoubleValue(row.Cell(starsStartYIndex), 3);
                                    subRow["endX"] = ParseDoubleValue(row.Cell(starsEndXIndex), 3);
                                    subRow["endY"] = ParseDoubleValue(row.Cell(starsEndYIndex), 3);
                                    subRow["Func"] = ParseStringValue(row.Cell(starsFuncIndex));
                                    subData.Add(subRow);
                                }
                                star["sub"] = subData;
                                starsData.Add(star);
                            }
                        }
                        result["stars"] = starsData;

                        string jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
                        File.WriteAllText(chartFilePath, jsonData);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error converting Excel to JSON: {e.Message}");
                    return null;
                }
                Debug.Log($"已将Excel谱面文件转为json格式");
            }
            else
            {
                Debug.LogError($"Neither Chart.json nor Chart.xlsx found in {songFolderPath}");
                return null;
            }
        }

        try
        {
            string jsonContent = File.ReadAllText(chartFilePath);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading Chart.json: {e.Message}");
            return null;
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