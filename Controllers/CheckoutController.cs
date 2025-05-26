// Controllers/CheckoutController.cs
using ADUSAdm.Data;
using ADUSClient.Assinatura;
using ADUSClient.Controller;
using Microsoft.AspNetCore.Mvc;

public class CheckoutController : Controller
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CheckoutController> _logger;
    private readonly ILogService _logService;
    private readonly ADUScontext _context;
    private readonly ParametroGuruControllerClient _parametros;

    public CheckoutController(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<CheckoutController> logger, ILogService logService, ADUScontext context, ParametroGuruControllerClient parametros)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _logService = logService;
        _context = context;
        _parametros = parametros;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var userAgent = Request.Headers["User-Agent"].ToString().ToLower();
        string idAfiliado = Request.Cookies["idafiliado"];
        string plata = Request.Cookies["idplataforma"];
        if (userAgent.Contains("iphone") || userAgent.Contains("android") || userAgent.Contains("mobile"))
        {
            return RedirectToAction("Mobile");
        }
        return View();
    }

    [HttpGet]
    public IActionResult Mobile()
    {
        string idAfiliado = Request.Cookies["idafiliado"];
        string plata = Request.Cookies["idplataforma"];
        return View("IndexMobile");
    }

    [HttpPost]
    public async Task<IActionResult> Index(CheckoutViewModel model)
    {
        if (!ModelState.IsValid || model.Email != model.EmailConfirmacao)
            return View(model);

        try
        {
            var par = await _parametros.ListaById(2);

            var http = _httpClientFactory.CreateClient();
            http.BaseAddress = new Uri("https://sandbox.asaas.com/api/v3/");
            http.DefaultRequestHeaders.Add(par.token, _config["Asaas:SandboxToken"]);

            // Criar cliente no Asaas
            var clientePayload = new
            {
                name = model.Nome,
                email = model.Email,
                phone = $"+{model.Ddi}{model.Telefone}",
                cpfCnpj = model.cpfCnpj
            };
            var clienteRes = await http.PostAsJsonAsync("customers", clientePayload);
            var cliente = await clienteRes.Content.ReadFromJsonAsync<dynamic>();

            model.IdClienteAsaas = cliente.id;
            var valorTotal = model.QuantidadeArvores * 47;
            var billingType = model.FormaPagamento.ToUpper() switch
            {
                "PIX" => "PIX",
                "BOLETO" => "BOLETO",
                _ => "CREDIT_CARD"
            };

            object cobrancaPayload;
            string token = null;

            if (billingType == "CREDIT_CARD")
            {
                var tokenPayload = new
                {
                    creditCard = new
                    {
                        holderName = model.NomeTitular,
                        number = model.NumeroCartao,
                        expiryMonth = model.Validade.Split('/')[0],
                        expiryYear = model.Validade.Split('/')[1],
                        ccv = model.Cvv
                    }
                };
                var tokenRes = await http.PostAsJsonAsync("creditCard/tokenize", tokenPayload);
                var tokenObj = await tokenRes.Content.ReadFromJsonAsync<dynamic>();

                if (tokenObj?.creditCardToken == null)
                {
                    await _logService.RegistrarAsync("Warning", "CheckoutController", "Falha na tokenização do cartão.", tokenObj?.ToString());
                    ViewBag.Mensagem = "Falha na tokenização do cartão.";
                    return View("Falha");
                }
                token = tokenObj.creditCardToken;

                cobrancaPayload = new
                {
                    customer = cliente.id.ToString(),
                    billingType = billingType,
                    creditCardToken = token,
                    value = valorTotal,
                    installmentCount = model.FormaPagamento == "Parcelado" ? model.Parcelas : null,
                    description = string.Format("{0} árvore(s) - R$47 cada", model.QuantidadeArvores),
                    dueDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                    postalService = false
                };
            }
            else
            {
                cobrancaPayload = new
                {
                    customer = cliente.id.ToString(),
                    billingType = billingType,
                    value = valorTotal,
                    description = string.Format("{0} árvore(s) - R$47 cada", model.QuantidadeArvores),
                    dueDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                    postalService = false
                };
            }

            var cobrancaRes = await http.PostAsJsonAsync("payments", cobrancaPayload);
            var cobranca = await cobrancaRes.Content.ReadFromJsonAsync<dynamic>();

            if (!cobrancaRes.IsSuccessStatusCode)
            {
                var erro = cobranca?.errors?[0]?.description ?? "Erro desconhecido na cobrança.";
                await _logService.RegistrarAsync("Warning", "CheckoutController", $"Falha na cobrança: {erro}", cobranca?.ToString());
                ViewBag.Mensagem = $"Falha na cobrança: {erro}";
                return View("Falha");
            }

            model.IdCobrancaAsaas = cobranca.id;
            model.LinkCobranca = cobranca.invoiceUrl;
            model.TipoPagamentoEfetivado = billingType;
            model.TokenCartao = token;
            model.LinhaDigitavel = cobranca.bankSlip?.digitableLine;
            model.PayloadQrCode = cobranca.pix?.payload;

            _context.Add(model);
            await _context.SaveChangesAsync();

            ViewBag.Mensagem = "Cobrança criada com sucesso.";
            ViewBag.Link = model.LinkCobranca;
            ViewBag.TipoPagamento = model.TipoPagamentoEfetivado;
            ViewBag.QrCode = model.PayloadQrCode;
            ViewBag.LinhaDigitavel = model.LinhaDigitavel;

            return View("Sucesso");
        }
        catch (Exception ex)
        {
            await _logService.RegistrarAsync("Error", "CheckoutController", "Erro inesperado durante checkout", ex.ToString());
            ViewBag.Mensagem = $"Erro inesperado: {ex.Message}";
            return View("Falha");
        }
    }
}