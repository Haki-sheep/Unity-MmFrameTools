using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_Budier
{
    public class Chunk
    {
        public Vector3Int coord;           // Chunk坐标
        public HashSet<Vector3Int> cubes;  // Chunk内的方块位置（局部坐标）
        public GameObject chunkObj;        // Chunk的父物体
    }
    /// <summary>
    /// 世界坐标 转 Chunk 转 Chunk内局部坐标
    /// 动态加载玩家所在区块
    /// </summary>
    public class ChunkSystem : MonoBehaviour
    {
        [Header("Chunk设置")]
        [LabelText("Chunk大小")]
        public int chunkSize = 16;

        [LabelText("加载范围(实际上是数量)")]
        public int loadRadius = 3;

        [Header("可视化")]
        [LabelText("显示Chunk边框")]
        public bool showChunkBounds = true;

        [LabelText("Chunk边框颜色")]
        public Color chunkBorderColor = Color.green;

        [LabelText("当前Chunk颜色")]
        public Color currentChunkColor = Color.red;

        [LabelText("显示玩家位置")]
        public bool showPlayerPos = true;

        [LabelText("玩家位置图标大小")]
        public float playerIconSize = 1f;

        [LabelText("当前Chunk坐标")]
        public Vector3Int currentChunkCoord;

        // 存储所有已加载的Chunk：Chunk坐标 → Chunk数据
        private Dictionary<Vector3Int, Chunk> loadedChunks = new Dictionary<Vector3Int, Chunk>();

        // 方块数据存储：网格坐标 → CubeData 方便以后保存或者其他模块使用
        private Dictionary<Vector3Int, CubeData> allCubes = new Dictionary<Vector3Int, CubeData>();

        #region 坐标转换
        /// <summary>
        /// 世界坐标转Chunk坐标
        /// </summary>
        public Vector3Int WorldToChunk(Vector3 worldPos)
        {
            //向下取整 
            int cx = Mathf.FloorToInt(worldPos.x / chunkSize);
            int cy = Mathf.FloorToInt(worldPos.y / chunkSize);
            int cz = Mathf.FloorToInt(worldPos.z / chunkSize);
            return new Vector3Int(cx, cy, cz);
        }


        /// <summary>
        /// 世界坐标转在Chunk内的局部坐标
        /// </summary>
        public Vector3Int WorldToChunkLocal(Vector3 worldPos)
        {
            //取模运算 保证worldpos在chunk中的相对位置
            int lx = Mathf.FloorToInt(worldPos.x) % chunkSize;
            int ly = Mathf.FloorToInt(worldPos.y) % chunkSize;
            int lz = Mathf.FloorToInt(worldPos.z) % chunkSize;
            //可能出现负数 所以反向映射回去
            if (lx < 0) lx += chunkSize;
            if (ly < 0) ly += chunkSize;
            if (lz < 0) lz += chunkSize;
            return new Vector3Int(lx, ly, lz);
        }

        /// <summary>
        /// 网格坐标转Chunk坐标
        /// </summary>
        public Vector3Int GridToChunk(Vector3Int gridPos)
        {
            int cx = Mathf.FloorToInt((float)gridPos.x / chunkSize);
            int cy = Mathf.FloorToInt((float)gridPos.y / chunkSize);
            int cz = Mathf.FloorToInt((float)gridPos.z / chunkSize);
            return new Vector3Int(cx, cy, cz);
        }
        /// <summary>
        /// 网格坐标转在Chunk内的局部坐标
        /// </summary>
        public Vector3Int GridToChunkLocal(Vector3Int gridPos)
        {
            int lx = ((gridPos.x % chunkSize) + chunkSize) % chunkSize;
            int ly = ((gridPos.y % chunkSize) + chunkSize) % chunkSize;
            int lz = ((gridPos.z % chunkSize) + chunkSize) % chunkSize;
            return new Vector3Int(lx, ly, lz);
        }

        /// <summary>
        /// 合并方法
        /// 网格坐标转Chunk坐标 和 局部坐标 
        /// </summary>
        public (Vector3Int chunkCoord, Vector3Int localPos) GridToChunkWithLocal(Vector3Int gridPos)
        {
            int cx = Mathf.FloorToInt((float)gridPos.x / chunkSize);
            int cy = Mathf.FloorToInt((float)gridPos.y / chunkSize);
            int cz = Mathf.FloorToInt((float)gridPos.z / chunkSize);

            int lx = ((gridPos.x % chunkSize) + chunkSize) % chunkSize;
            int ly = ((gridPos.y % chunkSize) + chunkSize) % chunkSize;
            int lz = ((gridPos.z % chunkSize) + chunkSize) % chunkSize;

            return (new Vector3Int(cx, cy, cz), new Vector3Int(lx, ly, lz));
        }
        #endregion

        #region 区块加载
        /// <summary>
        /// 根据玩家位置更新区块
        /// </summary>
        /// <param name="chunkCoord"></param>
        public void UpdateChunk(Vector3 playerPos)
        {
            Vector3Int newChunkCoord = WorldToChunk(playerPos);

            //如果新坐标和当前坐标相同，则不更新
            if (newChunkCoord == currentChunkCoord) return;

            currentChunkCoord = newChunkCoord;

            //准备区块数据
            HashSet<Vector3Int> neededChunks = new HashSet<Vector3Int>();
            for (int x = -loadRadius; x <= loadRadius; x++)
            {
                for (int y = -loadRadius; y <= loadRadius; y++)
                {
                    for (int z = -loadRadius; z <= loadRadius; z++)
                    {
                        neededChunks.Add(newChunkCoord + new Vector3Int(x, y, z));
                    }
                }
            }

            // 卸载不在范围内的Chunk
            List<Vector3Int> toUnload = new List<Vector3Int>();
            foreach (var chunk in loadedChunks.Keys)
            {
                //拿到每一个区块本身的坐标
                //判断是否在需要加载的区块范围内
                if (!neededChunks.Contains(chunk))
                {
                    toUnload.Add(chunk);
                }
            }
            foreach (var chunk in toUnload)
            {
                UnloadChunk(chunk);
            }


            // 加载新Chunk
            foreach (var chunk in neededChunks)
            {
                if (!loadedChunks.ContainsKey(chunk))
                {
                    LoadChunk(chunk);
                }
            }

            Debug.Log($"[Chunk] 当前Chunk: {currentChunkCoord}, 已加载: {loadedChunks.Count}");
        }

        private void LoadChunk(Vector3Int coord)
        {
            Chunk chunk = new Chunk
            {
                coord = coord,
                cubes = new HashSet<Vector3Int>(),
                chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}_{coord.z}")
            };
            chunk.chunkObj.transform.SetParent(transform);

            loadedChunks.Add(coord, chunk);
            // TODO: 从存储加载该Chunk的方块数据 (持久化)
        }

        private void UnloadChunk(Vector3Int coord)
        {
            if (loadedChunks.TryGetValue(coord, out Chunk chunk))
            {
                // TODO: 将该Chunk的方块数据保存到存储 (持久化)
                if (chunk.chunkObj != null)
                {
                    Destroy(chunk.chunkObj);
                }
                loadedChunks.Remove(coord);
            }
        }
        #endregion

        #region 方块管理

        // AddCube
        public void AddCube(Vector3Int gridPos, CubeData cubeData)
        {
            allCubes[gridPos] = cubeData;

            //网格坐标转Chunk坐标 和 局部坐标
            var (chunkCoord, localPos) = GridToChunkWithLocal(gridPos);
            //判断该区块是否已经加载
            if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                //如果已经加载过了 将该方块添加到区块内的方块集合中
                chunk.cubes.Add(localPos);
            }
        }

        // RemoveCube
        public void RemoveCube(Vector3Int gridPos)
        {
            allCubes.Remove(gridPos);

            var (chunkCoord, localPos) = GridToChunkWithLocal(gridPos);
            if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                chunk.cubes.Remove(localPos);
            }
        }

        /// <summary>
        /// 检查某位置是否有方块
        /// </summary>
        public bool HasCube(Vector3Int gridPos)
        {
            return allCubes.ContainsKey(gridPos);
        }

        /// <summary>
        /// 获取指定Chunk内的方块数量
        /// </summary>
        public int GetChunkCubeCount(Vector3Int chunkCoord)
        {
            if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                return chunk.cubes.Count;
            }
            return 0;
        }
        #endregion

        #region 可视化
        private void OnDrawGizmos()
        {
            if (!showChunkBounds && !showPlayerPos) return;

            // 获取玩家位置（如果没有设置，则使用场景中 tagged 的 MainCamera）
            Transform player = transform;
            Vector3 playerPos = player.position;

            // 显示玩家位置
            if (showPlayerPos)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(playerPos, Vector3.one * playerIconSize);
                // 绘制玩家所在的 Chunk 中心点
                Vector3Int playerChunk = WorldToChunk(playerPos);
                Vector3 chunkCenter = new Vector3(
                    (playerChunk.x + 0.5f) * chunkSize,
                    (playerChunk.y + 0.5f) * chunkSize,
                    (playerChunk.z + 0.5f) * chunkSize
                );
                Gizmos.DrawLine(playerPos, chunkCenter);
            }

            // 显示 Chunk 边框
            if (showChunkBounds)
            {
                // 绘制当前加载的所有 Chunk
                foreach (var kvp in loadedChunks)
                {
                    Vector3Int chunkCoord = kvp.Key;
                    bool isCurrentChunk = chunkCoord == currentChunkCoord;

                    // 设置颜色
                    Gizmos.color = isCurrentChunk ? currentChunkColor : chunkBorderColor;

                    // 计算 Chunk 的世界范围（左下角）
                    Vector3 chunkMin = new Vector3(
                        chunkCoord.x * chunkSize,
                        chunkCoord.y * chunkSize,
                        chunkCoord.z * chunkSize
                    );

                    // 绘制线框
                    DrawChunkBounds(chunkMin);
                }

                // 绘制加载范围（透明红色区域）
                Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
                Vector3 loadRangeMin = new Vector3(
                    (currentChunkCoord.x - loadRadius) * chunkSize,
                    (currentChunkCoord.y - loadRadius) * chunkSize,
                    (currentChunkCoord.z - loadRadius) * chunkSize
                );
                Vector3 loadRangeSize = new Vector3(
                    (loadRadius * 2 + 1) * chunkSize,
                    (loadRadius * 2 + 1) * chunkSize,
                    (loadRadius * 2 + 1) * chunkSize
                );
                Gizmos.DrawCube(loadRangeMin + loadRangeSize / 2, loadRangeSize);
            }
        }

        private void DrawChunkBounds(Vector3 chunkMin)
        {
            Vector3 size = Vector3.one * chunkSize;

            // 底面
            Gizmos.DrawLine(chunkMin, chunkMin + new Vector3(size.x, 0, 0));
            Gizmos.DrawLine(chunkMin, chunkMin + new Vector3(0, 0, size.z));
            Gizmos.DrawLine(chunkMin + new Vector3(size.x, 0, 0), chunkMin + new Vector3(size.x, 0, size.z));
            Gizmos.DrawLine(chunkMin + new Vector3(0, 0, size.z), chunkMin + new Vector3(size.x, 0, size.z));

            // 顶面
            Vector3 topMin = chunkMin + new Vector3(0, size.y, 0);
            Gizmos.DrawLine(topMin, topMin + new Vector3(size.x, 0, 0));
            Gizmos.DrawLine(topMin, topMin + new Vector3(0, 0, size.z));
            Gizmos.DrawLine(topMin + new Vector3(size.x, 0, 0), topMin + new Vector3(size.x, 0, size.z));
            Gizmos.DrawLine(topMin + new Vector3(0, 0, size.z), topMin + new Vector3(size.x, 0, size.z));

            // 竖线
            Gizmos.DrawLine(chunkMin, topMin);
            Gizmos.DrawLine(chunkMin + new Vector3(size.x, 0, 0), topMin + new Vector3(size.x, 0, 0));
            Gizmos.DrawLine(chunkMin + new Vector3(0, 0, size.z), topMin + new Vector3(0, 0, size.z));
            Gizmos.DrawLine(chunkMin + new Vector3(size.x, 0, size.z), topMin + new Vector3(size.x, 0, size.z));
        }
        #endregion

    }
}