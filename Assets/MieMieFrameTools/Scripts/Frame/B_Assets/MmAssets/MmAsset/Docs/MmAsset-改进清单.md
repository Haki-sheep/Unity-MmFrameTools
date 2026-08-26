# MmAsset 资源管理模块 · 改进清单

> 状态：9.1~9.5 已落地并进入编译验收  
> 范围：`Assets/MieMieFrameTools/Scripts/Frame/B_Assets/MmAssetsMethod/MmAsset`  
> 编辑器：`Assets/MieMieFrameTools/Editor/MmAssetForEditor`  
> 关联入口：`AssetFrame` / `ResourceManager` / `AssetBundleManager` / `HotAssetsManager`

---

## 1. 目标架构（四层）

已确认采用以下分层（交付 / 安全 / 使用）：

```
┌─────────────────────────────────────────────┐
│ 加载层  ResourceManager + AssetBundleManager │
│  业务 API ≈「自研版 Load」 非 Unity Resources │
│  打开 AB 时按需解密（不先全盘解密）            │
├─────────────────────────────────────────────┤
│ 加解密层  AES / BundleEncryptToggle（横切）   │
├──────────────────┬──────────────────────────┤
│ 随包层           │ 热更层                    │
│ StreamingAssets  │ 服务器 → HotAssets 目录   │
│ → 解压到可写目录 │ （差分下载落盘）           │
└──────────────────┴──────────────────────────┘
```

| 层 | 职责 | 现有实现 |
|----|------|----------|
| 随包层 | 首包内嵌；移动端拷贝/解压到可写目录 | `StreamingAssets` + `AssetsDeCompressManager` |
| 热更层 | 拉清单、差分下载、落盘、旧包清理 | `HotAssetsManager` / `HotAssetsModule` |
| 加解密层 | 打包加密；读盘时解密 | `AES` + `BundleEncryptToggle` |
| 加载层 | 按路径/CRC 取资源、依赖、池化、卸载 | `ResourceManager` + `AssetBundleManager` |

### 1.1 随包要不要「提前压缩」？

分两层看，别混：

| 类型 | 要不要 | 说明 |
|------|--------|------|
| **AB 自身压缩** | 要（打 AB 时选） | `ChunkBasedCompression`(LZ4) 等，运行时 `LoadFromFile` 直接读，无需再解压成「原始资源」 |
| **整包再打 Zip** | 可选，非必须 | 再压一层可略减安装包，但首启多一道解压；当前代码的「解压」主要是 **从 StreamingAssets 拷到 persistent**（安卓/iOS 只读限制），不是必须先做 Zip |

**建议默认：** 靠 Unity AB 压缩选项即可；不必再套一层 Zip，除非首包体积被渠道卡死再评估。

### 1.2 热更下载放到哪？

**平台根目录（系统定）+ 子目录（我们定）：**

- 根：`Application.persistentDataPath`（可读可写、卸载清理、适合热更）
- 子：`/MmAsset/{module}/hot/`（约定，应唯一由 `BundleSettings` 生成）

加载时：**同名文件优先热更目录，没有再用随包解压目录**（现有 `AssetBundleManager` 逻辑）。

### 1.3 加载层是不是等于 `Resources.Load`？

**角色类似（给名字/路径拿资源），实现完全不是一回事。**

| | Unity `Resources.Load` | MmAsset 加载层 |
|--|------------------------|----------------|
| 资源来源 | 工程 `Resources/` 打进包体 | 本地 AB（解压目录或热更目录） |
| 热更 | 基本不能热更 | 支持 |
| 加密 | 无 | 打开 AB 时解密 |
| 额外能力 | 无 | 对象池、依赖 AB、等下载再克隆 |

`BundleSettings` 放在 `Resources.Load("BundleSettings")` 只是配置引导，和业务资源加载不是一条路。

**解密时机：** 不在进游戏前把所有包解开；在加载层 `LoadFromFile` / `LoadFromMemory` 打开某个 AB 时按需解密（与「加载前整库解密」不同）。

---

## 1.4 模块现状摘要

业务经 `AssetFrame` 访问上述能力。编辑器：`Tools/MieMieFrameWork/MmAsset/资源管线`。

**已具备：** 四层目录、BootModule、UniTask、统一进度与取消、断点续传、校验重试、异步 AB、模块卸载、地址别名、共享依赖抽包、构建报告、Sample 与 CI 入口。

---

## 2. 改进优先级总览

| 优先级 | 目标 | 原则 |
|--------|------|------|
| **P0** | 正确性 | 路径/配置/枚举不一致会导致「下了却加载失败」 |
| **P1** | 性能与生命周期 | 主线程卡顿、内存峰值、卸载粒度 |
| **P2** | 易用性与架构 | 降低业务心智、目录边界、文档样例 |

**建议主线（一次只做一条链）：**  
~~统一路径并修 P0~~ → ~~补 `BootModule`~~ → ~~异步打开 AB / 按模块卸载~~ **均已落地**。  
§9.1~9.5 已采纳进 backlog；§9.6 平台差异暂缓（非 WebGL、手游未排期时再做）。

