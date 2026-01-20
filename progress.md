# StepManager 会话进度

## 2026-01-20
- 已定位 LWFramework Manager 启动注册入口：Assets/Scripts/Startup.cs
- 已确认 Manager 生命周期与驱动方式：MainManager.AddManager 立即 Init，MainManager.Update 每帧轮询 Update
- 已确认热更域类型发现模式：HotFixBaseManager 维护“特性→类型列表”，FSMManager 通过 HotfixMgr.GetAttrTypeDataList<T>() 获取
- 已根据需求重写 StepManager 计划：XML 数据驱动、DAG、前进/后退/跳转与过程补齐
- 已修正范围：StepManager 与 LWFMS 独立，并纳入 Editor 可视化编辑器规划
- 已新增 StepManager 生命周期回调接口（StepChanged/StepStart/StepEnd/AllStepsCompleted）并同步实现
- 已新增最小步骤 XML 与 StepDemoRunner 场景验证脚本，并接入 Startup 场景
- 已核对阶段2验收：新建/导入/导出/节点连线编辑已具备，校验仍缺不可达节点与孤立节点提示
- 已补齐不可达/孤立节点校验并复核阶段2验收通过
- 已完成阶段3：StepContext 结构化与类型安全读写、Apply 策略与跳转失败原因事件、事件顺序稳定化
