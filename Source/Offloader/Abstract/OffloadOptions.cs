using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Offloader;

public class OffloadOptions<T>
{
    protected internal Func<IServiceProvider, T, CancellationToken, Task>? ItemProcessorFunc { get; protected set; } 
        = (_, _, _) => Task.CompletedTask;

    protected internal Type? ItemProcessorServiceType { get; protected set; }
    
    internal Action<ILogger, T, Exception> ErrorLogger { get; private set; } = (_, _, _) => { };

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
}

public class OffloadFuncOptions<T> : OffloadOptions<T>
{
    public OffloadFuncOptions<T> UseItemProcessor(Func<IServiceProvider, T, CancellationToken, Task> processor)
    {
        ItemProcessorFunc = processor;
        ItemProcessorServiceType = null;

        return this;
    }
}
