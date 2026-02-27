# 分层身份验证系统 - 快速参考

## 📋 系统概述

本系统实现了**双重验证机制**：
- **本地验证**：医院名称="系统"的用户使用本地数据库
- **第三方验证**：其他医院的用户使用第三方服务

## 🔑 默认账户

### 本地管理员
```
医院名称: 系统
用户名: admin
密码: admin123
```

### 测试用第三方用户
```
医院名称: 测试医院
用户名: doctor
密码: doc123
```

## 🌐 关键URL

- 登录页面: http://localhost:5037/Login
- 医院服务器配置: http://localhost:5037/Admin/HospitalServerConfigs
- 医院API配置: http://localhost:5037/Admin/HospitalApiConfigs
- 访问拒绝页面: http://localhost:5037/AccessDenied
- 健康状态: http://localhost:5037/HealthStatus

## 🔐 权限矩阵

| 用户类型 | 登录 | 管理员页面 | 报表页面 |
|---------|------|-----------|---------|
| 本地管理员（系统+Admin角色） | ✅ | ✅ | ✅ |
| 本地普通用户（系统+其他角色） | ✅ | ❌ | ✅ |
| 第三方用户 | ✅ | ❌ | ✅ |
| 未登录用户 | - | ❌ | ❌ |

## 📁 核心文件

### 服务层
- `Services/UserAuthenticationService.cs` - 用户认证服务
- `Services/IUserAuthenticationService.cs` - 认证服务接口
- `Services/DatabaseInitializationService.cs` - 数据库初始化

### 辅助类
- `Helpers/AuthorizationHelper.cs` - 权限验证辅助
- `Filters/LocalAdminAuthorizationFilter.cs` - 管理员授权过滤器

### 页面
- `Pages/Login.cshtml.cs` - 登录页面逻辑
- `Pages/AccessDenied.cshtml` - 访问拒绝页面
- `Pages/Admin/HospitalServerConfigs.cshtml.cs` - 医院服务器配置
- `Pages/Admin/HospitalApiConfigs.cshtml.cs` - 医院API配置

### 数据模型
- `Models/User.cs` - 用户模型
- `Models/Role.cs` - 角色模型
- `Models/UserRole.cs` - 用户角色关联
- `Models/HospitalServerConfig.cs` - 医院服务器配置

## 🛠️ 使用示例

### 保护管理员页面
```csharp
using LisReportServer.Filters;

[LocalAdminOnly]  // 仅限本地系统管理员访问
public class YourPageModel : PageModel
{
    // 页面逻辑
}
```

### 检查用户权限
```csharp
using LisReportServer.Helpers;

// 检查是否为本地管理员
if (AuthorizationHelper.IsLocalAdmin(User))
{
    // 执行管理员操作
}

// 获取用户医院名称
var hospitalName = AuthorizationHelper.GetUserHospitalName(User);
```

### 验证用户凭据
```csharp
var authResult = await _userAuthenticationService.AuthenticateAsync(
    username, password, hospitalName);

if (authResult.Success)
{
    var user = authResult.User;
    var roles = authResult.Roles;
    // 处理登录成功
}
```

## 🔍 登录流程图

```
用户输入凭据
    ↓
医院名称 == "系统"?
    ↓ (是)          ↓ (否)
本地数据库验证    第三方验证
    ↓                ↓
检查用户&密码      调用第三方API
    ↓                ↓
获取用户角色       分配User角色
    ↓                ↓
    创建认证票据
        ↓
    登录成功
```

## 📊 数据库表结构

### Users（用户表）
- Id: 主键
- Username: 用户名（唯一）
- PasswordHash: 密码哈希（BCrypt）
- Email: 邮箱
- FullName: 姓名
- HospitalName: 所属医院
- IsActive: 是否激活
- CreatedAt: 创建时间
- LastLoginAt: 最后登录时间

### Roles（角色表）
- Id: 主键
- Name: 角色名称（Admin, Doctor, Nurse, Technician）
- Description: 角色描述
- CreatedAt: 创建时间

### UserRoles（用户角色关联表）
- Id: 主键
- UserId: 用户ID（外键）
- RoleId: 角色ID（外键）
- AssignedAt: 分配时间

