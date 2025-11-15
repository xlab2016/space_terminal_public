using SpaceTerminal.Api.WebSockets;
using SpaceTerminal.Core.Services;
using SpaceTerminal.Infrastructure.Encryption;
using SpaceTerminal.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application services
builder.Services.AddSingleton<IEncryptionService, RsaEncryptionService>();
builder.Services.AddSingleton<IClientManager, InMemoryClientManager>();
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddSingleton<MessageHandler>();

// Configure CORS for scalability
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};

app.UseWebSockets(webSocketOptions);

// WebSocket endpoint
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionManager = context.RequestServices.GetRequiredService<WebSocketConnectionManager>();
        var messageHandler = context.RequestServices.GetRequiredService<MessageHandler>();

        await connectionManager.HandleConnectionAsync(webSocket, messageHandler);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapControllers();

app.Run();
