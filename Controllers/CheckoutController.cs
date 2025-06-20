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

    public CheckoutController(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<CheckoutController> logger, ILogService logService, ADUScontext context, ParametroGuruControllerClient parametros, CartaoAssinaturaControllerClient cartoes, ParceiroControllerClient parceiro, SharedControllerClient shared, AssinaturaControllerClient assinatura, ParcelaControllerClient parcela)
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
        string idAfiliado = null;
        if (Request.Cookies.ContainsKey("idafiliado"))
        {
            idAfiliado = Request.Cookies["idafiliado"];
        }

        //model.QuantidadeArvores = 10;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            ModelState.AddModelError("", "Sessão inválida ou expirada.");
            return View("Falha");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.QuantidadeArvores <= 0)
        {
            ModelState.AddModelError("QuantidadeArvores", "Quantidade inválida.");
            return View(model);
        }
        /*
                if (!Regex.IsMatch(model.cpfCnpj, @"^\d{11}|\d{14}$"))
                {
                    ModelState.AddModelError("cpfCnpj", "CPF ou CNPJ inválido.");
                    return View(model);
                }
             */
        if (!Regex.IsMatch(model.Validade ?? "", @"^(0[1-9]|1[0-2])/\d{4}$"))
        {
            ModelState.AddModelError("Validade", "Formato da validade inválido (MM/AAAA).");
            return View(model);
        }

        string remoteip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            remoteip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0];
        }

        try
        {
            var par = await _parametros.ListaById(2);
            var http = _httpClientFactory.CreateClient();
            http.BaseAddress = new Uri("https://sandbox.asaas.com/api/v3/");
            http.DefaultRequestHeaders.Add("access_token", "$aact_hmlg_000MzkwODA2MWY2OGM3MWRlMDU2NWM3MzJlNzZmNGZhZGY6OmNjODVmNDllLTg2ZDYtNDFhMy05MDFhLTkyY2MwZDIyZWYxZDo6JGFhY2hfZWQwZjU1ODQtMjY3Yi00OGVhLTllOTUtYTlmNWQ4M2IwZGY5");

            //Criar cliente no Asaas caso não exista
            var clientePayload = new
            {
                name = model.Nome,
                email = model.Email,
                phone = $"+{model.Ddi}{model.Telefone}",
                cpfCnpj = model.cpfCnpj
            };

            var json = JsonSerializer.Serialize(clientePayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://api-sandbox.asaas.com/v3/customers?cpfCnpj=" + FBSLIb.StringLib.Somentenumero(model.cpfCnpj)),
                Headers =
                {
                    { "accept", "application/json" },
                    { "access_token", "$aact_hmlg_000MzkwODA2MWY2OGM3MWRlMDU2NWM3MzJlNzZmNGZhZGY6OmNjODVmNDllLTg2ZDYtNDFhMy05MDFhLTkyY2MwZDIyZWYxZDo6JGFhY2hfZWQwZjU1ODQtMjY3Yi00OGVhLTllOTUtYTlmNWQ4M2IwZGY5" },
                    {"User-Agent", "ADUSCheckout/1.0 (sandbox)"}
                }
            };
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            var docx = JsonDocument.Parse(body);

            string idcliente = " ";
            if (docx.RootElement.GetProperty("data").GetArrayLength() == 0)
            {
                request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://api-sandbox.asaas.com/v3/customers"),
                    Headers =
                {
                    { "accept", "application/json" },
                    { "access_token", "$aact_hmlg_000MzkwODA2MWY2OGM3MWRlMDU2NWM3MzJlNzZmNGZhZGY6OmNjODVmNDllLTg2ZDYtNDFhMy05MDFhLTkyY2MwZDIyZWYxZDo6JGFhY2hfZWQwZjU1ODQtMjY3Yi00OGVhLTllOTUtYTlmNWQ4M2IwZGY5" },
                    {"User-Agent", "ADUSCheckout/1.0 (sandbox)"}
                },
                    Content = content
                };
                response = await client.SendAsync(request);
                body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                idcliente = doc.RootElement.GetProperty("id").GetString();
            }
            else
            {
                idcliente = docx.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
            }

            string idcl;

            //Checar parceiro na ADUS, adicionar caso não exista
            var cliente = await _parceiro.Lista(FBSLIb.StringLib.Somentenumero(model.cpfCnpj), true, true, true, true);
            if (cliente.Count == 0)
            {
                var uf = await _shared.ListaUFBySigla(model.Estado);
                int icoduf = uf[0].id;
                var cidade = await _shared.ListaCidade(icoduf, model.Cidade);
                idcl = Guid.NewGuid().ToString();
                var parv = new ParceiroViewModel
                {
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
                    registro = FBSLIb.StringLib.Somentenumero(model.cpfCnpj),
                    id = idcl
                };
                var respc = await _parceiro.Adicionar(parv);
            }
            else
            {
                idcl = cliente[0].id;
            }

            string idplataforma = "";

            //var body = await response.Content.ReadAsStringAsync();

            string billingType = model.FormaPagamento.ToUpper() switch
            {
                "PIX" => "PIX",
                "BOLETO" => "BOLETO",
                _ => "CREDIT_CARD"
            };

            object cobrancaPayload;
            string token = null;
            string cctoken = " ", ccbandeira = " ", ccnumber = " ";
            object subsPayload;
            if (billingType == "CREDIT_CARD")
            {
                if (string.IsNullOrWhiteSpace(model.NumeroCartao) || string.IsNullOrWhiteSpace(model.Cvv))
                {
                    ModelState.AddModelError("", "Dados do cartão estão incompletos.");
                    return View(model);
                }

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
                    remoteIp = "187.100.100.100" //remoteip
                };
                json = JsonSerializer.Serialize(tokenPayload);
                content = new StringContent(json, Encoding.UTF8, "application/json");

                request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://api-sandbox.asaas.com/v3/creditCard/tokenizeCreditCard"),
                    Headers =
                    {
                        {"User-Agent", "ADUSCheckout/1.0 (sandbox)"},
                        { "accept", "application/json" },
                        { "access_token", "$aact_hmlg_000MzkwODA2MWY2OGM3MWRlMDU2NWM3MzJlNzZmNGZhZGY6OmNjODVmNDllLTg2ZDYtNDFhMy05MDFhLTkyY2MwZDIyZWYxZDo6JGFhY2hfZWQwZjU1ODQtMjY3Yi00OGVhLTllOTUtYTlmNWQ4M2IwZGY5" },
                    },
                    Content = content
                };
                using (response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    body = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(body);
                }
                using var doct = JsonDocument.Parse(body);
                cctoken = doct.RootElement.GetProperty("creditCardToken").GetString();
                ccbandeira = doct.RootElement.GetProperty("creditCardBrand").GetString();
                ccnumber = doct.RootElement.GetProperty("creditCardNumber").GetString();

                model.NumeroCartao = null;
                model.Cvv = null;

                subsPayload = new
                {
                    customer = idcliente,
                    billingType = "CREDIT_CARD",
                    value = model.ValorTotal,
                    nextDueDate = DateTime.Now.Date,
                    cycle = "MONTHLY",
                    description = model.QuantidadeArvores.ToString() + " ARVORES PROJETO TECA SOCIAL",
                    maxPayments = 84,
                    creditCardToken = cctoken,
                    remoteIp = "187.100.100.100" //remoteip
                };
            }
            else
            {
                subsPayload = new
                {
                    customer = idcliente,
                    billingType = billingType,
                    value = model.ValorTotal,
                    nextDueDate = DateTime.Now.Date,
                    cycle = "MONTHLY",
                    description = model.QuantidadeArvores.ToString() + " ARVORES PROJETO TECA SOCIAL",
                    maxPayments = 84,
                    remoteIp = "187.100.100.100" //remoteip
                };
            }
            json = JsonSerializer.Serialize(subsPayload);
            content = new StringContent(json, Encoding.UTF8, "application/json");
            request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api-sandbox.asaas.com/v3/subscriptions"),
                Headers =
                    {
                        {"User-Agent", "ADUSCheckout/1.0 (sandbox)"},
                        { "accept", "application/json" },
                        { "access_token", "$aact_hmlg_000MzkwODA2MWY2OGM3MWRlMDU2NWM3MzJlNzZmNGZhZGY6OmNjODVmNDllLTg2ZDYtNDFhMy05MDFhLTkyY2MwZDIyZWYxZDo6JGFhY2hfZWQwZjU1ODQtMjY3Yi00OGVhLTllOTUtYTlmNWQ4M2IwZGY5" },
                    },
                Content = content
            };
            using (response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                body = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(body);
            }
            using var docs = JsonDocument.Parse(body);
            string idsub = docs.RootElement.GetProperty("id").GetString();
            AddAssinaturaADUS(model, idsub, idcl, idAfiliado);
            if (billingType == "CREDIT_CARD" && ccbandeira != null && ccnumber != null && cctoken != null)
            {
                CartaoAssinaturaViewModel xx = new CartaoAssinaturaViewModel
                {
                    Ativo = true,
                    Bandeira = ccbandeira,
                    UltimosDigitos = ccnumber,
                    IdToken = cctoken,
                    IdAssinatura = idsub
                };

                await _cartoes.Adicionar(xx);
            }

            /*
                        cobrancaPayload = new
                        {
                            customer = clienteId,
                            billingType,
                            creditCardToken = token,
                            value = model.ValorTotal,
                            installmentCount = model.FormaPagamento == "Parcelado" ? model.Parcelas : null,
                            description = $"{model.QuantidadeArvores} árvore(s) - R$47 cada",
                            dueDate = model.FormaPagamento == "Boleto" && model.DataVencimento.HasValue
                            ? model.DataVencimento.Value.ToString("yyyy-MM-dd")
                            : DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                            postalService = false
                        };

                        var cobrancaRes = await http.PostAsJsonAsync("payments", cobrancaPayload);
                        if (!cobrancaRes.IsSuccessStatusCode)
                        {
                            var errorBody = await cobrancaRes.Content.ReadAsStringAsync();
                            _logger.LogWarning("Erro na criação da cobrança: {0}", errorBody);
                            ModelState.AddModelError("", "Erro ao criar cobrança. Verifique os dados e tente novamente.");
                            return View(model);
                        }

                        var cobrancaJson = await cobrancaRes.Content.ReadFromJsonAsync<JsonElement>();
                        model.IdCobrancaAsaas = cobrancaJson.GetProperty("id").GetString();
                        model.LinkCobranca = cobrancaJson.GetProperty("invoiceUrl").GetString();
                        model.LinhaDigitavel = cobrancaJson.GetProperty("bankSlip").GetProperty("digitableLine").GetString();
                        model.PayloadQrCode = cobrancaJson.GetProperty("pix").GetProperty("payload").GetString();
                        model.TipoPagamentoEfetivado = billingType;

                        _context.Add(model);
                        await _context.SaveChangesAsync();

                        ViewBag.Mensagem = "Cobrança criada com sucesso.";
                        ViewBag.Link = model.LinkCobranca;
                        ViewBag.TipoPagamento = model.TipoPagamentoEfetivado;
                        ViewBag.QrCode = model.PayloadQrCode;
                        ViewBag.LinhaDigitavel = model.LinhaDigitavel;
            */
            return View("Sucesso");
        }
        catch (Exception ex)
        {
            await _logService.RegistrarAsync("Error", "CheckoutController", "Erro inesperado durante checkout", ex.ToString());
            _logger.LogError(ex, "Erro inesperado no checkout");
            ModelState.AddModelError("", "Erro inesperado. Tente novamente mais tarde.");
            return View(model);
        }
    }

    public async Task<string> AddAssinaturaADUS(CheckoutViewModel m, string id, string idcliente, string idafiliado)
    {
        FormaPagto idforma = (m.FormaPagamento == "CREDIT_CARD") ? FormaPagto.Cartao : (m.FormaPagamento == "BOLETO") ? FormaPagto.Boleto : FormaPagto.Pix;
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
}