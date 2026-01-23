# StepSystem 总结与使用说明

本文基于现有 StepSystem（运行时 + 编辑器）代码与项目内计划/进度记录整理：
- 计划： [task_plan.md](file:///d:/UnityProject/AFramework/task_plan.md)
- 会话进度： [progress.md](file:///d:/UnityProject/AFramework/progress.md)

## 1. 系统定位

StepSystem（StepManager）面向“虚拟仿真教学步骤编排”场景：
- **数据驱动**：使用 XML 描述步骤图（DAG）、节点动作（Action）与连线（Edge）。
- **可扩展**：通过新增 Action 类扩展行为，XML 通过 type + param 引用。
- **可导航**：前进（Forward）、后退（Backward）、跳转（JumpTo），并在跳转时对中间节点进行 Apply 补齐。
- **可观察**：提供节点/动作切换、跳转进度、完成等事件回调。

StepSystem 与 LWFMS（Procedure/FSM）独立，可并存协作。

## 2. 目录与关键文件

### 2.1 编辑器（图编辑器）

位置：`Assets/LWFramework/Editor/StepSystem/`

- 主窗口： [StepEditorWindow.cs](file:///d:/UnityProject/AFramework/Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs)
- GraphView 视图层：
  - [StepGraphView.cs](file:///d:/UnityProject/AFramework/Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs)
  - [StepNodeView.cs](file:///d:/UnityProject/AFramework/Assets/LWFramework/Editor/StepSystem/GraphView/StepNodeView.cs)
- 编辑器数据结构（可 JsonUtility 序列化）： [StepEditorGraphData.cs](file:///d:/UnityProject/AFramework/Assets/LWFramework/Editor/StepSystem/StepEditorGraphData.cs)
- XML 导入/导出：
  - [StepXmlImporter.cs](file:///d:/UnityProject/AFramework/Assets/LWFramework/Editor/StepSystem/StepXmlImporter.cs)
  - [StepXmlExporter.cs](file:///d:/UnityProject/AFramework/Assets/LWFramework/Editor/StepSystem/StepXmlExporter.cs)

### 2.2 运行时（加载、执行、上下文）

位置：`Assets/LWFramework/RunTime/StepSystem/`

- 管理器：`StepManager`（通过 `ManagerUtility.StepMgr` 访问）
- XML 加载：`StepXmlLoader`
- DAG：`StepGraph/StepNode/StepEdge`
- 上下文：`StepContext`（保存/恢复、条件选路依据）
- Action：`BaseStepAction` 及具体动作实现（例如 Log/Move/Toggle/PlayAudio/WaitClick 等）

## 3. 编辑器使用说明（Step Editor）

### 3.1 打开窗口

菜单：`LWFramework/Step/Step Editor`

### 3.2 基本编辑

1) **新增节点**
- 在空白处右键，选择“新增节点”。

2) **连线**
- 鼠标点击节点输出端口（Out）→ 再点击目标节点输入端口（In）创建连线。
- 选中连线后，可在右侧属性面板编辑：优先级、条件、标签。

3) **设置开始节点**
- 选中节点后，右侧点击“设为开始节点”。

4) **编辑动作与参数**
- 选中节点后，在“动作”区域配置动作：
  - 类型：动作类的完整类型名（例如 `LWStep.StepLogAction`）。
  - 参数：键值对（写入 XML 的 `<param key="..." value="..." />`）。

5) **导入/导出 XML**
- 顶部工具栏：导入XML / 导出XML。

6) **校验**
- 顶部工具栏“校验”会检查：图 ID、节点 ID 重复/为空、开始节点合法性、连线条件/标签格式、DAG 无环、不可达节点、孤立节点。

### 3.3 运行时联调预览

右侧“运行时预览”区域：
- 选择一个 XML 作为预览输入
- 配置图 ID / 开始节点 / 定位节点 / 定位标签
- 点击“进入PlayMode预览”后：
  - PlayMode 中的示例驱动脚本会读取 EditorPrefs 并按配置启动
  - 编辑器窗口会高亮当前运行节点（标题带“运行中”标识）
- 选中节点后，可点击“跳转到此节点”（仅 PlayMode 且 StepManager 运行中有效）

## 4. XML 格式约定

核心结构：

```xml
<graph id="your_graph_id" start="start_node_id">
  <nodes>
    <node id="node_id" name="显示名" x="0.00" y="0.00">
      <actions>
        <action type="LWStep.StepLogAction">
          <param key="message" value="日志内容" />
        </action>
      </actions>
    </node>
  </nodes>
  <edges>
    <edge from="a" to="b" priority="10" condition="mode == A" tag="vip" />
  </edges>
</graph>
```

字段说明：
- `graph.id`：图 ID
- `graph.start`：开始节点 ID（可为空，但运行时 Start 通常需要可用起点）
- `node.x/y`：编辑器布局位置（导入导出会保留）
- `edge.priority`：出边候选排序依据（越大优先级越高）
- `edge.tag`：标签选路过滤（Forward/JumpTo 可指定 requiredTag）
- `edge.condition`：条件表达式（基于 StepContext 的简单判断）

条件表达式支持（当前实现）：
- `key == value`
- `key != value`
- `key`（truthy 判断：bool true / number 非 0 / string 非空）

## 5. 运行时接入（最小用法）

### 5.1 注册与访问

项目采用 Manager 体系手动注册（由 Startup 执行 AddManager）。运行时通过：

```csharp
IStepManager stepManager = ManagerUtility.StepMgr;
```

### 5.2 加载与启动

```csharp
stepManager.LoadGraph("Assets/0Res/RawFiles/StepStage4Test.xml");
stepManager.Start("step_stage4_demo");
```

### 5.3 导航

- 前进：`stepManager.Forward()` 或 `stepManager.Forward(requiredTag)`
- 后退：`stepManager.Backward()`
- 跳转：`stepManager.JumpTo(targetNodeId)` 或 `stepManager.JumpTo(targetNodeId, requiredTag)`

### 5.4 上下文保存/恢复

- 保存：`string json = stepManager.SaveContextToJson();`
- 恢复：`stepManager.LoadContextFromJson(json);`

## 6. 计划进度摘要（对照）

按计划文件分阶段核对：
- 阶段 1（运行时 MVP）：已完成（XML 加载、DAG、Forward/Backward/JumpTo + Apply 补齐、Action 扩展、Manager 接入）。
- 阶段 2（Editor MVP）：已完成（图编辑、导入导出、校验、属性面板）。
- 阶段 3（体验增强）：已完成（StepContext、事件观测、稳定事件顺序）；并按调整移除了策略跳转（补齐阶段统一 Apply）。
- 阶段 4（条件/标签/优先级/持久化）：已完成验证样例（Stage4Test + DemoRunner）。
- 阶段 5（Editor 对齐增强）：已实现条件/标签/优先级编辑；运行时预览联调能力可用。

## 7. 已知约束与建议

- 条件表达式是“轻量解析”，建议保持表达式简单、避免复杂嵌套。
- 标签要求无前后空格；否则校验会提示。
- 跳转补齐阶段统一执行 Apply：交互类动作不会阻塞跳转（这是当前设计决策）。
- 运行时预览依赖 EditorPrefs 与 DemoRunner 读取逻辑，建议在团队内统一约定流程入口场景。

## 8. Node/Action 执行逻辑与扩展规范

### 8.1 Node 执行流程（串行/并行）

StepNode 现支持两种执行模式：
- 串行模式（Serial）：按 Action 列表顺序逐个执行
- 并行模式（Parallel）：所有 Action 同时 Enter、并行 Update，全部完成后结束

模式来源与配置：
- XML：在 node 上配置 `mode="parallel"` 即并行；不写为串行默认
- 编辑器：节点属性面板“执行模式”下拉

执行时序规则：
- Enter：绑定上下文 → Reset 所有 Action
  - Serial：只 Enter 第一个 Action
  - Parallel：对全部 Action 依次 Enter
- Update：
  - Serial：仅 Update 当前 Action，完成后 Exit 并切下一个
  - Parallel：对所有未完成 Action Update，完成后立即 Exit
- 完成判定：
  - Serial：当前索引 >= Actions.Count
  - Parallel：所有 Action IsFinished
- Leave：
  - Serial：仅 Exit 当前 Action
  - Parallel：Exit 所有未退出的 Action
- Apply：对所有 Action 执行 Apply 并标记完成
- ApplyRemaining：
  - Serial：从当前索引到末尾 Apply
  - Parallel：对未完成的 Action Apply

### 8.2 Action 生命周期约束

Action 需遵守 BaseStepAction 生命周期：
- OnEnter：初始化与启动
- OnUpdate：推进逻辑，满足条件后调用 Finish
- OnExit：释放或收尾
- OnApply：用于跳转补齐，必须无阻塞并快速完成

并行模式的关键要求：
- OnUpdate 必须可多帧执行，不要依赖只调用一次
- OnApply 必须保证不会阻塞，否则 Jump/Forward 会卡死
- OnExit 应避免重复副作用（Exit 可能在并行节点中被多动作触发）

### 8.3 现有 Action 适配结论

 - StepLogAction：一次性输出日志，Enter 即完成，适配良好
 - StepMoveObjectAction：多帧位移（DoTween），等待完成，适配良好
- StepToggleObjectAction：一次性显隐，Enter 即完成，适配良好
- StepWaitMouseLeftClickAction：多帧等待输入，适配良好
- StepPlayAudioAction：默认即播即完成，已支持可选等待播放结束

StepPlayAudioAction 新增参数：
- `waitForFinish`（bool，默认 false）
  - false：保持旧行为，Enter 后立即完成
  - true：当 `isLoop=false` 时等待音频播放结束再完成
  - 若 `isLoop=true`，强制转为非阻塞，避免永久等待

### 8.4 新增 Action 设计清单

新增 Action 建议遵循以下清单：
- 明确是否为“瞬时动作”或“多帧动作”
- OnEnter 仅做启动，真正的完成判定放在 OnUpdate
- OnApply 必须不阻塞，并能快速写入必要状态
- 若涉及外部资源（音频/特效/对象池），Exit 中应考虑资源回收或停播策略
- 如需在并行模式下阻塞节点，必须在 OnUpdate 内决定 Finish 时机

### 8.5 对象类 Action 基类：BaseTargeStepAction

- 作用：统一“目标对象”查找与持有，供所有“控制对象的 Action”继承使用。
- 参数：`target`（string）对象名；进入时会查找并缓存到 `m_Target`。
- 生命周期：重载 Enter，先执行目标查找，再调用基类 Enter，确保后续逻辑拿到有效对象。
- 查找行为：未配置或找不到目标对象时输出警告，不抛异常，动作可选择提前完成。
- 代码参考：[BaseTargeStepAction.cs](file:///d:/UnityProject/AFramework/Assets/LWFramework/RunTime/StepSystem/Action/BaseTargeStepAction.cs#L1-L42)

### 8.6 StepMoveObjectAction（DoTween 动画移动）

- 继承：BaseTargeStepAction，具备统一目标查找与缓存。
- 参数：`x/y/z` 目标坐标、`isLocal` 是否使用局部坐标、`moveTime` 动画时长（秒）。
- 行为：
  - Enter：计算目标位置并调用 DoTween 启动移动（`DOMove`/`DOLocalMove`）。
  - Update：无需额外推进，等待 DoTween `OnComplete` 回调触发 `Finish`。
  - Exit：调用 `transform.DOKill()` 终止未完成的 Tween，避免残留与泄漏。
  - Apply：直接写入 transform 最终位置，保证跳转补齐快速收敛。
- 并行模式：作为“多帧动作”，可与其他动作并行执行；节点完成以 `Finish` 为准。
- 依赖：DG.Tweening（DoTween）；需在工程内已导入该库。
- 代码参考：[StepMoveObjectAction.cs](file:///d:/UnityProject/AFramework/Assets/LWFramework/RunTime/StepSystem/Action/StepMoveObjectAction.cs#L76-L143)

### 8.7 StepPlayAudioAction 行为说明（并行友好）

- 参数：`clip` 路径、`target` 可选跟随对象、`volume` 音量、`isLoop` 是否循环、`fadeInSeconds` 淡入时长、`waitForFinish` 是否等待播放结束。
- 行为：
  - Enter：执行播放；若 `waitForFinish=false` 或 `isLoop=true`，立即 `Finish`；否则等待频道停止后完成。
  - Update：在等待模式下检测频道播放状态，结束时 `Finish`。
  - Exit：不强制停止音频（避免并行节点重复副作用）；如需停止可在具体场景侧控制。
  - Apply：执行一次播放，用于跳转补齐的即时反馈。
- 基线：支持捕获/恢复上次频道与 Clip，用于回退一致性处理。
- 代码参考：[StepPlayAudioAction.cs](file:///d:/UnityProject/AFramework/Assets/LWFramework/RunTime/StepSystem/Action/StepPlayAudioAction.cs#L82-L144)
