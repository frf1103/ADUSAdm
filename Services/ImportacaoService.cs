using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using System.Threading.Tasks;

public class ImportProgressHub : Hub
{
    public async Task SendProgress(string connectionId, int percent)
    {
        await Clients.Client(connectionId).SendAsync("ReceiveProgress", percent);
    }
}

// 2. Serviço que envia atualizações de progresso (exemplo de uso interno)
public class ImportacaoService
{
    private readonly IHubContext<ImportProgressHub> _hubContext;

    public ImportacaoService(IHubContext<ImportProgressHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task ImportarAsync(string connectionId, DateTime inicio, DateTime fim)
    {
        for (int i = 1; i <= 100; i += 10)
        {
            // Simula trabalho
            await Task.Delay(300);

            // Envia progresso ao cliente conectado via SignalR
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", i);
        }
    }
}

// 3. Controller que inicia a importação

/*
[ApiController]
[Route("[controller]/[action]")]
public class ParametrosGuruController : Controller
{
    private readonly ImportacaoService _service;

    public ParametrosGuruController(ImportacaoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Importar(DateTime dataInicio, DateTime dataFim, string connectionId)
    {
        await _service.ImportarAsync(connectionId, dataInicio, dataFim);
        return Ok();
    }
}
*/