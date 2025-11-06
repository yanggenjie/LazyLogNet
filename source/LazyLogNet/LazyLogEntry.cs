using System;
using System.Collections.Generic;

namespace LazyLogNet;

/// <summary>
/// 日志条目类
/// </summary>
public class LazyLogEntry
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public LazyLogLevel Level { get; set; }

    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 线程ID
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// 结构化属性
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }
}