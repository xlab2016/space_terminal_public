using System.Collections.Concurrent;
using SpaceTerminal.Core.Models;
using SpaceTerminal.Core.Services;

namespace SpaceTerminal.Api.WebSockets;

public class MessageHandler
{
    private readonly IClientManager _clientManager;
    private readonly IEncryptionService _encryptionService;
    private readonly ConcurrentDictionary<string, string> _connectionToClientMap = new();
    private readonly ConcurrentDictionary<string, CommandExecution> _pendingCommands = new();
    private readonly ConcurrentDictionary<string, ChatGroup> _chatGroups = new();

    public MessageHandler(IClientManager clientManager, IEncryptionService encryptionService)
    {
        _clientManager = clientManager;
        _encryptionService = encryptionService;
    }

    public async Task HandleMessageAsync(Message message, string connectionId, WebSocketConnectionManager connectionManager)
    {
        switch (message.Type)
        {
            case MessageType.Authentication:
                await HandleAuthenticationAsync(message, connectionId, connectionManager);
                break;

            case MessageType.Command:
                await HandleCommandAsync(message, connectionId, connectionManager);
                break;

            case MessageType.CommandConfirmation:
                await HandleCommandConfirmationAsync(message, connectionId, connectionManager);
                break;

            case MessageType.DesktopStreamStart:
            case MessageType.DesktopStreamStop:
            case MessageType.DesktopFrame:
            case MessageType.AudioFrame:
                await HandleDesktopStreamAsync(message, connectionId, connectionManager);
                break;

            case MessageType.ChatMessage:
                await HandleChatMessageAsync(message, connectionId, connectionManager);
                break;

            case MessageType.ChatGroupCreate:
                await HandleChatGroupCreateAsync(message, connectionId, connectionManager);
                break;

            case MessageType.ChatGroupJoin:
                await HandleChatGroupJoinAsync(message, connectionId, connectionManager);
                break;

            case MessageType.Heartbeat:
                await HandleHeartbeatAsync(message, connectionId, connectionManager);
                break;

            default:
                await SendErrorAsync(connectionId, connectionManager, "Unknown message type");
                break;
        }
    }

    private async Task HandleAuthenticationAsync(Message message, string connectionId, WebSocketConnectionManager connectionManager)
    {
        try
        {
            var client = System.Text.Json.JsonSerializer.Deserialize<Client>(message.Payload);
            if (client != null)
            {
                await _clientManager.RegisterClientAsync(client);
                await _clientManager.UpdateClientStatusAsync(client.Id, true);
                await _clientManager.UpdateClientSessionAsync(client.Id, connectionId);

                _connectionToClientMap.TryAdd(connectionId, client.Id);

                var response = new Message
                {
                    Type = MessageType.AuthenticationResponse,
                    SenderId = "server",
                    ReceiverId = client.Id,
                    Payload = System.Text.Json.JsonSerializer.Serialize(new { success = true, clientId = client.Id })
                };

                await connectionManager.SendMessageAsync(connectionId, response);
            }
        }
        catch (Exception ex)
        {
            await SendErrorAsync(connectionId, connectionManager, $"Authentication failed: {ex.Message}");
        }
    }

    private async Task HandleCommandAsync(Message message, string connectionId, WebSocketConnectionManager connectionManager)
    {
        try
        {
            var commandExecution = System.Text.Json.JsonSerializer.Deserialize<CommandExecution>(message.Payload);
            if (commandExecution != null)
            {
                _pendingCommands.TryAdd(commandExecution.Id, commandExecution);

                // Send confirmation request to target client
                var confirmationRequest = new Message
                {
                    Type = MessageType.CommandConfirmationRequest,
                    SenderId = message.SenderId,
                    ReceiverId = commandExecution.ClientId,
                    Payload = System.Text.Json.JsonSerializer.Serialize(commandExecution)
                };

                var targetClient = await _clientManager.GetClientAsync(commandExecution.ClientId);
                if (targetClient?.SessionId != null)
                {
                    await connectionManager.SendMessageAsync(targetClient.SessionId, confirmationRequest);
                }
            }
        }
        catch (Exception ex)
        {
            await SendErrorAsync(connectionId, connectionManager, $"Command handling failed: {ex.Message}");
        }
    }

