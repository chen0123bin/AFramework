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
<name>lwframework</name>
<description>LWFramework 运行时核心接口速查与启动辅助。适用于：LWFramework 启动注册/初始化、ManagerUtility.*Mgr 返回 default 或告警排查、Procedure 流程状态机（IFSMManager）启动与切换、查询 IAssetsManager/IEventManager/IUIManager/IHotfixManager/IFSMManager/IManager 的职责与调用方式。业务代码优先通过 ManagerUtility.AssetsMgr/EventMgr/UIMgr/HotfixMgr/FSMMgr 调用能力，并对照 Assets/LWFramework/RunTime/Core/InterfaceManager 下接口核对签名与生命周期。</description>
<location>project</location>
</skill>

<skill>
<name>skill-creator</name>
<description>Guide for creating effective skills. This skill should be used when users want to create a new skill (or update an existing skill) that extends Claude's capabilities with specialized knowledge, workflows, or tool integrations.</description>
<location>project</location>
</skill>

<skill>
<name>uguitoolkit</name>
<description>Unity UGUI 结构 JSON 生成工具箱。适用于把界面设计、布局适配、交互反馈落到一个可存储的 UGUI 结构 JSON（严格遵循 references/UGUITempView.json 规则），并在 Unity 中根据该 JSON 自动创建 UGUI 层级与组件（Canvas、RectTransform、Image/Text/Button/InputField/ScrollRect 等）。当用户提出“生成/保存 UGUI 结构 JSON”“按模板自动搭 UI  JSON”“按 UGUITempView 规则自动创建 UGUI JSON”的需求时使用。。</description>
<location>project</location>
</skill>

<skill>
<name>ui-ux-pro-max</name>
<description>"UI/UX design intelligence. 50 styles, 21 palettes, 50 font pairings, 20 charts, 9 stacks (React, Next.js, Vue, Svelte, SwiftUI, React Native, Flutter, Tailwind, shadcn/ui). Actions: plan, build, create, design, implement, review, fix, improve, optimize, enhance, refactor, check UI/UX code. Projects: website, landing page, dashboard, admin panel, e-commerce, SaaS, portfolio, blog, mobile app, .html, .tsx, .vue, .svelte. Elements: button, modal, navbar, sidebar, card, table, form, chart. Styles: glassmorphism, claymorphism, minimalism, brutalism, neumorphism, bento grid, dark mode, responsive, skeuomorphism, flat design. Topics: color palette, accessibility, animation, layout, typography, font pairing, spacing, hover, shadow, gradient. Integrations: shadcn/ui MCP for component search and examples."</description>
<location>project</location>
</skill>

</available_skills>
<!-- SKILLS_TABLE_END -->

</skills_system>
