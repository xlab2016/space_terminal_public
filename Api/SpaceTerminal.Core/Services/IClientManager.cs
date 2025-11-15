using SpaceTerminal.Core.Models;

namespace SpaceTerminal.Core.Services;

public interface IClientManager
{
    Task<Client?> GetClientAsync(string clientId);
    Task<IEnumerable<Client>> GetOnlineClientsAsync();
    Task RegisterClientAsync(Client client);
    Task UpdateClientStatusAsync(string clientId, bool isOnline);
    Task UpdateClientSessionAsync(string clientId, string? sessionId);
}
