using System;
using System.Runtime.CompilerServices;

namespace LazyLogNet;

/// <summary>
/// 静态日志辅助工具类，提供全局访问的日志记录功能
/// 自动管理资源，无需手动调用 Shutdown
/// </summary>
public static class LazyLogHelper
{
    private static LazyLogger m_logger;
    private static readonly object m_lockObject = new object();
    private static volatile bool m_isInitialized = false;
    private static volatile bool m_isShuttingDown = false;
    private static LazyLoggerConfiguration m_lastConfiguration;

    /// <summary>
    /// 静态构造函数，注册应用程序退出事件
    /// </summary>
    static LazyLogHelper()
    {
        // 注册应用程序域卸载事件
        AppDomain.CurrentDomain.DomainUnload += OnAppDomainUnload;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        // 注册控制台取消事件（Ctrl+C等）
        try
        {
            Console.CancelKeyPress += OnConsoleCancelKeyPress;
        }
        catch
        {
            // 在某些环境下可能无法注册控制台事件，忽略异常
        }
    }

    /// <summary>
    /// 获取当前是否已初始化
    /// </summary>
    public static bool IsInitialized => m_isInitialized;

    /// <summary>
    /// 使用默认配置初始化日志系统
    /// </summary>
    public static void Initialize()
    {
        Initialize(LazyLoggerConfiguration.Default);
    }

    /// <summary>
    /// 使用指定配置初始化日志系统
    /// </summary>
    /// <param name="configuration">日志配置</param>
    /// <exception cref="ArgumentNullException">配置为null时抛出</exception>
    /// <exception cref="ArgumentException">配置无效时抛出</exception>
    public static void Initialize(LazyLoggerConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // 验证配置
        var (isValid, errors) = configuration.Validate();
        if (!isValid)
        {
            throw new ArgumentException($"日志配置无效:\n{string.Join("\n", errors)}", nameof(configuration));
        }

        if (m_isShuttingDown)
            return; // 如果正在关闭，忽略初始化请求

        lock (m_lockObject)
        {
            if (m_isShuttingDown)
                return;

            // 如果已经用相同配置初始化过，直接返回
            if (m_isInitialized && ConfigurationEquals(m_lastConfiguration, configuration))
                return;

            // 释放旧的 Logger
            if (m_isInitialized)
            {
                try
                {
                    m_logger?.Dispose();
                }
                catch
                {
                    // 忽略释放时的异常
                }
            }

            try
            {
                m_logger = new LazyLogger(configuration);
                m_lastConfiguration = CloneConfiguration(configuration);
                m_isInitialized = true;
            }
            catch
            {
                m_logger = null;
                m_lastConfiguration = null;
                m_isInitialized = false;
                throw;
            }
        }
    }

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    /// <param name="enableConsole">启用控制台输出</param>
    /// <param name="enableFile">启用文件输出</param>
    /// <param name="logFolderName">日志文件夹名称，如 "revit-logs", "autocad-logs" 等。如果指定，将覆盖filePath参数</param>
    /// <param name="filePath">完整的日志文件路径。如果logFolderName已指定，此参数将被忽略</param>
    /// <param name="minLevel">最小日志级别</param>
    public static void Initialize(bool enableConsole = true, bool enableFile = false,
        string logFolderName = null, string filePath = null, LazyLogLevel minLevel = LazyLogLevel.Info)
    {
        LazyLoggerConfiguration config;

        // 优先级：logFolderName > filePath > 默认配置
        if (!string.IsNullOrWhiteSpace(logFolderName))
        {
            config = LazyLoggerConfiguration.WithLogFolder(logFolderName);
        }
        else if (!string.IsNullOrWhiteSpace(filePath))
        {
            config = LazyLoggerConfiguration.WithFilePath(filePath);
        }
        else
        {
            config = new LazyLoggerConfiguration
            {
                EnableFile = enableFile
            };
        }

        config.EnableConsole = enableConsole;
        config.MinLevel = minLevel;

        Initialize(config);
    }

    /// <summary>
    /// 关闭日志系统并释放资源
    /// 注意：通常不需要手动调用此方法，系统会在应用程序退出时自动清理
    /// </summary>
    public static void Shutdown()
    {
        InternalShutdown();
    }

    /// <summary>
    /// 内部关闭方法
    /// </summary>
    private static void InternalShutdown()
    {
        if (m_isShuttingDown)
            return;

        lock (m_lockObject)
        {
            if (m_isShuttingDown)
                return;

            m_isShuttingDown = true;

            if (m_isInitialized)
            {
                try
                {
                    m_logger?.Dispose();
                }
                catch
                {
                    // 忽略关闭时的异常
                }
                finally
                {
                    m_logger = null;
                    m_lastConfiguration = null;
                    m_isInitialized = false;
                }
            }
        }
    }

    /// <summary>
    /// 确保日志系统已初始化，如果未初始化则使用默认配置
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureInitialized()
    {
        if (m_isShuttingDown)
            return;

        if (!m_isInitialized)
        {
            lock (m_lockObject)
            {
                if (!m_isInitialized && !m_isShuttingDown)
                {
                    try
                    {
                        Initialize();
                    }
                    catch
                    {
                        // 如果初始化失败，忽略异常，避免影响应用程序运行
                    }
                }
            }
        }
    }

