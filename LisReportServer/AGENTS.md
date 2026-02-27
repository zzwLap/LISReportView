# LIS 报告服务器 (LisReportServer)

## 项目概述

LisReportServer 是一个基于 ASP.NET Core 的医院检验信息系统（LIS）报告服务器应用程序。该项目提供报告管理、患者信息查询、医院服务器配置管理等功能，支持本地身份验证和基于 OpenID Connect 的单点登录（SSO）。

### 核心功能

- **报告管理**：管理检验报告的上传、查询和状态跟踪
- **患者信息管理**：支持通过住院号、检查号、门诊号查询患者信息
- **医院服务器配置**：管理多医院服务器连接配置
- **身份验证**：支持本地 Cookie 认证和 SSO 单点登录
- **健康检查**：实时监控 SSO 认证中心健康状态
- **时区支持**：统一的时区处理机制，支持多时区时间转换
- **令牌黑名单**：支持基于 Redis 或内存的令牌黑名单管理

### 技术栈

- **.NET 10.0**：使用最新的 .NET 平台
- **ASP.NET Core Razor Pages**：页面框架
- **Entity Framework Core**：ORM 数据访问
- **SQLite**：默认数据库（支持 SQL Server）
- **Redis**：可选的令牌黑名单存储
- **OpenID Connect**：SSO 认证支持

### 项目架构

```
LisReportServer/
├── Controllers/          # API 控制器
│   ├── AccountController.cs    # 认证控制器（SSO/本地登录）
│   └── Api/
│       ├── HealthController.cs     # 健康检查 API
│       └── ReportApiController.cs  # 报告 API
├── Data/                # 数据访问层
│   └── ApplicationDbContext.cs   # EF Core 上下文
├── Models/              # 数据模型
│   ├── HospitalServerConfig.cs   # 医院服务器配置
│   └── SSOSettings.cs            # SSO 配置
├── Services/            # 业务逻辑服务
│   ├── ReportService.cs              # 报告服务
│   ├── HospitalServerConfigService.cs # 医院配置服务
│   ├── SSOUserService.cs             # SSO 用户服务
│   ├── SSOHealthCheckService.cs      # SSO 健康检查
│   ├── TimezoneService.cs            # 时区服务
│   └── TokenBlacklistService.cs      # 令牌黑名单服务
├── Middleware/          # 中间件
│   └── TimezoneMiddleware.cs   # 时区处理中间件
├── Pages/               # Razor 页面
│   ├── Login.cshtml              # 登录页
│   ├── Dashboard.cshtml          # 仪表板
│   ├── HealthStatus.cshtml       # 健康状态页
│   └── Admin/
│       └── HospitalServerConfigs.cshtml  # 医院配置管理
└── TimezoneFeatures/     # 时区功能组件
    ├── TimezoneAwareEntityService.cs
    └── TimeValidationService.cs
```

## 构建和运行

### 前置要求

- .NET 10.0 SDK
- （可选）Redis 服务器（用于令牌黑名单）
- （可选）SSO 认证服务器

### 常用命令

```bash
# 构建项目
dotnet build

# 运行项目（开发环境，HTTP）
dotnet run --launch-profile http

# 运行项目（开发环境，HTTPS）
dotnet run --launch-profile https

# 发布项目
dotnet publish -c Release

# 运行数据库迁移
dotnet ef migrations add MigrationName

# 应用数据库迁移
dotnet ef database update
```

### 默认配置

- **HTTP 端口**：5037
- **HTTPS 端口**：7029
- **数据库**：SQLite (lisreportserver.db)
- **SSO**：默认禁用（可在 appsettings.json 中启用）

### 环境变量

项目支持通过环境变量配置：

- `ASPNETCORE_ENVIRONMENT`：运行环境（Development/Production）
- `ConnectionStrings__DefaultConnection`：数据库连接字符串
- `ConnectionStrings__Redis`：Redis 连接字符串（可选）

## 开发约定

### 代码风格

- 使用 `Nullable` 引用类型（已启用）
- 使用 `ImplicitUsings`（已启用）
- 遵循 C# 命名约定：
  - 类名：PascalCase
  - 方法名：PascalCase
  - 属性名：PascalCase
  - 私有字段：_camelCase
  - 参数：camelCase

### 服务注册模式

- 使用依赖注入（DI）容器管理服务
- 服务接口使用 `I` 前缀（如 `IReportService`）
- 作用域选择：
  - `AddScoped`：请求作用域（大多数服务）
  - `AddSingleton`：单例（如 ReportService）
  - `AddTransient`：瞬时作用域（如 CustomCookieAuthenticationEvents）
  - `AddHostedService`：后台服务（如 HealthStatusPublishingService）

### 数据库约定

- 所有时间字段使用 UTC 时间存储
- 使用 Entity Framework Core 进行数据访问
- 迁移文件存储在 `Migrations/` 目录
- 数据库上下文：`ApplicationDbContext`

### 身份验证流程

1. **SSO 模式**（当 SSOSettings.Enabled = true）：
   - 访问需要认证的页面 → 重定向到 `/Account/Login`
   - 检查 SSO 健康状态
   - 如果 SSO 可用 → 重定向到 OpenID Connect 认证
   - 如果 SSO 不可用 → 降级到本地登录

2. **本地认证模式**（默认）：
   - 使用 Cookie 认证
   - 登录页：`/Login`
   - 登出页：`/Logout`

