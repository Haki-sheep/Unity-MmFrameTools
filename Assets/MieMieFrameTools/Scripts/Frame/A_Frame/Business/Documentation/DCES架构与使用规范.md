# DCES 架构与使用规范

## 1 定位

DCES 是基于 Service Locator 的模块化轻量 Clean Architecture 并采用 Functional Core Imperative Shell 分离纯计算与副作用

对应关系

```text
整体模块结构          Modular Architecture
Service               Boundary
Executor              Use Case / Command Handler / Imperative Shell
Calculator            Domain Logic / Functional Core
Data                  Entity / State
GameHub               Service Locator
```

调用方向

```text
外部模块
    ↓
GameHub
    ↓
公共服务接口
    ↓
Service
    ↓
Executor
    ↓
Calculator
    ↓
Data
```

DCES 不完全是 DDD

- Data 偏数据容器
- Calculator 承载规则
- 属于偏贫血模型的领域设计

DCES 是职责约束 不是文件数量约束 不是每个请求都必须走完四层

DCES 最大价值是明确

```text
谁能修改数据
谁能执行副作用
谁能被其他模块看见
```

## 2 各层职责

### Data

Data 保存业务数据以及公共输入和结果

```text
Data
├── Config
├── Runtime
└── Save
```

- ConfigData 初始化后只读
- RuntimeData 是运行期间唯一业务真值 不向模块外暴露
- SaveData 只是序列化快照 加载后转换成 RuntimeData
- Request 和 Result 不是必选项
- 简单参数直接写在接口方法上
- 只有入参或回执明显变多时才在 Public/Data 增加 DTO
- 外部只能获得只读结果 DTO
- 数据修改必须由负责该业务的 Executor 完成
- 内部 Data 不允许被外部模块直接取得或修改

### Calculator

Calculator 按业务行为拆分纯计算 分为静态与实例两种形态

```text
静态 Calculator
├── 无字段
├── 入参全走方法参数
└── 适合公式型计算

实例 Calculator
├── 构造注入只读依赖 Config 表 策略
├── 构造后不可变
└── 适合反复使用同一套配置
```

选用

| 情况 | 选 |
|------|----|
| 纯公式 参数少 | 静态 |
| 依赖 Config / 数值表 / 公式策略 | 实例 由 Service 组装后注入 Executor |
| 函数签名开始堆 config table formula | 改为实例字段 |
| 需要可变缓存或未注入随机源 | 不算 Calculator 放到 Executor |

允许

- 读取方法参数
- 读取构造注入的不可变配置或只读快照
- 返回计算结果
- 静态 Calculator 使用无状态静态方法
- 实例 Calculator 调用同模块静态纯函数复用公式

禁止

- 修改 RuntimeData
- 调用 GameHub
- 发布事件
- 保存存档
- 访问场景对象或 UI
- 读取 Unity 时间和未注入的随机数
- 在实例 Calculator 中保存可变业务状态

相同输入必须产生相同结果

不要把大量无边界业务规则堆进巨型静态工具类

- 发现成本上升
- 参数列表膨胀
- 难以替换计算策略与单测替身
- 性能通常不是问题 可读性与可替换性才是

### Executor

Executor 是写入和副作用边界

允许

- 调用 Calculator
- 修改 RuntimeData
- 调用存档端口
- 发布事件
- 请求表现
- 返回明确的业务结果

禁止

- 向模块外暴露自身
- 重复实现 Calculator 已经负责的计算规则
- 默认直接调用同级 Executor
- 在深层代码中临时调用 GameHub 获取隐藏依赖

推荐执行顺序

```text
Calculator 生成结果
Executor 验证结果
Executor 一次性应用 RuntimeData
Executor 保存
Executor 发布事件和表现
```

复杂业务应返回明确 Result 不要只依赖异常或 bool

多个同级 Executor 需要共同完成一个流程时才增加 FlowExecutor

```text
BattleFlowExecutor
├── CombatExecutor
├── RewardExecutor
└── GrowthExecutor
```

### Service

Service 是模块公共入口 实现公共接口并组装模块内部对象

允许

- 持有 RuntimeData 和 Executor
- 将公共请求转发给对应 Executor
- 提供稳定查询
- 将内部结果转换成接口返回值或事件参数

禁止

