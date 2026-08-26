# MmAsset 使用手册

> 适用范围：`Assets/MieMieFrameTools/Scripts/Frame/B_Assets/MmAssetsMethod/MmAsset`  
> 编辑器：`Assets/MieMieFrameTools/Editor/MmAssetForEditor`  
> 推荐入口：`Tools/MieMieFrameWork/MmAsset/资源管线`
>
> 推荐测试顺序：Editor 直读 → 随包 AB → 热更 AB → 断点续传 → 对象池与卸载

---

## 1. 先认识运行流程

业务只需要记住一个启动入口：

```csharp
await AssetFrame.Instance.BootModule(BundleModuleEnum.ATest, progress, cancellationToken);
```

`BootModule` 内部依次执行：

1. 准备随包资源
2. 检查热更版本
3. 下载和校验热更资源
4. 加载资源地址表
5. 预热 ShaderVariantCollection
6. 返回业务层

资源读取优先级固定为：

```text
persistent/MmAsset/{module}/hot
    ↓ 没有目标文件
persistent/MmAsset/{module}/decompress
    ↓ 没有目标文件
StreamingAssets/AssetBundle/{module}
```

因此同名 AB 下载到热更目录后会自动覆盖随包版本，无需业务判断。

---

## 2. 必要设置

配置资产：

```text
Assets/MieMieFrameTools/Scripts/Frame/B_Assets/MmAssetsMethod/MmAsset/Resources/BundleSettings.asset
```

### 2.1 常用字段

| 字段 | 测试建议 | 作用 |
|---|---|---|
| 下载地址 | `http://127.0.0.1:8000` | CDN 或本地 HTTP 服务根地址 |
| 是否热更 | `NotHot` 或 `Hot` | 是否访问服务器检查更新 |
| 资源加载类型 | `Editor` 或 `AssetBundle` | 编辑器直接读取或从 AB 读取 |
| 最大热更线程数 | `3` | 同批文件最大并发数 |
| 下载失败重试次数 | `3` | 单文件失败后的重试次数 |
| 资源最低客户端版本 | 通常由热更窗口写入 | 低版本客户端触发强更异常 |
| 加密范围 | 推荐 `ConfigOnly` | 仅加密地址表或加密全部 AB |
| 目标平台 | 当前测试平台 | 必须与运行平台一致 |
| 压缩格式 | `ChunkBasedCompression` | LZ4 适合本地按需加载 |

### 2.2 两种加载模式

#### Editor

- 直接通过 `AssetDatabase` 读工程资源
- 不要求先构建 AB
- 适合验证加载 API
- 没有地址表时只能使用完整 `Assets/...` 路径

#### AssetBundle

- 从随包目录或热更目录读取 AB
- 必须先构建地址表和 AB
- 可使用自定义别名
- 用于验证真实发布流程

---

## 3. 模块配置

打开：

```text
Tools/MieMieFrameWork/MmAsset/资源管线
```

进入：

```text
构建/整包与内嵌
```

双击模块卡片可编辑模块。

### 3.1 模块基础规则

- 模块名必须是英文开头的 CSharp 标识符
- 模块保存后会自动更新 `BundleModuleEnum`
- AB 名称只允许英文 数字 下划线 短横线
- 模块目录和最终 AB 名会统一转为小写

示例：

```text
模块名：ATest
模块目录：atest
AB 名：atest_window.unity
```

### 3.2 分包方式

#### 预制体分包

指定一个或多个目录。

目录下每个 Prefab 单独生成一个 AB，材质 网格 贴图等依赖会自动收集。

#### 子文件夹分包

指定一个根目录。

根目录下每个子文件夹生成一个 AB。

#### 场景分包

指定场景目录。

每个 Scene 单独生成 AB，并自动收集场景依赖。

#### 模块整包

手动填写 AB 名与目录。

适合 UI 音频 公共配置等需要整体加载的内容。

### 3.3 交付方式

| 类型 | 随包复制 | 热更输出 | 适用场景 |
|---|---:|---:|---|
| `BuiltIn` | 是 | 否 | 启动必需资源 |
| `HotUpdate` | 否 | 是 | 可下载内容 |
| `Hybrid` | 是 | 是 | 首包带基础版 后续可覆盖 |

