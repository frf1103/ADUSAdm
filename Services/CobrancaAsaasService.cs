using ADUSClient.Assinatura;
using ADUSClient.Controller;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ADUSAdm.Services
{
    public class CobrancaAsaasService
    {
        private readonly ParcelaControllerClient _parcela;
        private readonly CheckoutService _checkoutService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<ImportProgressHub> _hubContext;
        private readonly ComumService _comum;

        public CobrancaAsaasService(ParcelaControllerClient parcela, IHttpContextAccessor httpContextAccessor, CheckoutService checkoutService, IHubContext<ImportProgressHub> hubContext, ComumService comum)
        {
            _parcela = parcela;

            _httpContextAccessor = httpContextAccessor;
            _checkoutService = checkoutService;
            _hubContext = hubContext;
            _comum = comum;
        }

        public async Task EnviarCobrancasPendentesAsync(DateTime ini, DateTime fim, string? idassinatura = " ", int? idforma = 3, string? connectionId = null)
        {
            string remoteIp = _comum.GetClientIp();
            var parcelas = await _parcela.ListaPendentesEnvio(ini, fim, idassinatura, idforma);
            int atual = 1;
            int total = parcelas.Count;
            foreach (var p in parcelas)
            {
                CheckoutViewModel c = new CheckoutViewModel
                {
                    DataVencimento = p.datavencimento,
                    cpfCnpj = p.registro,
                    Bairro = " ",
                    Cep = " ",
                    Ddi = " ",
                    Email = " ",
                    Cvv = " ",
                    Cidade = " ",
                    EmailConfirmacao = " ",
                    Estado = " ",
                    FormaPagamento = p.descforma,
                    idafiliado = p.idafiliado,
                    Logradouro = " ",
                    Nome = " ",
                    Numero = " ",
                    NomeTitular = " ",
                    NumeroCartao = " ",
                    Telefone = " ",
                    QuantidadeArvores = p.quantidadeArvores ?? 0,
                    TokenCartao = p.cctoken
                };
                // remoteIp = "187.100.100.100";

                await _checkoutService.ProcessarCheckout(c, remoteIp, p.idafiliado, p.idassinatura, p.id);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", (int)((atual * 100.0) / total));
                }
            }
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", 100);
            }
        }
    }
}