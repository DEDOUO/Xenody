using Params;
using UnityEngine;

public class AspectRatioManager : MonoBehaviour
{
    public Camera mainCamera; // 主摄像机
    public Material lineMaterial; // 线条材质
    public GameObject spectrumBorder; // 将 spectrumBorder 作为类的成员变量
    // 将剪裁后的屏幕宽度和高度改为静态变量
    public static float croppedScreenWidth;
    public static float croppedScreenHeight;

    public void SetupSpectrumBounds()
    {
        // 在 Start 方法中查找或创建 SpectrumBorder 对象
        spectrumBorder = GameObject.Find("SpectrumBorder");
        if (spectrumBorder == null)
        {
            spectrumBorder = new GameObject("SpectrumBorder");
        }

        // 清空其子物体
        foreach (Transform child in spectrumBorder.transform)
        {
            Destroy(child.gameObject);
        }

        // 获取屏幕的宽度和高度
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 计算屏幕的长宽比
        float screenAspectRatio = screenWidth / screenHeight;

        // 定义谱面的目标长宽比
        float targetAspectRatio = AspectRatioParams.AspectRatioDefault;

        float left, right, top, bottom;

        if (screenAspectRatio > targetAspectRatio)
        {
            // 屏幕较宽，左右截掉一部分
            float newWidth = screenHeight * targetAspectRatio;
            float offset = (screenWidth - newWidth) / 2f;
            left = offset;
            right = screenWidth - offset;
            top = screenHeight;
            bottom = 0;
            croppedScreenWidth = newWidth;
            croppedScreenHeight = screenHeight;
            Debug.Log($"左右截掉部分，谱面显示区间：左 {offset}，右 {screenWidth - offset}");
        }
        else
        {
            // 屏幕较方，上下截掉一部分
            float newHeight = screenWidth / targetAspectRatio;
            float offset = (screenHeight - newHeight) / 2f;
            left = 0;
            right = screenWidth;
            top = screenHeight - offset;
            bottom = offset;
            croppedScreenWidth = screenWidth;
            croppedScreenHeight = newHeight;
            Debug.Log($"上下截掉部分，谱面显示区间：上 {offset}，下 {screenHeight - offset}");
        }

        // 将屏幕坐标转换为世界坐标，深度设置为 nearClipPlane 的两倍
        Vector3 topLeft = mainCamera.ScreenToWorldPoint(new Vector3(left, top, mainCamera.nearClipPlane * 2));
        Vector3 topRight = mainCamera.ScreenToWorldPoint(new Vector3(right, top, mainCamera.nearClipPlane * 2));
        Vector3 bottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(left, bottom, mainCamera.nearClipPlane * 2));
        Vector3 bottomRight = mainCamera.ScreenToWorldPoint(new Vector3(right, bottom, mainCamera.nearClipPlane * 2));

        // 创建四条 LineRenderer 来绘制边框
        CreateLine(topLeft, topRight, "TopBorder", spectrumBorder);
        CreateLine(topRight, bottomRight, "RightBorder", spectrumBorder);
        CreateLine(bottomRight, bottomLeft, "BottomBorder", spectrumBorder);
        CreateLine(bottomLeft, topLeft, "LeftBorder", spectrumBorder);

        spectrumBorder.SetActive(false);
    }

    void CreateLine(Vector3 start, Vector3 end, string lineName, GameObject parent)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(parent.transform);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}
