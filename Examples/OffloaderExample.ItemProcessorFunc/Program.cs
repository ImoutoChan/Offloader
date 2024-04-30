using Offloader;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Add offload with func processor
builder.Services.AddOffload<ProcessNewAvatar>(options =>
{
    options
        // specify processor func (required)
        .UseItemProcessor(async (provider, user, ct) =>
        {
            // request any registered service (scoped should work fine)
            var http = provider.GetRequiredService<IHttpClientFactory>();

            // process item one by one
            Console.WriteLine($"Processing user {user.Name} with avatar {user.AvatarUrl}");
            await Task.Delay(2000, ct);
        })
        // specify error logger (optional)
        .UseErrorLogger((logger, user, ex) => logger.LogError(ex, "Log in any format {WithValues}", user.AvatarUrl))
        .UseDegreeOfParallelism(2);
});

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
        // ... work 
        
        await offloader.OffloadAsync(new ProcessNewAvatar(name, avatarUrl)); // offload heavy work

        return Results.Created();
    })
    .WithName("CreateUser")
    .WithOpenApi();

app.Run();

public record ProcessNewAvatar(string Name, string AvatarUrl);
