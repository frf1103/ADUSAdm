using ADUSClient.PlanoConta;
using ADUSAdm.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ADUSClient.Controller;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super")]
    public class PlanoContaController : Controller
    {
        private readonly PlanoContaControllerClient _clienteAPI;
        private readonly SessionManager _sessionManager;

        public PlanoContaController(PlanoContaControllerClient clienteAPI, SessionManager sessionManager)
        {
            _clienteAPI = clienteAPI;
            _sessionManager = sessionManager;
        }

        private const int TAMANHO_PAGINA = 10;

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            //            var lista = await _clienteAPI.ListarAsync(filtro);
            ViewBag.NumeroPagina = pagina;
            //     ViewBag.TotalPaginas = Math.Ceiling((decimal)lista.Count() / TAMANHO_PAGINA);
            ViewBag.filtro = filtro;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            PlanoContaViewModel model;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                model = new PlanoContaViewModel();
                ViewBag.Titulo = "Adicionar Plano de Conta";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                model = await _clienteAPI.BuscarPorIdAsync(id);
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar Plano de Conta";
                    ViewBag.Acao = "editar";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir Plano de Conta";
                    ViewBag.Acao = "excluir";
                }
                if (acao == 4)
                {
                    ViewBag.Titulo = "Visualizar Plano de Conta";
                    ViewBag.Acao = "ver";
                }
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    model = JsonConvert.DeserializeObject<PlanoContaViewModel>(TempData["dados"].ToString());
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            ViewBag.Sinais = new SelectList(new[]
            {
                new { Value = "D", Text = "Débito" },
                new { Value = "C", Text = "Crédito" }
            }, "Value", "Text", model?.Sinal);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(PlanoContaViewModel dados)
        {
            var response = await _clienteAPI.AdicionarAsync(dados);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var plano = JsonConvert.DeserializeObject<PlanoContaViewModel>(json);
                return RedirectToAction("Adicionar", new { acao = 2, id = plano.Id });
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);
            return View(dados);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, PlanoContaViewModel dados)
        {
            var response = await _clienteAPI.AtualizarAsync(id, dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            var erro = await response.Content.ReadAsStringAsync();
            TempData["Erro"] = erro;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            return RedirectToAction("Adicionar", new { acao = 2, id = dados.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, PlanoContaViewModel dados)
        {
            var response = await _clienteAPI.RemoverAsync(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);

            return View("Adicionar");
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string? filtro)
        {
            var lista = await _clienteAPI.ListarAsync(filtro);

            if (!string.IsNullOrEmpty(filtro))
                lista = lista.Where(p => p.Descricao != null && p.Descricao.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            return Json(lista);
        }
    }
}