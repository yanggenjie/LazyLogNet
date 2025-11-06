using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LazyLogNet;

/// <summary>
/// 依赖注入扩展方法
/// </summary>
public static class LazyLogNetServiceCollectionExtensions
{
    /// <summary>
    /// 添加LazyLogNet日志服务（使用默认配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLazyLogNet(this IServiceCollection services)
    {
        return AddLazyLogNet(services, LazyLoggerConfiguration.Default);
    }

    /// <summary>
    /// 添加LazyLogNet日志服务（使用指定配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">日志配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLazyLogNet(this IServiceCollection services, LazyLoggerConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // 注册配置为单例
        services.TryAddSingleton(configuration);

        // 注册日志器为单例
        services.TryAddSingleton<ILazyLogger>(provider =>
        {
            var config = provider.GetRequiredService<LazyLoggerConfiguration>();
            return new LazyLogger(config);
        });

        return services;
    }

    /// <summary>
    /// 添加LazyLogNet日志服务（使用配置委托）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLazyLogNet(this IServiceCollection services, Action<LazyLoggerConfiguration> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        var configuration = new LazyLoggerConfiguration();
        configureOptions(configuration);
        
        // 验证配置
        configuration.ValidateAndThrow();

        return AddLazyLogNet(services, configuration);
    }

    /// <summary>
    /// 添加LazyLogNet日志服务（仅控制台输出）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLazyLogNetConsole(this IServiceCollection services)
    {
        return AddLazyLogNet(services, LazyLoggerConfiguration.ConsoleOnly);
    }

    /// <summary>
    /// 添加LazyLogNet日志服务（仅文件输出）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="logFolderName">日志文件夹名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLazyLogNetFile(this IServiceCollection services, string logFolderName = "logs")
    {
        return AddLazyLogNet(services, LazyLoggerConfiguration.WithLogFolder(logFolderName));
    }

    /// <summary>
    /// 添加LazyLogNet日志服务（指定文件路径）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="filePath">日志文件路径</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLazyLogNetFileWithPath(this IServiceCollection services, string filePath)
    {
        return AddLazyLogNet(services, LazyLoggerConfiguration.WithFilePath(filePath));
    }
}