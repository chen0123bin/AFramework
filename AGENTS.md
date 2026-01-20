# AGENTS

<skills_system priority="1">

## Available Skills

<!-- SKILLS_TABLE_START -->
<usage>
When users ask you to perform tasks, check if any of the available skills below can help complete the task more effectively. Skills provide specialized capabilities and domain knowledge.

How to use skills:
- Invoke: Bash("openskills read <skill-name>")
- The skill content will load with detailed instructions on how to complete the task
- Base directory provided in output for resolving bundled resources (references/, scripts/, assets/)

Usage notes:
- Only use skills listed in <available_skills> below
- Do not invoke a skill that is already loaded in your context
- Each skill invocation is stateless
</usage>

<available_skills>

<skill>
<name>lwframework</name>
<description>LWFramework 管理器用法速查（ManagerUtility.*Mgr）。适用于：资源加载/实例化/场景/下载更新（IAssetsManager）、事件监听与派发（IEventManager）、UI 打开关闭与预加载（IUIManager）、热更加载与反射调用（IHotfixManager）、Procedure/FSM 切换（IFSMManager）、音频播放控制（IAudioManager）、对象池（GameObjectPool）等。重点用法集中在 references/examples.md。</description>
<location>project</location>
</skill>

<skill>
<name>planning-with-files</name>
<description>当用户提出“复杂任务规划”“需要文件化计划”“创建 task_plan.md / findings.md / progress.md”“多步骤研究或开发”“/clear 后继续并恢复上下文”等请求时使用。本技能实现 Manus 风格的文件化规划。</description>
<location>project</location>
</skill>

<skill>
<name>skill-creator</name>
<description>Guide for creating effective skills. This skill should be used when users want to create a new skill (or update an existing skill) that extends Claude's capabilities with specialized knowledge, workflows, or tool integrations.</description>
<location>project</location>
</skill>

<skill>
<name>skill-development</name>
<description>This skill should be used when the user wants to "create a skill", "add a skill to plugin", "write a new skill", "improve skill description", "organize skill content", or needs guidance on skill structure, progressive disclosure, or skill development best practices for Claude Code plugins.</description>
<location>project</location>
</skill>

<skill>
<name>uguitoolkit</name>
<description>Unity UGUI 结构 JSON 生成工具箱。适用于把界面设计、布局适配、交互反馈落到一个可存储的 UGUI 结构 JSON（严格遵循 assets/templates/UGUITempView.json 规则），并在 Unity 中根据该 JSON 自动创建 UGUI 层级与组件（Canvas、RectTransform、Image/Text/Button/InputField/ScrollRect 等）。当用户提出“生成/保存 UGUI 结构 JSON”“按模板自动搭 UI JSON”“按 UGUITempView 规则自动创建 UGUI JSON”的需求时使用。</description>
<location>project</location>
</skill>

</available_skills>
<!-- SKILLS_TABLE_END -->

</skills_system>
