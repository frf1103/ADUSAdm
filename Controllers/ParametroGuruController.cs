using Microsoft.AspNetCore.Mvc;
using ADUSClient.Controller;

using ADUSClient;
using Microsoft.AspNetCore.SignalR;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Text.Json.Serialization;
using System.Text.Json;
using ADUSClient.Parceiro;
using ADUSClient.Enum;
using ADUSClient.Assinatura;

public class ParametrosGuruController : Controller
{
    private readonly ParametroGuruControllerClient _client;
    private readonly ImportacaoService _service;
    private readonly IHubContext<ImportProgressHub> _hubContext;
    private readonly ParceiroControllerClient _parceiroapi;
    private readonly ParcelaControllerClient _parcela;
    private readonly AssinaturaControllerClient _assinatura;
    private readonly SharedControllerClient _shared;

    public ParametrosGuruController(ParametroGuruControllerClient client, ImportacaoService service, IHubContext<ImportProgressHub> hubContext, ParceiroControllerClient parceiroapi, ParcelaControllerClient parcela, AssinaturaControllerClient assinatura, SharedControllerClient shared)
    {
        _client = client;
        _service = service;
        _hubContext = hubContext;
        _parceiroapi = parceiroapi;
        _parcela = parcela;
        _assinatura = assinatura;
        _shared = shared;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await _client.ListaById(1);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(ParametrosGuruViewModel dados)
    {
        if (!ModelState.IsValid)
        {
            return View(dados);
        }

        var response = await _client.Salvar("1", dados);

        if (response.IsSuccessStatusCode)
        {
            TempData["Mensagem"] = "Parâmetros atualizados com sucesso.";
            return RedirectToAction(nameof(Editar));
        }

        var erro = await response.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, erro);
        return View("index");
    }

    [HttpPost]
    public async Task<IActionResult> Importar(DateTime dataInicio, DateTime dataFim, string connectionId)
    {
        try
        {
            await GetDataGuru(dataInicio, dataFim, connectionId);
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("ReceiveProgress", 100);

            return Ok();
        }
        catch (Exception ex)
        {
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("ReceiveError", "Erro: " + ex.Message);

            return StatusCode(500);
        }
    }

