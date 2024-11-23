using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
//using System.IO;
using TMPro;

// 选歌界面的脚本，负责处理歌曲列表展示和选择歌曲后跳转到播放场景的逻辑
public class SongSelectScript : MonoBehaviour
{
    public ScrollRect songListScrollView; // 在Unity中拖拽赋值，指向展示歌曲列表的滚动视图
    private List<string> songList = new List<string>(); // 用于存储歌曲名称的列表

    private void Start()
    {
        // 这里暂时只添加一首歌曲“Accelerate”，后续可从文件夹读取更多歌曲名添加进来
        songList.Add("Accelerate");
        PopulateSongList();
    }

    // 填充歌曲列表，创建按钮并添加点击事件监听
    private void PopulateSongList()
    {
        // 设置按钮的初始垂直位置偏移量，用于控制按钮在滚动视图中的排列位置
        float horizontalOffset = 40f;
        float verticalOffset = 40f;
        // 按钮的固定高度，可根据实际需求调整
        float buttonHeight = 50f;
        // 按钮之间的垂直间距，可根据实际需求调整
        float spacing = 20f;
        // 按钮的固定宽度，可根据实际需求调整，避免文字挤成竖线的情况
        float buttonWidth = 400f;
        // 新增变量，用于记录歌曲序号，从1开始
        int songIndex = 1; 

        foreach (string song in songList)
        {
            // 创建按钮对象，并确保添加了必要的UI组件（Button、RectTransform等）
            GameObject buttonObj = new GameObject($"Song{songIndex}", typeof(Button));
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
                textComponent.alignment = TextAlignmentOptions.Midline; // 设置文本对齐方式，与之前Text组件略有不同
            }
            textComponent.text = song;

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

            // 设置按钮文字的样式（字体、字号、颜色等）
            //textComponent.font = Resources.GetBuiltinResource<Font>("Jupiter.ttf");
            //textComponent.font = Resources.Load<Font>("Fonts/FontFiles/Jupiter");
            textComponent.font = Resources.Load<TMP_FontAsset>("Fonts/Jupiter SDF"); // 注意这里加载的是TextMeshPro的字体资源

            textComponent.fontSize = 50;
            textComponent.color = Color.white;

            button.onClick.AddListener(() =>
            {
                SongAndChartData.SetSelectedSong(song);
                SceneManager.LoadScene("AutoPlay");
            });

            buttonObj.transform.SetParent(songListScrollView.content);
            buttonObj.transform.localScale = Vector3.one;

            // 获取按钮的RectTransform组件，用于设置布局相关属性
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            // 设置按钮的锚点，均对齐左上角
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            // 设置按钮在垂直方向上的位置，基于之前的偏移量和间距来排列
            rectTransform.anchoredPosition = new Vector2(horizontalOffset, -verticalOffset);
            // 设置按钮的初始大小
            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            // 更新垂直偏移量，为下一个按钮的位置做准备
            verticalOffset += buttonHeight + spacing;

            songIndex++; // 每处理完一首歌曲对应的按钮，序号加1
        }
    }
}