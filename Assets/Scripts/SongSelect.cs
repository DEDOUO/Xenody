using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

// 选歌界面的脚本，负责处理歌曲列表展示和选择歌曲后跳转到播放场景的逻辑
public class SongSelect : MonoBehaviour
{
    public ScrollRect songListScrollView; // 在Unity中拖拽赋值，指向展示歌曲列表的滚动视图
    private List<string> songList = new List<string>(); // 用于存储歌曲名称的列表

    private void Start()
    {
        // 这里暂时只添加一首歌曲“Accelerate”，后续可从文件夹读取更多歌曲名添加进来
        //songList.Add("Accelerate");
        PopulateSongList();
    }

    // 填充歌曲列表，创建按钮并添加点击事件监听
    private void PopulateSongList()
    {
        Debug.Log($"获取歌曲列表...");
        // 设置按钮的初始垂直位置偏移量，用于控制按钮在滚动视图中的排列位置
        float horizontalOffset = 40f;
        float verticalOffset = 40f;
        // 按钮的固定高度，可根据实际需求调整
        float buttonHeight = 50f;
        // 按钮之间的垂直间距，可根据实际需求调整
        float spacing = 20f;
        // 按钮的固定宽度，可根据实际需求调整，避免文字挤成竖线的情况
        float buttonWidth = 400f;

        // 获取Songs文件夹的完整路径（假设在Assets/Resources下，你可根据实际调整）
        string songsFolderPath = Path.Combine(Application.streamingAssetsPath, "Songs");
        if (Directory.Exists(songsFolderPath))
        {
            // 获取Songs文件夹下所有的子文件夹（即歌曲文件夹）名称
            string[] songFolderNames = Directory.GetDirectories(songsFolderPath);
            foreach (string songFolder in songFolderNames)
            {
                //Debug.Log(songFolder);
                // 提取歌曲文件夹名作为歌曲名，这里假设歌曲文件夹名就是歌曲名，可根据实际情况调整提取逻辑
                string songName = Path.GetFileName(songFolder);
                songList.Add(songName);

                // 创建按钮对象，并确保添加了必要的UI组件（Button、RectTransform等）
                GameObject buttonObj = new GameObject($"Song{songList.Count}", typeof(Button));
                buttonObj.AddComponent<RectTransform>();

                // 添加Image组件，用于显示按钮背景，添加到按钮对象本身
                Image image = buttonObj.AddComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 1f); // 设置按钮背景颜色，可根据喜好调整
                                                               //image.sprite = Resources.Load<Sprite>("YourImageSpriteName"); // 如果想用图片作为背景，取消注释并替换为实际的图片资源名称

                // 创建一个空的子对象，用于挂载TextMeshProUGUI组件来显示文本内容
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform);

                // 获取按钮组件的方式更严谨一些
                Button button = buttonObj.GetComponent<Button>();
                if (button == null)
                {
                    Debug.LogError($"无法获取按钮组件，对象: {buttonObj.name}");
                    continue;
                }

                // 添加TextMeshProUGUI组件来显示文本内容，添加到子对象textObj上
                TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
                if (textComponent == null)
                {
                    textComponent = textObj.AddComponent<TextMeshProUGUI>();
                }
                textComponent.text = songName;

                // 设置文字居中对齐
                textComponent.alignment = TextAlignmentOptions.Center;

                // 设置按钮的样式相关属性（颜色、过渡效果等，和之前类似）
                button.targetGraphic = textComponent;
                button.transition = Selectable.Transition.ColorTint;

                ColorBlock colorBlock = button.colors;
                colorBlock.normalColor = new Color(1f, 1f, 1f, 1f);
                colorBlock.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                colorBlock.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                colorBlock.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                colorBlock.colorMultiplier = 1f;
                colorBlock.fadeDuration = 0.1f;
                button.colors = colorBlock;

                // 根据歌曲名包含的文字类型设置字体
                TMP_FontAsset fontAsset = GetFontBasedOnCharacters(songName);
                textComponent.font = fontAsset;

                // 根据字体设置字体大小
                if (IsNotoSansFont(fontAsset))
                {
                    textComponent.fontSize = 35;
                }
                else
                {
                    textComponent.fontSize = 50;
                }

                textComponent.color = Color.white;

                button.onClick.AddListener(() =>
                {
                    SongAndChartData.SetSelectedSong(songName);
                    SceneManager.LoadScene("AutoPlay");
                });

                buttonObj.transform.SetParent(songListScrollView.content);
                buttonObj.transform.localScale = Vector3.one;

                // 获取按钮的RectTransform组件，用于设置布局相关属性
                RectTransform buttonRectTransform = buttonObj.GetComponent<RectTransform>();
                // 设置按钮的锚点，均对齐左上角
                buttonRectTransform.anchorMin = new Vector2(0f, 1f);
                buttonRectTransform.anchorMax = new Vector2(0f, 1f);
                buttonRectTransform.pivot = new Vector2(0f, 1f);
                // 设置按钮在垂直方向上的位置，基于之前的偏移量和间距来排列
                // 这里添加了对z轴坐标的设置，将其设为0
                buttonRectTransform.anchoredPosition3D = new Vector3(horizontalOffset, -verticalOffset, 0f);
                // 设置按钮的初始大小
                buttonRectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);

