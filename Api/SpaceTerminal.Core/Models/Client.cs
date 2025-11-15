namespace SpaceTerminal.Core.Models;

public class Client
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public ClientType Type { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public string? SessionId { get; set; }
}

public enum ClientType
{
    Windows,
    MacOS,
    Android,
    IPhone
}
