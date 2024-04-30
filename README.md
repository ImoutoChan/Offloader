# Offloader

[![NuGet](https://img.shields.io/nuget/v/Offloader.svg?style=flat-square)](https://www.nuget.org/packages/Offloader/)
[![license](https://img.shields.io/github/license/ImoutoChan/Offloader.svg?style=flat-square)](https://github.com/ImoutoChan/Offloader)

A convenient way to unload some heavy work to background service from your main requests.  Install as [NuGet package](https://www.nuget.org/packages/Offloader/):

```powershell
Install-Package Offloader
```
```xml
<PackageReference Include="Offloader" Version="1.0.0" />
```
# Configurables
### Processor func or service
```csharp
// func
services.AddOffload<OffloadItemType>(x => x.UseItemProcessor(serviceProvider, item, ct) => ...);

// service, must implement IOffloadItemProcessor
services.AddOffload<OffloadItemType, YourServiceProcessorType>();
```
### Degree of Parallelism
```csharp
// default value is 1
x.UseDegreeOfParallelism(5)
```
### Custom error logging
```csharp
x.UseErrorLogger((logger, item, exception) => logger.LogError(ex, "Log in any format {WithValues}", item.Value))
```
# Samples
Full samples can be found in `Examples` directory.
## Simplest sample
```csharp
var builder = WebApplication.CreateBuilder(args);

// ...

builder.Services.AddOffload<OffloadItemType>(x => x.UseItemProcessor((_, item, _) => 
    {
        Console.WriteLine($"Processing item with value {item.State}");
        return Task.CompletedTask;
    })
});

// ...
    
var app = builder.Build();

// use offloader in your handlers or in your controllers
app.MapPost(
    "/my-heavy-endpoint", 
    (string itemValue, IOffloader<OffloadItemType> x) => x.OffloadAsync(new OffloadItemType(itemState)));

public record OffloadItemType(string State);
```
## Sample with inline processor function
```csharp
var builder = WebApplication.CreateBuilder(args);

// ...

// register offloader function for specified Type (in this sample it's OffloadItemType)
builder.Services.AddOffload<OffloadItemType>(options =>
{
    options
        // specify processor func (required)
        .UseItemProcessor(async (provider, item, ct) =>
        {
            // request any registered service (scoped should work fine)
            // e.g. var http = provider.GetRequiredService<IHttpClientFactory>();

            // process item one by one
            Console.WriteLine($"Processing item with value {item.Value}");
            await Task.Delay(2000, ct);
        })
        
        // specify error handler, e.g. logger (optional)
        .UseErrorLogger((logger, item, ex) => logger.LogError(ex, "Log in any format {WithValues}", item.Value))
        
        // specify degree of parallelism (optional, default is 1)
        .UseDegreeOfParallelism(2);
});

// ...
    
var app = builder.Build();

// use offloader in your handlers or in your controllers
app.MapPost(
    "/my-heavy-endpoint", 
    async (string itemValue, IOffloader<OffloadItemType> offloader) =>
    {
        // do some work before returning response to the client
        
        // offload heavy work to background service
        await offloader.OffloadAsync(new OffloadItemType(itemValue)); 

        return Results.Created();
    });

public record OffloadItemType(string Value);
```
## Sample with processor as a service class
```csharp
var builder = WebApplication.CreateBuilder(args);

// ...

// register offloader with processing service for certain offload type (in this sample it's ItemProcessorType and OffloadItemType)
builder.Services.AddOffload<OffloadItemType, OffloadItemProcessorType>(options =>
{
    options
        // specify error handler, e.g. logger (optional)
        .UseErrorLogger((logger, item, ex) => logger.LogError(ex, "Log in any format {WithValues}", item.Value))
        
        // specify degree of parallelism (optional, default is 1)
        .UseDegreeOfParallelism(2);
});

// ...
    
var app = builder.Build();

// use offloader in your handlers or in your controllers
app.MapPost(
    "/my-heavy-endpoint", 
    async (string name, string avatarUrl, IOffloader<OffloadItemType> offloader) =>
    {
        // do some work before returning response to the client
        
        // offload heavy work to background service~~~~
        await offloader.OffloadAsync(new OffloadItemType(itemValue)); 

        return Results.Created();
    });

// Create a processor that implements IOffloadItemProcessor<OffloadItemType>
public class OffloadItemProcessorType : IOffloadItemProcessor<OffloadItemType>
{
    // Inject any required service, for each item separate scope is created
    private readonly IHttpClientFactory _http;
    private static int _counter;

    public AvatarOffloadItemProcessor(IHttpClientFactory http) => _http = http;

    // Process item here
    public async Task ProcessAsync(ProcessNewAvatar item, CancellationToken ct)
    {
        // process items~~~~
        Interlocked.Increment(ref _counter);
        Console.WriteLine($"{_counter} | Processing user {item.Name} with avatar {item.AvatarUrl}");
        await Task.Delay(Random.Shared.Next(0, 4000), ct);
        Interlocked.Decrement(ref _counter);
        Console.WriteLine($"{_counter}");
    }
}

public record OffloadItemType(string Value);
```
