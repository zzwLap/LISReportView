# ASP.NET Core 时区处理最佳实践

## 概述

本文档详细介绍了如何在ASP.NET Core应用程序中系统性地处理时区问题，确保用户在不同时区使用服务时都能看到符合自己本地时区的时间显示。

## 1. 数据存储层面保持UTC时间一致性

### 1.1 数据库设计原则
- **始终在数据库中存储UTC时间**：无论是创建时间、更新时间还是业务时间，都应该以UTC格式存储
- **使用DateTimeOffset类型**：在需要保留时区信息的场景下，使用DateTimeOffset而非DateTime
- **数据库字段命名**：建议使用`CreatedUtc`、`UpdatedUtc`等命名方式明确表示UTC时间

### 1.2 EF Core 配置
```csharp
// 在DbContext中配置
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 为所有DateTime属性设置默认行为
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        foreach (var property in entityType.GetProperties())
        {
            if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
            {
                // 确保DateTime类型在数据库中正确处理
                property.SetColumnType("TEXT"); // SQLite中使用TEXT存储ISO 8601格式
            }
        }
    }
}
```

## 2. 传输过程中使用UTC时间

### 2.1 API 接口设计
- **API接收时间**：客户端发送的时间应为UTC时间或包含时区信息的时间
- **API返回时间**：统一返回UTC时间，由客户端或服务端模板引擎进行时区转换

### 2.2 JSON 序列化配置
```csharp
// 在Program.cs中配置
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// 确保JSON序列化正确处理DateTime
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

## 3. 界面显示时转换为客户端本地时间

### 3.1 Razor 视图中的时区转换
```html
@inject ITimezoneService TimezoneService

@{
    var clientTime = TimezoneService.ConvertUtcToClientTime(Model.CreatedUtc);
    var timezoneOffset = TimezoneService.GetCurrentTimezoneOffset();
}

<div class="time-display">
    <span title="@Model.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss") UTC">
        @clientTime.ToString("yyyy-MM-dd HH:mm:ss") (@timezoneOffset)
    </span>
</div>
```

### 3.2 JavaScript 时区处理
```javascript
// 客户端JavaScript获取本地时间显示
function displayLocalTime(utcTimeString) {
    const utcDate = new Date(utcTimeString + 'Z'); // 添加'Z'表示UTC时间
    const localTime = utcDate.toLocaleString(navigator.language, {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        timeZoneName: 'short'
    });
    return localTime;
}

// 自动发送时区信息到服务器
function sendTimezoneToServer() {
    const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    // 通过Cookie、Header或查询参数发送时区信息
    document.cookie = `timezone=${encodeURIComponent(timezone)}; path=/`;
}
```

## 4. 统一的时区处理机制

### 4.1 服务注册
```csharp
// 在Program.cs中注册服务
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITimezoneService, TimezoneService>();
builder.Services.AddScoped<ITimezoneAwareEntityService, TimezoneAwareEntityService>();
```

### 4.2 中间件配置
```csharp
// 在请求管道中添加时区中间件
app.UseMiddleware<TimezoneMiddleware>();
```

### 4.3 时区感知实体服务
我们创建了`TimezoneAwareEntityService`来自动处理实体中时间字段的转换：

```csharp
public class SomeController : Controller
{
    private readonly ITimezoneAwareEntityService _timezoneEntityService;

    public SomeController(ITimezoneAwareEntityService timezoneEntityService)
    {
        _timezoneEntityService = timezoneEntityService;
    }

