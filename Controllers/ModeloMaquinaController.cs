using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Azure;
using FarmPlannerClient.Enum;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;
using FarmPlannerClient.ModeloMaquina;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,AdminC,User,UserV")]
    public class ModeloMaquinaController : Controller
    {
        private readonly FarmPlannerClient.Controller.ModeloMaquinaControllerClient _culturaAPI;
        private readonly FarmPlannerClient.Controller.MarcaMaquinaControllerClient _marcaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public ModeloMaquinaController(FarmPlannerClient.Controller.ModeloMaquinaControllerClient culturaAPI, FarmPlannerClient.Controller.MarcaMaquinaControllerClient marcaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _marcaAPI = marcaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro, int idmarca, int pagina = 1)
        {
            Task<List<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel>> ret1 = _marcaAPI.Lista("");
            List<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel> t = await ret1;

            ViewBag.Marcas = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOption = idmarca.ToString();
            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole == "Admin");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Adicionar()
        {
            FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel c = new FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel();
            Task<List<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel>> ret1 = _marcaAPI.Lista("");
            List<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel> t = await ret1;

            ViewBag.Marcas = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });
            ViewBag.Combustiveis = new[] {
                new SelectListItem { Text = "Diesel BS500", Value = Combustivel.DIESEBS500.ToString() },
                new SelectListItem { Text = "Diesel S10", Value = Combustivel.DIESES10.ToString() },
                new SelectListItem { Text = "Gasolina", Value = Combustivel.GASOLINA.ToString() },
                new SelectListItem { Text = "Etanol", Value = Combustivel.ETANOL.ToString() },
                new SelectListItem { Text = "Jet A1", Value = Combustivel.JETA1.ToString() },
                new SelectListItem { Text = "Querosene", Value = Combustivel.QUEROSENE.ToString() }
            };

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, int acao = 2)
        {
            Task<FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel c = await ret;

            ViewBag.Id = id;
            Task<List<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel>> ret1 = _marcaAPI.Lista("");
            List<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel> t = await ret1;

            ViewBag.Marcas = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });
            ViewBag.Combustiveis = new[] {
                new SelectListItem { Text = "Diesel BS500", Value = Combustivel.DIESEBS500.ToString() },
                new SelectListItem { Text = "Diesel S10", Value = Combustivel.DIESES10.ToString() },
                new SelectListItem { Text = "Gasolina", Value = Combustivel.GASOLINA.ToString() },
                new SelectListItem { Text = "Etanol", Value = Combustivel.ETANOL.ToString() },
                new SelectListItem { Text = "Jet A1", Value = Combustivel.JETA1.ToString() },
                new SelectListItem { Text = "Querosene", Value = Combustivel.QUEROSENE.ToString() }
            };
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
            Task<FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel c = await ret;
            Task<List<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel>> ret1 = _marcaAPI.Lista("");
            List<FarmPlannerClient.MarcaMaquina.MarcaMaquinaViewModel> t = await ret1;

            ViewBag.Marcas = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });
            ViewBag.Combustiveis = new[] {
                new SelectListItem { Text = "Diesel BS500", Value = Combustivel.DIESEBS500.ToString() },
                new SelectListItem { Text = "Diesel S10", Value = Combustivel.DIESES10.ToString() },
                new SelectListItem { Text = "Gasolina", Value = Combustivel.GASOLINA.ToString() },
                new SelectListItem { Text = "Etanol", Value = Combustivel.ETANOL.ToString() },
                new SelectListItem { Text = "Jet A1", Value = Combustivel.JETA1.ToString() },
                new SelectListItem { Text = "Querosene", Value = Combustivel.QUEROSENE.ToString() }
            };

            return View(c);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel dados)
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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel dados)
        {
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel dados)
        {
            //FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel dados=new FarmPlannerClient.ModeloMaquina.ModeloMaquinaViewModel();
            var response = await _culturaAPI.Excluir(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("excluir");
        }

        public async Task<JsonResult> GetData(string? filtro, int idmarca = 0)
        {
            Task<List<ListModeloMaquinaViewModel>> ret = _culturaAPI.Lista(idmarca, filtro);
            List<ListModeloMaquinaViewModel> c = await ret;

            return Json(c);
        }
    }
}