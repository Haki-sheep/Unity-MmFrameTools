using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// 打包配置数据类
/// </summary>

namespace MieMieFrameWork.Asset
{
[CreateAssetMenu(fileName = "BuildBundleConfigura", menuName = "MmAsset/BuildBundleConfiguration")]
public class BuildBundleConfigura : ScriptableObject
{
    public static BuildBundleConfigura instance;

    public static BuildBundleConfigura Instance
    {
        get
        {
            if (instance == null)
            {
                instance = AssetDatabase.LoadAssetAtPath<BuildBundleConfigura>(
                    MmAssetPaths.AssetBundleConfigAsset);
            }
            return instance;
        }
    }

    [SerializeField]
    public List<BundleModuleData> bundleModuleDataList = new List<BundleModuleData>();

    /// <summary>
    /// 根据模块名获取模块数据
    /// </summary>
    /// <param name="moduleName">模块名</param>
    /// <returns>模块数据</returns>
    public BundleModuleData GetBundleDataByName(string moduleName)
    {
        return bundleModuleDataList.FirstOrDefault(data => data.moduleName == moduleName);
    }


    /// <summary>
    /// 根据模块名删除模块数据
    /// </summary>
    /// <param name="moduleName">模块名</param>
    public void RemoveBundleDataByName(string moduleName)
    {
        bundleModuleDataList.RemoveAll(data => data.moduleName == moduleName);
        SaveData();
    }

    /// <summary>
    /// 添加模块数据
    /// </summary>
    /// <param name="bundleModuleData">模块数据</param>
    public void AddBundleData(BundleModuleData bundleModuleData)
    {
        bundleModuleDataList.Add(bundleModuleData);
        SaveData();
    }
    /// <summary>
    /// 保存模块数据
    /// </summary>
    /// <returns></returns>
    public bool SaveModuleData(BundleModuleData bundleModuleData)
    {
        if(bundleModuleData == null){
            Debug.LogError("模块数据不能为空");
            return false;
        }
        SaveData();
        return true;
    }


    /// <summary>
    /// 保存数据
    /// </summary>
    public void SaveData()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
#endif
    }

}
}
