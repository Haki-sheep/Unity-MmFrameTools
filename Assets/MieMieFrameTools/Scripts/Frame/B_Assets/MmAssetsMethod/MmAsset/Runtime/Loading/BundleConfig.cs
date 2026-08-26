using System;
using System.Collections.Generic;

    /// <summary>
    /// 资源包配置
    /// 用于生成一整个模块的打包配置文件
    /// </summary>

namespace MieMieFrameWork.Asset
{
    [Serializable]
    public class BundleConfig
    {
        public List<BundleInfo> bundleInfoList;
    }

    /// <summary>
    /// 资源包信息
    /// 用于生成一整个模块的打包配置文件的单条记录
    /// </summary>
    [Serializable]
    public class BundleInfo
    {
        // 资源路径
        public string path;
        // 资源别名
        public string alias;
        // 资源crc path转的id
        public uint crc;
        // 资源包名称(模块名)
        public string bundleName;
        // 资源名称
        public string assetName;
        // 依赖资源列表
        public List<string> dependencielist;
    }

    /// <summary>
    /// 内置资源包配置
    /// 用于生成内置资源包的配置文件
    /// </summary>
    [Serializable]
    public class BuiltInBundleConfig
    {
        public List<BuiltInBundleInfo> builtInBundleInfoList;
    }

    /// <summary>
    /// 内置资源包信息
    /// 用于生成内置资源包的配置文件的单条记录
    /// </summary>
    [Serializable]
    public class BuiltInBundleInfo{
        // 资源文件名
        public string fileName;
        // 资源md5 : 校验本地资源是否与包内资源一致
        public string md5; 
        // 资源大小(kb)
        public float size;
    }

}
