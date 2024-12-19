using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldAnimationBehavior : StateMachineBehaviour
{
    private bool isLooping = false;
    private int startFrame = 1;
    private int endFrame = 6;

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 获取动画的帧率（假设动画剪辑已经设置好帧率）
        float frameRate = animator.GetCurrentAnimatorClipInfo(0)[0].clip.frameRate;
        // 计算当前帧
        int currentFrame = Mathf.FloorToInt(stateInfo.normalizedTime * frameRate);

        // 如果在开始帧和结束帧之间，并且还没有开始循环，则设置为循环状态
        if (currentFrame >= startFrame && currentFrame <= endFrame && !isLooping)
        {
            isLooping = true;
        }

        // 如果已经在循环状态且当前帧超过结束帧，则将动画时间设置回开始帧时间
        if (isLooping && currentFrame > endFrame)
        {
            animator.Play(stateInfo.fullPathHash, layerIndex, (float)startFrame / frameRate);
        }
    }
}