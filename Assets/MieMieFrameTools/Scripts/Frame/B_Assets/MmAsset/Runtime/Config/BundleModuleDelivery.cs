// 此脚本由 MmAsset 自动生成 请勿手动修改
namespace MieMieFrameWork.Asset
{
/// <summary>
/// 模块交付方式运行时查询
/// </summary>
public static class BundleModuleDelivery
{
    /// <summary>
    /// 获取模块交付方式
    /// </summary>
    public static E_BundleDeliveryMode Get(BundleModuleEnum bundleModuleEnum)
    {
        switch (bundleModuleEnum)
        {
            case BundleModuleEnum.Player:
                return E_BundleDeliveryMode.BuiltIn;
            case BundleModuleEnum.UI:
                return E_BundleDeliveryMode.BuiltIn;
            case BundleModuleEnum.Weapon:
                return E_BundleDeliveryMode.BuiltIn;
            case BundleModuleEnum.Consumable:
                return E_BundleDeliveryMode.BuiltIn;
            case BundleModuleEnum.Config:
                return E_BundleDeliveryMode.HotUpdate;
            case BundleModuleEnum.Equiment:
                return E_BundleDeliveryMode.HotUpdate;
            case BundleModuleEnum.Materials:
                return E_BundleDeliveryMode.BuiltIn;
            case BundleModuleEnum.CanPlaceAndBreakItem:
                return E_BundleDeliveryMode.BuiltIn;
            case BundleModuleEnum.Icon:
                return E_BundleDeliveryMode.BuiltIn;
            default:
                return E_BundleDeliveryMode.Hybrid;
        }
    }

    /// <summary>
    /// 是否需要提取随包资源
    /// </summary>
    public static bool NeedExtract(BundleModuleEnum bundleModuleEnum)
    {
        var eDeliveryMode = Get(bundleModuleEnum);
        return eDeliveryMode == E_BundleDeliveryMode.BuiltIn
               || eDeliveryMode == E_BundleDeliveryMode.Hybrid;
    }

    /// <summary>
    /// 是否需要执行热更
    /// </summary>
    public static bool NeedHotUpdate(BundleModuleEnum bundleModuleEnum)
    {
        var eDeliveryMode = Get(bundleModuleEnum);
        return eDeliveryMode == E_BundleDeliveryMode.HotUpdate
               || eDeliveryMode == E_BundleDeliveryMode.Hybrid;
    }
}
}
