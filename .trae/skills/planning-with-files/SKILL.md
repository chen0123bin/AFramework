---
name: planning-with-files
description: 当用户提出“复杂任务规划”“需要文件化计划”“创建 task_plan.md / findings.md / progress.md”“多步骤研究或开发”“/clear 后继续并恢复上下文”等请求时使用。本技能实现 Manus 风格的文件化规划。
---

# 使用文件进行规划

以 Manus 风格工作：使用持久化的 Markdown 文件作为“磁盘上的工作记忆”。

## 第一步：检查是否有上次会话 (v2.2.0)

开始工作前，检查上次会话是否存在未同步的上下文：

```bash
# Claude Code users
python3 ~/.claude/skills/planning-with-files/scripts/session-catchup.py "$(pwd)"

# Codex users
python3 ~/.codex/skills/planning-with-files/scripts/session-catchup.py "$(pwd)"

# Cursor users
python3 ~/.cursor/skills/planning-with-files/scripts/session-catchup.py "$(pwd)"
```

若 catchup 报告显示未同步上下文：
1. 运行 `git diff --stat` 查看实际代码变更
2. 读取当前规划文件
3. 基于 catchup + git diff 更新规划文件
4. 再继续任务

## 重要：文件放置位置

**模板位置（按 IDE）:**
- Claude Code: `~/.claude/skills/planning-with-files/templates/`
- Codex: `~/.codex/skills/planning-with-files/templates/`
- Cursor: `~/.cursor/skills/planning-with-files/templates/`

**规划文件**放在**项目目录**

| 位置 | 存放内容 |
|----------|-----------------|
| 技能目录（`~/.claude/skills/planning-with-files/` 或 `~/.codex/skills/planning-with-files/`） | 模板、脚本、参考资料 |
| 项目目录 | `task_plan.md`、`findings.md`、`progress.md` |

## 快速开始

在任何复杂任务前：

1. **创建 `task_plan.md`** — 参考 [templates/task_plan.md](templates/task_plan.md)
2. **创建 `findings.md`** — 参考 [templates/findings.md](templates/findings.md)
3. **创建 `progress.md`** — 参考 [templates/progress.md](templates/progress.md)
4. **决策前重读计划** — 保持目标在注意窗口
5. **每阶段后更新** — 标记完成，记录错误

> **注意：** 规划文件放在项目根目录，不放在技能安装目录。

## 核心模式

```
上下文窗口 = RAM（易失、有限）
文件系统 = 磁盘（持久、无限）

→ 任何重要信息都写入磁盘
```

## 文件用途

| 文件 | 用途 | 更新时机 |
|------|---------|----------------|
| `task_plan.md` | 阶段、进度、决策 | 每阶段后 |
| `findings.md` | 研究与发现 | 每次发现后 |
| `progress.md` | 会话日志、测试结果 | 全程更新 |

## 关键规则

### 1. 先创建计划
不要在没有 `task_plan.md` 的情况下开始复杂任务。不可协商。

### 2. 两次操作规则
> “每完成 2 次 view/browser/search 操作，立即把关键发现写入文本文件。”

避免视觉/多模态信息丢失。

### 3. 先读再决策
在重大决策前读取计划文件，保持目标在注意窗口。

### 4. 行动后更新
完成任一阶段后：
- 标记阶段状态：`in_progress` → `complete`
- 记录遇到的错误
- 记录创建/修改的文件

### 5. 记录所有错误
把所有错误写入计划文件，沉淀知识，避免重复。

```markdown
## 遇到的错误
| 错误 | 尝试 | 解决方案 |
|-------|---------|------------|
| FileNotFoundError | 1 | 创建默认配置 |
| API timeout | 2 | 增加重试逻辑 |
```

### 6. 不重复失败
```
if action_failed:
    next_action != same_action
```
跟踪尝试记录，调整方案。

## 三次失败处理流程

```
尝试 1：诊断并修复
  → 仔细阅读错误
  → 定位根因
  → 采取针对性修复

尝试 2：替代方案
  → 仍是同一错误？改用不同方法
  → 换工具？换库？
  → 绝不重复完全相同的失败动作

尝试 3：更广泛重思
  → 质疑假设
  → 搜索解决方案
  → 考虑更新计划

三次失败后：升级到用户
  → 说明已尝试内容
  → 共享具体错误
  → 请求指导
```

## 读取 vs 写入 决策矩阵

| 情况 | 动作 | 原因 |
|-----------|--------|--------|
| 刚写完文件 | 不要读 | 内容仍在上下文中 |
| 查看图片/PDF | 立即写入发现 | 多模态需先转为文本 |
| 浏览器返回数据 | 写入文件 | 截图不会持久存在 |
| 开始新阶段 | 读取计划/发现 | 重新定位目标 |
| 出现错误 | 读取相关文件 | 需要最新状态修复 |
| 间隔后恢复 | 读取全部规划文件 | 恢复上下文 |

## 5 问题重启测试

若能回答这些问题，说明上下文管理稳定：

| 问题 | 答案来源 |
|----------|---------------|
| 我在哪？ | task_plan.md 当前阶段 |
| 我去哪？ | 剩余阶段 |
| 目标是什么？ | 计划中的目标声明 |
| 我学到了什么？ | findings.md |
| 我做了什么？ | progress.md |

## 何时使用该模式

**适用：**
- 多步骤任务（3+ 步）
- 研究任务
- 构建/创建项目
- 需要大量工具调用的任务
- 需要组织与跟踪的任务

**不适用：**
- 简单问题
- 单文件修改
- 快速查询

## 模板

复制这些模板作为起点：

- [templates/task_plan.md](templates/task_plan.md) — 阶段跟踪
- [templates/findings.md](templates/findings.md) — 研究沉淀
- [templates/progress.md](templates/progress.md) — 会话记录

## 脚本

自动化辅助脚本：

- `scripts/init-session.sh` — 初始化规划文件
- `scripts/check-complete.sh` — 校验阶段是否全部完成
- `scripts/session-catchup.py` — 恢复上次会话上下文 (v2.2.0)

## 高级主题

- **Manus 原则：** 见 [reference.md](reference.md)
- **真实示例：** 见 [examples.md](examples.md)

## 反模式

| 不要 | 替代做法 |
|-------|------------|
| 用 TodoWrite 做持久化 | 创建 task_plan.md 文件 |
| 只说一次目标就忘记 | 决策前重读计划 |
| 隐藏错误并无声重试 | 把错误写入计划文件 |
| 把所有内容塞进上下文 | 将大内容写入文件 |
| 立刻开始执行 | 先创建计划文件 |
| 重复失败动作 | 记录尝试并调整方案 |
| 在技能目录创建文件 | 在项目目录创建文件 |