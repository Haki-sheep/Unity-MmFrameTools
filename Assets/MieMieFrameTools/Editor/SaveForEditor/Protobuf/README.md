# Unity Protobuf Generator Tool

## 工具说明
集成 Protobuf 编译器到 Unity 编辑器，通过 Tools 菜单或独立窗口生成 C# 代码。

## 目录结构
```
Assets/Editor/Protobuf/
├── ProtobufMenu.cs              # Tools 菜单入口
├── ProtobufGeneratorWindow.cs   # UI Toolkit 主窗口
├── Generator/
│   └── ProtocGenerator.cs       # protoc 编译器封装
└── UI/
    ├── ProtobufGeneratorWindow.uxml  # UI 布局
    └── ProtobufGeneratorWindow.uss   # 样式
```

## 使用方法

### 方法1: Tools 菜单
- `Tools/Protobuf/Compile All` — 编译所有 proto 文件
- `Tools/Protobuf/Open Generator Window` — 打开可视化窗口
- `Tools/Protobuf/Select Protoc Path` — 选择 protoc 编译器路径

### 方法2: 可视化窗口
1. 点击 `Tools/Protobuf/Open Generator Window`
2. 配置 protoc 路径（首次需要设置）
3. 点击 "编译" 按钮

## protoc 下载
如果未设置 protoc，工具会自动提示下载：
- Windows: https://github.com/protocolbuffers/protobuf/releases/download/v25.1/protoc-25.1-win64.zip
