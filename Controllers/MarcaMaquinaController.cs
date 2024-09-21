using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;
using FarmPlannerClient.MarcaMaquina;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,AdminC,User,UserV")]
    public class MarcaMaquinaController : Controller
    {
        private readonly FarmPlannerClient.Controller.MarcaMaquinaControllerClient _culturaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public MarcaMaquinaController(FarmPlannerClient.Controller.MarcaMaquinaControllerClient culturaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro)
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
            FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel c = new FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel();

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, int acao = 2)
        {
            Task<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel c = await ret;

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
            Task<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel c = await ret;

            return View(c);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel dados)
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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel dados)
        {
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel dados)
        {
            //FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel dados=new FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel();
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
            Task<List<MarcaMaquinaViewModel>> ret = _culturaAPI.Lista(filtro);
            List<MarcaMaquinaViewModel> c = await ret;

            return Json(c);
        }
    }
}