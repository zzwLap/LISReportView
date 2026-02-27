# LISView 项目完整文档

## 项目概述

LISView 是一个基于 .NET 的医院检验信息系统（LIS）解决方案，包含两个主要组件：
1. **LisReportServer** - LIS报告服务器（主应用）
2. **SSOAuthCenter** -单点登录认证中心

##系统架构

###整架构图
```
┌─────────────────────────────────────────────────────────────┐
│                        LISView 解决方案                        │
├─────────────────────────────┬─────────────────────────────────┤
│    LisReportServer (主应用)   │      SSOAuthCenter (认证中心)     │
│                             │                                 │
│ ┌──────────────────────┐    │  ┌─────────────────────────┐   │
│  │   Web UI (Razor)    │    │  │     Identity Server     │   │
│  │   -报管理页面     │    │  │     - OAuth2 服务        │   │
│  │   -健康监控页面     │    │  │     - 用户管理界面        │   │
│  │   -医配置管理     │    │  │     -应用管理           │   │
│ └─────────────────────┘    │  └─────────────────────────┘   │
│ ┌─────────────────────┐    │                                 │
│  │     API 服务        │    │                                 │
│  │   - REST API        │    │                                 │
│  │   -报告查询接口      │    │                                 │
│  │   -医服务接口      │    │                                 │
│ └─────────────────────┘    │                                 │
│  ┌─────────────────────┐    │  ┌─────────────────────────┐   │
│  │   业务逻辑层        │◄───┼──►│   认证服务            │   │
│  │   -报告服务        │    │  │    - OAuth2 实现        │   │
│  │   -医配置服务      │    │  │    - JWT 令牌管理        │   │
│  │   - 时区服务        │    │  │    - 用户会话管理        │   │
│  │   -健检查服务      │    │  └─────────────────────────┘   │
│  └─────────────────────┘    │                                 │
│  ┌─────────────────────┐    │                                 │
│  │   数据访问层        │    │                                 │
│  │   - Entity Framework│    │                                 │
│  │   - SQLite/SQL Server│   │                                 │
│ └─────────────────────┘    │                                 │
└─────────────────────────────┴─────────────────────────────────┘
```

## LisReportServer (主应用)

### 核心功能
- **报告管理**：检验报告的上传、查询、状态管理
- **患者信息管理**：通过住院号、检查号、门诊号查询患者信息
- **医院服务器配置**：管理多医院服务器连接配置
- **身份验证**：支持本地Cookie认证和SSO单点登录
- **健康监控**：实时监控SSO认证中心健康状态
- **时区处理**：统一的时区处理机制，支持多时区时间转换
- **令牌黑名单**：基于Redis或内存的令牌黑名单管理

### 技术栈
- **.NET 10.0** - 最新.NET平台
- **ASP.NET Core Razor Pages** - 页面框架
- **Entity Framework Core** - ORM数据访问
- **SQLite** - 默认数据库（支持SQL Server）
- **Redis** -可选的令牌黑名单存储
- **OpenID Connect** - SSO认证支持

### 项目结构
```
LisReportServer/
├── Controllers/                # API控制器
│   ├── AccountController.cs    #认证控制器
│  └── Api/                    # API端点
│       ├── HealthController.cs
│       └── ReportApiController.cs
├── Data/                       # 数据访问层
│  └── ApplicationDbContext.cs
├── Models/                     # 数据模型
│   ├── HospitalServerConfig.cs
│  └── SSOSettings.cs
├── Services/                   # 业务逻辑服务
│   ├── ReportService.cs
│   ├── HospitalServerConfigService.cs
│   ├── SSOUserService.cs
│   ├── SSOHealthCheckService.cs
│   ├── TimezoneService.cs
│   └── TokenBlacklistService.cs
├── Middleware/                 # 中间件
│   └── TimezoneMiddleware.cs
├── Pages/                      # Razor页面
│   ├── Login.cshtml
│   ├── Dashboard.cshtml
│   ├── HealthStatus.cshtml
│   └── Admin/
│      └── HospitalServerConfigs.cshtml
├── TimezoneFeatures/           # 时区功能组件
│   ├── TimezoneAwareEntityService.cs
│   └── TimeValidationService.cs
└── Docs/                       # 文档
    ├── TimezoneHandling.md
    ├── TimezoneBestPractices.md
    └── FrontendTimezoneGuide.md
```

