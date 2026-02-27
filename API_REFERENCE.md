# LISView API参文档

##概

LISView 提供了完整的 RESTful API接口，支持报告查询、患者管理、医院配置等功能。

##认证方式

### 1. 本地认证（Cookie）
- 通过登录页面获取认证 Cookie
- 自动附加到后续请求

### 2. SSO认证（Bearer Token）
- 通过 OAuth2流获取访问令牌
- 在请求头中添加：`Authorization: Bearer <token>`

## API端点

###报告服务 API

#### 获取报告列表
```http
GET /api/report/reports
GET /api/report/reports?patientId={patientId}
GET /api/report/reports?examId={examId}
GET /api/report/reports?outpatientId={outpatientId}
```

**参数说明：**
- `patientId`: 住院号
- `examId`:检查号
- `outpatientId`: 门诊号

**响应示例：**
```json
{
  "status": "success",
  "data": [
    {
      "id": "R001",
      "patientId": "P001",
      "patientName": "张三",
      "examId": "E001",
      "reportName": "血常规检查",
      "status": "completed",
      "createdAt": "2024-01-15T10:30:00Z",
      "updatedAt": "2024-01-15T11:45:00Z"
    }
  ]
}
```

#### 获取患者列表
```http
GET /api/report/patients
GET /api/report/patients?examId={examId}
GET /api/report/patients?patientId={patientId}
GET /api/report/patients?outpatientId={outpatientId}
```

**响应示例：**
```json
{
  "status": "success",
  "data": [
    {
      "patientId": "P001",
      "patientName": "张三",
      "examId": "E001",
      "outpatientId": "OP001",
      "department": "检验科",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

###医服务 API

#### 获取医院健康状态
```http
GET /api/hospitals/{hospitalCode}/health
```

**响应示例：**
```json
{
  "status": "Healthy",
  "responseTime": 45,
  "timestamp": "2024-01-15T11:30:00Z",
  "message": "Connection successful"
}
```

#### 获取医院患者列表
```http
GET /api/hospitals/{hospitalCode}/patients
```

#### 获取医院报告列表
```http
GET /api/hospitals/{hospitalCode}/reports
GET /api/hospitals/{hospitalCode}/reports/{reportId}
```

#### 上传报告
```http
POST /api/hospitals/{hospitalCode}/reports
Content-Type: application/json

{
  "patientId": "P001",
  "examId": "E001",
  "reportName": "血常规检查",
  "reportData": "..."
}
```

#### 更新报告状态
```http
PUT /api/hospitals/{hospitalCode}/reports/{reportId}/status
Content-Type: application/json

