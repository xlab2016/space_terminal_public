# Space Terminal - Setup Guide

## Prerequisites

### For API Server
- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Git (optional)

### For Windows Client
- .NET 8 Runtime
- Windows 10/11 or Windows Server

### For macOS Client
- .NET 8 Runtime
- macOS 11.0 or later

### For Android Client
- Android Studio Arctic Fox or later
- Android SDK (API Level 24+)
- Gradle 8.0+

### For iPhone Client
- Xcode 14.0 or later
- iOS 14.0+ deployment target
- macOS for development

## Installation

### 1. API Server Setup

**Step 1: Clone Repository**
```bash
git clone https://github.com/xlab2016/space_terminal_public.git
cd space_terminal_public
```

**Step 2: Build API Server**
```bash
cd Api
dotnet restore
dotnet build
```

**Step 3: Run API Server**
```bash
cd SpaceTerminal.Api
dotnet run
```

The server will start on `http://localhost:5000`

WebSocket endpoint: `ws://localhost:5000/ws`

**Step 4: Run Tests (Optional)**
```bash
cd ../SpaceTerminal.Tests
dotnet test
```

### 2. Windows Client Setup

**Step 1: Navigate to Windows Client**
```bash
cd Clients/Windows
```

**Step 2: Build Client**
```bash
dotnet build
```

**Step 3: Run Client**
```bash
dotnet run
```

Or specify server URL:
```bash
dotnet run ws://your-server-ip:5000/ws
```

### 3. macOS Client Setup

**Step 1: Navigate to macOS Client**
```bash
cd Clients/MacOS
```

**Step 2: Build Client**
```bash
dotnet build
```

**Step 3: Run Client**
```bash
dotnet run
```

Or specify server URL:
```bash
dotnet run ws://your-server-ip:5000/ws
```

### 4. Android Client Setup

**Step 1: Open Android Studio**
- Open Android Studio
- Select "Open an existing project"
- Navigate to `Clients/Android`

**Step 2: Sync Gradle**
- Android Studio will automatically sync Gradle
- Wait for dependencies to download

**Step 3: Update Server URL**
Edit `MainActivity.kt`:
```kotlin
// For emulator (localhost on host machine)
private val serverUrl = "ws://10.0.2.2:5000/ws"

// For physical device (use your server's IP)
private val serverUrl = "ws://192.168.1.100:5000/ws"
```

**Step 4: Run Application**
- Connect Android device or start emulator
- Click "Run" button in Android Studio

### 5. iPhone Client Setup

**Step 1: Open Xcode**
- Open Xcode
- Select "Open a project or file"
- Navigate to `Clients/IPhone/SpaceTerminal.xcodeproj`

**Step 2: Update Server URL**
Edit `WebSocketClient.swift`:
```swift
// For simulator (localhost on host machine)
private let serverURL = URL(string: "ws://localhost:5000/ws")!

// For physical device (use your server's IP)
private let serverURL = URL(string: "ws://192.168.1.100:5000/ws")!
```

**Step 3: Run Application**
- Select target device/simulator
- Click "Run" button (or Cmd+R)

## Configuration

### API Server Configuration

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
```

### Network Configuration

**Firewall Rules:**
- Open port 5000 (or your configured port)
- Allow WebSocket connections

**For Production:**
- Use WSS (WebSocket Secure) instead of WS
- Configure SSL/TLS certificates
- Use reverse proxy (nginx, Apache)

## Usage

### Windows/macOS Client Menu

After connecting, you'll see:
```
=== Main Menu ===
1. Send command to remote client
2. Send chat message
3. Create chat group
4. Request desktop sharing
5. Exit
```

### Android Client

- Enter recipient ID (optional for broadcast)
- Type message
- Click "Send Message"

### iPhone Client

- Tap "Connect" to establish connection
- Enter recipient ID (optional)
- Type message
- Tap "Send Message"

## Testing the System

### Test 1: Basic Chat

1. Start API server
2. Start Windows client (Client A)
3. Start macOS client (Client B)
4. Note the client IDs displayed
5. From Client A, send message to Client B using their ID
6. Verify message received on Client B

### Test 2: Remote Command Execution

1. Connect two clients
2. From Client A, select option 1 (Send command)
3. Enter Client B's ID
4. Enter command (e.g., "echo Hello")
5. Client B will show confirmation prompt
6. Approve on Client B
7. See command output on Client A

### Test 3: Group Chat

1. Connect multiple clients
2. From any client, create a group
3. Note the group ID
4. Other clients join using the group ID
5. Send messages to the group
6. Verify all members receive messages

## Troubleshooting

### Connection Issues

**Problem:** Client cannot connect to server

**Solutions:**
- Verify server is running: `netstat -an | grep 5000`
- Check firewall settings
- Verify correct server URL/IP
- Ensure network connectivity

### Authentication Failures

**Problem:** Authentication fails

**Solutions:**
- Check client sends correct message format
- Verify server logs for errors
- Ensure unique client IDs

### Command Execution Issues

**Problem:** Commands not executing

**Solutions:**
- Verify user approved command
- Check client has necessary permissions
- Review command syntax for platform (cmd.exe vs bash)

## Production Deployment

### Using Docker (Recommended)

**Create Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY Api/SpaceTerminal.Api/bin/Release/net8.0/publish/ .
EXPOSE 5000
ENTRYPOINT ["dotnet", "SpaceTerminal.Api.dll"]
```

**Build and Run:**
```bash
docker build -t space-terminal-api .
docker run -d -p 5000:5000 space-terminal-api
```

### Using systemd (Linux)

Create `/etc/systemd/system/space-terminal.service`:
```ini
[Unit]
Description=Space Terminal API Server
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/space-terminal
ExecStart=/usr/bin/dotnet /opt/space-terminal/SpaceTerminal.Api.dll
Restart=always
User=www-data

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable space-terminal
sudo systemctl start space-terminal
```

### SSL/TLS Configuration

**Using nginx as reverse proxy:**
```nginx
server {
    listen 443 ssl;
    server_name your-domain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location /ws {
        proxy_pass http://localhost:5000/ws;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}
```

## Support

For issues, questions, or contributions:
- GitHub Issues: https://github.com/xlab2016/space_terminal_public/issues
- Documentation: See `/docs` folder

## License

See LICENSE file in repository root.
