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
using RestSharp;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using static RootObject;
using Newtonsoft.Json;
using ADUSClient.MovimentoCaixa;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Net.WebRequestMethods;
using ADUSClient.Parcela;
using System.Drawing;
using Humanizer;
using MathNet.Numerics;

public class ParametrosGuruController : Controller
{
    private readonly ParametroGuruControllerClient _client;
    private readonly ImportacaoService _service;
    private readonly IHubContext<ImportProgressHub> _hubContext;
    private readonly ParceiroControllerClient _parceiroapi;
    private readonly ParcelaControllerClient _parcela;
    private readonly AssinaturaControllerClient _assinatura;
    private readonly SharedControllerClient _shared;
    private readonly TransacaoControllerClient _transacaoAPI;
    private readonly CentroCustoControllerClient _centroCustoAPI;
    private readonly PlanoContaControllerClient _categoriaAPI;
    private readonly ContaCorrenteControllerClient _contaCorrenteAPI;
    private readonly MovimentoCaixaControllerClient _movcaixaAPI;

    public ParametrosGuruController(ParametroGuruControllerClient client, ImportacaoService service, IHubContext<ImportProgressHub> hubContext, ParceiroControllerClient parceiroapi, ParcelaControllerClient parcela,
        AssinaturaControllerClient assinatura, SharedControllerClient shared, TransacaoControllerClient transacaoAPI,
        CentroCustoControllerClient centroCustoAPI, PlanoContaControllerClient categoriaAPI,
        ContaCorrenteControllerClient contaCorrenteAPI, MovimentoCaixaControllerClient movcaixaAPI)
    {
        _client = client;
        _service = service;
        _hubContext = hubContext;
        _parceiroapi = parceiroapi;
        _parcela = parcela;
        _assinatura = assinatura;
        _shared = shared;
        _transacaoAPI = transacaoAPI;
        _centroCustoAPI = centroCustoAPI;
        _categoriaAPI = categoriaAPI;

        _contaCorrenteAPI = contaCorrenteAPI;
        _movcaixaAPI = movcaixaAPI;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int idplataforma = 1)
    {
        var model = await _client.ListaById(idplataforma);
        ViewBag.idplataforma = idplataforma;

        var transacoes = await _transacaoAPI.Listar("");
        ViewBag.Transacoes = new SelectList(transacoes, "Id", "Descricao");

        var centros = await _centroCustoAPI.Listar("");
        ViewBag.CentroCustos = new SelectList(centros, "Id", "Descricao");

        var categorias = await _categoriaAPI.ListarAsync("");
        ViewBag.Categorias = new SelectList(categorias, "Id", "Descricao");

        var parceiros = await _parceiroapi.Lista("");
        ViewBag.Parceiros = new SelectList(parceiros, "id", "razaoSocial");

        var contas = await _contaCorrenteAPI.Listar("", null);
        ViewBag.Contas = new SelectList(contas, "id", "descricao");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(ParametrosGuruViewModel dados)
    {
        /*
        if (!ModelState.IsValid)
        {
            return View(dados);
        }
        */
        if (dados.urlsub == null)
            dados.urlsub = " ";
        var response = await _client.Salvar(dados.id.ToString(), dados);

        if (response.IsSuccessStatusCode)
        {
            TempData["Mensagem"] = "Parâmetros atualizados com sucesso.";
            return RedirectToAction("index", new { id = dados.id });
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
                    var transacao = System.Text.Json.JsonSerializer.Deserialize<RootObject>(itemJson);
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
            //   transactions = transactions.OrderBy(t => t.subscription.started_at).ToList();
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
                    DateTime createdAt = DateTimeOffset.FromUnixTimeSeconds(sub.subscription.started_at).UtcDateTime;
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

                        await _parceiroapi.Adicionar(new ParceiroViewModel
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
                            iduf = uf.id,
                            idcidade = cid.id,
                            profissao = "A DEFINIR"
                        });
                    };

                    // Verificar se já existe
                    Task<ADUSClient.Assinatura.AssinaturaViewModel> reta = _assinatura.ListaById(sub.subscription.internal_id);
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
                        await _assinatura.Adicionar(new AssinaturaViewModel
                        {
                            id = sub.subscription.internal_id,
                            datavenda = createdAt,
                            idformapagto = forma,
                            idparceiro = sub.contact.id,
                            idplataforma = sub.subscription.internal_id,
                            status = (StatusAssinatura)1,
                            preco = sub.product.unit_value,
                            qtd = sub.product.qty,
                            valor = sub.product.total_value,
                            observacao = sub.product.marketplace_name,
                            plataforma = sub.product.marketplace_name
                        });
                        DateTime dvcto = createdAt;
                        for (var i = 1; i <= 84; i++)
                        {
                            await _parcela.Adicionar(new ParcelaViewModel
                            {
                                id = Guid.NewGuid().ToString("N"),
                                datavencimento = dvcto,
                                idformapagto = forma,
                                numparcela = i,
                                idassinatura = sub.subscription.internal_id,
                                plataforma = sub.payment.marketplace_name,
                                valor = (decimal)sub.product.total_value,
                                idcheckout = " ",
                                descontos = 0,
                                acrescimos = 0,
                                valorliquido = (decimal)sub.product.total_value,
                                descontoantecipacao = 0,
                                descontoplataforma = 0,
                                comissao = (decimal)sub.product.total_value * 10 / 100
                            });
                            dvcto = dvcto.AddDays(30);
                        }

                        // a = await _assinatura.ListaById(sub.subscription.internal_id);
                    }
                    else
                    {
                        a.datavenda = createdAt;
                        _assinatura.Salvar(a.id, a);
                    }