{
  "status": "completed",
  "notes": "检查完成"
}
```

###健康检查 API

#### 系统健康状态
```http
GET /api/health/status
```

**响应示例：**
```json
{
  "overallStatus": "Healthy",
  "checks": [
    {
      "name": "Database",
      "status": "Healthy",
      "responseTime": 12
    },
    {
      "name": "SSO Service",
      "status": "Healthy",
      "responseTime": 25
    }
  ],
  "timestamp": "2024-01-15T11:30:00Z"
}
```

#### 详细健康信息
```http
GET /api/health/details
```

###认证 API

#### 用户登录
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

#### 用户登出
```http
POST /api/auth/logout
```

####刷新令牌
```http
POST /api/auth/refresh
Authorization: Bearer <refresh_token>
```

##错误处理

###标错误响应格式
```json
{
  "status": "error",
  "message": "错误描述信息",
  "errorCode": "ERROR_CODE",
  "timestamp": "2024-01-15T11:30:00Z"
}
```

###常错误码
- `400`: 请求参数错误
- `401`: 未认证
- `403`:权不足
- `404`:不存在
- `500`: 服务器内部错误
- `503`: 服务不可用

###错误示例
```json
{
  "status": "error",
  "message": "患者信息不存在",
  "errorCode": "PATIENT_NOT_FOUND",
  "timestamp": "2024-01-15T11:30:00Z"
}
```

## 请求头

###标请求头
```http
Accept: application/json
Content-Type: application/json
X-Timezone: Asia/Shanghai
X-Request-ID: unique-request-id
```

###认证相关头
```http
Authorization: Bearer <access_token>
X-Hospital-Code: hospital001
```

##响应格式

### 成功响应
```json
{
  "status": "success",
  "data": { /*具数据数据 */ },
  "message": "操作成功",
  "timestamp": "2024-01-15T11:30:00Z"
}
```

### 分页响应
```json
{
  "status": "success",
  "data": [ /* 数据列表 */ ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "total": 100,
    "totalPages": 5
  },
  "timestamp": "2024-01-15T11:30:00Z"
}
```

## 数据模型

### Report模型
```json
{
  "id": "string",
  "patientId": "string",
  "patientName": "string",
  "examId": "string",
  "reportName": "string",
  "reportData": "string",
  "status": "pending|processing|completed|failed",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T11:45:00Z"
}
```

### Patient模型
```json
{
  "patientId": "string",
  "patientName": "string",
  "examId": "string",
  "outpatientId": "string",
  "department": "string",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### HealthCheck 模型
```json
{
  "name": "string",
  "status": "Healthy|Unhealthy|Degraded",
  "responseTime": "number",
  "message": "string",
  "timestamp": "2024-01-15T11:30:00Z"
}
```

## 限流和配额

### API 限流策略
-匿用户：100 请求/小时
-认证用户：1000 请求/小时
-员：无限制

###配额管理
-报查询：每日 10000-患查询：每日 5000-健康检查：每分钟 60##版本控制

### API版本
当前版本：v1

###版本控制方式
通过 URL路径控制：
```
/api/v1/report/reports
/api/v1/hospitals/{code}/patients
```

##客端示例

### JavaScript (Fetch)
```javascript
// 获取报告列表
async function getReports(patientId) {
  const response = await fetch(`/api/report/reports?patientId=${patientId}`, {
    headers: {
      'Authorization': `Bearer ${accessToken}`,
      'X-Timezone': 'Asia/Shanghai'
    }
  });
  
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  
  const data = await response.json();
  return data.data;
}
```

### C# (HttpClient)
```csharp
public async Task<List<Report>> GetReportsAsync(string patientId)
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", accessToken);
    client.DefaultRequestHeaders.Add("X-Timezone", "Asia/Shanghai");
    
    var response = await client.GetAsync(
        $"/api/report/reports?patientId={patientId}");
    
    response.EnsureSuccessStatusCode();
    
    var json = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<ApiResponse<List<Report>>>(json);
    return result.Data;
}
```

### Python (requests)
```python
import requests

def get_reports(patient_id, access_token):
    headers = {
        'Authorization': f'Bearer {access_token}',
        'X-Timezone': 'Asia/Shanghai'
    }
    
    response = requests.get(
        f'/api/report/reports?patientId={patient_id}',
        headers=headers
    )
    
    response.raise_for_status()
    return response.json()['data']
```

## 最佳实践

### 1.错误处理
```javascript
try {
  const reports = await getReports(patientId);
  //处理成功响应
} catch (error) {
  if (error.status === 401) {
    //处理认证失败
    redirectToLogin();
  } else if (error.status === 403) {
    // 处理权限不足
    showPermissionDenied();
  } else {
    //处理其他错误
    showErrorMessage(error.message);
  }
}
```

### 2. 重试机制
```javascript
async function retryableRequest(url, options, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    try {
      const response = await fetch(url, options);
      return response;
    } catch (error) {
      if (i === maxRetries - 1) throw error;
      await new Promise(resolve => setTimeout(resolve, 1000 * (i + 1)));
    }
  }
}
```

### 3. 请求超时
```javascript
const controller = new AbortController();
const timeoutId = setTimeout(() => controller.abort(), 10000);

try {
  const response = await fetch('/api/report/reports', {
    signal: controller.signal
  });
  //处理响应
} finally {
  clearTimeout(timeoutId);
}
```

##变更日志

### v1.2.0 (2024-01-15)
- 新增时区处理API
- 优化健康检查接口
- 添加请求ID跟踪

### v1.1.0 (2024-01-10)
- 新增患者信息查询API
-改错误处理机制
- 添加API限流功能

### v1.0.0 (2024-01-01)
-初始版本发布
-基础报告查询功能
-医配置管理API

##技术支持

如需获取API相关的技术支持，请联系：
- 文档：查看完整项目文档
- 日志：检查应用日志文件
-社：项目维护团队