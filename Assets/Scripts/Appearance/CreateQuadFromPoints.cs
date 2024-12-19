using UnityEngine;
using UnityEngine.UI;

public class CreateQuadFromPoints : MonoBehaviour
{
    // 通过函数来创建四边形游戏物体，传入四个点坐标、要赋予的精灵、游戏物体名称和父物体
    public static GameObject CreateQuad(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4,  Sprite sprite, string objectName, GameObject parentObject)
    {
        // 创建四边形游戏物体
        GameObject quadObject = new GameObject(objectName);
        if (parentObject != null)
        {
            quadObject.transform.SetParent(parentObject.transform);
        }

        // 添加MeshFilter组件，用于定义网格形状（四边形的顶点、三角形索引等信息）
        MeshFilter meshFilter = quadObject.AddComponent<MeshFilter>();
        // 添加MeshRenderer组件，用于渲染网格（显示出来，并可以设置材质、纹理等）
        MeshRenderer meshRenderer = quadObject.AddComponent<MeshRenderer>();

        // 创建一个新的Mesh实例，用于构建四边形的网格数据
        Mesh mesh = new Mesh();

        // 定义四边形的顶点数组，按照顺序将给定的四个点添加进去
        Vector3[] vertices = new Vector3[4];
        vertices[0] = point1;
        vertices[1] = point2;
        vertices[2] = point3;
        vertices[3] = point4;

        // 定义四边形的三角形索引数组，这里按照顺时针（或逆时针，需与法线计算等保持一致）顺序指定三角形的顶点索引，构成两个三角形来组成四边形
        int[] triangles = new int[6];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        // 设置网格的顶点和三角形索引数据
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // 计算并设置网格的法线（用于光照等效果计算，这里简单设置为统一朝一个方向，可根据需求优化）
        mesh.RecalculateNormals();

        // 将构建好的网格数据赋值给MeshFilter组件
        meshFilter.mesh = mesh;

        // 通过代码基于指定的.shader文件创建材质
        Shader shader = Shader.Find("MaskMaterial"); // 使用绝对路径尝试查找
        if (shader == null)
        {
            Debug.LogError("无法找到指定的MaskMaterial.shader文件，请检查路径和资源设置！");
            return null;
        }
        Material material = new Material(shader);

        //Material material = new Material(Shader.Find("Sprites/Default"));

        // 将给定的精灵赋值给材质的主纹理属性
        //Debug.Log(material);
        material.mainTexture = sprite.texture;

        // 设置新的透明度属性值（这里设置为0.8，可根据需求调整）
        material.SetFloat("_Opacity", 0.8f);

        // 将材质赋值给MeshRenderer组件，使其能够按照设置渲染出带有精灵的四边形并应用遮罩效果
        meshRenderer.material = material;


        return quadObject;
    }
}