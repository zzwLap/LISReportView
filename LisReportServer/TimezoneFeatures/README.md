# 时区处理功能待用区

此目录包含与时区处理相关的功能组件，当前已移至此处待用，可在需要时启用。

## 包含的组件

### 1. OptimizedTimezoneService.cs
- 性能优化的时区服务
- 使用缓存机制减少重复计算
- 支持时区转换和时区偏移获取

### 2. TimeValidationService.cs
- 时间验证和标准化服务
- 支持多种时间格式解析
- 提供时间范围验证功能

### 3. DemoTimezoneController.cs
- 时区处理功能的演示API端点
- 包含时区信息获取和时间转换示例

### 4. TimezonePerformanceBestPractices.md
- 时区处理性能优化和最佳实践文档
- 包含性能影响评估和优化策略

## 启用方法

如需启用这些功能，请：

1. 将.cs文件移回相应目录：
   - 服务文件移至 `Services/` 目录
   - 控制器文件移至 `Controllers/` 目录

2. 在 `Program.cs` 中注册服务：
   ```csharp
   builder.Services.AddScoped<ITimezoneService, OptimizedTimezoneService>();
   builder.Services.AddScoped<ITimezoneAwareEntityService, TimezoneAwareEntityService>();
   builder.Services.AddScoped<ITimeValidationService, TimeValidationService>();
   ```

3. 如需要，启用中间件：
   ```csharp
   app.UseMiddleware<TimezoneMiddleware>();
   ```

## 用途说明

这些功能适用于：
- 需要在后端进行时区转换的场景
- 大量数据的批量时区转换
- 需要高性能时区处理的应用
- 需要统一时间验证和标准化的场景