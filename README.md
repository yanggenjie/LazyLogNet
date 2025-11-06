```
# LazyLogNet

[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](#)

一个高性能、轻量级的 .NET 日志库，专为现代应用程序设计。支持异步日志记录、智能文件管理、文件轮转和结构化日志。

## ✨ 核心特性

- 🚀 **高性能异步处理** - 基于自定义队列的无锁设计，优化的批量写入
- 📁 **智能文件管理** - 自动轮转、大小控制、智能路径选择
- 🔄 **日志轮转** - 支持基于文件大小的自动日志轮转
- 📊 **结构化日志** - 支持属性字典和模板替换
- 🎯 **零配置启动** - 开箱即用，智能默认配置
- 🌈 **彩色控制台输出** - 不同日志级别使用不同颜色
- 🔒 **线程安全** - 完全线程安全的设计
- 📦 **最小依赖** - 仅依赖 .NET Standard 2.0 和 Newtonsoft.Json
- 🔄 **自动资源管理** - 程序退出时自动清理，无需手动调用
- ✅ **配置验证** - 内置参数验证，确保配置合理性
```

## ✨ 核心特性

- 🚀 **高性能异步处理** - 基于自定义队列的无锁设计，优化的批量写入
- 📁 **智能文件管理** - 自动轮转、大小控制、智能路径选择
- 🗂️ **可配置日志文件夹** - 支持为不同应用程序配置专用日志目录
- 📊 **结构化日志** - 支持 JSON、键值对等多种格式
- 🎯 **零配置启动** - 开箱即用，智能默认配置
- 🌈 **彩色控制台输出** - 不同日志级别使用不同颜色
- 🔒 **线程安全** - 完全线程安全的设计
- 📦 **最小依赖** - 仅依赖 .NET Standard 2.0
- 🔄 **自动资源管理** - 程序退出时自动清理，无需手动调用
- ✅ **配置验证** - 内置参数验证，确保配置合理性
- 🔄 **日志轮转** - 支持基于文件大小的自动日志轮转

## 🎯 兼容性

- .NET Framework 4.6.1+
- .NET Core 2.0+
- .NET 5/6/7/8+
- Xamarin
- Unity (部分支持)

## 📚 依赖与版本

- 目标框架
  - `netstandard2.0`
- NuGet 依赖
  - `Microsoft.Extensions.DependencyInjection.Abstractions` `6.0.0` — 依赖注入扩展方法（用于 `IServiceCollection` 扩展）
  - `Newtonsoft.Json` `13.0.1` — 结构化日志的 JSON 序列化
- 包引用示例（源码项目）
  ```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="6.0.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
  </ItemGroup>
  ```
- 打包说明
  - `Package.nuspec` 中为兼容旧项目设置了最低兼容版本：
    - `Microsoft.Extensions.DependencyInjection.Abstractions` `2.0.0`
  - 推荐按源码项目版本使用，确保与示例和扩展方法一致。

## 📦 安装

### 通过项目引用
```xml
<ProjectReference Include="path/to/LazyLogNet/LazyLogNet.csproj" />
```

### 通过 DLL 引用
直接引用编译生成的 `LazyLogNet.dll` 文件。

### 依赖注入支持
LazyLogNet 支持依赖注入模式，可以轻松集成到 ASP.NET Core、WPF、WinForms 等应用程序中。需要安装 Microsoft.Extensions.DependencyInjection.Abstractions 包。

```xml
<!-- 在项目中添加依赖注入支持 -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="6.0.0" />
```

## 🚀 快速开始

### 基础使用

```csharp
using LazyLogNet;

// 直接使用，自动初始化
LazyLogHelper.Info("应用程序启动");
LazyLogHelper.Warn("这是一个警告");
LazyLogHelper.Error("发生了错误");

// 无需调用 Shutdown，程序退出时自动清理
```

### 文件日志配置

```csharp
using LazyLogNet;

// 启用文件输出
LazyLogHelper.Initialize(new LazyLoggerConfiguration
{
    EnableConsole = true,
    EnableFile = true,
    FilePath = "logs/app.log"
});

LazyLogHelper.Info("日志将同时输出到控制台和文件");
```

### 依赖注入使用

