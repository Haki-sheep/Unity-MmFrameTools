using System;
using System.IO;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 支持断点续传与强校验的单文件下载器
/// </summary>

namespace MieMieFrameWork.Asset
{
public sealed class DownloadThread
{
    /// <summary>
    /// 热更文件信息
    /// </summary>
    private readonly HotFileInfo hotFileInfo;

    /// <summary>
    /// 文件下载地址
    /// </summary>
    private readonly string downloadUrl;

    /// <summary>
    /// 正式文件路径
    /// </summary>
    private readonly string savePath;

    /// <summary>
    /// 下载增量通知
    /// </summary>
    private readonly Action<long> downloadedBytes;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    private readonly int maxRetryCount;

    /// <summary>
    /// 创建单文件下载器
    /// </summary>
    public DownloadThread(
        HotFileInfo hotFileInfo,
        string downloadUrl,
        string savePath,
        Action<long> downloadedBytes,
        int maxRetryCount)
    {
        this.hotFileInfo = hotFileInfo;
        this.downloadUrl = downloadUrl.TrimEnd('/') + "/" + hotFileInfo.abName;
        this.savePath = Path.Combine(savePath, hotFileInfo.abName);
        this.downloadedBytes = downloadedBytes;
        this.maxRetryCount = Math.Max(0, maxRetryCount);
    }

    /// <summary>
    /// 下载并校验文件
    /// </summary>
    public async UniTask DownloadAsync(CancellationToken cancellationToken)
    {
        await UniTask.RunOnThreadPool(
            () => DownloadBlocking(cancellationToken),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 在线程池执行阻塞下载
    /// </summary>
    private void DownloadBlocking(CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(savePath);
        Directory.CreateDirectory(directoryPath);
        var partialPath = savePath + ".download";
        Exception lastException = null;

        for (int retryIndex = 0; retryIndex <= maxRetryCount; retryIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                long expectedSize = hotFileInfo.sizeBytes > 0L
                    ? hotFileInfo.sizeBytes
                    : (long)(hotFileInfo.size * 1024f);
                if (!File.Exists(partialPath)
                    || expectedSize <= 0L
                    || new FileInfo(partialPath).Length != expectedSize)
                    DownloadAttempt(partialPath, cancellationToken);
                if (expectedSize > 0L && new FileInfo(partialPath).Length != expectedSize)
                    throw new InvalidDataException("下载文件大小校验失败 " + hotFileInfo.abName);
                if (!string.Equals(MD5.GetMd5FromFile(partialPath), hotFileInfo.md5, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("下载文件 MD5 校验失败 " + hotFileInfo.abName);

                if (File.Exists(savePath))
                    File.Delete(savePath);
                File.Move(partialPath, savePath);
                return;
            }
            catch (Exception exception)
            {
                lastException = exception;
                if (exception is InvalidDataException && File.Exists(partialPath))
                    File.Delete(partialPath);
            }
        }

        throw new IOException("下载失败 " + hotFileInfo.abName, lastException);
    }

    /// <summary>
    /// 执行单次下载
    /// </summary>
    private void DownloadAttempt(string partialPath, CancellationToken cancellationToken)
    {
        long existingLength = File.Exists(partialPath) ? new FileInfo(partialPath).Length : 0L;
        var request = WebRequest.Create(downloadUrl) as HttpWebRequest;
        request.Method = "GET";
        request.Timeout = 30000;
        request.ReadWriteTimeout = 30000;
        if (existingLength > 0L)
            request.AddRange(existingLength);

        using var response = request.GetResponse() as HttpWebResponse;
        bool canResume = existingLength == 0L || response.StatusCode == HttpStatusCode.PartialContent;
        if (!canResume)
        {
            response.Close();
            File.Delete(partialPath);
            DownloadAttempt(partialPath, cancellationToken);
            return;
        }

        using var responseStream = response.GetResponseStream();
        using var fileStream = new FileStream(
            partialPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.None);
        var bufferList = new byte[64 * 1024];
        int readLength;
        while ((readLength = responseStream.Read(bufferList, 0, bufferList.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            fileStream.Write(bufferList, 0, readLength);
            downloadedBytes?.Invoke(readLength);
        }
        fileStream.Flush();
    }
}
}
