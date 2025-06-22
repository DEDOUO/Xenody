////using UnityEngine;
////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine.SceneManagement;

////public class AudioListenerManager : MonoBehaviour
////{
////    // 单例模式
////    private static AudioListenerManager instance;
////    public static AudioListenerManager Instance
////    {
////        get
////        {
////            // 确保实例已初始化
////            if (instance == null)
////            {
////                instance = FindObjectOfType<AudioListenerManager>();

////                // 如果场景中没有，创建一个新的
////                if (instance == null)
////                {
////                    GameObject managerObj = new GameObject("AudioListenerManager");
////                    instance = managerObj.AddComponent<AudioListenerManager>();
////                    DontDestroyOnLoad(managerObj);
////                    //Debug.Log("自动创建AudioListenerManager实例");
////                }
////            }
////            return instance;
////        }
////    }

////    // 记录当前活动的AudioListener
////    public AudioListener activeListener;

////    private void Awake()
////    {
////        // 实现单例模式
////        if (instance == null)
////        {
////            instance = this;
////            DontDestroyOnLoad(gameObject);
////            //Debug.Log("AudioListenerManager实例已创建");
////        }
////        else
////        {
////            // 如果已经有实例，销毁当前对象
////            Destroy(gameObject);
////            return;
////        }
////    }

////    // 查找并设置活动的AudioListener
////    public void SetupActiveListener()
////    {
////        // 查找场景中所有的AudioListener
////        AudioListener[] allListeners = FindObjectsOfType<AudioListener>();

////        // 情况1：没有AudioListener
////        if (allListeners.Length == 0)
////        {
////            //Debug.LogWarning("场景中没有找到AudioListener，将创建一个新的");
////            CreateNewAudioListener();
////            return;
////        }

////        // 情况2：有多个AudioListener
////        if (allListeners.Length > 1)
////        {
////            //Debug.LogWarning("场景中发现多个AudioListener，将保留一个并禁用其他");
////            DisableExtraListeners(allListeners);
////        }

////        // 情况3：有一个AudioListener，设置为活动状态
////        activeListener = allListeners[0];
////        //Debug.Log("已设置活动AudioListener: " + activeListener.gameObject.name);
////    }

////    // 创建新的AudioListener
////    private void CreateNewAudioListener()
////    {
////        GameObject listenerObj = new GameObject("AudioListener");
////        activeListener = listenerObj.AddComponent<AudioListener>();
////        // 将新的AudioListener放置在主摄像机位置
////        Camera mainCam = Camera.main;
////        if (mainCam != null)
////        {
////            listenerObj.transform.position = mainCam.transform.position;
////            listenerObj.transform.rotation = mainCam.transform.rotation;
////        }
////        else
////        {
////            // 如果没有主摄像机，使用默认位置
////            listenerObj.transform.position = Vector3.zero;
////        }
////    }

////    // 禁用多余的AudioListener
////    private void DisableExtraListeners(AudioListener[] listeners)
////    {
////        // 选择第一个作为活动Listener
////        activeListener = listeners[0];

////        // 禁用其他所有Listener
////        for (int i = 1; i < listeners.Length; i++)
////        {
////            // 检查是否属于特定场景的Listener
////            if (listeners[i].gameObject.scene.name != "AutoPlay")
////            {
////                listeners[i].gameObject.SetActive(false);
////                Debug.Log("已禁用非活动AudioListener: " + listeners[i].gameObject.name);
////            }
////        }
////    }

////    // 在场景切换时更新AudioListener
////    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
////    {
////        SetupActiveListener();
////    }

////    // 当卸载场景时检查AudioListener
////    public void OnSceneUnloaded(Scene scene)
////    {
////        // 如果卸载的场景包含当前活动的Listener
////        if (activeListener != null && activeListener.gameObject.scene == scene)
////        {
////            Debug.Log("活动AudioListener所在场景已卸载，重新设置");
////            activeListener = null;
////            SetupActiveListener();
////        }
////    }
////}


//using UnityEngine;
//using UnityEngine.SceneManagement;
//using System.Collections;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public class SceneTransitionManager : MonoBehaviour
//{
//    public static SceneTransitionManager instance;

//    // SongSelect 场景元素（需在Inspector中赋值）
//    public GameObject jacketImage;       // 封面图
//    public GameObject leftDoor;          // 左门
//    public GameObject rightDoor;         // 右门
//    public float animationDuration = 0.5f;  // 动画持续时间
//    public Canvas songSelectCanvas;

//    // 场景相机引用
//    private Camera songSelectCamera;
//    private Camera autoPlayCamera;

