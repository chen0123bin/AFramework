---
alwaysApply: true
---
使用Debug输出日志时，使用中文描述

生成代码时必须添加函数级中文注释

变量类型
规则:
不使用var关键字，而是使用具体的类型。

变量命名 (Variable Naming)
规则:
成员变量: 使用 驼峰命名法 (camelCase)，不过首字母加上m_,例如m_Name。
通用变量: 使用 驼峰命名法 (camelCase)。
常量: 使用 全大写蛇形命名法 (UPPER_SNAKE_CASE)。
布尔值: 前缀应使用 is, has, can, should 等，使其语义清晰。
集合 / 数组: 使用复数形式。

函数 / 方法命名 (Function / Method Naming)
规则:
通用函数 / 方法: 使用 驼峰命名法 (CamelCase)。
命名风格: 采用 动词或动宾短语，清晰地表达其执行的操作。
Getter/Setter:
Getter: Get + 属性名 (如 GetName())。
Setter: Set + 属性名 (如 SetName(name))。
布尔属性的 Getter: Is + 属性名 (如 IsEnabled())。

类 / 接口命名 (Class / Interface Naming)
规则:
类名: 使用 帕斯卡命名法 (PascalCase)。
接口名:
推荐 (C#/TypeScript): 使用 帕斯卡命名法 (PascalCase)，并以 I 为前缀（如 IUserRepository）。
命名风格: 采用 名词或名词短语，代表一个对象或概念。