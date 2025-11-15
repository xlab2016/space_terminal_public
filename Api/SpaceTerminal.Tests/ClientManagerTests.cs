using SpaceTerminal.Core.Models;
using SpaceTerminal.Infrastructure.Services;
using Xunit;

namespace SpaceTerminal.Tests;

public class ClientManagerTests
{
    private readonly InMemoryClientManager _clientManager;

    public ClientManagerTests()
    {
        _clientManager = new InMemoryClientManager();
    }

    [Fact]
    public async Task RegisterClient_ShouldAddClient()
    {
        // Arrange
        var client = new Client
        {
            Id = "test-client-1",
            Name = "Test Client",
            Type = ClientType.Windows
        };

        // Act
        await _clientManager.RegisterClientAsync(client);
        var retrievedClient = await _clientManager.GetClientAsync(client.Id);

        // Assert
        Assert.NotNull(retrievedClient);
        Assert.Equal(client.Id, retrievedClient.Id);
        Assert.Equal(client.Name, retrievedClient.Name);
    }

    [Fact]
    public async Task UpdateClientStatus_ShouldUpdateStatus()
    {
        // Arrange
        var client = new Client
        {
            Id = "test-client-2",
            Name = "Test Client",
            IsOnline = false
        };
        await _clientManager.RegisterClientAsync(client);

        // Act
        await _clientManager.UpdateClientStatusAsync(client.Id, true);
        var retrievedClient = await _clientManager.GetClientAsync(client.Id);

        // Assert
        Assert.NotNull(retrievedClient);
        Assert.True(retrievedClient.IsOnline);
    }

    [Fact]
    public async Task GetOnlineClients_ShouldReturnOnlyOnlineClients()
    {
        // Arrange
        var client1 = new Client { Id = "client-1", Name = "Client 1", IsOnline = true };
        var client2 = new Client { Id = "client-2", Name = "Client 2", IsOnline = false };
        var client3 = new Client { Id = "client-3", Name = "Client 3", IsOnline = true };

        await _clientManager.RegisterClientAsync(client1);
        await _clientManager.RegisterClientAsync(client2);
        await _clientManager.RegisterClientAsync(client3);

        // Act
        var onlineClients = await _clientManager.GetOnlineClientsAsync();

        // Assert
        Assert.Equal(2, onlineClients.Count());
        Assert.Contains(onlineClients, c => c.Id == "client-1");
        Assert.Contains(onlineClients, c => c.Id == "client-3");
    }

    [Fact]
    public async Task UpdateClientSession_ShouldUpdateSessionId()
    {
        // Arrange
        var client = new Client { Id = "test-client-4", Name = "Test Client" };
        await _clientManager.RegisterClientAsync(client);

        // Act
        await _clientManager.UpdateClientSessionAsync(client.Id, "session-123");
        var retrievedClient = await _clientManager.GetClientAsync(client.Id);

        // Assert
        Assert.NotNull(retrievedClient);
        Assert.Equal("session-123", retrievedClient.SessionId);
    }
}
