using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SpaceTerminal.Shared;

namespace SpaceTerminal.MacOS;

class Program
{
    private static ClientWebSocket? _webSocket;
    private static Client? _client;
    private static string _serverUrl = "ws://localhost:5000/ws";
    private static CancellationTokenSource _cts = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Space Terminal - macOS Client ===");
        Console.WriteLine();

        if (args.Length > 0)
        {
            _serverUrl = args[0];
        }

        Console.Write("Enter client name: ");
        var clientName = Console.ReadLine() ?? "MacOS-Client";

        _client = new Client
        {
            Name = clientName,
            Type = ClientType.MacOS,
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
            Console.WriteLine("2. Send chat message");
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
            Status = "PendingConfirmation"
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
        Console.WriteLine($"\n[{message.Type}] Received from {message.SenderId}");

        switch (message.Type)
        {
            case MessageType.AuthenticationResponse:
                Console.WriteLine("Authentication successful!");
                break;

            case MessageType.CommandConfirmationRequest:
                await HandleCommandConfirmationRequestAsync(message);
                break;

            case MessageType.CommandResponse:
                Console.WriteLine($"Command response: {message.Payload}");
                break;

            case MessageType.ChatMessage:
                var chatMsg = JsonSerializer.Deserialize<dynamic>(message.Payload);
                Console.WriteLine($"Chat from {message.SenderId}: {chatMsg?.Content}");
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

        Console.Write("\nPress Enter to continue...");
    }

    private static async Task HandleCommandConfirmationRequestAsync(Message message)
    {
        var commandExecution = JsonSerializer.Deserialize<dynamic>(message.Payload);
        var command = commandExecution?.Command?.ToString();

        Console.WriteLine($"\nCommand execution request from {message.SenderId}:");
        Console.WriteLine($"Command: {command}");
        Console.Write("Approve? (y/n): ");

        var response = Console.ReadLine()?.ToLower();
        var approved = response == "y" || response == "yes";

        if (approved)
        {
            // Execute command (using bash/zsh on macOS)
            Console.WriteLine("Executing command...");
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                Console.WriteLine($"Output: {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Error: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Execution failed: {ex.Message}");
            }
        }

        var confirmationMessage = new Message
        {
            Type = MessageType.CommandConfirmation,
            SenderId = _client!.Id,
            ReceiverId = message.SenderId,
            Payload = JsonSerializer.Serialize(new
            {
                commandId = commandExecution?.Id?.ToString(),
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