```csharp
using Microsoft.Extensions.DependencyInjection;
using LazyLogNet;

// 创建服务容器
var services = new ServiceCollection();

// 注册LazyLogNet服务
services.AddLazyLogNet();

// 注册业务服务
services.AddTransient<UserService>();

// 构建服务提供者
using var serviceProvider = services.BuildServiceProvider();

// 获取服务并使用
var userService = serviceProvider.GetRequiredService<UserService>();

// 业务服务类
public class UserService
{
    private readonly ILazyLogger _logger;

    public UserService(ILazyLogger logger)
    {
        _logger = logger;
    }

    public void CreateUser(string name)
    {
        _logger.Info($"创建用户: {name}");
    }
}
```

### 文件路径配置

```csharp
using LazyLogNet;

// 使用默认文件路径配置
LazyLogHelper.Initialize(new LazyLoggerConfiguration 
{ 
    EnableFile = true
});
LazyLogHelper.Info("使用默认文件路径的日志");

// 使用自定义文件路径
LazyLogHelper.Initialize(new LazyLoggerConfiguration 
{ 
    EnableFile = true,
    FilePath = "D:/MyApp/logs/app.log"
});
LazyLogHelper.Info("使用自定义文件路径的日志");

// 使用配置对象
var config = new LazyLoggerConfiguration
{
    EnableFile = true,
    FilePath = "custom-app.log"
};
using var logger = new LazyLogger(config);
logger.Info("自定义文件路径的日志");
```

## 🔌 依赖注入配置

### 扩展方法

LazyLogNet 提供了扩展方法来简化依赖注入配置：

```csharp
using Microsoft.Extensions.DependencyInjection;
using LazyLogNet;

var services = new ServiceCollection();

// 使用默认配置
services.AddLazyLogNet();

// 使用自定义配置
services.AddLazyLogNet(config =>
{
    config.EnableConsole = true;
    config.EnableFile = true;
    config.FilePath = "my-app.log";
    config.MinLevel = LazyLogLevel.Debug;
    config.MaxFileSize = 10 * 1024 * 1024; // 10MB
    config.MaxRetainedFiles = 5;
});

// 使用预设配置
services.AddLazyLogNetConsole();                    // 仅控制台
services.AddLazyLogNetFile("my-logs");              // 仅文件（使用默认文件名）
services.AddLazyLogNetFileWithPath("D:/logs/app.log");     // 仅文件（指定路径）
```

### ASP.NET Core 集成

```csharp
// Program.cs (ASP.NET Core 6+)
using LazyLogNet;

var builder = WebApplication.CreateBuilder(args);

// 添加LazyLogNet服务
builder.Services.AddLazyLogNet(config =>
{
    config.EnableConsole = true;
    config.EnableFile = true;
    config.FilePath = "webapp.log";
    config.MinLevel = LazyLogLevel.Debug;
    config.MaxFileSize = 50 * 1024 * 1024; // 50MB
    config.MaxRetainedFiles = 10;
});

// 添加其他服务
builder.Services.AddControllers();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// 配置管道
app.MapControllers();
app.Run();

// 控制器中使用
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ILazyLogger _logger;
    private readonly IUserService _userService;

    public UsersController(ILazyLogger logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        _logger.InfoStructured("创建用户请求", new Dictionary<string, object>
        {
            ["UserName"] = request.Name,
            ["Email"] = request.Email,
            ["RequestId"] = HttpContext.TraceIdentifier
        });

        try
        {
            var user = await _userService.CreateUserAsync(request);
            _logger.InfoStructured("用户创建成功", new Dictionary<string, object>
            {
                ["UserId"] = user.Id,
                ["UserName"] = user.Name
            });
            
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.ErrorStructured("用户创建失败", new Dictionary<string, object>
            {
                ["UserName"] = request.Name,
                ["Email"] = request.Email,
                ["Error"] = ex.Message
            }, ex);
            
            return BadRequest("用户创建失败");
        }
    }
}
```

### WPF/WinForms 集成

