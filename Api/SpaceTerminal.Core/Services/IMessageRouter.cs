using SpaceTerminal.Core.Models;

namespace SpaceTerminal.Core.Services;

public interface IMessageRouter
{
    Task RouteMessageAsync(Message message);
    Task BroadcastToGroupAsync(string groupId, Message message);
    Task SendToClientAsync(string clientId, Message message);
}
