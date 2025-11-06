using System;
using System.Collections.Generic;
using System.IO;

namespace LazyLogNet;

/// <summary>
/// 日志配置类
/// </summary>
public class LazyLoggerConfiguration
{
    /// <summary>
    /// 是否启用控制台输出
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// 是否启用文件输出
    /// </summary>
    public bool EnableFile { get; set; } = false;

    /// <summary>
    /// 日志文件夹名称
    /// 默认: "logs"
    /// 示例: "revit-logs", "autocad-logs", "custom-app-logs"
    /// </summary>
    public string LogFolderName { get; set; } = "logs";

    /// <summary>
    /// 日志文件路径
    /// 默认使用用户应用数据目录下的指定文件夹
    /// </summary>
    public string FilePath { get; set; } = null;

    /// <summary>
    /// 获取完整的日志文件路径
    /// 如果FilePath为空，则使用默认路径策略
    /// </summary>
    public string GetEffectiveFilePath()
    {
        return string.IsNullOrEmpty(FilePath) ? GetDefaultLogPath() : FilePath;
    }

    /// <summary>
    /// 获取默认的日志文件路径
    /// </summary>
    /// <returns>默认日志文件路径</returns>
    private string GetDefaultLogPath()
    {
        try
        {
            // 优先使用用户的应用数据目录
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(appDataPath))
            {
                var logDir = Path.Combine(appDataPath, "LazyLogKit", LogFolderName ?? "logs");
                EnsureDirectoryExists(logDir);
                return Path.Combine(logDir, "app.log");
            }
        }
        catch
        {
            // 如果获取应用数据目录失败，继续尝试其他方案
        }

        try
        {
            // 备选方案：使用系统临时目录
            var tempPath = Path.GetTempPath();
            if (!string.IsNullOrEmpty(tempPath))
            {
                var logDir = Path.Combine(tempPath, "LazyLogKit", LogFolderName ?? "logs");
                EnsureDirectoryExists(logDir);
                return Path.Combine(logDir, "app.log");
            }
        }
        catch
        {
            // 如果临时目录也失败，继续尝试其他方案
        }