    /// <summary>
    /// 记录调试级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(string message)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.Debug(message);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录信息级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(string message)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.Info(message);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(string message)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.Warn(message);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string message)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.Error(message);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录错误级别日志（包含异常信息）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string message, Exception exception)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.Error(message, exception);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录严重错误级别日志
    /// </summary>
    /// <param name="message">日志消息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fatal(string message)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.Fatal(message);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录严重错误级别日志（包含异常信息）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fatal(string message, Exception exception)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.Fatal(message, exception);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录指定级别的日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(LazyLogLevel level, string message)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.Log(level, message);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录指定级别的日志（包含异常信息）
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(LazyLogLevel level, string message, Exception exception)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.Log(level, message, exception);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录结构化日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    /// <param name="exception">异常信息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogStructured(LazyLogLevel level, string message, System.Collections.Generic.Dictionary<string, object> properties = null, Exception exception = null)
    {
        if (m_isShuttingDown) return;
        EnsureInitialized();
        try
        {
            m_logger?.LogStructured(level, message, properties, exception);
        }
        catch
        {
            // 忽略日志记录异常，避免影响应用程序运行
        }
    }

    /// <summary>
    /// 记录结构化调试日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DebugStructured(string message, System.Collections.Generic.Dictionary<string, object> properties = null)
    {
        LogStructured(LazyLogLevel.Debug, message, properties);
    }

    /// <summary>
    /// 记录结构化信息日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InfoStructured(string message, System.Collections.Generic.Dictionary<string, object> properties = null)
    {
        LogStructured(LazyLogLevel.Info, message, properties);
    }

    /// <summary>
    /// 记录结构化警告日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WarnStructured(string message, System.Collections.Generic.Dictionary<string, object> properties = null)
    {
        LogStructured(LazyLogLevel.Warn, message, properties);
    }

    /// <summary>
    /// 记录结构化错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    /// <param name="exception">异常信息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ErrorStructured(string message, System.Collections.Generic.Dictionary<string, object> properties = null, Exception exception = null)
    {
        LogStructured(LazyLogLevel.Error, message, properties, exception);
    }

    /// <summary>
    /// 获取当前最小日志级别
    /// </summary>
    public static LazyLogLevel MinLevel
    {
        get
        {
            if (m_isShuttingDown) return LazyLogLevel.Info;
            EnsureInitialized();
            try
            {
                return m_logger?.MinLevel ?? LazyLogLevel.Info;
            }
            catch
            {
                return LazyLogLevel.Info;
            }
        }
    }

    /// <summary>
    /// 检查指定日志级别是否启用
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <returns>如果启用返回true，否则返回false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnabled(LazyLogLevel level)
    {
        if (m_isShuttingDown) return false;
        EnsureInitialized();
        try
        {
            return level >= MinLevel;
        }
        catch
        {
            return false;
        }
    }

    #region 事件处理器

    /// <summary>
    /// 应用程序域卸载事件处理器
    /// </summary>
    private static void OnAppDomainUnload(object sender, EventArgs e)
    {
        InternalShutdown();
    }

    /// <summary>
    /// 进程退出事件处理器
    /// </summary>
    private static void OnProcessExit(object sender, EventArgs e)
    {
        InternalShutdown();
    }

    /// <summary>
    /// 控制台取消事件处理器
    /// </summary>
    private static void OnConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        InternalShutdown();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 比较两个配置是否相等
    /// </summary>
    private static bool ConfigurationEquals(LazyLoggerConfiguration config1, LazyLoggerConfiguration config2)
    {
        if (config1 == null && config2 == null) return true;
        if (config1 == null || config2 == null) return false;

        return config1.EnableConsole == config2.EnableConsole &&
               config1.EnableFile == config2.EnableFile &&
               config1.FilePath == config2.FilePath &&
               config1.MinLevel == config2.MinLevel &&
               config1.MaxFileSize == config2.MaxFileSize &&
               config1.MaxRetainedFiles == config2.MaxRetainedFiles &&
               config1.QueueCapacity == config2.QueueCapacity &&
               config1.BatchSize == config2.BatchSize &&
               config1.FlushIntervalMs == config2.FlushIntervalMs &&
               config1.FileBufferSize == config2.FileBufferSize &&
               config1.EnableLogRotation == config2.EnableLogRotation &&
               config1.FileNamePattern == config2.FileNamePattern;
    }

    /// <summary>
    /// 克隆配置对象
    /// </summary>
    private static LazyLoggerConfiguration CloneConfiguration(LazyLoggerConfiguration config)
    {
        if (config == null) return null;

        return new LazyLoggerConfiguration
        {
            EnableConsole = config.EnableConsole,
            EnableFile = config.EnableFile,
            FilePath = config.FilePath,
            MinLevel = config.MinLevel,
            MaxFileSize = config.MaxFileSize,
            MaxRetainedFiles = config.MaxRetainedFiles,
            QueueCapacity = config.QueueCapacity,
            BatchSize = config.BatchSize,
            FlushIntervalMs = config.FlushIntervalMs,
            FileBufferSize = config.FileBufferSize,
            EnableLogRotation = config.EnableLogRotation,
            FileNamePattern = config.FileNamePattern
        };
    }

    #endregion
}