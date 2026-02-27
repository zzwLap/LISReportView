# LISView快入门指南

##项目简介

LISView 是一个医院检验信息系统（LIS）解决方案，包含两个核心组件：

- **LisReportServer** - 主报告服务器应用
- **SSOAuthCenter** -单点登录认证中心

##📋 系统要求

- .NET 10.0 SDK (LisReportServer)
- .NET 8.0 SDK (SSOAuthCenter)
- SQLite (默认数据库)
-可选：Redis (用于令牌黑名单)

##🛠️快速开始

### 1.克项目
```bash
git clone <repository-url>
cd LISView
```

### 2.构建项目
```bash
#构建主应用
cd LisReportServer
dotnet build

#构建认证中心
cd ../SSOAuthCenter/SSOAuthCenter
dotnet build
```

### 3. 数据库初始化
```bash
# LisReportServer
cd ../../../LisReportServer
dotnet ef database update

# SSOAuthCenter
cd ../SSOAuthCenter/SSOAuthCenter
dotnet ef database update
```

### 4. 启动应用

**启动主应用：**
```bash
cd ../../../LisReportServer
dotnet run --launch-profile https
#访问: https://localhost:7029
```

**启动认证中心（可选）：**
```bash
cd ../SSOAuthCenter/SSOAuthCenter
dotnet run
#访问: https://localhost:7001
```

##🔧核心功能

### LisReportServer 主要功能
-📊报告管理与查询
-👥信息管理
-🏥医服务器配置
-🔐本地/SSO认证
-健状态监控
-🌍时 时区处理支持

### SSOAuthCenter 主要功能
-🔑2认证服务
-👤管理
-📱用客户端管理
-🎯 角色权限控制

##🎯常操作

###管员登录
- 默认访问：https://localhost:7029
-员账户：admin/admin123

###配置医院服务器
1.访问管理页面：`/Admin/HospitalServerConfigs`
2. 添加医院配置信息
3.测试连接状态
4.启配置

###启SSO认证
在 `appsettings.json` 中配置：
```json
{
  "SSOSettings": {
    "Enabled": true,
    "Authority": "https://localhost:7001",
    "ClientId": "lisreportserver-client",
    "ClientSecret": "your-secret"
  }
}
```

##📚学资源

### 文档目录
-📖完整项目文档](PROJECT_DOCUMENTATION.md)
-⏰时区处理指南](LisReportServer/Docs/TimezoneHandling.md)
-🏆最佳实践](LisReportServer/Docs/TimezoneBestPractices.md)
-🌐前端时区指南](LisReportServer/Docs/FrontendTimezoneGuide.md)

### API端点
-报查询：`GET /api/report/reports`
-查询：`GET /api/report/patients`
-健康检查：`GET /api/health/status`

##🆘常问题

### 数据库连接失败
```bash
dotnet ef database update
```

### SSO认证问题
检查 `appsettings.json` 中的SSO配置是否正确

### 时区显示异常
确保使用 `ITimezoneService`进行时间转换

##🛡️ 安全提示

- 生产环境必须使用HTTPS
- 不要在代码中硬编码敏感信息
-定更新依赖包
-启日志监控

##🤝指南

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 发起 Pull Request

##📞支持

如遇到问题，请：
1. 查看完整文档
2.检查日志文件
3.项目维护团队

---
*Happy Coding! 🎉*