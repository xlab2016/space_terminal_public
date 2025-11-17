using Moq;
using SpaceTerminal.Api.WebSockets;
using SpaceTerminal.Core.Models;
using SpaceTerminal.Core.Services;
using System.Text.Json;
using Xunit;

namespace SpaceTerminal.Tests;

public class MessageHandlerTests
{
    private readonly Mock<IClientManager> _mockClientManager;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<WebSocketConnectionManager> _mockConnectionManager;
    private readonly MessageHandler _messageHandler;

    public MessageHandlerTests()
    {
        _mockClientManager = new Mock<IClientManager>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockConnectionManager = new Mock<WebSocketConnectionManager>();
        _messageHandler = new MessageHandler(_mockClientManager.Object, _mockEncryptionService.Object);
    }

    [Fact]
    public async Task HandleAuthenticationAsync_ShouldRegisterClient()
    {
        // Arrange
        var client = new Client
        {
            Id = "client-1",
            Name = "Test Client",
            Type = ClientType.Windows
        };

        var message = new Message
        {
            Type = MessageType.Authentication,
            Payload = JsonSerializer.Serialize(client)
        };

        var connectionId = "connection-1";

        _mockClientManager.Setup(m => m.RegisterClientAsync(It.IsAny<Client>()))
            .Returns(Task.CompletedTask);
        _mockClientManager.Setup(m => m.UpdateClientStatusAsync(client.Id, true))
            .Returns(Task.CompletedTask);
        _mockClientManager.Setup(m => m.UpdateClientSessionAsync(client.Id, connectionId))
            .Returns(Task.CompletedTask);

        // Act
        await _messageHandler.HandleMessageAsync(message, connectionId, _mockConnectionManager.Object);

        // Assert
        _mockClientManager.Verify(m => m.RegisterClientAsync(It.IsAny<Client>()), Times.Once);
        _mockClientManager.Verify(m => m.UpdateClientStatusAsync(client.Id, true), Times.Once);
        _mockClientManager.Verify(m => m.UpdateClientSessionAsync(client.Id, connectionId), Times.Once);
        _mockConnectionManager.Verify(m => m.SendMessageAsync(connectionId, It.Is<Message>(msg =>
            msg.Type == MessageType.AuthenticationResponse &&
            msg.SenderId == "server")), Times.Once);
    }

    [Fact]
    public async Task HandleCommandAsync_ShouldStorePendingCommand()
    {
        // Arrange
        var commandExecution = new CommandExecution
        {
            Id = "cmd-1",
            Command = "ls",
            ClientId = "client-1",
            RequesterId = "requester-1"
        };

        var message = new Message
        {
            Type = MessageType.Command,
            SenderId = "requester-1",
            Payload = JsonSerializer.Serialize(commandExecution)
        };

        var connectionId = "connection-1";
        var targetClient = new Client
        {
            Id = "client-1",
            SessionId = "session-1"
        };

        _mockClientManager.Setup(m => m.GetClientAsync("client-1"))
            .ReturnsAsync(targetClient);

        // Act
        await _messageHandler.HandleMessageAsync(message, connectionId, _mockConnectionManager.Object);

        // Assert
        _mockConnectionManager.Verify(m => m.SendMessageAsync(
            "session-1",
            It.Is<Message>(msg => msg.Type == MessageType.CommandConfirmationRequest)),
            Times.Once);
    }

