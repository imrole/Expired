@echo off
REM ============================================
REM 冰箱管家 - 后端 API Windows 部署脚本
REM 在目标 Windows Server 上以管理员身份运行
REM ============================================

echo ============================================
echo   冰箱管家 API 部署脚本 (Windows)
echo ============================================

REM Step 1: 检查 .NET Runtime
echo.
echo [1/4] 检查 .NET 运行时...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] .NET 未安装！请先安装 .NET 10 Runtime
    echo        https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)
for /f %%i in ('dotnet --version') do echo [OK] .NET %%i

REM Step 2: 还原 NuGet 包
echo.
echo [2/4] 还原 NuGet 包...
dotnet restore
if %errorlevel% neq 0 (
    echo [错误] NuGet 还原失败，检查网络连接
    pause
    exit /b 1
)
echo [OK] NuGet 还原完成

REM Step 3: 编译发布
echo.
echo [3/4] 编译发布...
dotnet publish -c Release -o publish --self-contained false
if %errorlevel% neq 0 (
    echo [错误] 编译失败
    pause
    exit /b 1
)
echo [OK] 编译发布完成

REM Step 4: 启动服务（后台运行）
echo.
echo [4/4] 启动服务...
echo 正在启动 API 服务 (端口 5000)...
echo.
echo 查看日志文件: publish\app.log
echo.

cd publish
start "" dotnet ExpiredAPI.dll --urls http://0.0.0.0:5000 > app.log 2>&1

echo ============================================
echo  部署完成！
echo  API 地址: http://localhost:5000
echo  进程 PID: 请在任务管理器中查看
echo ============================================
echo.
echo 测试: curl http://localhost:5000/api/food
echo.
pause
