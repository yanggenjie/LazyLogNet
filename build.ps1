# LazyLogNet 自动构建脚本
# 确保在编译时自动复制DLL到正确的bin目录

param(
    [string]$Configuration = "Release"
)

Write-Host "开始构建 LazyLogNet..." -ForegroundColor Green

# 设置工作目录
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptPath

# 清理旧的输出
Write-Host "清理旧的输出文件..." -ForegroundColor Yellow
if (Test-Path "source\bin") {
    Remove-Item "source\bin\*" -Force -Recurse -ErrorAction SilentlyContinue
}

# 构建项目
Write-Host "构建 LazyLogNet 项目..." -ForegroundColor Yellow
dotnet build "source\LazyLogNet\LazyLogNet.csproj" -c $Configuration

if ($LASTEXITCODE -eq 0) {
    Write-Host "构建成功！" -ForegroundColor Green
    
    # 检查输出文件
    if (Test-Path "source\bin\LazyLogNet.dll") {
        Write-Host "DLL 文件已成功输出到 source\bin\LazyLogNet.dll" -ForegroundColor Green
    } else {
        Write-Host "警告：未找到输出的 DLL 文件" -ForegroundColor Red
    }
    
    # 显示输出目录内容
    Write-Host "输出目录内容：" -ForegroundColor Cyan
    if (Test-Path "source\bin") {
        Get-ChildItem "source\bin" | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor White }
    }
    
} else {
    Write-Host "构建失败！" -ForegroundColor Red
    exit 1
}

Write-Host "构建完成！" -ForegroundColor Green