### Phase1 已落地摘要

- `BundleSettings`：路径唯一出口（热更/解压/内嵌/清单/config 文件名/Resolve）
- `GeneratorBundleConfigPath`：热更优先 → 解压 → 失败；LoadAsset 用 `*_AbConfig` 资源名
- `HotAssetsModule`：不再手写 Hot 路径；完成回调先减再加
- 枚举：`E_RuntimeBundleMode` / `E_EditorBuildKind`
- `Test.cs`：去掉重复 `OnMainThreadUpdate`

注意：模块目录现统一小写（如 `atest`）。需重新打 AB / 拷 StreamingAssets / 上传 Hot 目录，旧的 `ATest` 大小写路径不再作为运行时约定。

---

## 3. P0 正确性

### 3.1 AB 配置路径判定逻辑

**位置：** `AssetBundleManager.GeneratorBundleConfigPath`

**问题：**

1. `bundleConfigJsonPath` 仅为相对名（如 `atest_AbConfig`），不是完整磁盘路径，`File.Exists` 结果不可靠。
2. 分支语义疑似反了：`Exists == true` 时直接 `return false`；不存在时却改走解压路径。

**期望逻辑：**

```
热更目录存在 bundleConfig → 用热更路径
否则 解压目录存在 bundleConfig → 用解压路径
两边都没有 → return false
```

**验收：** 仅热更有包、仅解压有包、两边都无，三种情况路径与返回值正确。

---

### 3.2 热更落盘路径不统一

**位置：**

- `HotAssetsModule.HotAssetsSavePath`
- `BundleSettings.GetHotAssetsSavePath`

**问题：**

- 前者拼接疑似缺少 `/`，且模块名大小写可能与 Settings（`ToLower`）不一致。
- 下载写 A、加载读 B → 热更成功但运行时找不到文件。

**期望：** 全项目统一走 `BundleSettings` 的路径 API，禁止各处手写拼接。

**验收：** 同一模块下载目录与 `LoadAssetBundle` 读盘目录字符串完全一致。

---

### 3.3 同名枚举撞车

**位置：**

| 定义处 | 当前名 | 实际含义 |
|--------|--------|----------|
| `BundleSettings.cs` | `E_BuildBundleType` | 运行时：`NotHot` / `Hot` |
| `BuildBundleComplier.cs` | `E_BuildBundleType` | 编辑器：`AssetBundle` / `HotPatch` |

**期望重命名（示例）：**

- 运行时 → `E_RuntimeBundleMode`（`Offline` / `Hot`，或保留 `NotHot` / `Hot`）
- 编辑器 → `E_EditorBuildKind`（`AssetBundle` / `HotPatch`）

**验收：** 全局无同名冲突；打包窗口与运行时开关语义清晰。

---

### 3.4 热更完成回调重复订阅

**位置：** `HotAssetsModule.StartHotAssets` 中 `OnDownLoadAllAssetsComplete += hotFinish`

**问题：** 同一模块多次进入热更会叠加多份完成回调。

**期望：** 赋值覆盖、或先 `-=` 再 `+=`、或使用一次性回调 Token。

**验收：** 连续两次 `HotAssets(同一模块)`，完成回调只触发业务期望次数。

---

## 4. P1 性能与资源生命周期

| 项 | 现状 | 改进方向 | 备注 |
|----|------|----------|------|
| AES 读包 | `LoadFromMemory` 整包进内存 | 大包改为流式解密或临时落盘再 `LoadFromFile`；或仅加密清单/关键包 | 内存峰值 |
| 异步加载 | 资源 `LoadAssetAsync`，AB 打开仍同步 | `LoadFromFileAsync` + UniTask | 真正不卡主线程 |
| 对象池取出 | `List.RemoveAt(0)` O(n) | 栈式尾删，或 `Stack`/`Queue` | 高频 Instantiate 场景 |
| 深度清理 | `ClearResourcesAssets` 内 `GC.Collect` + `UnloadUnusedAssets` | 改为可选参数，默认不强制 GC | 切大场景再开 |
| 模块卸载 | 有引用计数，缺按模块卸 | 增加 `UnloadModule(BundleModuleEnum)` | 大厅⇄战斗切模块 |

---

## 5. P2 易用性与架构

### 5.1 一键启动管线 `BootModule`

**动机：** 业务需自行编排 解压 → 检版 → 热更 → `LoadAssetBundleConfig`。

**期望 API（草案）：**

```csharp
await AssetFrame.Instance.BootModule(
    BundleModuleEnum.ATest,
    onProgress: (step, p) => { /* UI */ });
```

内部顺序：解压内嵌 → 检查版本 → 热更下载 → 加载 AB 配置 → 完成。

同步：`Test.cs` 等业务侧不再手动调用 `OnMainThreadUpdate`（仅保留 `AssetFrame.Update`）。

---

### 5.2 路径别名表（可选）

**现状：** 业务必须写完整 `Assets/...` 路径，靠 CRC 索引。

