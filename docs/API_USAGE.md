# Space Terminal API Usage Guide

## WebSocket Connection

### Endpoint
```
ws://localhost:5000/ws
```

For production with SSL:
```
wss://your-domain.com/ws
```

## Message Protocol

All messages are JSON-formatted and follow this structure:

```json
{
  "id": "unique-message-id",
  "type": "MessageType",
  "senderId": "client-id",
  "receiverId": "optional-target-id",
  "groupId": "optional-group-id",
  "payload": "json-string-payload",
  "timestamp": "2024-01-01T00:00:00Z",
  "isEncrypted": true
}
```

## Authentication

### 1. Connect to WebSocket

```javascript
const ws = new WebSocket('ws://localhost:5000/ws');
```

### 2. Send Authentication Message

```json
{
  "id": "msg-001",
  "type": "Authentication",
  "senderId": "client-123",
  "payload": "{\"id\":\"client-123\",\"name\":\"My Client\",\"publicKey\":\"base64-encoded-key\",\"type\":\"Windows\"}",
  "timestamp": "2024-01-01T12:00:00Z",
  "isEncrypted": false
}
```

### 3. Receive Authentication Response

```json
{
  "type": "AuthenticationResponse",
  "senderId": "server",
  "receiverId": "client-123",
  "payload": "{\"success\":true,\"clientId\":\"client-123\"}"
}
```

## Remote Command Execution

### Step 1: Send Command Request

```json
{
  "type": "Command",
  "senderId": "requester-id",
  "receiverId": "target-client-id",
  "payload": "{\"Id\":\"cmd-001\",\"Command\":\"echo Hello\",\"ClientId\":\"target-client-id\",\"RequesterId\":\"requester-id\",\"Status\":\"PendingConfirmation\"}"
}
```

### Step 2: Target Receives Confirmation Request

```json
{
  "type": "CommandConfirmationRequest",
  "senderId": "requester-id",
  "receiverId": "target-client-id",
  "payload": "{\"Id\":\"cmd-001\",\"Command\":\"echo Hello\",...}"
}
```

### Step 3: Target Sends Confirmation

```json
{
  "type": "CommandConfirmation",
  "senderId": "target-client-id",
  "receiverId": "requester-id",
  "payload": "{\"commandId\":\"cmd-001\",\"approved\":true}"
}
```

### Step 4: Receive Command Response

```json
{
  "type": "CommandResponse",
  "senderId": "server",
  "receiverId": "requester-id",
  "payload": "{\"Id\":\"cmd-001\",\"Status\":\"Confirmed\",\"Output\":\"Hello\",\"Error\":null}"
}
```

## Chat Messaging

### Direct Message (1-to-1)

```json
{
  "type": "ChatMessage",
  "senderId": "client-a",
  "receiverId": "client-b",
  "payload": "{\"id\":\"chat-001\",\"senderId\":\"client-a\",\"receiverId\":\"client-b\",\"content\":\"Hello!\",\"timestamp\":\"2024-01-01T12:00:00Z\"}"
}
```

### Create Group Chat

```json
{
  "type": "ChatGroupCreate",
  "senderId": "client-a",
  "payload": "{\"id\":\"group-001\",\"name\":\"My Group\",\"memberIds\":[\"client-a\"],\"creatorId\":\"client-a\",\"createdAt\":\"2024-01-01T12:00:00Z\"}"
}
```

### Join Group

```json
{
  "type": "ChatGroupJoin",
  "senderId": "client-b",
  "payload": "{\"groupId\":\"group-001\"}"
}
```

### Send Group Message

```json
{
  "type": "ChatMessage",
  "senderId": "client-a",
  "groupId": "group-001",
  "payload": "{\"id\":\"chat-002\",\"senderId\":\"client-a\",\"groupId\":\"group-001\",\"content\":\"Hello group!\",\"timestamp\":\"2024-01-01T12:00:00Z\"}"
}
```

## Desktop Sharing

### Start Desktop Stream

```json
{
  "type": "DesktopStreamStart",
  "senderId": "viewer-id",
  "receiverId": "sharer-id",
  "payload": "{\"quality\":\"medium\",\"fps\":30}"
}
```

### Send Desktop Frame

```json
{
  "type": "DesktopFrame",
  "senderId": "sharer-id",
  "receiverId": "viewer-id",
  "payload": "{\"frameData\":\"base64-encoded-image\",\"timestamp\":\"2024-01-01T12:00:00.123Z\"}"
}
```

### Send Audio Frame

```json
{
  "type": "AudioFrame",
  "senderId": "sharer-id",
  "receiverId": "viewer-id",
  "payload": "{\"audioData\":\"base64-encoded-audio\",\"timestamp\":\"2024-01-01T12:00:00.123Z\"}"
}
```

### Stop Desktop Stream

```json
{
  "type": "DesktopStreamStop",
  "senderId": "viewer-id",
  "receiverId": "sharer-id",
  "payload": "{}"
}
```

## Heartbeat

Keep connection alive:

```json
{
  "type": "Heartbeat",
  "senderId": "client-id",
  "payload": "{}"
}
```

