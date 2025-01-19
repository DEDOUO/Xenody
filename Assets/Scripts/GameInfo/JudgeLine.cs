using System;
using UnityEngine;
using Note;
using Params;

public class JudgeLine
{
    public static void SetJudgeLineAlpha(GameObject judgeLine, float alpha)
    {
        SpriteRenderer spriteRenderer = judgeLine.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

}


