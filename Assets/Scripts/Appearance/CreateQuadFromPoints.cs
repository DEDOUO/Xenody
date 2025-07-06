using UnityEngine;


public class CreateQuadFromPoints : MonoBehaviour
{
    // 通过函数来创建四边形游戏物体，传入四个点坐标、要赋予的精灵、游戏物体名称和父物体
    public static GameObject CreateQuad(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4, Sprite sprite, string objectName, GameObject parentObject, int RenderQueue, float Alpha, string Theshader)
    {
        // 创建四边形游戏物体
        GameObject quadObject = new GameObject(objectName);
        if (parentObject != null)
        {
            quadObject.transform.SetParent(parentObject.transform);
            // 继承父物体的图层
            int parentLayer = parentObject.layer;
            quadObject.layer = parentLayer;
        }


        // 添加 MeshFilter 组件，用于定义网格形状（四边形的顶点、三角形索引等信息）
        MeshFilter meshFilter = quadObject.AddComponent<MeshFilter>();
        // 添加 MeshRenderer 组件，用于渲染网格（显示出来，并可以设置材质、纹理等）
        MeshRenderer meshRenderer = quadObject.AddComponent<MeshRenderer>();


        // 创建一个新的 Mesh 实例，用于构建四边形的网格数据
        Mesh mesh = new Mesh();


        // 定义四边形的顶点数组，按照顺序将给定的四个点添加进去
        Vector3[] vertices = new Vector3[4];
        vertices[0] = point1;
        vertices[1] = point2;
        vertices[2] = point3;
        vertices[3] = point4;


        // 定义四边形的三角形索引数组，这里构建两组三角形，分别对应四边形的正面和背面
        int[] triangles = new int[12];
        // 正面三角形
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;
        // 背面三角形（顺序与正面相反，以保证法线方向相反）
        triangles[6] = 2;
        triangles[7] = 1;
        triangles[8] = 0;
        triangles[9] = 3;
        triangles[10] = 2;
        triangles[11] = 0;

        // 添加UV坐标设置
        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0f, 0f);
        uv[1] = new Vector2(0f, 1f);
        uv[2] = new Vector2(1f, 1f);
        uv[3] = new Vector2(1f, 0f);

        // 设置网格的顶点和三角形索引数据
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        // 计算并设置网格的法线（用于光照等效果计算，这里简单设置为统一朝一个方向，可根据需求优化）
        mesh.RecalculateNormals();


        // 将构建好的网格数据赋值给 MeshFilter 组件
        meshFilter.mesh = mesh;


        // 通过代码基于指定的.shader 文件创建材质
        Shader shader = Shader.Find(Theshader); // 使用绝对路径尝试查找
        if (shader == null)
        {
            //Debug.Log(Theshader);
            Debug.LogError("无法找到指定的shader文件，请检查路径和资源设置！");
            return null;
        }
        Material material = new Material(shader);
        // 自定义渲染队列
        material.renderQueue = RenderQueue;


        // 将给定的精灵赋值给材质的主纹理属性
        material.mainTexture = sprite.texture;


        // 将材质赋值给 MeshRenderer 组件，使其能够按照设置渲染出带有精灵的四边形并应用遮罩效果
        meshRenderer.material = material;

        //将透明度设为Alpha
        meshRenderer.material.SetFloat("_Opacity", Alpha);

        // 打印物体名和四个顶点坐标
        //Debug.Log($"创建四边形: {objectName}");
        //Debug.Log($"顶点1: ({point1.x:F3}, {point1.y:F3}, {point1.z:F3})");
        //Debug.Log($"顶点2: ({point2.x:F3}, {point2.y:F3}, {point2.z:F3})");
        //Debug.Log($"顶点3: ({point3.x:F3}, {point3.y:F3}, {point3.z:F3})");
        //Debug.Log($"顶点4: ({point4.x:F3}, {point4.y:F3}, {point4.z:F3})");

        return quadObject;
    }
}