```csharp
// App.xaml.cs (WPF) 或 Program.cs (WinForms)
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LazyLogNet;

public partial class App : Application
{
    private IHost _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // 添加LazyLogNet
                services.AddLazyLogNet(config =>
                {
                    config.EnableConsole = false;  // WPF通常不需要控制台输出
                    config.EnableFile = true;
                    config.FilePath = "wpf-app.log";
                    config.MaxFileSize = 20 * 1024 * 1024; // 20MB
                });

                // 添加窗口和服务
                services.AddTransient<MainWindow>();
                services.AddSingleton<IDataService, DataService>();
            })
            .Build();

        base.OnStartup(e);

        // 显示主窗口
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}

// MainWindow.xaml.cs
public partial class MainWindow : Window
{
    private readonly ILazyLogger _logger;
    private readonly IDataService _dataService;

    public MainWindow(ILazyLogger logger, IDataService dataService)
    {
        _logger = logger;
        _dataService = dataService;
        InitializeComponent();
        
        _logger.Info("主窗口已初始化");
    }

    private async void LoadDataButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.Info("开始加载数据");
        
        try
        {
            var data = await _dataService.LoadDataAsync();
            _logger.InfoStructured("数据加载完成", new Dictionary<string, object>
            {
                ["RecordCount"] = data.Count,
                ["LoadTime"] = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.Error("数据加载失败", ex);
            MessageBox.Show("数据加载失败，请查看日志了解详情。");
        }
    }
}
```

### 控制台应用程序集成

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LazyLogNet;

class Program
{
    static async Task Main(string[] args)
    {
        // 创建主机
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 添加LazyLogNet
                services.AddLazyLogNet(config =>
                {
                    config.EnableConsole = true;
                    config.EnableFile = true;
                    config.FilePath = "console-app.log";
                    config.MinLevel = LazyLogLevel.Debug;
                });

                // 添加应用服务
                services.AddSingleton<IApplication, Application>();
                services.AddTransient<IWorkerService, WorkerService>();
            })
            .Build();

        // 运行应用
        var app = host.Services.GetRequiredService<IApplication>();
        await app.RunAsync();
    }
}

public interface IApplication
{
    Task RunAsync();
}

public class Application : IApplication
{
    private readonly ILazyLogger _logger;
    private readonly IWorkerService _workerService;

    public Application(ILazyLogger logger, IWorkerService workerService)
    {
        _logger = logger;
        _workerService = workerService;
    }

    public async Task RunAsync()
    {
        _logger.Info("应用程序启动");

        try
        {
            await _workerService.DoWorkAsync();
            _logger.Info("应用程序正常结束");
        }
        catch (Exception ex)
        {
            _logger.Fatal("应用程序异常退出", ex);
            throw;
        }
    }
}
```

## 📚 详细配置

### LazyLoggerConfiguration 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableConsole` | `bool` | `true` | 启用控制台输出 |
| `EnableFile` | `bool` | `false` | 启用文件输出 |
| `FilePath` | `string` | `null` | 完整的日志文件路径（最高优先级） |
| `FilePath` | `string` | `null` | 日志文件路径（优先使用此配置） |
| `MinLevel` | `LogLevel` | `LogLevel.Info` | 最小日志级别 |
| `MaxFileSize` | `long` | `10MB` | 单个文件最大大小 |
| `MaxRetainedFiles` | `int` | `5` | 最大保留文件数 |
| `QueueCapacity` | `int` | `1024` | 日志队列容量 |
| `BatchSize` | `int` | `100` | 批处理大小 |
| `FlushIntervalMs` | `int` | `1000` | 刷新间隔（毫秒） |
| `FileBufferSize` | `int` | `4096` | 文件缓冲区大小 |
| `EnableLogRotation` | `bool` | `true` | 是否启用日志轮转 |
| `FileNamePattern` | `string` | `"{0}_{1:yyyyMMdd_HHmmss}.log"` | 日志文件名模式 |


### 日志级别

```csharp
public enum LazyLogLevel
{
    Debug = 0,    // 调试信息
    Info = 1,     // 一般信息
    Warn = 2,     // 警告信息
    Error = 3,    // 错误信息
    Fatal = 4     // 严重错误
}
```

### 预设配置