### 时区处理约定

- 数据库存储 UTC 时间
- API 返回 UTC 时间
- 前端负责时区转换
- 使用 `ITimezoneService` 进行时区转换
- 时区信息来源优先级：查询参数 > 请求头 > Cookie

### API 端点约定

- API 路径前缀：`/api/`
- 使用属性路由：`[ApiController]` 和 `[Route("api/[controller]")]`
- 返回标准的 HTTP 状态码
- 使用 `IActionResult` 作为返回类型

### 日志记录

- 使用内置的 `ILogger<T>` 接口
- 日志级别：Trace < Debug < Information < Warning < Error < Critical
- 配置在 `appsettings.json` 中

## 关键配置文件

### appsettings.json

主要配置项：
- `ConnectionStrings`：数据库连接字符串
- `SSOSettings`：SSO 认证配置
- `Logging`：日志配置

### LisReportServer.csproj

项目依赖：
- Microsoft.AspNetCore.Authentication.OpenIdConnect
- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.EntityFrameworkCore.SqlServer
- StackExchange.Redis

### launchSettings.json

开发环境启动配置：
- http profile：http://localhost:5037
- https profile：https://localhost:7029;http://localhost:5037

## 数据模型

### HospitalServerConfig

医院服务器配置实体：
- `Id`：主键
- `HospitalName`：医院名称
- `HospitalCode`：医院编码（唯一）
- `ServerAddress`：服务器地址
- `Port`：端口号
- `Username`：用户名
- `EncryptedPassword`：加密密码
- `OtherParameters`：其他参数
- `IsActive`：是否激活
- `CreatedAt`：创建时间（UTC）
- `UpdatedAt`：更新时间（UTC）

### SSOSettings

SSO 配置模型：
- `Enabled`：是否启用 SSO
- `Authority`：认证服务器地址
- `ClientId`：客户端 ID
- `ClientSecret`：客户端密钥
- `RedirectUri`：重定向 URI
- `ResponseType`：响应类型
- `Scope`：权限范围
- `HealthCheckIntervalSeconds`：健康检查间隔（秒）

## 常见任务

### 添加新的数据库迁移

```bash
dotnet ef migrations add AddNewFeature
```

### 应用待处理的迁移

```bash
dotnet ef database update
```

### 启用 SSO 认证

在 `appsettings.json` 中设置：
```json
{
  "SSOSettings": {
    "Enabled": true,
    "Authority": "https://your-sso-server.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "https://localhost:7029/signin-oidc"
  }
}
```

### 配置 Redis 连接

在 `appsettings.json` 中添加：
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### 添加新的 API 端点

1. 在 `Controllers/Api/` 下创建控制器
2. 使用 `[ApiController]` 和 `[Route("api/[controller]")]` 属性
3. 注入所需的服务
4. 返回 `IActionResult`

### 添加新的 Razor 页面

1. 在 `Pages/` 下创建 `.cshtml` 和 `.cshtml.cs` 文件
2. 继承 `PageModel` 类
3. 在 `Program.cs` 中配置授权策略（如需要）

## 测试

### 手动测试

1. 启动应用：`dotnet run`
2. 访问首页：`https://localhost:7029`
3. 测试登录功能
4. 测试 API 端点

### API 测试示例

```bash
# 获取患者列表
curl https://localhost:7029/api/report/patients

# 通过检查号查询患者
curl "https://localhost:7029/api/report/patients?examId=E001"

# 获取报告列表
curl https://localhost:7029/api/report/reports

# 通过住院号查询报告
curl "https://localhost:7029/api/report/reports?patientId=P001"
```

## 故障排查

### 数据库连接问题

- 检查 `appsettings.json` 中的连接字符串
- 确保 SQLite 文件有读写权限
- 运行 `dotnet ef database update` 确保迁移已应用

### SSO 认证失败

- 检查 SSOSettings 配置是否正确
- 验证认证服务器是否可访问
- 查看日志获取详细错误信息
- 检查回调 URL 是否匹配

### 时区转换问题

- 确保使用 `ITimezoneService` 进行转换
- 验证时区 ID 是否正确（IANA 格式）
- 检查 TimezoneMiddleware 是否正确配置

## 项目依赖关系

```
Controllers
  ├── Services
  │   ├── Data (ApplicationDbContext)
  │   └── Models
  └── Models

Pages
  ├── Services
  └── Models

Middleware
  └── Services (ITimezoneService)

Services
  ├── Data (ApplicationDbContext)
  ├── Models
  └── Helpers (TimezoneHelper)
```

## 安全注意事项

1. **敏感信息**：不要在代码中硬编码密钥或密码
2. **密码加密**：使用 `EncryptedPassword` 字段存储加密后的密码
3. **令牌管理**：使用令牌黑名单防止令牌重放攻击
4. **HTTPS**：生产环境始终使用 HTTPS
5. **输入验证**：使用数据注解验证用户输入
6. **授权**：使用 Razor Pages 约定控制页面访问权限

## 扩展阅读

- 时区处理文档：`Docs/TimezoneHandling.md`
- 时区最佳实践：`Docs/TimezoneBestPractices.md`
- 前端时区指南：`Docs/FrontEndTimezoneGuide.md`
- 时区性能最佳实践：`TimezoneFeatures/TimezonePerformanceBestPractices.md`

## 许可证

本项目是 LISView 解决方案的一部分。