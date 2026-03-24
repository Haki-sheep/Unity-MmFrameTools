using System;
using System.Collections.Generic;
using Mm_Budier;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mm_Budier
{
    public class CubeEntry
    {
        public CubeEntry(CubeData data, GameObject spawnedObj, List<Vector3Int> occupiedGrids)
        {
            this.data = data;
            this.spawnedObj = spawnedObj;
            this.occupiedGrids = occupiedGrids;
        }
        public CubeData data;          // 方块数据
        public GameObject spawnedObj;  // 实例化的物体
        public List<Vector3Int> occupiedGrids; // 该方块占用的所有网格
    }

    [RequireComponent(typeof(VirtualGrid), typeof(CubePreview))]
    public class CubeManager : SerializedMonoBehaviour, IMmBuilderInterface
    {
        [Header("必要组件")]
        [LabelText("方块根节点")] public Transform cubeRoot;
        [LabelText("虚拟网格组件")] public VirtualGrid virtualGrid;
        [LabelText("方块预览组件")] public CubePreview cubePreview;
        [LabelText("区块加载系统")] public ChunkSystem chunkSystem;

        [LabelText("相机组件")] public Camera mainCamera;//做第一人称的话用这个
        [LabelText("玩家碰撞体组件")] public Collider playerCollider;

        [LabelText("配置文件")] public CubeBuiderSystemConfig config;

        [Header("Debug")]
        [LabelText("当前活跃方块类型")] public CubeData activeCubeData;
        public Dictionary<Vector3Int, CubeEntry> cubeMgrDict = new();//总方块字典

        //运行时变量
        [NonSerialized] public static CubeManager Instance;
        void Awake()
        {
            // 单例初始化
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            virtualGrid ??= GetComponent<VirtualGrid>();
            cubePreview ??= GetComponent<CubePreview>();
            mainCamera ??= Camera.main;

            config.InitCameraInfo(mainCamera);
        }

        void Update()
        {
            TryPlaceCube(activeCubeData);
            chunkSystem?.UpdateChunk(playerCollider.transform.position);
        }


        #region 公共函数
        public void TryPlaceCube(CubeData cubeData)
        {
            //0.拿到数据
            activeCubeData = cubeData;
            //创建传递数据
            Vector3 worldPos = default;
            Vector3Int gridPos = default;
            bool canPlace = false;

            //1.检测是否需要射线检测（UI打开/相机移动/时间间隔）
            if (config.CanRaycast())
            {
                if (!HandleRaycast(ref worldPos)) return;

                //2.网格化与校验处理
                (gridPos, canPlace) = HandleGridPos(worldPos, cubeData);

                // 更新缓存
                config.UpdateCache(worldPos, gridPos, canPlace);
            }
            else//不满足检测条件，使用缓存结果
            {
                if (!config.TryGetCachedResult(out worldPos, out gridPos, out canPlace))
                {
                    return; // 没有缓存数据，无法放置
                }
            }

            //3.更新预览
            HandlePreview(gridPos, cubeData, canPlace);

            //4.处理输入
            if (HandleInput(canPlace))
            {
                //5.放置方块
                HandlePlaceCube(gridPos, cubeData, canPlace);
            }

        }


        /// <summary>
        /// 破坏方块
        /// </summary>
        /// <param name="worldPos"></param>
        public void TryBreakCube(Vector3 worldPos)
        {
            var gridPos = virtualGrid.WorldToGrid(worldPos);
            BreakCube(gridPos);
            activeCubeData = null;
        }

        #endregion


        #region 射线检测

        /// <summary>
        /// 射线检测
        /// </summary>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        private bool HandleRaycast(ref Vector3 targetPos)
        {
            //初始化
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            RaycastHit[] raycastHits = config.raycastHits;
            var raycastMaxDistance = config.raycastMaxDistance;
            var CheckLayer = config.groundLayer | config.cubeLayer;
            var groundLayer = config.groundLayer;
            // 同时检测地面和方块层
            int hitCount = Physics.RaycastNonAlloc(
                        ray,
                        raycastHits,
                        raycastMaxDistance,
                        CheckLayer);

            if (hitCount > 0)
            {
                // 找到最近的碰撞点
                RaycastHit closestHit = raycastHits[0];
                float closestDist = closestHit.distance;

                for (int i = 1; i < hitCount; i++)
                {
                    if (raycastHits[i].distance < closestDist)
                    {
                        closestDist = raycastHits[i].distance;
                        closestHit = raycastHits[i];
                    }
                }

                // 根据 Layer 判断是地面还是方块
                if (((1 << closestHit.collider.gameObject.layer) & groundLayer) != 0)
                {
                    // Debug.Log($"命中地面: point={closestHit.point}, normal={closestHit.normal}");
                    targetPos = closestHit.point + closestHit.normal * virtualGrid.gridUnitSize / 2;
                }
                else
                {
                    // Debug.Log($"命中方块: point={closestHit.point}, normal={closestHit.normal}");
                    targetPos = closestHit.point + closestHit.normal * virtualGrid.gridUnitSize / 2;
                }

                return true;
            }

            return false;
        }

        #endregion
        #region 网格化与校验处理
        private (Vector3Int, bool) HandleGridPos(Vector3 worldPos, CubeData cubeData)
        {
            var gridPos = virtualGrid.WorldToGrid(worldPos);
            // 校验只做一次 
            var occupiedGrids = CompCubeOccupiedGrid(gridPos, cubeData);//计算持有占用网格位置
            bool canPlace = ValidCanPlaceCube(gridPos, cubeData)//验证是否能放置
                         && ValidAllOccupiedGrids(occupiedGrids);//验证所有占用网格是否未被占用

            return (gridPos, canPlace);
        }
        #endregion
        #region 更新预览
        /// <summary>
        /// 更新预览
        /// </summary>
        /// <param name="gridPos"></param>
        /// <param name="cubeData"></param>
        /// <param name="canPlace"></param>
        private void HandlePreview(Vector3Int gridPos, CubeData cubeData, bool canPlace)
        {
            //网格转世界坐标 不然没法同步到世界预览
            var worldPos = virtualGrid.GridToWorldCenter(gridPos);
            //更新View预览视图
            cubePreview.UpdatePreView(worldPos, cubeData, canPlace);
        }
        #endregion

        #region 处理输入
        private bool HandleInput(bool canPlace)
        {
            if (!canPlace) return false;
            //处理输入
            //旋转
            //放置
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
            //销毁
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region 放置/销毁

        /// <summary>
        /// 放置方块
        /// </summary>
        /// <param name="gridPos"></param>
        /// <param name="cubeData"></param>
        /// <param name="canPlace"></param>
        private void HandlePlaceCube(Vector3Int gridPos, CubeData cubeData, bool canPlace)
        {
            if (!canPlace) return;

            // 1.计算方块会占用到的所有网格
            var occupiedGrids = CompCubeOccupiedGrid(gridPos, cubeData);

            // 2.放下
            var spawnedObj = TryCreatCube(gridPos, cubeData);

            // 3.保存数据
            HandleData(gridPos, cubeData, spawnedObj, occupiedGrids);
        }


        /// <summary>
        /// 校验是否可以放置方块
        /// </summary>
        /// <param name="gridPos"></param>
        /// <param name="cubeData"></param>
        /// <returns></returns>
        private bool ValidCanPlaceCube(Vector3Int gridPos, CubeData cubeData)
        {
            //验证是否是空气方块
            if (cubeData.CubeType == CubeType.Air)
            {
                print("想放置空气方块" + cubeData.CubeType);
                return false;
            }
            //验证是否已有方块
            if (cubeMgrDict.ContainsKey(gridPos))
            {
                print("该位置已有方块" + gridPos);
                return false;
            }
            //验证是否超出边界
            if (!virtualGrid.ValidBoundary(gridPos))
            {
                print("该位置超出边界" + gridPos);
                return false;
            }
            if (!ValidPlayerCollision(gridPos, cubeData))
            {
                return false;
            }

            if (!CustomVaild())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 验证和玩家碰撞体合在一起了
        /// </summary>
        /// <param name="gridPos">网格位置</param>
        /// <param name="cubeData">方块数据</param>
        /// <returns></returns>
        private bool ValidPlayerCollision(Vector3Int gridPos, CubeData cubeData)
        {
            // 使用 CubeData 里缓存的 bounds 大小
            var cubeSize = cubeData.cachedBoundsSize;
            var cubeCenter = virtualGrid.GridToWorldCenter(gridPos);
            var cubeBounds = new Bounds(cubeCenter, cubeSize);
            if (playerCollider.bounds.Intersects(cubeBounds))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 计算方块会占用到的所有网格
        /// </summary>
        /// <param name="targetGridPos"></param>
        /// <param name="cubeData"></param>
        /// <returns></returns>
        private List<Vector3Int> CompCubeOccupiedGrid(Vector3Int targetGridPos, CubeData cubeData)
        {
            if (cubeData.CubePrefabInfo is null) return null;

            //拿到方块的主格子
            var anchorGrid = cubeData.CubePrefabInfo.GetAnchorGrid();
            //拿到方块的尺寸
            var size = cubeData.CubePrefabInfo.Size;

            // 计算方块左下角的起始网格（主格子 - 锚点偏移）
            Vector3Int startGrid = targetGridPos - (anchorGrid - Vector3Int.one);
            // 遍历尺寸，计算所有占用的网格
            List<Vector3Int> occupiedGrid = new List<Vector3Int>();
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        occupiedGrid.Add(new Vector3Int(
                            startGrid.x + x,
                            startGrid.y + y,
                            startGrid.z + z
                        ));
                    }
                }
            }

            return occupiedGrid;
        }

        /// <summary>
        /// 校验所有占用网格是否未被占用
        /// </summary>
        /// <param name="occupiedGrids"></param>
        /// <returns></returns>
        private bool ValidAllOccupiedGrids(List<Vector3Int> occupiedGrids)
        {
            foreach (var pos in occupiedGrids)
            {
                //优先从Chunk检查
                if (chunkSystem is not null)
                {
                    if (chunkSystem.HasCube(pos)) return false;
                }
                else
                {
                    if (cubeMgrDict.ContainsKey(pos))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 尝试放置方块
        /// </summary>
        /// <returns></returns>
        private GameObject TryCreatCube(Vector3Int targetGridPos, CubeData cubeData)
        {
            var tempObj = cubeData.CubePrefab;

            if (tempObj)
            {
                // 把网格坐标转换成世界坐标
                var worldPos = virtualGrid.GridToWorldCenter(targetGridPos);
                //创建方块
                var spawnedObj = GameObject.Instantiate(
                    tempObj,
                    worldPos,//放置位置
                    Quaternion.identity,
                    cubeRoot
                );
                spawnedObj.layer = config.cubeLayer;
                return spawnedObj;
            }


            print("得不到预制体" + cubeData.CubePrefab);
            return null;
        }

        /// <summary>
        /// 保存数据(到内存)
        /// </summary>
        /// <param name="targetGridPos">目标网格位置</param>
        /// <param name="cubeData">方块数据</param>
        /// <param name="spawnedObj">实例化的物体</param>
        /// <param name="occupiedGrids">占用网格</param>
        private void HandleData(Vector3Int targetGridPos, 
                                CubeData cubeData,
                                GameObject spawnedObj, 
                                List<Vector3Int> occupiedGrids)
        {
            CubeEntry entry = new CubeEntry(cubeData, spawnedObj, occupiedGrids);
            // 遍历所有网格存入字典
            foreach (var pos in occupiedGrids)
            {
                // 保存到内存
                cubeMgrDict.Add(pos, entry);
                // 同步到 ChunkSystem
                chunkSystem?.AddCube(pos, cubeData);
            }
        }

        /// <summary>
        /// 破坏方块
        /// </summary>
        /// <param name="gridPos"></param>
        private void BreakCube(Vector3Int gridPos)
        {
            if (!cubeMgrDict.TryGetValue(gridPos, out var entry)) return;

            // 销毁方块
            if (entry.spawnedObj != null) Destroy(entry.spawnedObj);

            // 删除所有占用网格
            foreach (var pos in entry.occupiedGrids)
            {
                if (cubeMgrDict.ContainsKey(pos)) cubeMgrDict.Remove(pos);

                // 同步到 ChunkSystem
                chunkSystem?.RemoveCube(pos);
            }
        }

        public bool CustomVaild()
        {
            return true;
        }
        #endregion


    }
}