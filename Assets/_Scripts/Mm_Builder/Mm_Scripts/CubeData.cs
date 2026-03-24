using UnityEngine;
using Sirenix.OdinInspector;

namespace Mm_Budier
{
    public enum CubeType
    {
        Air, // 破坏用

        //基础建造方块
        Dirt,       // 泥土
        Stone,      // 石头
        Wood,       // 木头
        Grass,      // 草地（可以做地表）
    }

    [CreateAssetMenu(fileName = "NewCube", menuName = "Mm_Builder/CubeData")]
    public class CubeData : SerializedScriptableObject
    {
        [LabelText("类型")] public CubeType CubeType;
        [LabelText("名称")] public string CubeName;
        [LabelText("是否有实体")] public bool IsSolid;
        [LabelText("预制体")] public GameObject CubePrefab;
        [LabelText("方块信息")] public CubeDataInfo CubePrefabInfo;

        [HideInInspector] public Vector3 cachedBoundsSize;

        /// <summary>
        /// 缓存bounds大小（Editor或运行时调用一次）
        /// </summary>
        [Button]
        public void CacheBounds()
        {
            if (CubePrefab != null)
            {
                cachedBoundsSize = CubePrefab.GetComponent<Collider>().bounds.size;
            }
        }

        /// <summary>
        /// 初始化尺寸
        /// </summary>
        [Button]
        public void InitSize()
        {
            //预制体
            CubePrefab.transform.localScale = new Vector3(CubePrefabInfo.Size.x,
                                                            CubePrefabInfo.Size.y,
                                                            CubePrefabInfo.Size.z);
        }


    }

    [System.Serializable]
    public class CubeDataInfo
    {
        public Vector3Int Size;
        public Vector3Int AnchorGrid = Vector3Int.one;
        public Vector3Int GetAnchorGrid()
        {
            // 如果尺寸是1x1x1，强制返回(1,1,1)；否则用配置值
            if (Size == Vector3Int.one)
            {
                return Vector3Int.one;
            }
            // 防止配置值超出尺寸范围
            return new Vector3Int(
                Mathf.Clamp(AnchorGrid.x, 1, Size.x),
                Mathf.Clamp(AnchorGrid.y, 1, Size.y),
                Mathf.Clamp(AnchorGrid.z, 1, Size.z)
            );
        }

    }
}
