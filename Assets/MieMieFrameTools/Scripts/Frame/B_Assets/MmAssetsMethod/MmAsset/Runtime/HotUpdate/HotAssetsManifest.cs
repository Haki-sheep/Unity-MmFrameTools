using System.Collections.Generic;
/// <summary>
/// 热更资源包清单
/// </summary>

namespace MieMieFrameWork.Asset
{
public class HotAssetsManifest
{
    /// <summary>
    /// 更新公告
    /// </summary>
    public string updateNotice;
    /// <summary>
    /// 下载地址
    /// </summary>
    public string downloadUrl;

    /// <summary>
    /// 最低客户端版本
    /// </summary>
    public string minClientVersion;

    /// <summary>
    /// 资源版本
    /// </summary>
    public string resourceVersion;

    /// <summary>
    /// 热更资源包列表
    /// </summary>
    public List<HotAssetsPatch> patchList = new();
}


/// <summary>
/// 热更资源包信息
/// </summary>
public class HotAssetsPatch
{

    /// <summary>
    /// 版本号
    /// </summary>
    public string version;
    /// <summary>
    /// 文件列表
    /// </summary>
    public List<HotFileInfo> fileList = new();
}


/// <summary>
/// 热更资源文件信息
/// </summary>
public class HotFileInfo
{
    /// <summary>
    /// AB 包名
    /// </summary>
    public string abName;
    /// <summary>
    /// 文件 MD5
    /// </summary>
    public string md5;
    /// <summary>
    /// 文件大小
    /// </summary>
    public float size;

    /// <summary>
    /// 文件精确字节数
    /// </summary>
    public long sizeBytes;
}
}
