using UnityEditor;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class PingPongAnimationStateController : MonoBehaviour
{
    private Animator animator;
    private int pingPongStateHash;
    private bool isPingPonging = false;
    //private bool isTransitioningToPingPong = false;
    private float frameTime;
    private int currentFrame = 1;
    private bool goingForward = true;
    private const string PingPongStateName = "HoldStart";
    private int totalFrames = 6;
    private float FixRate = 6/26f;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on " + gameObject.name);
            return;
        }

        // 获取动画状态的Hash值，用于后续比较
        //pingPongStateHash = Animator.StringToHash(PingPongStateName);
        pingPongStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        //Debug.Log(pingPongStateHash);
        //Debug.Log(FixRate);

        // 假设动画是每秒60帧，每帧持续时间
        frameTime = 1f / 60f;

    }

    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        ControlPingPongAnimation(stateInfo);
        //Debug.Log(stateInfo.fullPathHash);
        // 检查是否正在过渡到PingPong状态
        //if (animator.IsInTransition(0))
        //{
        //    Debug.Log(1);
        //    // 如果正在过渡，并且目标状态是PingPong，则设置标志
        //    if (animator.GetNextAnimatorStateInfo(0).fullPathHash == pingPongStateHash)
        //    {
        //        isTransitioningToPingPong = true;
        //    }
        //}
        //else
        //{
        // 如果不是过渡状态，检查当前状态是否是PingPong

        //if (stateInfo.fullPathHash == pingPongStateHash)
        //    {
        //    Debug.Log(1);
        //        // 如果是PingPong状态，并且之前不是在过渡中，则开始控制动画
        //        //if (!isTransitioningToPingPong)
        //        //{
        //            ControlPingPongAnimation(stateInfo);
        //        //}

        //        //// 重置过渡标志
        //        //isTransitioningToPingPong = false;
        //    }
        //    else
        //    {
        //        // 如果不是PingPong状态，则停止控制
        //        isPingPonging = false;
        //    }
        //}
    }

    private void ControlPingPongAnimation(AnimatorStateInfo stateInfo)
    {
        if (!isPingPonging)
        {
            // 首次进入状态时重置帧数和方向
            currentFrame = 1;
            goingForward = true;
            isPingPonging = true;
            animator.Play(PingPongStateName, 0, 0f);
        }

        // 根据时间更新帧
        float normalizedTime = stateInfo.normalizedTime % 1f;

        int frameIndex = Mathf.FloorToInt(normalizedTime / frameTime);

        // 防止帧超出范围
        frameIndex = Mathf.Clamp(frameIndex, 0, totalFrames - 1);

        // 如果是我们控制的帧范围内，则根据方向更新当前帧
        if (goingForward)
        {
            if (currentFrame < totalFrames)
            {
                currentFrame++;
            }
            else
            {
                goingForward = false;
            }
        }
        else
        {
            if (currentFrame > 1)
            {
                currentFrame--;
            }
            else
            {
                goingForward = true;
            }
        }

        // 设置动画到正确的帧（这里使用normalizedTime来控制）
        float targetTime = (currentFrame - 1) * frameTime;

        //Debug.Log(frameTime);
        //Debug.Log(currentFrame);
        //Debug.Log(targetTime);
        //Debug.Log(targetTime / FixRate);
        animator.Play(PingPongStateName, 0, targetTime/FixRate);
    }
}

