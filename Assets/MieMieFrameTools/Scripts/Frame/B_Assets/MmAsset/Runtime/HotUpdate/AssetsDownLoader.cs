using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 并发资源下载器
/// </summary>

namespace MieMieFrameWork.Asset
{
public sealed class AssetsDownLoader
{
    /// <summary>
    /// 待下载文件队列
    /// </summary>
    private readonly Queue<HotFileInfo> downloadQueue;

    /// <summary>
    /// 下载基础地址
    /// </summary>
    private readonly string downloadUrl;

    /// <summary>
    /// 下载保存目录
    /// </summary>
    private readonly string savePath;

    /// <summary>
    /// 单文件完成通知
    /// </summary>
    private readonly Action<HotFileInfo> fileCompleted;

    /// <summary>
    /// 最大下载线程数
    /// </summary>
    private readonly int maxDownloadThreadCount;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    private readonly int maxRetryCount;

    /// <summary>
    /// 已下载字节数
    /// </summary>
    private long downloadedByteCount;

    public long DownloadedByteCount => Interlocked.Read(ref downloadedByteCount);

    /// <summary>
    /// 创建并发资源下载器
    /// </summary>
    public AssetsDownLoader(
        IEnumerable<HotFileInfo> downloadFileList,
        string downloadUrl,
        string savePath,
        int maxDownloadThreadCount,
        int maxRetryCount,
        Action<HotFileInfo> fileCompleted)
    {
        downloadQueue = new Queue<HotFileInfo>(downloadFileList);
        this.downloadUrl = downloadUrl;
        this.savePath = savePath;
        this.maxDownloadThreadCount = Math.Max(1, maxDownloadThreadCount);
        this.maxRetryCount = Math.Max(0, maxRetryCount);
        this.fileCompleted = fileCompleted;
    }

    /// <summary>
    /// 下载全部文件
    /// </summary>
    public async UniTask DownloadAsync(CancellationToken cancellationToken)
    {
        int workerCount = Math.Min(maxDownloadThreadCount, downloadQueue.Count);
        var workerTaskList = new List<UniTask>(workerCount);
        for (int workerIndex = 0; workerIndex < workerCount; workerIndex++)
            workerTaskList.Add(DownloadWorkerAsync(cancellationToken));

        await UniTask.WhenAll(workerTaskList);
    }

    /// <summary>
    /// 下载工作协程
    /// </summary>
    private async UniTask DownloadWorkerAsync(CancellationToken cancellationToken)
    {
        while (TryDequeue(out var hotFileInfo))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var downloadThread = new DownloadThread(
                hotFileInfo,
                downloadUrl,
                savePath,
                AddDownloadedBytes,
                maxRetryCount);
            await downloadThread.DownloadAsync(cancellationToken);
            fileCompleted?.Invoke(hotFileInfo);
        }
    }

    /// <summary>
    /// 线程安全取出下载项
    /// </summary>
    private bool TryDequeue(out HotFileInfo hotFileInfo)
    {
        lock (downloadQueue)
        {
            if (downloadQueue.Count > 0)
            {
                hotFileInfo = downloadQueue.Dequeue();
                return true;
            }
        }

        hotFileInfo = null;
        return false;
    }

    /// <summary>
    /// 累计下载字节数
    /// </summary>
    private void AddDownloadedBytes(long byteCount)
    {
        Interlocked.Add(ref downloadedByteCount, byteCount);
    }
}
}
