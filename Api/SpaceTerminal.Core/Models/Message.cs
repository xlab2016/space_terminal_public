namespace SpaceTerminal.Core.Models;

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
