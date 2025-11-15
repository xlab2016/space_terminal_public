# Space Terminal

A comprehensive .NET 8 API server for secure remote management of devices across multiple platforms (Windows, macOS, Android, iPhone).

## Features

- **Secure WebSocket Communication**: Real-time bidirectional communication using WebSocket protocol
- **End-to-End Encryption**: RSA-4096 asymmetric encryption for all messages
- **Multi-Platform Support**: Native clients for Windows, macOS, Android, and iPhone
- **Remote Command Execution**: Execute commands remotely with user confirmation
- **Desktop Sharing**: Stream desktop with video/audio compression (like AnyDesk/TeamViewer)
- **Chat System**: Direct messaging and group chat functionality
- **Scalable Architecture**: Built for high availability and horizontal scaling
- **Authentication & Authorization**: Secure client authentication with public key cryptography

## Architecture

### API Server (.NET 8)
- **WebSocket Server**: Real-time communication hub
- **Encryption Service**: RSA-4096 for secure data transmission
- **Client Manager**: Track and manage connected clients
- **Message Router**: Intelligent message routing between clients
- **Scalable Design**: Ready for load balancing and clustering

### Client Applications

#### Windows Client (CLI)
- .NET 8 console application
- Full command execution support
- Desktop sharing capabilities
- Real-time chat

#### macOS Client (CLI)
- .NET 8 console application
- Native Unix command execution
- Full feature parity with Windows client

#### Android Client
- Native Android app (Kotlin)
- Material Design UI
- Push notification support
- Mobile-optimized interface

#### iPhone Client
- Native iOS app (Swift/SwiftUI)
- Modern iOS design
- Background connectivity
- Optimized for iPhone/iPad

## Quick Start

### Prerequisites
- .NET 8 SDK
- For mobile development: Android Studio or Xcode

### Start API Server
```bash
cd Api/SpaceTerminal.Api
dotnet run
```

Server starts at: `ws://localhost:5000/ws`

### Start Windows Client
```bash
cd Clients/Windows
dotnet run
```

### Start macOS Client
```bash
cd Clients/MacOS
dotnet run
```

For mobile clients, see [Setup Guide](docs/SETUP.md).

## Documentation

- **[Architecture](docs/ARCHITECTURE.md)**: Detailed system architecture and design
- **[Setup Guide](docs/SETUP.md)**: Complete installation and configuration instructions
- **[API Protocol](docs/ARCHITECTURE.md#3-communication-protocol)**: WebSocket message protocol specification

## Core Features

### 1. Remote Command Execution
Execute commands on remote devices with mandatory user approval:
```
User A → Command Request → Server → User B (Approval) → Execution → Result → User A
```

### 2. Desktop Sharing
Real-time desktop streaming with compression:
- Configurable video quality
- Audio streaming support
- Frame-based transmission
- Low latency

### 3. Secure Chat
Encrypted messaging system:
- Direct 1-to-1 messaging
- Group chat support
- Message persistence
- Real-time delivery

### 4. Authentication
Secure client authentication:
- Public/private key pairs
- Digital signatures
- Message integrity verification
- Client identity management

## Security

- **RSA-4096 Encryption**: Industry-standard asymmetric encryption
- **Message Signing**: Cryptographic signatures for message authenticity
- **User Confirmation**: Mandatory approval for sensitive operations
- **Secure WebSocket**: Can be upgraded to WSS with TLS/SSL
- **No Anonymous Access**: All clients must authenticate

## Technology Stack

**Backend:**
- .NET 8
- ASP.NET Core WebSocket
- System.Security.Cryptography

**Clients:**
- Windows/macOS: .NET 8, C#
- Android: Kotlin, OkHttp3, Gson
- iPhone: Swift 5.9, SwiftUI, URLSession

## Testing

Run unit tests:
```bash
cd Api/SpaceTerminal.Tests
dotnet test
```

## Deployment

### Docker
```bash
docker build -t space-terminal .
docker run -p 5000:5000 space-terminal
```

### Production
See [Setup Guide](docs/SETUP.md#production-deployment) for:
- SSL/TLS configuration
- Reverse proxy setup
- Systemd service
- Load balancing

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

See LICENSE file.

## Support

For issues and questions:
- GitHub Issues: https://github.com/xlab2016/space_terminal_public/issues

## Project Structure

```
space_terminal_public/
├── Api/
│   ├── SpaceTerminal.Api/          # API server application
│   ├── SpaceTerminal.Core/         # Domain models and interfaces
│   ├── SpaceTerminal.Infrastructure/ # Service implementations
│   └── SpaceTerminal.Tests/        # Unit tests
├── Clients/
│   ├── Windows/                    # Windows CLI client
│   ├── MacOS/                      # macOS CLI client
│   ├── Android/                    # Android mobile client
│   └── IPhone/                     # iPhone mobile client
├── Shared/                         # Shared protocol definitions
└── docs/                           # Documentation
    ├── ARCHITECTURE.md
    └── SETUP.md
```