using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mm_Budier
{
    [CreateAssetMenu(fileName = "NetConfig", menuName = "Mm_Builder/BdSystemConfig")]
    public class CubeBuiderSystemConfig : SerializedScriptableObject
    {
        [Header("射线检测设置")] //此处可替换为其他检测方式 只需要提供相同参数即可
        [LabelText("射线检测方块层级"), SerializeField] public LayerMask cubeLayer;
        [LabelText("射线检测最大距离"), SerializeField] public float raycastMaxDistance;
        [LabelText("地面层级"), SerializeField] public LayerMask groundLayer;
        [LabelText("最大命中缓存"), SerializeField] public RaycastHit[] raycastHits = new RaycastHit[1];


        [Header("性能优化")]
        [LabelText("开启射线检测优化"), SerializeField] public bool isOpenRaycastOptimize;
        [LabelText("射线检测间隔（秒）"), SerializeField, ShowIf("isOpenRaycastOptimize")] public float raycastInterval = 0.1f;
        [LabelText("打开其他UI项目时跳过检测"), SerializeField] public bool onUICloseRaycast;

        // 缓存射线检测结果
        private Vector3 cachedWorldPos;
        private Vector3Int cachedGridPos;
        private bool cachedCanPlace;
        private bool hasCachedResult;

        // 优化相关
        private float lastRaycastTime;
        private Vector3 lastCamPos;
        private Quaternion lastCamRot;
        private Camera mainCamera;

        public void InitCameraInfo(Camera camera)
        {
            mainCamera = camera;
            lastCamPos = mainCamera.transform.position;
            lastCamRot = mainCamera.transform.rotation;
            lastRaycastTime = -raycastInterval; // 初始化时允许立即检测
        }

        /// <summary>
        /// 判断是否需要进行射线检测
        /// 优先级：UI打开 > 相机移动 > 时间间隔
        /// </summary>
        public bool CanRaycast()
        {
            // 1. UI 打开时不检测
            if (onUICloseRaycast) return false;

            // 没开启优化时，始终允许检测
            if (!isOpenRaycastOptimize) return true;

            // 2. 相机移动了，立即检测
            if (mainCamera != null)
            {
                if (Vector3.SqrMagnitude(lastCamPos - mainCamera.transform.position) > 0.0001f
                    || Quaternion.Angle(lastCamRot, mainCamera.transform.rotation) > 0.1f)
                {
                    lastCamPos = mainCamera.transform.position;
                    lastCamRot = mainCamera.transform.rotation;
                    lastRaycastTime = Time.time;
                    return true;
                }
            }

            // 3. 相机没动，按时间间隔检测
            if (Time.time - lastRaycastTime >= raycastInterval)
            {
                lastRaycastTime = Time.time;
                return true;
            }

            // 4. 都不满足，用缓存
            return false;
        }

        /// <summary>
        /// 更新缓存的检测结果
        /// </summary>
        public void UpdateCache(Vector3 worldPos, Vector3Int gridPos, bool canPlace)
        {
            cachedWorldPos = worldPos;
            cachedGridPos = gridPos;
            cachedCanPlace = canPlace;
            hasCachedResult = true;
        }

        /// <summary>
        /// 获取缓存的检测结果
        /// </summary>
        public bool TryGetCachedResult(out Vector3 worldPos, out Vector3Int gridPos, out bool canPlace)
        {
            if (hasCachedResult)
            {
                worldPos = cachedWorldPos;
                gridPos = cachedGridPos;
                canPlace = cachedCanPlace;
                return true;
            }
            worldPos = default;
            gridPos = default;
            canPlace = false;
            return false;
        }
    }
}
