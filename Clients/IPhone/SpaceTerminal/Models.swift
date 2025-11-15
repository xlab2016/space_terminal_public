import Foundation

enum MessageType: String, Codable {
    case authentication = "Authentication"
    case authenticationResponse = "AuthenticationResponse"
    case command = "Command"
    case commandResponse = "CommandResponse"
    case commandConfirmationRequest = "CommandConfirmationRequest"
    case commandConfirmation = "CommandConfirmation"
    case desktopStreamStart = "DesktopStreamStart"
    case desktopStreamStop = "DesktopStreamStop"
    case desktopFrame = "DesktopFrame"
    case audioFrame = "AudioFrame"
    case chatMessage = "ChatMessage"
    case chatGroupCreate = "ChatGroupCreate"
    case chatGroupJoin = "ChatGroupJoin"
    case chatGroupLeave = "ChatGroupLeave"
    case heartbeat = "Heartbeat"
    case error = "Error"
}

enum ClientType: String, Codable {
    case windows = "Windows"
    case macOS = "MacOS"
    case android = "Android"
    case iPhone = "IPhone"
}

struct Message: Codable {
    let id: String
    let type: MessageType
    let senderId: String
    let receiverId: String?
    let groupId: String?
    let payload: String
    let timestamp: String
    let isEncrypted: Bool

    init(type: MessageType, senderId: String, receiverId: String? = nil, groupId: String? = nil, payload: String = "") {
        self.id = UUID().uuidString
        self.type = type
        self.senderId = senderId
        self.receiverId = receiverId
        self.groupId = groupId
        self.payload = payload
        self.timestamp = ISO8601DateFormatter().string(from: Date())
        self.isEncrypted = true
    }
}

struct Client: Codable {
    let id: String
    let name: String
    let publicKey: String
    let type: ClientType
}

struct ChatMessage: Codable {
    let id: String
    let senderId: String
    let receiverId: String?
    let content: String
    let timestamp: String

    init(senderId: String, receiverId: String?, content: String) {
        self.id = UUID().uuidString
        self.senderId = senderId
        self.receiverId = receiverId
        self.content = content
        self.timestamp = ISO8601DateFormatter().string(from: Date())
    }
}
