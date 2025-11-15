namespace SpaceTerminal.Core.Models;

public class CommandExecution
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Command { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string RequesterId { get; set; } = string.Empty;
    public CommandStatus Status { get; set; } = CommandStatus.PendingConfirmation;
    public string? Output { get; set; }
    public string? Error { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
}

public enum CommandStatus
{
    PendingConfirmation,
    Confirmed,
    Rejected,
    Executing,
    Completed,
    Failed
}
