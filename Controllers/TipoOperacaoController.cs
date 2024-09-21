using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;
using FarmPlannerClient.TipoOperacao;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,AdminC,User,UserV")]
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
            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole == "Admin");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Adicionar()
        {
            FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel c = new FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel();

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, int acao = 2)
        {
            Task<FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel c = await ret;

            ViewBag.Id = id;
            ViewBag.acao = acao;

            if ((acao == 2 && User.IsInRole("Admin")) || acao == 4)
            {
                return View(c);
            }
            else
            {
                return View("AccessDenied");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Excluir(int id)
        {
            Task<FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.TipoOperacao.TipoOperacaoViewModel c = await ret;

            return View(c);
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        public async Task<JsonResult> GetData(string? filtro)
        {
            Task<List<TipoOperacaoViewModel>> ret = _culturaAPI.Lista(filtro);
            List<TipoOperacaoViewModel> c = await ret;

            return Json(c);
        }
    }
}