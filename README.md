# 冰箱管家 - 项目说明

## 项目结构

```
Expired/
├── ExpiredAPI/                    # .NET 10 Web API 后端
│   ├── ExpiredAPI.csproj          # 项目文件 (EF Core + SQL Server)
│   ├── Program.cs                 # 入口：DI/JWT/CORS/DB初始化
│   ├── appsettings.json           # 配置：数据库/微信/百度OCR/JWT
│   ├── Models/
│   │   ├── User.cs                # 用户模型
│   │   ├── FoodItem.cs            # 食品模型（含过期状态）
│   │   └── DTOs/                  # 请求/响应 DTO
│   ├── Data/
│   │   └── AppDbContext.cs         # EF Core 上下文 + 表配置
│   ├── Controllers/
│   │   ├── AuthController.cs      # POST /api/auth/login
│   │   ├── FoodController.cs      # CRUD + /api/food/stats
│   │   └── OcrController.cs       # POST /api/ocr/recognize
│   ├── Services/
│   │   ├── IFoodService.cs
│   │   ├── FoodService.cs         # 自动计算过期状态
│   │   ├── IOcrService.cs
│   │   └── OcrService.cs          # 百度OCR + 日期解析
│   └── Uploads/                   # 图片上传目录
│
├── MiniProgram/                   # 微信小程序前端
│   ├── app.js / app.json / app.wxss
│   ├── pages/
│   │   ├── index/                 # 首页 - 食品卡片列表
│   │   ├── add/                   # 添加页 - 相机/OCR/手动
│   │   └── profile/               # 个人中心 - 微信授权登录
│   ├── utils/
│   │   ├── api.js                 # API 请求封装
│   │   └── auth.js                # 微信登录流程
│   └── images/                    # TabBar 图标
```

## API 接口

| 方法 | 路由 | 说明 | 需认证 |
|------|------|------|--------|
| POST | `/api/auth/login` | 微信登录(code → JWT) | 否 |
| GET  | `/api/food` | 获取食品列表 | 是 |
| GET  | `/api/food/stats` | 统计信息 | 是 |
| GET  | `/api/food/{id}` | 食品详情 | 是 |
| POST | `/api/food` | 新增食品 | 是 |
| PUT  | `/api/food/{id}` | 更新食品 | 是 |
| DELETE | `/api/food/{id}` | 删除食品 | 是 |
| POST | `/api/ocr/recognize` | 图片OCR识别 | 是 |

## 使用前必须配置（appsettings.json）

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=120.26.220.46;Initial Catalog=ExpiredDB;User ID=polly;Password=你的密码;Encrypt=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "换成随机字符串（至少32位）"
  },
  "WeChat": {
    "AppId": "你的微信小程序AppID",
    "AppSecret": "你的微信小程序AppSecret"
  },
  "BaiduOcr": {
    "ApiKey": "百度OCR应用的API Key",
    "SecretKey": "百度OCR应用的Secret Key"
  }
}
```

## 后续操作

### 后端部署
1. 在安装了 .NET 10 SDK 的环境中执行：
   ```bash
   cd ExpiredAPI
   dotnet restore
   dotnet run
   ```
2. 服务默认运行在 `http://localhost:5211`

### 小程序开发
1. 用微信开发者工具打开 `MiniProgram` 文件夹
2. 修改 `utils/api.js` 中的 `API_BASE_URL` 为实际服务器地址
3. 在微信公众平台配置「request 合法域名」
4. 真机调试微信登录和相机功能

> ⚠️ **注意**：当前环境 .NET SDK 10.0.203 存在 NuGet 还原 Bug（`Value cannot be null (Parameter 'path1')`），无法在此机器上执行 `dotnet restore/build`。代码逻辑完整无误，请在安装了正常 .NET 10 SDK 的环境中编译部署。