```csharp
using LazyLogNet;

// 仅控制台输出
using var consoleLogger = new LazyLogger(LazyLoggerConfiguration.ConsoleOnly);

// 仅文件输出
using var fileLogger = new LazyLogger(LazyLoggerConfiguration.FileOnly);

// 默认配置（控制台 + 文件）
using var defaultLogger = new LazyLogger(LazyLoggerConfiguration.Default);

// 指定日志文件
using var customLogger = new LazyLogger(LazyLoggerConfiguration.WithFilePath("my-app.log"));

// 指定完整文件路径
using var pathLogger = new LazyLogger(LazyLoggerConfiguration.WithFilePath("D:\\MyApp\\logs\\app.log"));
```

## 📁 文件路径配置

### 路径配置优先级

1. **完整文件路径** (`FilePath` 属性) - 最高优先级
2. **默认路径** - 使用当前目录下的日志文件

## 📊 结构化日志

### 使用示例

```csharp
using LazyLogNet;
using System.Collections.Generic;

// 记录结构化日志
var properties = new Dictionary<string, object>
{
    ["UserId"] = 12345,
    ["Action"] = "登录",
    ["IP"] = "192.168.1.100"
};

LazyLogHelper.InfoStructured("用户登录成功", properties);
```

## 🔧 高级配置

### 自定义配置

```csharp
using LazyLogNet;

var config = new LazyLoggerConfiguration
{
    EnableConsole = true,
    EnableFile = true,
    LogFolderName = "my-app-logs",
    MinLevel = LazyLogLevel.Debug,
    MaxFileSize = 50 * 1024 * 1024,  // 50MB
    MaxRetainedFiles = 10,
    QueueCapacity = 2048,
    BatchSize = 200,
    FlushIntervalMs = 500,

    EnableLogRotation = true,
    FileNamePattern = "app_{1:yyyyMMdd_HHmmss}.log"
};

LazyLogHelper.Initialize(config);
```

### Logger 实例使用

```csharp
using LazyLogNet;

// 使用自定义配置
var config = LazyLoggerConfiguration.WithLogFolder("my-app-logs");
using var logger = new LazyLogger(config);

logger.Debug("调试信息");
logger.Info("一般信息");
logger.Warn("警告信息");
logger.Error("错误信息");
logger.Fatal("严重错误");

// 带异常的日志
try
{
    // 可能抛出异常的代码
}
catch (Exception ex)
{
    logger.Error("操作失败", ex);
}

// 结构化日志
var userAction = new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["Action"] = "CreateProject",
    ["ProjectId"] = projectId
};
logger.InfoStructured("用户创建项目", userAction);
```

## 🔄 日志轮转

### 自动轮转功能

```csharp
var config = new LazyLoggerConfiguration
{
    EnableFile = true,
    EnableLogRotation = true,
    MaxFileSize = 10 * 1024 * 1024,  // 10MB
    MaxRetainedFiles = 5,
    FileNamePattern = "app_{1:yyyyMMdd_HHmmss}.log"
};

// 当日志文件达到10MB时，自动创建新文件
// 保留最近5个日志文件，自动删除旧文件
```

### 文件命名模式

- `{0}`: 基础文件名（不含扩展名）
- `{1}`: 时间戳（DateTime格式）

示例：
- `"app_{1:yyyyMMdd}.log"` → `app_20240115.log`
- `"{0}_{1:yyyyMMdd_HHmmss}.log"` → `app_20240115_143022.log`

## ✅ 配置验证

### 自动验证

```csharp
var config = new LazyLoggerConfiguration
{
    QueueCapacity = -1,  // 无效值
    BatchSize = 0        // 无效值
};

// 验证配置
var (isValid, errors) = config.Validate();
if (!isValid)
{
    Console.WriteLine("配置错误:");
    foreach (var error in errors)
    {
        Console.WriteLine($"- {error}");
    }
}

// 或者直接抛出异常
try
{
    config.ValidateAndThrow();
}
catch (ArgumentException ex)
{
    Console.WriteLine($"配置无效: {ex.Message}");
}
```

## 🎯 最佳实践

### 1. 依赖注入模式