        // 最后的备选方案：当前用户目录
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                var logDir = Path.Combine(userProfile, "LazyLogKit", LogFolderName ?? "logs");
                EnsureDirectoryExists(logDir);
                return Path.Combine(logDir, "app.log");
            }
        }
        catch
        {
            // 所有方案都失败，使用相对路径作为最后的备选
        }

        // 如果所有系统目录都无法访问，回退到相对路径
        return "app.log";
    }

    /// <summary>
    /// 最小日志级别
    /// </summary>
    public LazyLogLevel MinLevel { get; set; } = LazyLogLevel.Info;

    /// <summary>
    /// 最大文件大小（字节），0表示不限制
    /// </summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 最大保留文件数量
    /// </summary>
    public int MaxRetainedFiles { get; set; } = 5;

    /// <summary>
    /// 日志队列容量
    /// 默认: 1024 (基于性能测试优化)
    /// </summary>
    public int QueueCapacity { get; set; } = 1024;
    /// <summary>
    /// 批处理大小
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// 刷新间隔（毫秒）
    /// </summary>
    public int FlushIntervalMs { get; set; } = 1000;

    /// <summary>
    /// 文件缓冲区大小
    /// </summary>
    public int FileBufferSize { get; set; } = 4096;

    /// <summary>
    /// 是否启用日志轮转
    /// </summary>
    public bool EnableLogRotation { get; set; } = true;

    /// <summary>
    /// 日志文件名模式（用于轮转）
    /// </summary>
    public string FileNamePattern { get; set; } = "{0}_{1:yyyyMMdd_HHmmss}.log";

    /// <summary>
    /// 日志格式类型
    /// 默认: LogFormat.Text
    /// </summary>
    public LazyLogFormat LogFormat { get; set; } = LazyLogFormat.Text;

    /// <summary>
    /// 是否在日志中包含结构化数据
    /// 默认: false
    /// </summary>
    public bool IncludeStructuredData { get; set; } = false;

    /// <summary>
    /// 确保目录存在，如果不存在则创建
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    private static void EnsureDirectoryExists(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        catch
        {
            // 忽略目录创建失败的异常，让后续的文件操作处理
        }
    }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static LazyLoggerConfiguration Default => new LazyLoggerConfiguration();

    /// <summary>
    /// 创建仅控制台输出的配置
    /// </summary>
    public static LazyLoggerConfiguration ConsoleOnly => new LazyLoggerConfiguration() { EnableFile = false };

    /// <summary>
    /// 创建仅文件输出的配置
    /// </summary>
    public static LazyLoggerConfiguration FileOnly => new LazyLoggerConfiguration() { EnableConsole = false, EnableFile = true };

    /// <summary>
    /// 创建指定日志文件夹的配置
    /// </summary>
    /// <param name="folderName">日志文件夹名称</param>
    /// <returns>配置实例</returns>
    public static LazyLoggerConfiguration WithLogFolder(string folderName)
    {
        return new LazyLoggerConfiguration()
        {
            LogFolderName = folderName,
            EnableFile = true
        };
    }

    /// <summary>
    /// 创建指定完整路径的配置
    /// </summary>
    /// <param name="filePath">完整的日志文件路径</param>
    /// <returns>配置实例</returns>
    public static LazyLoggerConfiguration WithFilePath(string filePath)
    {
        return new LazyLoggerConfiguration()
        {
            FilePath = filePath,
            EnableFile = true
        };
    }

    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    /// <returns>验证结果和错误信息</returns>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // 验证队列容量
        if (QueueCapacity <= 0)
        {
            errors.Add("队列容量必须大于0");
        }
        else if (QueueCapacity > 1000000)
        {
            errors.Add("队列容量不应超过1,000,000以避免内存问题");
        }

        // 验证批处理大小
        if (BatchSize <= 0)
        {
            errors.Add("批处理大小必须大于0");
        }
        else if (BatchSize > QueueCapacity)
        {
            errors.Add("批处理大小不能超过队列容量");
        }

        // 验证刷新间隔
        if (FlushIntervalMs <= 0)
        {
            errors.Add("刷新间隔必须大于0毫秒");
        }
        else if (FlushIntervalMs > 60000)
        {
            errors.Add("刷新间隔不应超过60秒以确保及时写入");
        }

        // 验证文件相关配置
        if (EnableFile)
        {
            // 检查有效文件路径（可以是FilePath或默认路径）
            var effectiveFilePath = GetEffectiveFilePath();
            if (string.IsNullOrWhiteSpace(effectiveFilePath))
            {
                errors.Add("启用文件输出时，无法确定有效的日志文件路径");
            }

            if (MaxFileSize <= 0)
            {
                errors.Add("最大文件大小必须大于0");
            }
            else if (MaxFileSize < 1024)
            {
                errors.Add("最大文件大小不应小于1KB");
            }

            if (MaxRetainedFiles < 0)
            {
                errors.Add("保留文件数量不能为负数");
            }
            else if (MaxRetainedFiles > 1000)
            {
                errors.Add("保留文件数量不应超过1000以避免磁盘空间问题");
            }

            if (string.IsNullOrWhiteSpace(FileNamePattern))
            {
                errors.Add("文件名模式不能为空");
            }
            else if (!FileNamePattern.Contains("{0}") || !FileNamePattern.Contains("{1"))
            {
                errors.Add("文件名模式必须包含 {0} 和 {1} 占位符");
            }
        }

        // 验证输出配置
        if (!EnableConsole && !EnableFile)
        {
            errors.Add("必须至少启用控制台输出或文件输出中的一种");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 验证并抛出异常（如果配置无效）
    /// </summary>
    /// <exception cref="ArgumentException">配置无效时抛出</exception>
    public void ValidateAndThrow()
    {
        var (isValid, errors) = Validate();
        if (!isValid)
        {
            throw new ArgumentException($"配置验证失败:\n{string.Join("\n", errors)}");
        }
    }
}