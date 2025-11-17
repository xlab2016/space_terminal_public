using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SpaceTerminal.Shared;

namespace SpaceTerminal.Windows;

class Program
{
    private static ClientWebSocket? _webSocket;
    private static Client? _client;
    private static string _serverUrl = "ws://localhost:5000/ws";
    private static CancellationTokenSource _cts = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Space Terminal - Windows Client ===");
        Console.WriteLine();

        if (args.Length > 0)
        {
            _serverUrl = args[0];
        }

        Console.Write("Enter client name: ");
        var clientName = Console.ReadLine() ?? "Windows-Client";

        _client = new Client
        {
            Name = clientName,
            Type = ClientType.Windows,
            PublicKey = GeneratePublicKey()
        };

        await ConnectAsync();

        // Start message receiving task
        var receiveTask = Task.Run(() => ReceiveMessagesAsync(_cts.Token));

        // Main menu
        await ShowMenuAsync();

        _cts.Cancel();
        await receiveTask;
    }

    private static async Task ConnectAsync()
    {
        try
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(_serverUrl), CancellationToken.None);
            Console.WriteLine($"Connected to server: {_serverUrl}");

            // Send authentication
            var authMessage = new Message
            {
                Type = MessageType.Authentication,
                SenderId = _client!.Id,
                Payload = JsonSerializer.Serialize(_client)
            };

            await SendMessageAsync(authMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }
    }

    private static async Task ShowMenuAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            Console.WriteLine("\n=== Main Menu ===");
            Console.WriteLine("1. Send command to remote client");
            Console.WriteLine("2. Start chat");
            Console.WriteLine("3. Create chat group");
            Console.WriteLine("4. Request desktop sharing");
            Console.WriteLine("5. Exit");
            Console.Write("Select option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await SendCommandAsync();
                    break;
                case "2":
                    await SendChatMessageAsync();
                    break;
                case "3":
                    await CreateChatGroupAsync();
                    break;
                case "4":
                    await RequestDesktopSharingAsync();
                    break;
                case "5":
                    _cts.Cancel();
                    return;
                default:
                    Console.WriteLine("Invalid option");
                    break;
            }
        }
    }

    private static async Task SendCommandAsync()
    {
        Console.Write("Enter target client ID: ");
        var targetId = Console.ReadLine();

        Console.Write("Enter command: ");
        var command = Console.ReadLine();

        if (string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(command))
        {
            Console.WriteLine("Invalid input");
            return;
        }

        var commandExecution = new
        {
            Id = Guid.NewGuid().ToString(),
            Command = command,
            ClientId = targetId,
            RequesterId = _client!.Id,
            Status = 0, // CommandStatus.PendingConfirmation
            RequestedAt = DateTime.UtcNow
        };

        var message = new Message
        {
            Type = MessageType.Command,
            SenderId = _client.Id,
            ReceiverId = targetId,
            Payload = JsonSerializer.Serialize(commandExecution)
        };

        await SendMessageAsync(message);
        Console.WriteLine("Command sent, waiting for confirmation...");
    }

    private static async Task SendChatMessageAsync()
    {
        Console.Write("Enter recipient ID (or leave empty for broadcast): ");
        var recipientId = Console.ReadLine();

        Console.Write("Enter message: ");
        var content = Console.ReadLine();

        if (string.IsNullOrEmpty(content))
        {
            Console.WriteLine("Message cannot be empty");
            return;
        }

        var chatMessage = new
        {
            Id = Guid.NewGuid().ToString(),
            SenderId = _client!.Id,
            ReceiverId = string.IsNullOrEmpty(recipientId) ? null : recipientId,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        var message = new Message
        {
            Type = MessageType.ChatMessage,
            SenderId = _client.Id,
            ReceiverId = string.IsNullOrEmpty(recipientId) ? null : recipientId,
            Payload = JsonSerializer.Serialize(chatMessage)
        };

        await SendMessageAsync(message);
        Console.WriteLine("Message sent");
    }

    private static async Task CreateChatGroupAsync()
    {
        Console.Write("Enter group name: ");
        var groupName = Console.ReadLine();

        if (string.IsNullOrEmpty(groupName))
        {
            Console.WriteLine("Group name cannot be empty");
            return;
        }

        var group = new
        {
            Id = Guid.NewGuid().ToString(),
            Name = groupName,
            MemberIds = new[] { _client!.Id },
            CreatorId = _client.Id,
            CreatedAt = DateTime.UtcNow
        };

        var message = new Message
        {
            Type = MessageType.ChatGroupCreate,
            SenderId = _client.Id,
            Payload = JsonSerializer.Serialize(group)
        };

        await SendMessageAsync(message);
        Console.WriteLine("Group creation request sent");
    }

    private static async Task RequestDesktopSharingAsync()
    {
        Console.Write("Enter target client ID: ");
        var targetId = Console.ReadLine();

        if (string.IsNullOrEmpty(targetId))
        {
            Console.WriteLine("Invalid input");
            return;
        }

        var message = new Message
        {
            Type = MessageType.DesktopStreamStart,
            SenderId = _client!.Id,
            ReceiverId = targetId,
            Payload = JsonSerializer.Serialize(new { quality = "medium", fps = 30 })
        };

        await SendMessageAsync(message);
        Console.WriteLine("Desktop sharing request sent");
    }

    private static async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 64];

        try
        {
            while (_webSocket != null && _webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = Message.FromJson(messageJson);

                    if (message != null)
                    {
                        await HandleReceivedMessageAsync(message);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError receiving message: {ex.Message}");
        }
    }

    private static async Task HandleReceivedMessageAsync(Message message)
    {
        // Don't log CommandConfirmationRequest as it has its own UI
        if (message.Type != MessageType.CommandConfirmationRequest)
        {
            Console.WriteLine($"\n[{message.Type}] Received from {message.SenderId}");
        }

        switch (message.Type)
        {
            case MessageType.AuthenticationResponse:
                try
                {
                    using var doc = JsonDocument.Parse(message.Payload);
                    var root = doc.RootElement;
                    var clientId = root.TryGetProperty("clientId", out var idElement) ? idElement.GetString() : _client?.Id;
                    Console.WriteLine($"Authentication successful! Client ID: {clientId}");
                }
                catch
                {
                    Console.WriteLine($"Authentication successful! Client ID: {_client?.Id}");
                }
                break;

            case MessageType.CommandConfirmationRequest:
                await HandleCommandConfirmationRequestAsync(message);
                break;

            case MessageType.CommandResponse:
                Console.WriteLine($"Command response: {message.Payload}");
                break;

            case MessageType.ChatMessage:
                try
                {
                    using var chatDoc = JsonDocument.Parse(message.Payload);
                    var chatRoot = chatDoc.RootElement;
                    var content = chatRoot.TryGetProperty("Content", out var contentElement) ? contentElement.GetString() : "";
                    Console.WriteLine($"Chat from {message.SenderId}: {content}");
                }
                catch
                {
                    Console.WriteLine($"Chat from {message.SenderId}: [unable to parse message]");
                }
                break;

            case MessageType.DesktopStreamStart:
                Console.WriteLine("Desktop sharing started");
                break;

            case MessageType.DesktopFrame:
                Console.WriteLine("Received desktop frame (video stream)");
                break;

            case MessageType.Error:
                Console.WriteLine($"Error: {message.Payload}");
                break;

            default:
                Console.WriteLine($"Unhandled message type: {message.Type}");
                break;
        }
    }

    private static async Task HandleCommandConfirmationRequestAsync(Message message)
    {
        using var doc = JsonDocument.Parse(message.Payload);
        var root = doc.RootElement;
        var commandId = root.TryGetProperty("Id", out var idElement) ? idElement.GetString() : null;
        var command = root.TryGetProperty("Command", out var cmdElement) ? cmdElement.GetString() : null;

        Console.WriteLine($"\n[!] Command execution request received from {message.SenderId}");
        Console.WriteLine($"    Command: {command}");
        Console.WriteLine($"    Opening confirmation window...");

        // Create temporary PowerShell script for confirmation
        var tempScript = Path.Combine(Path.GetTempPath(), $"confirm_{Guid.NewGuid()}.ps1");
        var scriptContent = $@"
$host.UI.RawUI.WindowTitle = 'Command Confirmation Request'
Write-Host ''
Write-Host '================================================================' -ForegroundColor Cyan
Write-Host '  COMMAND EXECUTION REQUEST' -ForegroundColor Yellow
Write-Host '================================================================' -ForegroundColor Cyan
Write-Host ''
Write-Host 'From: {message.SenderId}' -ForegroundColor White
Write-Host 'Command: {command}' -ForegroundColor Green
Write-Host ''
Write-Host '================================================================' -ForegroundColor Cyan
Write-Host ''
$response = Read-Host 'Approve and execute? (y/n)'
if ($response -eq 'y' -or $response -eq 'yes') {{
    exit 0
}} else {{
    exit 1
}}
";
        File.WriteAllText(tempScript, scriptContent);

        var approved = false;
        string output = "";
        string error = "";

        try
        {
            // Open confirmation in new window
            var confirmProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false
                }
            };

            confirmProcess.Start();
            await confirmProcess.WaitForExitAsync();
            approved = confirmProcess.ExitCode == 0;

            Console.WriteLine($"    User response: {(approved ? "APPROVED" : "REJECTED")}");

            if (approved)
            {
                // Execute command
                Console.WriteLine($"    Executing command...");
                var execProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                execProcess.Start();
                output = await execProcess.StandardOutput.ReadToEndAsync();
                error = await execProcess.StandardError.ReadToEndAsync();
                await execProcess.WaitForExitAsync();

                Console.WriteLine($"    ✓ Command executed successfully");
                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine($"\n    Output:\n{output}");
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"\n    Error:\n{error}");
                }
            }
            else
            {
                Console.WriteLine($"    Command rejected by user");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ Error: {ex.Message}");
            error = ex.Message;
            approved = false;
        }
        finally
        {
            // Clean up temp script
            try { File.Delete(tempScript); } catch { }
        }

        var confirmationMessage = new Message
        {
            Type = MessageType.CommandConfirmation,
            SenderId = _client!.Id,
            ReceiverId = message.SenderId,
            Payload = JsonSerializer.Serialize(new
            {
                commandId = commandId,
                approved = approved
            })
        };

        await SendMessageAsync(confirmationMessage);
    }

    private static async Task SendMessageAsync(Message message)
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            var messageJson = message.ToJson();
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private static string GeneratePublicKey()
    {
        // Simple key generation for demo purposes
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
