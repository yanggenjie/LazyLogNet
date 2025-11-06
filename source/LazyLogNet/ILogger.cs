using System;
using System.Collections.Generic;

namespace LazyLogNet;

/// <summary>
/// 日志接口
/// </summary>
public interface ILazyLogger
{
    /// <summary>
    /// 获取或设置最小日志级别
    /// </summary>
    LazyLogLevel MinLevel { get; set; }

    /// <summary>
    /// 记录调试信息
    /// </summary>
    /// <param name="message">日志消息</param>
    void Debug(string message);

    /// <summary>
    /// 记录调试信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    void Debug(string message, Exception exception);

    /// <summary>
    /// 记录一般信息
    /// </summary>
    /// <param name="message">日志消息</param>
    void Info(string message);

    /// <summary>
    /// 记录一般信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    void Info(string message, Exception exception);

    /// <summary>
    /// 记录警告信息
    /// </summary>
    /// <param name="message">日志消息</param>
    void Warn(string message);

    /// <summary>
    /// 记录警告信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    void Warn(string message, Exception exception);

    /// <summary>
    /// 记录错误信息
    /// </summary>
    /// <param name="message">日志消息</param>
    void Error(string message);

    /// <summary>
    /// 记录错误信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    void Error(string message, Exception exception);

    /// <summary>
    /// 记录严重错误信息
    /// </summary>
    /// <param name="message">日志消息</param>
    void Fatal(string message);

    /// <summary>
    /// 记录严重错误信息（带异常）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    void Fatal(string message, Exception exception);

    /// <summary>
    /// 记录结构化日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    /// <param name="exception">异常信息</param>
    void LogStructured(LazyLogLevel level, string message, Dictionary<string, object>? properties = null, Exception? exception = null);

    /// <summary>
    /// 记录结构化调试日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    /// <param name="exception">异常信息</param>
    void DebugStructured(string message, Dictionary<string, object>? properties = null, Exception? exception = null);

    /// <summary>
    /// 记录结构化信息日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    void InfoStructured(string message, Dictionary<string, object>? properties = null);

    /// <summary>
    /// 记录结构化警告日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    void WarnStructured(string message, Dictionary<string, object>? properties = null);

    /// <summary>
    /// 记录结构化错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    /// <param name="exception">异常信息</param>
    void ErrorStructured(string message, Dictionary<string, object>? properties = null, Exception? exception = null);

    /// <summary>
    /// 记录结构化严重错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="properties">自定义属性</param>
    /// <param name="exception">异常信息</param>
    void FatalStructured(string message, Dictionary<string, object>? properties = null, Exception? exception = null);

    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    void Log(LazyLogLevel level, string message);

    /// <summary>
    /// 记录日志（带异常）
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常信息</param>
    void Log(LazyLogLevel level, string message, Exception exception);
}