**改进：** 打包时额外生成 `shortName → path/crc`；业务可用 `"Enemy/Goblin"`。

**说明：** 不必上完整 Addressables；第二轮再做。

---

### 5.3 `InstantiateAndLoad` 语义

**现状：** 本地无资源时可能先 `Instantiate` 失败打 Error，再进入等待下载队列。

**期望：** 先查配置/磁盘是否存在；不存在则静默注册等待，避免错误日志噪音。

---

### 5.4 目录与程序集边界

**现状：** Runtime 目录下存在 `BundleBuild/Editor` 相关结构，边界模糊。

**期望：**

```
MmAsset/
  Runtime/     # 纯运行时
  Editor/      # 打包与窗口
  Docs/        # 文档
```

Runtime / Editor 分 asmdef，避免误引用。

---

### 5.5 最小 Sample

建议一份可运行示例覆盖：

1. `InitFrame`
2. 解压进度
3. 热更进度
4. `Instantiate` 一个测试 Prefab
5. `Release` 回池

---

## 6. 暂不优先（记录备忘）

- 完整 Addressables 式远程 Catalog / 依赖可视化
- 多版本回滚 UI 与完整 Diff 工具链（若已有 HotPatch 窗口可后续增强）
- 无额外需求时不主动加判空/边界兜底（保持现有风格）

---

## 7. 讨论区

### Q1 是否可分为：随包 / 热更 / 加解密？

**结论：可以，而且这是「资源从哪来 + 是否加密」的划分，和现有代码大致对应。**

| 你说的部分 | 现有对应 | 说明 |
|------------|----------|------|
| 随包资源 | StreamingAssets 内嵌 + `DeCompressAssets` 解压 | 首包带上 装完解压到持久化目录 |
| 热更资源 | `HotAssets` 下载到 HotAssets 目录 | 检版后按清单差分下载 |
| 加密/解密 | `BundleEncryptToggle` + `AES` | 横切能力 打包装加密 加载时解密 同时作用于随包与热更文件 |

补充：完整框架还有第四块 **运行时加载**（`ResourceManager` + 对象池），不属于「资源从哪来」，而是「拿到本地文件后怎么用」。  
架构表述推荐：

```
交付层：随包 | 热更
安全层：加解密（横切二者）
使用层：加载 / 池化 / 卸载
```

是否改代码：暂不。仅统一文档与心智模型；后续重构目录可按此三分 + 加载层拆文件夹。

---

### Q2 是否要把所有游戏资源都勾选？只勾预制体，材质网格会进包吗？

**结论：不需要勾全项目所有资源；预制体分包模式会自动收集依赖进包。**

使用方式：

1. 在模块配置里只配「要进该模块的目录策略」：预制体分包 / 子文件夹分包 / 整包。
2. 未出现在任何模块配置路径下的资源 → **不会被打进该模块 AB**。
3. 不需要在工程里把每个材质网格手动勾一遍。

**只配预制体目录时（`prefabPacks`）：**

- `BuildAllPrefab` 对每个 Prefab 调用 `AssetDatabase.GetDependencies`。
- 材质、网格、贴图、依赖的其它 Prefab 等会进入该 Prefab 对应 AB 的收集列表（与预制体打进同一包名），脚本 `.cs` 在写配置时会被排除。
- 因此：**只勾/只配预制体目录，一般会把身上引用的材质、网格等一起打进去**，运行时加载该 Prefab 通常够用。

注意点：

| 情况 | 行为 |
|------|------|
| 依赖已被别的包先收集（`IsRepeatPath`） | 本 Prefab 包不再重复打这份资源，依赖写到别的 AB，运行时靠 `dependencielist` 加载 |
| 多个 Prefab 共用同一材质 | 谁先打包谁「抢走」该资源进自己的包 → 可能包体不均、共享差；后续可考虑抽公共依赖包 |
| 资源不在任何模块配置里、也不是已收集 Prefab 的依赖 | 不会进包，运行时 CRC 找不到 |

是否改代码：暂不。若以后要做「共享依赖自动抽包」再立项。

---

### Q3 资源管理器还应考虑什么？（补充项）

见 **§9**。结论：**9.1~9.5 采纳**；**9.6 暂缓**（WebGL/手游未排期）。

---

### Q4 四层架构细节 + 路径规划诉求

**结论：四层成立；随包不必再套 Zip；热更落 `persistentDataPath/HotAssets`；加载层 ≠ `Resources.Load`；解密在打开 AB 时按需做。路径规划见 §10，改代码时以 `BundleSettings`（或新建 `AssetPathConfig`）为唯一出口。**

是否改代码：暂不（本文档先定规范，下次改代码按 §10 收敛）。

---

