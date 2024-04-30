using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Offloader.Tests;

public class OffloaderTests
{
    [Fact]
    public async Task FuncOffloaderShouldProcessItem()
    {
        // assert
        var services = PrepareServices();

        var processed = false;
        services.AddOffload<TestOffloadPayload>(options => options.UseItemProcessor((provider, payload, ct) =>
        {
            processed = true;
            return Task.CompletedTask;
        }));

        var offloader = await PrepareOffloader<TestOffloadPayload>(services);

        // act
        await offloader.OffloadAsync(new TestOffloadPayload(1));
        await Task.Delay(100);

        // assert
        Assert.True(processed);
    }

    [Fact]
    public async Task ServiceOffloaderShouldProcessItem()
    {
        // assert
        var services = PrepareServices();
        services.AddOffload<TestOffloadPayload, TestOffloadPayloadProcessor>();

        var offloader = await PrepareOffloader<TestOffloadPayload>(services);

        // act
        await offloader.OffloadAsync(new TestOffloadPayload(123));
        await Task.Delay(100);

        // assert
        Assert.Equal(123, TestOffloadPayloadProcessor.ProcessedValue);
    }

    [Fact]
    public async Task OffloaderShouldNotProcessMoreItemsThan1ByDefault()
    {
        // assert
        var services = PrepareServices();
        services.AddOffload<TestParallelOffloadPayload, TestParallelOffloadPayloadProcessor>();

        var offloader = await PrepareOffloader<TestParallelOffloadPayload>(services);

        // act
        for (var i = 0; i < 20; i++)
            await offloader.OffloadAsync(new TestParallelOffloadPayload(1));

        await Task.Delay(3000);
    }

    [Fact]
    public async Task OffloaderShouldNotProcessMoreItemsThanDegreeOfParallelism()
    {
        // assert
        const int maxDegreeOfParallelism = 5;

        var services = PrepareServices();
        services.AddOffload<TestParallelOffloadPayload, TestParallelOffloadPayloadProcessor>(
            x => x.UseDegreeOfParallelism(maxDegreeOfParallelism));

        var offloader = await PrepareOffloader<TestParallelOffloadPayload>(services);

        // act
        for (var i = 0; i < 20; i++)
            await offloader.OffloadAsync(new TestParallelOffloadPayload(maxDegreeOfParallelism));

        await Task.Delay(3000);
    }

    [Fact]
    public async Task OffloaderShouldProcessDegreeOfParallelismItemsAtSomePoint()
    {
        // assert
        const int maxDegreeOfParallelism = 5;

        var services = PrepareServices();
        services.AddOffload<TestParallelOffloadPayload, TestParallelOffloadPayloadProcessor>(
            x => x.UseDegreeOfParallelism(maxDegreeOfParallelism));

        var offloader = await PrepareOffloader<TestParallelOffloadPayload>(services);

        // act
        for (var i = 0; i < 20; i++)
            await offloader.OffloadAsync(new TestParallelOffloadPayload(maxDegreeOfParallelism));

        await Task.Delay(3000);

        // assert
        Assert.Equal(maxDegreeOfParallelism, TestParallelOffloadPayloadProcessor.MaxCounter);
    }

    private IServiceCollection PrepareServices()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        return services;
    }

    private async Task<IOffloader<T>> PrepareOffloader<T>(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();

        var hostedService = provider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(CancellationToken.None);

        return provider.GetRequiredService<IOffloader<T>>();
    }
}

public record TestOffloadPayload(int CounterValue);

public record TestParallelOffloadPayload(int MaxDegreeOfParallelism);

public class TestOffloadPayloadProcessor : IOffloadItemProcessor<TestOffloadPayload>
{
    public static int ProcessedValue = -1;

    public Task ProcessAsync(TestOffloadPayload item, CancellationToken ct)
    {
        ProcessedValue = item.CounterValue;
        return Task.CompletedTask;
    }
}

public class TestParallelOffloadPayloadProcessor : IOffloadItemProcessor<TestParallelOffloadPayload>
{
    public static int Counter;
    public static int MaxCounter;

    public async Task ProcessAsync(TestParallelOffloadPayload item, CancellationToken ct)
    {
        var newCounter = Interlocked.Increment(ref Counter);

        if (newCounter > MaxCounter)
        {
            var currentMax = MaxCounter;
            while (Interlocked.CompareExchange(ref MaxCounter, newCounter, currentMax) != currentMax)
                currentMax = MaxCounter;
        }

        if (Counter > item.MaxDegreeOfParallelism)
            Assert.Fail("More items processed than expected.");

        await Task.Delay(100, ct);
        Interlocked.Decrement(ref Counter);
    }
}
