import SwiftUI

struct ContentView: View {
    @StateObject private var webSocketClient = WebSocketClient()
    @State private var recipientId = ""
    @State private var messageText = ""

    var body: some View {
        VStack(spacing: 20) {
            Text("Space Terminal - iPhone")
                .font(.title)
                .fontWeight(.bold)

            Text("Status: \(webSocketClient.statusMessage)")
                .foregroundColor(webSocketClient.isConnected ? .green : .red)

            if webSocketClient.isConnected {
                VStack(spacing: 10) {
                    TextField("Recipient ID (optional)", text: $recipientId)
                        .textFieldStyle(RoundedBorderTextFieldStyle())
                        .autocapitalization(.none)

                    TextField("Enter message", text: $messageText)
                        .textFieldStyle(RoundedBorderTextFieldStyle())

                    Button(action: sendMessage) {
                        Text("Send Message")
                            .frame(maxWidth: .infinity)
                            .padding()
                            .background(Color.blue)
                            .foregroundColor(.white)
                            .cornerRadius(8)
                    }
                    .disabled(messageText.isEmpty)
                }
                .padding()
            }

            ScrollView {
                VStack(alignment: .leading, spacing: 8) {
                    ForEach(webSocketClient.messages, id: \.self) { message in
                        Text(message)
                            .padding(8)
                            .background(Color.gray.opacity(0.1))
                            .cornerRadius(4)
                    }
                }
            }
            .frame(maxHeight: .infinity)

            HStack(spacing: 20) {
                Button(action: {
                    webSocketClient.connect()
                }) {
                    Text("Connect")
                        .padding()
                        .background(Color.green)
                        .foregroundColor(.white)
                        .cornerRadius(8)
                }
                .disabled(webSocketClient.isConnected)

                Button(action: {
                    webSocketClient.disconnect()
                }) {
                    Text("Disconnect")
                        .padding()
                        .background(Color.red)
                        .foregroundColor(.white)
                        .cornerRadius(8)
                }
                .disabled(!webSocketClient.isConnected)
            }
        }
        .padding()
        .onAppear {
            webSocketClient.connect()
        }
    }

    private func sendMessage() {
        let recipient = recipientId.isEmpty ? nil : recipientId
        webSocketClient.sendChatMessage(to: recipient, content: messageText)
        messageText = ""
    }
}

struct ContentView_Previews: PreviewProvider {
    static var previews: some View {
        ContentView()
    }
}
