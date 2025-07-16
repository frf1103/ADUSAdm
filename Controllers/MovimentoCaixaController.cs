using ADUSClient.MovimentoCaixa;
using ADUSClient.Controller;
using ADUSAdm.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using RestSharp;
using System.Text.Json;
using System.Text;
using ADUSClient.Banco;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super")]
    public class MovimentoCaixaController : Controller
    {
        private readonly MovimentoCaixaControllerClient _clienteAPI;
        private readonly TransacaoControllerClient _transacaoAPI;
        private readonly CentroCustoControllerClient _centroCustoAPI;
        private readonly PlanoContaControllerClient _categoriaAPI;
        private readonly ParceiroControllerClient _parceiroAPI;
        private readonly ContaCorrenteControllerClient _contaCorrenteAPI;
        private readonly SessionManager _sessionManager;
        private readonly TransacBancoControllerClient _extratoAPI;
        private readonly ParametroGuruControllerClient _parametro;
        private readonly ParcelaControllerClient _parcela;

        public MovimentoCaixaController(
            MovimentoCaixaControllerClient clienteAPI,
            TransacaoControllerClient transacaoAPI,
            CentroCustoControllerClient centroCustoAPI,
            PlanoContaControllerClient categoriaAPI,
            ParceiroControllerClient parceiroAPI,
            ContaCorrenteControllerClient contaCorrenteAPI,
            SessionManager sessionManager,
            TransacBancoControllerClient extratoAPI,
            ParametroGuruControllerClient parametro,
            ParcelaControllerClient parcela)
        {
            _clienteAPI = clienteAPI;
            _transacaoAPI = transacaoAPI;
            _centroCustoAPI = centroCustoAPI;
            _categoriaAPI = categoriaAPI;
            _parceiroAPI = parceiroAPI;
            _contaCorrenteAPI = contaCorrenteAPI;
            _sessionManager = sessionManager;
            _extratoAPI = extratoAPI;
            _parametro = parametro;
            _parcela = parcela;
        }

        private async Task CarregarViewBagsAsync()
        {
            var transacoes = await _transacaoAPI.Listar("");
            ViewBag.Transacoes = new SelectList(transacoes, "Id", "Descricao");

            var centros = await _centroCustoAPI.Listar("");
            ViewBag.CentroCustos = new SelectList(centros, "Id", "Descricao");

            var categorias = await _categoriaAPI.ListarAsync("");
            ViewBag.Categorias = new SelectList(categorias, "Id", "Descricao");

            var parceiros = await _parceiroAPI.Lista("", true, false, false, false);
            ViewBag.Parceiros = new SelectList(parceiros, "id", "razaoSocial");

            var contas = await _contaCorrenteAPI.Listar("", null);
            ViewBag.Contas = new SelectList(contas, "id", "descricao");

            ViewBag.Sinais = new SelectList(new[]
            {
                new { Valor = "D", Texto = "Débito" },
                new { Valor = "C", Texto = "Crédito" }
            }, "Valor", "Texto");
        }

        public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, int? idtransacao, int? idcentrocusto, string? idparceiro, int? idcontacorrente, int? idcategoria, string? filtro)
        {
            ViewBag.permissao = (_sessionManager.userrole != "UserV");

            //var lista = await _clienteAPI.ListarAsync(ini, fim, idtransacao, idcentrocusto, idparceiro, idcontacorrente, idcategoria, filtro);
            DateTime? ini, fim;
            ini = dataInicio; fim = dataFim;
            if (ini == null)
            {
                ini = DateTime.Now.Date.AddDays(-7);
                fim = DateTime.Now.Date;
            }
            ViewBag.ini = ini?.ToString("yyyy-MM-dd");
            ViewBag.fim = fim?.ToString("yyyy-MM-dd");
            ViewBag.idtransacao = idtransacao ?? 0;
            ViewBag.idcentrocusto = idcentrocusto ?? 0;
            ViewBag.idparceiro = idparceiro ?? "0";
            ViewBag.idcontacorrente = idcontacorrente ?? 0;
            ViewBag.idcategoria = idcategoria ?? 0;
            ViewBag.filtro = filtro;

            await CarregarViewBagsAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(int id = 0, int acao = 1)
        {
            MovimentoCaixaViewModel model = id == 0 ? new MovimentoCaixaViewModel() : await _clienteAPI.ObterPorIdAsync(id) ?? new MovimentoCaixaViewModel();

            ViewBag.idacao = acao;
            ViewBag.Titulo = acao switch
            {
                1 => "Adicionar Movimento",
                2 => "Editar Movimento",
                3 => "Excluir Movimento",
                4 => "Visualizar Movimento",
                _ => "Movimento"
            };
            ViewBag.Acao = acao switch
            {
                1 => "Adicionar",
                2 => "Editar",
                3 => "Excluir",
                _ => "Visualizar"
            };
            if (acao == 4)
            {
                ViewBag.descacao = "disabled";
            }

            await CarregarViewBagsAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(MovimentoCaixaViewModel dados)
        {
            var response = await _clienteAPI.AdicionarAsync(dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);

            await CarregarViewBagsAsync();

            return View(dados);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, MovimentoCaixaViewModel dados)
        {
            var response = await _clienteAPI.AtualizarAsync(id, dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);
            TempData["Erro"] = erro;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            return RedirectToAction("Adicionar", new { id, acao = 2 });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            var response = await _clienteAPI.RemoverAsync(id);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);

            return RedirectToAction("Adicionar", new { id, acao = 3 });
        }

        [HttpGet]
        public async Task<JsonResult> GetData(DateTime? dataInicio, DateTime? dataFim, int? idTransacao, int? idCentroCusto, string? idParceiro, string? idContaCorrente, int? idCategoria, string? descricao)
        {
            var lista = await _clienteAPI.ListarAsync(dataInicio, dataFim, idTransacao, idCentroCusto, idParceiro, idContaCorrente, idCategoria, descricao);
            return Json(lista);
        }

        [HttpGet]
        public async Task<IActionResult> ImportarExtrato(string plataforma, string idContaCorrente, DateTime dataInicio, DateTime dataFim, int acao = 0)
        {
            var contas = await _contaCorrenteAPI.Listar("", null);
            var centroscusto = await _centroCustoAPI.Listar("");
            var categorias = await _categoriaAPI.ListarAsync("");
            ViewBag.contas = new SelectList(contas, "id", "descricao");
            var transac = await _transacaoAPI.Listar("");
            ViewBag.transacoes = new SelectList(transac, "Id", "Descricao");
            ViewBag.categorias = new SelectList(categorias, "Id", "Descricao");
            ViewBag.centroscusto = new SelectList(centroscusto, "Id", "Descricao");
            DateTime dini, dfim;
            List<ExtratoViewModel> extrato = new List<ExtratoViewModel>();
            if (acao == 1)
            {
                try
                {
                    var conta = await _contaCorrenteAPI.GetById(idContaCorrente);
                    int idbanco = conta.bancoId;

                    if (plataforma == "pagarme")
                    {
                        extrato = await ExtratoPagarme(dataInicio, dataFim, idbanco, idContaCorrente);
                    }
                    else if (plataforma == "asaas")
                    {
                        extrato = await ExtratoAsaas(dataInicio, dataFim, idbanco, idContaCorrente);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Erro = "Erro ao importar extrato: " + ex.Message;
                }
            }
            dini = dataInicio;
            dfim = dataFim;
            if (dini.Year == 01)
            {
                dini = DateTime.Now.Date.AddDays(-1);
                dfim = DateTime.Now.Date;
            }
            ViewBag.dtinicio = dini.ToString("yyyy-MM-dd");
            ViewBag.dtfim = dfim.ToString("yyyy-MM-dd");
            ViewBag.contaCorrente = idContaCorrente;
            ViewBag.plataforma = plataforma;
            return View(extrato);
        }

        [HttpGet]
        public async Task<IActionResult> Extrato(string idContaCorrente, DateTime dataInicio, DateTime dataFim, int acao = 0)
        {
            var contas = await _contaCorrenteAPI.Listar("", null);
            ViewBag.contas = new SelectList(contas, "id", "descricao");
            var transac = await _transacaoAPI.Listar("");
            DateTime dini, dfim;
            dini = dataInicio;
            dfim = dataFim;
            List<ExtratoConta> extrato = new List<ExtratoConta>();
            if (acao == 1)
            {
                ViewBag.contaNome = contas.Find(x => x.id == idContaCorrente).descricao;
                ViewBag.dini = dini.ToString();
                ViewBag.dfim = dfim.ToString();

                extrato = await _clienteAPI.Extrato(dini, dfim, idContaCorrente);
            }
            if (dini.Year == 01)
            {
                dini = DateTime.Now.Date.AddDays(-1);
                dfim = DateTime.Now.Date;
            }
            ViewBag.dtinicio = dini.ToString("yyyy-MM-dd");
            ViewBag.dtfim = dfim.ToString("yyyy-MM-dd");
            ViewBag.contaCorrente = idContaCorrente;
            return View(extrato);
        }

        public async Task<List<ExtratoViewModel>> ExtratoPagarme(DateTime ini, DateTime fim, int bancoid, string idcontacorrente)
        {
            List<ExtratoViewModel>? lista = new List<ExtratoViewModel>();
            var model = await _parametro.ListaById(3);

            var secretKey = model.token;

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
            bool hasMore = true;
            int pagina = 1;

            while (hasMore)
            {
                string url = "https://api.pagar.me/core/v5/balance/operations";

                url += "?created_since=" + ini.ToString("yyyy-MM-dd") + "&created_until=" + fim.ToString("yyyy-MM-dd") + "&size=100&page=" + pagina.ToString();

                var options = new RestClientOptions(url);
                var client = new RestClient(options);

                var request = new RestRequest();
                request.AddHeader("Authorization", $"Basic {base64Auth}");
                request.AddHeader("accept", "application/json");

                var response = await client.ExecuteGetAsync(request);

                /*
                if (!response.IsSuccessful)
                {
                    Console.WriteLine("Erro: " + response.ErrorMessage);

                    break;
                }*/

                // Adicione sua lógica de deserialização aqui

                var json = response.Content; // string com JSON recebido do Pagar.me

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        string idrec = "";
                        if (item.TryGetProperty("movement_object", out var mov))
                        {
                            if (mov.TryGetProperty("recipient_id", out var rec))
                            {
                                idrec = rec.GetString();
                            }
                        }
                        //"re_cm4dczn1igtsv0l9tj0rmd5gt"
                        if (item.GetProperty("type").GetString() != "external_settlement" & item.GetProperty("type").GetString() != "payable")
                        {
                            //"external_settlement"
                            //"re_cm4dczn1igtsv0l9tj0rmd5gt"
                            var movcaixa = await _clienteAPI.ListarAsync(null, null, null, null, null, idcontacorrente, null, null, item.GetProperty("id").ToString());
                            if (movcaixa == null || movcaixa.Count == 0)
                            {
                                var v = await BuscaTransacao(item.GetProperty("type").GetString(), bancoid);
                                lista.Add(new ExtratoViewModel
                                {
                                    datamov = item.GetProperty("created_at").GetDateTime().Date,
                                    Historico = item.GetProperty("type").GetString(),
                                    id = item.GetProperty("id").ToString(),
                                    IdTransacao = v[0],
                                    idCategoria = v[1],
                                    IdTransacBanco = item.GetProperty("type").GetString(),
                                    idCentrocusto = v[2],
                                    idparceiro = model.idparceiro,
                                    idbanco = bancoid,
                                    Valor = (item.GetProperty("amount").GetDecimal() / 100 - item.GetProperty("fee").GetDecimal() / 100)
                                });
                            }
                        }
                    }
                }

                if (root.GetProperty("data").GetArrayLength() == 0)
                    hasMore = false;
                else
                    pagina++;
            }
            return lista;
        }

        public async Task<List<ExtratoViewModel>> ExtratoAsaas(DateTime ini, DateTime fim, int bancoid, string idcontacorrente)
        {
            List<ExtratoViewModel>? lista = new List<ExtratoViewModel>();
            var model = await _parametro.ListaById(2);

            var secretKey = model.token;

            bool hasMore = true;
            int linhas = 0, linha = 0;
            int offset = 0;
            int limit = 50;

            while (hasMore)
            {
                string url = "https://api.asaas.com/v3/financialTransactions";
                url += "?startDate=" + ini.ToString("yyyy-MM-dd") + "&finishDate=" + fim.ToString("yyyy-MM-dd");
                url = url + "&status=RECEIVED&limit=" + limit.ToString() +
                            "&offset=" + offset.ToString();

                var options = new RestClientOptions(url);
                var client = new RestClient(options);

                var request = new RestRequest();
                request.AddHeader("access_token", secretKey); // ou "Authorization", $"Bearer {apiKey}"

                request.AddHeader("accept", "application/json");

                var response = await client.ExecuteGetAsync(request);
                /*
                                if (!response.IsSuccessful)
                                {
                                    Console.WriteLine("Erro: " + response.ErrorMessage);
                                    break;
                                }
                */
                // Adicione sua lógica de deserialização aqui

                var json = response.Content; // string com JSON recebido do Pagar.me

                //"PAYMENT_RECEIVED"
                //"INTERNAL_TRANSFER_DEBIT"

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        string idrec = "";
                        if (item.GetProperty("type").GetString() != "PAYMENT_RECEIVED"
                        && item.GetProperty("type").GetString() != "INTERNAL_TRANSFER_DEBIT")
                        //&& item.GetProperty("type").GetString() != "PAYMENT_FEE")
                        {
                            //"external_settlement"
                            //"re_cm4dczn1igtsv0l9tj0rmd5gt"
                            var movcaixa = await _clienteAPI.ListarAsync(null, null, null, null, null, idcontacorrente, null, null, item.GetProperty("id").ToString());
                            if (movcaixa == null || movcaixa.Count == 0)
                            {
                                var v = await BuscaTransacao(item.GetProperty("type").GetString(), bancoid);
                                string idparceiro = model.idparceiro;
                                if (item.GetProperty("type").GetString() == "PAYMENT_FEE")
                                {
                                    //paymentId
                                    if (item.GetProperty("paymentId").GetString() != null)
                                    {
                                        var parcela = await _parcela.ListaByIdCheckout(item.GetProperty("paymentId").GetString());
                                        if (parcela != null)
                                        {
                                            idparceiro = parcela.idparceiro;
                                        }
                                    }
                                }
                                lista.Add(new ExtratoViewModel
                                {
                                    datamov = item.GetProperty("date").GetDateTime().Date,
                                    Historico = item.GetProperty("description").GetString(),
                                    id = item.GetProperty("id").ToString(),
                                    IdTransacao = v[0],
                                    idCategoria = v[1],
                                    IdTransacBanco = item.GetProperty("type").GetString(),
                                    idCentrocusto = v[2],
                                    idparceiro = idparceiro,
                                    idbanco = bancoid,
                                    Valor = item.GetProperty("value").GetDecimal()
                                }); ;
                            }
                        }
                    }
                }
                hasMore = root.GetProperty("hasMore").GetBoolean();
                offset += limit;
            }
            return lista;
        }

        private async Task<int[]> BuscaTransacao(string tipo, int idBanco)
        {
            // Busca a transação correspondente com base no tipo e banco
            var transacao = await _extratoAPI
                .ObterPorId(tipo, idBanco);
            if (transacao != null)
                return new int[] { transacao.idTransacao, transacao.idcategoria, transacao.idcentrocusto };
            else return new int[] { 0, 0, 0 };
        }

        [HttpPost]
        public async Task<IActionResult> SalvarExtrato([FromBody] List<ExtratoViewModel> extratos)
        {
            if (extratos == null || !extratos.Any())
                return BadRequest("Lista de extratos vazia.");

            try
            {
                foreach (var item in extratos)
                {
                    // Exemplo: salvar em API TransacBanco (ou montar uma TransacBancoViewModel e enviar)

                    var transac = new MovimentoCaixaViewModel
                    {
                        DataMov = item.datamov,
                        IdCategoria = item.idCategoria ?? 0,
                        IdCentroCusto = item.idCentrocusto ?? 0,
                        IdTransacao = item.IdTransacao,
                        Valor = Math.Abs(item.Valor),
                        Observacao = item.Historico,
                        Sinal = (item.Valor < 0 ? "D" : "C"),
                        IdContaCorrente = item.idConta,
                        idmovbanco = item.id,
                        idparceiro = item.idparceiro
                    };

                    await _clienteAPI.AdicionarAsync(transac);

                    var v = await BuscaTransacao(item.IdTransacBanco, item.idbanco);
                    if (v[0] == 0)
                    {
                        await _extratoAPI.Adicionar(new TransacBancoViewModel
                        {
                            IdBanco = item.idbanco,
                            idcategoria = item.idCategoria ?? 0,
                            idTransacao = item.IdTransacao,
                            IdTransacBanco = item.IdTransacBanco,
                            idcentrocusto = item.idCentrocusto ?? 0
                        });
                    }
                }

                return Ok(new { mensagem = "Extratos salvos com sucesso." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao salvar extratos: " + ex.Message);
            }
        }
    }
}