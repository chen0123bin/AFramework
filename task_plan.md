# StepManager（LWFramework）任务计划

## 目标
- 在 LWFramework 中新增一套面向“虚拟仿真教学”的步骤管理系统 StepManager。
- 采用 XML 数据驱动：步骤、动作、连线（DAG）均由 XML 配置定义，运行时解析执行。
- 支持快速操作：前进、后退、跳转。
- 跳转需“补齐过程结果”：例如从步骤1跳到步骤4，需要按规则处理步骤2、步骤3带来的结果。
- 行为可扩展：步骤由多个“动作（Action）”组成，动作通过继承扩展（等待用户操作/控制对象移动/显隐/到达检测/播放音频/播放视频等）。
- 支持非线性流程：全流程为有向无环图（DAG），允许分支与汇合。
- StepManager 与现有 LWFMS（Procedure/FSM）是两套独立系统，互不依赖、互不替代。
- 与现有 Manager 体系一致：通过 Startup 手动注册到 MainManager，并通过 ManagerUtility 快速访问。
- 提供 Editor 可视化编辑器：支持图编辑（节点/连线/属性/动作参数）、校验、预览与导出 XML。

## 非目标（本次不做）
- 不在本阶段把 LWFMS（Procedure/FSM）改造成 StepManager 的适配层。
- 不在本阶段引入复杂脚本热重载/运行时编辑 XML。

## 约束与既有架构对齐
- Manager 体系：MainManager 通过 AddManager 手动注册（无自动扫描注册）。
- 生命周期：IManager 仅包含 Init/Update，Update 由 MainManager 每帧轮询驱动。
- 数据驱动：StepManager 不依赖特性扫描发现 Step，核心输入为 XML。
- 解析与性能：XML 解析需控制 GC（避免 LINQ to XML 热路径；优先 XmlReader/XmlDocument 解析一次并缓存运行时图）。
- 代码规范：不使用 var；成员变量 m_ 前缀；方法 PascalCase；布尔命名 is/has/can/should 前缀。

## 与 LWFMS 的关系
- LWFMS（Procedure/FSM）聚焦“游戏/应用顶层流程状态机”。
- StepManager 聚焦“教学步骤与步骤行为编排”（数据驱动 + DAG + 过程补齐）。
- 两者可并存：Procedure 可以选择性地启动/停止 StepManager，但 StepManager 不依赖 Procedure 才能运行。

## 设计概述（拟定）
### 核心概念
- StepGraph：由 XML 定义的 DAG（节点=步骤，边=跳转/前进路线），包含校验与查询能力。
- StepNode（步骤节点）：图中的一个节点，包含步骤元数据与动作列表。
- StepEdge（有向边）：节点间连线，可能带条件与优先级。
- StepAction（动作）：步骤内的可执行行为单元，通过继承扩展。
- StepContext：运行时上下文（教学过程的状态与结果集合），动作读写它以产出“过程结果”。
- StepHistory：历史栈/轨迹，用于后退与回放。

### 执行模型
- 执行方式：StepManager 维护当前节点，驱动当前步骤内动作的进入/更新/退出。
- 前进：根据当前节点出边选择下一节点（默认边/最高优先级边/满足条件边）。
- 后退：从 StepHistory 回退到上一个访问节点，并按回退策略处理结果（见“结果策略”）。
- 跳转：从当前节点跳转到目标节点时，需要先计算一条“可达路径”，再对路径上的中间节点做“过程补齐”。

### 结果策略（跳转与后退的关键）
- 过程补齐：跳转时对中间节点执行“快速应用（Apply）模式”，确保这些节点的动作结果写入 StepContext。
- 交互动作处理：某些动作依赖用户交互（等待点击/输入）。在 Apply 模式下需要可配置策略：
  - 跳过并写入默认结果
  - 自动通过（按预设答案/参数）
  - 直接阻止跳转并返回失败原因
- 回退一致性：后退时可配置结果回滚策略：
  - 不回滚（仅回退展示/指引，保持已发生结果）
  - 回滚到快照（进入节点前保存快照，回退时恢复）
  - 事件反向执行（每个动作提供可选 Undo）

