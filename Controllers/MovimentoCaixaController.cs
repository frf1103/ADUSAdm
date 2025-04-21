using ADUSClient.MovimentoCaixa;
using ADUSClient.Controller;
using ADUSAdm.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

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

        public MovimentoCaixaController(
            MovimentoCaixaControllerClient clienteAPI,
            TransacaoControllerClient transacaoAPI,
            CentroCustoControllerClient centroCustoAPI,
            PlanoContaControllerClient categoriaAPI,
            ParceiroControllerClient parceiroAPI,
            ContaCorrenteControllerClient contaCorrenteAPI,
            SessionManager sessionManager)
        {
            _clienteAPI = clienteAPI;
            _transacaoAPI = transacaoAPI;
            _centroCustoAPI = centroCustoAPI;
            _categoriaAPI = categoriaAPI;
            _parceiroAPI = parceiroAPI;
            _contaCorrenteAPI = contaCorrenteAPI;
            _sessionManager = sessionManager;
        }

        private async Task CarregarViewBagsAsync()
        {
            var transacoes = await _transacaoAPI.Listar("");
            ViewBag.Transacoes = new SelectList(transacoes, "Id", "Descricao");

            var centros = await _centroCustoAPI.Listar("");
            ViewBag.CentroCustos = new SelectList(centros, "Id", "Descricao");

            var categorias = await _categoriaAPI.ListarAsync("");
            ViewBag.Categorias = new SelectList(categorias, "Id", "Descricao");

            var parceiros = await _parceiroAPI.Lista("");
            ViewBag.Parceiros = new SelectList(parceiros, "id", "razaoSocial");

            var contas = await _contaCorrenteAPI.Listar("", null);
            ViewBag.Contas = new SelectList(contas, "id", "descricao");

            ViewBag.Sinais = new SelectList(new[]
            {
                new { Valor = "D", Texto = "Débito" },
                new { Valor = "C", Texto = "Crédito" }
            }, "Valor", "Texto");
        }

        public async Task<IActionResult> Index(DateTime? ini, DateTime? fim, int? idtransacao, int? idcentrocusto, string? idparceiro, int? idcontacorrente, int? idcategoria, string? filtro)
        {
            ViewBag.permissao = (_sessionManager.userrole != "UserV");

            //var lista = await _clienteAPI.ListarAsync(ini, fim, idtransacao, idcentrocusto, idparceiro, idcontacorrente, idcategoria, filtro);

            ViewBag.ini = ini?.ToString("yyyy-MM-dd");
            ViewBag.fim = fim?.ToString("yyyy-MM-dd");
            ViewBag.idtransacao = idtransacao;
            ViewBag.idcentrocusto = idcentrocusto;
            ViewBag.idparceiro = idparceiro;
            ViewBag.idcontacorrente = idcontacorrente;
            ViewBag.idcategoria = idcategoria;
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
                _ => "Ver"
            };

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
        public async Task<JsonResult> GetData(DateTime? dataInicio, DateTime? dataFim, int? idTransacao, int? idCentroCusto, string? idParceiro, int? idContaCorrente, int? idCategoria, string? descricao)
        {
            var lista = await _clienteAPI.ListarAsync(dataInicio, dataFim, idTransacao, idCentroCusto, idParceiro, idContaCorrente, idCategoria, descricao);
            return Json(lista);
        }
    }
}