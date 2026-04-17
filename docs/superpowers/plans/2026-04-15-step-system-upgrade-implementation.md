# StepSystem 一体化升级 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在保持 XML 为主数据源的前提下，完成 `StepSystem` 的编辑器 Graph 升级、Action 扩展和示例体系升级，使其成为可维护、可联调、可扩展的通用流程编排工具。

**Architecture:** 本计划按“先地基、后表现、再扩展”的顺序推进。前两项先建立 `Action` 元数据与 XML 归一化模型，第三到第五项完成 Graph 呈现、编辑效率和运行时联调闭环，第六到第八项补齐动作族、示例族与文档入口。每个任务结束后，仓库都应保持可编译、可测试、可导出 XML。

**Tech Stack:** Unity 2022.3.62f3、C#、UIElements/GraphView、现有 `LWFramework.Runtime` / `LWFramework.Editor` 程序集、`dotnet test`、XML、Markdown

---

## 执行顺序

- 先阅读 [文件结构与职责](#文件结构与职责)
- 再按顺序执行：
  - [Task 1: 建立 Action 元数据地基](#task-1-建立-action-元数据地基)
  - [Task 2: 收口 XML 归一化模型](#task-2-收口-xml-归一化模型)
  - [Task 3: 升级 Graph 节点卡片与连线标签](#task-3-升级-graph-节点卡片与连线标签)
  - [Task 4: 增强编辑效率](#task-4-增强编辑效率)
  - [Task 5: 补运行时联调快照](#task-5-补运行时联调快照)
  - [Task 6: 实现流程控制与上下文动作](#task-6-实现流程控制与上下文动作)
  - [Task 7: 实现对象控制与实例化动作](#task-7-实现对象控制与实例化动作)
  - [Task 8: 建立示例体系与编辑器模板入口](#task-8-建立示例体系与编辑器模板入口)

---

### Task 8: 建立示例体系与编辑器模板入口

**Files:**
- Create: `Assets/Tests/StepSystem/EditMode/StepExampleXmlTests.cs`
- Create: `Assets/LWFramework/Editor/StepSystem/Templates/StepExampleTemplateCatalog.cs`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_BasicFlow.xml`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_ConditionBranch.xml`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_ParallelActions.xml`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_ContextOps.xml`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_ObjectControl.xml`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Context.xml`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Object.xml`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_Actions_AudioFx.xml`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_Flow_TeachingDemo.xml`
- Create: `Assets/0Res/RawFiles/StepExamples/StepExample_Flow_GeneralPipeline.xml`
- Modify: `Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/StepSystem_总结与使用说明.md`

- [ ] **Step 1: 先写示例 XML 回归测试**

`Assets/Tests/StepSystem/EditMode/StepExampleXmlTests.cs`

```csharp
using System.IO;
using LWStep;
using LWStep.Editor;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 示例 XML 导入导出与运行时校验。
    /// </summary>
    public sealed class StepExampleXmlTests
    {
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_BasicFlow.xml")]
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_ConditionBranch.xml")]
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_ParallelActions.xml")]
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_ContextOps.xml")]
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_ObjectControl.xml")]
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Context.xml")]
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Object.xml")]
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_Actions_AudioFx.xml")]
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_Flow_TeachingDemo.xml")]
        [TestCase("Assets/0Res/RawFiles/StepExamples/StepExample_Flow_GeneralPipeline.xml")]
        public void ExampleXml_ShouldImportExportAndValidate(string path)
        {
            string xml = File.ReadAllText(path);
            StepEditorGraphData editorData = StepXmlImporter.LoadFromText(xml);
            Assert.IsNotNull(editorData);
            Assert.IsNotEmpty(editorData.Nodes);

            string exportedXml = StepXmlExporter.ExportToText(editorData);
            StepActionFactory actionFactory = new StepActionFactory();
            StepXmlLoader loader = new StepXmlLoader();
            StepGraph graph = loader.LoadFromText(exportedXml, Path.GetFileNameWithoutExtension(path), actionFactory);

            Assert.IsNotNull(graph);
            Assert.IsEmpty(graph.Validate());
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认示例目录和模板入口尚不存在**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepExampleXmlTests
```

Expected:
- 测试失败，提示示例 XML 文件不存在。

- [ ] **Step 3: 新建示例目录、模板入口并更新文档**

`Assets/LWFramework/Editor/StepSystem/Templates/StepExampleTemplateCatalog.cs`

```csharp
using System.Collections.Generic;

namespace LWStep.Editor
{
    /// <summary>
    /// Step 示例模板目录。
    /// </summary>
    public static class StepExampleTemplateCatalog
    {
        public static readonly List<string> ExamplePaths = new List<string>
        {
            "Assets/0Res/RawFiles/StepExamples/StepExample_BasicFlow.xml",
            "Assets/0Res/RawFiles/StepExamples/StepExample_ConditionBranch.xml",
            "Assets/0Res/RawFiles/StepExamples/StepExample_ParallelActions.xml",
            "Assets/0Res/RawFiles/StepExamples/StepExample_ContextOps.xml",
            "Assets/0Res/RawFiles/StepExamples/StepExample_ObjectControl.xml",
            "Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Context.xml",
            "Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Object.xml",
            "Assets/0Res/RawFiles/StepExamples/StepExample_Actions_AudioFx.xml",
            "Assets/0Res/RawFiles/StepExamples/StepExample_Flow_TeachingDemo.xml",
            "Assets/0Res/RawFiles/StepExamples/StepExample_Flow_GeneralPipeline.xml",
        };
    }
}
```

把 `Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs` 的 Toolbar 增加模板按钮：

```csharp
            ToolbarMenu exampleMenu = new ToolbarMenu();
            exampleMenu.text = "导入示例";
            for (int i = 0; i < StepExampleTemplateCatalog.ExamplePaths.Count; i++)
            {
                string examplePath = StepExampleTemplateCatalog.ExamplePaths[i];
                exampleMenu.menu.AppendAction(examplePath, _ => ImportExampleTemplate(examplePath));
            }
            toolbar.Add(exampleMenu);
```

并增加：

```csharp
        /// <summary>
        /// 导入指定示例模板。
        /// </summary>
        private void ImportExampleTemplate(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            string xmlText = File.ReadAllText(path);
            StepEditorGraphData importedData = StepXmlImporter.LoadFromText(xmlText);
            if (importedData == null)
            {
                return;
            }

            LoadGraphData(importedData);
            SaveUndoSnapshot("导入示例模板");
        }
```

创建以下示例文件：

`Assets/0Res/RawFiles/StepExamples/StepExample_BasicFlow.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_basic_flow" start="node_start">
  <nodes>
    <node id="node_start" name="开始" x="0.00" y="0.00"><actions><action type="LWStep.StepLogAction"><param key="message" value="基础流程开始" /></action></actions></node>
    <node id="node_wait" name="等待" x="260.00" y="0.00"><actions><action type="LWStep.StepWaitSecondsAction"><param key="seconds" value="0.5" /></action></actions></node>
    <node id="node_end" name="结束" x="520.00" y="0.00"><actions><action type="LWStep.StepLogAction"><param key="message" value="基础流程结束" /></action></actions></node>
  </nodes>
  <edges><edge from="node_start" to="node_wait" priority="0" /><edge from="node_wait" to="node_end" priority="0" /></edges>
</graph>
```

`Assets/0Res/RawFiles/StepExamples/StepExample_ConditionBranch.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_condition_branch" start="node_start">
  <nodes>
    <node id="node_start" name="写入上下文" x="0.00" y="0.00"><actions><action type="LWStep.StepSetContextValueAction"><param key="key" value="mode" /><param key="value" value="A" /></action></actions></node>
    <node id="node_a" name="模式A" x="280.00" y="-80.00"><actions><action type="LWStep.StepLogAction"><param key="message" value="进入 A 分支" /></action></actions></node>
    <node id="node_other" name="其他模式" x="280.00" y="80.00"><actions><action type="LWStep.StepLogAction"><param key="message" value="进入其他分支" /></action></actions></node>
  </nodes>
  <edges><edge from="node_start" to="node_a" priority="10" conditionKey="mode" comparisonType="EqualTo" conditionValue="A" /><edge from="node_start" to="node_other" priority="0" conditionKey="mode" comparisonType="NotEqualTo" conditionValue="A" /></edges>
</graph>
```

`Assets/0Res/RawFiles/StepExamples/StepExample_ParallelActions.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_parallel_actions" start="node_parallel">
  <nodes>
    <node id="node_parallel" name="并行动作" x="0.00" y="0.00" mode="parallel"><actions><action type="LWStep.StepWaitSecondsAction"><param key="seconds" value="0.25" /></action><action type="LWStep.StepSetPositionAction"><param key="target" value="Cube" /><param key="x" value="1" /><param key="y" value="1" /><param key="z" value="0" /><param key="isLocal" value="true" /></action></actions></node>
    <node id="node_end" name="结束" x="320.00" y="0.00"><actions><action type="LWStep.StepLogAction"><param key="message" value="并行动作结束" /></action></actions></node>
  </nodes>
  <edges><edge from="node_parallel" to="node_end" priority="0" /></edges>
</graph>
```

`Assets/0Res/RawFiles/StepExamples/StepExample_ContextOps.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_context_ops" start="node_set">
  <nodes>
    <node id="node_set" name="写入上下文" x="0.00" y="0.00"><actions><action type="LWStep.StepSetContextValueAction"><param key="key" value="score" /><param key="value" value="5" /></action></actions></node>
    <node id="node_dispatch" name="派发事件" x="260.00" y="0.00"><actions><action type="LWStep.StepDispatchEventAction"><param key="eventName" value="STEP_CONTEXT_READY" /></action></actions></node>
    <node id="node_remove" name="移除上下文" x="520.00" y="0.00"><actions><action type="LWStep.StepRemoveContextValueAction"><param key="key" value="score" /></action></actions></node>
  </nodes>
  <edges><edge from="node_set" to="node_dispatch" priority="0" /><edge from="node_dispatch" to="node_remove" priority="0" /></edges>
</graph>
```

`Assets/0Res/RawFiles/StepExamples/StepExample_ObjectControl.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_object_control" start="node_show">
  <nodes>
    <node id="node_show" name="激活对象" x="0.00" y="0.00"><actions><action type="LWStep.StepSetActiveAction"><param key="target" value="Cube" /><param key="active" value="true" /></action></actions></node>
    <node id="node_move" name="调整变换" x="260.00" y="0.00"><actions><action type="LWStep.StepSetPositionAction"><param key="target" value="Cube" /><param key="x" value="1" /><param key="y" value="2" /><param key="z" value="3" /><param key="isLocal" value="true" /></action><action type="LWStep.StepSetRotationAction"><param key="target" value="Cube" /><param key="x" value="0" /><param key="y" value="90" /><param key="z" value="0" /><param key="isLocal" value="true" /></action><action type="LWStep.StepSetScaleAction"><param key="target" value="Cube" /><param key="x" value="2" /><param key="y" value="2" /><param key="z" value="2" /></action></actions></node>
    <node id="node_destroy" name="销毁对象" x="520.00" y="0.00"><actions><action type="LWStep.StepDestroyTargetAction"><param key="target" value="Cube" /></action></actions></node>
  </nodes>
  <edges><edge from="node_show" to="node_move" priority="0" /><edge from="node_move" to="node_destroy" priority="0" /></edges>
</graph>
```

`Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Context.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_actions_context" start="node_set_mode">
  <nodes>
    <node id="node_set_mode" name="设置模式" x="0.00" y="0.00"><actions><action type="LWStep.StepSetContextValueAction"><param key="key" value="mode" /><param key="value" value="practice" /></action></actions></node>
    <node id="node_set_score" name="设置分数" x="240.00" y="0.00"><actions><action type="LWStep.StepSetContextValueAction"><param key="key" value="score" /><param key="value" value="100" /></action></actions></node>
    <node id="node_remove_mode" name="删除模式" x="480.00" y="0.00"><actions><action type="LWStep.StepRemoveContextValueAction"><param key="key" value="mode" /></action></actions></node>
  </nodes>
  <edges><edge from="node_set_mode" to="node_set_score" priority="0" /><edge from="node_set_score" to="node_remove_mode" priority="0" /></edges>
</graph>
```

`Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Object.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_actions_object" start="node_spawn">
  <nodes>
    <node id="node_spawn" name="实例化" x="0.00" y="0.00"><actions><action type="LWStep.StepInstantiatePrefabAction"><param key="prefab" value="Assets/0Res/Prefabs/Cube.prefab" /><param key="instanceName" value="StepSpawnedCube" /></action></actions></node>
    <node id="node_parent" name="设置父节点" x="260.00" y="0.00"><actions><action type="LWStep.StepSetParentAction"><param key="target" value="StepSpawnedCube" /><param key="parent" value="StepRoot" /><param key="worldPositionStays" value="true" /></action></actions></node>
    <node id="node_cleanup" name="清理" x="520.00" y="0.00"><actions><action type="LWStep.StepDestroyTargetAction"><param key="target" value="StepSpawnedCube" /></action></actions></node>
  </nodes>
  <edges><edge from="node_spawn" to="node_parent" priority="0" /><edge from="node_parent" to="node_cleanup" priority="0" /></edges>
</graph>
```

`Assets/0Res/RawFiles/StepExamples/StepExample_Actions_AudioFx.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_actions_audiofx" start="node_audio">
  <nodes>
    <node id="node_audio" name="播放音频" x="0.00" y="0.00"><actions><action type="LWStep.StepPlayAudioAction"><param key="clip" value="Assets/0Res/Audios/bgm.wav" /><param key="target" value="AudioSourceObj" /><param key="volume" value="1" /><param key="isLoop" value="false" /><param key="fadeInSeconds" value="0" /></action></actions></node>
    <node id="node_fx" name="播放粒子" x="260.00" y="0.00"><actions><action type="LWStep.StepPlayParticleAction"><param key="target" value="SnowVFX" /><param key="waitForFinish" value="false" /><param key="restart" value="true" /></action></actions></node>
  </nodes>
  <edges><edge from="node_audio" to="node_fx" priority="0" /></edges>
</graph>
```

`Assets/0Res/RawFiles/StepExamples/StepExample_Flow_TeachingDemo.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_flow_teaching_demo" start="node_tip">
  <nodes>
    <node id="node_tip" name="步骤提示" x="0.00" y="0.00"><actions><action type="LWStep.StepLogAction"><param key="message" value="请先观察设备状态" /></action></actions></node>
    <node id="node_wait" name="等待学员确认" x="240.00" y="0.00"><actions><action type="LWStep.StepWaitMouseLeftClickAction" /></actions></node>
    <node id="node_highlight" name="高亮目标" x="480.00" y="0.00"><actions><action type="LWStep.StepSetActiveAction"><param key="target" value="TeachingHighlight" /><param key="active" value="true" /></action></actions></node>
    <node id="node_branch" name="结果判断" x="720.00" y="0.00"><actions><action type="LWStep.StepSetContextValueAction"><param key="key" value="passed" /><param key="value" value="true" /></action></actions></node>
    <node id="node_success" name="通过" x="980.00" y="-80.00"><actions><action type="LWStep.StepLogAction"><param key="message" value="教学流程通过" /></action></actions></node>
    <node id="node_retry" name="重试" x="980.00" y="80.00"><actions><action type="LWStep.StepLogAction"><param key="message" value="请重新尝试" /></action></actions></node>
  </nodes>
  <edges><edge from="node_tip" to="node_wait" priority="0" /><edge from="node_wait" to="node_highlight" priority="0" /><edge from="node_highlight" to="node_branch" priority="0" /><edge from="node_branch" to="node_success" priority="10" conditionKey="passed" comparisonType="EqualTo" conditionValue="true" /><edge from="node_branch" to="node_retry" priority="0" conditionKey="passed" comparisonType="NotEqualTo" conditionValue="true" /></edges>
</graph>
```

`Assets/0Res/RawFiles/StepExamples/StepExample_Flow_GeneralPipeline.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<graph id="step_example_flow_general_pipeline" start="node_bootstrap">
  <nodes>
    <node id="node_bootstrap" name="初始化" x="0.00" y="0.00"><actions><action type="LWStep.StepSetContextValueAction"><param key="key" value="mode" /><param key="value" value="pipeline" /></action><action type="LWStep.StepDispatchEventAction"><param key="eventName" value="PIPELINE_BOOTSTRAP" /></action></actions></node>
    <node id="node_parallel" name="并行阶段" x="280.00" y="0.00" mode="parallel"><actions><action type="LWStep.StepWaitSecondsAction"><param key="seconds" value="0.2" /></action><action type="LWStep.StepInstantiatePrefabAction"><param key="prefab" value="Assets/0Res/Prefabs/Cube.prefab" /><param key="instanceName" value="PipelineCube" /></action></actions></node>
    <node id="node_finalize" name="收尾" x="560.00" y="0.00"><actions><action type="LWStep.StepSetPositionAction"><param key="target" value="PipelineCube" /><param key="x" value="0" /><param key="y" value="1" /><param key="z" value="2" /><param key="isLocal" value="true" /></action><action type="LWStep.StepDestroyTargetAction"><param key="target" value="PipelineCube" /></action></actions></node>
  </nodes>
  <edges><edge from="node_bootstrap" to="node_parallel" priority="0" /><edge from="node_parallel" to="node_finalize" priority="0" /></edges>
</graph>
```

把 `Assets/LWFramework/Editor/StepSystem/StepSystem_总结与使用说明.md` 增加以下段落：

```markdown
## 9. 升级后的 Graph 与模板入口

- 节点卡片现在会直接显示：
  - 节点 ID
  - 节点名称
  - 执行模式
  - 最多 3 条动作摘要
- 连线会直接显示：
  - 优先级
  - 条件 Key / 比较类型 / 条件值
- 工具栏新增：
  - 自动布局
  - 重复
  - 导入示例

## 10. 示例目录

示例统一放在：`Assets/0Res/RawFiles/StepExamples/`

- 基础能力：
  - `StepExample_BasicFlow.xml`
  - `StepExample_ConditionBranch.xml`
  - `StepExample_ParallelActions.xml`
  - `StepExample_ContextOps.xml`
  - `StepExample_ObjectControl.xml`
- Action 主题：
  - `StepExample_Actions_Context.xml`
  - `StepExample_Actions_Object.xml`
  - `StepExample_Actions_AudioFx.xml`
- 综合流程：
  - `StepExample_Flow_TeachingDemo.xml`
  - `StepExample_Flow_GeneralPipeline.xml`
```

- [ ] **Step 4: 运行示例 XML 回归测试和全量 StepSystem EditMode 测试**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepExampleXmlTests
```

Expected:
- `StepExampleXmlTests` 全部 PASS。

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj
```

Expected:
- `StepActionDescriptorRegistryTests`
- `StepXmlRoundTripTests`
- `StepGraphPresentationBuilderTests`
- `StepGraphAutoLayoutTests`
- `StepEditorClipboardTests`
- `StepRuntimeDebugTrackerTests`
- `StepWorkflowActionsTests`
- `StepObjectActionsTests`
- `StepExampleXmlTests`
  全部 PASS。

- [ ] **Step 5: 提交示例体系与文档入口**

```bash
git add Assets/Tests/StepSystem/EditMode/StepExampleXmlTests.cs Assets/LWFramework/Editor/StepSystem/Templates/StepExampleTemplateCatalog.cs Assets/0Res/RawFiles/StepExamples Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs Assets/LWFramework/Editor/StepSystem/StepSystem_总结与使用说明.md
git commit -m "feat: add StepSystem example suite and template entry"
```

---

## 自检

### 1. 规格覆盖

- `XML 主数据源与规范化读写`：Task 2
- `Graph 可视化增强`：Task 3、Task 5
- `Graph 编辑效率增强`：Task 4、Task 8
- `运行时联调增强`：Task 5
- `Action 元数据体系`：Task 1
- `新增通用 Action`：Task 6、Task 7
- `框架桥接 Action 预留`：Task 1 的分类结构、Task 8 的模板入口
- `示例体系分层`：Task 8

结论：设计规格中的核心需求都有对应任务，没有范围遗漏。

### 2. 占位符扫描

- 已避免使用计划失败语句，任务内容均已具体化。
- 每个任务都给出了具体文件、代码片段、命令和期望结果。

### 3. 类型与命名一致性

- 动作元数据统一使用 `StepActionInfoAttribute`、`StepActionDescriptor`、`StepActionDescriptorRegistry`
- 运行时联调统一使用 `StepRuntimeDebugTracker`、`StepRuntimeDebugSnapshot`
- 自动布局统一使用 `StepGraphAutoLayout`
- 剪贴板统一使用 `StepEditorClipboard`、`StepEditorClipboardPayload`

结论：命名在各任务之间保持一致，可直接进入执行阶段。

### Task 7: 实现对象控制与实例化动作

**Files:**
- Create: `Assets/Tests/StepSystem/EditMode/StepObjectActionsTests.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepSetActiveAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepSetPositionAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepSetRotationAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepSetScaleAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepSetParentAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepDestroyTargetAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepInstantiatePrefabAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepPlayParticleAction.cs`

- [ ] **Step 1: 先写对象控制动作失败测试**

`Assets/Tests/StepSystem/EditMode/StepObjectActionsTests.cs`

```csharp
using LWStep;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 对象控制动作测试。
    /// </summary>
    public sealed class StepObjectActionsTests
    {
        [Test]
        public void StepSetActiveAction_Apply_ShouldChangeActiveState()
        {
            GameObject target = new GameObject("StepTarget_SetActive");
            try
            {
                StepSetActiveAction action = new StepSetActiveAction();
                action.SetParameters(new System.Collections.Generic.Dictionary<string, string>
                {
                    { "target", "StepTarget_SetActive" },
                    { "active", "false" }
                });

                action.Apply();

                Assert.IsFalse(target.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void StepSetPositionAction_Apply_ShouldWriteTransformPosition()
        {
            GameObject target = new GameObject("StepTarget_SetPosition");
            try
            {
                StepSetPositionAction action = new StepSetPositionAction();
                action.SetParameters(new System.Collections.Generic.Dictionary<string, string>
                {
                    { "target", "StepTarget_SetPosition" },
                    { "x", "1" },
                    { "y", "2" },
                    { "z", "3" },
                    { "isLocal", "true" }
                });

                action.Apply();

                Assert.AreEqual(new Vector3(1f, 2f, 3f), target.transform.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void StepDestroyTargetAction_Apply_ShouldDestroyTargetImmediatelyInEditor()
        {
            GameObject target = new GameObject("StepTarget_Destroy");

            StepDestroyTargetAction action = new StepDestroyTargetAction();
            action.SetParameters(new System.Collections.Generic.Dictionary<string, string>
            {
                { "target", "StepTarget_Destroy" }
            });

            action.Apply();

            Assert.IsNull(GameObject.Find("StepTarget_Destroy"));
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认对象控制动作尚不存在**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepObjectActionsTests
```

Expected:
- 编译失败，提示 `StepSetActiveAction`、`StepSetPositionAction`、`StepDestroyTargetAction` 不存在。

- [ ] **Step 3: 实现对象控制、实例化与粒子动作**

`Assets/LWFramework/RunTime/StepSystem/Action/StepSetActiveAction.cs`

```csharp
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 设置对象激活状态。
    /// </summary>
    [StepActionInfo("设置激活状态", "对象控制", SummaryTemplate = "Active:{target}", Description = "调用 GameObject.SetActive", Keywords = "gameobject active")]
    public sealed class StepSetActiveAction : BaseTargeStepAction
    {
        [StepParam("active", Label = "是否激活", Order = 1)]
        private bool m_Active = true;

        protected override void OnEnter()
        {
            ApplyState();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { ApplyState(); }

        private void ApplyState()
        {
            if (m_Target != null)
            {
                m_Target.SetActive(m_Active);
            }
        }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepSetPositionAction.cs`

```csharp
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 立即设置位置。
    /// </summary>
    [StepActionInfo("设置位置", "对象控制", SummaryTemplate = "Pos:{target}", Description = "立即设置 Transform 位置", Keywords = "position transform")]
    public sealed class StepSetPositionAction : BaseTargeStepAction
    {
        [StepParam("x", Label = "X", Order = 1)] private float m_X;
        [StepParam("y", Label = "Y", Order = 2)] private float m_Y;
        [StepParam("z", Label = "Z", Order = 3)] private float m_Z;
        [StepParam("isLocal", Label = "局部坐标", Order = 4)] private bool m_IsLocal = true;

        protected override void OnEnter()
        {
            ApplyPosition();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { ApplyPosition(); }

        private void ApplyPosition()
        {
            if (m_Target == null)
            {
                return;
            }

            Vector3 position = new Vector3(m_X, m_Y, m_Z);
            if (m_IsLocal)
            {
                m_Target.transform.localPosition = position;
            }
            else
            {
                m_Target.transform.position = position;
            }
        }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepSetRotationAction.cs`

```csharp
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 立即设置旋转。
    /// </summary>
    [StepActionInfo("设置旋转", "对象控制", SummaryTemplate = "Rot:{target}", Description = "立即设置 Transform 旋转", Keywords = "rotation transform")]
    public sealed class StepSetRotationAction : BaseTargeStepAction
    {
        [StepParam("x", Label = "X", Order = 1)] private float m_X;
        [StepParam("y", Label = "Y", Order = 2)] private float m_Y;
        [StepParam("z", Label = "Z", Order = 3)] private float m_Z;
        [StepParam("isLocal", Label = "局部旋转", Order = 4)] private bool m_IsLocal = true;

        protected override void OnEnter()
        {
            ApplyRotation();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { ApplyRotation(); }

        private void ApplyRotation()
        {
            if (m_Target == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(m_X, m_Y, m_Z);
            if (m_IsLocal)
            {
                m_Target.transform.localRotation = rotation;
            }
            else
            {
                m_Target.transform.rotation = rotation;
            }
        }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepSetScaleAction.cs`

```csharp
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 立即设置缩放。
    /// </summary>
    [StepActionInfo("设置缩放", "对象控制", SummaryTemplate = "Scale:{target}", Description = "立即设置局部缩放", Keywords = "scale transform")]
    public sealed class StepSetScaleAction : BaseTargeStepAction
    {
        [StepParam("x", Label = "X", Order = 1)] private float m_X = 1f;
        [StepParam("y", Label = "Y", Order = 2)] private float m_Y = 1f;
        [StepParam("z", Label = "Z", Order = 3)] private float m_Z = 1f;

        protected override void OnEnter()
        {
            ApplyScale();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { ApplyScale(); }

        private void ApplyScale()
        {
            if (m_Target != null)
            {
                m_Target.transform.localScale = new Vector3(m_X, m_Y, m_Z);
            }
        }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepSetParentAction.cs`

```csharp
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 设置父节点。
    /// </summary>
    [StepActionInfo("设置父节点", "对象控制", SummaryTemplate = "Parent:{target}", Description = "把目标对象挂到新的父节点下", Keywords = "parent transform")]
    public sealed class StepSetParentAction : BaseTargeStepAction
    {
        [StepParam("parent", Label = "父节点名", Order = 1)]
        private string m_ParentName;

        [StepParam("worldPositionStays", Label = "保持世界坐标", Order = 2)]
        private bool m_WorldPositionStays = true;

        protected override void OnEnter()
        {
            ApplyParent();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { ApplyParent(); }

        private void ApplyParent()
        {
            if (m_Target == null)
            {
                return;
            }

            Transform parent = string.IsNullOrEmpty(m_ParentName) ? null : GameObject.Find(m_ParentName)?.transform;
            m_Target.transform.SetParent(parent, m_WorldPositionStays);
        }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepDestroyTargetAction.cs`

```csharp
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 销毁目标对象。
    /// </summary>
    [StepActionInfo("销毁对象", "对象控制", SummaryTemplate = "Destroy:{target}", Description = "销毁目标对象", Keywords = "destroy gameobject")]
    public sealed class StepDestroyTargetAction : BaseTargeStepAction
    {
        protected override void OnEnter()
        {
            DestroyTarget();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { DestroyTarget(); }

        private void DestroyTarget()
        {
            if (m_Target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(m_Target);
            }
            else
            {
                Object.DestroyImmediate(m_Target);
            }
        }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepInstantiatePrefabAction.cs`

```csharp
using LWCore;
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 实例化预制体。
    /// </summary>
    [StepActionInfo("实例化预制体", "对象控制", SummaryTemplate = "Spawn:{prefab}", Description = "加载并实例化一个预制体", Keywords = "prefab instantiate spawn")]
    public sealed class StepInstantiatePrefabAction : BaseStepAction
    {
        [StepParam("prefab", Label = "预制体路径", Order = 0)]
        private string m_PrefabPath;

        [StepParam("instanceName", Label = "实例名", Order = 1)]
        private string m_InstanceName;

        [StepParam("parent", Label = "父节点名", Order = 2)]
        private string m_ParentName;

        protected override void OnEnter()
        {
            InstantiatePrefab();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { InstantiatePrefab(); }

        private void InstantiatePrefab()
        {
            if (string.IsNullOrEmpty(m_PrefabPath) || ManagerUtility.AssetsMgr == null)
            {
                return;
            }

            GameObject prefab = ManagerUtility.AssetsMgr.LoadAsset<GameObject>(m_PrefabPath);
            if (prefab == null)
            {
                return;
            }

            Transform parent = string.IsNullOrEmpty(m_ParentName) ? null : GameObject.Find(m_ParentName)?.transform;
            GameObject instance = Object.Instantiate(prefab, parent);
            if (!string.IsNullOrEmpty(m_InstanceName))
            {
                instance.name = m_InstanceName;
            }
        }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepPlayParticleAction.cs`

```csharp
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 播放粒子系统。
    /// </summary>
    [StepActionInfo("播放粒子", "动画与特效", SummaryTemplate = "Particle:{target}", Description = "播放目标对象上的粒子系统", Keywords = "particle play effect")]
    public sealed class StepPlayParticleAction : BaseTargeStepAction
    {
        [StepParam("waitForFinish", Label = "等待结束", Order = 1)]
        private bool m_WaitForFinish;

        [StepParam("restart", Label = "重新开始", Order = 2)]
        private bool m_Restart = true;

        private ParticleSystem m_ParticleSystem;

        protected override void OnEnter()
        {
            if (m_Target == null)
            {
                Finish();
                return;
            }

            m_ParticleSystem = m_Target.GetComponent<ParticleSystem>();
            if (m_ParticleSystem == null)
            {
                Finish();
                return;
            }

            if (m_Restart)
            {
                m_ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            m_ParticleSystem.Play(true);

            if (!m_WaitForFinish)
            {
                Finish();
            }
        }

        protected override void OnUpdate()
        {
            if (m_ParticleSystem == null || !m_ParticleSystem.IsAlive(true))
            {
                Finish();
            }
        }

        protected override void OnExit() { }
        protected override void OnApply()
        {
            if (m_Target != null)
            {
                ParticleSystem particleSystem = m_Target.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    particleSystem.Play(true);
                }
            }
        }
    }
}
```

- [ ] **Step 4: 重新运行对象控制动作测试**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepObjectActionsTests
```

Expected:
- `StepObjectActionsTests` 全部 PASS。

- [ ] **Step 5: 提交对象控制与实例化动作**

```bash
git add Assets/Tests/StepSystem/EditMode/StepObjectActionsTests.cs Assets/LWFramework/RunTime/StepSystem/Action/StepSetActiveAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepSetPositionAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepSetRotationAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepSetScaleAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepSetParentAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepDestroyTargetAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepInstantiatePrefabAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepPlayParticleAction.cs
git commit -m "feat: add StepSystem object control actions"
```

---

### Task 6: 实现流程控制与上下文动作

**Files:**
- Create: `Assets/Tests/StepSystem/EditMode/StepWorkflowActionsTests.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepWaitSecondsAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepSetContextValueAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepRemoveContextValueAction.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepDispatchEventAction.cs`

- [ ] **Step 1: 先写流程控制与上下文动作失败测试**

`Assets/Tests/StepSystem/EditMode/StepWorkflowActionsTests.cs`

```csharp
using System.Threading;
using LWStep;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 通用流程动作测试。
    /// </summary>
    public sealed class StepWorkflowActionsTests
    {
        [Test]
        public void StepSetContextValueAction_Apply_ShouldWriteParsedValue()
        {
            StepContext context = new StepContext();
            StepSetContextValueAction action = new StepSetContextValueAction();
            action.SetContext(context);
            action.SetParameters(new System.Collections.Generic.Dictionary<string, string>
            {
                { "key", "score" },
                { "value", "5" }
            });

            action.Apply();

            Assert.AreEqual(5, context.GetValue("score", 0));
        }

        [Test]
        public void StepRemoveContextValueAction_Apply_ShouldDeleteKey()
        {
            StepContext context = new StepContext();
            context.SetValue("mode", "A");
            StepRemoveContextValueAction action = new StepRemoveContextValueAction();
            action.SetContext(context);
            action.SetParameters(new System.Collections.Generic.Dictionary<string, string>
            {
                { "key", "mode" }
            });

            action.Apply();

            Assert.IsFalse(context.HasKey("mode"));
        }

        [Test]
        public void StepWaitSecondsAction_Update_ShouldFinishAfterDuration()
        {
            StepWaitSecondsAction action = new StepWaitSecondsAction();
            action.SetParameters(new System.Collections.Generic.Dictionary<string, string>
            {
                { "seconds", "0.02" }
            });

            action.Enter();
            Thread.Sleep(40);
            action.Update();

            Assert.IsTrue(action.IsFinished);
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认新动作尚不存在**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepWorkflowActionsTests
```

Expected:
- 编译失败，提示 `StepWaitSecondsAction`、`StepSetContextValueAction`、`StepRemoveContextValueAction` 不存在。

- [ ] **Step 3: 实现首批流程控制与上下文动作**

`Assets/LWFramework/RunTime/StepSystem/Action/StepWaitSecondsAction.cs`

```csharp
using UnityEngine;

namespace LWStep
{
    /// <summary>
    /// 等待指定秒数。
    /// </summary>
    [StepActionInfo("等待秒数", "流程控制", SummaryTemplate = "Wait:{seconds}s", Description = "等待指定秒数后继续", Keywords = "wait delay time")]
    public sealed class StepWaitSecondsAction : BaseStepAction
    {
        [StepParam("seconds", Label = "等待秒数", Order = 0)]
        private float m_Seconds = 1f;

        private float m_EndTime;

        protected override void OnEnter()
        {
            m_EndTime = Time.realtimeSinceStartup + Mathf.Max(0f, m_Seconds);
            if (m_Seconds <= 0f)
            {
                Finish();
            }
        }

        protected override void OnUpdate()
        {
            if (Time.realtimeSinceStartup >= m_EndTime)
            {
                Finish();
            }
        }

        protected override void OnExit() { }
        protected override void OnApply() { Finish(); }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepSetContextValueAction.cs`

```csharp
namespace LWStep
{
    /// <summary>
    /// 写入上下文键值。
    /// </summary>
    [StepActionInfo("写入上下文", "上下文", SummaryTemplate = "Set:{key}", Description = "把字符串自动解析后写入 StepContext", Keywords = "context set value")]
    public sealed class StepSetContextValueAction : BaseStepAction
    {
        [StepParam("key", Label = "上下文键", Order = 0)]
        private string m_Key;

        [StepParam("value", Label = "上下文值", Order = 1)]
        private string m_Value;

        protected override void OnEnter()
        {
            WriteValue();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { WriteValue(); }

        private void WriteValue()
        {
            if (string.IsNullOrEmpty(m_Key))
            {
                return;
            }

            object parsedValue;
            if (StepUtility.TryParseBasicValue(m_Value, out parsedValue))
            {
                GetContext().SetValue(m_Key, parsedValue);
            }
            else
            {
                GetContext().SetValue(m_Key, m_Value ?? string.Empty);
            }
        }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepRemoveContextValueAction.cs`

```csharp
namespace LWStep
{
    /// <summary>
    /// 删除上下文键值。
    /// </summary>
    [StepActionInfo("移除上下文", "上下文", SummaryTemplate = "Remove:{key}", Description = "删除指定的 StepContext 键", Keywords = "context remove key")]
    public sealed class StepRemoveContextValueAction : BaseStepAction
    {
        [StepParam("key", Label = "上下文键", Order = 0)]
        private string m_Key;

        protected override void OnEnter()
        {
            RemoveValue();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { RemoveValue(); }

        private void RemoveValue()
        {
            if (!string.IsNullOrEmpty(m_Key))
            {
                GetContext().RemoveKey(m_Key);
            }
        }
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Action/StepDispatchEventAction.cs`

```csharp
using LWCore;

namespace LWStep
{
    /// <summary>
    /// 派发框架事件。
    /// </summary>
    [StepActionInfo("派发事件", "流程控制", SummaryTemplate = "Event:{eventName}", Description = "向 EventMgr 派发一个无参事件", Keywords = "event dispatch")]
    public sealed class StepDispatchEventAction : BaseStepAction
    {
        [StepParam("eventName", Label = "事件名", Order = 0)]
        private string m_EventName;

        protected override void OnEnter()
        {
            Dispatch();
            Finish();
        }

        protected override void OnUpdate() { }
        protected override void OnExit() { }
        protected override void OnApply() { Dispatch(); }

        private void Dispatch()
        {
            if (!string.IsNullOrEmpty(m_EventName) && ManagerUtility.EventMgr != null)
            {
                ManagerUtility.EventMgr.DispatchEvent(m_EventName);
            }
        }
    }
}
```

- [ ] **Step 4: 重新运行流程动作测试**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepWorkflowActionsTests
```

Expected:
- `StepWorkflowActionsTests` 全部 PASS。

- [ ] **Step 5: 提交流程控制与上下文动作**

```bash
git add Assets/Tests/StepSystem/EditMode/StepWorkflowActionsTests.cs Assets/LWFramework/RunTime/StepSystem/Action/StepWaitSecondsAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepSetContextValueAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepRemoveContextValueAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepDispatchEventAction.cs
git commit -m "feat: add StepSystem workflow actions"
```

---

### Task 5: 补运行时联调快照

**Files:**
- Create: `Assets/Tests/StepSystem/EditMode/StepRuntimeDebugTrackerTests.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Diagnostics/StepRuntimeDebugSnapshot.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Diagnostics/StepRuntimeDebugTracker.cs`
- Modify: `Assets/LWFramework/RunTime/Core/InterfaceManager/IStepManager.cs`
- Modify: `Assets/LWFramework/RunTime/StepSystem/StepManager.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs`

- [ ] **Step 1: 先写运行时联调轨迹失败测试**

`Assets/Tests/StepSystem/EditMode/StepRuntimeDebugTrackerTests.cs`

```csharp
using LWStep;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 运行时联调轨迹记录测试。
    /// </summary>
    public sealed class StepRuntimeDebugTrackerTests
    {
        [Test]
        public void CreateSnapshot_ShouldExposeTrailCurrentActionAndEvents()
        {
            StepContext context = new StepContext();
            context.SetValue("mode", "A");
            context.SetValue("score", 5);

            StepRuntimeDebugTracker tracker = new StepRuntimeDebugTracker();
            tracker.RecordNodeEnter("node_start");
            tracker.RecordActionChanged("StepMoveObjectAction");
            tracker.RecordNodeEnter("node_middle");
            tracker.RecordJump("node_end");

            StepRuntimeDebugSnapshot snapshot = tracker.CreateSnapshot(context, "node_middle", "StepMoveObjectAction");

            Assert.AreEqual("node_middle", snapshot.CurrentNodeId);
            Assert.AreEqual("StepMoveObjectAction", snapshot.CurrentActionName);
            CollectionAssert.Contains(snapshot.TrailNodeIds, "node_start");
            CollectionAssert.Contains(snapshot.TrailNodeIds, "node_middle");
            Assert.AreEqual("A", snapshot.ContextValues["mode"]);
            Assert.AreEqual("5", snapshot.ContextValues["score"]);
            Assert.AreEqual("Jump:node_end", snapshot.RecentEvents[snapshot.RecentEvents.Count - 1]);
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认调试快照和记录器尚不存在**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepRuntimeDebugTrackerTests
```

Expected:
- 编译失败，提示 `StepRuntimeDebugTracker` 或 `StepRuntimeDebugSnapshot` 不存在。

- [ ] **Step 3: 实现运行时联调数据接口，并接到编辑器**

`Assets/LWFramework/RunTime/StepSystem/Diagnostics/StepRuntimeDebugSnapshot.cs`

```csharp
using System.Collections.Generic;

namespace LWStep
{
    /// <summary>
    /// StepSystem 运行时调试快照。
    /// </summary>
    public sealed class StepRuntimeDebugSnapshot
    {
        public string CurrentNodeId;
        public string CurrentActionName;
        public List<string> TrailNodeIds = new List<string>();
        public List<string> RecentEvents = new List<string>();
        public Dictionary<string, string> ContextValues = new Dictionary<string, string>();
    }
}
```

`Assets/LWFramework/RunTime/StepSystem/Diagnostics/StepRuntimeDebugTracker.cs`

```csharp
using System.Collections.Generic;

namespace LWStep
{
    /// <summary>
    /// StepSystem 运行时调试轨迹记录器。
    /// </summary>
    public sealed class StepRuntimeDebugTracker
    {
        private const int MAX_EVENT_COUNT = 16;
        private readonly List<string> m_TrailNodeIds = new List<string>();
        private readonly Queue<string> m_RecentEvents = new Queue<string>();

        public void Clear()
        {
            m_TrailNodeIds.Clear();
            m_RecentEvents.Clear();
        }

        public void RecordNodeEnter(string nodeId)
        {
            if (!string.IsNullOrEmpty(nodeId) && !m_TrailNodeIds.Contains(nodeId))
            {
                m_TrailNodeIds.Add(nodeId);
            }
            AppendEvent("Enter:" + nodeId);
        }

        public void RecordNodeLeave(string nodeId)
        {
            AppendEvent("Leave:" + nodeId);
        }

        public void RecordActionChanged(string actionName)
        {
            AppendEvent("Action:" + actionName);
        }

        public void RecordJump(string nodeId)
        {
            AppendEvent("Jump:" + nodeId);
        }

        public StepRuntimeDebugSnapshot CreateSnapshot(StepContext context, string currentNodeId, string currentActionName)
        {
            StepRuntimeDebugSnapshot snapshot = new StepRuntimeDebugSnapshot();
            snapshot.CurrentNodeId = currentNodeId ?? string.Empty;
            snapshot.CurrentActionName = currentActionName ?? string.Empty;
            snapshot.TrailNodeIds.AddRange(m_TrailNodeIds);

            foreach (string evt in m_RecentEvents)
            {
                snapshot.RecentEvents.Add(evt);
            }

            if (context != null)
            {
                Dictionary<string, object> rawData = context.CloneData();
                foreach (KeyValuePair<string, object> kvp in rawData)
                {
                    snapshot.ContextValues[kvp.Key] = kvp.Value != null ? kvp.Value.ToString() : string.Empty;
                }
            }

            return snapshot;
        }

        private void AppendEvent(string message)
        {
            if (m_RecentEvents.Count >= MAX_EVENT_COUNT)
            {
                m_RecentEvents.Dequeue();
            }
            m_RecentEvents.Enqueue(message);
        }
    }
}
```

把 `Assets/LWFramework/RunTime/Core/InterfaceManager/IStepManager.cs` 增加：

```csharp
        /// <summary>
        /// 获取当前运行时调试快照。
        /// </summary>
        StepRuntimeDebugSnapshot GetRuntimeDebugSnapshot();
```

把 `Assets/LWFramework/RunTime/StepSystem/StepManager.cs` 增加字段、初始化和接口实现：

```csharp
        private StepRuntimeDebugTracker m_DebugTracker;
```

```csharp
            m_DebugTracker = new StepRuntimeDebugTracker();
```

```csharp
        public StepRuntimeDebugSnapshot GetRuntimeDebugSnapshot()
        {
            string actionName = m_CurrentNode != null ? m_CurrentNode.GetCurrentActionName() : string.Empty;
            return m_DebugTracker.CreateSnapshot(m_Context, CurrentNodeId, actionName);
        }
```

并在这些位置追加记录：

```csharp
            m_DebugTracker.Clear();
            m_DebugTracker.RecordNodeEnter(node.Id);
```

```csharp
            if (isActionChanged)
            {
                NotifyActionChanged(false);
                m_DebugTracker.RecordActionChanged(m_CurrentNode.GetCurrentActionName());
            }
```

```csharp
            OnNodeEnter?.Invoke(node.Id);
            m_DebugTracker.RecordNodeEnter(node.Id);
```

```csharp
                    OnJumpProgress?.Invoke(node.Id);
                    m_DebugTracker.RecordJump(node.Id);
```

把 `Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs` 的运行时刷新改成：

```csharp
            StepRuntimeDebugSnapshot snapshot = stepManager.GetRuntimeDebugSnapshot();
            m_GraphView.SetRuntimeTrail(snapshot.TrailNodeIds);

            if (m_ContextText != null)
            {
                m_ContextText.value = "CurrentNode: " + snapshot.CurrentNodeId + "\nCurrentAction: " + snapshot.CurrentActionName + "\n" + stepManager.GetContextToJson();
            }
```

把 `Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs` 的节点刷新处改成：

```csharp
                StepNodePresentation presentation = StepGraphPresentationBuilder.BuildNodePresentation(
                    m_Data,
                    nodeData,
                    status,
                    kvp.Key == m_RuntimeNodeId ? "Running" : string.Empty,
                    m_RuntimeTrailNodeIds.Contains(nodeData.Id));
```

- [ ] **Step 4: 重新运行联调轨迹测试**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepRuntimeDebugTrackerTests
```

Expected:
- `StepRuntimeDebugTrackerTests` 全部 PASS。

- [ ] **Step 5: 提交运行时联调快照**

```bash
git add Assets/Tests/StepSystem/EditMode/StepRuntimeDebugTrackerTests.cs Assets/LWFramework/RunTime/StepSystem/Diagnostics/StepRuntimeDebugSnapshot.cs Assets/LWFramework/RunTime/StepSystem/Diagnostics/StepRuntimeDebugTracker.cs Assets/LWFramework/RunTime/Core/InterfaceManager/IStepManager.cs Assets/LWFramework/RunTime/StepSystem/StepManager.cs Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs
git commit -m "feat: add StepSystem runtime debug snapshot"
```

---

### Task 4: 增强编辑效率

**Files:**
- Create: `Assets/Tests/StepSystem/EditMode/StepGraphAutoLayoutTests.cs`
- Create: `Assets/Tests/StepSystem/EditMode/StepEditorClipboardTests.cs`
- Create: `Assets/LWFramework/Editor/StepSystem/Layout/StepGraphAutoLayout.cs`
- Create: `Assets/LWFramework/Editor/StepSystem/Clipboard/StepEditorClipboard.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs`

- [ ] **Step 1: 先写自动布局和复制粘贴失败测试**

`Assets/Tests/StepSystem/EditMode/StepGraphAutoLayoutTests.cs`

```csharp
using LWStep.Editor;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// Graph 自动布局测试。
    /// </summary>
    public sealed class StepGraphAutoLayoutTests
    {
        [Test]
        public void ApplyLeftToRight_ShouldIncreaseXAlongMainPath()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.StartNodeId = "start";
            data.Nodes.Add(new StepEditorNodeData { Id = "start" });
            data.Nodes.Add(new StepEditorNodeData { Id = "middle" });
            data.Nodes.Add(new StepEditorNodeData { Id = "end" });
            data.Edges.Add(new StepEditorEdgeData { FromId = "start", ToId = "middle" });
            data.Edges.Add(new StepEditorEdgeData { FromId = "middle", ToId = "end" });

            StepGraphAutoLayout.ApplyLeftToRight(data, 240f, 160f);

            Assert.Less(data.GetNode("start").Position.x, data.GetNode("middle").Position.x);
            Assert.Less(data.GetNode("middle").Position.x, data.GetNode("end").Position.x);
        }
    }
}
```

`Assets/Tests/StepSystem/EditMode/StepEditorClipboardTests.cs`

```csharp
using LWStep.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// Graph 复制粘贴纯数据测试。
    /// </summary>
    public sealed class StepEditorClipboardTests
    {
        [Test]
        public void PasteNodes_ShouldCloneNodeAndInternalEdges()
        {
            StepEditorGraphData data = new StepEditorGraphData();

            StepEditorNodeData nodeA = new StepEditorNodeData { Id = "node_a", Position = Vector2.zero };
            StepEditorNodeData nodeB = new StepEditorNodeData { Id = "node_b", Position = new Vector2(200f, 0f) };
            data.Nodes.Add(nodeA);
            data.Nodes.Add(nodeB);
            data.Edges.Add(new StepEditorEdgeData { FromId = "node_a", ToId = "node_b", Priority = 10 });

            StepEditorClipboardPayload payload = StepEditorClipboard.Copy(data, new[] { "node_a", "node_b" });
            StepEditorClipboard.Paste(data, payload, new Vector2(40f, 20f));

            Assert.IsNotNull(data.GetNode("node_a_copy1"));
            Assert.IsNotNull(data.GetNode("node_b_copy1"));
            Assert.IsNotNull(data.GetEdge("node_a_copy1", "node_b_copy1"));
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认自动布局和剪贴板工具尚不存在**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter "StepGraphAutoLayoutTests|StepEditorClipboardTests"
```

Expected:
- 编译失败，提示 `StepGraphAutoLayout`、`StepEditorClipboard` 或 `StepEditorClipboardPayload` 不存在。

- [ ] **Step 3: 实现自动布局、复制粘贴、动作排序和分类选择器**

`Assets/LWFramework/Editor/StepSystem/Layout/StepGraphAutoLayout.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace LWStep.Editor
{
    /// <summary>
    /// DAG 横向布局工具。
    /// </summary>
    public static class StepGraphAutoLayout
    {
        public static void ApplyLeftToRight(StepEditorGraphData data, float xSpacing, float ySpacing)
        {
            Dictionary<string, int> depthMap = new Dictionary<string, int>();
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(data.StartNodeId);
            depthMap[data.StartNodeId] = 0;

            while (queue.Count > 0)
            {
                string nodeId = queue.Dequeue();
                int depth = depthMap[nodeId];
                for (int i = 0; i < data.Edges.Count; i++)
                {
                    StepEditorEdgeData edge = data.Edges[i];
                    if (edge.FromId != nodeId || depthMap.ContainsKey(edge.ToId))
                    {
                        continue;
                    }
                    depthMap[edge.ToId] = depth + 1;
                    queue.Enqueue(edge.ToId);
                }
            }

            Dictionary<int, int> rowMap = new Dictionary<int, int>();
            for (int i = 0; i < data.Nodes.Count; i++)
            {
                StepEditorNodeData node = data.Nodes[i];
                int depth = depthMap.TryGetValue(node.Id, out int mappedDepth) ? mappedDepth : 0;
                int row = rowMap.TryGetValue(depth, out int currentRow) ? currentRow : 0;
                node.Position = new Vector2(depth * xSpacing, row * ySpacing);
                rowMap[depth] = row + 1;
            }
        }
    }
}
```

`Assets/LWFramework/Editor/StepSystem/Clipboard/StepEditorClipboard.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace LWStep.Editor
{
    /// <summary>
    /// Graph 剪贴板数据。
    /// </summary>
    public sealed class StepEditorClipboardPayload
    {
        public List<StepEditorNodeData> Nodes = new List<StepEditorNodeData>();
        public List<StepEditorEdgeData> Edges = new List<StepEditorEdgeData>();
    }

    /// <summary>
    /// Graph 复制粘贴工具。
    /// </summary>
    public static class StepEditorClipboard
    {
        public static StepEditorClipboardPayload Copy(StepEditorGraphData data, string[] nodeIds)
        {
            HashSet<string> selected = new HashSet<string>(nodeIds);
            StepEditorClipboardPayload payload = new StepEditorClipboardPayload();

            for (int i = 0; i < data.Nodes.Count; i++)
            {
                StepEditorNodeData node = data.Nodes[i];
                if (!selected.Contains(node.Id))
                {
                    continue;
                }

                StepEditorNodeData clone = new StepEditorNodeData();
                clone.Id = node.Id;
                clone.Name = node.Name;
                clone.Mode = node.Mode;
                clone.Position = node.Position;
                for (int j = 0; j < node.Actions.Count; j++)
                {
                    StepEditorActionData actionClone = new StepEditorActionData();
                    actionClone.TypeName = node.Actions[j].TypeName;
                    for (int k = 0; k < node.Actions[j].Parameters.Count; k++)
                    {
                        StepEditorParameterData parameter = node.Actions[j].Parameters[k];
                        actionClone.Parameters.Add(new StepEditorParameterData { Key = parameter.Key, Value = parameter.Value });
                    }
                    clone.Actions.Add(actionClone);
                }
                payload.Nodes.Add(clone);
            }

            for (int i = 0; i < data.Edges.Count; i++)
            {
                StepEditorEdgeData edge = data.Edges[i];
                if (selected.Contains(edge.FromId) && selected.Contains(edge.ToId))
                {
                    payload.Edges.Add(new StepEditorEdgeData
                    {
                        FromId = edge.FromId,
                        ToId = edge.ToId,
                        Priority = edge.Priority,
                        ConditionKey = edge.ConditionKey,
                        ConditionComparisonType = edge.ConditionComparisonType,
                        ConditionValue = edge.ConditionValue,
                    });
                }
            }

            return payload;
        }

        public static void Paste(StepEditorGraphData data, StepEditorClipboardPayload payload, Vector2 offset)
        {
            Dictionary<string, string> idMap = new Dictionary<string, string>();
            for (int i = 0; i < payload.Nodes.Count; i++)
            {
                StepEditorNodeData source = payload.Nodes[i];
                string newId = source.Id + "_copy1";
                while (data.GetNode(newId) != null)
                {
                    newId += "_1";
                }
                idMap[source.Id] = newId;

                StepEditorNodeData clone = new StepEditorNodeData();
                clone.Id = newId;
                clone.Name = source.Name;
                clone.Mode = source.Mode;
                clone.Position = source.Position + offset;
                clone.Actions = new List<StepEditorActionData>();
                for (int j = 0; j < source.Actions.Count; j++)
                {
                    StepEditorActionData actionClone = new StepEditorActionData();
                    actionClone.TypeName = source.Actions[j].TypeName;
                    for (int k = 0; k < source.Actions[j].Parameters.Count; k++)
                    {
                        StepEditorParameterData parameter = source.Actions[j].Parameters[k];
                        actionClone.Parameters.Add(new StepEditorParameterData { Key = parameter.Key, Value = parameter.Value });
                    }
                    clone.Actions.Add(actionClone);
                }
                data.Nodes.Add(clone);
            }

            for (int i = 0; i < payload.Edges.Count; i++)
            {
                StepEditorEdgeData source = payload.Edges[i];
                data.Edges.Add(new StepEditorEdgeData
                {
                    FromId = idMap[source.FromId],
                    ToId = idMap[source.ToId],
                    Priority = source.Priority,
                    ConditionKey = source.ConditionKey,
                    ConditionComparisonType = source.ConditionComparisonType,
                    ConditionValue = source.ConditionValue,
                });
            }
        }
    }
}
```

在 `Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs` 中：

```csharp
            Button duplicateButton = new Button(OnDuplicateSelection) { text = "重复" };
            Button autoLayoutButton = new Button(OnAutoLayout) { text = "自动布局" };
            toolbar.Add(duplicateButton);
            toolbar.Add(autoLayoutButton);
```

```csharp
        /// <summary>
        /// 重复当前选中的节点。
        /// </summary>
        private void OnDuplicateSelection()
        {
            List<string> selectedNodeIds = new List<string>();
            for (int i = 0; i < m_GraphView.selection.Count; i++)
            {
                StepNodeView nodeView = m_GraphView.selection[i] as StepNodeView;
                if (nodeView != null)
                {
                    selectedNodeIds.Add(nodeView.Data.Id);
                }
            }

            if (selectedNodeIds.Count == 0)
            {
                return;
            }

            StepEditorClipboardPayload payload = StepEditorClipboard.Copy(m_Data, selectedNodeIds.ToArray());
            StepEditorClipboard.Paste(m_Data, payload, new Vector2(40f, 20f));
            LoadGraphData(m_Data);
            SaveUndoSnapshot("重复节点");
        }

        /// <summary>
        /// 对当前图执行自动布局。
        /// </summary>
        private void OnAutoLayout()
        {
            if (m_Data == null || m_Data.Nodes == null || m_Data.Nodes.Count == 0)
            {
                return;
            }

            StepGraphAutoLayout.ApplyLeftToRight(m_Data, 260f, 180f);
            LoadGraphData(m_Data);
            SaveUndoSnapshot("自动布局");
        }
```

```csharp
            List<string> options = new List<string>();
            List<string> optionTypes = new List<string>();
            options.Add("未选择");
            optionTypes.Add(string.Empty);

            for (int i = 0; i < s_ActionTypes.Length; i++)
            {
                Type type = s_ActionTypes[i];
                StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(type);
                options.Add(descriptor.Category + "/" + descriptor.DisplayName);
                optionTypes.Add(type.FullName);
            }
```

```csharp
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("上移"))
                    {
                        StepEditorActionData current = m_SelectedNode.Actions[i];
                        m_SelectedNode.Actions[i] = m_SelectedNode.Actions[i - 1];
                        m_SelectedNode.Actions[i - 1] = current;
                        SaveUndoSnapshot("动作上移");
                    }

                    GUI.enabled = i < m_SelectedNode.Actions.Count - 1;
                    if (GUILayout.Button("下移"))
                    {
                        StepEditorActionData current = m_SelectedNode.Actions[i];
                        m_SelectedNode.Actions[i] = m_SelectedNode.Actions[i + 1];
                        m_SelectedNode.Actions[i + 1] = current;
                        SaveUndoSnapshot("动作下移");
                    }
                    GUI.enabled = true;
                }
```

- [ ] **Step 4: 重新运行自动布局和剪贴板测试**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter "StepGraphAutoLayoutTests|StepEditorClipboardTests"
```

Expected:
- `StepGraphAutoLayoutTests` 和 `StepEditorClipboardTests` 全部 PASS。

- [ ] **Step 5: 提交编辑效率增强**

```bash
git add Assets/Tests/StepSystem/EditMode/StepGraphAutoLayoutTests.cs Assets/Tests/StepSystem/EditMode/StepEditorClipboardTests.cs Assets/LWFramework/Editor/StepSystem/Layout/StepGraphAutoLayout.cs Assets/LWFramework/Editor/StepSystem/Clipboard/StepEditorClipboard.cs Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs
git commit -m "feat: improve StepSystem editor productivity"
```

---

### Task 3: 升级 Graph 节点卡片与连线标签

**Files:**
- Create: `Assets/Tests/StepSystem/EditMode/StepGraphPresentationBuilderTests.cs`
- Create: `Assets/LWFramework/Editor/StepSystem/Presentation/StepNodePresentation.cs`
- Create: `Assets/LWFramework/Editor/StepSystem/Presentation/StepEdgePresentation.cs`
- Create: `Assets/LWFramework/Editor/StepSystem/Presentation/StepGraphPresentationBuilder.cs`
- Create: `Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphStyles.uss`
- Modify: `Assets/LWFramework/Editor/StepSystem/GraphView/StepNodeView.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs`

- [ ] **Step 1: 先写 Graph 展示模型测试**

`Assets/Tests/StepSystem/EditMode/StepGraphPresentationBuilderTests.cs`

```csharp
using LWStep;
using LWStep.Editor;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// 节点卡片与连线标签构建测试。
    /// </summary>
    public sealed class StepGraphPresentationBuilderTests
    {
        /// <summary>
        /// 验证节点卡片能生成开始节点标签、执行模式和动作摘要。
        /// </summary>
        [Test]
        public void BuildNodePresentation_ShouldExposeBadgesAndSummary()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.StartNodeId = "node_start";
            StepEditorNodeData node = new StepEditorNodeData();
            node.Id = "node_start";
            node.Name = "开始";
            node.Mode = StepNodeMode.Parallel;
            StepEditorActionData action = new StepEditorActionData();
            action.TypeName = typeof(StepMoveObjectAction).FullName;
            action.SetParameterValue("target", "Cube");
            node.Actions.Add(action);
            data.Nodes.Add(node);

            StepNodePresentation presentation = StepGraphPresentationBuilder.BuildNodePresentation(data, node, StepNodeStatus.Running, "StepMoveObjectAction", true);

            Assert.AreEqual("node_start", presentation.Title);
            Assert.AreEqual("开始", presentation.Subtitle);
            CollectionAssert.Contains(presentation.Badges, "Start");
            CollectionAssert.Contains(presentation.Badges, "Parallel");
            Assert.AreEqual("Move:Cube", presentation.ActionSummaries[0]);
            Assert.IsTrue(presentation.IsRunning);
            Assert.IsTrue(presentation.IsInTrail);
        }

        /// <summary>
        /// 验证连线标签能显示条件和优先级。
        /// </summary>
        [Test]
        public void BuildEdgePresentation_ShouldExposePriorityAndCondition()
        {
            StepEditorEdgeData edge = new StepEditorEdgeData();
            edge.FromId = "a";
            edge.ToId = "b";
            edge.Priority = 20;
            edge.ConditionKey = "mode";
            edge.ConditionComparisonType = ComparisonType.EqualTo;
            edge.ConditionValue = "A";

            StepEdgePresentation presentation = StepGraphPresentationBuilder.BuildEdgePresentation(edge, false);

            Assert.AreEqual("P20 | mode EqualTo A", presentation.Label);
            Assert.IsTrue(presentation.HasCondition);
            Assert.IsFalse(presentation.HasError);
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认当前缺少展示模型而失败**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepGraphPresentationBuilderTests
```

Expected:
- 编译失败，提示 `StepNodePresentation`、`StepEdgePresentation` 或 `StepGraphPresentationBuilder` 不存在。

- [ ] **Step 3: 实现 Graph 视图模型和基础卡片 UI**

`Assets/LWFramework/Editor/StepSystem/Presentation/StepNodePresentation.cs`

```csharp
using System.Collections.Generic;

namespace LWStep.Editor
{
    /// <summary>
    /// 节点卡片展示结果。
    /// </summary>
    public sealed class StepNodePresentation
    {
        public string Title;
        public string Subtitle;
        public List<string> Badges = new List<string>();
        public List<string> ActionSummaries = new List<string>();
        public bool IsRunning;
        public bool IsCompleted;
        public bool HasWarning;
        public bool HasError;
        public bool IsInTrail;
        public string CurrentActionName;
    }
}
```

`Assets/LWFramework/Editor/StepSystem/Presentation/StepEdgePresentation.cs`

```csharp
namespace LWStep.Editor
{
    /// <summary>
    /// 连线标签展示结果。
    /// </summary>
    public sealed class StepEdgePresentation
    {
        public string Label;
        public bool HasCondition;
        public bool HasError;
    }
}
```

`Assets/LWFramework/Editor/StepSystem/Presentation/StepGraphPresentationBuilder.cs`

```csharp
namespace LWStep.Editor
{
    /// <summary>
    /// Graph 展示结果构建器。
    /// </summary>
    public static class StepGraphPresentationBuilder
    {
        public static StepNodePresentation BuildNodePresentation(
            StepEditorGraphData graph,
            StepEditorNodeData node,
            StepNodeStatus status,
            string currentActionName,
            bool isInTrail)
        {
            StepNodePresentation presentation = new StepNodePresentation();
            presentation.Title = node.Id;
            presentation.Subtitle = string.IsNullOrEmpty(node.Name) ? "未命名节点" : node.Name;
            presentation.IsRunning = status == StepNodeStatus.Running;
            presentation.IsCompleted = status == StepNodeStatus.Completed;
            presentation.IsInTrail = isInTrail;
            presentation.CurrentActionName = currentActionName ?? string.Empty;

            if (graph != null && graph.StartNodeId == node.Id)
            {
                presentation.Badges.Add("Start");
            }

            presentation.Badges.Add(node.Mode == StepNodeMode.Parallel ? "Parallel" : "Serial");

            for (int i = 0; i < node.Actions.Count && i < 3; i++)
            {
                presentation.ActionSummaries.Add(StepActionDescriptorRegistry.BuildSummary(node.Actions[i].TypeName, node.Actions[i].Parameters));
            }

            if (node.Actions.Count > 3)
            {
                presentation.ActionSummaries.Add("+" + (node.Actions.Count - 3));
            }

            return presentation;
        }

        public static StepEdgePresentation BuildEdgePresentation(StepEditorEdgeData edge, bool hasError)
        {
            StepEdgePresentation presentation = new StepEdgePresentation();
            presentation.HasCondition = !string.IsNullOrEmpty(edge.ConditionKey);
            presentation.HasError = hasError;
            presentation.Label = presentation.HasCondition
                ? "P" + edge.Priority + " | " + edge.ConditionKey + " " + edge.ConditionComparisonType + " " + edge.ConditionValue
                : "P" + edge.Priority;
            return presentation;
        }
    }
}
```

`Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphStyles.uss`

```css
.step-node-subtitle {
    color: rgb(180, 180, 180);
    font-size: 11px;
    margin-bottom: 4px;
}

.step-node-badge {
    background-color: rgb(56, 56, 56);
    color: white;
    margin-right: 4px;
    padding-left: 4px;
    padding-right: 4px;
    border-radius: 3px;
}

.step-node-running {
    border-left-width: 4px;
    border-left-color: rgb(255, 200, 60);
}

.step-node-completed {
    border-left-width: 4px;
    border-left-color: rgb(90, 200, 120);
}

.step-node-trail {
    background-color: rgba(60, 140, 255, 0.08);
}

.step-edge-label {
    background-color: rgba(36, 36, 36, 0.92);
    color: white;
    padding-left: 4px;
    padding-right: 4px;
    border-radius: 3px;
    font-size: 10px;
}
```

把 `Assets/LWFramework/Editor/StepSystem/GraphView/StepNodeView.cs` 中构造和更新 UI 的部分替换为：

```csharp
        private Label m_SubtitleLabel;
        private VisualElement m_BadgeContainer;
        private VisualElement m_SummaryContainer;

        public StepNodeView(StepEditorNodeData data)
        {
            m_GraphView = GetFirstAncestorOfType<StepGraphView>();
            m_Data = data;
            title = data.Id;
            viewDataKey = data.Id;

            m_InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            m_InputPort.portName = "In";
            inputContainer.Add(m_InputPort);

            m_OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            m_OutputPort.portName = "Out";
            outputContainer.Add(m_OutputPort);

            m_SubtitleLabel = new Label();
            m_SubtitleLabel.AddToClassList("step-node-subtitle");
            extensionContainer.Add(m_SubtitleLabel);

            m_BadgeContainer = new VisualElement();
            m_BadgeContainer.style.flexDirection = FlexDirection.Row;
            extensionContainer.Add(m_BadgeContainer);

            m_SummaryContainer = new VisualElement();
            m_SummaryContainer.style.flexDirection = FlexDirection.Column;
            extensionContainer.Add(m_SummaryContainer);

            SetPosition(new Rect(data.Position, new Vector2(220.0f, 150.0f)));
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RefreshExpandedState();
            RefreshPorts();
        }

        public void BindPresentation(StepNodePresentation presentation)
        {
            title = presentation.Title;
            m_SubtitleLabel.text = presentation.Subtitle;
            m_BadgeContainer.Clear();
            for (int i = 0; i < presentation.Badges.Count; i++)
            {
                Label badge = new Label(presentation.Badges[i]);
                badge.AddToClassList("step-node-badge");
                m_BadgeContainer.Add(badge);
            }

            m_SummaryContainer.Clear();
            for (int i = 0; i < presentation.ActionSummaries.Count; i++)
            {
                m_SummaryContainer.Add(new Label(presentation.ActionSummaries[i]));
            }

            mainContainer.EnableInClassList("step-node-running", presentation.IsRunning);
            mainContainer.EnableInClassList("step-node-completed", presentation.IsCompleted);
            mainContainer.EnableInClassList("step-node-trail", presentation.IsInTrail);
        }
```

把 `Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs` 的 `ConfigureEdgeView` 和 `UpdateAllNodeTitles` 替换为：

```csharp
        private readonly HashSet<string> m_RuntimeTrailNodeIds = new HashSet<string>();

        public void SetRuntimeTrail(List<string> nodeIds)
        {
            m_RuntimeTrailNodeIds.Clear();
            if (nodeIds != null)
            {
                for (int i = 0; i < nodeIds.Count; i++)
                {
                    m_RuntimeTrailNodeIds.Add(nodeIds[i]);
                }
            }
            UpdateAllNodeTitles();
        }

        private void UpdateAllNodeTitles()
        {
            foreach (KeyValuePair<string, StepNodeView> kvp in m_NodeViews)
            {
                StepNodeStatus status = StepNodeStatus.Unfinished;
                if (m_RuntimeNodeStatuses != null)
                {
                    m_RuntimeNodeStatuses.TryGetValue(kvp.Key, out status);
                }

                StepEditorNodeData nodeData = kvp.Value.Data;
                StepNodePresentation presentation = StepGraphPresentationBuilder.BuildNodePresentation(
                    m_Data,
                    nodeData,
                    status,
                    string.Empty,
                    m_RuntimeTrailNodeIds.Contains(nodeData.Id));
                kvp.Value.BindPresentation(presentation);
            }
        }

        private void ConfigureEdgeView(Edge edge, StepEditorEdgeData edgeData)
        {
            if (edge == null || edgeData == null)
            {
                return;
            }

            edge.userData = edgeData;
            edge.Clear();
            StepEdgePresentation presentation = StepGraphPresentationBuilder.BuildEdgePresentation(edgeData, false);
            Label label = new Label(presentation.Label);
            label.AddToClassList("step-edge-label");
            edge.Add(label);
        }
```

- [ ] **Step 4: 再跑展示模型测试**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepGraphPresentationBuilderTests
```

Expected:
- `StepGraphPresentationBuilderTests` 全部 PASS。

- [ ] **Step 5: 提交 Graph 卡片与连线标签**

```bash
git add Assets/Tests/StepSystem/EditMode/StepGraphPresentationBuilderTests.cs Assets/LWFramework/Editor/StepSystem/Presentation/StepNodePresentation.cs Assets/LWFramework/Editor/StepSystem/Presentation/StepEdgePresentation.cs Assets/LWFramework/Editor/StepSystem/Presentation/StepGraphPresentationBuilder.cs Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphStyles.uss Assets/LWFramework/Editor/StepSystem/GraphView/StepNodeView.cs Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs
git commit -m "feat: upgrade StepSystem graph presentation"
```

---

## 文件结构与职责

### 新增文件

- `Assets/LWFramework/RunTime/StepSystem/Action/StepActionInfoAttribute.cs`
  - 为 `Action` 声明显示名、分类、摘要模板和关键词。
- `Assets/LWFramework/RunTime/StepSystem/Diagnostics/StepRuntimeDebugSnapshot.cs`
- `Assets/LWFramework/RunTime/StepSystem/Diagnostics/StepRuntimeDebugTracker.cs`
  - 运行时调试快照与轨迹记录器。
- `Assets/LWFramework/Editor/StepSystem/Metadata/StepActionDescriptor.cs`
- `Assets/LWFramework/Editor/StepSystem/Metadata/StepActionDescriptorRegistry.cs`
  - 编辑器动作描述和注册表。
- `Assets/LWFramework/Editor/StepSystem/Presentation/StepNodePresentation.cs`
- `Assets/LWFramework/Editor/StepSystem/Presentation/StepEdgePresentation.cs`
- `Assets/LWFramework/Editor/StepSystem/Presentation/StepGraphPresentationBuilder.cs`
- `Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphStyles.uss`
  - Graph 卡片和连线标签展示层。
- `Assets/LWFramework/Editor/StepSystem/Layout/StepGraphAutoLayout.cs`
- `Assets/LWFramework/Editor/StepSystem/Clipboard/StepEditorClipboard.cs`
- `Assets/LWFramework/Editor/StepSystem/Templates/StepExampleTemplateCatalog.cs`
  - 自动布局、剪贴板和示例模板目录。
- `Assets/LWFramework/RunTime/StepSystem/Action/StepWaitSecondsAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepSetContextValueAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepRemoveContextValueAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepDispatchEventAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepSetActiveAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepSetPositionAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepSetRotationAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepSetScaleAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepSetParentAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepDestroyTargetAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepInstantiatePrefabAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepPlayParticleAction.cs`
  - 首批通用动作。
- `Assets/Tests/StepSystem/EditMode/StepActionDescriptorRegistryTests.cs`
- `Assets/Tests/StepSystem/EditMode/StepXmlRoundTripTests.cs`
- `Assets/Tests/StepSystem/EditMode/StepGraphPresentationBuilderTests.cs`
- `Assets/Tests/StepSystem/EditMode/StepGraphAutoLayoutTests.cs`
- `Assets/Tests/StepSystem/EditMode/StepEditorClipboardTests.cs`
- `Assets/Tests/StepSystem/EditMode/StepRuntimeDebugTrackerTests.cs`
- `Assets/Tests/StepSystem/EditMode/StepWorkflowActionsTests.cs`
- `Assets/Tests/StepSystem/EditMode/StepObjectActionsTests.cs`
- `Assets/Tests/StepSystem/EditMode/StepExampleXmlTests.cs`
  - StepSystem 升级回归测试。
- `Assets/0Res/RawFiles/StepExamples/StepExample_BasicFlow.xml`
- `Assets/0Res/RawFiles/StepExamples/StepExample_ConditionBranch.xml`
- `Assets/0Res/RawFiles/StepExamples/StepExample_ParallelActions.xml`
- `Assets/0Res/RawFiles/StepExamples/StepExample_ContextOps.xml`
- `Assets/0Res/RawFiles/StepExamples/StepExample_ObjectControl.xml`
- `Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Context.xml`
- `Assets/0Res/RawFiles/StepExamples/StepExample_Actions_Object.xml`
- `Assets/0Res/RawFiles/StepExamples/StepExample_Actions_AudioFx.xml`
- `Assets/0Res/RawFiles/StepExamples/StepExample_Flow_TeachingDemo.xml`
- `Assets/0Res/RawFiles/StepExamples/StepExample_Flow_GeneralPipeline.xml`
  - 分层示例 XML。

### 修改文件

- `Assets/Tests/StepSystem/EditMode/LWFramework.Tests.StepSystem.EditMode.asmdef`
  - 让 StepSystem EditMode 测试同时引用运行时和编辑器程序集。
- `Assets/LWFramework/RunTime/StepSystem/Action/BaseStepAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepLogAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepWaitMouseLeftClickAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepMoveObjectAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepToggleObjectAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepPlayAudioAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepRotateObjectAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepScaleObjectAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepAnimationAction.cs`
- `Assets/LWFramework/RunTime/StepSystem/Action/StepLegacyAnimationAction.cs`
  - 为现有常用动作补齐显示元数据。
- `Assets/LWFramework/Editor/StepSystem/StepEditorGraphData.cs`
- `Assets/LWFramework/Editor/StepSystem/StepXmlImporter.cs`
- `Assets/LWFramework/Editor/StepSystem/StepXmlExporter.cs`
  - 实现旧格式导入兼容与新格式导出收口。
- `Assets/LWFramework/Editor/StepSystem/GraphView/StepNodeView.cs`
- `Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphView.cs`
- `Assets/LWFramework/Editor/StepSystem/StepEditorWindow.cs`
  - Graph 卡片视图、连线标签、编辑效率增强、联调面板与模板入口。
- `Assets/LWFramework/RunTime/Core/InterfaceManager/IStepManager.cs`
- `Assets/LWFramework/RunTime/StepSystem/StepManager.cs`
- `Assets/LWFramework/RunTime/StepSystem/Graph/StepNode.cs`
  - 对外暴露运行时调试快照。
- `Assets/LWFramework/Editor/StepSystem/StepSystem_总结与使用说明.md`
  - 更新升级后的使用说明。

---

### Task 1: 建立 Action 元数据地基

**Files:**
- Modify: `Assets/Tests/StepSystem/EditMode/LWFramework.Tests.StepSystem.EditMode.asmdef`
- Create: `Assets/Tests/StepSystem/EditMode/StepActionDescriptorRegistryTests.cs`
- Create: `Assets/LWFramework/RunTime/StepSystem/Action/StepActionInfoAttribute.cs`
- Create: `Assets/LWFramework/Editor/StepSystem/Metadata/StepActionDescriptor.cs`
- Create: `Assets/LWFramework/Editor/StepSystem/Metadata/StepActionDescriptorRegistry.cs`
- Modify: `Assets/LWFramework/RunTime/StepSystem/Action/BaseStepAction.cs`
- Modify: `Assets/LWFramework/RunTime/StepSystem/Action/StepLogAction.cs`
- Modify: `Assets/LWFramework/RunTime/StepSystem/Action/StepWaitMouseLeftClickAction.cs`
- Modify: `Assets/LWFramework/RunTime/StepSystem/Action/StepMoveObjectAction.cs`
- Modify: `Assets/LWFramework/RunTime/StepSystem/Action/StepToggleObjectAction.cs`
- Modify: `Assets/LWFramework/RunTime/StepSystem/Action/StepPlayAudioAction.cs`

- [ ] **Step 1: 先把测试程序集接到编辑器代码，再写失败测试**

`Assets/Tests/StepSystem/EditMode/LWFramework.Tests.StepSystem.EditMode.asmdef`

```json
{
  "name": "LWFramework.Tests.StepSystem.EditMode",
  "rootNamespace": "LWFramework.Tests.StepSystem.EditMode",
  "references": [
    "LWFramework.Runtime",
    "LWFramework.Editor"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "optionalUnityReferences": [
    "TestAssemblies"
  ],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`Assets/Tests/StepSystem/EditMode/StepActionDescriptorRegistryTests.cs`

```csharp
using System.Collections.Generic;
using LWStep;
using LWStep.Editor;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// StepActionDescriptorRegistry 元数据测试。
    /// </summary>
    public sealed class StepActionDescriptorRegistryTests
    {
        /// <summary>
        /// 验证注册表能读取动作显示信息和参数顺序。
        /// </summary>
        [Test]
        public void GetDescriptor_ForAnnotatedAction_ShouldExposeDisplayMetadata()
        {
            StepActionDescriptor descriptor = StepActionDescriptorRegistry.GetDescriptor(typeof(StepLogAction));

            Assert.IsNotNull(descriptor);
            Assert.AreEqual("输出日志", descriptor.DisplayName);
            Assert.AreEqual("调试", descriptor.Category);
            Assert.AreEqual("Log:{message}", descriptor.SummaryTemplate);
            Assert.AreEqual("message", descriptor.Parameters[0].Key);
        }

        /// <summary>
        /// 验证摘要模板能正确替换参数。
        /// </summary>
        [Test]
        public void BuildSummary_ForMoveAction_ShouldUseTargetParameter()
        {
            List<StepEditorParameterData> parameters = new List<StepEditorParameterData>();
            parameters.Add(new StepEditorParameterData { Key = "target", Value = "Cube" });
            parameters.Add(new StepEditorParameterData { Key = "moveTime", Value = "0.5" });

            string summary = StepActionDescriptorRegistry.BuildSummary(typeof(StepMoveObjectAction).FullName, parameters);

            Assert.AreEqual("Move:Cube", summary);
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认元数据基础设施尚不存在而失败**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepActionDescriptorRegistryTests
```

Expected:
- 编译失败，提示 `StepActionInfoAttribute`、`StepActionDescriptorRegistry` 或 `StepActionDescriptor` 不存在。

- [ ] **Step 3: 实现动作元数据与注册表最小闭环**

`Assets/LWFramework/RunTime/StepSystem/Action/StepActionInfoAttribute.cs`

```csharp
using System;

namespace LWStep
{
    /// <summary>
    /// 步骤动作展示元数据。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StepActionInfoAttribute : Attribute
    {
        public string DisplayName { get; private set; }
        public string Category { get; private set; }
        public string SummaryTemplate { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }

        /// <summary>
        /// 创建动作展示元数据。
        /// </summary>
        public StepActionInfoAttribute(string displayName, string category)
        {
            DisplayName = displayName;
            Category = category;
            SummaryTemplate = string.Empty;
            Description = string.Empty;
            Keywords = string.Empty;
        }
    }
}
```

把 `Assets/LWFramework/RunTime/StepSystem/Action/BaseStepAction.cs` 中 `StepParamAttribute` 替换为：

```csharp
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class StepParamAttribute : Attribute
    {
        public string Key { get; private set; }
        public string Label { get; set; }
        public int Order { get; set; }
        public bool IsAdvanced { get; set; }

        /// <summary>
        /// 创建参数绑定特性。
        /// </summary>
        public StepParamAttribute(string key)
        {
            Key = key;
            Label = string.Empty;
            Order = 0;
            IsAdvanced = false;
        }
    }
```

`Assets/LWFramework/Editor/StepSystem/Metadata/StepActionDescriptor.cs`

```csharp
using System.Collections.Generic;

namespace LWStep.Editor
{
    /// <summary>
    /// 编辑器动作描述。
    /// </summary>
    public sealed class StepActionDescriptor
    {
        public string TypeName;
        public string DisplayName;
        public string Category;
        public string Description;
        public string SummaryTemplate;
        public string Keywords;
        public List<StepActionParameterDescriptor> Parameters = new List<StepActionParameterDescriptor>();
    }

    /// <summary>
    /// 编辑器动作参数描述。
    /// </summary>
    public sealed class StepActionParameterDescriptor
    {
        public string Key;
        public string Label;
        public int Order;
        public bool IsAdvanced;
        public System.Type ValueType;
    }
}
```

`Assets/LWFramework/Editor/StepSystem/Metadata/StepActionDescriptorRegistry.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LWStep.Editor
{
    /// <summary>
    /// 动作描述缓存。
    /// </summary>
    public static class StepActionDescriptorRegistry
    {
        private static readonly Dictionary<string, StepActionDescriptor> s_Descriptors = new Dictionary<string, StepActionDescriptor>();

        public static StepActionDescriptor GetDescriptor(Type actionType)
        {
            if (actionType == null || string.IsNullOrEmpty(actionType.FullName))
            {
                return null;
            }

            if (s_Descriptors.TryGetValue(actionType.FullName, out StepActionDescriptor cached))
            {
                return cached;
            }

            StepActionDescriptor descriptor = BuildDescriptor(actionType);
            s_Descriptors[actionType.FullName] = descriptor;
            return descriptor;
        }

        public static string BuildSummary(string typeName, List<StepEditorParameterData> parameters)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return "未选择";
            }

            Type actionType = Type.GetType(typeName);
            if (actionType == null)
            {
                return typeName;
            }

            StepActionDescriptor descriptor = GetDescriptor(actionType);
            if (descriptor == null || string.IsNullOrEmpty(descriptor.SummaryTemplate))
            {
                return descriptor != null ? descriptor.DisplayName : actionType.Name;
            }

            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    StepEditorParameterData parameter = parameters[i];
                    if (parameter != null && !string.IsNullOrEmpty(parameter.Key))
                    {
                        values[parameter.Key] = parameter.Value ?? string.Empty;
                    }
                }
            }

            return Regex.Replace(descriptor.SummaryTemplate, "\\{([^\\}]+)\\}", match =>
            {
                string key = match.Groups[1].Value;
                return values.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value) ? value : "-";
            });
        }

        private static StepActionDescriptor BuildDescriptor(Type actionType)
        {
            StepActionInfoAttribute info = actionType.GetCustomAttribute<StepActionInfoAttribute>(false);
            List<StepParamBinding> bindings = StepUtility.CreateStepParamBindings(actionType);
            bindings.Sort((a, b) =>
            {
                StepParamAttribute left = GetParamAttribute(a);
                StepParamAttribute right = GetParamAttribute(b);
                return left.Order.CompareTo(right.Order);
            });

            StepActionDescriptor descriptor = new StepActionDescriptor();
            descriptor.TypeName = actionType.FullName;
            descriptor.DisplayName = info != null && !string.IsNullOrEmpty(info.DisplayName) ? info.DisplayName : actionType.Name;
            descriptor.Category = info != null && !string.IsNullOrEmpty(info.Category) ? info.Category : "未分类";
            descriptor.Description = info != null ? info.Description : string.Empty;
            descriptor.SummaryTemplate = info != null ? info.SummaryTemplate : string.Empty;
            descriptor.Keywords = info != null ? info.Keywords : string.Empty;

            for (int i = 0; i < bindings.Count; i++)
            {
                StepParamBinding binding = bindings[i];
                StepParamAttribute param = GetParamAttribute(binding);
                StepActionParameterDescriptor parameterDescriptor = new StepActionParameterDescriptor();
                parameterDescriptor.Key = binding.Key;
                parameterDescriptor.Label = !string.IsNullOrEmpty(param.Label) ? param.Label : binding.Key;
                parameterDescriptor.Order = param.Order;
                parameterDescriptor.IsAdvanced = param.IsAdvanced;
                parameterDescriptor.ValueType = binding.ValueType;
                descriptor.Parameters.Add(parameterDescriptor);
            }

            return descriptor;
        }

        private static StepParamAttribute GetParamAttribute(StepParamBinding binding)
        {
            if (binding.Field != null)
            {
                return Attribute.GetCustomAttribute(binding.Field, typeof(StepParamAttribute), true) as StepParamAttribute;
            }
            return Attribute.GetCustomAttribute(binding.Property, typeof(StepParamAttribute), true) as StepParamAttribute;
        }
    }
}
```

对以下文件增加动作级元数据和参数展示顺序：

```csharp
// Assets/LWFramework/RunTime/StepSystem/Action/StepLogAction.cs
[StepActionInfo("输出日志", "调试", SummaryTemplate = "Log:{message}", Description = "输出一条步骤日志", Keywords = "log debug message")]
public class StepLogAction : BaseStepAction
{
    [StepParam("message", Label = "日志内容", Order = 0)]
    private string m_Message = "步骤动作执行";
```

```csharp
// Assets/LWFramework/RunTime/StepSystem/Action/StepWaitMouseLeftClickAction.cs
[StepActionInfo("等待鼠标左键", "流程控制", SummaryTemplate = "Wait:MouseLeftClick", Description = "等待用户点击鼠标左键", Keywords = "wait mouse click input")]
public class StepWaitMouseLeftClickAction : BaseStepAction
{
```

```csharp
// Assets/LWFramework/RunTime/StepSystem/Action/StepMoveObjectAction.cs
[StepActionInfo("移动对象", "对象控制", SummaryTemplate = "Move:{target}", Description = "按时长移动目标对象", Keywords = "move transform tween")]
public class StepMoveObjectAction : BaseTargeStepAction, IStepBaselineStateAction
{
    [StepParam("x", Label = "X", Order = 1)] private float m_X;
    [StepParam("y", Label = "Y", Order = 2)] private float m_Y;
    [StepParam("z", Label = "Z", Order = 3)] private float m_Z;
    [StepParam("isLocal", Label = "局部坐标", Order = 4)] private bool m_IsLocal;
    [StepParam("moveTime", Label = "移动时长", Order = 5)] private float m_MoveTime;
```

```csharp
// Assets/LWFramework/RunTime/StepSystem/Action/StepToggleObjectAction.cs
[StepActionInfo("切换渲染显隐", "对象控制", SummaryTemplate = "Toggle:{target}", Description = "切换目标 Renderer.enabled", Keywords = "toggle renderer visible")]
public class StepToggleObjectAction : BaseTargeStepAction, IStepBaselineStateAction
{
    [StepParam("isActive", Label = "启用渲染", Order = 1)]
    private bool m_IsActive = true;
```

```csharp
// Assets/LWFramework/RunTime/StepSystem/Action/StepPlayAudioAction.cs
[StepActionInfo("播放音频", "音频", SummaryTemplate = "Audio:{clip}", Description = "播放指定音频资源", Keywords = "audio clip play")]
public class StepPlayAudioAction : BaseTargeStepAction, IStepBaselineStateAction
{
    [StepParam("clip", Label = "音频路径", Order = 0)] private string m_ClipPath;
    [StepParam("volume", Label = "音量", Order = 1)] private float m_Volume = -1f;
    [StepParam("isLoop", Label = "循环播放", Order = 2)] private bool m_IsLoop;
    [StepParam("fadeInSeconds", Label = "淡入时长", Order = 3)] private float m_FadeInSeconds;
```

- [ ] **Step 4: 重新运行元数据测试，确认通过**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepActionDescriptorRegistryTests
```

Expected:
- `StepActionDescriptorRegistryTests` 全部 PASS。

- [ ] **Step 5: 提交元数据地基**

```bash
git add Assets/Tests/StepSystem/EditMode/LWFramework.Tests.StepSystem.EditMode.asmdef Assets/Tests/StepSystem/EditMode/StepActionDescriptorRegistryTests.cs Assets/LWFramework/RunTime/StepSystem/Action/StepActionInfoAttribute.cs Assets/LWFramework/Editor/StepSystem/Metadata/StepActionDescriptor.cs Assets/LWFramework/Editor/StepSystem/Metadata/StepActionDescriptorRegistry.cs Assets/LWFramework/RunTime/StepSystem/Action/BaseStepAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepLogAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepWaitMouseLeftClickAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepMoveObjectAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepToggleObjectAction.cs Assets/LWFramework/RunTime/StepSystem/Action/StepPlayAudioAction.cs
git commit -m "feat: add StepSystem action metadata foundation"
```

---

### Task 2: 收口 XML 归一化模型

**Files:**
- Create: `Assets/Tests/StepSystem/EditMode/StepXmlRoundTripTests.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/StepEditorGraphData.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/StepXmlImporter.cs`
- Modify: `Assets/LWFramework/Editor/StepSystem/StepXmlExporter.cs`

- [ ] **Step 1: 先写 XML 导入导出失败测试**

`Assets/Tests/StepSystem/EditMode/StepXmlRoundTripTests.cs`

```csharp
using LWStep.Editor;
using NUnit.Framework;

namespace LWFramework.Tests.StepSystem.EditMode
{
    /// <summary>
    /// Step XML 兼容与收口测试。
    /// </summary>
    public sealed class StepXmlRoundTripTests
    {
        /// <summary>
        /// 验证导入器能读取 graph.id 和旧 action 属性参数。
        /// </summary>
        [Test]
        public void LoadFromText_ShouldReadGraphIdAndInlineActionAttributes()
        {
            string xml = "<graph id=\"demo_graph\" start=\"node_start\"><nodes><node id=\"node_start\" name=\"开始\" x=\"0\" y=\"0\"><actions><action type=\"LWStep.StepLogAction\" message=\"hello\" /></actions></node></nodes><edges /></graph>";

            StepEditorGraphData data = StepXmlImporter.LoadFromText(xml);

            Assert.IsNotNull(data);
            Assert.AreEqual("demo_graph", data.GraphId);
            Assert.AreEqual("hello", data.Nodes[0].Actions[0].GetParameterValue("message"));
        }

        /// <summary>
        /// 验证导出器会统一输出 param 节点。
        /// </summary>
        [Test]
        public void ExportToText_ShouldAlwaysWriteParamNodes()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            data.GraphId = "demo_graph";
            data.StartNodeId = "node_start";

            StepEditorNodeData node = new StepEditorNodeData();
            node.Id = "node_start";
            node.Name = "开始";
            StepEditorActionData action = new StepEditorActionData();
            action.TypeName = "LWStep.StepLogAction";
            action.SetParameterValue("message", "hello");
            node.Actions.Add(action);
            data.Nodes.Add(node);

            string xml = StepXmlExporter.ExportToText(data);

            StringAssert.Contains("<graph id=\"demo_graph\" start=\"node_start\">", xml);
            StringAssert.Contains("<param key=\"message\" value=\"hello\" />", xml);
            StringAssert.DoesNotContain("message=\"hello\"", xml);
        }
    }
}
```

- [ ] **Step 2: 运行测试，确认当前缺少 GraphId 和参数辅助方法而失败**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepXmlRoundTripTests
```

Expected:
- 编译失败或测试失败，提示 `GraphId`、`GetParameterValue` 或 `SetParameterValue` 不存在。

- [ ] **Step 3: 给编辑器数据模型补齐 GraphId 和参数操作能力**

把 `Assets/LWFramework/Editor/StepSystem/StepEditorGraphData.cs` 改成：

```csharp
using System;
using System.Collections.Generic;
using LWStep;
using UnityEngine;

namespace LWStep.Editor
{
    [Serializable]
    public class StepEditorGraphData
    {
        public string GraphId;
        public string StartNodeId;
        public List<StepEditorNodeData> Nodes;
        public List<StepEditorEdgeData> Edges;

        public StepEditorGraphData()
        {
            GraphId = string.Empty;
            StartNodeId = string.Empty;
            Nodes = new List<StepEditorNodeData>();
            Edges = new List<StepEditorEdgeData>();
        }

        public StepEditorNodeData GetNode(string nodeId)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Id == nodeId)
                {
                    return Nodes[i];
                }
            }
            return null;
        }

        public StepEditorEdgeData GetEdge(string fromId, string toId)
        {
            for (int i = 0; i < Edges.Count; i++)
            {
                StepEditorEdgeData edge = Edges[i];
                if (edge.FromId == fromId && edge.ToId == toId)
                {
                    return edge;
                }
            }
            return null;
        }
    }

    [Serializable]
    public class StepEditorNodeData
    {
        public string Id;
        public string Name;
        public Vector2 Position;
        public StepNodeMode Mode;
        public List<StepEditorActionData> Actions;

        public StepEditorNodeData()
        {
            Id = string.Empty;
            Name = string.Empty;
            Position = Vector2.zero;
            Mode = StepNodeMode.Serial;
            Actions = new List<StepEditorActionData>();
        }
    }

    [Serializable]
    public class StepEditorEdgeData
    {
        public string FromId;
        public string ToId;
        public int Priority;
        public string ConditionKey;
        public ComparisonType ConditionComparisonType;
        public string ConditionValue;
    }

    [Serializable]
    public class StepEditorActionData
    {
        public string TypeName;
        public List<StepEditorParameterData> Parameters;

        public StepEditorActionData()
        {
            TypeName = string.Empty;
            Parameters = new List<StepEditorParameterData>();
        }

        public string GetParameterValue(string key, string defaultValue = "")
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                StepEditorParameterData parameter = Parameters[i];
                if (parameter != null && parameter.Key == key)
                {
                    return parameter.Value ?? string.Empty;
                }
            }
            return defaultValue;
        }

        public void SetParameterValue(string key, string value)
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                StepEditorParameterData parameter = Parameters[i];
                if (parameter != null && parameter.Key == key)
                {
                    parameter.Value = value ?? string.Empty;
                    return;
                }
            }

            Parameters.Add(new StepEditorParameterData
            {
                Key = key,
                Value = value ?? string.Empty,
            });
        }
    }

    [Serializable]
    public class StepEditorParameterData
    {
        public string Key;
        public string Value;
    }
}
```

把 `Assets/LWFramework/Editor/StepSystem/StepXmlImporter.cs` 中 graph 读取和参数读取替换为：

```csharp
            StepEditorGraphData data = new StepEditorGraphData();
            data.GraphId = GetAttr(graphElement, "id");
            data.StartNodeId = GetAttr(graphElement, "start");
```

以及：

```csharp
                            actionData.TypeName = GetAttr(actionElement, "type");

                            if (actionElement.HasAttributes)
                            {
                                for (int k = 0; k < actionElement.Attributes.Count; k++)
                                {
                                    XmlAttribute attr = actionElement.Attributes[k];
                                    if (attr.Name == "type")
                                    {
                                        continue;
                                    }
                                    actionData.SetParameterValue(attr.Name, attr.Value);
                                }
                            }

                            XmlNodeList paramList = actionElement.SelectNodes("param");
                            for (int k = 0; k < paramList.Count; k++)
                            {
                                XmlElement paramElement = paramList[k] as XmlElement;
                                if (paramElement == null)
                                {
                                    continue;
                                }
                                string key = GetAttr(paramElement, "key");
                                string value = GetAttr(paramElement, "value");
                                if (!string.IsNullOrEmpty(key))
                                {
                                    actionData.SetParameterValue(key, value);
                                }
                            }
```

把 `Assets/LWFramework/Editor/StepSystem/StepXmlExporter.cs` 的 graph 头和 action 参数导出替换为：

```csharp
            graphElement.SetAttribute("id", data.GraphId ?? string.Empty);
            graphElement.SetAttribute("start", data.StartNodeId ?? string.Empty);
```

以及：

```csharp
                    for (int k = 0; k < action.Parameters.Count; k++)
                    {
                        StepEditorParameterData parameter = action.Parameters[k];
                        if (parameter == null || string.IsNullOrEmpty(parameter.Key))
                        {
                            continue;
                        }

                        XmlElement paramElement = doc.CreateElement("param");
                        paramElement.SetAttribute("key", parameter.Key);
                        paramElement.SetAttribute("value", parameter.Value ?? string.Empty);
                        actionElement.AppendChild(paramElement);
                    }
```

- [ ] **Step 4: 重新运行 XML 收口测试**

Run:

```bash
dotnet test LWFramework.Tests.StepSystem.EditMode.csproj --filter StepXmlRoundTripTests
```

Expected:
- `StepXmlRoundTripTests` 全部 PASS。

- [ ] **Step 5: 提交 XML 归一化模型**

```bash
git add Assets/Tests/StepSystem/EditMode/StepXmlRoundTripTests.cs Assets/LWFramework/Editor/StepSystem/StepEditorGraphData.cs Assets/LWFramework/Editor/StepSystem/StepXmlImporter.cs Assets/LWFramework/Editor/StepSystem/StepXmlExporter.cs
git commit -m "feat: normalize StepSystem XML editor model"
```

---