###配文件置文件

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=lisreportserver.db"
  },
  "SSOSettings": {
    "Enabled": false,
    "Authority": "https://localhost:7001",
    "ClientId": "lisreportserver-client",
    "ClientSecret": "lisreportserver-secret",
    "RedirectUri": "https://localhost:7000/signin-oidc",
    "ResponseType": "code",
    "Scope": "openid profile email",
    "HealthCheckIntervalSeconds": 30
  }
}
```

###启动配置
- **HTTP端口**：5037
- **HTTPS端口**：7029
- **数据库**：SQLite (lisreportserver.db)
- **默认认证**：本地Cookie认证

### 数据模型

#### HospitalServerConfig
```csharp
public class HospitalServerConfig
{
    public int Id { get; set; }
    public string HospitalName { get; set; }        //医名称
    public string HospitalCode { get; set; }        //医编码（唯一）
    public string ServerAddress { get; set; }       // 服务器地址
    public int? Port { get; set; }                  //端
    public string? Username { get; set; }            // 用户名
    public string? EncryptedPassword { get; set; }   // 加密密码
    public string? OtherParameters { get; set; }     //其他参数
    public bool IsActive { get; set; } = true;       // 是否激活
    public DateTime CreatedAt { get; set; }          // 创建时间（UTC）
    public DateTime UpdatedAt { get; set; }          // 更新时间（UTC）
}
```

## SSOAuthCenter (认证中心)

###核心功能
- **OAuth2认证服务**：标准的OAuth2认证实现
- **用户管理**：用户账户创建、编辑、权限管理
- **应用管理**：客户端应用注册和配置
- **角色权限**：基于角色的访问控制
- **会话管理**：用户登录会话跟踪

###技术栈
- **.NET 8.0** -的.NET平台版本
- **ASP.NET Core Razor Pages** -管界面
- **Entity Framework Core** - 数据访问
- **SQLite** - 数据存储
- **JWT Token** -认证令牌管理

### 项目结构
```
SSOAuthCenter/
├── Controllers/                # API控制器
│   └── OAuthController.cs
├── Data/                      # 数据访问层
│  └── ApplicationDbContext.cs
├── Models/                    # 数据模型
│   ├── User.cs
│   ├── ClientApplication.cs
│   ├── Role.cs
│   ├── UserRole.cs
│  └── AuthToken.cs
├── Services/                  # 业务逻辑服务
│   ├── AuthService.cs
│  └── OAuth2Service.cs
├── Pages/                     #管页面理页面
│   └── Users/
│       ├── Index.cshtml      # 用户列表
│       ├── Create.cshtml     # 创建用户
│       ├── Edit.cshtml       #编辑用户
│       └── Details.cshtml    # 用户详情
└── Migrations/                # 数据库迁移
```

### 数据模型

#### User
```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }       // 用户名
    public string Email { get; set; }           //
    public string? PasswordHash { get; set; }   //密码哈希
    public string? FirstName { get; set; }      // 名
    public string? LastName { get; set; }        //    public bool IsActive { get; set; } = true   // 是否激活
    public bool IsEmailConfirmed { get; set; } = false  //是否验证
    public DateTime CreatedAt { get; set; }     // 创建时间
    public DateTime UpdatedAt { get; set; }     // 更新时间
    public virtual ICollection<UserRole> UserRoles { get; set; }  //角关系
}
```

#### ClientApplication
```csharp
public class ClientApplication
{
    public int Id { get; set; }
    public string ClientId { get; set; }              //客端ID
    public string ClientSecret { get; set; }          //客户端密钥
    public string ClientName { get; set; }             //应用名称
    public string RedirectUri { get; set; }            // 重定向URI
    public string? LogoutRedirectUri { get; set; }     // 登出重定向URI
    public bool IsActive { get; set; } = true          // 是否激活
    public DateTime CreatedAt { get; set; }            // 创建时间
    public DateTime UpdatedAt { get; set; }            // 更新时间
}
```

## 系统集成

###认证流程

#### 1. 本地认证模式（默认）
```
用户访问 → 认证检查 → 本地登录 → 会话管理 →授权访问
```

#### 2. SSO认证模式
```
用户访问 → SSO检查 →认证中心重定向 → OpenID Connect流程 →回验证 → 会话创建 →授权访问
```

### 时区处理统一方案

####处流程
```
1. 数据存储 → 数据库存储UTC时间
2. 数据传输 → API返回UTC时间
3. 时区转换 → 中间件自动处理
4.显示端显示 → 自动转换为客户端时区
```

#### 时区信息获取优先级
1. 查询参数: `?timezone=Asia/Shanghai`
2. 请求头: `X-Timezone: Asia/Shanghai`
3. Cookie: `timezone=Asia/Shanghai`

## 开发指南

###环境准备
1.安装 .NET 10.0 SDK
2. （可选）安装 Redis 服务器
3.安装 SQLite工具

###构建和运行

#### LisReportServer
```bash
#进项目目录
cd LisReportServer

#构建项目
dotnet build

#运行项目（HTTP）
dotnet run --launch-profile http

#运行项目（HTTPS）
dotnet run --launch-profile https

#应用数据库迁移
dotnet ef database update
```

#### SSOAuthCenter
```bash
#进项目目录
cd SSOAuthCenter/SSOAuthCenter

#构建项目
dotnet build

#运行项目
dotnet run

#应用数据库迁移
dotnet ef database update
```

### 数据库管理

####迁命令
```bash
# 创建新迁移
dotnet ef migrations add MigrationName