                    //incluir parcela

                    DateTime dtcr = DateTime.Now;
                    string nossonumero = null;
                    nossonumero = sub.payment.marketplace_id;

                    if (sub.dates.OrderedAt != null)
                    {
                        dtcr = DateTimeOffset.FromUnixTimeSeconds(sub.dates.created_at).UtcDateTime;
                    }

                    ParcelaViewModel parc = await _parcela.ListaByIdCheckout(nossonumero);
                    if (parc == null)
                    {
                        parc = await _parcela.ListaByAssinatura(sub.subscription.internal_id, sub.invoice.cycle);

                        if (parc == null)
                        {
                            await _parcela.Adicionar(new ADUSClient.Parcela.ParcelaViewModel
                            {
                                id = Guid.NewGuid().ToString("N"),
                                datavencimento = dtcr,
                                idformapagto = forma,
                                numparcela = sub.invoice.cycle,
                                idassinatura = sub.subscription.internal_id,
                                plataforma = sub.payment.marketplace_name,
                                valor = (decimal)sub.product.total_value,
                                idcheckout = sub.id,
                                nossonumero = nossonumero,
                                descontos = 0,
                                acrescimos = 0,
                                valorliquido = (decimal)sub.product.total_value,
                                descontoantecipacao = 0,
                                descontoplataforma = 0,
                                comissao = 0,
                                databaixa = (forma == FormaPagto.Cartao) ? dtcr : null
                            });
                        }
                        else
                        {
                            parc.datavencimento = dtcr;
                            parc.idformapagto = forma;

                            parc.idassinatura = sub.subscription.internal_id;
                            parc.plataforma = sub.payment.marketplace_name;
                            parc.valor = (decimal)sub.product.total_value;
                            parc.idcheckout = sub.id;
                            parc.nossonumero = nossonumero;
                            parc.valorliquido = (decimal)sub.product.total_value;
                            parc.databaixa = (forma == FormaPagto.Cartao) ? dtcr : null;
                            parc.dataestimadapagto = await BuscarDataEstimada(sub.payment.marketplace_name, nossonumero);
                            await _parcela.Salvar(parc.id, parc);
                        }
                    }
                    else
                    {
                        parc.databaixa = (forma == FormaPagto.Cartao) ? dtcr : null;
                        parc.dataestimadapagto = await BuscarDataEstimada(sub.payment.marketplace_name, nossonumero);
                        await _parcela.Salvar(parc.id, parc);
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

    public async Task<JsonResult> GetBaixasAsaasByData(string ini, string fim, string connectionId)
    {
        var model = await _client.ListaById(2);

        var secretKey = model.token;

        var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

        var allPayables = new List<string>(); // ou seu model
        string cursor = null;
        bool hasMore = true;
        int linhas = 0, linha = 0;
        int offset = 0;
        int limit = 50;

        while (hasMore)
        {
            string url = model.urltransac;

            url += "?dueDate[ge]=" + ini + "&dueDate[le]=" + fim;
            url = url + "&status=RECEIVED&limit=" + limit.ToString() +
                "&offset=" + offset.ToString();
            var options = new RestClientOptions(url);
            var client = new RestClient(options);

            var request = new RestRequest();
            request.AddHeader("access_token", secretKey);

            request.AddHeader("accept", "application/json");

            var response = await client.ExecuteGetAsync(request);

            if (!response.IsSuccessful)
            {
                Console.WriteLine("Erro: " + response.ErrorMessage);
                break;
            }

            // Adicione sua lógica de deserialização aqui
            allPayables.Add(response.Content); // ou deserialize

            var json = response.Content; // string com JSON recebido do Pagar.me

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            linhas = root.GetProperty("totalCount").GetInt32();

            if (root.TryGetProperty("data", out var dataArray))
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    var id = item.GetProperty("id").GetString();
                    var parc = await _parcela.ListaByIdCheckout(id);
                    if (parc != null)
                    {
                        if (parc.idcaixa == 0 || parc.idcaixa == null)
                        {
                            var sobs = item.GetProperty("description").GetString();
                            if (item.GetProperty("pixTransaction").GetString() != null)
                            {
                                sobs = sobs + " " + "PIX TRANSACTION: " + item.GetProperty("pixTransaction").GetString() + " " +
                                    "PIXQRCODE: " + item.GetProperty("pixQrCodeId").GetString();
                            }

                            if (item.TryGetProperty("split", out var comissao))
                            {
                                parc.comissao = item.GetProperty("split")[0].GetProperty("totalValue").GetDecimal();
                            }
                            else parc.comissao = 0;

                            var respc = await _movcaixaAPI.AdicionarAsync(new ADUSClient.MovimentoCaixa.MovimentoCaixaViewModel
                            {
                                DataMov = item.GetProperty("creditDate").GetDateTime().Date,
                                IdCategoria = (int)model.idcategoria,
                                IdCentroCusto = (int)model.idccusto,
                                IdContaCorrente = model.idconta,
                                IdTransacao = (int)model.idtransacao,
                                idparceiro = parc.idparceiro,
                                Valor = item.GetProperty("netValue").GetDecimal() - parc.comissao,
                                Sinal = "C",
                                Observacao = sobs
                            });
                            string y = await respc.Content.ReadAsStringAsync();
                            var result = JsonConvert.DeserializeObject<MovimentoCaixaViewModel>(y);
                            if (item.GetProperty("billingType").GetString() != "CREDIT_CARD")
                            {
                                parc.databaixa = item.GetProperty("paymentDate").GetDateTime().Date;
                            }
                            parc.idcaixa = result.Id;
                            parc.descontoplataforma = parc.valor - item.GetProperty("netValue").GetDecimal();
                            parc.valorliquido = item.GetProperty("netValue").GetDecimal() - parc.comissao;
                            parc.dataestimadapagto = item.GetProperty("estimatedCreditDate").GetDateTime().Date;
                            await _parcela.Salvar(parc.id, parc);
                        }
                    }
                    else
                    {
                        if (connectionId != null)
                        {
                            var erro = "Registro de cobrança: " + id + "-" + item.GetProperty("billingType").GetString() + "-" + item.GetProperty("paymentDate").GetDateTime().ToString() + " " + item.GetProperty("netValue").GetDecimal().ToString();
                            await _hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveLog", $"Erro ao importar registro X: {erro}");
                        }
                    }

                    //var acquirerNSU = item.GetProperty("gateway_id").GetString();
                    linha++;
                    if (connectionId != null)
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", (int)((linha * 100.0) / linhas));
                    }
                }
            }
            hasMore = root.GetProperty("hasMore").GetBoolean();
            offset += limit;
        }
        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", 100);
        return Json("OK");
    }

    public async Task<JsonResult> GetBaixasPagarmeByData(string ini, string fim, string connectionId)
    {
        var model = await _client.ListaById(3);

        var secretKey = model.token;

        var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

        var allPayables = new List<string>(); // ou seu model
        string cursor = null;
        bool hasMore = true;
        int pagina = 1;

        while (hasMore)
        {
            string url = model.urltransac;

            if (cursor == null)
                url += "?payment_date_since=" + ini + "&payment_date_until=" + fim;
            else
                url = cursor;
            url = url + "&size=1000&page=" + pagina.ToString();
            //  url = "https://api.pagar.me/core/v5/charges?created_since=" + ini + "&created_until=" + fim;
            var options = new RestClientOptions(url);
            var client = new RestClient(options);

            var request = new RestRequest();
            request.AddHeader("Authorization", $"Basic {base64Auth}");
            request.AddHeader("accept", "application/json");

            var response = await client.ExecuteGetAsync(request);

            if (!response.IsSuccessful)
            {
                Console.WriteLine("Erro: " + response.ErrorMessage);
                break;
            }

            // Adicione sua lógica de deserialização aqui
            allPayables.Add(response.Content); // ou deserialize

            var json = response.Content; // string com JSON recebido do Pagar.me
            int linhas = 0; int linha = 0;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataArray))
            {
                linhas = dataArray.GetArrayLength();

                foreach (var item in dataArray.EnumerateArray())
                {
                    var id = item.GetProperty("charge_id").GetString();
                    var parc = await _parcela.ListaByIdCheckout(id);
                    if (parc != null)
                    {
                        if (parc.idcaixa == 0 || parc.idcaixa == null)
                        {
                            decimal comissao, valor, adiant, taxa;
                            valor = 0;
                            adiant = 0;
                            taxa = 0;
                            comissao = 0;
                            if (item.GetProperty("recipient_id").GetString() == "re_cm4dczn1igtsv0l9tj0rmd5gt")
                            {
                                comissao = item.GetProperty("amount").GetDecimal() / 100;
                            }
                            else
                            {
                                valor = item.GetProperty("amount").GetDecimal() / 100;
                                adiant = item.GetProperty("anticipation_fee").GetDecimal() / 100;
                                taxa = item.GetProperty("fee").GetDecimal() / 100;
                            }
                            if (valor != 0)
                            {
                                var respc = await _movcaixaAPI.AdicionarAsync(new ADUSClient.MovimentoCaixa.MovimentoCaixaViewModel
                                {
                                    DataMov = item.GetProperty("payment_date").GetDateTime().Date,
                                    IdCategoria = (int)model.idcategoria,
                                    IdCentroCusto = (int)model.idccusto,
                                    IdContaCorrente = model.idconta,
                                    IdTransacao = (int)model.idtransacao,
                                    idparceiro = parc.idparceiro,
                                    Valor = (decimal)(valor - adiant - taxa),
                                    Sinal = "C"
                                });
                                if (comissao == 0)
                                {
                                    string urlc = model.urltransac + "?charge_id=" + id + "&recipient_id=re_cm4dczn1igtsv0l9tj0rmd5gt";
                                    var optionsc = new RestClientOptions(urlc);
                                    var clientc = new RestClient(optionsc);

                                    var requestc = new RestRequest();
                                    requestc.AddHeader("Authorization", $"Basic {base64Auth}");
                                    requestc.AddHeader("accept", "application/json");

                                    var responsec = await clientc.ExecuteGetAsync(requestc);

                                    if (responsec.IsSuccessful)
                                    {
                                        var jsonc = responsec.Content;
                                        using var docc = JsonDocument.Parse(jsonc);
                                        comissao = docc.RootElement.GetProperty("data")[0].GetProperty("amount").GetDecimal() / 100;
                                    }
                                }
                                string y = await respc.Content.ReadAsStringAsync();
                                var result = JsonConvert.DeserializeObject<MovimentoCaixaViewModel>(y);

                                //payment_method: "credit_card"
                                if (item.GetProperty("payment_method").GetString() != "credit_card")
                                {
                                    parc.databaixa = item.GetProperty("payment_date").GetDateTime().Date;
                                }
                                parc.idcaixa = result.Id;
                                parc.descontoplataforma = (decimal)taxa;
                                parc.comissao = (decimal)comissao;
                                parc.descontoantecipacao = (decimal)adiant;
                                parc.dataestimadapagto = item.GetProperty("payment_date").GetDateTime().Date;
                                parc.valorliquido = (decimal)(valor - adiant - taxa);
                                await _parcela.Salvar(parc.id, parc);
                                linha++;
                                if (connectionId != null)
                                {
                                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", (int)((linha * 100.0) / linhas));
                                }
                            }
                        }
                    }
                }                   //     var acquirerNSU = item.GetProperty("gateway_id").GetString();
            }
            // Verifica o header 'X-PagarMe-Cursor'
            if (root.GetProperty("data").GetArrayLength() == 0)
                hasMore = false;
            else
            {
                // Processar dados
                pagina++;
            }            /*
            var options = new RestClientOptions(url);
            var client = new RestClient(options);
            var request = new RestRequest("");

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
            request.AddHeader("accept", "application/json");

            request.AddHeader("Authorization", $"Basic {base64Auth}");
            var response = await client.GetAsync(request);

            //            Console.WriteLine("{0}", response.Content);
            */
        }
        if (connectionId != null)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", 100);
        }
        return Json("OK");
    }

    public async Task<DateTime>? BuscarDataEstimada(string plataforma, string id)
    {
        if (plataforma == "pagarme2")
        {
            var model = await _client.ListaById(3);

            var secretKey = model.token;

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

            var allPayables = new List<string>(); // ou seu model
            string cursor = null;
            bool hasMore = true;

            string url = model.urltransac + "?charge_id=" + id;

            //  url = "https://api.pagar.me/core/v5/charges?created_since=" + ini + "&created_until=" + fim;
            var options = new RestClientOptions(url);
            var client = new RestClient(options);

            var request = new RestRequest();
            request.AddHeader("Authorization", $"Basic {base64Auth}");
            request.AddHeader("accept", "application/json");

            var response = await client.ExecuteGetAsync(request);

            if (!response.IsSuccessful)
            {
                Console.WriteLine("Erro: " + response.ErrorMessage);
                return DateTime.Now.Date;
            }

            // Adicione sua lógica de deserialização aqui
            allPayables.Add(response.Content); // ou deserialize

            var json = response.Content; // string com JSON recebido do Pagar.me

            return JsonDocument.Parse(json).RootElement.GetProperty("data")[0].GetProperty("payment_date").GetDateTime().Date;
        }
        else
        {
            var model = await _client.ListaById(2);

            var secretKey = model.token;

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

            var allPayables = new List<string>(); // ou seu model
            string cursor = null;
            bool hasMore = true;
            int linhas = 0, linha = 0;
            int offset = 0;
            int limit = 50;

            string url = model.urltransac;

            url += "/" + id;
            var options = new RestClientOptions(url);
            var client = new RestClient(options);

            var request = new RestRequest();
            request.AddHeader("access_token", secretKey);

            request.AddHeader("accept", "application/json");

            var response = await client.ExecuteGetAsync(request);

            if (!response.IsSuccessful)
            {
                Console.WriteLine("Erro: " + response.ErrorMessage);
                return DateTime.Now.Date;
            }

            // Adicione sua lógica de deserialização aqui
            allPayables.Add(response.Content); // ou deserialize

            var json = response.Content; // string com JSON recebido do Pagar.me

            return JsonDocument.Parse(json).RootElement.GetProperty("estimatedCreditDate").GetDateTime().Date;
        }
    }
}