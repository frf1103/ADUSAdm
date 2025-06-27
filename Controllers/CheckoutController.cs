// Controller refatorado com boas práticas e validações
using ADUSAdm.Data;
using ADUSClient.Assinatura;
using ADUSClient.Controller;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FBSLIb;
using ADUSClient.Enum;
using ADUSClient.Parceiro;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using ADUSClient.Parcela;
using Newtonsoft.Json.Linq;
using ADUSAdm.Shared;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using static RootObject;
using ADUSClient;

[RequireHttps]
public class CheckoutController : Controller
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CheckoutController> _logger;
    private readonly ILogService _logService;
    private readonly ADUScontext _context;
    private readonly ParametroGuruControllerClient _parametros;
    private readonly CartaoAssinaturaControllerClient _cartoes;
    private readonly ParceiroControllerClient _parceiro;
    private readonly SharedControllerClient _shared;
    private readonly AssinaturaControllerClient _assinatura;
    private readonly ParcelaControllerClient _parcela;
    private readonly ASAASSettings _AsaasSettings;
    private readonly LogCheckoutControllerClient _log;

    public CheckoutController(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<CheckoutController> logger, ILogService logService, ADUScontext context, ParametroGuruControllerClient parametros, CartaoAssinaturaControllerClient cartoes, ParceiroControllerClient parceiro, SharedControllerClient shared, AssinaturaControllerClient assinatura, ParcelaControllerClient parcela, IOptions<ASAASSettings> asaasSettings, LogCheckoutControllerClient log)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _logService = logService;
        _context = context;
        _parametros = parametros;
        _cartoes = cartoes;
        _parceiro = parceiro;
        _shared = shared;
        _assinatura = assinatura;
        _parcela = parcela;
        _AsaasSettings = asaasSettings.Value;
        _log = log;
    }

    [HttpGet]
    public IActionResult Index(string nome, string fone, string email)
    {
        if (!Request.Cookies.ContainsKey("session_id"))
        {
            var sessionId = Guid.NewGuid().ToString();
            Response.Cookies.Append("session_id", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }

        var userAgent = Request.Headers["User-Agent"].ToString().ToLower();
        if (userAgent.Contains("iphone") || userAgent.Contains("android") || userAgent.Contains("mobile"))
        {
            return RedirectToAction("Mobile", new { nome, fone, email });
        }
        ViewBag.nome = nome;
        ViewBag.fone = fone;
        ViewBag.email = email;
        return View();
    }

    [HttpGet]
    public IActionResult Mobile(string nome, string fone, string email)
    {
        ViewBag.nome = nome;
        ViewBag.fone = fone;
        ViewBag.email = email;
        return View("IndexMobile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel model)
    {
        var sessionId = Request.Cookies["session_id"];
        string idAfiliado = Request.Cookies.ContainsKey("idafiliado") ? Request.Cookies["idafiliado"] : null;

        string remoteIp = HttpContext.Request.Headers.ContainsKey("X-Forwarded-For")
            ? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0]
            : HttpContext.Connection.RemoteIpAddress?.ToString();
        remoteIp = "187.100.100.100";

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            await RegistrarLog(model.Nome, remoteIp, "Sessão Inválida", "", "", "", "400", "Sessão expirada");
            ModelState.AddModelError("", "Sessão inválida ou expirada.");
            return View("Falha");
        }

        if (!ModelState.IsValid || model.QuantidadeArvores <= 0)
        {
            await RegistrarLog(model.Nome, remoteIp, "ModelState Inválido", "", "", "", "400", "Validação de formulário falhou");
            return View(model);
        }

        string billingType = model.FormaPagamento.ToUpper() switch
        {
            "PIX" => "PIX",
            "BOLETO" => "BOLETO",
            _ => "CREDIT_CARD"
        };

        if (billingType == "CREDIT_CARD" && !Regex.IsMatch(model.Validade ?? "", @"^(0[1-9]|1[0-2])/\d{4}$"))
        {
            await RegistrarLog(model.Nome, remoteIp, "Validade Cartão Inválida", "", "", "", "400", "Formato da validade inválido");
            ModelState.AddModelError("Validade", "Formato da validade inválido (MM/AAAA).");
            return View(model);
        }

        try
        {
            var client = new HttpClient();

            // ----- 1) Consultar Cliente -----
            var getClientRequest = new HttpRequestMessage(HttpMethod.Get, _AsaasSettings.urlparceiro + "?cpfCnpj=" + FBSLIb.StringLib.Somentenumero(model.cpfCnpj));
            getClientRequest.Headers.Add("accept", "application/json");
            getClientRequest.Headers.Add("access_token", _AsaasSettings.access_token);
            getClientRequest.Headers.Add("User-Agent", _AsaasSettings.useragent);

            await RegistrarLog(model.Nome, remoteIp, "GET Cliente Asaas", getClientRequest.RequestUri.ToString(), "", "", "Iniciando");

            var response = await client.SendAsync(getClientRequest);
            string body = await response.Content.ReadAsStringAsync();
            await RegistrarLog(model.Nome, remoteIp, "GET Cliente Asaas", getClientRequest.RequestUri.ToString(), "", body, response.StatusCode.ToString());

            string idcliente, idcl;

            using (var docx = JsonDocument.Parse(body))
            {
                if (docx.RootElement.GetProperty("data").GetArrayLength() == 0)
                {
                    var clientePayload = new
                    {
                        name = model.Nome,
                        email = model.Email,
                        phone = $"+{model.Ddi}{model.Telefone}",
                        cpfCnpj = model.cpfCnpj
                    };

                    var json = JsonSerializer.Serialize(clientePayload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var postClientRequest = new HttpRequestMessage(HttpMethod.Post, _AsaasSettings.urlparceiro)
                    {
                        Content = content
                    };
                    postClientRequest.Headers.Add("accept", "application/json");
                    postClientRequest.Headers.Add("access_token", _AsaasSettings.access_token);
                    postClientRequest.Headers.Add("User-Agent", _AsaasSettings.useragent);

                    await RegistrarLog(model.Nome, remoteIp, "POST Criação Cliente Asaas", _AsaasSettings.urlparceiro, json, "", "Iniciando");

                    response = await client.SendAsync(postClientRequest);
                    body = await response.Content.ReadAsStringAsync();
                    await RegistrarLog(model.Nome, remoteIp, "POST Criação Cliente Asaas", _AsaasSettings.urlparceiro, json, body, response.StatusCode.ToString());

                    using var doc = JsonDocument.Parse(body);
                    idcliente = doc.RootElement.GetProperty("id").GetString();
                }
                else
                {
                    idcliente = docx.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
                }
            }
            idcl = await CriarOuRecuperarParceiroADUS(model, idcliente);

            // ----- 2) Tokenizar Cartão -----
            string cctoken = null;
            if (billingType == "CREDIT_CARD")
            {
                var tokenPayload = new
                {
                    customer = idcliente,
                    creditCard = new
                    {
                        holderName = model.NomeTitular,
                        number = model.NumeroCartao,
                        expiryMonth = model.Validade.Split('/')[0],
                        expiryYear = model.Validade.Split('/')[1],
                        ccv = model.Cvv
                    },
                    creditCardHolderInfo = new
                    {
                        name = model.Nome,
                        email = model.Email,
                        cpfCnpj = StringLib.Somentenumero(model.cpfCnpj),
                        postalCode = model.Cep,
                        addressNumber = model.Numero,
                        addressComplement = model.Complemento,
                        phone = model.Telefone
                    },
                    remoteIp = remoteIp
                };

                var jsonToken = JsonSerializer.Serialize(tokenPayload);
                var contentToken = new StringContent(jsonToken, Encoding.UTF8, "application/json");

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _AsaasSettings.urltokencartao)
                {
                    Content = contentToken
                };
                tokenRequest.Headers.Add("accept", "application/json");
                tokenRequest.Headers.Add("access_token", _AsaasSettings.access_token);
                tokenRequest.Headers.Add("User-Agent", _AsaasSettings.useragent);

                await RegistrarLog(model.Nome, remoteIp, "POST Tokenizar Cartão", _AsaasSettings.urltokencartao, jsonToken, "", "Iniciando");

                response = await client.SendAsync(tokenRequest);
                string tokenBody = await response.Content.ReadAsStringAsync();

                await RegistrarLog(model.Nome, remoteIp, "POST Tokenizar Cartão", _AsaasSettings.urltokencartao, jsonToken, tokenBody, response.StatusCode.ToString(), (!response.IsSuccessStatusCode ? "Erro ao tokenizar" : null));

                if (!response.IsSuccessStatusCode)
                    return View("Falha");

                using var tokenDoc = JsonDocument.Parse(tokenBody);
                cctoken = tokenDoc.RootElement.GetProperty("creditCardToken").GetString();
            }
            //
            // ----- 3) Criar Assinatura ou Pagamento -----
            string idsub = await CriarAssinaturaOuPagamento(model, idcliente, cctoken, billingType, remoteIp, idAfiliado);

            if (model.FormaPagamento == "Parcelado")
            {
                await AddAssinaturaADUS(model, idsub, idcl, idAfiliado);
            }
            // ----- 4) Obter Payments -----

            await Task.Delay(4000);
            var payments = await ObterPaymentsDaSubscription(idsub, model.FormaPagamento, client);

            if (payments.Count == 0)
            {
                await RegistrarLog(model.Nome, remoteIp, "GET Payments", "GET " + _AsaasSettings.urlpayments, "", "Nenhum payment retornado", "404", "Sem cobrança gerada");
                ViewBag.Mensagem = "Nenhuma cobrança gerada.";
                return View("Falha");
            }

            //await ProcessarParcelas(model, billingType, payments, idcliente, idsub);

            var primeiroPayment = payments.FirstOrDefault();
            if (primeiroPayment != null)
            {
                ViewBag.Link = primeiroPayment.InvoiceUrl;
            }

            return View("Sucesso");
        }
        catch (Exception ex)
        {
            await RegistrarLog(model.Nome, remoteIp, "Erro Geral Checkout", "", "", "", "500", ex.ToString());
            _logger.LogError(ex, "Erro inesperado no checkout");
            ViewBag.Mensagem = "Erro inesperado. Tente novamente mais tarde.";
            return View("Falha");
        }
    }

    public async Task<string> AddAssinaturaADUS(CheckoutViewModel m, string id, string idcliente, string idafiliado)
    {
        FormaPagto idforma = (m.FormaPagamento == "PIX") ? FormaPagto.Pix : (m.FormaPagamento == "BOLETO") ? FormaPagto.Boleto : FormaPagto.Cartao;
        await _assinatura.Adicionar(new AssinaturaViewModel
        {
            id = id,
            datavenda = DateTime.Now.Date,
            idformapagto = idforma,
            idparceiro = idcliente,
            idplataforma = id,
            status = (StatusAssinatura)1,
            preco = 47,
            qtd = m.QuantidadeArvores,
            valor = m.QuantidadeArvores * 47,
            observacao = " ",
            plataforma = "ASAAS",
            idafiliado = idafiliado
        });

        return null;
    }

    private async Task<string> CriarOuRecuperarParceiroADUS(CheckoutViewModel model, string idclienteAsaas)
    {
        var cliente = await _parceiro.Lista(FBSLIb.StringLib.Somentenumero(model.cpfCnpj), true, true, true, true);

        if (cliente.Count > 0)
            return cliente[0].id;

        var uf = await _shared.ListaUFBySigla(model.Estado);
        int icoduf = uf[0].id;
        var cidade = await _shared.ListaCidade(icoduf, model.Cidade);

        string idcl = Guid.NewGuid().ToString();
        var parv = new ParceiroViewModel
        {
            id = idcl,
            idCidade = cidade[0].id,
            fantasia = model.Nome,
            email = model.Email,
            fone1 = model.Telefone,
            estadoCivil = 0,
            dtNascimento = DateTime.Now.Date,
            bairro = model.Bairro,
            logradouro = model.Logradouro,
            numero = model.Numero,
            razaoSocial = model.Nome,
            cep = model.Cep,
            complemento = model.Complemento,
            iduf = uf[0].id,
            isassinante = true,
            sexo = ADUSClient.Enum.TipoSexo.Indiferente,
            tipodePessoa = (model.cpfCnpj.Length > 15) ? ADUSClient.Enum.TipodePessoa.Jurídica : ADUSClient.Enum.TipodePessoa.Física,
            profissao = "INDEFINIDO",
            registro = FBSLIb.StringLib.Somentenumero(model.cpfCnpj)
        };

        await _parceiro.Adicionar(parv);
        return idcl;
    }

    private async Task<string> CriarAssinaturaOuPagamento(CheckoutViewModel model, string idcliente, string cctoken, string billingType, string remoteip, string idafiliado)
    {
        var client = new HttpClient();
        string idsub;

        if (model.FormaPagamento != "Parcelado")
        {
            var subscriptionPayload = new
            {
                customer = idcliente,
                billingType = billingType,
                value = model.ValorTotal,
                nextDueDate = DateTime.Now.Date,
                cycle = "MONTHLY",
                description = $"{model.QuantidadeArvores} ARVORES PROJETO TECA SOCIAL",
                maxPayments = 84,
                creditCardToken = cctoken,
                remoteIp = remoteip,
                externalReference = idafiliado
            };

            var json = JsonSerializer.Serialize(subscriptionPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, _AsaasSettings.urlsubscription)
            {
                Content = content
            };
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("access_token", _AsaasSettings.access_token);
            request.Headers.Add("User-Agent", _AsaasSettings.useragent);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            await RegistrarLog(model.Nome, remoteip, "POST Assinatura", _AsaasSettings.urlsubscription, json, body, response.StatusCode.ToString(), (!response.IsSuccessStatusCode ? "Erro ao assinar" : null));
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Erro ao criar assinatura Asaas: {body}");
                throw new Exception("Falha ao criar assinatura.");
            }

            using var doc = JsonDocument.Parse(body);
            idsub = doc.RootElement.GetProperty("id").GetString();
        }
        else
        {
            idsub = Guid.NewGuid().ToString();
            var paymentPayload = new
            {
                customer = idcliente,
                billingType = billingType,
                value = model.ValorTotal,
                dueDate = DateTime.Now.Date,
                description = $"{model.QuantidadeArvores} ARVORES PROJETO TECA SOCIAL",
                installmentCount = model.Parcelas,
                totalValue = model.ValorTotal,
                creditCardToken = cctoken,
                externalReference = idsub,
                remoteIp = remoteip
            };

            var json = JsonSerializer.Serialize(paymentPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, _AsaasSettings.urlpayments)
            {
                Content = content
            };
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("access_token", _AsaasSettings.access_token);
            request.Headers.Add("User-Agent", _AsaasSettings.useragent);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            await RegistrarLog(model.Nome, remoteip, "POST Cobranca parcelada", _AsaasSettings.urlsubscription, json, body, response.StatusCode.ToString(), (!response.IsSuccessStatusCode ? "Erro na cobranca parcelada" : null));
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Erro ao criar pagamento parcelado Asaas: {body}");
                throw new Exception("Falha ao criar pagamento parcelado.");
            }
        }

        return idsub;
    }

    private async Task<List<PaymentDTO>> ObterPaymentsDaSubscription(string idsub, string billingType, HttpClient client)
    {
        string filtro = (billingType != "Parcelado")
            ? $"subscription={idsub}&limit=21&offset=0"
            : $"externalReference={idsub}&limit=21&offset=0";

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_AsaasSettings.urlpayments}?{filtro}");
        request.Headers.Add("accept", "application/json");
        request.Headers.Add("access_token", _AsaasSettings.access_token);
        request.Headers.Add("User-Agent", _AsaasSettings.useragent);

        var response = await client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Erro ao buscar payments Asaas: {body}");
            throw new Exception("Falha ao obter payments da assinatura.");
        }

        var list = new List<PaymentDTO>();

        using (var doc = JsonDocument.Parse(body))
        {
            var data = doc.RootElement.GetProperty("data");

            foreach (var item in data.EnumerateArray())
            {
                var payment = new PaymentDTO
                {
                    Id = item.GetProperty("id").GetString(),
                    Status = item.GetProperty("status").GetString(),
                    NetValue = item.TryGetProperty("netValue", out var nv) && nv.ValueKind == JsonValueKind.Number ? nv.GetDecimal() : (decimal?)null,
                    DueDate = item.GetProperty("dueDate").GetDateTime(),
                    EstimatedCreditDate = item.TryGetProperty("estimatedCreditDate", out var ec) && ec.ValueKind == JsonValueKind.String
                        ? ec.GetDateTime()
                        : (DateTime?)null,
                    InvoiceUrl = item.TryGetProperty("invoiceUrl", out var url) ? url.GetString() : null,
                    BillingType = item.TryGetProperty("billingType", out var bt) ? bt.GetString() : null,
                    Value = item.TryGetProperty("value", out var vl) ? vl.GetDecimal() : 0,
                };

                list.Add(payment);
            }
        }

        return list;
    }

    private async Task ProcessarParcelas(CheckoutViewModel model, string billingType, List<PaymentDTO> payments, string idcl, string idsub)
    {
        int numeroParcela = payments.Count;

        foreach (var payment in payments)
        {
            decimal valor = payment.Value;
            decimal netValue = payment.NetValue ?? payment.Value;

            DateTime? dataBaixa = (billingType == "CREDIT_CARD" && payment.Status == "CONFIRMED") ? DateTime.Now.Date : null;

            await _parcela.Adicionar(new ParcelaViewModel
            {
                id = payment.Id,
                nossonumero = payment.Id,
                datavencimento = payment.DueDate,
                databaixa = dataBaixa,
                idassinatura = idsub,
                idcheckout = payment.Id,
                comissao = (decimal)0.10 * valor,
                valor = valor,
                numparcela = numeroParcela,
                valorliquido = netValue - (decimal)0.10 * valor,
                acrescimos = 0,
                descontoantecipacao = 0,
                descontoplataforma = valor - netValue,
                idformapagto = billingType switch
                {
                    "PIX" => FormaPagto.Pix,
                    "BOLETO" => FormaPagto.Boleto,
                    _ => FormaPagto.Cartao
                },
                idparceiro = idcl,
                plataforma = "Asaas",
                descontos = 0,
                dataestimadapagto = payment.EstimatedCreditDate
            });

            numeroParcela--;
        }
    }

    public class PaymentDTO
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public decimal? NetValue { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? EstimatedCreditDate { get; set; }
        public string InvoiceUrl { get; set; }
        public string BillingType { get; set; }
        public decimal Value { get; set; }
    }

    private async Task RegistrarLog(string nomeCliente, string ip, string tipoOperacao, string url, string payload, string retorno, string statusHttp, string erro = null)
    {
        var log = new LogCheckoutViewModel
        {
            NomeCliente = nomeCliente,
            IpOrigem = ip,
            TipoOperacao = tipoOperacao,
            UrlRequisicao = url,
            PayloadEnviado = payload,
            RetornoApi = retorno,
            StatusHttp = statusHttp,
            Erro = erro
        };
        _log.Adicionar(log);
    }
}