### 关键能力（v1）
- XML 加载：从指定路径加载 XML，解析为 StepGraph，并在加载阶段校验 DAG（无环、节点唯一、边合法）。
- 运行控制：Start(graphId/startNodeId)、Stop、Restart、ResetContext。
- 导航能力：Forward、Back、JumpTo(targetNodeId)。
- 跳转补齐：JumpTo 会计算路径并依次对中间节点执行 Apply 模式。
- 查询能力：当前节点、当前动作、是否运行中、可前进节点集合、历史轨迹。
- 事件回调：OnNodeEnter/OnNodeLeave/OnNodeChanged/OnActionChanged/OnJumpProgress/OnAllStepsCompleted。

### 扩展能力（v2，纳入阶段 3）
- 异步动作：动作支持 UniTask，适配资源加载/网络等待/视频准备等，并可取消。
- 条件与决策：边支持条件表达式（从 StepContext 读取）与优先级决策。
- 多路径跳转：当存在多条可达路径时，支持基于规则选择路径（最短/优先级/标签）。
- 持久化：StepContext 序列化与恢复（教学断点续学）。

## XML 数据格式（拟定）
> 以“可扩展、可版本演进”为原则，动作以 type + params 表达。

- graph
  - id/version/start
- nodes/node
  - id/name
  - actions/action
    - type（对应动作类名/注册名）
    - params（键值参数）
- edges/edge
  - from/to
  - priority
  - condition（可选）
  - tags（可选，用于跳转选路）

验收点：
- 能从 XML 构建 DAG 并校验无环。
- 动作参数可扩展（新增动作不破坏老配置）。

## 文件落点（拟定）
> 以“最少侵入现有架构”为原则，沿用 InterfaceManager + 手动注册模式。

- 新增接口（对外能力）：
  - Assets/LWFramework/RunTime/Core/InterfaceManager/IStepManager.cs
- 新增 Step 系统实现（建议新目录）：
  - Assets/LWFramework/RunTime/StepSystem/StepManager.cs
  - Assets/LWFramework/RunTime/StepSystem/Graph/StepGraph.cs
  - Assets/LWFramework/RunTime/StepSystem/Graph/StepNode.cs
  - Assets/LWFramework/RunTime/StepSystem/Graph/StepEdge.cs
  - Assets/LWFramework/RunTime/StepSystem/Context/StepContext.cs
  - Assets/LWFramework/RunTime/StepSystem/Context/StepContextSnapshot.cs
  - Assets/LWFramework/RunTime/StepSystem/Action/BaseStepAction.cs
  - Assets/LWFramework/RunTime/StepSystem/Action/StepActionFactory.cs
  - Assets/LWFramework/RunTime/StepSystem/IO/StepXmlLoader.cs
- 新增 Editor 可视化编辑器（建议新目录）：
  - Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs
  - Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs
  - Assets/LWFramework/Editor/StepSystem/GraphView/StepNodeView.cs
  - Assets/LWFramework/Editor/StepSystem/GraphView/StepEdgeView.cs
  - Assets/LWFramework/Editor/StepSystem/Inspector/StepNodeInspector.cs
  - Assets/LWFramework/Editor/StepSystem/IO/StepXmlExporter.cs
  - Assets/LWFramework/Editor/StepSystem/IO/StepXmlImporter.cs
- 现有文件修改：
  - Assets/LWFramework/RunTime/Core/ManagerUtility.cs（新增 StepMgr 快捷访问）
  - Assets/Scripts/Startup.cs（新增 StepManager 的 AddManager 注册）

## 分阶段实现计划与验收
### 阶段 1：最小可用（MVP）
- 实现 XML Loader（解析 graph/nodes/edges/actions），构建 StepGraph 并做 DAG 校验。
- 实现 StepManager（Init/Update 驱动），支持 Start/Stop/Forward/Back/JumpTo 的基础流程。
- 实现 StepAction 继承体系与工厂（按 type 创建动作实例）。
- 在 Startup.cs 注册 StepManager，在 ManagerUtility 暴露 StepMgr。

验收点：
- 能从 XML 启动一个 DAG 流程，并可前进/后退。
- JumpTo 能按路径对中间节点执行 Apply 模式并产出结果。
- 可扩展动作：新增一个动作类，无需改 StepManager 核心逻辑即可被 XML 引用执行。
- Update 过程中无明显 GC 分配（不使用 LINQ/闭包/字符串拼接热路径）。

