using ADUSAdm.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ADUSAdm.Services
{
    public class AsaasSyncService : BackgroundService
    {
        private readonly ILogger<AsaasSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ASAASSettings _asaas;

        public AsaasSyncService(ILogger<AsaasSyncService> logger, IServiceProvider serviceProvider, IOptions<ASAASSettings> asaasSettings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _asaas = asaasSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_asaas.sincronize == "1")
            {
                _logger.LogInformation("AsaasSyncService iniciado.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var meuServico = scope.ServiceProvider
                                .GetRequiredService<CobrancaAsaasService>();

                            //await meuServico.EnviarCobrancasPendentesAsync(DateTime.Now.Date, DateTime.Now.Date);
                            await meuServico.EnviarCobrancasPendentesAsync(DateTime.Now.AddDays(-15).Date, DateTime.Now.AddDays(0).Date, null, 3, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro no serviço AsaasSyncService.");
                    }
                    int minutos = int.Parse(_asaas.timesinc);
                    await Task.Delay(TimeSpan.FromMinutes(minutos), stoppingToken);
                }

                _logger.LogInformation("AsaasSyncService finalizado.");
            }
        }
    }
}