- 把 Executor Calculator 或内部 RuntimeData 返回给外部
- 在 Service 中复制复杂业务计算
- 让其他模块直接依赖具体 Service 实现类
- 用 Action 回调向 Executor 注入跨模块通知出口
- 在公共服务接口上堆 C# event 作为跨模块广播

跨模块通信只保留两条蓝海路径

```text
命令 / 查询
外部 → GameHub → IXxxService

状态通知
模块 Executor → MmGlobalEventBus.Publish(XxxEvents.Key)
外部 → MmGlobalEventBus.Subscribe(XxxEvents.Key)
```

Public/Interface 定义能做什么
Public/Event 定义会发生什么
不要把通知做成构造函数里的 Action 委托

## 3 查询和命令路径

命令写入使用完整链路

```text
Service → Executor → Calculator → RuntimeData
```

查询读取允许短路径

```text
Service → Calculator → Data
Service → ReadOnly RuntimeData
```

读取一个属性时不需要创建空壳 Executor

DCES 表示模块的四种职责 不应该理解为每个请求必须走完四层

## 4 GameHub 规则

`GameHub` 是单脚本服务注册表 内部就是一张接口到实例的字典

`IGameService` 与 `GameHub` 放在同一文件

正确注册

```csharp
GameHub.Register<IPlayerService>(PlayerService);
```

错误注册

```csharp
GameHub.Register(PlayerService);
GameHub.Register<PlayerService>(PlayerService);
GameHub.Register<IGameService>(PlayerService);
```

注册键必须是具体模块的公共接口

同接口重复注册会覆盖旧实例

必需依赖使用 `Get`

```csharp
IPlayerService PlayerService = GameHub.Get<IPlayerService>();
```

服务未注册时 `Get` 返回 null 调用方应视为组装错误

可选依赖使用 `TryGet`

```csharp
if (GameHub.TryGet(out IPhotoModeService PhotoModeService))
{
    PhotoModeService.Open();
}
```

模块销毁时注销

```csharp
private void OnDestroy()
{
    GameHub.Unregister<IPlayerService>();
}
```

GameHub 面向 Unity 主线程使用 不作为多线程依赖注入容器

高频逻辑不要每帧反复 `Get` 应获取一次后缓存

## 5 跨模块依赖

GameHub 只能用于模块边界和组装入口

推荐位置

- Bootstrap
- Service 组装入口
- 跨模块 FlowService
- UI 或 Gameplay 的外部消费入口

禁止位置

- Data
- Calculator
- 普通业务实体
- 普通 Executor 内部临时获取
- 高频 Update 内重复获取服务

Executor 依赖其他模块时由上层获取公共接口后通过构造函数注入

```csharp
IInventoryService InventoryService = GameHub.Get<IInventoryService>();
var RewardExecutor = new RewardExecutor(InventoryService);
```

跨模块复杂流程放入独立 FlowService 避免模块间循环依赖

```text
BattleFlowService
├── IPlayerService
├── IInventoryService
└── IRewardService
```

FlowService 只能调用其他模块的公共服务接口 不访问模块内部 Executor

## 6 按需裁剪

推荐目录

```text
Player
├── Public
│   ├── Interface
│   │   └── IPlayerService.cs
│   └── Event
│       └── PlayerEvents.cs
├── Data
│   ├── Config
│   ├── Runtime
│   └── Save
├── Calculator
├── Executor
└── PlayerService.cs
```

简单入参直接写在接口方法上 不要为单个 int 再造 Request Result

参数或回执明显变多时再按需增加 `Public/Data` 下的 DTO

目录按实际职责创建

- 没有配置时不创建 Config
- 没有存档时不创建 Save
- 没有计算规则时不创建 Calculator
- 没有复合流程时不创建 FlowExecutor
- Data 类型不要为了满足目录结构而创建空类
- 只有一个简单 Executor 时不需要额外 FlowExecutor

简单模块不要为了四层齐全而制造类爆炸

## 7 主要缺陷与规避

### 隐藏依赖

任何类都能写 `GameHub.Get` 会导致构造函数看不出真实依赖

规避 限制 GameHub 出现范围 Calculator 和 Data 禁止访问 普通 Executor 通过构造函数接收依赖

### 强制四层产生空壳

读取也创建 Executor 会产生大量无意义代码