| 编号 | 问题 | 结论摘要 | 是否改代码 |
|------|------|----------|------------|
| Q1 | 三分：随包/热更/加解密 | 可以；加「加载层」成四层 | 否 |
| Q2 | 是否勾全资源；Prefab 依赖 | 不必勾全；Prefab 模式会打依赖 | 否 |
| Q3 | 还应考虑什么 | 9.1~9.5 采纳；9.6 暂缓 | 否 |
| Q4 | 四层细节 / 压缩 / 落盘 / 是否等于 Resources / 路径 | 见 §1 与 §10 | 否（先规范） |
| Q5 | AB 压缩算法 / 加密自研还是库 | 见 §11；默认 LZ4；加密用 BCL AES 薄封装即可 | 否 |
| Q6 | 回调满天飞要什么不要什么 | 见 §12；业务 UniTask+Progress，内部保留下载切主线程 | 否（先表决） |
| Q7 | JSON 散落分不清 | 见 §13；三类职责 + 目标目录收敛 | 否（先规范） |

---

### Q5 压缩算法业内选型？加密自研还是引库？

**结论见 §11。**  
压缩：本框架（自研下载 + `LoadFromFile`）默认 **LZ4（`ChunkBasedCompression`）**。  
加密：**不必引 AssetStore 插件**；用运行时自带的 `System.Security.Cryptography` 做 AES 薄封装即可（现有 `AES.cs` 路线对，后续可收紧用法与范围）。

是否改代码：暂不。定默认策略后，改 `BundleSettings` 默认压缩选项与加密范围时再动。

---

## 8. 改动记录

| 日期 | 内容 | 说明 |
|------|------|------|
| 2026-07-26 | 初稿 | 评估后落地文档，代码暂不修改 |
| 2026-07-26 | Q1~Q3 | 补充讨论结论与 §9 考虑项 |
| 2026-07-26 | Q4 + §1/§10 | 四层架构、路径规划；9.1~9.5 采纳 9.6 暂缓 |
| 2026-07-26 | Q5 + §11 | 压缩/加密业内选型与本项目建议 |
| 2026-07-26 | §12 | 回调清单：留/改/砍；性能原则快准狠 |
| 2026-07-26 | Q7 + §13 | JSON/清单职责对照与目录收敛建议 |
| 2026-07-26 | Phase1 | P0 路径统一 枚举拆分 回调去叠加 已改代码 |
| 2026-07-26 | Phase2 | 9.1~9.5 四层目录 Runtime UniTask 热更可靠性 构建管线 编辑器 Sample CI 已改代码 |
| 2026-07-27 | §12.4 | 回调表决表按最优方案拍板 与现网代码一致 |

---

## 9. Backlog 与采纳状态

| 小节 | 主题 | 状态 |
|------|------|------|
| 9.1 | 打包与依赖策略（共享抽包、包体分析、Shader、Scene） | **已落地** |
| 9.2 | 版本与热更可靠性（校验、续传、旧包清理、强更 App） | **已落地** |
| 9.3 | 运行时加载体验（UnloadModule、进度取消、异步 AB、别名） | **已落地** |
| 9.4 | 安全与包体（加密范围、首包/热更分流） | **已落地** |
| 9.5 | 工程化（CI、Sample、路径唯一数据源） | **已落地** |
| 9.6 | 平台差异（WebGL / 手游专项） | **暂缓** 有明确目标平台再做 |

### 9.1 打包与依赖策略（采纳）

- 共享依赖抽包（`common` AB）
- 冗余与包体分析报告
- Shader 变体策略 / 预热
- Scene 进 AB 规则

### 9.2 版本与热更可靠性（采纳）

- 下载后强校验失败重下
- 断点续传 / 失败重试
- 旧版本清理
- 资源版本与客户端兼容（强制更 App）

### 9.3 运行时加载体验（采纳）

- `UnloadModule`
- Boot 统一进度与取消
- `LoadFromFileAsync`
- 地址别名表

### 9.4 安全与包体（采纳）

- 加密范围策略（忌无脑全量 AES + LoadFromMemory）
- 首包 vs 纯热更分流规则与工具

### 9.5 工程化（采纳）

- CI 打多平台 + 上传 CDN
- 最小 Sample + 自检菜单
- **路径唯一数据源**（与 §10 绑定，优先落地）

### 9.6 平台差异（暂缓）

- WebGL / 手游专项权限与路径差异：有排期再开

---

## 10. 路径规划（系统定 vs 我们定）

### 10.1 原则

1. **系统根路径只读 API，不手写盘符**  
   只用 `Application.streamingAssetsPath` / `Application.persistentDataPath`。
2. **业务子目录只在一处定义**  
   全部经 `BundleSettings`（或后续 `AssetPathConfig`）的 `GetXxx`，禁止 `HotAssetsModule` 等处再拼字符串。
3. **读盘优先级固定**  
   热更目录 > 随包解压目录；都不存在则失败。
4. **模块名大小写统一**  
   建议目录一律 `module.ToLowerInvariant()`，与清单/URL 一致。

### 10.2 目录表

