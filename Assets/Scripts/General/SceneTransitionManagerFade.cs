using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManagerFade : MonoBehaviour
{
    public static SceneTransitionManagerFade instance;

    [Header("过渡设置")]
    public string maskObjectName = "Mask";      // 遮罩对象名称
    public float fadeInDuration = 0.3f;         // 黑屏淡入持续时间
    public float fadeOutDuration = 0.3f;        // 黑屏淡出持续时间
    public bool destroyOnLoad = false;           // 加载完成后是否销毁管理器
    public float postLoadDelay = 0.1f;           // 加载场景后延迟查找遮罩的时间

    private Image transitionMask;               // 过渡遮罩
    private bool isTransitioning = false;
    private string targetScene;                 // 目标场景名称

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //Debug.Log($"SceneTransitionManagerFade 单例初始化于: {gameObject.name}");
            DontDestroyOnLoad(gameObject);

            // 注册场景加载完成回调
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // 确保移除回调以避免内存泄漏
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 执行淡入淡出场景过渡（替换模式）
    /// </summary>
    public IEnumerator TransitionToScene(string sceneName)
    {
        if (isTransitioning) yield break;

        isTransitioning = true;
        targetScene = sceneName;

        // 确保遮罩已找到
        if (transitionMask == null)
            FindTransitionMask();

        // 淡入：当前场景变黑
        yield return FadeToBlack();

        // 加载新场景（替换当前场景）
        yield return LoadSceneSingle(sceneName);

        // 后续逻辑将在场景加载完成回调中处理
    }

    /// <summary>
    /// 淡入到黑屏效果
    /// </summary>
    private IEnumerator FadeToBlack()
    {
        if (transitionMask == null) yield break;

        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeInDuration);
            transitionMask.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        transitionMask.color = new Color(0, 0, 0, 1f);
    }

    /// <summary>
    /// 以替换模式加载场景
    /// </summary>
    private IEnumerator LoadSceneSingle(string sceneName)
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return loadOp;
    }

    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == targetScene && isTransitioning)
        {
            // 立即设置遮罩为不透明，避免闪烁
            StartCoroutine(SetMaskToOpaqueThenFadeOut());
        }
    }
    /// <summary>
    /// 先设置遮罩为不透明，再执行淡出动画
    /// </summary>
    private IEnumerator SetMaskToOpaqueThenFadeOut()
    {
        // 等待场景对象初始化完成
        //yield return new WaitForSeconds(postLoadDelay);

        // 在新场景中查找遮罩
        FindTransitionMask();

        if (transitionMask != null)
        {
            // 强制设置为不透明
            transitionMask.color = new Color(0, 0, 0, 1f);

            // 执行淡出动画
            float timer = 0f;
            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(timer / fadeOutDuration);
                transitionMask.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            transitionMask.color = new Color(0, 0, 0, 0f);
        }

        isTransitioning = false;

        // 过渡完成后销毁自身
        if (destroyOnLoad)
            Destroy(gameObject);
    }
    /// <summary>
    /// 延迟后执行从黑屏淡出效果
    /// </summary>
    private IEnumerator DelayedFadeFromBlack()
    {
        // 等待场景对象初始化完成
        yield return new WaitForSeconds(postLoadDelay);

        // 在新场景中查找遮罩
        FindTransitionMask();

        if (transitionMask != null)
        {
            // 淡出：新场景显现
            yield return FadeFromBlack();
        }

        isTransitioning = false;

        // 过渡完成后销毁自身
        if (destroyOnLoad)
            Destroy(gameObject);
    }

    /// <summary>
    /// 从黑屏淡出效果
    /// </summary>
    private IEnumerator FadeFromBlack()
    {
        float timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / fadeOutDuration);
            transitionMask.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        transitionMask.color = new Color(0, 0, 0, 0f);
    }

    /// <summary>
    /// 在场景中查找过渡遮罩
    /// </summary>
    private void FindTransitionMask()
    {
        GameObject maskObject = GameObject.Find(maskObjectName);
        if (maskObject != null)
        {
            transitionMask = maskObject.GetComponent<Image>();
            if (transitionMask == null)
            {
                Debug.LogError($"找到名为{maskObjectName}的对象，但缺少Image组件");
            }
        }
        else
        {
            Debug.LogError($"未找到过渡遮罩对象: {maskObjectName}");
        }
    }
}