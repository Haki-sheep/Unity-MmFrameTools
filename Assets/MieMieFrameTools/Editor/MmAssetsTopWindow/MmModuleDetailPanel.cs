using Sirenix.OdinInspector;
using UnityEditor;

namespace MieMieFrameWork.Editor.MmAssets
{
    public class MmModuleDetailPanel
    {
        /// <summary>
        /// 当前模块清单数据
        /// </summary>
        private readonly MmModuleEntry entry;

        public MmModuleDetailPanel(MmModuleEntry entry)
        {
            this.entry = entry;
            Refresh();
        }

        [Title("@TitleText", bold: true)]
        [HideLabel, DisplayAsString]
        [PropertyOrder(-10)]
        public string StatusLine;

        [LabelText("分类")]
        [ReadOnly, PropertyOrder(0)]
        public string Category;

        [LabelText("版本")]
        [ReadOnly, PropertyOrder(1)]
        public string Version;

        [LabelText("标签")]
        [ReadOnly, PropertyOrder(2)]
        public string Tags;

        [LabelText("描述"), MultiLineProperty(4)]
        [ReadOnly, PropertyOrder(3)]
        public string Description;

        public string TitleText => entry?.displayName ?? "未知模块";

        /// <summary>
        /// 从清单刷新当前模块详情
        /// </summary>
        public void Refresh()
        {
            if (entry == null)
                return;

            bool isInstalled = MmModuleCatalogStore.IsInstalled(entry);
            Category = entry.category;
            Version = entry.version;
            Tags = entry.tags != null && entry.tags.Count > 0 ? string.Join(" · ", entry.tags) : "无";
            Description = entry.description;
            StatusLine = BuildStatusLine(isInstalled);
        }

        private string BuildStatusLine(bool isInstalled)
        {
            string install = isInstalled ? "● 已安装" : "○ 未安装";
            string builtIn = entry.isBuiltIn ? "内置" : "外部";
            return $"{install}   |   {builtIn}";
        }

        [Button("编辑清单 JSON", ButtonSizes.Medium)]
        [PropertyOrder(99)]
        private void OpenCatalogJson()
        {
            string assetPath = MmEditorPaths.EditorRoot + "/MmAssetsTopWindow/MmModuleCatalog.json";
            UnityEngine.Object json = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (json != null)
            {
                AssetDatabase.OpenAsset(json);
                EditorGUIUtility.PingObject(json);
            }
        }
    }
}