| 用途 | 完整形态（示意） | 谁决定 | 权限/原因 |
|------|------------------|--------|-----------|
| 随包内嵌根 | `{streamingAssetsPath}/AssetBundle/{Module}/` | 根=系统；`AssetBundle/模块`=我们 | 随安装包只读；安卓等不宜直接 `File` 乱读 |
| 随包解压落地 | `{persistentDataPath}/MmAsset/{module}/decompress/` | 根=系统；子目录=我们 | 可读可写；供 `LoadFromFile` |
| 热更落地 | `{persistentDataPath}/MmAsset/{module}/hot/` | 根=系统；子目录=我们 | 可读可写；服务器下载目标 |
| 热更清单（本地） | `{persistentDataPath}/MmAsset/{module}/manifest/` | 我们 | 区分 server 与 local |
| 服务器 URL | `{downloadUrl}/HotAssets/{module}/...` | 我们（配置） | CDN/静态服务器约定 |
| 编辑器打出目录 | 工程内约定输出路径（Build 窗口） | 我们 | 仅编辑器；再 Copy 到 StreamingAssets 或上传 |
| 框架配置 SO | `Resources/BundleSettings` | 我们（必须放 Resources 才能用现有 Load） | 引导配置，体积应极小 |
| 内嵌清单 TextAsset | `Resources/{module}_builtin.json` | 我们 | 解压比对 MD5 用 |

### 10.3 数据流（简图）

```
[编辑器打 AB]
    → 输出目录（我们定）
    → Copy → StreamingAssets/AssetBundle/{Module}/   （随包）
    → 上传 → {downloadUrl}/HotAssets/{Module}/       （热更源）

[运行时首次]
    StreamingAssets ──提取──→ persistent/MmAsset/{module}/decompress/

[运行时热更]
    服务器 ──下载──→ persistent/.../HotAssets/{module}/

[运行时加载]
    优先 hot → 否则 decompress → 最后 StreamingAssets → 按需流式解密后 Load
```

### 10.4 现状问题（待改代码时清）

| 问题 | 位置 | 处理 |
|------|------|------|
| 热更路径缺 `/`、大小写不一致 | `HotAssetsModule.HotAssetsSavePath` vs `BundleSettings.GetHotAssetsSavePath` | 删除前者拼接，统一 Settings |
| 配置路径 `File.Exists` 用了相对名 | `GeneratorBundleConfigPath` | 按 §3.1 用完整路径判断 |
| SerializeField 默认值里写死 `Application.xxx` | `BundleSettings` | 运行时用属性拼接更稳（SO 序列化时 Application 可能不对） |
| 多处手写 `/HotAssets/` | 下载 URL 与本地 | 抽常量或统一 API |

### 10.5 目标 API 草案（改代码时）

```csharp
// 唯一出口示例
string GetBuiltInPath(BundleModuleEnum m);      // StreamingAssets/...
string GetDecompressPath(BundleModuleEnum m);   // persistent/MmAsset/{module}/decompress/...
string GetHotPath(BundleModuleEnum m);          // persistent/MmAsset/{module}/hot/...
string GetHotUrl(BundleModuleEnum m);           // downloadUrl + ...
string ResolveBundleFile(BundleModuleEnum m, string abName);
// Resolve = Hot 存在用 Hot 否则 Decompress
```

业务与热更/加载代码 **禁止** 再直接拼 `persistentDataPath + "/HotAssets"`。

---

## 11. 压缩与加密选型

### 11.1 AssetBundle 压缩（Unity 官方三档）

| 格式 | 对应选项 | 体积 | 加载 | 业内用法 |
|------|----------|------|------|----------|
| **LZ4** | `ChunkBasedCompression` | 中 | 快，可按块解压 | **本地/随包/落盘后常驻首选** |
| **LZMA** | 默认/`None` 等整包流式 | 最小 | 慢，往往要整包进内存再读 | **CDN 下载包**常见；用官方 `UnityWebRequestAssetBundle` 缓存时会再压成 LZ4 |
| **Uncompressed** | `UncompressedAssetBundle` | 最大 | 通常最快（吃 IO） | 编辑器/极致加载；正式包少用 |

官方/Addressables 常见口径：

- **本地内容 → LZ4**
- **远程下载 → LZMA**（再靠官方缓存转 LZ4）

**对本项目（MmAsset）的含义：**

当前是 **自研下载落盘 + `LoadFromFile`**，没有走 Unity AB Cache 的「下完自动转 LZ4」。若服务器发 LZMA，客户端每次 `LoadFromFile` 都会偏慢。

**本项目建议默认：**

1. **随包 + 热更落盘后的包：统一打 LZ4（`ChunkBasedCompression`）** —— 最省事、加载稳，业内自研热更里最常见。
2. 若以后 CDN 流量/包体极度敏感：可「服务器存 LZMA → 下载后本地重压成 LZ4 再存 HotAssets」，属于增强项，不必一期做。
3. 不必在 AB 外再套一层 Zip（与 §1.1 一致）。

### 11.2 加密：自研还是引库？