    private async Task HandleCommandConfirmationAsync(Message message, string connectionId, WebSocketConnectionManager connectionManager)
    {
        try
        {
            var confirmation = System.Text.Json.JsonSerializer.Deserialize<dynamic>(message.Payload);
            var commandId = confirmation?.commandId?.ToString();
            var approved = confirmation?.approved?.GetBoolean() ?? false;

            if (commandId != null && _pendingCommands.TryGetValue(commandId, out var command))
            {
                command.Status = approved ? CommandStatus.Confirmed : CommandStatus.Rejected;
                command.ConfirmedAt = DateTime.UtcNow;

                // Notify requester
                var requester = await _clientManager.GetClientAsync(command.RequesterId);
                if (requester?.SessionId != null)
                {
                    var response = new Message
                    {
                        Type = MessageType.CommandResponse,
                        SenderId = "server",
                        ReceiverId = command.RequesterId,
                        Payload = System.Text.Json.JsonSerializer.Serialize(command)
                    };

                    await connectionManager.SendMessageAsync(requester.SessionId, response);
                }
            }
        }
        catch (Exception ex)
        {
            await SendErrorAsync(connectionId, connectionManager, $"Command confirmation failed: {ex.Message}");
        }
    }

    private async Task HandleDesktopStreamAsync(Message message, string connectionId, WebSocketConnectionManager connectionManager)
    {
        // Forward desktop stream messages to the intended recipient
        if (message.ReceiverId != null)
        {
            var targetClient = await _clientManager.GetClientAsync(message.ReceiverId);
            if (targetClient?.SessionId != null)
            {
                await connectionManager.SendMessageAsync(targetClient.SessionId, message);
            }
        }
    }

    private async Task HandleChatMessageAsync(Message message, string connectionId, WebSocketConnectionManager connectionManager)
    {
        try
        {
            var chatMessage = System.Text.Json.JsonSerializer.Deserialize<ChatMessage>(message.Payload);
            if (chatMessage != null)
            {
                if (chatMessage.GroupId != null)
                {
                    // Send to all group members
                    if (_chatGroups.TryGetValue(chatMessage.GroupId, out var group))
                    {
                        foreach (var memberId in group.MemberIds)
                        {
                            var member = await _clientManager.GetClientAsync(memberId);
                            if (member?.SessionId != null)
                            {
                                await connectionManager.SendMessageAsync(member.SessionId, message);
                            }
                        }
                    }
                }
                else if (chatMessage.ReceiverId != null)
                {
                    // Direct message
                    var recipient = await _clientManager.GetClientAsync(chatMessage.ReceiverId);
                    if (recipient?.SessionId != null)
                    {
                        await connectionManager.SendMessageAsync(recipient.SessionId, message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await SendErrorAsync(connectionId, connectionManager, $"Chat message failed: {ex.Message}");
        }
    }

    private async Task HandleChatGroupCreateAsync(Message message, string connectionId, WebSocketConnectionManager connectionManager)
    {
        try
        {
            var group = System.Text.Json.JsonSerializer.Deserialize<ChatGroup>(message.Payload);
            if (group != null)
            {
                _chatGroups.TryAdd(group.Id, group);

                var response = new Message
                {
                    Type = MessageType.ChatGroupCreate,
                    SenderId = "server",
                    ReceiverId = message.SenderId,
                    Payload = System.Text.Json.JsonSerializer.Serialize(new { success = true, groupId = group.Id })
                };

                await connectionManager.SendMessageAsync(connectionId, response);
            }
        }
        catch (Exception ex)
        {
            await SendErrorAsync(connectionId, connectionManager, $"Group creation failed: {ex.Message}");
        }
    }

    private async Task HandleChatGroupJoinAsync(Message message, string connectionId, WebSocketConnectionManager connectionManager)
    {
        try
        {
            var joinRequest = System.Text.Json.JsonSerializer.Deserialize<dynamic>(message.Payload);
            var groupId = joinRequest?.groupId?.ToString();

            if (groupId != null && _chatGroups.TryGetValue(groupId, out var group))
            {
                if (!group.MemberIds.Contains(message.SenderId))
                {
                    group.MemberIds.Add(message.SenderId);
                }

                var response = new Message
                {
                    Type = MessageType.ChatGroupJoin,
                    SenderId = "server",
                    ReceiverId = message.SenderId,
                    Payload = System.Text.Json.JsonSerializer.Serialize(new { success = true, groupId })
                };

                await connectionManager.SendMessageAsync(connectionId, response);
            }
        }
        catch (Exception ex)
        {
            await SendErrorAsync(connectionId, connectionManager, $"Group join failed: {ex.Message}");
        }
    }

    private async Task HandleHeartbeatAsync(Message message, string connectionId, WebSocketConnectionManager connectionManager)
    {
        if (_connectionToClientMap.TryGetValue(connectionId, out var clientId))
        {
            await _clientManager.UpdateClientStatusAsync(clientId, true);
        }
    }

    private async Task SendErrorAsync(string connectionId, WebSocketConnectionManager connectionManager, string errorMessage)
    {
        var errorResponse = new Message
        {
            Type = MessageType.Error,
            SenderId = "server",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { error = errorMessage })
        };

        await connectionManager.SendMessageAsync(connectionId, errorResponse);
    }
}