                // 获取文本对象的RectTransform组件
                RectTransform textRectTransform = textObj.GetComponent<RectTransform>();
                // 设置文本对象的锚点、枢轴与按钮一致
                textRectTransform.anchorMin = new Vector2(0f, 0f);
                textRectTransform.anchorMax = new Vector2(1f, 1f);
                textRectTransform.pivot = new Vector2(0.5f, 0.5f);
                // 设置文本对象的位置和大小与按钮一致
                textRectTransform.offsetMin = Vector2.zero;
                textRectTransform.offsetMax = Vector2.zero;

                // 更新垂直偏移量，为下一个按钮的位置做准备
                verticalOffset += buttonHeight + spacing;
            }
        }
        else
        {
            Debug.LogError("Songs文件夹不存在，请检查路径是否正确！");
        }
    }

    // 根据字符串包含的文字类型返回对应的字体
    private TMP_FontAsset GetFontBasedOnCharacters(string input)
    {
        if (string.IsNullOrEmpty(input)) return null; // 空字符串处理

        // 先判断中文（范围：0x4E00 ~ 0x9FFF）
        if (ContainsChinese(input))
        {
            return Resources.Load<TMP_FontAsset>("Fonts/NotoSansSC-Regular SDF");
        }
        // 再判断韩文（范围：0xAC00 ~ 0xD7AF 等）
        else if (ContainsKorean(input))
        {
            return Resources.Load<TMP_FontAsset>("Fonts/NotoSansKR-Regular SDF");
        }
        // 最后判断日文（仅包含平假名、片假名，不含汉字）
        else if (ContainsJapanese(input))
        {
            return Resources.Load<TMP_FontAsset>("Fonts/NotoSansJP-Regular SDF");
        }
        // 其他语言（英文、数字、符号等）
        return Resources.Load<TMP_FontAsset>("Fonts/Jupiter SDF");
    }

    // 判断是否为 CJK 统一表意文字（汉字，排除日文假名、韩文组合字符等）
    private bool IsCJKUnifiedIdeograph(char c)
    {
        // 中文、日文、韩文共用的统一表意文字范围（0x4E00 ~ 0x9FFF 和 0x3400 ~ 0x4DBF 等）
        // 注意：此范围包含中日韩汉字，需结合具体需求调整
        return (c >= 0x4E00 && c <= 0x9FFF) ||
               (c >= 0x3400 && c <= 0x4DBF) ||
               (c >= 0x20000 && c <= 0x2A6DF) ||
               (c >= 0x2A700 && c <= 0x2B73F) ||
               (c >= 0x2B740 && c <= 0x2B81F) ||
               (c >= 0x2B820 && c <= 0x2CEAF) ||
               (c >= 0xF900 && c <= 0xFAFF) ||
               (c >= 0x2F800 && c <= 0x2FA1F);
    }

    // 判断是否包含中文（使用自定义的 CJK 检查）
    private bool ContainsChinese(string input)
    {
        foreach (char c in input)
        {
            if (IsCJKUnifiedIdeograph(c))
            {
                return true;
            }
        }
        return false;
    }
    // 判断是否包含韩文
    private bool ContainsKorean(string input)
    {
        foreach (char c in input)
        {
            // 韩文基本字符 + 组合字符 + 扩展字符
            if ((c >= 0xAC00 && c <= 0xD7AF) || // 韩文音节
                (c >= 0x1100 && c <= 0x11FF) || // 韩文声母
                (c >= 0x3130 && c <= 0x318F) || // 韩文韵母
                (c >= 0xA960 && c <= 0xA97F) || // 韩文扩展
                (c >= 0xD7B0 && c <= 0xD7FF))    // 韩文扩展
            {
                return true;
            }
        }
        return false;
    }

    // 判断是否包含日文（仅平假名、片假名，不含汉字）
    private bool ContainsJapanese(string input)
    {
        foreach (char c in input)
        {
            // 平假名（ぁ-ゖ）
            if ((c >= 0x3040 && c <= 0x309F) ||
                // 片假名（ァ-ヺ、ㇰ-ㇿ）
                (c >= 0x30A0 && c <= 0x30FF) ||
                // 片假名扩展（ヵ-ヶ、ㇰ-ㇿ）
                (c >= 0x31F0 && c <= 0x31FF))
            {
                return true;
            }
        }
        return false;
    }

    // 判断是否为NotoSans系列字体
    private bool IsNotoSansFont(TMP_FontAsset fontAsset)
    {
        return fontAsset != null && (
            fontAsset.name.Contains("NotoSansKR-Regular") ||
            fontAsset.name.Contains("NotoSansJP-Regular") ||
            fontAsset.name.Contains("NotoSansSC-Regular")
        );
    }
}