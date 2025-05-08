using UnityEngine;

public class HoldAnimationBehavior : StateMachineBehaviour
{
    private int startFrame = 1;
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
        // 如果HoldEnd触发器被触发，允许过渡
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("HoldStart") && animator.GetBool(holdEndHash))
        {
            //Debug.Log(1);
            // 禁用脚本中的动画时间设置逻辑，允许过渡
            return;
        }

        // 将动画时间设置为第一帧对应的时间
        animator.Play(stateInfo.fullPathHash, layerIndex, (float)startFrame / frameRate / FixRate);
    }
}