| 方案 | 评价 | 是否推荐 |
|------|------|----------|
| **BCL `System.Security.Cryptography`（AES）** | 运行时自带，无第三方依赖；薄封装几十～百行即可 | **推荐**（现有 `AES.cs` 即此路） |
| AssetStore「AB 加密插件」 | 往往绑定别人热更框架，重、难改 | 不推荐 |
| BouncyCastle 等大库 | 算法全但体积大 | 游戏 AB 场景无必要 |
| 自研 XOR / 偏移打乱 | 实现极小，仅防小白解包 | 可作「轻扰」；不当真安全 |
| XXTEA 等小算法 | 国内部分项目爱用，快、代码短 | 可作 AES 替代，收益不大 |

**业内真实情况：**

- AB 加密主要是 **提高破解成本**，防不住决心逆向（密钥终究在客户端）。
- 常见做法：**清单 + 关键包 AES**；或全量加密但避免每次整包 `LoadFromMemory`。
- **不需要为了加密再买插件**；自己搓一层封装调用系统 AES 就够。

**本项目建议：**

1. 继续用现有 AES 封装，不引新库。
2. 密钥勿写死在明文常量（可拆分/混淆；真要严再考虑下发会话密钥，成本高）。
3. 与 §9.4 对齐：优先加密 **Manifest / bundleConfig**；全量加密时评估改为「解密落临时可写文件再 `LoadFromFile`」或流式，避免大包内存尖峰。
4. 若只要防闲人：可对 AB 头做简单异或，加载更快——安全级别声明为「防误看」即可。

---

## 12. 性能原则与回调清单

### 12.1 性能原则（快准狠）

| 原则 | 落地 |
|------|------|
| **快** | 地址查找 Dictionary O(1)；热路径少分配、少回调跳转；AB 用 LZ4 + 真异步打开；池化尾删 |
| **准** | 路径唯一出口；热更优先于解压；CRC/别名与打包表一致；下载强校验 |
| **狠** | API 面砍薄：一条 Boot / 一套 Load；内部管线自己串，业务少填回调 |

回调过多会直接伤「快」和「狠」：一次热更链路里多层 `Action` 套娃，难追、易漏退订、易重复订阅。

### 12.2 回调全景（Runtime）

#### A. 业务对外 API（Test / UI 会碰到）

| 回调 | 所在 | 现状作用 | 建议 |
|------|------|----------|------|
| `HotAssets(..., startHot, hotFinish, waitDownload)` | `IHotAssets` | 开始下 / 下完 / 排队等 | **改** → 优先 `UniTask` 完成；进度用 `IProgress` 或单一 `OnHotProgress`；排队可并进进度状态枚举 |
| `CheckAssetsVersion(..., Action<bool,float>)` | `IHotAssets` | 是否要更、多大 | **改** → 已是 `UniTask`，去掉 Action，改 `return (needHot, sizeMb)` |
| `StartDeCompress...(Action callback)` | `IDeCompressAssets` | 解压完成 | **改** → `UniTask`；进度已有属性可轮询或 `IProgress` |
| `InstantiateAndLoad(loadAsync, loading, param1, param2)` | `IResources` | 等热更再克隆 | **改** → `UniTask<GameObject>` + `CancellationToken`；砍 `object param1/2`（闭包即可） |
| `loading` Action | 同上 | 开始等下载时通知 | **可砍** → 由进度/状态代替 |

#### B. 模块级事件（半公开）

| 回调 | 所在 | 现状作用 | 建议 |
|------|------|----------|------|
| `OnDownLoadAllAssetsComplete` | `HotAssetsModule` | 模块下完；`+= hotFinish` 易叠 | **改** → 单次完成源（UniTaskCompletionSource）或赋值而非 `+=` |
| `OnDownLoadAssetBundleListener` | `HotAssetsModule` | 单 AB 下完字符串通知 | **砍对外** → 仅内部；UI 要细节走统一 Progress（已下字节/总数） |
| `OnDownLoadAbConfigListener` | `HotAssetsModule` | **声明了基本未用** | **砍** |
| `WaitDownloadModule` 里三个 Action | `HotAssetsManager` | 排队模块暂存回调 | **留结构、减字段** → 排队只存 moduleEnum + UniTask 续跑 |
| `hotAssetsProgress` | `WaitDownloadModule` | **字段在，业务链几乎未接** | **砍或做成真进度事件** 二选一，禁止空挂 |

#### C. 框架内总线（业务不应直接碰）

| 回调 | 所在 | 现状作用 | 建议 |
|------|------|----------|------|
| `static DownLoadBundleFinish` | `HotAssetsManager` | 驱动 `InstantiateAndLoad` 补克隆 | **留但改** → 非 static、可退订；或内联到加载层接口，禁止业务再 `+=` |
| `DownloadThread` onSuccess/onFailed | 下载线程 | 线程回传 | **留**（内部必须） |
| `AssetsDownLoader` 主线程队列 | 下载器 | 切回主线程 Invoke | **留**（内部必须） |
| `DownloadEventHandler.downloadEvent` | 下载器 | 成功/失败/完成三种事件 | **留内部**；对外不要再暴露第三套 |

#### D. Editor

