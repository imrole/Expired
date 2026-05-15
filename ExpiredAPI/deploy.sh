#!/bin/bash
# ============================================
# 冰箱管家 - 后端 API 部署脚本
# 在目标服务器上执行
# ============================================

set -e

# 配置
APP_NAME="ExpiredAPI"
APP_DIR="/opt/$APP_NAME"
PUBLISH_DIR="$APP_DIR/publish"
SERVICE_NAME="expired-api"

echo "============================================"
echo "  冰箱管家 API 部署脚本"
echo "============================================"

# 检测操作系统
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    OS="linux"
elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]] || [[ -n "$WINDIR" ]]; then
    OS="windows"
else
    OS="unknown"
fi
echo "检测到操作系统: $OS"

# Step 1: 检查 .NET 运行时
echo ""
echo "[1/4] 检查 .NET 运行时..."
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "  ✅ .NET 已安装，版本: $DOTNET_VERSION"
else
    echo "  ❌ .NET 未安装！请先安装 .NET 10 Runtime"
    echo "     https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
fi

# Step 2: 还原 NuGet 包
echo ""
echo "[2/4] 还原 NuGet 包..."
cd "$APP_DIR"
dotnet restore
echo "  ✅ NuGet 包还原完成"

# Step 3: 编译发布
echo ""
echo "[3/4] 编译发布..."
dotnet publish -c Release -o "$PUBLISH_DIR" --self-contained false
echo "  ✅ 编译发布完成 → $PUBLISH_DIR"

# Step 4: 配置服务
echo ""
echo "[4/4] 配置服务..."

if [[ "$OS" == "linux" ]]; then
    # 创建 systemd 服务
    SERVICE_FILE="/etc/systemd/system/$SERVICE_NAME.service"
    if [ ! -f "$SERVICE_FILE" ]; then
        cat > /tmp/$SERVICE_NAME.service << 'EOF'
[Unit]
Description=冰箱管家 API Service
After=network.target

[Service]
WorkingDirectory=/opt/ExpiredAPI/publish
ExecStart=/usr/bin/dotnet /opt/ExpiredAPI/publish/ExpiredAPI.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=expired-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
EOF
        sudo mv /tmp/$SERVICE_NAME.service $SERVICE_FILE
        sudo systemctl daemon-reload
        echo "  ✅ systemd 服务已创建: $SERVICE_FILE"
    else
        echo "  ℹ️ 服务文件已存在，跳过创建"
    fi

    # 启动服务
    sudo systemctl enable $SERVICE_NAME
    sudo systemctl restart $SERVICE_NAME
    echo "  ✅ 服务已启动"

    # 检查状态
    sleep 2
    if systemctl is-active --quiet $SERVICE_NAME; then
        echo "  ✅ 服务运行正常"
        sudo systemctl status $SERVICE_NAME --no-pager | head -10
    else
        echo "  ❌ 服务启动失败，请查看日志: sudo journalctl -u $SERVICE_NAME -f"
    fi

elif [[ "$OS" == "windows" ]]; then
    # Windows 服务（使用 sc 命令或直接前台运行）
    echo "  ℹ️ Windows 环境，使用以下命令启动："
    echo "     cd $PUBLISH_DIR"
    echo "     dotnet ExpiredAPI.dll --urls http://0.0.0.0:5000"
    echo ""
    echo "  或创建 Windows 服务："
    echo "     sc create $SERVICE_NAME binPath=\"dotnet $PUBLISH_DIR\\ExpiredAPI.dll\""
fi

echo ""
echo "============================================"
echo "  ✅ 部署完成！"
echo "  API 地址：http://<服务器IP>:5000"
echo "============================================"
echo ""
echo "验证健康状态："
echo "  curl http://localhost:5000/api/food"
