using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LazyLogNet;

/// <summary>
/// 日志格式类型
/// </summary>
public enum LazyLogFormat
{
    /// <summary>
    /// 纯文本格式（默认）
    /// </summary>
    Text,

    /// <summary>
    /// JSON格式
    /// </summary>
    Json,

    /// <summary>
    /// 结构化键值对格式
    /// </summary>
    KeyValue
}

/// <summary>
/// 结构化日志数据
/// </summary>
public class StructuredLogData
{
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 日志级别
    /// </summary>
    public LazyLogLevel Level { get; set; }

    /// <summary>
    /// 线程ID
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// 自定义属性
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 转换为JSON格式
    /// </summary>
    public string ToJson()
    {
        var jsonObject = new Dictionary<string, object>
        {
            ["timestamp"] = Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            ["level"] = Level.ToString(),
            ["threadId"] = ThreadId,
            ["message"] = Message
        };

        if (Exception != null)
        {
            jsonObject["exception"] = new
            {
                type = Exception.GetType().Name,
                message = Exception.Message,
                stackTrace = Exception.StackTrace
            };
        }

        foreach (var prop in Properties)
        {
            jsonObject[prop.Key] = prop.Value;
        }

        return JsonConvert.SerializeObject(jsonObject, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
        });
    }

    /// <summary>
    /// 转换为键值对格式
    /// </summary>
    public string ToKeyValue()
    {
        var parts = new List<string>
        {
            $"timestamp={Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
            $"level={Level}",
            $"threadId={ThreadId}",
            $"message=\"{Message}\""
        };

        if (Exception != null)
        {
            parts.Add($"exception=\"{Exception.GetType().Name}: {Exception.Message}\"");
        }

        foreach (var prop in Properties)
        {
            parts.Add($"{prop.Key}={prop.Value}");
        }

        return string.Join(" ", parts);
    }
}