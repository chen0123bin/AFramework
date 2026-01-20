# StepManager 调研记录（LWFramework）

## 现有 Manager 体系结论
- MainManager 负责持有并驱动所有 IManager：AddManager 会立即调用 Init，并把实例加入字典与列表；Update 每帧轮询调用每个 Manager.Update。
- Manager 的注册方式为“启动脚本手动 AddManager”，项目当前实际使用的启动脚本为 Assets/Scripts/Startup.cs。
- ManagerUtility 提供强类型快捷访问（MainMgr/AssetsMgr/EventMgr/UIMgr/HotfixMgr/FSMMgr/AudioMgr），内部通过 MainManager.GetManager<T>() 获取。

## 现有状态机（LWFMS）可复用模式
- FSMManager 的类型发现依赖 HotfixMgr.GetAttrTypeDataList<FSMTypeAttribute>()，由热更管理器维护“特性→类型列表”的字典。
- FSMStateMachine 构造时接收 classDataList，并用 HotfixMgr.Instantiate<BaseFSMState>(type.ToString()) 创建状态实例。
- BaseFSMState 提供 OnInit/OnEnter/OnLeave/OnUpdate/OnTermination 生命周期，适合作为 Step 的设计参考。

## StepManager 新方案要点
- StepManager 改为 XML 数据驱动，不再依赖 StepTypeAttribute 等特性扫描。
- 非线性流程采用 DAG：节点=步骤，边=跳转/前进路线，加载阶段强校验无环与合法性。
- 跳转需要“过程补齐”：从当前节点到目标节点计算路径，并对中间节点执行 Apply 模式以产出结果。
- 动作体系采用继承扩展：XML 用 type + params 描述动作，运行时通过 FullName 反射实例化具体动作类。
- StepManager 与 LWFMS（Procedure/FSM）是两套独立系统，互不依赖，可并存协作。
- 新增 Editor 可视化编辑器：提供 DAG 图编辑、校验、导入导出 XML 与运行时联调预览。
- 保留关键回调：OnNodeEnter/OnNodeLeave/OnNodeChanged/OnActionChanged/OnJumpProgress/OnAllStepsCompleted。

## 关键代码参考
- MainManager：Assets/LWFramework/RunTime/Core/MainManager.cs
- ManagerUtility：Assets/LWFramework/RunTime/Core/ManagerUtility.cs
- 启动脚本：Assets/Scripts/Startup.cs
- FSMManager / FSMStateMachine：Assets/LWFramework/RunTime/FMS/
- HotFixBaseManager：Assets/LWFramework/RunTime/HotFix/HotFixBaseManager.cs

## 与模板差异
- Editor/Resources/Template 中出现的 [ManagerClass]/[ManagerHotfixClass] 在当前工程未找到定义与实际用法；推断当前版本不依赖“特性扫描自动注册 Manager”，仍以 Startup 手动注册为准。

## 2026-01-20 补充：移除 ApplyWithStrategy 策略跳转
- 背景：策略跳转会让 JumpTo/Backward/Forward 的补齐链路引入“失败原因+分支策略”的复杂度，维护成本高。
- 决策：彻底移除 StepApplyStrategy/IStepInteractiveAction/ApplyWithStrategy，补齐阶段统一只调用 Apply。
- 行为变化：交互类动作在补齐阶段直接 Finish，不再阻塞跳转。
- 影响面：StepNode/StepManager/IStepManager/StepWaitMouseLeftClickAction 的带策略 API 全部删除或改为直接 Apply。
