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
<name>canvas-design</name>
<description>Create beautiful visual art in .png and .pdf documents using design philosophy. You should use this skill when the user asks to create a poster, piece of art, design, or other static piece. Create original visual designs, never copying existing artists' work to avoid copyright violations.</description>
<location>project</location>
</skill>

<skill>
<name>skill-creator</name>
<description>Guide for creating effective skills. This skill should be used when users want to create a new skill (or update an existing skill) that extends Claude's capabilities with specialized knowledge, workflows, or tool integrations.</description>
<location>project</location>
</skill>

<skill>
<name>uguitoolkit</name>
<description>Unity UGUI 结构 JSON 生成与自动搭建工具箱。适用于把界面设计、布局适配、交互反馈落到一个可存储的 UGUI 结构 JSON（严格遵循 references/UGUITempView.json 规则），并在 Unity 中根据该 JSON 自动创建 UGUI 层级与组件（Canvas、RectTransform、Image/Text/Button/InputField/ScrollRect 等）。当用户提出“生成/保存 UGUI 结构 JSON”“按模板自动搭 UI”“按 UGUITempView 规则自动创建 UGUI”的需求时使用。</description>
<location>project</location>
</skill>

</available_skills>
<!-- SKILLS_TABLE_END -->

</skills_system>
