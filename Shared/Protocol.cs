using System;
using System.Text.Json;

namespace SpaceTerminal.Shared;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MessageType Type { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string? ReceiverId { get; set; }
    public string? GroupId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsEncrypted { get; set; } = true;

    public string ToJson() => JsonSerializer.Serialize(this);
    public static Message? FromJson(string json) => JsonSerializer.Deserialize<Message>(json);
}

public enum MessageType
{
    Authentication,
    AuthenticationResponse,
    Command,
    CommandResponse,
    CommandConfirmationRequest,
    CommandConfirmation,
    DesktopStreamStart,
    DesktopStreamStop,
    DesktopFrame,
    AudioFrame,
    ChatMessage,
    ChatGroupCreate,
    ChatGroupJoin,
    ChatGroupLeave,
    Heartbeat,
    Error
}

public enum ClientType
{
    Windows,
    MacOS,
    Android,
    IPhone
}

public class Client
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public ClientType Type { get; set; }
}
