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
    public AspectRatioManager aspectRatioManager; // 引用 MusicAndChartPlayer 实例

    private void Start()
    {
        musicAndChartPlayer = GetComponent<MusicAndChartPlayer>();
        aspectRatioManager = GetComponent<AspectRatioManager>();
    }

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
        MusicSlider = musicAndChartPlayer.MusicSlider;
        GameObject spectrumBorder = aspectRatioManager.spectrumBorder;
        //Debug.Log(spectrumBorder);
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
        //如果恢复播放
        if (isPaused)
        {
            Slider slider = MusicSlider.GetComponent<Slider>();
            audioSource.time = slider.value * audioSource.clip.length;
            musicAndChartPlayer.ResetAllNotes(audioSource.time);
            CheckArrowVisibility(musicAndChartPlayer.SubStarsParent, audioSource.time, musicAndChartPlayer.subStarInfoDict);


            MusicSlider.SetActive(false);
            audioSource.Play();
            isPaused = false;
        }
        //如果暂停
        else
        {
            audioSource.Pause();
            isPaused = true;
            MusicSlider.SetActive(true);
            Slider slider = MusicSlider.GetComponent<Slider>();
            slider.value = audioSource.time / audioSource.clip.length;
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