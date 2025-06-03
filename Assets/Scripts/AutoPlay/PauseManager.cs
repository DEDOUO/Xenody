using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using static Utility;

public class PauseManager : MonoBehaviour
{
    public bool isPaused = false;
    public AudioSource audioSource;
    public GameObject MusicSlider;
    public MusicAndChartPlayer musicAndChartPlayer; // 引用 MusicAndChartPlayer 实例
    public AspectRatioManager aspectRatioManager; // 引用 AspectRatioManager 实例

    private Slider slider;
    //private bool isDragging = false;

    private void Start()
    {
        musicAndChartPlayer = GetComponent<MusicAndChartPlayer>();
        aspectRatioManager = GetComponent<AspectRatioManager>();
        audioSource = musicAndChartPlayer.audioSource;
        MusicSlider = musicAndChartPlayer.MusicSlider;

        // 初始化滑块引用并添加事件监听
        if (MusicSlider != null)
        {
            slider = MusicSlider.GetComponent<Slider>();
            if (slider != null)
            {
                slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }
    }

    // 实现 IBeginDragHandler 接口（开始拖动时触发）
    //public void OnBeginDrag(PointerEventData eventData)
    //{
    //    if (eventData.pointerDrag != null && eventData.pointerDrag.TryGetComponent<Slider>(out Slider slider))
    //    {
    //        isDragging = true;
    //    }
    //}

    //// 实现 IEndDragHandler 接口（结束拖动时触发）
    //public void OnEndDrag(PointerEventData eventData)
    //{
    //    isDragging = false;
    //}

    public void CheckPauseButtonClick()
    {
        // 确保 EventSystem 存在
        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem 未找到，请在场景中添加 EventSystem 组件。");
            return;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);

        // 检查结果列表是否为空
        if (results.Count > 0)
        {
            foreach (RaycastResult result in results)
            {
                if (result.gameObject.CompareTag("PauseButton"))
                {
                    TogglePause();
                    break;
                }
            }
        }
        else
        {
            Debug.Log("Raycast 没有找到任何结果。");
        }
    }

    public void TogglePause()
    {
        audioSource = musicAndChartPlayer.audioSource;
        //MusicSlider = musicAndChartPlayer.MusicSlider;
        GameObject spectrumBorder = aspectRatioManager.spectrumBorder;

        if (spectrumBorder != null)
        {
            if (isPaused)
            {
                spectrumBorder.SetActive(false);
                SetChildrenActive(spectrumBorder, false);
            }
            else
            {
                spectrumBorder.SetActive(true);
                SetChildrenActive(spectrumBorder, true);
            }
        }

        // 如果恢复播放
        if (isPaused)
        {
            // 默认鼠标拖动的时候，Note也会实时更新

            audioSource.time = slider.value * audioSource.clip.length;
            musicAndChartPlayer.ResetAllNotes(audioSource.time);
            CheckArrowVisibility(audioSource.time,musicAndChartPlayer.subStarInfoDict,musicAndChartPlayer.SubStarsParent);

            musicAndChartPlayer.elapsedTime = audioSource.time;
            musicAndChartPlayer.accumulatedTime = 0f;
            MusicSlider.SetActive(false);
            audioSource.Play();
            musicAndChartPlayer.IsPlaying = true;
            isPaused = false;
        }
        // 如果暂停
        else
        {
            audioSource.Pause();
            musicAndChartPlayer.IsPlaying = false;
            isPaused = true;
            MusicSlider.SetActive(true);
            //Debug.Log(slider);
            //Debug.Log(audioSource);
            slider.value = audioSource.time / audioSource.clip.length;
        }
    }

    // 滑块值变化时立即更新谱面
    private void OnSliderValueChanged(float value)
    {
        if (isPaused && slider != null && audioSource != null && musicAndChartPlayer != null)
        {
            float time = value * audioSource.clip.length;
            audioSource.time = time;
            musicAndChartPlayer.elapsedTime = audioSource.time;
            musicAndChartPlayer.accumulatedTime = 0f;
            musicAndChartPlayer.ResetAllNotes(time);
            CheckArrowVisibility(time,
                musicAndChartPlayer.subStarInfoDict,
                musicAndChartPlayer.SubStarsParent);
        }
    }


    private void SetChildrenActive(GameObject parent, bool active)
    {
        foreach (Transform child in parent.transform)
        {
            child.gameObject.SetActive(active);
        }
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}