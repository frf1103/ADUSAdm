using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TipoOperacaoController : Controller
    {
        private readonly FarmPlannerClient.Controller.TipoOperacaoControllerClient _culturaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public TipoOperacaoController(FarmPlannerClient.Controller.TipoOperacaoControllerClient culturaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel>> ret = _culturaAPI.Lista(filtro);
            List<FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel> c = await ret;

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)c.Count() / TAMANHO_PAGINA);
            return View(c.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar()
        {
            FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel c = new FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel();

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            Task<FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel c = await ret;

            ViewBag.Id = id;

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Excluir(int id)
        {
            Task<FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel c = await ret;

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel dados)
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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel dados)
        {
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel dados)
        {
            //FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel dados=new FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel();
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