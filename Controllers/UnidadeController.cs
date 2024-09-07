using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;

using FarmPlannerClient.Produto;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UnidadeController : Controller
    {
        private readonly FarmPlannerClient.Controller.UnidadeControllerClient _culturaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public UnidadeController(FarmPlannerClient.Controller.UnidadeControllerClient culturaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<UnidadeViewModel>> ret = _culturaAPI.Lista(filtro);
            List<UnidadeViewModel> c = await ret;

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)c.Count() / TAMANHO_PAGINA);
            return View(c.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar()
        {
            UnidadeViewModel c = new FarmPlannerClient.Produto.UnidadeViewModel();

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            Task<UnidadeViewModel> ret = _culturaAPI.ListaById(id);
            UnidadeViewModel c = await ret;

            ViewBag.Id = id;

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Excluir(int id)
        {
            Task<UnidadeViewModel> ret = _culturaAPI.ListaById(id);
            UnidadeViewModel c = await ret;

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, UnidadeViewModel dados)
        {
            var response = await _culturaAPI.Salvar(id, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("editar");
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(UnidadeViewModel dados)
        {
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<UnidadeViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, UnidadeViewModel dados)
        {
            //FarmPlannerClient.Unidade.UnidadeViewModel dados=new FarmPlannerClient.Unidade.UnidadeViewModel();
            var response = await _culturaAPI.Excluir(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("excluir");
        }
    }
}