//    // 场景切换状态
//    public bool isTransitioning = false;
//    private string targetScene = "";

//    // 新增：记录门的初始位置，用于开门时还原
//    private Vector3 leftDoorInitialPos;
//    private Vector3 rightDoorInitialPos;

//    // 新增：用于接收 AutoPlay 初始化完成的回调标记
//    public bool isAutoPlayInitialized = false;

//    void Awake()
//    {
//        if (instance == null)
//        {
//            instance = this;
//            DontDestroyOnLoad(gameObject);

//            // 初始化SongSelect场景的相机引用
//            songSelectCamera = FindCameraInScene("SongSelect");

//            // 记录门的初始位置
//            if (leftDoor != null) leftDoorInitialPos = leftDoor.transform.localPosition;
//            if (rightDoor != null) rightDoorInitialPos = rightDoor.transform.localPosition;
//            //Debug.Log(leftDoorInitialPos);
//            //Debug.Log(rightDoorInitialPos);
//        }
//        else
//        {
//            Debug.Log($"销毁重复的 SceneTransitionManager: {gameObject.name}");
//            Destroy(gameObject);
//        }
//    }

//    public void StartTransitionToScene(string sceneName)
//    {
//        if (isTransitioning) return;

//        isTransitioning = true;
//        targetScene = sceneName;

//        if (SceneManager.GetActiveScene().name == "SongSelect")
//        {
//            StartCoroutine(PlayEnterAnimationAndLoadScene());
//        }
//    }


//    // 开始场景切换动画（对外暴露，让 AutoPlay 初始化完后调用真正的开门流程）
//    public void StartRealTransition()
//    {
//        if (isTransitioning) return;

//        isTransitioning = true;
//        StartCoroutine(PlayRealExitAnimationAndUnloadScene());
//    }

//    // 播放进入动画并加载场景（只做加载和初始化谱面相关，不执行开门）
//    public IEnumerator PlayEnterAnimationAndLoadScene()
//    {
//        // 提前初始化AudioListenerManager
//        AudioListenerManager.Instance.SetupActiveListener();

//        // 同时开始封面图和门的关闭动画
//        yield return StartCoroutine(PlaySimultaneousAnimations());

//        // 卸载 SongSelect 场景的 EventSystem 和非活动AudioListener
//        yield return StartCoroutine(UnloadSongSelectSystemComponents());

//        // 处理 Panel 内容，隐藏选歌残留
//        HideSongSelectPanelContent();

//        // 加载目标场景
//        yield return StartCoroutine(LoadAdditiveScene(targetScene));

//        // 新增：加载完成后立即检查AudioListener
//        AudioListenerManager.Instance.SetupActiveListener();

//        // 标记加载场景阶段完成，等待 AutoPlay 初始化
//        isTransitioning = false;
//    }

//    // 真正执行开门动画和卸载场景的协程（等 AutoPlay 初始化完后调用）
//    private IEnumerator PlayRealExitAnimationAndUnloadScene()
//    {
//        // 播放门打开动画（回到初始位置）
//        yield return StartCoroutine(OpenDoors());

//        // 播放封面图淡出动画
//        yield return StartCoroutine(FadeOutJacket());

//        // 启用目标场景的相机
//        if (autoPlayCamera != null)
//        {
//            autoPlayCamera.enabled = true;
//            Debug.Log($"启用 {targetScene} 场景的相机");
//        }

//        // 卸载SongSelect场景前，确保AudioListener已转移
//        yield return StartCoroutine(PrepareForSongSelectUnload());

//        // 卸载SongSelect场景
//        yield return StartCoroutine(UnloadSongSelectScene());

//        isTransitioning = false;
//        Debug.Log("场景切换完成");
//    }

//    // 处理 SongSelect 场景 Panel 内容，隐藏选歌残留
//    private void HideSongSelectPanelContent()
//    {
//        // 1. 找到 Canvas 下的 Panel（假设场景里只有一个符合条件的 Canvas 和 Panel 结构，可根据实际情况优化查找逻辑）
//        Canvas songSelectCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
//        if (songSelectCanvas == null)
//        {
//            Debug.LogError("未找到 SongSelect 场景的 Canvas！");
//            return;
//        }

//        Transform panelTrans = songSelectCanvas.transform.Find("Panel");
//        if (panelTrans == null)
//        {
//            Debug.LogError("Canvas 下未找到 Panel 物体！");
//            return;
//        }

//        // 2. 找到并隐藏 Scroll View、DifficultyLabel 等不需要的子物体
//        // 可根据实际物体名称灵活调整，这里演示按名称查找
//        GameObject scrollView = panelTrans.Find("Scroll View")?.gameObject;
//        if (scrollView != null)
//        {
//            scrollView.SetActive(false);
//        }