    [Fact]
    public async Task HandleCommandConfirmationAsync_WithApproval_ShouldUpdateCommandStatus()
    {
        // Arrange
        var commandExecution = new CommandExecution
        {
            Id = "cmd-1",
            Command = "ls",
            ClientId = "client-1",
            RequesterId = "requester-1"
        };

        // First, send a command to create a pending command
        var commandMessage = new Message
        {
            Type = MessageType.Command,
            SenderId = "requester-1",
            Payload = JsonSerializer.Serialize(commandExecution)
        };

        var targetClient = new Client { Id = "client-1", SessionId = "session-1" };
        var requesterClient = new Client { Id = "requester-1", SessionId = "session-requester" };

        _mockClientManager.Setup(m => m.GetClientAsync("client-1"))
            .ReturnsAsync(targetClient);
        _mockClientManager.Setup(m => m.GetClientAsync("requester-1"))
            .ReturnsAsync(requesterClient);

        await _messageHandler.HandleMessageAsync(commandMessage, "connection-1", _mockConnectionManager.Object);

        _mockConnectionManager.Invocations.Clear(); // Clear previous invocations

        // Now send confirmation
        var confirmation = new { commandId = "cmd-1", approved = true };
        var confirmationMessage = new Message
        {
            Type = MessageType.CommandConfirmation,
            Payload = JsonSerializer.Serialize(confirmation)
        };

        // Act
        await _messageHandler.HandleMessageAsync(confirmationMessage, "connection-1", _mockConnectionManager.Object);

        // Assert
        _mockConnectionManager.Verify(m => m.SendMessageAsync(
            "session-requester",
            It.IsAny<Message>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCommandConfirmationAsync_WithRejection_ShouldUpdateCommandStatus()
    {
        // Arrange
        var commandExecution = new CommandExecution
        {
            Id = "cmd-2",
            Command = "rm -rf",
            ClientId = "client-1",
            RequesterId = "requester-1"
        };

        var commandMessage = new Message
        {
            Type = MessageType.Command,
            SenderId = "requester-1",
            Payload = JsonSerializer.Serialize(commandExecution)
        };

        var targetClient = new Client { Id = "client-1", SessionId = "session-1" };
        var requesterClient = new Client { Id = "requester-1", SessionId = "session-requester" };

        _mockClientManager.Setup(m => m.GetClientAsync("client-1"))
            .ReturnsAsync(targetClient);
        _mockClientManager.Setup(m => m.GetClientAsync("requester-1"))
            .ReturnsAsync(requesterClient);

        await _messageHandler.HandleMessageAsync(commandMessage, "connection-1", _mockConnectionManager.Object);

        _mockConnectionManager.Invocations.Clear(); // Clear previous invocations

        var confirmation = new { commandId = "cmd-2", approved = false };
        var confirmationMessage = new Message
        {
            Type = MessageType.CommandConfirmation,
            Payload = JsonSerializer.Serialize(confirmation)
        };

        // Act
        await _messageHandler.HandleMessageAsync(confirmationMessage, "connection-1", _mockConnectionManager.Object);

        // Assert
        _mockConnectionManager.Verify(m => m.SendMessageAsync(
            "session-requester",
            It.IsAny<Message>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleDesktopStreamAsync_ShouldForwardToRecipient()
    {
        // Arrange
        var targetClient = new Client { Id = "client-1", SessionId = "session-1" };
        var message = new Message
        {
            Type = MessageType.DesktopFrame,
            SenderId = "sender-1",
            ReceiverId = "client-1",
            Payload = "frame-data"
        };

        _mockClientManager.Setup(m => m.GetClientAsync("client-1"))
            .ReturnsAsync(targetClient);

        // Act
        await _messageHandler.HandleMessageAsync(message, "connection-sender", _mockConnectionManager.Object);

        // Assert
        _mockConnectionManager.Verify(m => m.SendMessageAsync("session-1", message), Times.Once);
    }

    [Fact]
    public async Task HandleChatGroupCreateAsync_ShouldCreateGroup()
    {
        // Arrange
        var chatGroup = new ChatGroup
        {
            Id = "group-1",
            Name = "Test Group",
            CreatorId = "creator-1"
        };

        var message = new Message
        {
            Type = MessageType.ChatGroupCreate,
            SenderId = "creator-1",
            Payload = JsonSerializer.Serialize(chatGroup)
        };

        // Act
        await _messageHandler.HandleMessageAsync(message, "connection-1", _mockConnectionManager.Object);

        // Assert
        _mockConnectionManager.Verify(m => m.SendMessageAsync(
            "connection-1",
            It.Is<Message>(msg =>
                msg.Type == MessageType.ChatGroupCreate &&
                msg.SenderId == "server")),
            Times.Once);
    }

    [Fact]
    public async Task HandleChatGroupJoinAsync_ShouldAddMemberToGroup()
    {
        // Arrange
        var chatGroup = new ChatGroup
        {
            Id = "group-1",
            Name = "Test Group",
            CreatorId = "creator-1",
            MemberIds = new List<string> { "creator-1" }
        };

        var createMessage = new Message
        {
            Type = MessageType.ChatGroupCreate,
            SenderId = "creator-1",
            Payload = JsonSerializer.Serialize(chatGroup)
        };

        await _messageHandler.HandleMessageAsync(createMessage, "connection-1", _mockConnectionManager.Object);

        _mockConnectionManager.Invocations.Clear(); // Clear previous invocations

        var joinRequest = new { groupId = "group-1" };
        var joinMessage = new Message
        {
            Type = MessageType.ChatGroupJoin,
            SenderId = "member-1",
            Payload = JsonSerializer.Serialize(joinRequest)
        };

        // Act
        await _messageHandler.HandleMessageAsync(joinMessage, "connection-2", _mockConnectionManager.Object);

        // Assert
        _mockConnectionManager.Verify(m => m.SendMessageAsync(
            "connection-2",
            It.IsAny<Message>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleChatMessageAsync_DirectMessage_ShouldSendToRecipient()
    {
        // Arrange
        var chatMessage = new ChatMessage
        {
            Id = "msg-1",
            SenderId = "sender-1",
            ReceiverId = "receiver-1",
            Content = "Hello"
        };

        var message = new Message
        {
            Type = MessageType.ChatMessage,
            SenderId = "sender-1",
            ReceiverId = "receiver-1",
            Payload = JsonSerializer.Serialize(chatMessage)
        };

        var recipientClient = new Client { Id = "receiver-1", SessionId = "session-receiver" };

        _mockClientManager.Setup(m => m.GetClientAsync("receiver-1"))
            .ReturnsAsync(recipientClient);

        // Act
        await _messageHandler.HandleMessageAsync(message, "connection-sender", _mockConnectionManager.Object);

        // Assert
        _mockConnectionManager.Verify(m => m.SendMessageAsync("session-receiver", message), Times.Once);
    }

    [Fact]
    public async Task HandleChatMessageAsync_GroupMessage_ShouldSendToAllMembers()
    {
        // Arrange
        var chatGroup = new ChatGroup
        {
            Id = "group-1",
            Name = "Test Group",
            MemberIds = new List<string> { "member-1", "member-2" }
        };

        var createMessage = new Message
        {
            Type = MessageType.ChatGroupCreate,
            SenderId = "creator-1",
            Payload = JsonSerializer.Serialize(chatGroup)
        };

        await _messageHandler.HandleMessageAsync(createMessage, "connection-1", _mockConnectionManager.Object);

        var chatMessage = new ChatMessage
        {
            Id = "msg-1",
            SenderId = "member-1",
            GroupId = "group-1",
            Content = "Hello group"
        };

        var message = new Message
        {
            Type = MessageType.ChatMessage,
            SenderId = "member-1",
            GroupId = "group-1",
            Payload = JsonSerializer.Serialize(chatMessage)
        };

        var member1Client = new Client { Id = "member-1", SessionId = "session-1" };
        var member2Client = new Client { Id = "member-2", SessionId = "session-2" };

        _mockClientManager.Setup(m => m.GetClientAsync("member-1"))
            .ReturnsAsync(member1Client);
        _mockClientManager.Setup(m => m.GetClientAsync("member-2"))
            .ReturnsAsync(member2Client);

        // Act
        await _messageHandler.HandleMessageAsync(message, "connection-sender", _mockConnectionManager.Object);

        // Assert
        _mockConnectionManager.Verify(m => m.SendMessageAsync("session-1", message), Times.Once);
        _mockConnectionManager.Verify(m => m.SendMessageAsync("session-2", message), Times.Once);
    }

    [Fact]
    public async Task HandleHeartbeatAsync_ShouldUpdateClientStatus()
    {
        // Arrange
        var client = new Client
        {
            Id = "client-1",
            Name = "Test Client"
        };

        var authMessage = new Message
        {
            Type = MessageType.Authentication,
            Payload = JsonSerializer.Serialize(client)
        };

        var connectionId = "connection-1";

        _mockClientManager.Setup(m => m.RegisterClientAsync(It.IsAny<Client>()))
            .Returns(Task.CompletedTask);
        _mockClientManager.Setup(m => m.UpdateClientStatusAsync(client.Id, true))
            .Returns(Task.CompletedTask);
        _mockClientManager.Setup(m => m.UpdateClientSessionAsync(client.Id, connectionId))
            .Returns(Task.CompletedTask);

        await _messageHandler.HandleMessageAsync(authMessage, connectionId, _mockConnectionManager.Object);

        var heartbeatMessage = new Message
        {
            Type = MessageType.Heartbeat,
            SenderId = client.Id
        };

        // Act
        await _messageHandler.HandleMessageAsync(heartbeatMessage, connectionId, _mockConnectionManager.Object);

        // Assert
        _mockClientManager.Verify(m => m.UpdateClientStatusAsync(client.Id, true), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleMessageAsync_UnknownMessageType_ShouldSendError()
    {
        // Arrange
        var message = new Message
        {
            Type = (MessageType)999,
            SenderId = "sender-1"
        };

        // Act
        await _messageHandler.HandleMessageAsync(message, "connection-1", _mockConnectionManager.Object);

        // Assert
        _mockConnectionManager.Verify(m => m.SendMessageAsync(
            "connection-1",
            It.Is<Message>(msg => msg.Type == MessageType.Error)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAuthenticationAsync_WithNullClient_ShouldHandleGracefully()
    {
        // Arrange
        var message = new Message
        {
            Type = MessageType.Authentication,
            Payload = "invalid-json"
        };

        // Act & Assert - should not throw
        await _messageHandler.HandleMessageAsync(message, "connection-1", _mockConnectionManager.Object);
    }
}
