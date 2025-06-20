using UnityEngine;
//using System.Collections.Generic;
//using System.Threading.Tasks;
using TMPro;
//using DocumentFormat.OpenXml.Office2010.ExcelAc;


public class AutoPlay : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioSource[] audioSources;
    public AudioSource TapSoundEffect;
    public AudioSource SlideSoundEffect;
    public AudioSource FlickSoundEffect;
    public AudioSource HoldSoundEffect;
    public AudioSource StarHeadSoundEffect;
    public AudioSource StarSoundEffect;

    public GameObject AutoPlayService; // 用于挂载 MusicAndChartLoader 组件的游戏对象
    public GameObject JudgePlanesParent;
    public GameObject JudgeLinesParent;
    public GameObject ColorLinesParent;
    public GameObject TapsParent;
    public GameObject SlidesParent;
    public GameObject FlicksParent;
    public GameObject FlickArrowsParent;
    public GameObject HoldsParent;
    public GameObject HoldOutlinesParent;
    public GameObject StarsParent;
    public GameObject subStarsParent;
    public GameObject JudgeTexturesParent;
    public GameObject MultiHitLinesParent;
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private TMP_Text ComboText;
    [SerializeField] private TMP_Text ScoreText;


    //public RectTransform SubStarsParentRect;

    public Sprite JudgePlaneSprite;
    public Sprite HoldSprite;
    //public Sprite StarArrowSprite;
    public GlobalRenderOrderManager renderOrderManager;
    public GameObject AnimatorContainer;


    //private List<float> JudgePlanesStartT = new List<float>(); // 判定面的开始时间（用于JudgeLine出现时间计算）
    //private List<float> JudgePlanesEndT = new List<float>(); // 判定面的结束时间（用于JudgeLine结束时间计算）
    //public Dictionary<float, List<string>> startTimeToInstanceNames = new Dictionary<float, List<string>>(); // 存储startT到对应实例名列表的映射

    //private Dictionary<GameObject, bool> tapReachedJudgmentLine = new Dictionary<GameObject, bool>();
    private void Awake()
    {
        // 查找场景内游戏物体
        AutoPlayService = GameObject.Find("AutoPlayService");
        JudgePlanesParent = GameObject.Find("JudgePlanesParent");
        JudgeLinesParent = GameObject.Find("JudgeLinesParent");
        ColorLinesParent = GameObject.Find("ColorLinesParent");
        TapsParent = GameObject.Find("TapsParent");
        SlidesParent = GameObject.Find("SlidesParent");
        FlicksParent = GameObject.Find("FlicksParent");
        FlickArrowsParent = GameObject.Find("FlickArrowsParent");
        HoldsParent = GameObject.Find("HoldsParent");
        HoldOutlinesParent = GameObject.Find("HoldOutlinesParent");
        StarsParent = GameObject.Find("StarsParent");
        subStarsParent = GameObject.Find("SubStarsParent");
        JudgeTexturesParent = GameObject.Find("JudgeTexturesParent");
        MultiHitLinesParent = GameObject.Find("MultiHitLinesParent");
        fpsText = GameObject.Find("FPS").GetComponent<TextMeshProUGUI>();
        ComboText = GameObject.Find("ComboText").GetComponent<TextMeshProUGUI>();
        ScoreText = GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();

        //SubStarsParentRect = subStarsParent.GetComponent<RectTransform>();

        // 加载Sprite
        JudgePlaneSprite = Resources.Load<Sprite>("Sprites/TrackBlack2");
        if (JudgePlaneSprite == null)
        {
            Debug.LogError("Failed to load sprite: Sprites/TrackBlack2");
        }
        HoldSprite = Resources.Load<Sprite>("Sprites/HoldBlueSprite2");
        if (HoldSprite == null)
        {
            Debug.LogError("Failed to load sprite: Sprites/HoldBlueSprite2");
        }

        // 查找包含AudioSource的GameObject
        GameObject audioObj = GameObject.Find("AudioService");
        if (audioObj != null)
        {
            audioSource = audioObj.GetComponent<AudioSource>();
        }

        // 获取音效源
        GameObject SoundObj = GameObject.Find("HitsoundService");
        if (SoundObj != null)
        {

            audioSources = SoundObj.GetComponents<AudioSource>();
            TapSoundEffect = audioSources[0];
            SlideSoundEffect = audioSources[1];
            FlickSoundEffect = audioSources[2];
            //Hold与Tap音效一致
            HoldSoundEffect = audioSources[0];

            StarHeadSoundEffect = audioSources[3];
            StarSoundEffect = audioSources[4];

        }

        // 查找包含GlobalRenderOrderManager的GameObject
        GameObject renderObj = GameObject.Find("GlobalRenderOrderManager");
        if (renderObj != null)
        {
            renderOrderManager = renderObj.GetComponent<GlobalRenderOrderManager>();
        }

        // 查找Animator Container
        AnimatorContainer = GameObject.Find("AnimatorContainer");
    }

    private async void Start()
    {

        //跟据屏幕长宽比确认谱面范围
        AspectRatioManager aspectRatioManager = GetComponent<AspectRatioManager>();
        aspectRatioManager.SetupSpectrumBounds();

        // 加载音乐和谱面文件
        //MusicAndChartLoader loader = new MusicAndChartLoader(audioSource);
        MusicAndChartLoader loader = GetComponent<MusicAndChartLoader>();
        loader.Initialize(audioSource);
        await loader.LoadMusicAndChartAsync();

        // 获取加载后的谱面和音频源
        Chart chart = loader.GetChart();

        // 实例化谱面内容
        ChartInstantiator instantiator = GetComponent<ChartInstantiator>();
        instantiator.SetParameters(JudgePlanesParent, JudgeLinesParent, ColorLinesParent, TapsParent, SlidesParent, FlicksParent, FlickArrowsParent, HoldsParent, HoldOutlinesParent, StarsParent, subStarsParent, JudgeTexturesParent, MultiHitLinesParent,
            JudgePlaneSprite, HoldSprite, renderOrderManager, AnimatorContainer, fpsText);
        instantiator.InstantiateAll(chart);

        // 计算连击和得分列表
        ScoreManager scoreManager = GetComponent<ScoreManager>();
        scoreManager.CalculateAutoPlayScores(chart);


        // 先禁用MusicAndChartPlayer组件，避免在谱面加载时其Update方法干扰
        MusicAndChartPlayer player = GetComponent<MusicAndChartPlayer>();


        if (player == null)
        {
            Debug.LogError("未找到 MusicAndChartPlayer 组件，请确保该组件已挂载到当前对象！");
            return;
        }
        player.enabled = false;

        // 播放音乐和更新谱面位置
        player.SetParameters(audioSource, JudgePlanesParent, JudgeLinesParent, ColorLinesParent, TapsParent, SlidesParent, FlicksParent, FlickArrowsParent, HoldsParent, HoldOutlinesParent, StarsParent, subStarsParent, JudgeTexturesParent, MultiHitLinesParent,
            TapSoundEffect, SlideSoundEffect, FlickSoundEffect, HoldSoundEffect, StarHeadSoundEffect, StarSoundEffect, chart, fpsText, ComboText, ScoreText);
        player.SetParameters2(instantiator.startTimeToInstanceNames, instantiator.holdTimes, instantiator.keyReachedJudgment, 
            instantiator.JudgePlanesStartT, instantiator.JudgePlanesEndT, instantiator.subStarInfoDict, instantiator.starTrackTimes, instantiator.GradientColorList, instantiator.ChartStartTime);
        player.SetParameters3(scoreManager.SumComboMap, scoreManager.SumScoreMap, scoreManager.JudgePosMap);
        player.enabled = true;
        player.PlayMusicAndChart(chart);
    }
}