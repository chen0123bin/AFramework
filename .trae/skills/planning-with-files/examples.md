# 示例：文件化规划的实践

## 示例 1：研究型任务

**用户请求：**「研究晨练的好处并写一份总结」

### 循环 1：创建计划
```bash
Write task_plan.md
```

```markdown
# 任务计划：晨练益处研究

## 目标
撰写一份关于晨练益处的研究总结。

## 阶段
- [ ] 阶段 1：创建此计划 ✓
- [ ] 阶段 2：搜索并收集资料
- [ ] 阶段 3：综合研究发现
- [ ] 阶段 4：交付总结

## 关键问题
1. 身体健康的益处有哪些？
2. 心理健康的益处有哪些？
3. 有哪些科学研究支持？

## 状态
**当前在阶段 1** - 正在创建计划
```

### 循环 2：研究
```bash
Read task_plan.md           # 刷新目标
WebSearch "morning exercise benefits"
Write notes.md              # 存储研究发现
Edit task_plan.md           # 标记阶段 2 完成
```

### 循环 3：综合
```bash
Read task_plan.md           # 刷新目标
Read notes.md               # 获取研究发现
Write morning_exercise_summary.md
Edit task_plan.md           # 标记阶段 3 完成
```

### 循环 4：交付
```bash
Read task_plan.md           # 验证已完成
Deliver morning_exercise_summary.md
```

---

## 示例 2：修复缺陷任务

**用户请求：**「修复认证模块中的登录缺陷」

### task_plan.md
```markdown
# 任务计划：修复登录缺陷

## 目标
定位并修复导致登录失败的缺陷。

## 阶段
- [x] 阶段 1：理解缺陷报告 ✓
- [x] 阶段 2：定位相关代码 ✓
- [ ] 阶段 3：识别根因（当前）
- [ ] 阶段 4：实施修复
- [ ] 阶段 5：测试与验证

## 关键问题
1. 出现了什么错误信息？
2. 哪个文件处理认证？
3. 最近有哪些变更？

## 已做决策
- 认证处理器位于 src/auth/login.ts
- 错误发生在 validateToken() 函数

## 遇到的错误
- [初始] TypeError: Cannot read property 'token' of undefined
  → 根因：user 对象未正确 await

## 状态
**当前在阶段 3** - 已找到根因，准备修复
```

---

## 示例 3：功能开发

**用户请求：**「为设置页添加深色模式切换」

### 三文件模式实践

**task_plan.md：**
```markdown
# 任务计划：深色模式切换

## 目标
在设置中添加可用的深色模式切换。

## 阶段
- [x] 阶段 1：研究现有主题系统 ✓
- [x] 阶段 2：设计实现方案 ✓
- [ ] 阶段 3：实现切换组件（当前）
- [ ] 阶段 4：加入主题切换逻辑
- [ ] 阶段 5：测试与打磨

## 已做决策
- 使用 CSS 自定义属性实现主题
- 使用 localStorage 存储偏好
- 切换组件位于 SettingsPage.tsx

## 状态
**当前在阶段 3** - 构建切换组件
```

**notes.md：**
```markdown
# 笔记：深色模式实现

## 现有主题系统
- 位置：src/styles/theme.ts
- 使用：CSS 自定义属性
- 当前主题：仅 light

## 需要修改的文件
1. src/styles/theme.ts - 添加深色主题颜色
2. src/components/SettingsPage.tsx - 添加切换组件
3. src/hooks/useTheme.ts - 创建新 Hook
4. src/App.tsx - 用 ThemeProvider 包裹

## 颜色决策
- 深色背景：#1a1a2e
- 深色表面：#16213e
- 深色文本：#eaeaea
```

**dark_mode_implementation.md：**（交付物）
```markdown
# 深色模式实现

## 变更内容

### 1. 添加深色主题颜色
文件：src/styles/theme.ts
...

### 2. 创建 useTheme Hook
文件：src/hooks/useTheme.ts
...
```

---

## 示例 4：错误恢复模式

遇到失败时，不要隐藏：

### 之前（错误做法）
```
Action: Read config.json
Error: File not found
Action: Read config.json  # 静默重试
Action: Read config.json  # 再次重试
```

### 之后（正确做法）
```
Action: Read config.json
Error: File not found

# 更新 task_plan.md：
## 遇到的错误
- 找不到 config.json → 将创建默认配置

Action: Write config.json (默认配置)
Action: Read config.json
Success!
```

---

## 决策前先读计划模式

**在重大决策前总是读取计划文件：**

```
[已经进行了很多工具调用...]
[上下文变得很长...]
[最初目标可能被遗忘...]

→ Read task_plan.md          # 这会把目标重新带到注意窗口！
→ 现在再做决定               # 目标在上下文中是新鲜的
```

这就是 Manus 能在约 50 次工具调用后仍不迷失的原因。计划文件充当“目标刷新”机制。
