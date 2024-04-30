using Offloader;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Add offload with service processor
builder.Services.AddOffload<ProcessNewAvatar, AvatarOffloadItemProcessor>(
    options => options
        .UseErrorLogger((logger, user, ex) => logger.LogError(ex, "Log in any format {WithValues}", user.AvatarUrl))
        .UseDegreeOfParallelism(5));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/user", async (string name, string avatarUrl, IOffloader<ProcessNewAvatar> offloader) =>
    {
        // create user

        // offload heavy work
        await offloader.OffloadAsync(new ProcessNewAvatar(name, avatarUrl));

        return Results.Created();
    })
    .WithName("CreateUser")
    .WithOpenApi();

app.Run();

public record ProcessNewAvatar(string Name, string AvatarUrl);

public class AvatarOffloadItemProcessor : IOffloadItemProcessor<ProcessNewAvatar>
{
    /// <summary>
    /// Inject any required service.
    /// </summary>
    private readonly IHttpClientFactory _http;
    private static int _counter;

    public AvatarOffloadItemProcessor(IHttpClientFactory http) => _http = http;

    public async Task ProcessAsync(ProcessNewAvatar item, CancellationToken ct)
    {
        // process item one by one
        Interlocked.Increment(ref _counter);
        Console.WriteLine($"{_counter} | Processing user {item.Name} with avatar {item.AvatarUrl}");
        await Task.Delay(Random.Shared.Next(0, 4000), ct);
        Interlocked.Decrement(ref _counter);
        Console.WriteLine($"{_counter}");
    }
}
