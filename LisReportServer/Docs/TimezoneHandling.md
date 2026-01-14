# 统一时区处理方案

## 概述

本方案提供了一个统一的时区处理机制，可以在整个系统中一致地处理UTC时间与客户端本地时间之间的转换。

## 组件

### 1. TimezoneMiddleware
- 自动从请求中提取时区信息（查询参数、请求头或Cookie）
- 将时区信息存储在HTTP上下文中供后续使用

### 2. ITimezoneService 和 TimezoneService
- 提供统一的时区转换服务
- 可以将UTC时间转换为客户端时区时间
- 支持指定时区ID的转换

### 3. TimezoneHelper
- 提供IANA时区到Windows时区的映射
- 支持常见的时区转换

### 4. TimezoneExtensions
- 为HttpContext提供扩展方法
- 简化时区转换的使用

## 使用方法

### 在控制器中使用

```csharp
public class SomeController : Controller
{
    private readonly ITimezoneService _timezoneService;

    public SomeController(ITimezoneService timezoneService)
    {
        _timezoneService = timezoneService;
    }

    public IActionResult SomeAction()
    {
        var utcTime = DateTime.UtcNow;
        var clientTime = _timezoneService.ConvertUtcToClientTime(utcTime);
        var timezoneOffset = _timezoneService.GetCurrentTimezoneOffset();

        // 使用转换后的时间...
        return View();
    }
}
```

### 在Razor页面中使用

```csharp
public class SomePageModel : PageModel
{
    private readonly ITimezoneService _timezoneService;

    public SomePageModel(ITimezoneService timezoneService)
    {
        _timezoneService = timezoneService;
    }

    public void OnGet()
    {
        var utcTime = DateTime.UtcNow;
        var clientTime = _timezoneService.ConvertUtcToClientTime(utcTime);
    }
}
```

### 在Razor视图中使用

```html
@inject ITimezoneService TimezoneService

@{
    var clientTime = TimezoneService.ConvertUtcToClientTime(Model.UtcTime);
    var timezoneOffset = TimezoneService.GetCurrentTimezoneOffset();
}

<p>本地时间: @clientTime.ToString("yyyy-MM-dd HH:mm:ss") (@timezoneOffset)</p>
```

### 使用HttpContext扩展方法

```csharp
// 在任何可以访问HttpContext的地方
var clientTime = HttpContext.ToClientTime(utcTime);
var timezoneOffset = HttpContext.GetTimezoneOffset();
```

## 时区信息来源

系统按以下优先级获取客户端时区信息：

1. 查询参数: `?timezone=Asia/Shanghai`
2. 请求头: `X-Timezone: Asia/Shanghai`
3. Cookie: `timezone=Asia/Shanghai`

## 常见时区ID

- 中国标准时间: `Asia/Shanghai`
- 日本标准时间: `Asia/Tokyo`
- 美国东部时间: `America/New_York`
- 美国西部时间: `America/Los_Angeles`
- 英国时间: `Europe/London`
- 欧洲中部时间: `Europe/Paris`
- 印度标准时间: `Asia/Kolkata`

## 最佳实践

1. 在数据库中始终存储UTC时间
2. 在传输过程中使用UTC时间
3. 在显示给用户时转换为客户端本地时间
4. 使用统一的服务进行时区转换，避免在多处重复实现
5. 为API端点提供时区参数支持