规避 查询允许短路径 命令才走完整写入链路

### Data 退化成公共数据袋

多个 Executor 随意修改同一 RuntimeData 会使约束散落

规避 RuntimeData 模块私有 外部只通过接口返回值或 Event 参数取结果 修改由对应业务 Executor 完成

### 简单模块类爆炸

两个方法的小模块仍创建全套四层会得不偿失

规避 按职责裁剪 不为目录外形创建空类

### 跨模块循环依赖

模块 Service 互相 `Get` 会造成初始化顺序和递归问题

规避 复杂跨模块流程独立成 FlowService 只编排公共接口

### 副作用中途失败

先改数据再播表现再存档再发事件 中途失败会出现半完成状态

规避 先计算再验证再一次性写 RuntimeData 再保存 最后发布事件和表现 复杂业务返回明确 Result

## 8 最终约束

```text
Service 是模块公共入口
Executor 是写入和副作用边界
Calculator 是纯计算核心
Data 是模块私有状态
GameHub 只定位公共 Service
跨模块命令走 Interface
跨模块通知走 EventKey
每个请求不强制走完所有层
跨模块复杂流程由独立 FlowService 编排
```

## 9 示例说明

示例位于 `Business/Samples/Player`

示例通过独立的 `MieMieFrameWork.DCES.Samples` 程序集与框架运行时代码隔离

示例业务是玩家受到伤害 同时演示静态与实例 Calculator

```text
PlayerDcesSampleBootstrap
    ↓ 创建 PlayerConfigData
PlayerService
    ↓ 组装 PlayerDamageCalculator(config)
PlayerDamageExecutor
    ↓
PlayerDamageCalculator.Calculate          ← 实例 持有只读 Config
    ↓
PlayerDamageMath.ApplyReduction           ← 静态 纯公式
PlayerDamageMath.ResolveHealth            ← 静态 纯公式
    ↓
PlayerRuntimeData.SetHealth
    ↓
MmGlobalEventBus.Publish(PlayerEvents.HealthChanged)
    ↓
PlayerDcesSampleConsumer 订阅消费
```

对应文件

```text
Public/Interface/IPlayerService.cs        跨模块命令入口
Public/Event/PlayerEvents.cs              跨模块通知 Key
Data/Config/PlayerConfigData.cs           只读配置
Calculator/PlayerDamageMath.cs            静态纯函数
Calculator/PlayerDamageCalculator.cs      实例计算器
Executor/PlayerDamageExecutor.cs          写状态并发布事件
```

运行步骤

1. 在场景物体上挂载 `PlayerDcesSampleBootstrap`
2. 在 Inspector 调整 `Initial Health` 与 `Damage Reduction Percent`
3. 在另一个场景物体上挂载 `PlayerDcesSampleConsumer`
4. 进入 Play Mode
5. 在 Consumer 组件菜单执行 `DCES Sample Take Damage`
6. Console 会显示减免后的实际伤害 剩余生命和死亡状态

默认减免 `20%` 请求伤害 `25` 时实际伤害约为 `20`

Bootstrap 在 Awake 创建并注册玩家服务

Consumer 在 Start 获取一次公共接口并订阅 `PlayerEvents.HealthChanged`

Bootstrap 销毁时 `Unregister` 玩家服务

Consumer 销毁时释放事件订阅令牌

## 10 代码审查清单

提交新的 DCES 模块前检查

- 公共接口是否继承 IGameService
- GameHub 是否按公共接口类型注册
- 外部是否只依赖公共接口和公共 EventKey
- 简单参数是否直接写在接口上 没有多余 DTO
- 跨模块通知是否走 Public/Event 而不是 Action 回调或接口上的 C# event
- RuntimeData 是否仍然是模块私有
- Calculator 是否保持纯计算
- 静态与实例 Calculator 选用是否合理
- 实例 Calculator 是否只持有只读依赖
- Executor 是否承担全部状态修改和副作用
- Executor 是否按 计算 验证 写状态 保存 副作用 的顺序执行
- Service 是否只负责组装 转发和稳定查询
- 模块销毁时是否 Unregister 对应服务
- EventBus 订阅是否存在对应 Dispose
- 高频路径是否缓存服务引用
- 跨模块流程是否只调用公共 Service
- 是否创建了没有实际职责的空层
)
