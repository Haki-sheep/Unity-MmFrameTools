
namespace MieMieFrameWork.Asset
{
﻿/*****************************************************************************************
*
* Title: ZM 资源管理框架  
*
*Description: 集多版本、多模块热更、回退、加密、解密、压缩、内嵌、解压、下载、Editor加载、Bundle加载、 冗余剔除、多版本颗粒化、 等一体式框架
*
* Author: 铸梦xu
*
* Date: 2019.7.1
*
* Modify: 
*
* *****************************************************************************************/
using System.IO;
using System.Text;

    public class MD5
    {
        /// <summary>
        /// 传一个文件的路径，返回该文件的MD5字符串
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetMd5FromFile(string path)
        {
            using (System.Security.Cryptography.MD5 md5File = System.Security.Cryptography.MD5.Create())
            {
                using (FileStream fileRead = File.OpenRead(path))
                {
                    byte[] md5Buffer = md5File.ComputeHash(fileRead);
                    md5File.Clear();
                    StringBuilder sbMd5 = new StringBuilder();
                    for (int i = 0; i < md5Buffer.Length; i++)
                    {
                        sbMd5.Append(md5Buffer[i].ToString("X2"));
                    }
                    return sbMd5.ToString();
                }
            }

        }




        /// <summary>
        /// 传一个字符串，方法改字符串的MD5字符串
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string GetMd5FromString(string msg)
        {
            //1.创建一个用来计算MD5值的类的对象
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                //把字符串转换成byte[]
                byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);
                //2.计算给定字符串的MD5值，如何把一个长度为16的byte[]数组转换为一个长度为32的字符串：就是把每个byte转成16进制同时保留2位即可
                byte[] md5Buffer = md5.ComputeHash(msgBuffer);
                md5.Clear();
                StringBuilder sbMd5 = new StringBuilder();
                for (int i = 0; i < md5Buffer.Length; i++)
                {
                    //字母小写：x ;字母大写：X
                    sbMd5.Append(md5Buffer[i].ToString("X2"));
                }

                return sbMd5.ToString();
            }
        }
    }
}
