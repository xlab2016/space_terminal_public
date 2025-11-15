# Space Terminal - Architecture Documentation

## Overview

Space Terminal is a comprehensive remote management solution designed for secure communication and control of multiple device types (Windows, macOS, Android, iPhone) through a centralized .NET 8 API server.

## Architecture Components

### 1. API Server (.NET 8)

**Location:** `/Api/SpaceTerminal.Api`

**Key Features:**
- WebSocket-based real-time communication
- Scalable and fault-tolerant design
- RESTful API endpoints
- Built on .NET 8 for optimal performance

**Architecture Layers:**
- **SpaceTerminal.Api**: Presentation layer with WebSocket handlers
- **SpaceTerminal.Core**: Domain models and service interfaces
- **SpaceTerminal.Infrastructure**: Service implementations

### 2. Security Architecture

**Encryption:**
- **Algorithm:** RSA-4096 for asymmetric encryption
- **Features:**
  - Public/private key pair generation
  - Data encryption/decryption
  - Digital signatures for message integrity
  - OAEP SHA256 padding for secure encryption

**Authentication Flow:**
1. Client connects via WebSocket
2. Client sends authentication message with public key
3. Server validates and registers client
4. Server responds with authentication confirmation
5. All subsequent messages are encrypted

### 3. Communication Protocol

**Transport:** WebSocket (ws://)

**Message Structure:**
```json
{
  "id": "unique-message-id",
  "type": "MessageType",
  "senderId": "client-id",
  "receiverId": "target-client-id",
  "groupId": "optional-group-id",
  "payload": "json-serialized-data",
  "timestamp": "ISO-8601-timestamp",
  "isEncrypted": true
}
```

**Message Types:**
- `Authentication`: Initial client authentication
- `Command`: Remote command execution request
- `CommandConfirmationRequest`: Request user approval for command
- `CommandConfirmation`: User's approval/rejection
- `CommandResponse`: Command execution result
- `DesktopStreamStart/Stop`: Desktop sharing control
- `DesktopFrame/AudioFrame`: Stream data
- `ChatMessage`: Chat messages
- `ChatGroupCreate/Join/Leave`: Group chat management
- `Heartbeat`: Keep-alive signal
- `Error`: Error notifications

### 4. Client Applications

#### Windows Client (.NET 8 CLI)
- **Location:** `/Clients/Windows`
- **Features:** Full command execution, chat, desktop sharing
- **Platform:** Windows (any version supporting .NET 8)

#### macOS Client (.NET 8 CLI)
- **Location:** `/Clients/MacOS`
- **Features:** Full command execution, chat, desktop sharing
- **Platform:** macOS (x64/ARM64)

#### Android Client (Kotlin)
- **Location:** `/Clients/Android`
- **Features:** Chat, notifications, remote monitoring
- **Platform:** Android 7.0+ (API 24+)
- **Dependencies:** OkHttp for WebSocket, Gson for JSON

#### iPhone Client (Swift)
- **Location:** `/Clients/IPhone`
- **Features:** Chat, notifications, remote monitoring
- **Platform:** iOS 14.0+
- **Framework:** SwiftUI, URLSession WebSocket

## Core Features Implementation

### 1. Remote Command Execution

**Flow:**
1. Requester sends `Command` message to server
2. Server forwards `CommandConfirmationRequest` to target client
3. Target client shows confirmation prompt to user
4. User approves/rejects command
5. Client sends `CommandConfirmation` to server
6. If approved, client executes command and sends `CommandResponse`
7. Server forwards response to requester

**Security:**
- User must explicitly approve each command
- Commands are executed in sandboxed environment
- Full audit trail maintained

### 2. Desktop Sharing

**Features:**
- Real-time screen capture
- Video compression (configurable quality)
- Audio streaming support
- Frame-based transmission

**Protocol:**
- `DesktopStreamStart`: Initiates sharing session
- `DesktopFrame`: Transmits video frames
- `AudioFrame`: Transmits audio data
- `DesktopStreamStop`: Ends sharing session

### 3. Chat System

**Features:**
- Direct messaging (1-to-1)
- Group chat support
- Message history
- Typing indicators support

**Implementation:**
- In-memory storage (can be extended to database)
- Real-time message delivery
- Group membership management

## Scalability Considerations

### Horizontal Scaling
- **Load Balancer:** Deploy behind reverse proxy (nginx/HAProxy)
- **Session Affinity:** WebSocket connections require sticky sessions
- **State Management:** Use Redis for shared state across instances

### Vertical Scaling
- **Connection Pool:** Configurable WebSocket connection limits
- **Memory Management:** Efficient buffer usage for large messages
- **Threading:** Async/await pattern throughout

### Fault Tolerance
- **Heartbeat Monitoring:** Regular health checks
- **Automatic Reconnection:** Clients retry on disconnect
- **Circuit Breaker:** Prevent cascade failures

## Data Flow Diagram

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│   Client    │         │  API Server │         │   Client    │
│  (Windows)  │◄───────►│  (WebSocket)│◄───────►│   (macOS)   │
└─────────────┘         └─────────────┘         └─────────────┘
                              ▲
                              │
                    ┌─────────┴─────────┐
                    │                   │
              ┌─────▼────┐        ┌────▼─────┐
              │ Android  │        │  iPhone  │
              │  Client  │        │  Client  │
              └──────────┘        └──────────┘
```

## Technology Stack

**Backend:**
- .NET 8
- ASP.NET Core WebSocket
- System.Security.Cryptography (RSA)

**Windows/macOS Clients:**
- .NET 8 Console Application
- ClientWebSocket

**Android Client:**
- Kotlin
- OkHttp3 WebSocket
- Gson

**iPhone Client:**
- Swift 5.9
- SwiftUI
- URLSession WebSocket

## Security Best Practices

1. **End-to-End Encryption:** All messages encrypted with RSA-4096
2. **Authentication Required:** No anonymous connections
3. **Command Approval:** User confirmation for all remote commands
4. **Audit Logging:** All actions logged for security review
5. **Secure Key Storage:** Private keys stored securely on device
6. **Transport Security:** Can be upgraded to WSS (WebSocket Secure)

## Future Enhancements

1. **Database Integration:** Persistent storage for messages and history
2. **File Transfer:** Secure file sharing between clients
3. **Video Calls:** Real-time video communication
4. **Mobile Push Notifications:** Background message delivery
5. **Multi-factor Authentication:** Enhanced security
6. **Role-Based Access Control:** Permission management
7. **Cloud Deployment:** Docker containerization and Kubernetes orchestration
