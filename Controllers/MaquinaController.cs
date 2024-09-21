using FarmPlannerAdm.Shared;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Data;
using FarmPlannerClient.Localidade;
using FarmPlannerClient.Regiao;
using Newtonsoft.Json;
using FarmPlannerClient.DefAreas;
using FarmPlannerClient.Controller;
using FarmPlannerClient.Parceiro;

using FarmPlannerClient.PrincipioAtivo;
using FarmPlannerClient.Maquina;
using Microsoft.AspNetCore.Mvc.Rendering;
using FarmPlannerClient.ModeloMaquina;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class MaquinaController : Controller
    {
        private readonly FarmPlannerClient.Controller.MaquinaControllerClient _MaquinaAPI;

        private readonly FarmPlannerClient.Controller.ModeloMaquinaControllerClient _modeloAPI;

        private readonly SessionManager _sessionManager;

        public MaquinaController(MaquinaControllerClient maquinaAPI, ModeloMaquinaControllerClient modeloAPI, SessionManager sessionManager)
        {
            _MaquinaAPI = maquinaAPI;
            _modeloAPI = modeloAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(int? idmodelo, string? filtro)
        {
            Task<List<ListModeloMaquinaViewModel>> retm = _modeloAPI.Lista(0, "");
            List<ListModeloMaquinaViewModel> p = await retm;

            ViewBag.modelos = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOption = idmodelo.ToString();
            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            return View();
        }

        public async Task<JsonResult> GetData(int idmodelo, string? filtro)
        {
            Task<List<ListMaquinaViewModel>> ret = _MaquinaAPI.Lista(idmodelo, _sessionManager.contaguid, _sessionManager.idorganizacao, filtro);
            List<ListMaquinaViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            MaquinaViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new MaquinaViewModel();
                c.idconta = _sessionManager.contaguid;
                ViewBag.Titulo = "Adicionar uma Maquina ";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<MaquinaViewModel> ret = _MaquinaAPI.ListaById(id, _sessionManager.contaguid);
                c = await ret;

                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar uma Maquina";
                    ViewBag.Acao = "editar";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir uma Maquina";
                    ViewBag.Acao = "excluir";
                }
                if (acao == 4)
                {
                    ViewBag.Titulo = "Visualizar";
                    ViewBag.Acao = "ver";
                }
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<MaquinaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<ListModeloMaquinaViewModel>> retm = _modeloAPI.Lista(0, "");
            List<ListModeloMaquinaViewModel> p = await retm;

            ViewBag.modelos = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, MaquinaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;

            var response = await _MaquinaAPI.Salvar(id, _sessionManager.contaguid, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionar", new { acao = 1, id = 0 });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Adicionar(MaquinaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.idorganizacao = _sessionManager.idorganizacao;

            var response = await _MaquinaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                //  string y = await response.Content.ReadAsStringAsync();
                //  var result = JsonSerializer.Deserialize<FarmPlannerClient.Defarea.MaquinaViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionar", new { acao = 1, id = 0 });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Excluir(int id, MaquinaViewModel dados)
        {
            //FarmPlannerClient.DefAreas.DefAreasViewModel dados=new FarmPlannerClient.DefAreas.DefAreasViewModel();
            var response = await _MaquinaAPI.Excluir(id, _sessionManager.contaguid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }
    }
}