第一次测试建议使用 `Hybrid`。

### 3.4 共享依赖

开启：

```text
自动抽取共享依赖
```

默认阈值为 `2`。

同一个材质 贴图 网格等被两个及以上业务包引用时，会抽到：

```text
atest_common.unity
```

构建报告位于：

```text
BuildOutput/Reports/atest_build_report.json
```

### 3.5 资源别名

在模块的“地址别名”页配置：

```text
资源别名：UI/LoginPanel
目标资源：LoginPanel.prefab
```

业务加载时使用：

```csharp
var instance = await AssetFrame.Instance.Resources.InstantiateAsync(
    "UI/LoginPanel",
    cancellationToken: cancellationToken);
```

未配置自定义别名时，系统会自动生成去掉 `Assets/` 和扩展名的路径别名。

例如：

```text
Assets/Game/Prefabs/Role.prefab
```

自动别名：

```text
Game/Prefabs/Role
```

---

## 4. 第一项测试：Editor 直接加载

### 4.1 设置

```text
是否热更：NotHot
资源加载类型：Editor
```

### 4.2 测试脚本

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class EditorLoadTest : MonoBehaviour
{
    /// <summary>
    /// 测试 Editor 资源加载
    /// </summary>
    private async UniTask Start()
    {
        await AssetFrame.Instance.BootModule(BundleModuleEnum.ATest);

        var instance = await AssetFrame.Instance.Resources.InstantiateAsync(
            "Assets/BundleDemo/Prefabs/Objects/Cube.prefab",
            cancellationToken: this.GetCancellationTokenOnDestroy());

        instance.transform.position = Vector3.zero;
    }
}
```

请把示例路径改成工程中真实存在的 Prefab 路径。

### 4.3 预期结果

- 不需要构建 AB
- 进入 Play Mode 后成功创建 Prefab
- Hierarchy 出现 `RecyclObjRoot`
- Console 没有地址表或 AB 缺失错误

---

## 5. 第二项测试：随包 AssetBundle

### 5.1 设置

```text
是否热更：NotHot
资源加载类型：AssetBundle
目标平台：当前运行平台
压缩格式：ChunkBasedCompression
```

模块交付方式使用：

```text
BuiltIn 或 Hybrid
```

### 5.2 构建步骤

打开：

```text
Tools/MieMieFrameWork/MmAsset/资源管线
```

依次执行：

1. 勾选 `ATest`
2. 点击“打包”
3. 等待构建完成
4. 点击“内嵌”

### 5.3 构建结果

原始构建产物：

```text
BuildOutput/Bundles/atest/{Platform}/
```

内嵌产物：

```text
Assets/StreamingAssets/AssetBundle/atest/
```

随包清单：

```text
Assets/MieMieFrameTools/Scripts/Frame/B_Assets/MmAssetsMethod/MmAsset/Resources/atest_builtin.json
```

地址表 AB：

```text
atest_abconfig.unity
```

### 5.4 启动并加载

```csharp
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class BuiltInLoadTest : MonoBehaviour
{
    /// <summary>
    /// 测试随包资源加载
    /// </summary>
    private async UniTask Start()
    {
        var progress = new Progress<AssetBootProgress>(OnBootProgress);

        await AssetFrame.Instance.BootModule(
            BundleModuleEnum.ATest,
            progress,
            this.GetCancellationTokenOnDestroy());

        var instance = await AssetFrame.Instance.Resources.InstantiateAsync(
            "Game/Prefabs/Role",
            cancellationToken: this.GetCancellationTokenOnDestroy());

        instance.transform.position = Vector3.zero;
    }

    /// <summary>
    /// 输出启动进度
    /// </summary>
    private void OnBootProgress(AssetBootProgress progress)
    {
        Debug.Log(
            progress.Stage
            + " "
            + progress.Progress
            + " "
            + progress.Message);
    }
}
```

把 `Game/Prefabs/Role` 改成已生成的自动别名或自定义别名。

### 5.5 预期结果

启动阶段依次出现：

```text
Decompress
LoadConfig
Completed
```

Windows 编辑器会直接读取 StreamingAssets。

Android 和 iOS 会先将需要的 AB 提取到：

```text
Application.persistentDataPath/MmAsset/atest/decompress/
```

---

## 6. 第三项测试：本地热更服务器

### 6.1 准备初始版本

先完成“随包 AssetBundle”测试，保证安装包内已有旧版本资源。

然后修改一个 Prefab 材质 贴图或其他资源。

### 6.2 设置

```text
是否热更：Hot
资源加载类型：AssetBundle
下载地址：http://127.0.0.1:8000
```

如果在手机上测试，不能使用手机自己的 `127.0.0.1`。

应填写电脑局域网地址，例如：

```text
http://192.168.0.106:8000
```

### 6.3 构建热更

进入：

```text
Tools/MieMieFrameWork/MmAsset/资源管线
构建/热更资源
```

填写：

```text
母包版本：1.0.0
本地热更：1.0.1
热更公告：测试资源更新
```

点击：

```text
打包热更
```

生成目录：

```text
BuildOutput/Hot/atest/hot_manifest.json
BuildOutput/Hot/atest/1.0.1/{Platform}/*.unity
```

### 6.4 本地服务器目录

准备一个服务器根目录：

```text
ServerRoot/
└── HotAssets/
    └── atest/
        ├── hot_manifest.json
        └── 1.0.1/
            └── StandaloneWindows64/
                ├── atest_abconfig.unity
                ├── atest_common.unity
                └── 其他 AB
```

将：

```text
BuildOutput/Hot/atest/
```

复制到：

```text
ServerRoot/HotAssets/atest/
```

### 6.5 启动 HTTP 服务

在 `ServerRoot` 目录执行：

```powershell
python -m http.server 8000
```

浏览器访问：

```text
http://127.0.0.1:8000/HotAssets/atest/hot_manifest.json
```

能看到 JSON 才表示目录正确。

### 6.6 运行测试

继续使用：

```csharp
await AssetFrame.Instance.BootModule(BundleModuleEnum.ATest, progress, cancellationToken);
```

### 6.7 预期结果

启动阶段依次出现：

```text
Decompress
CheckVersion
Download
LoadConfig
Completed
```

下载文件落在：

```text
Application.persistentDataPath/MmAsset/atest/hot/
```

清单缓存位于：

```text
Application.persistentDataPath/MmAsset/atest/manifest/server.json
Application.persistentDataPath/MmAsset/atest/manifest/local.json
```

再次运行时，MD5 一致的文件不会重复下载。

---

## 7. 第四项测试：断点续传与强校验

建议准备一个较大的 AB。

### 7.1 断点续传

1. 开始热更下载
2. 下载中退出游戏
3. 检查热更目录

未完成文件后缀为：

```text
.download
```

4. 再次启动游戏

客户端会根据临时文件长度发送 HTTP Range 请求并继续下载。

### 7.2 MD5 强校验

1. 在服务器上手动改动一个 AB 的任意字节
2. 不修改 `hot_manifest.json` 中的 MD5
3. 启动热更

预期：

- 文件下载完成后 MD5 校验失败
- 自动删除错误临时文件
- 按配置次数重试
- 最终失败时不会覆盖正式文件
- `local.json` 不会更新为新版本

恢复正确 AB 后重新启动即可继续更新。

---

## 8. 第五项测试：客户端强制更新

热更窗口中的“母包版本”会写入：

```text
minClientVersion
```

客户端使用：

```text
Application.version
```

也就是 Player Settings 中的 Version。

例如：

```text
客户端版本：1.0.0
Manifest minClientVersion：2.0.0
```

`BootModule` 会抛出：

```csharp
AssetUpdateRequiredException
```

业务处理示例：

```csharp
try
{
    await AssetFrame.Instance.BootModule(
        BundleModuleEnum.ATest,
        progress,
        cancellationToken);
}
catch (AssetUpdateRequiredException exception)
{
    Debug.Log("需要更新客户端 最低版本 " + exception.MinimumVersion);
    // 在这里打开商店或安装包下载页面
}
```

---

## 9. 常用资源加载 API

以下 API 都应在对应模块 `BootModule` 完成后使用。

### 9.1 加载但不实例化

```csharp
var prefab = AssetFrame.Instance.Resources.LoadResource<GameObject>(
    "Game/Prefabs/Role");
```

异步：

```csharp
var prefab = await AssetFrame.Instance.Resources.LoadResourceAsync<GameObject>(
    "Game/Prefabs/Role",
    cancellationToken);
```

### 9.2 实例化预制体

同步：

```csharp
var instance = AssetFrame.Instance.Resources.Instantiate(
    "Game/Prefabs/Role",
    parent);
```

异步：

```csharp
var instance = await AssetFrame.Instance.Resources.InstantiateAsync(
    "Game/Prefabs/Role",
    parent,
    cancellationToken: cancellationToken);
```

### 9.3 边下载边等待目标资源

```csharp
var instance = await AssetFrame.Instance.Resources.InstantiateWhenReadyAsync(
    "Game/Prefabs/Role",
    parent,
    cancellationToken);
```

目标 AB 下载完成后会自动继续加载，不需要业务注册回调。

### 9.4 图片 音频 文本

```csharp
Sprite icon = AssetFrame.Instance.Resources.LoadSprite("UI/Icon");
Texture texture = AssetFrame.Instance.Resources.LoadTexture("UI/Background");
AudioClip audio = AssetFrame.Instance.Resources.LoadAudio("Audio/Bgm");
TextAsset config = AssetFrame.Instance.Resources.LoadTextAsset("Config/Role");
```

异步图片：

```csharp
Sprite icon = await AssetFrame.Instance.Resources.LoadSpriteAsync(
    "UI/Icon",
    image,
    true,
    cancellationToken);
```

图集：

```csharp
Sprite icon = AssetFrame.Instance.Resources.LoadAtlasSprite(
    "UI/MainAtlas",
    "IconName");
```

---

## 10. 对象池与资源卸载

### 10.1 预热对象池

```csharp
AssetFrame.Instance.Resources.PreLoadObj(
    "Game/Prefabs/Bullet",
    20);
```

### 10.2 回收实例

```csharp
AssetFrame.Instance.Resources.Release(instance);
```

对象会隐藏并移动到：

```text
RecyclObjRoot
```

下次加载同一资源时通过栈结构 O1 取出。

### 10.3 彻底销毁实例

```csharp
AssetFrame.Instance.Resources.Release(instance, true);
```

### 10.4 卸载单个模块

```csharp
AssetFrame.Instance.Resources.UnloadModule(
    BundleModuleEnum.ATest,
    true);
```

会清理：

- 模块创建的实例
- 模块对象池
- 模块资源缓存
- 模块 AB 和依赖
- 模块地址索引

### 10.5 全局清理

只清空池中闲置实例：

```csharp
AssetFrame.Instance.Resources.ClearResourcesAssets(false);
```

深度清理并主动回收：

```csharp
AssetFrame.Instance.Resources.ClearResourcesAssets(
    true,
    true);
```

`collectGarbage` 会触发 `UnloadUnusedAssets` 和 `GC.Collect`，不要在频繁流程中调用。

---

## 11. 加密测试

### 11.1 推荐设置

```text
是否加密：开启
加密范围：ConfigOnly
加密密钥：自定义字符串
```

`ConfigOnly` 只加密地址表，运行时开销较小。

`AllBundles` 会加密全部 AB。

### 11.2 运行机制

构建时使用 AES 加密。

加载时：

1. 检查文件 AES 头
2. 流式解密到 `temporaryCachePath`
3. 使用 `LoadFromFile` 或 `LoadFromFileAsync`
4. 避免整包 `LoadFromMemory` 的双份内存峰值

解密缓存：

```text
Application.temporaryCachePath/MmAsset/{module}/decrypted/
```

### 11.3 注意事项

- 加密设置改变后必须重新构建全部相关 AB
- 构建和运行必须使用同一密钥
- 不要在同一版本中途更换密钥
- 客户端内密钥只能提高分析成本，不能替代服务端权限控制

---

## 12. 热更文件上传

编辑器“上传资源”使用 HTTP PUT。

需要配置环境变量：

```text
MMASSET_UPLOAD_URL
MMASSET_UPLOAD_TOKEN
```

其中 Token 可选。

上传后的远程目录约定：

```text
{UploadUrl}/HotAssets/{module}/hot_manifest.json
{UploadUrl}/HotAssets/{module}/{version}/{platform}/*.unity
```

如果服务器不支持 HTTP PUT，请先使用手动复制方式测试。

---

## 13. CI 构建

命令行入口：

```text
MmAssetCIBuild.BuildFromCommandLine
```

示例：

```powershell
Unity.exe -batchmode -quit -projectPath . `
  -executeMethod MmAssetCIBuild.BuildFromCommandLine `
  -mmAssetKind hot `
  -mmAssetVersion 1.2.0 `
  -mmAssetTarget StandaloneWindows64 `
  -mmAssetUpload true
```

参数：

| 参数 | 示例 | 说明 |
|---|---|---|
| `-mmAssetKind` | `full` 或 `hot` | 完整 AB 或热更 |
| `-mmAssetVersion` | `1.2.0` | 热更资源版本 |
| `-mmAssetTarget` | `StandaloneWindows64` | 目标平台 |
| `-mmAssetUpload` | `true` | 构建后是否上传 |

多平台建议由 CI matrix 每个平台启动一次 Unity。

---

## 14. 自检

菜单：

```text
Tools/MieMieFrameWork/MmAsset/运行自检
```

构建按钮也会自动执行自检。

主要检查：

- BundleSettings 是否存在
- 热更地址是否配置
- 下载线程数是否合法
- 加密密钥是否为空
- 模块枚举是否同步
- 模块名称是否重复
- AB 名是否为 ASCII 安全字符
- 自定义别名是否重复
- 构建目录是否存在
- 共享依赖阈值是否合法

Console 出现零错误即可开始构建。

---

## 15. 建议逐项验收清单

### 测试一 Editor 直读

- [ ] 设置 `Editor + NotHot`
- [ ] 使用完整 `Assets/...` 路径实例化 Prefab
- [ ] 回收后再次创建验证对象池

### 测试二 随包 AB

- [ ] 设置 `AssetBundle + NotHot`
- [ ] 点击“打包”
- [ ] 点击“内嵌”
- [ ] 确认 `atest_builtin.json`
- [ ] `BootModule` 完成
- [ ] 使用别名加载资源

### 测试三 热更

- [ ] 修改资源
- [ ] 构建版本 `1.0.1`
- [ ] 部署本地 HTTP 目录
- [ ] 设置 `AssetBundle + Hot`
- [ ] 观察 CheckVersion 和 Download
- [ ] 再次启动确认不重复下载

### 测试四 可靠性

- [ ] 中断下载后验证 `.download`
- [ ] 重启验证 HTTP Range
- [ ] 篡改 AB 验证 MD5 重试
- [ ] 升级版本后验证旧 AB 清理
- [ ] 提高 minClientVersion 验证强更异常

### 测试五 生命周期

- [ ] `PreLoadObj`
- [ ] `Release`
- [ ] `Release(obj true)`
- [ ] `UnloadModule`
- [ ] `ClearResourcesAssets`

---

## 16. 常见问题

### 找不到资源地址表

确认已经：

1. 使用当前代码重新打包
2. 点击“内嵌”或部署热更 AB
3. 目标平台与运行平台一致
4. 目录名为小写模块名

### Editor 模式下别名无效

没有加载 AbConfig 时，Editor 模式无法知道自定义别名对应哪个资源。

请先使用完整 `Assets/...` 路径，或先构建并启动模块。

### 浏览器能打开 Manifest 但 AB 下载失败

检查 `hot_manifest.json` 中的 `downloadUrl`。

它必须能直接拼出：

```text
{downloadUrl}/{abName}
```

### 手机连接不到本地服务器

- 不要使用 `127.0.0.1`
- 使用电脑局域网 IP
- 确认手机和电脑在同一网络
- 确认 Windows 防火墙允许端口

### 修改资源后运行仍是旧版本

资源优先读取 persistent 热更目录。

开发测试时可清理：

```text
Application.persistentDataPath/MmAsset/atest/
```

再重新启动。

### AB 名能不能写中文

Unity 本身可能允许部分中文名称，但 CDN URL 大小写 文件系统和跨平台工具链更容易出问题。

当前构建器会主动限制为 ASCII 安全名称。

