//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor.Animations;
using UnityEngine;

public class HoldAnimationBehavior : StateMachineBehaviour
{
    private bool isGoingForward = true;
    private int startFrame = 1;
    private int endFrame = 6;
    // 用于存储帧率
    private float frameRate;
    private float FixRate = 6 / 26f;
    private int holdEndHash;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            frameRate = clipInfo[0].clip.frameRate;
        }
        else
        {
            // 可以在这里添加一些处理逻辑，比如打印警告信息或者设置默认帧率
            //Debug.LogWarning("No clip info found for the animator.");
            frameRate = 60f; // 设置一个默认帧率，你可以根据实际情况调整
        }
        // 初始化holdEndHash变量
        holdEndHash = Animator.StringToHash("HoldEnd");
        //Debug.Log(holdEndHash);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //Debug.Log(animator.GetCurrentAnimatorStateInfo(0));
        // 如果HoldEnd触发器被触发，允许过渡
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("HoldStart") && animator.GetBool(holdEndHash))
        {
            Debug.Log(1);
            // 禁用脚本中的动画时间设置逻辑，允许过渡
            return;
        }

        // 计算当前帧
        int currentFrame = Mathf.FloorToInt(stateInfo.normalizedTime * frameRate);

        // 如果正在正向播放且到达结束帧，则开始反向播放
        if (isGoingForward && currentFrame >= endFrame)
        {
            isGoingForward = false;
        }
        // 如果正在反向播放且到达开始帧，则开始正向播放
        else if (!isGoingForward && currentFrame <= startFrame)
        {
            isGoingForward = true;
        }

        // 根据播放方向计算目标帧
        int targetFrame;
        if (isGoingForward)
        {
            targetFrame = Mathf.Min(currentFrame, endFrame);
        }
        else
        {
            targetFrame = Mathf.Max(currentFrame, startFrame);
        }

        // 将动画时间设置为目标帧对应的时间
        animator.Play(stateInfo.fullPathHash, layerIndex, (float)targetFrame / frameRate/ FixRate);
    }
}