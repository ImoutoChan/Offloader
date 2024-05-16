using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Offloader.Implementation;

internal class OffloadHostedService<T> : IHostedService
{
    private readonly IOffloadReader<T> _offload;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<OffloadOptions<T>> _options;
    private readonly ILogger<OffloadHostedService<T>> _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    public OffloadHostedService(
        IOffloadReader<T> offload,
        IServiceProvider serviceProvider,
        IOptions<OffloadOptions<T>> options,
        ILogger<OffloadHostedService<T>> logger)
    {
        _offload = offload;
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ProcessAsync(_cancellationTokenSource.Token), CancellationToken.None);

        return Task.CompletedTask;
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        var reader = _offload.Reader;
        var degreeOfParallelism = _options.Value.DegreeOfParallelism;
        var tasksBuffer = new List<Task>(degreeOfParallelism);
        
        while (await reader.WaitToReadAsync(ct))
        while (reader.TryRead(out var item))
        {
            var processingTask = ProcessSingleItemAsync(item, ct);
            tasksBuffer.Add(processingTask);

            if (tasksBuffer.Count < degreeOfParallelism) 
                continue;

            await Task.WhenAny(tasksBuffer);
            tasksBuffer.RemoveAll(t => t.IsCompleted);
        }
        
        await Task.WhenAll(tasksBuffer);
    }

    private async Task ProcessSingleItemAsync(T item, CancellationToken ct)
    {
        var options = _options.Value;
        try
        {
            using var scope = _serviceProvider.CreateScope();

            if (options.IsItemProcessorFuncSet)
            {
                await options.ItemProcessorFunc(scope.ServiceProvider, item, ct);
            }
            else
            {
                var service = scope.ServiceProvider.GetRequiredService<IOffloadItemProcessor<T>>();
                await service.ProcessAsync(item, ct);
            }
        }
        catch (Exception e)
        {
            options.ErrorLogger(_logger, item, e);
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _offload.Complete();
        _cancellationTokenSource?.Cancel();

        return Task.CompletedTask;
    }
}
