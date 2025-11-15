import Foundation

class WebSocketClient: NSObject, ObservableObject, URLSessionWebSocketDelegate {
    @Published var isConnected = false
    @Published var messages: [String] = []
    @Published var statusMessage = "Disconnected"

    private var webSocketTask: URLSessionWebSocketTask?
    private let clientId = UUID().uuidString
    private let serverURL = URL(string: "ws://localhost:5000/ws")!

    func connect() {
        let session = URLSession(configuration: .default, delegate: self, delegateQueue: OperationQueue())
        webSocketTask = session.webSocketTask(with: serverURL)
        webSocketTask?.resume()

        receiveMessage()
        authenticate()
    }

    func disconnect() {
        webSocketTask?.cancel(with: .goingAway, reason: nil)
        isConnected = false
        statusMessage = "Disconnected"
    }

    private func authenticate() {
        let client = Client(
            id: clientId,
            name: "iPhone-Client",
            publicKey: generatePublicKey(),
            type: .iPhone
        )

        let encoder = JSONEncoder()
        if let clientData = try? encoder.encode(client),
           let clientJson = String(data: clientData, encoding: .utf8) {

            let message = Message(
                type: .authentication,
                senderId: clientId,
                payload: clientJson
            )

            sendMessage(message)
        }
    }

    func sendChatMessage(to receiverId: String?, content: String) {
        let chatMessage = ChatMessage(
            senderId: clientId,
            receiverId: receiverId,
            content: content
        )

        let encoder = JSONEncoder()
        if let chatData = try? encoder.encode(chatMessage),
           let chatJson = String(data: chatData, encoding: .utf8) {

            let message = Message(
                type: .chatMessage,
                senderId: clientId,
                receiverId: receiverId,
                payload: chatJson
            )

            sendMessage(message)
        }
    }

    private func sendMessage(_ message: Message) {
        let encoder = JSONEncoder()
        if let messageData = try? encoder.encode(message),
           let messageJson = String(data: messageData, encoding: .utf8) {

            let task = URLSessionWebSocketTask.Message.string(messageJson)
            webSocketTask?.send(task) { error in
                if let error = error {
                    print("Error sending message: \(error)")
                }
            }
        }
    }

    private func receiveMessage() {
        webSocketTask?.receive { [weak self] result in
            switch result {
            case .success(let message):
                switch message {
                case .string(let text):
                    self?.handleReceivedMessage(text)
                case .data(let data):
                    if let text = String(data: data, encoding: .utf8) {
                        self?.handleReceivedMessage(text)
                    }
                @unknown default:
                    break
                }

                // Continue receiving messages
                self?.receiveMessage()

            case .failure(let error):
                print("Error receiving message: \(error)")
            }
        }
    }

    private func handleReceivedMessage(_ messageJson: String) {
        let decoder = JSONDecoder()
        if let data = messageJson.data(using: .utf8),
           let message = try? decoder.decode(Message.self, from: data) {

            DispatchQueue.main.async {
                switch message.type {
                case .authenticationResponse:
                    self.statusMessage = "Connected"
                    self.messages.append("Authentication successful!")

                case .chatMessage:
                    if let chatData = message.payload.data(using: .utf8),
                       let chatMessage = try? decoder.decode(ChatMessage.self, from: chatData) {
                        self.messages.append("[\(message.senderId)]: \(chatMessage.content)")
                    }

                case .commandConfirmationRequest:
                    self.messages.append("Command confirmation requested: \(message.payload)")

                case .error:
                    self.messages.append("Error: \(message.payload)")

                default:
                    self.messages.append("Received: \(message.type)")
                }
            }
        }
    }

    private func generatePublicKey() -> String {
        return UUID().uuidString.data(using: .utf8)?.base64EncodedString() ?? ""
    }

    // URLSessionWebSocketDelegate methods
    func urlSession(_ session: URLSession, webSocketTask: URLSessionWebSocketTask, didOpenWithProtocol protocol: String?) {
        DispatchQueue.main.async {
            self.isConnected = true
            self.statusMessage = "Connected"
        }
    }

    func urlSession(_ session: URLSession, webSocketTask: URLSessionWebSocketTask, didCloseWith closeCode: URLSessionWebSocketTask.CloseCode, reason: Data?) {
        DispatchQueue.main.async {
            self.isConnected = false
            self.statusMessage = "Disconnected"
        }
    }
}