    public IActionResult GetData()
    {
        var entities = _someService.GetAllEntities();
        // 自动将UTC时间转换为客户端时区时间
        var entitiesWithClientTime = _timezoneEntityService.ConvertUtcFieldsToClientTime(entities);
        return View(entitiesWithClientTime);
    }
}
```

## 5. API 端点支持时区参数

### 5.1 带时区参数的API端点
```csharp
[HttpGet]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<SomeDto>> GetData([FromQuery]string timezone = null)
{
    var data = await _someService.GetDataAsync();
    
    // 如果客户端提供了时区信息，进行相应转换
    if (!string.IsNullOrEmpty(timezone))
    {
        var clientTimezoneInfo = TimezoneHelper.GetTimeZoneInfoFromIana(timezone);
        if (clientTimezoneInfo != null)
        {
            // 对数据中的时间字段进行转换
            data.ConvertTimesToTimezone(clientTimezoneInfo);
        }
    }
    else
    {
        // 使用当前请求的时区设置
        var currentTimezoneInfo = _timezoneService.GetCurrentTimezone();
        data.ConvertTimesToTimezone(currentTimezoneInfo);
    }
    
    return data;
}
```

## 6. 代码实现示例

### 6.1 服务层实现
```csharp
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ITimezoneAwareEntityService _timezoneEntityService;

    public ReportService(
        ApplicationDbContext context, 
        ITimezoneAwareEntityService timezoneEntityService)
    {
        _context = context;
        _timezoneEntityService = timezoneEntityService;
    }

    public async Task<List<ReportDto>> GetReportsAsync()
    {
        var reports = await _context.Reports
            .Select(r => new ReportDto
            {
                Id = r.Id,
                Name = r.Name,
                CreatedUtc = r.CreatedUtc,  // 始终存储为UTC
                UpdatedUtc = r.UpdatedUtc   // 始终存储为UTC
            })
            .ToListAsync();

        // 返回给客户端前转换为客户端时区时间
        return reports.Select(r => _timezoneEntityService.ConvertUtcFieldsToClientTime(r)).ToList();
    }

    public async Task CreateReportAsync(CreateReportDto dto)
    {
        // 从客户端接收时间时，转换为UTC存储
        var report = new Report
        {
            Name = dto.Name,
            CreatedUtc = _timezoneEntityService.ConvertClientTimeToUtc(dto.Created), // 转换为UTC
            UpdatedUtc = DateTime.UtcNow
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();
    }
}
```

### 6.2 控制器实现
```csharp
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ITimezoneService _timezoneService;

    public ReportsController(IReportService reportService, ITimezoneService timezoneService)
    {
        _reportService = reportService;
        _timezoneService = timezoneService;
    }

    public async Task<IActionResult> Index()
    {
        var reports = await _reportService.GetReportsAsync();
        ViewBag.ClientTimezone = _timezoneService.GetCurrentTimezoneOffset();
        return View(reports);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateReportDto dto)
    {
        try
        {
            await _reportService.CreateReportAsync(dto);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "创建报告失败：" + ex.Message);
            return View(dto);
        }
    }
}
```

### 6.3 Razor 页面实现
```csharp
public class ReportListModel : PageModel
{
    private readonly IReportService _reportService;
    private readonly ITimezoneService _timezoneService;

    public ReportListModel(IReportService reportService, ITimezoneService timezoneService)
    {
        _reportService = reportService;
        _timezoneService = timezoneService;
    }

    public List<ReportDto> Reports { get; set; } = new();
    public string ClientTimezone { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        Reports = await _reportService.GetReportsAsync();
        ClientTimezone = _timezoneService.GetCurrentTimezoneOffset();
        return Page();
    }
}
```

## 7. 自动检测客户端时区信息

### 7.1 时区中间件实现
我们的`TimezoneMiddleware`自动从以下来源获取时区信息：
1. 查询参数: `?timezone=Asia/Shanghai`
2. 请求头: `X-Timezone: Asia/Shanghai`
3. Cookie: `timezone=Asia/Shanghai`

### 7.2 前端自动检测
```javascript
// 页面加载时自动检测并设置时区
document.addEventListener('DOMContentLoaded', function() {
    const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    
    // 通过Cookie发送时区信息
    document.cookie = `timezone=${encodeURIComponent(timezone)}; path=/; SameSite=Lax`;
    
    // 或者通过Ajax发送时区信息
    fetch('/api/timezone/set', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ timezone: timezone })
    }).catch(console.error);
});
```

## 8. 错误处理机制

### 8.1 时区转换错误处理
```csharp
public DateTime ConvertUtcToClientTime(DateTime utcDateTime)
{
    try
    {
        var timezoneInfo = GetCurrentTimezone();
        if (timezoneInfo != null)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timezoneInfo);
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to convert UTC time to client timezone, falling back to local time");
    }

    // 如果转换失败，返回服务器本地时间作为备用方案
    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZoneInfo.Local);
}
```

### 8.2 无效时区ID处理
```csharp
public TimeZoneInfo GetTimezoneInfoFromIana(string ianaTimeZoneId)
{
    try
    {
        // 尝试直接获取Windows时区
        var windowsTimeZone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
        return windowsTimeZone;
    }
    catch
    {
        // 如果直接匹配失败，尝试映射IANA到Windows时区
        var mapping = GetIanaToWindowsMapping();
        if (mapping.ContainsKey(ianaTimeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(mapping[ianaTimeZoneId]);
            }
            catch
            {
                // 如果映射失败，返回本地时区
                return TimeZoneInfo.Local;
            }
        }
        return TimeZoneInfo.Local; // 默认返回本地时区
    }
}
```

## 9. 最佳实践总结

1. **存储UTC，显示本地**：数据库中始终存储UTC时间，在界面显示时转换为客户端本地时间
2. **统一转换服务**：使用统一的时区服务进行转换，避免在多处重复实现
3. **自动检测优先**：优先使用自动检测的时区信息，其次才是手动指定
4. **优雅降级**：在时区转换失败时，提供合理的备用方案（通常是服务器本地时间）
5. **明确标识**：在界面中明确标识时间是UTC时间还是本地时间，避免混淆
6. **API兼容**：为API端点提供时区参数支持，增加灵活性
7. **性能考虑**：对于大量数据的时区转换，考虑批量处理和缓存机制

通过遵循以上最佳实践，可以确保ASP.NET Core应用程序在处理时区问题时既具有一致性又有良好的用户体验。