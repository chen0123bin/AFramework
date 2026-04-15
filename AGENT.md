# AGENT.md

本文件用于约束后续在本仓库中的代码、文档、测试与工程操作。除非用户明确覆盖，默认遵循以下规则。

---

## 1. 项目概览

- 本仓库是 Unity 工程，当前 Unity 版本为 `2022.3.62f3`。
- 当前项目由两层组成：
  - **框架层**：`Assets/LWFramework/RunTime`、`Assets/LWFramework/Editor`
  - **宿主业务层**：`Assets/Scripts`、`Assets/0Res`、`Assets/Arts`
- 当前启动入口为 [Assets/Scripts/Startup.cs](D:/UnityProject/AFramework/Assets/Scripts/Startup.cs)。
- 当前框架核心启动链为：
  - `Startup`
  - `FrameworkBootstrapper`
  - `MainManager`
  - 核心管理器注册与初始化
  - 可选模块注册
  - 热更预热
  - `StartProcedure`

---

## 2. 当前框架边界

### 2.1 核心层

以下模块视为当前框架核心能力：

- `Core`
- `Event`
- `FSM/Procedure`
- `Assets`
- `UI`
- `Hotfix`

### 2.2 宿主项目层

以下内容视为宿主项目职责：

- `Startup/Bootstrap`
- 业务 `Procedure`
- 业务 `UI/View`
- 场景、Prefab、配置表、项目资源
- 是否启用 `Audio`
- 是否启用 `StepSystem`

### 2.3 业务插件层

- `StepSystem` 视为业务插件层能力。
- `StepSystem` 不应再通过 `LWCore.ManagerUtility` 暴露公共入口。
- 当前插件访问入口应使用 [Assets/LWFramework/RunTime/StepSystem/StepManagerUtility.cs](D:/UnityProject/AFramework/Assets/LWFramework/RunTime/StepSystem/StepManagerUtility.cs)。

---

## 3. 目录与放置规则

### 3.1 运行时代码

- 框架运行时代码放在 `Assets/LWFramework/RunTime/`
- 宿主业务代码放在 `Assets/Scripts/`
- `Editor` 专用代码只能放在 `Assets/LWFramework/Editor/` 或明确的 Editor 目录下

### 3.2 测试代码

- 框架 EditMode 测试放在 `Assets/Tests/Framework/EditMode/`
- StepSystem EditMode 测试放在 `Assets/Tests/StepSystem/EditMode/`
- 新增测试优先使用独立 `asmdef`
- 测试程序集必须是 Unity Test Assembly

### 3.3 文档

- 项目文档统一使用中文
- 涉及框架接入、架构说明、模块边界的文档优先放在 `docs/`

---

## 4. 日志与注释

- 使用 `Debug` 或框架日志输出时，使用中文描述。
- 生成代码时必须添加函数级中文注释。
- 注释应说明“为什么这么做”或“这段逻辑负责什么”，避免无信息量注释。

---

## 5. 代码风格

- 避免过度防御式编程：不要为了“更健壮”而新增大量冗余判空/判状态/判边界。
- 以工程既有调用约定为前提：能保证不为 null/不越界的路径，不重复加“层层 if”。
- 只在高风险边界加必要校验：外部输入、网络/文件 IO、序列化/反序列化、反射、跨线程回调、资源加载、平台差异等。
- 当需要保护时，优先少量前置条件 + 清晰失败策略（早返回/抛异常/断言其一），避免散落的重复判断。
- 遵循现有风格：4 空格缩进，Allman 大括号。
- 修改现有代码时优先延续原有模式，不做与当前任务无关的大重构。

---

## 6. 变量类型

- 不使用 `var` 关键字，使用具体类型。

---

## 7. 命名规范

### 7.1 变量命名

- 成员变量：驼峰命名法（camelCase），首字母加 `m_`，例如 `m_Name`
- 静态变量：驼峰命名法（camelCase），首字母加 `s_`，例如 `s_Name`
- 通用变量：驼峰命名法（camelCase）
- 常量：全大写蛇形命名法（UPPER_SNAKE_CASE）
- 布尔值：前缀使用 `is` / `has` / `can` / `should`
- 集合/数组：使用复数形式

