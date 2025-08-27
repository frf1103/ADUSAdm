using ADUSAdm.Services;
using ADUSAdm.Shared;
using ADUSClient.Controller;
using ADUSClient.Parceiro;
using ADUSClient.Parcela;
using Humanizer;
using MathNet.Numerics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Admin,Super,User, UserV,Afiliado,Coprodutor")]
    public class ParcelaController : Controller
    {
        private readonly ParcelaControllerClient _clienteAPI;
        private readonly ParceiroControllerClient _parceiroAPI;
        private readonly AssinaturaControllerClient _assinaturaAPI;
        private readonly SessionManager _sessionManager;
        private readonly ASAASSettings _asaas;
        private readonly CheckoutService _chk;

        public ParcelaController(ParcelaControllerClient clienteAPI, ParceiroControllerClient parceiroAPI, AssinaturaControllerClient assinaturaAPI, SessionManager sessionManager, IOptions<ASAASSettings> asaasSettings, CheckoutService chk)
        {
            _clienteAPI = clienteAPI;
            _parceiroAPI = parceiroAPI;
            _assinaturaAPI = assinaturaAPI;
            _sessionManager = sessionManager;
            _asaas = asaasSettings.Value;
            _chk = chk;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime ini, DateTime fim, int tipodata = 0, int status = 3, string idparceiro = "0", int forma = 3, string idassinatura = "", string? filtro = "", int? checkout = 2)
        {
            ViewBag.permissao = (_sessionManager.userrole == "Admin" || _sessionManager.userrole == "Super");
            ViewBag.Titulo = "Parcelas";
            ViewBag.idBanco = ""; // se for necessário para filtro em select
            ViewBag.Descricao = filtro;
            ViewBag.slCheckout = checkout;

            List<ListParceiroViewModel> t = await _parceiroAPI.Lista("", true, false, false, false);

            var a = await _assinaturaAPI.ListaById(idassinatura);

            ViewBag.parceiros = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            /*
            $('#status').val(@ViewBag.Status).trigger('change');
            $('#forma').val(@ViewBag.Forma).trigger('change');
            $('#tipodata').val(@ViewBag.tipodata).trigger('change');

            */

            ViewBag.Status = status.ToString();
            ViewBag.Forma = forma.ToString();
            ViewBag.tipodata = tipodata.ToString();
            ViewBag.SelectedOptionS = idparceiro.ToString();
            ViewBag.idassinatura = idassinatura.ToString();
            ViewBag.idparceiro = idparceiro.ToString();
            if (a == null)
            {
                if (ini.Year == 1)
                {
                    ViewBag.dtinicio = (DateTime.Now.AddDays(-30)).ToString("yyyy-MM-dd");
                }
                else
                {
                    ViewBag.dtinicio = ini.ToString("yyyy-MM-dd");
                }
            }
            else
            {
                ViewBag.dtinicio = a.datavenda.ToString("yyyy-MM-dd");
            }
            if (fim.Year == 1)
            {
                ViewBag.dtfim = (DateTime.Now.AddDays(30)).ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtfim = fim.ToString("yyyy-MM-dd");
            }

            //var lista = await _clienteAPI.ListarParcela(dataIni, dataFim, tipodata, status, idparceiro, forma, idassinatura, filtro);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            return View("Index");
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpGet]
        public async Task<IActionResult> Adicionar(string id, DateTime ini, DateTime fim, int acao = 0, string idparceiro = "0", string idassina = "0", int forma = 0, int tipodata = 0, int status = 0
            )
        {
            ParcelaViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new ParcelaViewModel
                {
                    id = Guid.NewGuid().ToString("N"),
                    idassinatura = idassina
                };
                ViewBag.Titulo = "Adicionar Parcela";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                c = await _clienteAPI.ListaById(id);
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar Parcela";
                    ViewBag.Acao = "editar";
                }
                else if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir Parcela";
                    ViewBag.Acao = "excluir";
                }
                else if (acao == 4)
                {
                    ViewBag.Titulo = "Visualizar Parcela";
                    ViewBag.Acao = "visualizar";
                }
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<ParcelaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            ViewBag.Formas = new List<SelectListItem>
            {
                new SelectListItem { Text = "Cartão", Value = "0" },
                new SelectListItem { Text = "Boleto", Value = "1" },
                new SelectListItem { Text = "Pix", Value = "2" }
            };
            ViewBag.idparceiro = idparceiro;
            ViewBag.idassinatura = idassina;
            ViewBag.tipodada = tipodata;
            ViewBag.forma = forma;
            ViewBag.ini = ini.ToString("yyyy-MM-dd");
            ViewBag.fim = fim.ToString("yyyy-MM-dd");
            ViewBag.status = status;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Console.WriteLine($"idassinatura = {c.idassinatura}");

            return View(c);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Super,User,Afiliado,Coprodutor")]
        public async Task<IActionResult> Visualizar(string id)
        {
            ParcelaViewModel c;

            int acao = 4;
            if (acao == 1)
            {
                c = new ParcelaViewModel
                {
                    id = Guid.NewGuid().ToString("N"),
                };
                ViewBag.Titulo = "Adicionar Parcela";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                c = await _clienteAPI.ListaById(id);
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar Parcela";
                    ViewBag.Acao = "editar";
                }
                else if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir Parcela";
                    ViewBag.Acao = "excluir";
                }
                else if (acao == 4)
                {
                    ViewBag.Titulo = "Visualizar Parcela";
                    ViewBag.Acao = "visualizar";
                }
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<ParcelaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            ViewBag.Formas = new List<SelectListItem>
            {
            new SelectListItem { Text = "Cartão", Value = "0" },
            new SelectListItem { Text = "Boleto", Value = "1" },
            new SelectListItem { Text = "Pix", Value = "2" }
            };
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Console.WriteLine($"idassinatura = {c.idassinatura}");

            return View("adicionar", c);
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpPost]
        public async Task<IActionResult> Adicionar(ParcelaViewModel dados)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            var response = await _clienteAPI.Adicionar(dados);
            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ParcelaViewModel>(y);
                return RedirectToAction("Adicionar", new { id = result.id, acao = 2 });
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);
            return View("Adicionar", dados);
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpPost]
        public async Task<IActionResult> Editar(ParcelaViewModel dados)
        {
            var response = await _clienteAPI.Salvar(dados.id, dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            TempData["Erro"] = x;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            return RedirectToAction("Adicionar", new { acao = 2, id = dados.id });
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpPost]
        public async Task<IActionResult> Excluir(string id, ParcelaViewModel dados)
        {
            var response = await _clienteAPI.Excluir(id);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("Adicionar");
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpGet]
        public async Task<JsonResult> GetData(DateTime ini, DateTime fim, int tipodata, int status, string idparceiro, int forma, string idassinatura, string? filtro, int? checkout = 2)
        {
            var lista = await _clienteAPI.ListarParcela(ini, fim, tipodata, status, idparceiro ?? "0", forma, idassinatura ?? "0", filtro ?? " ", checkout);
            var x = lista.OrderBy(x => x.datavencimento);
            return Json(x);
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpGet]
        public async Task<JsonResult> ParcelasDisponiveis(
                 DateTime inicio,
                 DateTime fim,
                 string plataforma,
                 string registro,
                 string term = "",
                 int page = 1,
                 int pageSize = 20
 )
        {
            var parceiro = await _parceiroAPI.ListaByRegistro(registro);
            string idparceiro = registro;
            // Chama sua API atual de parcelas
            var lista = await _clienteAPI.ListarParcela(
                inicio.AddDays(-90),
                fim.AddDays(90),
                0,    // ajuste se precisar
                status: 3,      // ajuste se precisar
                idparceiro: idparceiro,
                forma: 3,
                idassinatura: "0",
                filtro: " ",
                checkout: 0
            );

            // Filtra por plataforma
            if (!string.IsNullOrWhiteSpace(plataforma))
                lista = lista.Where(p => p.plataforma == plataforma).ToList();

            // Filtro pelo termo do Select2
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim().ToLower();
                lista = lista.Where(p =>
                    (!string.IsNullOrEmpty(p.nomeparceiro) && p.nomeparceiro.ToLower().Contains(t)) ||
                    (!string.IsNullOrEmpty(p.descforma) && p.descforma.ToLower().Contains(t)) ||
                    p.id.ToString().Contains(t)
                ).ToList();
            }

            // Ordena por vencimento
            var query = lista.OrderBy(p => p.datavencimento);

            // Paginação
            var total = query.Count();
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
            var skip = (page - 1) * pageSize;

            var items = query
                .Skip(skip)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.id,
                    vencimento = p.datavencimento,
                    nome = p.nomeparceiro,
                    tipo = p.descforma
                })
                .ToList();

            return Json(new
            {
                items,
                total,
                pageSize
            });
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpGet]
        public async Task<IActionResult> visaogeralcarteira(DateTime vencimentoInicio, DateTime vencimentoFim, string idparceiro = "0", int acao = 0)
        {
            List<ListParceiroViewModel> t = await _parceiroAPI.Lista("", true, false, false, false);

            ViewBag.parceiros = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            ViewBag.idParceiro = idparceiro;

            if (vencimentoInicio.Year == 1)
            {
                ViewBag.ini = "2000-01-01";
            }
            else
            {
                ViewBag.ini = vencimentoInicio.ToString("yyyy-MM-dd");
            }
            if (vencimentoFim.Year == 1)
            {
                ViewBag.fim = "2500-01-01";
            }
            else
            {
                ViewBag.fim = vencimentoFim.ToString("yyyy-MM-dd");
            }
            List<visaogeralviewmodel> visao = await _clienteAPI.VisaoGeral(ViewBag.idParceiro, ViewBag.ini, ViewBag.fim);
            ViewBag.TotalComissao = visao.Sum(v => v.comissaoPaga);
            ViewBag.TotalRecebido = visao.Sum(v => v.valorRecebido);
            ViewBag.TotalAntecipacao = visao.Sum(v => v.taxaAntecipacao);
            ViewBag.TotalPlataforma = visao.Sum(v => v.taxaPlataforma);
            ViewBag.TotalPago = visao.Sum(v => v.valorPago);
            ViewBag.TotalAVencer = visao.Sum(v => v.valorAVencer);
            ViewBag.TotalVencido = visao.Sum(v => v.valorVencidas);
            ViewBag.TotalValor = visao.Sum(v => v.valortotal);
            ViewBag.arvores = visao.Sum(v => v.arvores);
            ViewBag.Acompensar = visao.Sum(v => v.acompensar);
            ViewBag.hoje = DateTime.Now.Date.AddDays(-1).ToString("yyyy-MM-dd");
            return View(visao);
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpGet]
        public async Task<IActionResult> ImportarParcela()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpGet]
        public async Task<IActionResult> MostrarParcelas(DateTime dataInicio, DateTime dataFim, string plataforma)
        {
            var secretKey = _asaas.access_token;

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));

            var allPayables = new List<string>(); // ou seu model
            string cursor = null;
            bool hasMore = true;
            int linhas = 0, linha = 0;
            int offset = 0;
            int limit = 50;
            List<ImportarParcelaViewModel> retorno = new List<ImportarParcelaViewModel>();
            while (hasMore)
            {
                string url = _asaas.urlpayments;//model.urltransac;

                url += "?dueDate[ge]=" + dataInicio.ToString("yyyy-MM-dd") + "&dueDate[le]=" + dataFim.ToString("yyyy-MM-dd");
                url = url + "&limit=" + limit.ToString() +
                    "&offset=" + offset.ToString();
                var options = new RestClientOptions(url);
                var client = new RestClient(options);

                var request = new RestRequest();
                request.AddHeader("access_token", _asaas.access_token);

                request.AddHeader("accept", "application/json");

                var response = await client.ExecuteGetAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine("Erro: " + response.ErrorException.Message);
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
                        var idc = item.GetProperty("customer").GetString();
                        var parceiro = await _chk.BuscarIdParceiroPorCustomerId(idc);
                        var parcela = await _clienteAPI.ListaByIdCheckout(id);
                        if (parcela == null && parceiro != null)
                        {
                            retorno.Add(new ImportarParcelaViewModel
                            {
                                id = id,
                                nome = parceiro.razaoSocial,
                                data = item.GetProperty("originalDueDate").GetDateTime(),
                                dataestimada = item.TryGetProperty("estimatedCreditDate", out var prop) &&
                                                prop.ValueKind != JsonValueKind.Null &&
                                                prop.ValueKind != JsonValueKind.Undefined
                                                ? prop.GetDateTime()
                                                : (DateTime?)null,
                                registro = parceiro.id,
                                valor = item.GetProperty("value").GetDecimal(),
                                tipo = item.GetProperty("billingType").GetString(),
                                descontoplataforma = item.GetProperty("value").GetDecimal() - item.GetProperty("netValue").GetDecimal(),
                                comissao = item.GetProperty("value").GetDecimal() * (decimal)0.10
                            }); ;
                        }
                        //var acquirerNSU = item.GetProperty("gateway_id").GetString();
                        linha++;
                    }
                }
                hasMore = root.GetProperty("hasMore").GetBoolean();
                offset += limit;
            }
            ViewBag.inicio = dataInicio.ToString("yyyy-MM-dd");
            ViewBag.fim = dataFim.ToString("yyyy-MM-dd");
            return View("importarparcela", retorno);
        }

        [HttpPost]
        public async Task<IActionResult> SalvarParcelas([FromBody] List<ParcelaSalva> parcelas)
        {
            if (parcelas == null || !parcelas.Any())
                return BadRequest("Lista de Parcelas vazia.");

            try
            {
                foreach (var item in parcelas)
                {
                    // Exemplo: salvar em API TransacBanco (ou montar uma TransacBancoViewModel e enviar)

                    var p = await _clienteAPI.ListaById(item.idParcela);
                    if (p != null)
                    {
                        p.nossonumero = item.idcheckout;
                        await _clienteAPI.Salvar(p.id, p);
                    }
                }

                return Ok(new { mensagem = "Parcelas salvas com sucesso." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao salvar parcelas: " + ex.Message);
            }
        }

        public class ParcelaSalva
        {
            public string idcheckout { get; set; }
            public string idParcela { get; set; }
        }
    }
}