    public async Task<JsonResult> GetDataGuru(DateTime ini, DateTime fim, string connectionId)
    {
        var client = new HttpClient();
        bool hasMore = true;
        string? nextCursor = null;

        var c = await _client.ListaById(1);

        //string addressSub = c.urlsub + "?started_at_ini=" + dini.ToString("yyyy-MM-dd") + "&started_at_end=" + dfim.ToString("yyyy-MM-dd");

        string responsebody;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        int linha = 0, linhas = 0;
        while (hasMore)
        {
            var url = string.IsNullOrEmpty(nextCursor) ? c.urltransac : $"{c.urltransac}?cursor={Uri.EscapeDataString(nextCursor)}";

            url = url + (string.IsNullOrEmpty(nextCursor) ? "?" : "&") + "ordered_at_ini=" + ini.ToString("yyyy-MM-dd") + " & ordered_at_end=" + fim.ToString("yyyy-MM-dd");
            //+ "&transaction_status=" + status;
            // Autenticação, se necessário
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.token);

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var jsonDoc = JsonDocument.Parse(content);
            var transactions = new List<RootObject>();
            string logPath = "erros.log";

            if (jsonDoc.RootElement.TryGetProperty("total_rows", out var totrows))
            {
                linhas = totrows.GetInt32();
            }
            foreach (var element in jsonDoc.RootElement.GetProperty("data").EnumerateArray())
            {
                try
                {
                    var itemJson = element.GetRawText();
                    var transacao = JsonSerializer.Deserialize<RootObject>(itemJson);
                    if (transacao != null)
                        transactions.Add(transacao);
                }
                catch (Exception ex)
                {
                    var erro = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Erro ao desserializar item: {ex.Message}{Environment.NewLine}";
                    continue;
                    //File.AppendAllText(logPath, erro);
                }
            }

            // var result = JsonSerializer.Deserialize<Transactions>(content, options);

            foreach (var sub in transactions)
            {
                linha = linha + 1;
                if (connectionId != null)
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", (int)((linha * 100.0) / linhas));
                }
                if (sub.status == "approved")
                {
                    // Converter UnixTime
                    DateTime createdAt = DateTimeOffset.FromUnixTimeSeconds(sub.dates.created_at).UtcDateTime;
                    string doc = sub.contact.doc;
                    int tipopessoa = 0;
                    if (doc.Trim().Length > 11)
                    {
                        tipopessoa = 1;
                    }

                    Task<ADUSClient.Parceiro.ParceiroViewModel> retp = _parceiroapi.ListaById(sub.contact.id);
                    ADUSClient.Parceiro.ParceiroViewModel t = await retp;

                    if (t == null)
                    {
                        Task<ADUSClient.Parceiro.ViaCep> retc = _parceiroapi.BuscarEnderecoAsync(sub.contact.address_zip_code);
                        ADUSClient.Parceiro.ViaCep viacep = await retc;

                        Task<ADUSClient.Localidade.UFViewModel> retuf = _shared.ListaUFIBGE(viacep.Ibge.Substring(0, 2));
                        ADUSClient.Localidade.UFViewModel uf = await retuf;

                        Task<ADUSClient.Localidade.MunicipioViewModel> retcid = _shared.ListaCidadeIBGE(viacep.Ibge);
                        ADUSClient.Localidade.MunicipioViewModel cid = await retcid;

                        _parceiroapi.Adicionar(new ParceiroViewModel
                        {
                            registro = doc,
                            tipodePessoa = (TipodePessoa)tipopessoa,
                            dtNascimento = createdAt,
                            estadoCivil = 0,
                            fantasia = sub.contact.name,
                            razaoSocial = sub.contact.name,
                            cep = sub.contact.address_zip_code,
                            logradouro = sub.contact.address,
                            bairro = sub.contact.address_district,
                            fone1 = sub.contact.phone_local_code.Trim() + sub.contact.phone_number,
                            numero = sub.contact.address_number,
                            complemento = sub.contact.address_comp,
                            email = sub.contact.email,
                            id = sub.contact.id,
                            uf = uf.id,
                            cidade = cid.id,
                            profissao = "A DEFINIR"
                        });
                    };

                    // Verificar se já existe
                    Task<ADUSClient.Assinatura.AssinaturaViewModel> reta = _assinatura.ListaById(sub.subscription.id);
                    ADUSClient.Assinatura.AssinaturaViewModel a = await reta;

                    FormaPagto forma = FormaPagto.Pix;
                    if (sub.payment.method.ToLower() == "credit_card")
                    {
                        forma = FormaPagto.Cartao;
                    }
                    if (sub.payment.method.ToLower() == "billet")
                    {
                        forma = FormaPagto.Boleto;
                    }

                    if (a == null)
                    {
                        _assinatura.Adicionar(new AssinaturaViewModel
                        {
                            id = sub.subscription.internal_id,
                            datavenda = createdAt,
                            idformapagto = forma,
                            idparceiro = sub.contact.id,
                            idplataforma = sub.subscription.id,
                            status = (StatusAssinatura)1,
                            preco = sub.product.unit_value,
                            qtd = sub.product.qty,
                            valor = sub.product.total_value,
                            observacao = sub.product.marketplace_name
                        });
                    }

                    //incluir parcela

                    DateTime dtcr = DateTime.Now;
                    if (sub.dates.OrderedAt != null)
                    {
                        dtcr = DateTimeOffset.FromUnixTimeSeconds(sub.dates.created_at).UtcDateTime;
                    }

                    Task<ADUSClient.Parcela.ParcelaViewModel> retparc = _parcela.ListaByIdCheckout(sub.id);
                    ADUSClient.Parcela.ParcelaViewModel parc = await retparc;
                    if (parc == null)
                    {
                        string nossonumero = null;
                        if (sub.payment.marketplace_name.ToLower() == "pagarme" || sub.payment.marketplace_name.ToLower() == "pagarme2")
                        {
                            nossonumero = sub.payment.acquirer.nsu;
                        }
                        else
                        {
                            nossonumero = sub.payment.marketplace_id;
                        }
                        _parcela.Adicionar(new ADUSClient.Parcela.ParcelaViewModel
                        {
                            id = Guid.NewGuid().ToString("N"),
                            datavencimento = dtcr,
                            idformapagto = forma,
                            numparcela = sub.invoice.cycle,
                            idassinatura = sub.subscription.internal_id,
                            plataforma = sub.payment.marketplace_name,
                            valor = sub.product.total_value,
                            idcheckout = sub.id,
                            nossonumero = nossonumero,
                            descontos = 0,
                            acrescimos = 0,
                            valorliquido = sub.product.total_value,
                            descontoantecipacao = 0,
                            descontoplataforma = 0,
                            comissao = 0,
                            databaixa = (forma == FormaPagto.Cartao) ? dtcr : null
                        }); ;
                    }
                    else
                    {
                        parc.databaixa = (sub.payment.acquirer.code == "0000") ? dtcr : null;
                        _parcela.Salvar(parc.id, parc);
                    }
                }
            }

            if (jsonDoc.RootElement.TryGetProperty("has_more_pages", out var hasMorePagesProperty))
            {
                hasMore = (hasMorePagesProperty.GetInt32() == 1);
            }
            if (hasMore)
            {
                if (jsonDoc.RootElement.TryGetProperty("next_cursor", out var nextC))
                {
                    nextCursor = nextC.GetString();
                }
            }
        }
        _client.Salvar("1", new ADUSClient.ParametrosGuruViewModel
        {
            ultdata = fim.AddDays(1),
        });

        return Json("OK");
    }
}