//        GameObject difficultyLabel = panelTrans.Find("Difficulty Label")?.gameObject;
//        if (difficultyLabel != null)
//        {
//            difficultyLabel.SetActive(false);
//        }

//        // 3. 清空 Panel 的 Image 组件 Sprite（让背景透明，避免遮挡）
//        Image panelImage = panelTrans.GetComponent<Image>();
//        if (panelImage != null)
//        {
//            panelImage.sprite = null;
//            panelImage.color = new Color(1, 1, 1, 0); // 也可设置透明色加强效果
//        }
//    }

//    // 卸载SongSelect场景的系统组件（包括EventSystem和非活动AudioListener）
//    private IEnumerator UnloadSongSelectSystemComponents()
//    {
//        // 卸载EventSystem
//        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
//        foreach (EventSystem eventSystem in eventSystems)
//        {
//            if (eventSystem.gameObject.scene.name == "SongSelect")
//            {
//                //Debug.Log("卸载 SongSelect 场景的 EventSystem");
//                Destroy(eventSystem.gameObject);
//            }
//        }

//        // 卸载SongSelect场景中的非活动AudioListener
//        AudioListener[] audioListeners = FindObjectsOfType<AudioListener>();
//        foreach (AudioListener listener in audioListeners)
//        {
//            if (listener.gameObject.scene.name == "SongSelect" && listener != AudioListenerManager.Instance.activeListener)
//            {
//                Debug.Log("卸载 SongSelect 场景的非活动 AudioListener");
//                Destroy(listener.gameObject);
//            }
//        }

//        yield return null;
//    }

//    // 准备卸载SongSelect场景，确保AudioListener已正确转移
//    private IEnumerator PrepareForSongSelectUnload()
//    {
//        // 确保目标场景中已有活动的AudioListener
//        AudioListenerManager.Instance.SetupActiveListener();

//        // 给系统一些时间处理
//        yield return new WaitForEndOfFrame();
//    }

//    // 同时播放封面图和门的关闭动画
//    private IEnumerator PlaySimultaneousAnimations()
//    {
//        if (jacketImage == null || leftDoor == null || rightDoor == null)
//        {
//            Debug.LogError("动画对象缺失，无法播放同时动画");
//            yield break;
//        }

//        // 创建并启动两个动画协程
//        Coroutine moveJacketCoroutine = StartCoroutine(MoveJacketToCenter());
//        Coroutine closeDoorsCoroutine = StartCoroutine(CloseDoors());

//        // 等待两个动画都完成
//        yield return moveJacketCoroutine;
//        yield return closeDoorsCoroutine;

//        //Debug.Log("所有入场动画完成");
//    }

//    // 封面图移动到中央
//    private IEnumerator MoveJacketToCenter()
//    {
//        if (jacketImage == null)
//        {
//            Debug.LogError("未找到封面图对象");
//            yield break;
//        }

//        Vector3 startPos = jacketImage.transform.localPosition;
//        Vector3 targetPos = new Vector3(0f, startPos.y, startPos.z); // 移动到中央
//        float timer = 0f;

//        while (timer < animationDuration)
//        {
//            timer += Time.deltaTime;
//            // 使用改进的缓动函数，传入当前时间和总时长
//            jacketImage.transform.localPosition = Vector3.Lerp(startPos, targetPos,
//                EaseInOutQuad(timer, animationDuration));
//            yield return null;
//        }

//        jacketImage.transform.localPosition = targetPos;
//        //Debug.Log("封面图移动到中央完成");
//    }

//    // 加载Additive场景
//    private IEnumerator LoadAdditiveScene(string sceneName)
//    {
//        Debug.Log($"加载场景: {sceneName} (Additive模式)");

//        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

//        while (!asyncOperation.isDone)
//        {
//            yield return null;
//        }

//        //// 查找目标场景的相机并临时禁用
//        //autoPlayCamera = FindCameraInScene(sceneName);
//        //if (autoPlayCamera != null)
//        //{
//        //    autoPlayCamera.enabled = false;
//        //    Debug.Log($"禁用 {sceneName} 场景的相机");
//        //}

//        // 激活新场景
//        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
//    }

//    // 关闭门动画（记录最终关闭位置，不过我们主要用初始位置开门，这里逻辑保留关闭过程）
//    private IEnumerator CloseDoors()
//    {
//        if (leftDoor == null || rightDoor == null)
//        {
//            Debug.LogError("未找到门对象");
//            yield break;
//        }