Recommended interval: Every 30 seconds

## Error Handling

### Error Response Format

```json
{
  "type": "Error",
  "senderId": "server",
  "receiverId": "client-id",
  "payload": "{\"error\":\"Error message description\"}"
}
```

### Common Errors

**Authentication Failed:**
```json
{
  "type": "Error",
  "payload": "{\"error\":\"Authentication failed: Invalid credentials\"}"
}
```

**Command Execution Failed:**
```json
{
  "type": "Error",
  "payload": "{\"error\":\"Command handling failed: Target client not found\"}"
}
```

**Unknown Message Type:**
```json
{
  "type": "Error",
  "payload": "{\"error\":\"Unknown message type\"}"
}
```

## Message Types Reference

| Type | Description | Requires Receiver | Requires Group |
|------|-------------|-------------------|----------------|
| Authentication | Initial client authentication | No | No |
| AuthenticationResponse | Server auth confirmation | Yes | No |
| Command | Remote command request | Yes | No |
| CommandConfirmationRequest | Request user approval | Yes | No |
| CommandConfirmation | User's approval/rejection | Yes | No |
| CommandResponse | Command execution result | Yes | No |
| DesktopStreamStart | Start desktop sharing | Yes | No |
| DesktopStreamStop | Stop desktop sharing | Yes | No |
| DesktopFrame | Video frame data | Yes | No |
| AudioFrame | Audio frame data | Yes | No |
| ChatMessage | Chat message | Optional | Optional |
| ChatGroupCreate | Create new group | No | No |
| ChatGroupJoin | Join existing group | No | No |
| ChatGroupLeave | Leave group | No | Yes |
| Heartbeat | Keep-alive signal | No | No |
| Error | Error notification | Yes | No |

## Client Implementation Examples

### JavaScript/Node.js

```javascript
const WebSocket = require('ws');

const ws = new WebSocket('ws://localhost:5000/ws');
const clientId = generateClientId();

ws.on('open', () => {
  // Authenticate
  const authMessage = {
    type: 'Authentication',
    senderId: clientId,
    payload: JSON.stringify({
      id: clientId,
      name: 'JS Client',
      publicKey: generatePublicKey(),
      type: 'Windows'
    })
  };
  ws.send(JSON.stringify(authMessage));
});

ws.on('message', (data) => {
  const message = JSON.parse(data);
  console.log('Received:', message);

  if (message.type === 'CommandConfirmationRequest') {
    // Handle command confirmation
    handleCommandConfirmation(message);
  }
});

function sendChatMessage(recipientId, content) {
  const message = {
    type: 'ChatMessage',
    senderId: clientId,
    receiverId: recipientId,
    payload: JSON.stringify({
      senderId: clientId,
      receiverId: recipientId,
      content: content,
      timestamp: new Date().toISOString()
    })
  };
  ws.send(JSON.stringify(message));
}
```

### Python

```python
import websocket
import json
import uuid

client_id = str(uuid.uuid4())

def on_message(ws, message):
    data = json.loads(message)
    print(f"Received: {data['type']}")

    if data['type'] == 'CommandConfirmationRequest':
        handle_command_confirmation(ws, data)

def on_open(ws):
    auth_message = {
        'type': 'Authentication',
        'senderId': client_id,
        'payload': json.dumps({
            'id': client_id,
            'name': 'Python Client',
            'publicKey': generate_public_key(),
            'type': 'Windows'
        })
    }
    ws.send(json.dumps(auth_message))

ws = websocket.WebSocketApp(
    'ws://localhost:5000/ws',
    on_message=on_message,
    on_open=on_open
)

ws.run_forever()
```

## Best Practices

1. **Always authenticate** immediately after connection
2. **Send heartbeats** every 30 seconds to keep connection alive
3. **Handle errors** gracefully and log them
4. **Encrypt payloads** when transmitting sensitive data
5. **Validate messages** before processing
6. **Use unique IDs** for all messages
7. **Close connections** properly when done
8. **Implement reconnection** logic for network issues

## Rate Limiting

Current implementation has no rate limiting. For production:

- Recommended: Max 100 messages per second per client
- Heartbeat: Every 30 seconds (not more frequently)
- Desktop frames: Max 60 FPS

## Security Considerations

1. **Use WSS in production** (WebSocket Secure)
2. **Validate all message types** before processing
3. **Sanitize command inputs** to prevent injection
4. **Implement timeout** for command confirmations
5. **Log all commands** for audit purposes
6. **Use strong encryption** for sensitive payloads
7. **Authenticate every message** using digital signatures

## Troubleshooting

### Connection Drops

```javascript
ws.on('close', () => {
  console.log('Connection closed, reconnecting...');
  setTimeout(connect, 5000);
});
```

### Message Not Received

- Check client is authenticated
- Verify receiver ID is correct
- Ensure receiver is online
- Check server logs for errors

### Command Execution Timeout

Implement timeout handling:

```javascript
const commandTimeout = setTimeout(() => {
  console.log('Command confirmation timeout');
  // Handle timeout
}, 60000); // 60 second timeout
```
