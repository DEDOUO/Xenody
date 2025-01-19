using UnityEngine;
using System.Collections.Generic;

public class GlobalRenderOrderManager : MonoBehaviour
{

    // 定义委托类型，表示无参数无返回值的方法
    public delegate void ObjectsCreatedHandler();
    // 定义事件，基于上面的委托类型
    public event ObjectsCreatedHandler OnObjectsCreated;

    [System.Serializable]
    public class RenderObjectGroup
    {
        public GameObject parentGameObject;
        public List<GameObject> childObjects = new List<GameObject>();
        public int groupRenderOrder;
    }

    public List<RenderObjectGroup> renderObjectGroups = new List<RenderObjectGroup>();

    void Start()
    {
        //// 收集各个父物体下的子物体
        //foreach (RenderObjectGroup group in renderObjectGroups)
        //{
        //    if (group.parentGameObject != null)
        //    {
        //        foreach (Transform child in group.parentGameObject.transform)
        //        {
        //            group.childObjects.Add(child.gameObject);
        //        }
        //    }
        //}

        //// 可以在这里进行一些初始化排序等操作，确保顺序正确（虽然按照添加顺序一般没问题，但以防万一）
        //renderObjectGroups.Sort((a, b) => a.groupRenderOrder.CompareTo(b.groupRenderOrder));
    }

    void LateUpdate()
    {
        int overallOrder = 0;
        foreach (RenderObjectGroup group in renderObjectGroups)
        {
            foreach (GameObject child in group.childObjects)
            {
                MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
                SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();

                if (meshRenderer != null)
                {
                    Material[] materials = meshRenderer.materials;
                    foreach (Material material in materials)
                    {
                        material.renderQueue = overallOrder;
                    }
                }
                else if (spriteRenderer != null)
                {
                    spriteRenderer.sortingLayerName = "CustomLayer";
                    spriteRenderer.sortingOrder = overallOrder;
                }
                overallOrder++;
            }
        }
    }

    // 新增一个公共方法，用于在合适的时机触发事件，这里假设在某个条件满足时触发，你可以根据实际情况调整这个方法的调用时机
    public void CheckAndTriggerObjectsCreatedEvent()
    {
        // 这里可以添加一些条件判断，比如判断相关子物体是否已经全部创建完成等，暂时简单直接触发事件
        if (OnObjectsCreated != null)
        {
            OnObjectsCreated();
        }
    }
}