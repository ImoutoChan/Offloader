using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Offloader;

public class OffloadOptions<T>
{
    protected internal Func<IServiceProvider, T, CancellationToken, Task>? ItemProcessorFunc { get; protected set; } 
        = (_, _, _) => Task.CompletedTask;

    protected internal Type? ItemProcessorServiceType { get; protected set; }

    internal Action<ILogger, T, Exception> ErrorLogger { get; private set; }
        = (logger, _, exception) =>
            logger.LogError(exception, "Unable to process an offloaded item of type {ItemType}", typeof(T));

    [MemberNotNullWhen(true, nameof(ItemProcessorFunc))]
    [MemberNotNullWhen(false, nameof(ItemProcessorServiceType))]
    internal bool IsItemProcessorFuncSet => ItemProcessorFunc != null;
    
    internal int DegreeOfParallelism { get; private set; } = 1;
    
    public OffloadOptions<T> UseDegreeOfParallelism(int degreeOfParallelism = 1)
    {
        DegreeOfParallelism = degreeOfParallelism;

        return this;
    }
    
    public OffloadOptions<T> UseErrorLogger(Action<ILogger, T, Exception> logger)
    {
        ErrorLogger = logger;

        return this;
    }
    
    internal OffloadOptions<T> UseItemProcessor<TService>() 
        where TService : IOffloadItemProcessor<T>
    {
        ItemProcessorFunc = null;
        ItemProcessorServiceType = typeof(TService);

        return this;
    }

    public OffloadOptions<T> UseItemProcessor(Func<IServiceProvider, T, CancellationToken, Task> processor)
    {
        if (ItemProcessorServiceType != null)
            throw new InvalidOperationException(
                "Item processor service type is already set. Use AddOffload<TItem> in order to set custom processing function.");
        
        ItemProcessorFunc = processor;

        return this;
    }
}
