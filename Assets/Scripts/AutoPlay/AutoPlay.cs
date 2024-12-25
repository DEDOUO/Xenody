using UnityEngine;
using System.Collections.Generic;
 

public class AutoPlay : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioSource[] audioSources;
    public AudioSource TapSoundEffect;
    public AudioSource SlideSoundEffect;
    public AudioSource FlickSoundEffect;
    public AudioSource HoldSoundEffect;
    public AudioSource StarHeadSoundEffect;

    public GameObject JudgePlanesParent;
    public GameObject JudgeLinesParent;
    public GameObject TapsParent;
    public GameObject SlidesParent;
    public GameObject FlicksParent;
    public GameObject HoldsParent;
    public GameObject StarsParent;

    public Sprite JudgePlaneSprite;
    public Sprite HoldSprite;
    public GlobalRenderOrderManager renderOrderManager;
    public GameObject AnimatorContainer;
    

    private Dictionary<GameObject, bool> tapReachedJudgmentLine = new Dictionary<GameObject, bool>();
    private void Awake()
    {
        // 查找JudgePlanesParent
        JudgePlanesParent = GameObject.Find("JudgePlanesParent");
        // 查找JudgeLinesParent
        JudgeLinesParent = GameObject.Find("JudgeLinesParent");
        // 查找TapsParent
        TapsParent = GameObject.Find("TapsParent");
        // 查找SlidesParent
        SlidesParent = GameObject.Find("SlidesParent");
        // 查找FlicksParent
        FlicksParent = GameObject.Find("FlicksParent");
        // 查找HoldsParent
        HoldsParent = GameObject.Find("HoldsParent");
        // 查找StarsParent
        StarsParent = GameObject.Find("StarsParent");

        // 加载JudgePlaneSprite
        JudgePlaneSprite = Resources.Load<Sprite>("Sprites/TrackBlack");

        // 加载JudgePlaneSprite
        HoldSprite = Resources.Load<Sprite>("Sprites/TrackConflict");

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
            //TapSoundEffect = SoundObj.GetComponent<AudioSource>();
            //SlideSoundEffect = SoundObj.GetComponent<AudioSource>();
            //FlickSoundEffect = SoundObj.GetComponent<AudioSource>();

            audioSources = SoundObj.GetComponents<AudioSource>();
            TapSoundEffect = audioSources[0];
            SlideSoundEffect = audioSources[1];
            FlickSoundEffect = audioSources[2];
            //Hold与Tap音效一致
            HoldSoundEffect = audioSources[0];

            StarHeadSoundEffect = audioSources[3];

            // 检查并处理AudioClip
            //if (TapSoundEffect.clip != null)
            //{
            //    Debug.Log(TapSoundEffect.clip);
            //}
            //if (SlideSoundEffect.clip != null)
            //{
            //    Debug.Log(SlideSoundEffect.clip);
            //}
            //if (FlickSoundEffect.clip != null)
            //{
            //    Debug.Log(FlickSoundEffect.clip);
            //}
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

    private void Start()
    {
        // 加载音乐和谱面文件
        MusicAndChartLoader loader = new MusicAndChartLoader(audioSource);
        loader.LoadMusicAndChart();

        // 获取加载后的谱面和音频源
        Chart chart = loader.GetChart();
        //audioSource = loader.GetAudioSource();

        // 实例化谱面内容
        ChartInstantiator instantiator = GetComponent<ChartInstantiator>();
        instantiator.SetParameters(JudgePlanesParent, JudgeLinesParent, TapsParent, SlidesParent, FlicksParent, HoldsParent, StarsParent,
            JudgePlaneSprite, HoldSprite, 
            renderOrderManager, AnimatorContainer);

        // 先禁用MusicAndChartPlayer组件，避免在谱面加载时其Update方法干扰
        MusicAndChartPlayer player = GetComponent<MusicAndChartPlayer>();
        player.enabled = false;

        instantiator.InstantiateJudgePlanesAndJudgeLines(chart);
        instantiator.InstantiateTaps(chart);
        instantiator.InstantiateSlides(chart);
        instantiator.InstantiateFlicks(chart);
        instantiator.InstantiateHolds(chart);
        instantiator.InstantiateStarHeads(chart);

        // 播放音乐和更新谱面位置
        player.SetParameters(audioSource, JudgePlanesParent, JudgeLinesParent, TapsParent, SlidesParent, FlicksParent, HoldsParent, StarsParent,
            TapSoundEffect, SlideSoundEffect, FlickSoundEffect, HoldSoundEffect, StarHeadSoundEffect, chart);
        player.enabled = true;
        player.PlayMusicAndChart(chart);
    }
}