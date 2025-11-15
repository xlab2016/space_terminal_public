using System.Collections.Concurrent;
using SpaceTerminal.Core.Models;
using SpaceTerminal.Core.Services;

namespace SpaceTerminal.Infrastructure.Services;

public class InMemoryClientManager : IClientManager
{
    private readonly ConcurrentDictionary<string, Client> _clients = new();

    public Task<Client?> GetClientAsync(string clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return Task.FromResult(client);
    }

    public Task<IEnumerable<Client>> GetOnlineClientsAsync()
    {
        var onlineClients = _clients.Values.Where(c => c.IsOnline);
        return Task.FromResult(onlineClients);
    }

    public Task RegisterClientAsync(Client client)
    {
        _clients.AddOrUpdate(client.Id, client, (_, _) => client);
        return Task.CompletedTask;
    }

    public Task UpdateClientStatusAsync(string clientId, bool isOnline)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            client.IsOnline = isOnline;
            client.LastSeen = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task UpdateClientSessionAsync(string clientId, string? sessionId)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            client.SessionId = sessionId;
        }
        return Task.CompletedTask;
    }
}
