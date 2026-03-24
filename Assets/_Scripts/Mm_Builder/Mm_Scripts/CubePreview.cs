using System.Collections.Generic;
using Mm_Budier;
using Sirenix.OdinInspector;
using UnityEngine;

public class CubePreview : MonoBehaviour
{
    [Header("预览设置")]
    [LabelText("可放置材质")] public Material preTrueMaterial;
    [LabelText("不可放置材质")] public Material preFalseMaterial;

    private GameObject curPreCubeObj;  // 当前预览方块
    private CubeData curCubeData;       // 当前方块数据
    private Queue<GameObject> pool = new(); // 对象池

    /// <summary>
    /// 更新预览
    /// </summary>
    public void UpdatePreView(Vector3 worldPos, CubeData cubeData, bool canPlace)
    {
        // 如果方块数据变了，重新创建预览
        if (curCubeData != cubeData)
        {
            curCubeData = cubeData;
            CreatePreviewCube(cubeData);
        }

        // 更新位置
        if (curPreCubeObj != null)
        {
            curPreCubeObj.transform.position = worldPos;
            UpdateMaterial(canPlace);
        }
    }

    /// <summary>
    /// 隐藏预览
    /// </summary>
    public void HidePreview()
    {
        if (curPreCubeObj != null)
        {
            curPreCubeObj.SetActive(false);
        }
    }

    /// <summary>
    /// 创建预览方块
    /// </summary>
    private void CreatePreviewCube(CubeData cubeData)
    {
        // 先销毁旧的
        if (curPreCubeObj != null)
        {
            pool.Enqueue(curPreCubeObj);
        }

        // 从池里取 or 创建新的
        if (pool.Count > 0)
        {
            curPreCubeObj = pool.Dequeue();
            curPreCubeObj.SetActive(true);
        }
        else
        {
            curPreCubeObj = Instantiate(cubeData.CubePrefab, transform);
        }

        // 设置默认材质（后续 UpdateMaterial 会更新）
        var renderer = curPreCubeObj.GetComponent<MeshRenderer>();
        renderer.material = preTrueMaterial;
        
        // 禁用碰撞
        var collider = curPreCubeObj.GetComponent<Collider>();
        if (collider) collider.enabled = false;
    }

    /// <summary>
    /// 更新材质
    /// </summary>
    private void UpdateMaterial(bool canPlace)
    {
        var renderer = curPreCubeObj.GetComponent<MeshRenderer>();
        renderer.material = canPlace ? preTrueMaterial : preFalseMaterial;
    }

    /// <summary>
    /// 回收所有预览对象
    /// </summary>
    private void OnDestroy()
    {
        while (pool.Count > 0)
        {
            Destroy(pool.Dequeue());
        }
    }
}