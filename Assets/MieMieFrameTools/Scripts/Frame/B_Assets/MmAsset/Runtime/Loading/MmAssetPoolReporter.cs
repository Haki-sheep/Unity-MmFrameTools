namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// MmAsset 资源实例池运行时快照
    /// </summary>
    public struct MmAssetPoolReporter
    {
        /// <summary>
        /// 资源路径 CRC
        /// </summary>
        public uint PoolKey;

        /// <summary>
        /// 资源路径
        /// </summary>
        public string ResourcePath;

        /// <summary>
        /// 闲置实例数量
        /// </summary>
        public int PooledCount;

        /// <summary>
        /// 活跃实例数量
        /// </summary>
        public int ActiveCount;

        /// <summary>
        /// 累计创建数量
        /// </summary>
        public int TotalCreated => PooledCount + ActiveCount;
    }
}
