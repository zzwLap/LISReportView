# 分层身份验证系统说明

## 系统架构

本系统实现了分层身份验证机制，根据用户所属医院的不同，采用不同的验证方式：

### 1. 本地数据库验证
- **适用对象**：医院名称为"系统"的用户
- **验证方式**：通过本地数据库的 User 表进行身份验证
- **密码存储**：使用 BCrypt 加密存储
- **权限管理**：基于角色的权限控制（Role-Based Access Control）

### 2. 第三方验证服务
- **适用对象**：医院名称不是"系统"的用户
- **验证方式**：使用第三方验证服务（当前保留原有验证逻辑）
- **默认权限**：验证成功后给予"User"角色

## 核心组件

### 1. 用户认证服务 (IUserAuthenticationService)
位置：`Services/UserAuthenticationService.cs`

主要功能：
- 验证用户凭据
- 检查医院配置是否存在且启用
- 验证用户是否属于指定医院
- 密码验证（BCrypt）
- 获取用户角色信息

### 2. 授权辅助类 (AuthorizationHelper)
位置：`Helpers/AuthorizationHelper.cs`

提供的方法：
- `IsLocalSystemUser()` - 检查是否为本地系统用户
- `IsAdminUser()` - 检查是否具有管理员角色
- `IsLocalAdmin()` - 检查是否为本地管理员（同时满足本地用户和管理员角色）
- `GetUserHospitalName()` - 获取用户的医院名称
- `GetUsername()` - 获取用户名

### 3. 本地管理员授权过滤器 (LocalAdminAuthorizationFilter)
位置：`Filters/LocalAdminAuthorizationFilter.cs`

使用方式：
```csharp
[LocalAdminOnly]
public class YourPageModel : PageModel
{
    // 此页面仅限本地系统管理员访问
}
```

## 登录流程

### 本地用户登录流程（医院名称="系统"）

1. 用户在登录页面输入：
   - 医院名称："系统"
   - 用户名
   - 密码

2. 系统检查流程：
   ```
   ┌─────────────────────────┐
   │ 医院名称 == "系统"?     │
   └───────────┬─────────────┘
               │ 是
               ▼
   ┌─────────────────────────┐
   │ 调用本地数据库验证      │
   └───────────┬─────────────┘
               │
               ▼
   ┌─────────────────────────┐
   │ 检查医院配置是否激活    │
   └───────────┬─────────────┘
               │
               ▼
   ┌─────────────────────────┐
   │ 验证用户名和密码        │
   └───────────┬─────────────┘
               │
               ▼
   ┌─────────────────────────┐
   │ 获取用户角色            │
   └───────────┬─────────────┘
               │
               ▼
   ┌─────────────────────────┐
   │ 创建认证票据和Claims    │
   └───────────┬─────────────┘
               │
               ▼
   ┌─────────────────────────┐
   │ 登录成功                │
   └─────────────────────────┘
   ```

3. 创建的 Claims：
   - HospitalName: "系统"
   - Name: 用户名
   - NameIdentifier: 用户名
   - LoginTime: 登录时间
   - SessionId: 会话ID
   - IsLocalUser: "True"
   - Role: 用户的所有角色

### 第三方用户登录流程（医院名称≠"系统"）

1. 用户在登录页面输入：
   - 医院名称：非"系统"的其他医院名称
   - 用户名
   - 密码

2. 系统检查流程：
   ```
   ┌─────────────────────────┐
   │ 医院名称 == "系统"?     │
   └───────────┬─────────────┘
               │ 否
               ▼
   ┌─────────────────────────┐
   │ 调用第三方验证服务      │
   └───────────┬─────────────┘
               │
               ▼
   ┌─────────────────────────┐
   │ 验证成功                │
   └───────────┬─────────────┘
               │
               ▼
   ┌─────────────────────────┐
   │ 分配"User"角色          │
   └───────────┬─────────────┘
               │
               ▼
   ┌─────────────────────────┐
   │ 创建认证票据和Claims    │
   └───────────┬─────────────┘
               │
               ▼
   ┌─────────────────────────┐
   │ 登录成功                │
   └─────────────────────────┘
   ```

3. 创建的 Claims：
   - HospitalName: 实际的医院名称
   - Name: 用户名
   - NameIdentifier: 用户名
   - LoginTime: 登录时间
   - SessionId: 会话ID
   - IsLocalUser: "False"
   - Role: "User"

## 权限控制

### 管理员页面权限

