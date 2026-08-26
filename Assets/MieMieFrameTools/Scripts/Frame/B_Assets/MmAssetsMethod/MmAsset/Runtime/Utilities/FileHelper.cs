using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;



namespace MieMieFrameWork.Asset
{
public class FileHelper
{
    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="path"></param>
    public static void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    /// <summary>
    /// 删除文件夹  
    /// </summary>
    /// <param name="path"></param>
    public static void DeleteFolder(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="data"></param>
    public static void WriteFile(string path, byte[] data){
        if(File.Exists(path))
            File.Delete(path);
        
        var stream = File.Create(path);
        stream.Write(data, 0, data.Length);
        stream.Flush();
        stream.Close();
        stream.Dispose();
    }

    /// <summary>
    /// 读取文件
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static byte[] ReadFile(string path){
        if(!File.Exists(path))
            return null;
        var stream = File.OpenRead(path);
        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        stream.Flush();
        stream.Close();
        stream.Dispose();
        return data;
    }
}
}
