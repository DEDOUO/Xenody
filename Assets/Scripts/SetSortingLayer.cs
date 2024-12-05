using UnityEngine;
//using System.Collections;

public class SetSortingLayer : MonoBehaviour
{
    void Start()
    {
        // 获取父物体的排序图层和图层顺序
        SpriteRenderer parentRenderer = transform.parent.GetComponent<SpriteRenderer>();
        if (parentRenderer != null)
        {
            string parentSortingLayer = parentRenderer.sortingLayerName;
            int parentOrderInLayer = parentRenderer.sortingOrder;

            // 设置子物体（Image）的排序图层和图层顺序
            SpriteRenderer childRenderer = GetComponent<SpriteRenderer>();
            if (childRenderer != null)
            {
                childRenderer.sortingLayerName = parentSortingLayer;
                childRenderer.sortingOrder = parentOrderInLayer;
            }
        }
    }
}