所有管理员页面（位于 `/Pages/Admin/` 目录下）都使用 `[LocalAdminOnly]` 特性进行保护：

- `HospitalServerConfigs` - 医院服务器配置管理
- `HospitalApiConfigs` - 医院API配置管理

访问条件：
1. 用户必须已登录
2. 医院名称必须为"系统"
3. 必须具有"Admin"或"管理员"角色

### 普通页面权限

- `ReportsDashboard` - 需要登录（`[Authorize]`）
- 其他公开页面 - 无需登录

### 访问被拒绝处理

当用户尝试访问没有权限的页面时：
1. 未登录用户 → 重定向到登录页面
2. 已登录但权限不足 → 重定向到 `/AccessDenied` 页面，显示友好的错误信息

## 默认账户

系统初始化时会自动创建：

### 默认管理员账户
- **用户名**：admin
- **密码**：admin123
- **医院名称**：系统
- **角色**：Admin
- **邮箱**：admin@system.com

### 默认医院配置
- **医院名称**：系统
- **医院编码**：SYSTEM
- **状态**：激活

## 安全特性

1. **密码加密**：使用 BCrypt 算法加密存储密码
2. **角色验证**：基于角色的访问控制
3. **会话管理**：每次登录生成唯一的会话ID
4. **医院隔离**：用户只能访问其所属医院的资源
5. **审计日志**：记录登录成功和失败的日志

## 扩展指南

### 添加新的受保护页面

```csharp
using LisReportServer.Filters;

namespace LisReportServer.Pages
{
    [LocalAdminOnly]  // 仅限本地系统管理员
    public class YourNewPageModel : PageModel
    {
        // 页面逻辑
    }
}
```

### 添加新角色

1. 在数据库中添加新角色：
```csharp
var newRole = new Role 
{ 
    Name = "新角色名称", 
    Description = "角色描述" 
};
_context.Roles.Add(newRole);
await _context.SaveChangesAsync();
```

2. 为用户分配角色：
```csharp
var userRole = new UserRole
{
    UserId = userId,
    RoleId = roleId,
    AssignedAt = DateTime.UtcNow
};
_context.UserRoles.Add(userRole);
await _context.SaveChangesAsync();
```

### 检查用户权限（在代码中）

```csharp
// 检查是否为本地管理员
if (AuthorizationHelper.IsLocalAdmin(User))
{
    // 执行管理员操作
}

// 检查是否为本地系统用户
if (AuthorizationHelper.IsLocalSystemUser(User))
{
    // 执行本地用户操作
}

// 获取用户医院名称
var hospitalName = AuthorizationHelper.GetUserHospitalName(User);
```

## 测试建议

### 测试场景

1. **本地管理员登录**
   - 医院名称：系统
   - 用户名：admin
   - 密码：admin123
   - 预期：可以访问所有管理员页面

2. **第三方用户登录**
   - 医院名称：测试医院
   - 用户名/密码：根据第三方验证服务设置
   - 预期：不能访问管理员页面，被重定向到AccessDenied

3. **权限验证**
   - 未登录访问管理员页面 → 重定向到登录页面
   - 普通用户访问管理员页面 → 重定向到AccessDenied页面
   - 管理员访问管理员页面 → 正常访问

## 日志监控

系统会记录以下关键事件：
- 本地用户登录成功/失败
- 第三方用户登录成功/失败
- 权限验证失败
- 数据库初始化状态

查看日志：
```bash
# 查看应用程序日志
tail -f logs/app.log
```

## 常见问题

### Q: 如何修改默认管理员密码？
A: 登录后访问用户管理页面修改密码，或直接在数据库中更新 PasswordHash 字段（使用 BCrypt 加密新密码）。

### Q: 如何添加新的本地用户？
A: 在数据库的 Users 表中插入新记录，确保 HospitalName 为"系统"，并通过 UserRoles 表分配相应角色。

### Q: 第三方用户可以访问哪些页面？
A: 第三方用户只能访问标记为 `[Authorize]` 的普通页面，不能访问标记为 `[LocalAdminOnly]` 的管理员页面。

### Q: 如何禁用某个用户？
A: 在 Users 表中将该用户的 IsActive 字段设置为 false。

## 总结

本系统实现了灵活的分层身份验证机制，支持：
- 本地数据库验证（用于系统管理员）
- 第三方验证服务（用于其他医院用户）
- 基于角色的权限控制
- 细粒度的页面访问控制
- 完善的安全机制和审计日志
