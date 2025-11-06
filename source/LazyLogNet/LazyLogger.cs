using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LazyLogNet;

/// <summary>
/// 高性能日志实现类
/// </summary>
public class LazyLogger : ILazyLogger, IDisposable
{
    private readonly LazyLogQueue<LazyLogEntry> m_logQueue;
    private readonly LazyLogQueueWriter<LazyLogEntry> m_logWriter;
    private readonly LazyLogQueueReader<LazyLogEntry> m_logReader;
    private readonly CancellationTokenSource m_cancellationTokenSource = new();
    private readonly Task m_processingTask;
    private readonly LazyLoggerConfiguration m_configuration;
    private readonly SemaphoreSlim m_fileSemaphore = new(1, 1);
    private readonly StringBuilder m_stringBuilder = new(1024);
    private FileStream m_fileStream;
    private StreamWriter m_streamWriter;
    private long m_currentFileSize = 0;
    private int m_currentFileIndex = 0;
    private bool m_disposed;

    /// <summary>
    /// 获取或设置最小日志级别
    /// </summary>
    public LazyLogLevel MinLevel { get; set; } = LazyLogLevel.Info;

    /// <summary>
    /// 创建Logger实例
    /// </summary>
    /// <param name="configuration">日志配置</param>
    public LazyLogger(LazyLoggerConfiguration configuration)
    {
        m_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // 验证配置
        try
        {
            m_configuration.ValidateAndThrow();
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Logger配置无效: {ex.Message}", nameof(configuration), ex);
        }

        MinLevel = configuration.MinLevel;

        // 创建高性能队列用于日志处理
        m_logQueue = new LazyLogQueue<LazyLogEntry>(configuration.QueueCapacity);
        m_logWriter = new LazyLogQueueWriter<LazyLogEntry>(m_logQueue);
        m_logReader = new LazyLogQueueReader<LazyLogEntry>(m_logQueue);

        // 初始化文件流
        if (configuration.EnableFile)
        {
            var effectiveFilePath = configuration.GetEffectiveFilePath();
            if (!string.IsNullOrEmpty(effectiveFilePath))
            {
                InitializeFileStream(effectiveFilePath);
            }
        }

        // 启动后台处理任务
        m_processingTask = Task.Run(ProcessLogQueueAsync);
    }

    /// <summary>
    /// 初始化文件流
    /// </summary>
    /// <param name="filePath">文件路径</param>
    private void InitializeFileStream(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 检查是否需要轮转文件
            if (m_configuration.EnableLogRotation && File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length >= m_configuration.MaxFileSize)
                {
                    RotateLogFile(filePath);
                }
                else
                {
                    m_currentFileSize = fileInfo.Length;
                }
            }

