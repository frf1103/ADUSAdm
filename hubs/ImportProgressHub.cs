using Microsoft.AspNetCore.SignalR;

namespace ADUSAdm.Hubs
{
    public class ImportProgressHub : Hub
    {
        // Retorna o connectionId para o cliente
        public Task<string> GetConnectionId()
        {
            return Task.FromResult(Context.ConnectionId);
        }
    }
}