//        // 获取门的初始位置（其实已经在 Awake 记录过，这里也可以直接用记录的）
//        Vector3 leftStartPos = leftDoorInitialPos;
//        Vector3 rightStartPos = rightDoorInitialPos;

//        // 计算关闭位置
//        //float doorWidth = 0f;
//        Vector3 leftEndPos = new Vector3(0f, leftStartPos.y, leftStartPos.z);
//        Vector3 rightEndPos = new Vector3(0f, rightStartPos.y, rightStartPos.z);

//        float timer = 0f;
//        while (timer < animationDuration)
//        {
//            timer += Time.deltaTime;
//            // 使用改进的缓动函数，传入当前时间和总时长
//            leftDoor.transform.localPosition = Vector3.Lerp(leftStartPos, leftEndPos,
//                EaseInOutQuad(timer, animationDuration));
//            rightDoor.transform.localPosition = Vector3.Lerp(rightStartPos, rightEndPos,
//                EaseInOutQuad(timer, animationDuration));
//            yield return null;
//        }

//        leftDoor.transform.localPosition = leftEndPos;
//        rightDoor.transform.localPosition = rightEndPos;
//        //Debug.Log("门关闭动画完成");
//    }

//    // 打开门动画（回到初始记录的位置）
//    private IEnumerator OpenDoors()
//    {
//        if (leftDoor == null || rightDoor == null)
//        {
//            Debug.LogError("未找到门对象");
//            yield break;
//        }

//        // 获取门的关闭位置（当前位置）和初始位置（要回到的位置）
//        Vector3 leftStartPos = leftDoor.transform.localPosition;
//        Vector3 leftEndPos = leftDoorInitialPos;

//        Vector3 rightStartPos = rightDoor.transform.localPosition;
//        Vector3 rightEndPos = rightDoorInitialPos;

//        float timer = 0f;
//        while (timer < animationDuration)
//        {
//            timer += Time.deltaTime;
//            // 使用改进的缓动函数，传入当前时间和总时长
//            leftDoor.transform.localPosition = Vector3.Lerp(leftStartPos, leftEndPos,
//                EaseInOutQuad(timer, animationDuration));
//            rightDoor.transform.localPosition = Vector3.Lerp(rightStartPos, rightEndPos,
//                EaseInOutQuad(timer, animationDuration));
//            yield return null;
//        }

//        leftDoor.transform.localPosition = leftEndPos;
//        rightDoor.transform.localPosition = rightEndPos;
//        //Debug.Log("门打开动画完成（回到初始位置）");
//    }

//    // 封面图淡出动画
//    private IEnumerator FadeOutJacket()
//    {
//        if (jacketImage == null)
//        {
//            Debug.LogError("未找到封面图对象");
//            yield break;
//        }

//        CanvasGroup canvasGroup = jacketImage.GetComponent<CanvasGroup>();
//        if (canvasGroup == null)
//        {
//            canvasGroup = jacketImage.AddComponent<CanvasGroup>();
//        }

//        float timer = 0f;
//        while (timer < animationDuration)
//        {
//            timer += Time.deltaTime;
//            // 使用改进的缓动函数，传入当前时间和总时长
//            canvasGroup.alpha = Mathf.Lerp(1f, 0f, EaseInOutQuad(timer, animationDuration));
//            yield return null;
//        }

//        canvasGroup.alpha = 0f;
//        jacketImage.SetActive(false);
//        //Debug.Log("封面图淡出完成");
//    }

//    // 卸载SongSelect场景
//    private IEnumerator UnloadSongSelectScene()
//    {
//        Scene oldScene = SceneManager.GetSceneByName("SongSelect");
//        if (oldScene.isLoaded)
//        {
//            //Debug.Log("卸载SongSelect场景...");
//            AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(oldScene);

//            while (!asyncOperation.isDone)
//            {
//                yield return null;
//            }

//            songSelectCamera = null;
//            Debug.Log("SongSelect场景卸载完成");
//        }
//    }

//    // 查找指定场景中的相机
//    private Camera FindCameraInScene(string sceneName)
//    {
//        Camera mainCamera = Camera.main;
//        if (mainCamera != null && mainCamera.gameObject.scene.name == sceneName)
//        {
//            return mainCamera;
//        }

//        Debug.LogWarning($"未找到 {sceneName} 场景中的主相机");
//        return null;
//    }

//    // 改进的缓动函数 - 二次方缓入缓出，接受当前时间和总时长
//    private float EaseInOutQuad(float currentTime, float duration)
//    {
//        float t = currentTime / duration;
//        if (t < 0.5f)
//            return 2f * t * t;
//        else
//            return -1f + (4f - 2f * t) * t;
//    }
//}