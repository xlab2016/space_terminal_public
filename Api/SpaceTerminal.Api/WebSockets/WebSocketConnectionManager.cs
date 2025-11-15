using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SpaceTerminal.Core.Models;

namespace SpaceTerminal.Api.WebSockets;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public async Task HandleConnectionAsync(WebSocket webSocket, MessageHandler messageHandler)
    {
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, webSocket);

        try
        {
            var buffer = new byte[1024 * 64]; // 64KB buffer for large messages

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<Message>(messageJson);

                    if (message != null)
                    {
                        await messageHandler.HandleMessageAsync(message, connectionId, this);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.InternalServerError,
                    "Connection error",
                    CancellationToken.None);
            }
        }
    }

    public async Task SendMessageAsync(string connectionId, Message message)
    {
        if (_connections.TryGetValue(connectionId, out var webSocket) &&
            webSocket.State == WebSocketState.Open)
        {
            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            await webSocket.SendAsync(
                new ArraySegment<byte>(messageBytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }

    public async Task BroadcastMessageAsync(Message message, string? excludeConnectionId = null)
    {
        var tasks = _connections
            .Where(c => c.Key != excludeConnectionId && c.Value.State == WebSocketState.Open)
            .Select(c => SendMessageAsync(c.Key, message));

        await Task.WhenAll(tasks);
    }

    public IEnumerable<string> GetAllConnectionIds() => _connections.Keys;
}