`EditorApplication.delayCall`：仅编辑器开窗，**留**，不进运行时讨论。

### 12.3 讨论结论（已拍板 · 行业常见最优）

原则一句话：**业务只碰 `UniTask + IProgress + CancellationToken`；线程切主线程与单包就绪通知只留框架内部。**

**要（保留或形态升级）：**

1. 热更/解压 **完成** → `await UniTask`（主路径）
2. 热更/解压 **进度** → 单一 `IProgress<AssetBootProgress>`，阶段含 `Decompress / CheckVersion / Download / Queued / LoadConfig / Completed`
3. 下载线程 → 主线程 **内部** 回调（必须留）
4. 「单文件下完通知加载层补克隆」→ **实例事件 + Init 可退订**（必须留；禁止 static 永久 +=；业务禁止订阅）

**不要（砍或合并）：**

1. 同一 API 上同时 `start + finish + wait + version Action` 四套
2. `OnDownLoadAbConfigListener`
3. `InstantiateAndLoad` 的 `param1/param2` 以及单独的 `loading` Action
4. 业务侧再手动 `OnMainThreadUpdate`
5. 对外暴露「每个 AB 名字符串 Listener」
6. 空挂的 `hotAssetsProgress`（进度已并进 `AssetBootProgress`，禁止再复活空字段）

**推荐业务最终长相（已落地）：**

```csharp
await AssetFrame.Instance.BootModule(BundleModuleEnum.ATest, progress);
var go = await AssetFrame.Instance.Resources.InstantiateAsync(path, token: cts.Token);
```

热更进行中要边下边显：

```csharp
var go = await Resources.InstantiateWhenReadyAsync(path, token: cts.Token);
```

### 12.4 表决表（最优拍板）

| 项 | 留 / 改 / 砍 | 结论 | 理由（一句话） |
|----|--------------|------|----------------|
| HotAssets 多回调 → UniTask + Progress | **改** | **采纳 · 已落地** | 完成用 await 进度用 IProgress 是热更 UI 最稳形态 少漏退订 |
| CheckAssetsVersion 去掉 Action | **改** | **采纳 · 已落地** | 返回 `HotUpdateCheckResult` 比双回调清晰 可组合 |
| 解压完成 → UniTask | **改** | **采纳 · 已落地** | 与热更同构 便于 `BootModule` 串行 await |
| InstantiateAndLoad → WhenReady UniTask | **改** | **采纳 · 已落地** | 边下边显只保留一个 await 出口 |
| param1/param2、loading | **砍** | **采纳 · 已落地** | 闭包可捕获上下文 loading 状态由 Progress/WhenReady 覆盖 |
| AbConfigListener | **砍** | **采纳 · 已落地** | 从未形成有效业务链 留着只增噪音 |
| AssetBundleListener 对外 | **砍对外** | **采纳 · 对内保留事件** | UI 要字节进度走 Progress 单包名给业务没用 |
| DownLoadBundleFinish 去 static | **改** | **采纳 · 已落地** | 现为实例 `BundleDownloaded` + `Init` 可退订 业务勿 `+=` |
| hotAssetsProgress 空字段 | **砍** | **采纳 · 已落地** | 排队进度已用 `AssetBootStage.Queued` 禁止空挂第二套 |

**唯一可选打磨（非必须）：**  
`IHotAssets.BundleDownloaded` 仍挂在公开接口上；若再抠封装可改成构造注入的内部 `Action`，从 `IHotAssets` 摘掉，避免业务误订。功能上当前已正确，不阻塞验收。

---

## 13. JSON / 清单对照表（哪个是哪个）

当前容易晕，是因为 **同一种「表」有编辑器草稿、随包副本、热更副本、运行时缓存** 多份落点。按 **职责** 认，不要按文件名猜。

### 13.1 一张表认亲

| 昵称 | 你现在能看到的路径（例） | 类型/结构 | 干什么 | 谁写 | 谁读 |
|------|--------------------------|-----------|--------|------|------|
| **资源地址表 AbConfig** | `.../MmAsset/Generated/atest_AbConfig.json`（编辑器中间产物） | `BundleConfig`：path/alias/crc/bundleName/assetName/依赖 | **路径或别名→CRC→落在哪个 AB**，加载用 | 打 AB 时 `WriteAssetBundleConfig` | 运行时从 **AB 里的 config** 读 |
| **AbConfig 的 AB 壳** | `atest_abconfig.unity`（StreamingAssets / HotAssets） | 上面 JSON 打进 TextAsset 再打成 AB | 真机加载用的「地址表载体」 | 打包 | `AssetBundleManager.LoadAssetBundleConfig` |
| **内嵌文件清单** | `.../MmAsset/Resources/atest_builtin.json` | `BuiltInBundleConfig`：fileName/md5/size | **首包要提取哪些文件、MD5 对不对** | Copy 到 StreamingAssets 时写 | `AssetsDeCompressManager` via `Resources.Load` |
| **热更总清单** | `BuildOutput/Hot/atest/hot_manifest.json` | `HotAssetsManifest`：公告/版本/兼容版本/fileList | **服务器有哪些补丁、每个 AB 的 md5/size** | 打 HotPatch 时写，上传 CDN | 客户端下载后比对 |
| **服务器清单缓存** | `{persistent}/MmAsset/atest/manifest/server.json` | 同上 | 刚从 CDN 拉下来的副本 | 运行时下载后保存 | 算差分 |
| **本地已生效清单** | `{persistent}/MmAsset/atest/manifest/local.json` | 同上 | 「我已经更到哪版」 | 热更成功后写 | 下次比对 |
| **Unity 自带 .manifest** | 同目录 `xxx.unity.manifest` | Unity 文本 | 依赖/哈希，**给编辑器/管线看** | `BuildPipeline` | 你们打包后常删；业务加载一般不用 |
| **模块分包配置** | `BuildBundleConfigura`（ScriptableObject，不是 json） | `BundleModuleData` 列表 | 编辑器勾哪些目录怎么打 | Odin 窗口 | 仅编辑器 |
| **运行时设置** | `Resources/BundleSettings`（SO） | 下载 URL、是否热更、加密等 | 框架开关 | 编辑器 | 运行时 |