```csharp
// 推荐：使用依赖注入
public class OrderService
{
    private readonly ILazyLogger _logger;
    private readonly IOrderRepository _repository;

    public OrderService(ILazyLogger logger, IOrderRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.InfoStructured("开始创建订单", new Dictionary<string, object>
        {
            ["CustomerId"] = request.CustomerId,
            ["ProductCount"] = request.Items.Count,
            ["TotalAmount"] = request.TotalAmount
        });

        try
        {
            var order = await _repository.CreateAsync(request);
            
            _logger.InfoStructured("订单创建成功", new Dictionary<string, object>
            {
                ["OrderId"] = order.Id,
                ["CustomerId"] = order.CustomerId,
                ["Amount"] = order.TotalAmount
            });

            return order;
        }
        catch (Exception ex)
        {
            _logger.ErrorStructured("订单创建失败", new Dictionary<string, object>
            {
                ["CustomerId"] = request.CustomerId,
                ["Error"] = ex.Message
            }, ex);
            throw;
        }
    }
}

// 服务注册
services.AddLazyLogNet(config =>
{
    config.EnableFile = true;
    config.LogFolderName = "order-service-logs";
    config.LogFormat = LazyLogFormat.Json;
    config.IncludeStructuredData = true;
});
services.AddScoped<IOrderService, OrderService>();
services.AddScoped<IOrderRepository, OrderRepository>();
```

### 2. 应用程序日志分离

```csharp
// 为不同的插件或模块配置独立的日志文件夹
LazyLogHelper.Initialize(enableFile: true, logFolderName: "revit-plugin-logs");
LazyLogHelper.Initialize(enableFile: true, logFolderName: "autocad-plugin-logs");
LazyLogHelper.Initialize(enableFile: true, logFolderName: "main-app-logs");
```

### 3. 结构化日志记录

```csharp
// 记录用户操作
var userAction = new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["Action"] = "CreateProject",
    ["ProjectId"] = projectId,
    ["Duration"] = stopwatch.ElapsedMilliseconds
};
LazyLogHelper.InfoStructured("用户创建项目", userAction);
```

### 4. 异常处理

```csharp
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    LazyLogHelper.Error($"操作失败: {ex.Message}", ex);
    throw; // 重新抛出异常
}
```

### 5. 性能监控

```csharp
var stopwatch = Stopwatch.StartNew();
try
{
    // 执行操作
    DoSomething();
    
    LazyLogHelper.Info($"操作完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
}
finally
{
    stopwatch.Stop();
}
```

### 6. 配置管理

```csharp
// 开发环境
var devConfig = new LazyLoggerConfiguration
{
    MinLevel = LazyLogLevel.Debug,
    EnableConsole = true,
    EnableFile = true,
    LogFormat = LazyLogFormat.Text
};

// 生产环境
var prodConfig = new LazyLoggerConfiguration
{
    MinLevel = LazyLogLevel.Info,
    EnableConsole = false,
    EnableFile = true,
    LogFormat = LazyLogFormat.Json,
    MaxFileSize = 100 * 1024 * 1024,  // 100MB
    MaxRetainedFiles = 30
};
```

## 📈 性能特性

- **异步处理**: 基于自定义队列的高性能异步处理
- **批量写入**: 减少 I/O 操作次数
- **内存优化**: 对象池和高效字符串处理
- **队列优化**: 基于基准测试的 1K 队列容量
- **无锁设计**: 避免线程竞争
- **智能缓冲**: 可配置的文件缓冲区大小

## 🔒 线程安全

- 所有公共 API 都是线程安全的
- 支持多线程并发日志记录
- 内部使用无锁数据结构
- 自动处理资源竞争

## 🏗️ 项目结构

```
LazyLogNet/
├── LazyLogNet/              # 核心库
│   ├── ILogger.cs          # 日志接口
│   ├── LazyLogger.cs       # 日志实现
│   ├── LazyLogHelper.cs    # 静态辅助类
│   ├── LazyLoggerConfiguration.cs  # 配置类
│   ├── LazyLogLevel.cs     # 日志级别
│   ├── LazyLogFormat.cs    # 日志格式
│   └── LazyLogEntry.cs     # 日志条目
├── LazyLogNet.Example/      # 示例项目
│   ├── Program.cs          # 主程序
│   ├── ConfigurableLogFolderExample.cs
│   ├── StructuredLoggingExample.cs
│   └── ...
└── LazyLogNet.Benchmark/    # 性能测试
    ├── AdvancedBenchmark.cs
    └── Program.cs
```

## 📄 许可证

MIT License - 详见 [LICENSE](LICENSE) 文件。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📞 支持

如有问题或建议，请通过 GitHub Issues 联系我们。