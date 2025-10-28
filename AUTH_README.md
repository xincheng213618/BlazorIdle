# BlazorIdle 认证系统说明

## 功能概述

本项目实现了一个完整的JWT认证系统，包括：

- 用户注册和登录功能
- JWT令牌生成和验证
- 密码使用BCrypt加密存储
- 用户信息保存在SQLite数据库
- 前端自动拦截未登录用户并跳转到登录页面
- 默认内置测试账号

## 默认测试账号

- 用户名: `test123`
- 密码: `test123123`

## 技术栈

### 后端 (BlazorIdle.Server)
- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core with SQLite
- JWT Bearer Authentication
- BCrypt.Net for password hashing

### 前端 (BlazorIdle)
- Blazor WebAssembly (.NET 8.0)
- Blazored.LocalStorage for token storage
- Microsoft.AspNetCore.Components.Authorization

## 项目结构

```
BlazorIdle/
├── BlazorIdle.Server/              # 后端API服务器
│   ├── Controllers/
│   │   └── AuthController.cs       # 认证API控制器
│   ├── Data/
│   │   └── GameDbContext.cs        # 数据库上下文
│   ├── Services/
│   │   ├── JwtService.cs           # JWT令牌生成服务
│   │   └── PasswordHasher.cs       # 密码加密服务
│   └── appsettings.json            # JWT配置
├── BlazorIdle/                     # 前端Blazor应用
│   ├── Pages/
│   │   ├── Home.razor              # 首页（需要登录）
│   │   ├── Login.razor             # 登录页面
│   │   └── Register.razor          # 注册页面
│   ├── Services/
│   │   ├── AuthService.cs          # 认证API调用服务
│   │   └── CustomAuthStateProvider.cs  # 认证状态提供者
│   └── Shared/
│       └── RedirectToLogin.razor   # 未登录自动跳转组件
└── BlazorIdle.Shared/              # 共享模型和DTO
    ├── Models/
    │   └── User.cs                 # 用户模型
    └── DTOs/
        ├── LoginRequest.cs
        ├── RegisterRequest.cs
        └── AuthResponse.cs
```

## API端点

### POST /api/auth/login
登录接口

**请求体:**
```json
{
  "username": "test123",
  "password": "test123123"
}
```

**成功响应:**
```json
{
  "success": true,
  "message": "登录成功",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "test123"
}
```

### POST /api/auth/register
注册接口

**请求体:**
```json
{
  "username": "newuser",
  "password": "password123"
}
```

**成功响应:**
```json
{
  "success": true,
  "message": "注册成功",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "newuser"
}
```

## 运行项目

### 1. 启动后端服务器

```bash
cd BlazorIdle.Server
dotnet run
```

服务器将在以下端口启动:
- HTTPS: https://localhost:7056
- HTTP: http://localhost:5056

### 2. 启动前端应用

```bash
cd BlazorIdle
dotnet run
```

前端应用将在以下端口启动:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

### 3. 访问应用

打开浏览器访问 `https://localhost:5001`

如果未登录，将自动跳转到登录页面。

## 数据库

应用使用SQLite数据库，数据库文件位于 `BlazorIdle.Server/gamedata.db`

### 数据库表结构

**Users表:**
- Id (INTEGER, PRIMARY KEY)
- Username (TEXT, UNIQUE, NOT NULL)
- PasswordHash (TEXT, NOT NULL)
- CreatedAt (TEXT, NOT NULL)

## JWT配置

JWT配置在 `appsettings.json` 中：

```json
{
  "Jwt": {
    "Key": "YourSecretKeyForJWTTokenGenerationMustBeAtLeast32CharactersLong",
    "Issuer": "BlazorIdle.Server",
    "Audience": "BlazorIdle.Client"
  }
}
```

令牌有效期：7天

## 安全特性

1. **密码加密**: 使用BCrypt算法对密码进行哈希存储
2. **JWT令牌**: 使用HMAC-SHA256签名算法
3. **自动拦截**: 未登录用户自动跳转到登录页
4. **令牌存储**: JWT令牌存储在浏览器的LocalStorage中
5. **CORS配置**: 限制API访问来源

## 测试验证

所有认证功能已通过测试:
- ✅ 使用正确凭据登录返回JWT令牌
- ✅ 使用错误凭据登录返回错误信息
- ✅ 注册新用户并返回JWT令牌
- ✅ 注册重复用户名返回错误信息
- ✅ 默认测试用户在数据库初始化时自动创建

## 常见问题

### 1. 如何修改JWT密钥？

编辑 `BlazorIdle.Server/appsettings.json` 文件中的 `Jwt:Key` 值。密钥长度必须至少32个字符。

### 2. 如何修改令牌有效期？

编辑 `BlazorIdle.Server/Services/JwtService.cs` 中的 `expires` 参数：

```csharp
expires: DateTime.UtcNow.AddDays(7)  // 修改为所需天数
```

### 3. 如何重置数据库？

删除 `BlazorIdle.Server/gamedata.db` 文件，重新启动后端服务器将自动创建新数据库并添加默认测试用户。

## 注意事项

1. 本项目的JWT密钥仅用于开发测试，生产环境需要使用更安全的密钥
2. 生产环境建议启用HTTPS
3. 建议定期更新依赖包以获取安全更新
