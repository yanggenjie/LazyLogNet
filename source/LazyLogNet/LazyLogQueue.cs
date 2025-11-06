using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LazyLogNet;

/// <summary>
/// 自定义的线程安全日志队列，替代System.Threading.Channels
/// </summary>
internal class LazyLogQueue<T> : IDisposable
{
    private readonly ConcurrentQueue<T> m_queue;
    private readonly SemaphoreSlim m_semaphore;
    private readonly CancellationTokenSource m_cancellationTokenSource;
    private readonly int m_maxCapacity;
    private volatile bool m_disposed;
    private volatile bool m_completed;

    /// <summary>
    /// 获取队列中的项目数量
    /// </summary>
    public int Count => m_queue.Count;

    /// <summary>
    /// 获取队列是否已完成
    /// </summary>
    public bool IsCompleted => m_completed;

    /// <summary>
    /// 创建日志队列实例
    /// </summary>
    /// <param name="maxCapacity">最大容量</param>
    public LazyLogQueue(int maxCapacity = 1000)
    {
        m_maxCapacity = maxCapacity;
        m_queue = new ConcurrentQueue<T>();
        m_semaphore = new SemaphoreSlim(0);
        m_cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 尝试写入项目到队列
    /// </summary>
    /// <param name="item">要写入的项目</param>
    /// <returns>是否成功写入</returns>
    public bool TryWrite(T item)
    {
        if (m_disposed || m_completed)
            return false;

        // 检查容量限制
        if (m_queue.Count >= m_maxCapacity)
            return false;

        m_queue.Enqueue(item);
        m_semaphore.Release();
        return true;
    }

    /// <summary>
    /// 尝试从队列读取项目
    /// </summary>
    /// <param name="item">读取的项目</param>
    /// <returns>是否成功读取</returns>
    public bool TryRead(out T item)
    {
        return m_queue.TryDequeue(out item);
    }

    /// <summary>
    /// 等待队列中有可读取的项目
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有项目可读取</returns>
    public async Task<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        if (m_disposed)
            return false;

        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, m_cancellationTokenSource.Token).Token;

        try
        {
            // 如果队列中已有项目，直接返回
            if (!m_queue.IsEmpty)
                return true;

            // 如果已完成且队列为空，返回false
            if (m_completed)
                return false;

            // 等待新项目
            await m_semaphore.WaitAsync(combinedToken);
            return !m_disposed && (!m_completed || !m_queue.IsEmpty);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// 标记队列为完成状态
    /// </summary>
    public void Complete()
    {
        m_completed = true;
        m_cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (m_disposed)
            return;

        m_disposed = true;
        Complete();

        m_semaphore?.Dispose();
        m_cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// 队列写入器
/// </summary>
internal class LazyLogQueueWriter<T>
{
    private readonly LazyLogQueue<T> m_queue;

    public LazyLogQueueWriter(LazyLogQueue<T> queue)
    {
        m_queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    /// <summary>
    /// 尝试写入项目
    /// </summary>
    /// <param name="item">要写入的项目</param>
    /// <returns>是否成功写入</returns>
    public bool TryWrite(T item)
    {
        return m_queue.TryWrite(item);
    }

    /// <summary>
    /// 标记写入完成
    /// </summary>
    public void Complete()
    {
        m_queue.Complete();
    }
}

/// <summary>
/// 队列读取器
/// </summary>
internal class LazyLogQueueReader<T>
{
    private readonly LazyLogQueue<T> m_queue;

    public LazyLogQueueReader(LazyLogQueue<T> queue)
    {
        m_queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    /// <summary>
    /// 尝试读取项目
    /// </summary>
    /// <param name="item">读取的项目</param>
    /// <returns>是否成功读取</returns>
    public bool TryRead(out T item)
    {
        return m_queue.TryRead(out item);
    }

    /// <summary>
    /// 等待有项目可读取
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有项目可读取</returns>
    public Task<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return m_queue.WaitToReadAsync(cancellationToken);
    }
}