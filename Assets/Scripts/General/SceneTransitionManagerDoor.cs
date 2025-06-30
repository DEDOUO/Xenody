using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneTransitionManagerDoor : MonoBehaviour
{
    public static SceneTransitionManagerDoor instance;

    // SongSelect 场景元素（需在Inspector中赋值）
    public GameObject jacketImage;       // 封面图
    public GameObject leftDoor;          // 左门
    public GameObject rightDoor;         // 右门
    private float animationDuration = 0.5f;  // 动画持续时间
    public GameObject transitionCanvas; // 新增：用于引用 TransitionCanvas 的游戏对象，需在Inspector赋值

    // 场景相机引用
    private Camera songSelectCamera;
    private Camera autoPlayCamera;

    // 场景切换状态
    public bool isTransitioning = false;
    private string targetScene = "";

    // 记录门的初始位置，用于开门时还原
    private Vector3 leftDoorInitialPos;
    private Vector3 rightDoorInitialPos;

    // 记录门的初始锚点和轴心信息，用于开门时还原
    private RectTransform leftDoorRectTrans;
    private Vector2 leftDoorInitialAnchorMin;
    private Vector2 leftDoorInitialAnchorMax;
    private Vector2 leftDoorInitialPivot;
    private RectTransform rightDoorRectTrans;
    private Vector2 rightDoorInitialAnchorMin;
    private Vector2 rightDoorInitialAnchorMax;
    private Vector2 rightDoorInitialPivot;

    // 用于接收 AutoPlay 初始化完成的回调标记
    public bool isAutoPlayInitialized = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化SongSelect场景的相机引用
            songSelectCamera = FindCameraInScene("SongSelect");

            // 记录门的初始位置、锚点、轴心信息
            if (leftDoor != null)
            {
                leftDoorRectTrans = leftDoor.GetComponent<RectTransform>();
                leftDoorInitialPos = leftDoor.transform.localPosition;
                leftDoorInitialAnchorMin = leftDoorRectTrans.anchorMin;
                leftDoorInitialAnchorMax = leftDoorRectTrans.anchorMax;
                leftDoorInitialPivot = leftDoorRectTrans.pivot;
            }
            if (rightDoor != null)
            {
                rightDoorRectTrans = rightDoor.GetComponent<RectTransform>();
                rightDoorInitialPos = rightDoor.transform.localPosition;
                rightDoorInitialAnchorMin = rightDoorRectTrans.anchorMin;
                rightDoorInitialAnchorMax = rightDoorRectTrans.anchorMax;
                rightDoorInitialPivot = rightDoorRectTrans.pivot;
            }
        }
        else
        {
            Debug.Log($"销毁重复的 SceneTransitionManager: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    public void StartTransitionToScene(string sceneName)
    {
        if (isTransitioning) return;

        isTransitioning = true;
        targetScene = sceneName;

        if (SceneManager.GetActiveScene().name == "SongSelect")
        {
            // 在加载新场景前，将 TransitionCanvas 及其子物体设置为 DontDestroyOnLoad
            if (transitionCanvas != null)
            {
                DontDestroyOnLoad(transitionCanvas);
            }
            else
            {
                Debug.LogError("TransitionCanvas 未赋值，请在Inspector中设置");
            }
            StartCoroutine(PlayEnterAnimationAndLoadScene());
        }
    }

    // 开始场景切换动画（对外暴露，让 AutoPlay 初始化完后调用真正的开门流程）
    public void StartRealTransition()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        StartCoroutine(PlayRealExitAnimationAndUnloadScene());
    }

    // 播放进入动画并加载场景（只做加载和初始化谱面相关，不执行开门）
    public IEnumerator PlayEnterAnimationAndLoadScene()
    {
        // 同时开始封面图和门的关闭动画
        yield return StartCoroutine(PlaySimultaneousAnimations());

        // 加载目标场景，使用替换模式
        yield return StartCoroutine(LoadReplaceScene(targetScene));

        // 标记加载场景阶段完成，等待 AutoPlay 初始化
        isTransitioning = false;
    }

    // 真正执行开门动画和卸载场景的协程（等 AutoPlay 初始化完后调用）
    private IEnumerator PlayRealExitAnimationAndUnloadScene()
    {
        // 同时播放门打开动画和封面图淡出动画
        Coroutine openDoorsCoroutine = StartCoroutine(OpenDoors());
        Coroutine fadeOutJacketCoroutine = StartCoroutine(FadeOutJacket());

        // 等待两个动画都完成
        yield return openDoorsCoroutine;
        yield return fadeOutJacketCoroutine;

        // 启用目标场景的相机
        if (autoPlayCamera != null)
        {
            autoPlayCamera.enabled = true;
            Debug.Log($"启用 {targetScene} 场景的相机");
        }

        // 卸载SongSelect场景
        yield return StartCoroutine(UnloadSongSelectScene());

        isTransitioning = false;
        //Debug.Log("场景切换完成");

        // 场景切换完成后，销毁 TransitionCanvas
        if (transitionCanvas != null)
        {
            Destroy(transitionCanvas);
        }
        // 卸载完成后，删除自身
        Destroy(gameObject);
    }

    // 同时播放封面图和门的关闭动画
    private IEnumerator PlaySimultaneousAnimations()
    {
        if (jacketImage == null || leftDoor == null || rightDoor == null)
        {
            Debug.LogError("动画对象缺失，无法播放同时动画");
            yield break;
        }

        // 创建并启动两个动画协程
        Coroutine moveJacketCoroutine = StartCoroutine(MoveJacketToCenter());
        Coroutine closeDoorsCoroutine = StartCoroutine(CloseDoors());

        // 等待两个动画都完成
        yield return moveJacketCoroutine;
        yield return closeDoorsCoroutine;
    }

    // 封面图移动到中央
    private IEnumerator MoveJacketToCenter()
    {
        if (jacketImage == null)
        {
            Debug.LogError("未找到封面图对象");
            yield break;
        }

        Vector3 startPos = jacketImage.transform.localPosition;
        Vector3 targetPos = new Vector3(0f, startPos.y, startPos.z);
        float timer = 0f;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            jacketImage.transform.localPosition = Vector3.Lerp(startPos, targetPos,
                EaseInOutQuad(timer, animationDuration));
            yield return null;
        }

        jacketImage.transform.localPosition = targetPos;
    }

    // 加载Replace场景，即卸载当前场景并加载新场景
    private IEnumerator LoadReplaceScene(string sceneName)
    {
        //Debug.Log($"加载场景: {sceneName} (Replace模式)");

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        // 激活新场景
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
    }

    // 关闭门动画 
    private IEnumerator CloseDoors()
    {
        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogError("未找到门对象");
            yield break;
        }

        // 记录关门开始前的位置
        Vector3 leftStartPos = leftDoor.transform.localPosition;
        Vector3 rightStartPos = rightDoor.transform.localPosition;

        // 目标锚点和轴心（正中央）
        Vector2 targetAnchorMin = new Vector2(0.5f, 0.5f);
        Vector2 targetAnchorMax = new Vector2(0.5f, 0.5f);
        Vector2 targetPivot = new Vector2(0.5f, 0.5f);

        // 先设置锚点和轴心为目标状态
        if (leftDoorRectTrans != null)
        {
            leftDoorRectTrans.anchorMin = targetAnchorMin;
            leftDoorRectTrans.anchorMax = targetAnchorMax;
            leftDoorRectTrans.pivot = targetPivot;
        }
        if (rightDoorRectTrans != null)
        {
            rightDoorRectTrans.anchorMin = targetAnchorMin;
            rightDoorRectTrans.anchorMax = targetAnchorMax;
            rightDoorRectTrans.pivot = targetPivot;
        }

        // 计算关门后的目标位置（基于新锚点和轴心的中央位置）
        Vector3 leftEndPos = new Vector3(0f, leftStartPos.y, leftStartPos.z);
        Vector3 rightEndPos = new Vector3(0f, rightStartPos.y, rightStartPos.z);

        float timer = 0f;
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            leftDoor.transform.localPosition = Vector3.Lerp(leftStartPos, leftEndPos,
                EaseInOutQuad(timer, animationDuration));
            rightDoor.transform.localPosition = Vector3.Lerp(rightStartPos, rightEndPos,
                EaseInOutQuad(timer, animationDuration));
            yield return null;
        }

        leftDoor.transform.localPosition = leftEndPos;
        rightDoor.transform.localPosition = rightEndPos;
    }

    // 打开门动画（回到初始记录的位置、锚点、轴心）
    private IEnumerator OpenDoors()
    {
        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogError("未找到门对象");
            yield break;
        }

        // 记录开门开始前的位置
        Vector3 leftStartPos = leftDoor.transform.localPosition;
        Vector3 rightStartPos = rightDoor.transform.localPosition;

        // 目标位置为初始位置（基于初始锚点和轴心）
        Vector3 leftEndPos = leftDoorInitialPos;
        Vector3 rightEndPos = rightDoorInitialPos;

        float timer = 0f;
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            leftDoor.transform.localPosition = Vector3.Lerp(leftStartPos, leftEndPos,
                EaseInOutQuad(timer, animationDuration));
            rightDoor.transform.localPosition = Vector3.Lerp(rightStartPos, rightEndPos,
                EaseInOutQuad(timer, animationDuration));
            yield return null;
        }

        leftDoor.transform.localPosition = leftEndPos;
        rightDoor.transform.localPosition = rightEndPos;

        // 还原锚点和轴心为初始状态
        if (leftDoorRectTrans != null)
        {
            leftDoorRectTrans.anchorMin = leftDoorInitialAnchorMin;
            leftDoorRectTrans.anchorMax = leftDoorInitialAnchorMax;
            leftDoorRectTrans.pivot = leftDoorInitialPivot;
        }
        if (rightDoorRectTrans != null)
        {
            rightDoorRectTrans.anchorMin = rightDoorInitialAnchorMin;
            rightDoorRectTrans.anchorMax = rightDoorInitialAnchorMax;
            rightDoorRectTrans.pivot = rightDoorInitialPivot;
        }
    }

    // 封面图淡出动画
    private IEnumerator FadeOutJacket()
    {
        if (jacketImage == null)
        {
            Debug.LogError("未找到封面图对象");
            yield break;
        }

        CanvasGroup canvasGroup = jacketImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = jacketImage.AddComponent<CanvasGroup>();
        }

        float timer = 0f;
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, EaseInOutQuad(timer, animationDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        jacketImage.SetActive(false);
    }

    // 卸载SongSelect场景
    private IEnumerator UnloadSongSelectScene()
    {
        Scene oldScene = SceneManager.GetSceneByName("SongSelect");
        if (oldScene.isLoaded)
        {
            AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(oldScene);

            while (!asyncOperation.isDone)
            {
                yield return null;
            }

            songSelectCamera = null;
            Debug.Log("SongSelect场景卸载完成");
        }
    }

    // 查找指定场景中的相机
    private Camera FindCameraInScene(string sceneName)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.gameObject.scene.name == sceneName)
        {
            return mainCamera;
        }

        Debug.LogWarning($"未找到 {sceneName} 场景中的主相机");
        return null;
    }

    // 改进的缓动函数 - 二次方缓入缓出，接受当前时间和总时长
    private float EaseInOutQuad(float currentTime, float duration)
    {
        float t = currentTime / duration;
        if (t < 0.5f)
            return 2f * t * t;
        else
            return -1f + (4f - 2f * t) * t;
    }
}