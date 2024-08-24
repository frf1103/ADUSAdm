using FarmPlannerAdm.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text.Json;

namespace FarmPlannerAdm.Controllers

{
    [Authorize(Roles = "Admin")]
    public class RegiaoController : Controller
    {
        private readonly FarmPlannerClient.Controller.RegiaoControllerClient _culturaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public RegiaoController(FarmPlannerClient.Controller.RegiaoControllerClient culturaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.Regiao.RegiaoViewModel>> ret = _culturaAPI.Lista(filtro);
            List<FarmPlannerClient.Regiao.RegiaoViewModel> c = await ret;

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)c.Count() / TAMANHO_PAGINA);
            return View(c.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar()
        {
            FarmPlannerClient.Regiao.RegiaoViewModel c = new FarmPlannerClient.Regiao.RegiaoViewModel();

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            Task<FarmPlannerClient.Regiao.RegiaoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.Regiao.RegiaoViewModel c = await ret;

            ViewBag.Id = id;

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Excluir(int id)
        {
            Task<FarmPlannerClient.Regiao.RegiaoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.Regiao.RegiaoViewModel c = await ret;

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.Regiao.RegiaoViewModel dados)
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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.Regiao.RegiaoViewModel dados)
        {
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.Regiao.RegiaoViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.Regiao.RegiaoViewModel dados)
        {
            //FarmPlannerClient.Regiao.RegiaoViewModel dados=new FarmPlannerClient.Regiao.RegiaoViewModel();
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