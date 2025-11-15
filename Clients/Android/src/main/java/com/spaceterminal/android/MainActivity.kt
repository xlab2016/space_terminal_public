package com.spaceterminal.android

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import okhttp3.*
import com.google.gson.Gson
import java.util.*

class MainActivity : AppCompatActivity() {
    private lateinit var webSocketClient: WebSocketClient
    private val serverUrl = "ws://10.0.2.2:5000/ws" // Android emulator localhost

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        webSocketClient = WebSocketClient(serverUrl)
        webSocketClient.connect()
    }

    override fun onDestroy() {
        super.onDestroy()
        webSocketClient.disconnect()
    }
}

class WebSocketClient(private val serverUrl: String) {
    private val client = OkHttpClient()
    private var webSocket: WebSocket? = null
    private val gson = Gson()
    private val clientId = UUID.randomUUID().toString()

    fun connect() {
        val request = Request.Builder()
            .url(serverUrl)
            .build()

        webSocket = client.newWebSocket(request, object : WebSocketListener() {
            override fun onOpen(webSocket: WebSocket, response: Response) {
                println("Connected to server")
                authenticate()
            }

            override fun onMessage(webSocket: WebSocket, text: String) {
                handleMessage(text)
            }

            override fun onClosing(webSocket: WebSocket, code: Int, reason: String) {
                println("Connection closing: $reason")
            }

            override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
                println("Connection failed: ${t.message}")
            }
        })
    }

    fun disconnect() {
        webSocket?.close(1000, "Client disconnecting")
    }

    private fun authenticate() {
        val client = Client(
            id = clientId,
            name = "Android-Client",
            publicKey = generatePublicKey(),
            type = ClientType.Android
        )

        val message = Message(
            type = MessageType.Authentication,
            senderId = clientId,
            payload = gson.toJson(client)
        )

        sendMessage(message)
    }

    private fun handleMessage(messageJson: String) {
        try {
            val message = gson.fromJson(messageJson, Message::class.java)

            when (message.type) {
                MessageType.AuthenticationResponse -> {
                    println("Authentication successful!")
                }
                MessageType.CommandConfirmationRequest -> {
                    handleCommandConfirmation(message)
                }
                MessageType.ChatMessage -> {
                    println("Chat message: ${message.payload}")
                }
                MessageType.DesktopStreamStart -> {
                    println("Desktop sharing started")
                }
                MessageType.Error -> {
                    println("Error: ${message.payload}")
                }
                else -> {
                    println("Unhandled message type: ${message.type}")
                }
            }
        } catch (e: Exception) {
            println("Error handling message: ${e.message}")
        }
    }

    private fun handleCommandConfirmation(message: Message) {
        // In a real app, show a dialog to user for confirmation
        println("Command confirmation request: ${message.payload}")

        // Auto-approve for demo (should be user interaction in production)
        val confirmation = mapOf(
            "commandId" to message.id,
            "approved" to true
        )

        val confirmationMessage = Message(
            type = MessageType.CommandConfirmation,
            senderId = clientId,
            receiverId = message.senderId,
            payload = gson.toJson(confirmation)
        )

        sendMessage(confirmationMessage)
    }

    fun sendChatMessage(receiverId: String?, content: String) {
        val chatMessage = ChatMessage(
            senderId = clientId,
            receiverId = receiverId,
            content = content
        )

        val message = Message(
            type = MessageType.ChatMessage,
            senderId = clientId,
            receiverId = receiverId,
            payload = gson.toJson(chatMessage)
        )

        sendMessage(message)
    }

    private fun sendMessage(message: Message) {
        val messageJson = gson.toJson(message)
        webSocket?.send(messageJson)
    }

    private fun generatePublicKey(): String {
        return Base64.getEncoder().encodeToString(UUID.randomUUID().toString().toByteArray())
    }
}

// Data models
data class Message(
    val id: String = UUID.randomUUID().toString(),
    val type: MessageType,
    val senderId: String,
    val receiverId: String? = null,
    val groupId: String? = null,
    val payload: String = "",
    val timestamp: String = Date().toString(),
    val isEncrypted: Boolean = true
)

enum class MessageType {
    Authentication,
    AuthenticationResponse,
    Command,
    CommandResponse,
    CommandConfirmationRequest,
    CommandConfirmation,
    DesktopStreamStart,
    DesktopStreamStop,
    DesktopFrame,
    AudioFrame,
    ChatMessage,
    ChatGroupCreate,
    ChatGroupJoin,
    ChatGroupLeave,
    Heartbeat,
    Error
}

enum class ClientType {
    Windows,
    MacOS,
    Android,
    IPhone
}

data class Client(
    val id: String,
    val name: String,
    val publicKey: String,
    val type: ClientType
)

data class ChatMessage(
    val id: String = UUID.randomUUID().toString(),
    val senderId: String,
    val receiverId: String?,
    val content: String,
    val timestamp: String = Date().toString()
)