### HospitalServerConfigs（医院配置表）
- Id: 主键
- HospitalName: 医院名称
- HospitalCode: 医院编码
- ServerAddress: 服务器地址
- Port: 端口
- IsActive: 是否启用
- ...

## 🔧 常用命令

### 启动应用程序
```bash
cd d:\Learn\LISView\LisReportServer
dotnet run
```

### 查看数据库内容
```bash
# 查看所有用户
sqlite3 lisreportserver.db "SELECT Username, HospitalName, IsActive FROM Users;"

# 查看所有角色
sqlite3 lisreportserver.db "SELECT Name, Description FROM Roles;"

# 查看用户角色关联
sqlite3 lisreportserver.db "
SELECT u.Username, r.Name 
FROM Users u 
JOIN UserRoles ur ON u.Id = ur.UserId 
JOIN Roles r ON ur.RoleId = r.Id;"
```

### 编译项目
```bash
cd d:\Learn\LISView\LisReportServer
dotnet build
```

### 清理并重新编译
```bash
cd d:\Learn\LISView\LisReportServer
dotnet clean
dotnet build
```

## 🚨 故障排查

### 登录失败
1. 检查医院名称是否正确（注意大小写）
2. 验证用户名和密码
3. 查看日志了解详细错误信息

### 权限被拒绝
1. 确认用户医院名称为"系统"
2. 检查用户是否有Admin角色
3. 查看UserRoles表确认角色关联

### 页面404错误
1. 确认应用程序正在运行
2. 检查URL拼写是否正确
3. 查看路由配置

## 📝 开发者注意事项

### 密码存储
- **永远不要**直接存储明文密码
- 使用 `UserAuthenticationService.HashPassword()` 加密密码
- 使用 `BCrypt.Net.BCrypt.Verify()` 验证密码

### 权限验证
- 管理员页面**必须**添加 `[LocalAdminOnly]` 特性
- 普通受保护页面使用 `[Authorize]` 特性
- 在代码中使用 `AuthorizationHelper` 进行权限检查

### 日志记录
- 登录成功/失败都要记录日志
- 包含用户名、医院名称等关键信息
- **不要**在日志中记录密码

### 安全原则
1. 验证所有用户输入
2. 使用参数化查询防止SQL注入
3. 实施最小权限原则
4. 定期审查访问日志
5. 保持依赖包更新

## 📚 相关文档

- **详细指南**: `Docs/LayeredAuthenticationGuide.md`
- **测试指南**: `Docs/TestGuide.md`
- **时区处理**: `Docs/TimezoneHandling.md`

## 🎯 快速测试清单

- [ ] 使用admin账户登录成功
- [ ] 访问医院服务器配置页面
- [ ] 访问医院API配置页面
- [ ] 使用第三方用户登录
- [ ] 验证第三方用户无法访问管理员页面
- [ ] 检查AccessDenied页面显示正确
- [ ] 未登录访问受保护页面重定向到登录页

## ⚙️ 配置说明

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=lisreportserver.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "LisReportServer.Services.UserAuthenticationService": "Information"
    }
  }
}
```

## 🎨 自定义扩展

### 添加新角色
```csharp
var newRole = new Role 
{ 
    Name = "NewRole", 
    Description = "新角色描述" 
};
await _context.Roles.AddAsync(newRole);
await _context.SaveChangesAsync();
```

### 为用户分配角色
```csharp
var userRole = new UserRole
{
    UserId = user.Id,
    RoleId = role.Id,
    AssignedAt = DateTime.UtcNow
};
await _context.UserRoles.AddAsync(userRole);
await _context.SaveChangesAsync();
```

### 创建新用户
```csharp
var newUser = new User
{
    Username = "newuser",
    PasswordHash = UserAuthenticationService.HashPassword("password"),
    HospitalName = "系统",
    Email = "newuser@example.com",
    FullName = "新用户",
    IsActive = true
};
await _context.Users.AddAsync(newUser);
await _context.SaveChangesAsync();
```

---

**版本**: 1.0  
**最后更新**: 2026-01-27  
**维护者**: LISView开发团队
