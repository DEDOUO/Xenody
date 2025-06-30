using UnityEngine;

public class FrameRateLock : MonoBehaviour
{
    void Start()
    {
        // 关键代码：设置目标帧率为 120
        Application.targetFrameRate = 120;

        // 确保垂直同步已关闭（否则 targetFrameRate 可能失效）
        QualitySettings.vSyncCount = 0;
    }
}