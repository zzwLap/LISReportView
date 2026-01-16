# 前端时区展示统一设置指南

## 概述

本文档介绍如何在前端统一设置时区展示，确保用户在其本地时区中正确显示时间信息。

## 实现原理

前端时区处理采用以下策略：
- 后端始终存储和传输UTC时间
- 前端根据用户设备的本地时区进行时间转换和展示
- 提供统一的JavaScript库处理所有时区转换

## 使用方法

### 1. 引入资源文件

在需要时区处理的页面中引入以下文件：

```html
<link rel="stylesheet" href="/css/timezone-styles.css" />
<script src="/js/timezone-helper.js"></script>
```

### 2. HTML标记约定

使用特定的HTML属性来标识需要转换的时间元素：

```html
<!-- 基本用法 -->
<span data-utc-time="2023-12-25T10:30:00Z">Loading...</span>

<!-- 指定格式 -->
<span data-utc-time="2023-12-25T10:30:00Z" data-format="datetime">Loading...</span>

<!-- 不显示时区后缀（默认行为） -->
<span data-utc-time="2023-12-25T10:30:00Z">Loading...</span>

<!-- 显示时区后缀 -->
<span data-utc-time="2023-12-25T10:30:00Z" data-show-timezone="true">Loading...</span>
```

### 3. 格式选项

- `full` (默认): 显示完整日期时间
- `date`: 仅显示日期
- `time`: 仅显示时间
- `datetime`: 显示日期和时间（不含秒）

### 4. JavaScript API

#### 初始化时间元素
```javascript
// 自动处理页面上所有带data-utc-time属性的元素
timezoneHelper.initializeTimeElements();
```

#### 手动转换时间
```javascript
// 转换UTC时间到本地时间
const localTime = timezoneHelper.convertUtcToLocal('2023-12-25T10:30:00Z');

// 格式化时间（简洁版）
const formattedTime = timezoneHelper.formatToLocalTime('2023-12-25T10:30:00Z', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
});
```

#### 动态内容处理
```javascript
// 对于动态加载的内容，手动刷新时间显示
window.refreshTimeDisplays();
```

## 实现细节

### 1. 自动初始化
页面加载完成后，脚本会自动查找所有`data-utc-time`属性的元素并进行转换。

### 2. 时区检测
使用`Intl.DateTimeFormat().resolvedOptions().timeZone`检测用户的本地时区。

### 3. 格式化选项
利用`toLocaleString`方法根据用户的本地设置格式化时间。

## 最佳实践

### 1. 后端时间输出
后端应始终输出ISO 8601格式的UTC时间，例如：
```
2023-12-25T10:30:00Z
```

### 2. 前端标记
在需要展示时间的地方使用`data-utc-time`属性：
```html
<td data-utc-time="@Model.CreatedUtc" data-format="datetime">@Model.CreatedUtc</td>
```

### 3. 动态内容处理
对于通过AJAX加载的内容，调用`refreshTimeDisplays()`函数更新时间显示。

## 错误处理

- 无效的时间字符串会被原样返回
- 转换错误会被记录到控制台
- 确保时间格式符合ISO 8601标准

## 性能考虑

- 时间转换在客户端进行，不增加服务器负担
- 使用浏览器内置的国际化API，性能良好
- 仅对需要转换的元素进行处理