阶段一验收核对（2026-01-20）：
- 能从 XML 启动一个 DAG 流程，并可前进/后退：已完成最小 XML 与场景脚本接入，待 PlayMode 日志验证。
- JumpTo 能按路径对中间节点执行 Apply 模式并产出结果：已实现。
- 可扩展动作：新增一个动作类，无需改 StepManager 核心逻辑即可被 XML 引用执行：已实现。
- Update 过程中无明显 GC 分配（不使用 LINQ/闭包/字符串拼接热路径）：已实现代码约束，待 Profiler 验证。

### 阶段 2：Editor 可视化编辑器（MVP）
- 提供 StepEditorWindow，可视化编辑 DAG（增删节点、拖拽连线、设置 start 节点）。
- 提供节点属性面板：节点基础信息、动作列表、动作参数编辑。
- 提供校验面板：无环校验、缺失节点、不可达节点、孤立节点、重复 id。
- 支持导出/导入 XML，并确保导出的 XML 可被运行时正确加载。

验收点：
- 能在编辑器内从零搭建一个可运行的 DAG 并导出 XML。
- 能导入 XML 并无损还原图结构与参数。
- 校验信息清晰可定位（能指向具体节点/边/字段）。

阶段二验收核对（2026-01-20）：
- 能在编辑器内从零搭建一个可运行的 DAG 并导出 XML：已实现。
- 能导入 XML 并无损还原图结构与参数：已实现。
- 校验信息清晰可定位（能指向具体节点/边/字段）：已实现。

### 阶段 3：运行时使用体验增强
- 完善 StepContext（结构化 key、类型安全读写、可选快照）。
- 完善跳转策略（交互动作在 Apply 模式下的可配置策略）。
- 增加事件与观测（节点/动作切换、跳转进度、失败原因）。

验收点：
- Step 之间可通过 StepContext 共享数据。
- 跳转/后退在结果策略上行为一致且可预测。
- 事件回调顺序稳定（Leave→Enter、Action 切换顺序固定）。

阶段三验收核对（2026-01-20）：
- Step 之间可通过 StepContext 共享数据：已实现。
- 跳转/后退在结果策略上行为一致且可预测：已实现（Apply 策略可配置并在跳转补齐中生效）。
- 事件回调顺序稳定（Leave→Enter、Action 切换顺序固定）：已实现。

### 阶段 4：V2 扩展能力（异步/条件/多路径/持久化）
- 支持动作异步执行（UniTask），并且可取消（Stop/JumpTo 中断正在执行的动作）。
- 支持条件边与选路决策（基于 StepContext 条件 + priority + tag）。
- 支持多路径 JumpTo 的路径选择策略（最短/优先级/tag），并在歧义时返回明确错误。
- 支持 StepContext 持久化与恢复（用于断点续学）。

验收点：
- 异步动作不会阻塞主线程，取消可控且无资源泄漏。
- 条件边选路结果稳定且可解释（能输出选择原因/命中条件）。
- JumpTo 在存在多条可达路径时行为符合预期策略。
- 支持中途 Stop/Reset，不遗留未完成任务，恢复后可继续执行。

### 阶段 5：Editor 增强（对齐 V2）
- 支持条件边编辑（表达式/优先级/tag 的可视化配置与校验）。
- 支持动作异步/交互策略配置（Apply 模式策略、默认值、自动通过参数）。
- 支持运行时预览联调：在 Editor 中选择一个 XML，启动 PlayMode 后自动加载并定位到指定节点。

验收点：
- Editor 中配置的条件/策略可正确序列化到 XML，并被运行时识别。
- 能在编辑器侧快速复现“跳转补齐”路径与结果策略配置。

## 风险与对策
- XML 配置错误成本高：加载阶段强校验（无环、缺失节点、孤立节点、不可达节点、重复 id），并提供可读错误信息。
- 跳转路径多解：提供明确的选路策略（最短/优先级/tag），并在歧义时返回错误而非静默选择。
- 交互动作在 Apply 模式下不可执行：通过策略化处理（默认值/自动通过/阻止跳转）。
- Update 切换重入：StepManager 内部用状态机保护（切换请求排队到帧末执行）。
- Editor Graph 编辑复杂度高：先做 MVP（节点/边/属性/导入导出/校验），再逐步增强。