另外：HotPatch 版本目录下还有具体 AB 文件，例如  
`HotAssets/ATest/1/StandaloneWindows64/atest_cube.unity` —— **那是包体，不是 JSON**。

### 13.2 三者核心区别（最容易混）

```
AbConfig          = 「游戏里某个资源在哪个 AB」(地址簿)
builtInBundleInfo = 「安装包里塞了哪些 AB 文件」(首包清单)
HotAssetsManifest = 「网上要下哪些 AB、版本与 MD5」(热更清单)
```

- 查 `Cube.prefab` 去哪 → **AbConfig**  
- 第一次解压拷哪些 → **builtInBundleInfo**  
- 要不要下载、下哪些 → **HotAssetsManifest**

### 13.3 为啥觉得乱

1. AbConfig **既有** `Assets/*.json` **又有** `*_abconfig.unity`（同一内容两种形态）  
2. Manifest **工程根一份**（给上传），**手机 persistent 又两份**（Server/Local）  
3. 命名不统一：`atest_AbConfig` / `ATestbuiltInBundleInfo` / `HotAssetsManifest` / `ServerATest...`  
4. 路径散：`Assets/`、`Resources/`、工程根 `HotAssets/`、`persistentDataPath` 根目录

### 13.4 目标收敛（改代码时按这个收）

建议物理目录（与 §10 一致）：

```
[编辑器产出 / 可上传]
{Project}/BuildOutput/Bundles/{Module}/{Platform}/   # AB + abconfig.unity
{Project}/BuildOutput/Hot/{Module}/hot_manifest.json
{Project}/BuildOutput/Hot/{Module}/{ver}/{Platform}/*.unity

[随包]
StreamingAssets/AssetBundle/{Module}/                # 内嵌 AB
Resources 仅保留极小引导 或 把 builtIn 清单也放进 StreamingAssets（少占 Resources）

[运行时可写 · 全部挂在 BundleSettings 出口下]
persistent/MmAsset/{module}/decompress/              # 解压落地
persistent/MmAsset/{module}/hot/                     # 热更 AB
persistent/MmAsset/{module}/manifest/server.json     # 原 Server*Manifest
persistent/MmAsset/{module}/manifest/local.json      # 原 Local*Manifest
```

**命名规范建议：**

| 职责 | 固定文件名 |
|------|------------|
| 地址表 AB | `{module}_abconfig.unity`（小写） |
| 内嵌清单 | `{module}_builtin.json` |
| 热更清单 | `hot_manifest.json` |
| 禁止 | 再往 `Assets/` 根扔 `{module}_AbConfig.json` 当「正式源」（最多作打 AB 的临时输入，打完可进 Library/Temp） |

### 13.5 你日常怎么认（口诀）

| 看到 | 想成 |
|------|------|
| `*AbConfig*` / `*_abconfig*` | 地址簿 |
| `*builtIn*` / `*builtin*` | 首包文件列表 |
| `*HotAssetsManifest*` / `*manifest/server|local*` | 热更版本清单 |
| `*.unity.manifest` | Unity 副作用，可忽略 |
| `BuildBundleConfigura` | 编辑器怎么打包，不是运行时 JSON |

---

## 14. CI 构建入口

Unity 命令行方法：`MmAssetCIBuild.BuildFromCommandLine`

```powershell
Unity.exe -batchmode -quit -projectPath . `
  -executeMethod MmAssetCIBuild.BuildFromCommandLine `
  -mmAssetKind hot `
  -mmAssetVersion 1.2.0 `
  -mmAssetTarget StandaloneWindows64 `
  -mmAssetUpload true
```

多平台由 CI matrix 每个平台启动一次 Unity。上传使用通用 HTTP PUT：

- `MMASSET_UPLOAD_URL`：CDN 上传根地址
- `MMASSET_UPLOAD_TOKEN`：可选 Bearer Token