### 7.2 函数/方法命名

- 通用函数/方法：帕斯卡命名法（PascalCase）
- 命名风格：动词或动宾短语，清晰表达执行操作
- Getter / Setter：
  - Getter：`Get + 属性名`
  - Setter：`Set + 属性名`
  - 布尔 Getter：`Is + 属性名`

### 7.3 类/接口命名

- 类名：帕斯卡命名法（PascalCase）
- 接口名：帕斯卡命名法（PascalCase），并以 `I` 为前缀

---

## 8. 框架相关专用规则

### 8.1 Assets 访问规则

- 资源系统对外统一入口是 `ManagerUtility.AssetsMgr`
- 不要再新增或恢复 `LWAssetsManager.Instance` 这类旧单例访问方式
- 文档、示例、测试中的资源访问写法也要统一到 `ManagerUtility.AssetsMgr`

### 8.2 Hotfix 规则

- 默认热更路线是 `ByCode`
- `Reflection` 是可选路线
- `Reflection` 的程序集加载路径应与当前实现保持一致：
  - `Assets/0Res/RawFiles/Hotfix/*.dll.bytes`
- Reflection 的装配逻辑由 `HotFixRefManager` 负责
- 不要在未明确立项的情况下把 `HybridCLR` 重新扩成正式主路线

### 8.3 StepSystem 规则

- 不要再从 `ManagerUtility` 暴露 `StepMgr`
- StepSystem 访问统一使用 `StepManagerUtility`
- 修改 StepSystem 相关逻辑时，注意它是“业务插件”而不是“默认核心层”
- 核心层不能反向依赖 StepSystem

### 8.4 Startup 规则

- `Startup` 是宿主项目层，不是框架底层公共库
- 涉及默认行为的调整，要考虑当前 demo 场景和菜单入口是否仍然可用
- 如果改动 `Startup` 的默认开关，必须确认不会让现有音频/步骤演示流程直接失效

### 8.5 Package 规则

- 当前项目已经移除了重复的 `com.cysharp.unitask` 本地 package 依赖
- 不要再次把 `Packages/manifest.json` 改回与 `Assets/LWFramework/Plugins/UniTask` 重复的状态

---

## 9. Unity 工程注意事项

- 新增 Unity 资源文件时，要同时提交对应 `.meta`
- 新增目录时，也要注意目录级 `.meta`
- 不要把 `.vscode/settings.json` 中 worktree 自动生成的 `dotnet.defaultSolution` 改动提交进去
- `ProjectSettings/Packages/com.unity.testtools.codecoverage/Settings.json` 常被测试过程改动，非任务需要时不要提交

---

## 10. 测试与验证

### 10.1 常用验证命令

本机当前 Unity 路径可使用：

```powershell
& 'E:\Softwear\UnityEditor\2022.3.62f3\Editor\Unity.exe' -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/EditMode.xml -quit -logFile Logs/EditMode.log
```

### 10.2 当前测试策略

- 优先补 EditMode 测试
- 当前环境中 `-testResults` 生成的 XML 可能不稳定
- 在本机自动化验证时，至少确认：
  - Unity 进程退出码为 `0`
  - 日志包含成功退出标记
  - 无脚本编译错误

---

## 11. 提交与修改原则

- 提交信息优先使用中文，简短且有目的性
- 推荐格式：
  - `feat: ...`
  - `fix: ...`
  - `refactor: ...`
  - `docs: ...`
  - `chore: ...`
- 不要把无关改动和任务改动混到同一个提交中
- 遇到 worktree / 编辑器自动生成噪音，先清理再提交

---

## 12. 默认工作方式

- 分析项目时，先区分“框架层 / 宿主层 / 插件层”
- 改代码前，优先确认改动属于哪一层
- 生成文档时，优先保证和当前实现一致，不要写“未来也许会这样”的口径
- 涉及启动链、Hotfix、StepSystem、测试程序集时，先看现有实现再修改，避免把已收口的规则写回旧状态