# 查看迁移状态
dotnet ef migrations list

#撤最后迁移
dotnet ef migrations remove
```

###配置管理

####启SSO认证
```json
{
  "SSOSettings": {
    "Enabled": true,
    "Authority": "https://localhost:7001",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "https://localhost:7029/signin-oidc"
  }
}
```

####配置Redis连接
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

## API参考

###报服务API

#### 获取报告列表
```http
GET /api/report/reports
GET /api/report/reports?patientId=P001
```

#### 获取患者信息
```http
GET /api/report/patients
GET /api/report/patients?examId=E001
GET /api/report/patients?patientId=P001
```

###医服务API

#### 获取健康状态
```http
GET /api/hospitals/{hospitalCode}/health
```

#### 获取医院数据
```http
GET /api/hospitals/{hospitalCode}/patients
GET /api/hospitals/{hospitalCode}/reports
GET /api/hospitals/{hospitalCode}/reports/{reportId}
```

#### 数据操作
```http
POST /api/hospitals/{hospitalCode}/reports
PUT /api/hospitals/{hospitalCode}/reports/{reportId}/status
```

###认证相关API

#### OpenID Connect端点
```http
GET /connect/authorize          # 认证授权
POST /connect/token            # 令牌获取
GET /connect/userinfo          # 用户信息
GET /connect/logout           # 登出
```

## 最佳实践

###编码规范
-启可空引用类型
-启隐式using
- 使用C#命名约定
-统一依赖注入配置
-接口命名（如IUserService）

### 服务作用域
```csharp
builder.Services.AddScoped<IService, Service>()    // 请求作用域
builder.Services.AddSingleton<IService, Service>()  //单例
builder.Services.AddTransient<IService, Service>() //
builder.Services.AddHostedService<BackgroundService>() //后台服务
```

###错处理处理
1.统一异常处理
2. 日志记录
3.检查降级
4.缓空结果
5.幂操作支持

###性能优化
- 使用缓存避免重复查询
- 数据库查询优化
-任务延迟初始化
-入日志监控
-列队加载渐进展现

###测试检查
确保所有功能正常运转的方法清单和日志记录计划

最终用户的良好体验依赖于所有组件的正确配置和运行。请按以下步骤验证系统：

1. 系统启动
   - [ ] LisReportServer正常启动
   - [ ] SSOAuthCenter正常启动（如启用）
   - [ ] 数据库连接成功
   - [ ] Redis连接成功（如配置）

2.功能测试
   - [ ]可访问
   - [ ] 登录功能正常
   - [ ]报查询功能
   - [ ]医配置管理

3.高功能验证
   - [ ] SSO认证流程
   - [ ] 时区转换正确
   - [ ]健检查监控
   - [ ] 令牌黑名单管理

4.性能监控
   - [ ] 页面加载时间
   - [ ] API响应时间
   - [ ] 数据库查询性能
   - [ ]内存使用情况

##故障排除

###问题

#### 数据库连接失败
-检查连接字符串配置
- 确认数据库文件权限
-运行数据库迁移命令

#### SSO认证失败
-验证SSO配置正确性
-检查认证服务器可达性
- 确认回调URL匹配
- 查看详细日志信息

#### 时区转换异常
- 确认使用ITimezoneService
-验证时区ID格式正确
-检查中间件配置
- 确认客户端时区信息

####性能问题
-检查缓存配置
- 优化数据库查询
-监控资源使用
- 分析慢查询日志

##安全考虑

### 数据安全
-敏感信息不硬编码
-密码加密存储
- 输入验证和清理
- SQL注入防护

###认安全
- HTTPS强制使用
- 令牌过期管理
- 令牌黑名单机制
- 会话安全配置

###访控制
-角基础权限
- 页面授权策略
- API访问控制
-审日志记录

## 项目维护

###版本管理
-语义化版本
-定安全更新
- 依赖包版本管理
-迁脚本维护

###监控告警
- 系统健康检查
-性能指标监控
-错误日志分析
- 自动告警机制

###备策略
- 数据库定期备份
-配置文件备份
-迁脚本版本控制
-灾恢复计划

## 附录

###参文档
- [时区处理文档](LisReportServer/Docs/TimezoneHandling.md)
- [时区最佳实践](LisReportServer/Docs/TimezoneBestPractices.md)
- [前端时区指南](LisReportServer/Docs/FrontendTimezoneGuide.md)
- [时区性能优化](LisReportServer/TimezoneFeatures/TimezonePerformanceBestPractices.md)

###常时区ID
- 中国标准时间: `Asia/Shanghai`
- 日本标准时间: `Asia/Tokyo`
-东部时间: `America/New_York`
-西部时间: `America/Los_Angeles`
-英国时间: `Europe/London`
-中部时间: `Europe/Paris`
-印标准时间: `Asia/Kolkata`

###信息
本项目是LISView解决方案的一部分，如有问题请联系项目维护团队。