            m_fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read,
                m_configuration.FileBufferSize, FileOptions.SequentialScan);
            m_streamWriter = new StreamWriter(m_fileStream, Encoding.UTF8, m_configuration.FileBufferSize, false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize log file stream: {ex.Message}");
        }
    }

    /// <summary>
    /// 轮转日志文件
    /// </summary>
    /// <param name="currentFilePath">当前文件路径</param>
    private void RotateLogFile(string currentFilePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(currentFilePath) ?? "";
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentFilePath);
            var extension = Path.GetExtension(currentFilePath);

            // 生成唯一的文件名，避免冲突
            string newFilePath;
            int attempt = 0;
            do
            {
                var timestamp = DateTime.Now;
                var suffix = attempt > 0 ? $"_{attempt}" : "";
                var newFileName = string.Format(m_configuration.FileNamePattern, fileNameWithoutExtension, timestamp) + suffix;
                newFilePath = Path.Combine(directory, newFileName);
                attempt++;
            }
            while (File.Exists(newFilePath) && attempt < 1000); // 最多尝试1000次

            // 移动当前文件
            if (!File.Exists(newFilePath))
            {
                File.Move(currentFilePath, newFilePath);
            }
            else
            {
                // 如果仍然冲突，使用GUID确保唯一性
                var guidFileName = $"{fileNameWithoutExtension}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";
                newFilePath = Path.Combine(directory, guidFileName);
                File.Move(currentFilePath, newFilePath);
            }

            // 清理旧文件
            CleanupOldLogFiles(directory, fileNameWithoutExtension, extension);

            m_currentFileSize = 0;
            m_currentFileIndex++;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to rotate log file: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理旧的日志文件
    /// </summary>
    /// <param name="directory">目录</param>
    /// <param name="baseFileName">基础文件名</param>
    /// <param name="extension">文件扩展名</param>
    private void CleanupOldLogFiles(string directory, string baseFileName, string extension)
    {
        try
        {
            var pattern = $"{baseFileName}_*{extension}";
            var files = Directory.GetFiles(directory, pattern)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(m_configuration.MaxRetainedFiles)
                .ToArray();

            foreach (var file in files)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to delete old log file {file.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to cleanup old log files: {ex.Message}");
        }
    }

    /// <summary>
    /// 记录调试信息
    /// </summary>
    /// <param name="message">日志消息</param>
    public void Debug(string message)
    {
        Log(LazyLogLevel.Debug, message);
    }

    /// <summary>
    /// 记录调试信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    public void Debug(string message, Exception exception)
    {
        Log(LazyLogLevel.Debug, message, exception);
    }

    /// <summary>
    /// 记录一般信息
    /// </summary>
    /// <param name="message">日志消息</param>
    public void Info(string message)
    {
        Log(LazyLogLevel.Info, message);
    }

    /// <summary>
    /// 记录一般信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    public void Info(string message, Exception exception)
    {
        Log(LazyLogLevel.Info, message, exception);
    }

    /// <summary>
    /// 记录警告信息
    /// </summary>
    /// <param name="message">日志消息</param>
    public void Warn(string message)
    {
        Log(LazyLogLevel.Warn, message);
    }

    /// <summary>
    /// 记录警告信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    public void Warn(string message, Exception exception)
    {
        Log(LazyLogLevel.Warn, message, exception);
    }

    /// <summary>
    /// 记录错误信息
    /// </summary>
    /// <param name="message">日志消息</param>
    public void Error(string message)
    {
        Log(LazyLogLevel.Error, message);
    }

    /// <summary>
    /// 记录错误信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    public void Error(string message, Exception exception)
    {
        Log(LazyLogLevel.Error, message, exception);
    }

    /// <summary>
    /// 记录严重错误信息
    /// </summary>
    /// <param name="message">日志消息</param>
    public void Fatal(string message)
    {
        Log(LazyLogLevel.Fatal, message);
    }

    /// <summary>
    /// 记录严重错误信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    public void Fatal(string message, Exception exception)
    {
        Log(LazyLogLevel.Fatal, message, exception);
    }

    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    public void Log(LazyLogLevel level, string message)
    {
        Log(level, message, null);
    }

    /// <summary>
    /// 记录结构化日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="template">消息模板</param>
    /// <param name="properties">属性字典</param>
    public void LogStructured(LazyLogLevel level, string template, Dictionary<string, object> properties)
    {
        LogStructured(level, template, properties, null);
    }

    /// <summary>
    /// 记录结构化日志（带异常）
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="template">消息模板</param>
    /// <param name="properties">属性字典</param>
    /// <param name="exception">异常信息</param>
    public void LogStructured(LazyLogLevel level, string template, Dictionary<string, object>? properties = null, Exception? exception = null)
    {
        // 检查日志级别
        if (level < MinLevel)
            return;

        var logEntry = new LazyLogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = template,
            Exception = exception,
            ThreadId = Thread.CurrentThread.ManagedThreadId,
            Properties = properties
        };

        // 使用TryWrite避免阻塞，如果队列满了则丢弃日志
        if (!m_logWriter.TryWrite(logEntry))
        {
            // 队列满时的处理策略：可以选择丢弃或者等待
            // 这里选择丢弃以避免阻塞调用线程
        }
    }

    /// <summary>
    /// 记录结构化调试日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    public void DebugStructured(string message, Dictionary<string, object>? properties = null)
    {
        LogStructured(LazyLogLevel.Debug, message, properties);
    }
    public void DebugStructured(string message, Dictionary<string, object>? properties = null, Exception? exception = null)
    {
        LogStructured(LazyLogLevel.Debug, message, properties, exception);
    }

    public void FatalStructured(string message, Dictionary<string, object>? properties = null, Exception? exception = null)
    {
        LogStructured(LazyLogLevel.Fatal, message, properties, exception);
    }
    /// <summary>
    /// 记录结构化信息日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    public void InfoStructured(string message, Dictionary<string, object>? properties = null)
    {
        LogStructured(LazyLogLevel.Info, message, properties);
    }

    /// <summary>
    /// 记录结构化警告日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    public void WarnStructured(string message, Dictionary<string, object>? properties = null)
    {
        LogStructured(LazyLogLevel.Warn, message, properties);
    }

    /// <summary>
    /// 记录结构化错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    /// <param name="exception">异常信息</param>
    public void ErrorStructured(string message, Dictionary<string, object>? properties = null, Exception? exception = null)
    {
        LogStructured(LazyLogLevel.Error, message, properties, exception);
    }

    /// <summary>
    /// 记录日志（带异常）
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    public void Log(LazyLogLevel level, string message, Exception exception)
    {
        // 检查日志级别
        if (level < MinLevel)
            return;

        var logEntry = new LazyLogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            Exception = exception,
            ThreadId = Thread.CurrentThread.ManagedThreadId
        };

        // 使用TryWrite避免阻塞，如果队列满了则丢弃日志
        if (!m_logWriter.TryWrite(logEntry))
        {
            // 队列满时的处理策略：可以选择丢弃或者等待
            // 这里选择丢弃以避免阻塞调用线程
        }
    }

    /// <summary>
    /// 后台处理日志队列（异步批量处理）
    /// </summary>
    private async Task ProcessLogQueueAsync()
    {
        var token = m_cancellationTokenSource.Token;
        var batch = new List<LazyLogEntry>(m_configuration.BatchSize);
        var lastFlushTime = DateTime.UtcNow;
        var flushInterval = TimeSpan.FromMilliseconds(m_configuration.FlushIntervalMs);

        try
        {
            while (await m_logReader.WaitToReadAsync(token))
            {
                while (m_logReader.TryRead(out var logEntry))
                {
                    batch.Add(logEntry);

                    // 优化批次检查逻辑，减少DateTime.UtcNow调用
                    var shouldFlushBySize = batch.Count >= m_configuration.BatchSize;
                    var shouldFlushByTime = false;

                    if (!shouldFlushBySize)
                    {
                        var now = DateTime.UtcNow;
                        shouldFlushByTime = (now - lastFlushTime) >= flushInterval;
                        if (shouldFlushByTime)
                        {
                            lastFlushTime = now;
                        }
                    }

                    if (shouldFlushBySize || shouldFlushByTime)
                    {
                        await ProcessLogBatch(batch);
                        batch.Clear();
                        if (shouldFlushBySize)
                        {
                            lastFlushTime = DateTime.UtcNow;
                        }
                    }
                }
            }

            // 处理剩余的日志条目
            if (batch.Count > 0)
            {
                await ProcessLogBatch(batch);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，处理剩余日志
            if (batch.Count > 0)
            {
                await ProcessLogBatch(batch);
            }
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine($"Error in log processing task: {ex.Message}");
            }
            catch
            {
                // 忽略输出错误
            }
        }
    }

    /// <summary>
    /// 批量处理日志条目
    /// </summary>
    /// <param name="batch">日志批次</param>
    private async Task ProcessLogBatch(List<LazyLogEntry> batch)
    {
        if (batch.Count == 0) return;

        try
        {
            // 控制台输出
            if (m_configuration.EnableConsole)
            {
                await WriteToConsole(batch);
            }

            // 文件输出
            if (m_configuration.EnableFile && m_streamWriter != null)
            {
                await WriteToFile(batch);
            }
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine($"Error processing log batch: {ex.Message}");
            }
            catch
            {
                // 忽略输出错误
            }
        }
    }

    /// <summary>
    /// 批量写入控制台
    /// </summary>
    /// <param name="batch">日志批次</param>
    private Task WriteToConsole(List<LazyLogEntry> batch)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            foreach (var logEntry in batch)
            {
                Console.ForegroundColor = logEntry.Level switch
                {
                    LazyLogLevel.Debug => ConsoleColor.Gray,
                    LazyLogLevel.Info => ConsoleColor.White,
                    LazyLogLevel.Warn => ConsoleColor.Yellow,
                    LazyLogLevel.Error => ConsoleColor.Red,
                    LazyLogLevel.Fatal => ConsoleColor.Magenta,
                    _ => originalColor
                };

                Console.WriteLine(FormatLogEntry(logEntry));
            }
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 批量写入文件
    /// </summary>
    /// <param name="batch">日志批次</param>
    private async Task WriteToFile(List<LazyLogEntry> batch)
    {
        if (m_streamWriter == null) return;

        await m_fileSemaphore.WaitAsync();
        try
        {
            // 使用StringBuilder批量构建内容
            m_stringBuilder.Clear();
            foreach (var logEntry in batch)
            {
                m_stringBuilder.AppendLine(FormatLogEntry(logEntry));
            }

            var content = m_stringBuilder.ToString();
            var contentBytes = Encoding.UTF8.GetByteCount(content);

            // 检查是否需要轮转文件
            if (m_configuration.EnableLogRotation &&
                m_currentFileSize + contentBytes > m_configuration.MaxFileSize)
            {
                await RotateCurrentFile();
            }

            // 异步写入文件
            await m_streamWriter.WriteAsync(content);
            await m_streamWriter.FlushAsync();

            m_currentFileSize += contentBytes;
        }
        finally
        {
            m_fileSemaphore.Release();
        }
    }

    /// <summary>
    /// 轮转当前文件
    /// </summary>
    private Task RotateCurrentFile()
    {
        try
        {
            // 关闭当前文件流
            m_streamWriter?.Dispose();
            m_fileStream?.Dispose();

            // 轮转文件
            var effectiveFilePath = m_configuration.GetEffectiveFilePath();
            RotateLogFile(effectiveFilePath);

            // 重新初始化文件流
            InitializeFileStream(effectiveFilePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to rotate current file: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 格式化日志条目
    /// </summary>
    /// <param name="logEntry">日志条目</param>
    /// <returns>格式化后的日志字符串</returns>
    private string FormatLogEntry(LazyLogEntry logEntry)
    {
        var message = logEntry.Message;

        // 如果有属性，进行模板替换和结构化处理
        if (logEntry.Properties != null && logEntry.Properties.Count > 0)
        {
            // 替换模板中的占位符
            message = ReplaceMessageTemplate(logEntry.Message, logEntry.Properties);

            // 添加结构化属性信息
            if (m_configuration.IncludeStructuredData)
            {
                var propertiesJson = JsonConvert.SerializeObject(logEntry.Properties, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                });
                message += $" | Properties: {propertiesJson}";
            }
        }

        var baseMessage = $"{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{logEntry.Level}] [T{logEntry.ThreadId:D2}] {message}";

        if (logEntry.Exception != null)
        {
            baseMessage += $"{Environment.NewLine}Exception: {logEntry.Exception}";
        }

        return baseMessage;
    }

    /// <summary>
    /// 替换消息模板中的占位符
    /// </summary>
    /// <param name="template">消息模板</param>
    /// <param name="properties">属性字典</param>
    /// <returns>替换后的消息</returns>
    private string ReplaceMessageTemplate(string template, Dictionary<string, object> properties)
    {
        if (string.IsNullOrEmpty(template) || properties == null || properties.Count == 0)
            return template;

        var result = template;
        foreach (var kvp in properties)
        {
            var placeholder = $"{{{kvp.Key}}}";
            if (result.Contains(placeholder))
            {
                var value = kvp.Value?.ToString() ?? "null";
                result = result.Replace(placeholder, value);
            }
        }

        return result;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源的具体实现
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!m_disposed && disposing)
        {
            try
            {
                // 停止接收新的日志条目
                m_logWriter.Complete();

                // 等待处理任务完成，最多等待5秒
                if (!m_processingTask.Wait(5000))
                {
                    m_cancellationTokenSource.Cancel();
                    m_processingTask.Wait(1000); // 再等待1秒强制取消
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Console.Error.WriteLine($"Error during logger disposal: {ex.Message}");
                }
                catch
                {
                    // 忽略输出错误
                }
            }
            finally
            {
                // 清理资源
                m_streamWriter?.Dispose();
                m_fileStream?.Dispose();
                m_fileSemaphore?.Dispose();
                m_logQueue?.Dispose();
                m_cancellationTokenSource?.Dispose();
                m_disposed = true;
            }
        }
    }


}