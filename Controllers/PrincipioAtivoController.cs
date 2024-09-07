using FarmPlannerAdm.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text.Json;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,AdminC,User, UserV")]
    public class PrincipioAtivoController : Controller
    {
        private readonly FarmPlannerClient.Controller.PrincipioAtivoControllerClient _culturaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public PrincipioAtivoController(FarmPlannerClient.Controller.PrincipioAtivoControllerClient culturaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel>> ret = _culturaAPI.Lista(filtro);
            List<FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel> c = await ret;

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)c.Count() / TAMANHO_PAGINA);
            return View(c.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar()
        {
            FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel c = new FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel();

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, int acao = 0)
        {
            Task<FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel c = await ret;

            ViewBag.Id = id;

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Excluir(int id)
        {
            Task<FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel c = await ret;

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel dados)
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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel dados)
        {
            // dados.idconta = _sessionManager.contaguid;
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel dados)
        {
            //FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel dados=new FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel();
            var response = await _culturaAPI.Excluir(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("excluir");
        }

        public async Task<JsonResult> GetData(string? filtro)
        {
            Task<List<FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel>> ret = _culturaAPI.Lista(filtro);
            List<FarmPlannerClient.PrincipioAtivo.PrincipioAtivoViewModel> c = await ret;

            return Json(c);
        }
    }
}