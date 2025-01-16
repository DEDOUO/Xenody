using UnityEngine;


public class OverlayCameraManager : MonoBehaviour
{
    public Camera[] overlayCameras;


    void Start()
    {
        // 调用方法来设置 Overlay Camera 的深度，从而控制它们的渲染顺序
        //SetOverlayCameraDepth();
    }


    void SetOverlayCameraDepth()
    {
        // 按顺序设置每个 Overlay Camera 的深度，深度越大，渲染越靠后，会覆盖在前面的画面上
        for (int i = 0; i < overlayCameras.Length; i++)
        {
            if (overlayCameras[i] != null)
            {
                overlayCameras[i].depth = i;
                //Debug.Log(overlayCameras[i] + "..." + i);
            }
            else
            {
                Debug.LogWarning("Overlay Camera at index " + i + " is null.");
            }
        }
    }


    //void Update()
    //{
    //    // 你可以在此处添加额外的逻辑，根据不同的游戏状态或用户输入动态调整 Overlay Camera 的深度
    //    // 例如，根据用户输入交换两个 Overlay Camera 的深度
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        SwapOverlayCameraDepth(0, 1);
    //    }
    //}


    //void SwapOverlayCameraDepth(int index1, int index2)
    //{
    //    if (index1 >= 0 && index1 < overlayCameras.Length && index2 >= 0 && index2 < overlayCameras.Length)
    //    {
    //        float tempDepth = overlayCameras[index1].depth;
    //        overlayCameras[index1].depth = overlayCameras[index2].depth;
    //        overlayCameras[index2].depth = tempDepth;
    //    }
    //    else
    //    {
    //        Debug.LogError("Invalid indices for swapping Overlay Camera depth.");
    //    }
    //}
}