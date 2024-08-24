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
using FarmPlannerClient.Operacao;
using Microsoft.AspNetCore.Mvc.Rendering;
using FarmPlannerClient.TipoOperacao;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC")]
    public class OperacaoController : Controller
    {
        private readonly FarmPlannerClient.Controller.OperacaoControllerClient _OperacaoAPI;

        private readonly FarmPlannerClient.Controller.TipoOperacaoControllerClient _tipoAPI;
        private readonly SessionManager _sessionManager;

        public OperacaoController(OperacaoControllerClient operacaoAPI, TipoOperacaoControllerClient tipoAPI, SessionManager sessionManager)
        {
            _OperacaoAPI = operacaoAPI;
            _tipoAPI = tipoAPI;
            _sessionManager = sessionManager;
        }

        private const int TAMANHO_PAGINA = 5;

        public async Task<IActionResult> Index(int? idtipo, string? filtro, int pagina = 1)
        {
            Task<List<TipoOperacaoViewModel>> retpr = _tipoAPI.Lista("");
            List<TipoOperacaoViewModel> pr = await retpr;

            ViewBag.tipos = pr.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOption = idtipo.ToString();
            ViewBag.filtro = filtro;

            return View();
        }

        public async Task<JsonResult> GetData(int idtipo, string? filtro)
        {
            Task<List<OperacaoViewModel>> ret = _OperacaoAPI.Lista(_sessionManager.contaguid, idtipo, filtro);
            List<OperacaoViewModel> c = await ret;

            return Json(c);
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            OperacaoViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new OperacaoViewModel();
                c.idconta = _sessionManager.contaguid;
                ViewBag.Titulo = "Adicionar uma Operacao ";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<OperacaoViewModel> ret = _OperacaoAPI.ListaById(id, _sessionManager.contaguid);
                c = await ret;

                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar uma Operacao";
                    ViewBag.Acao = "editar";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir uma Operacao";
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
                    c = JsonConvert.DeserializeObject<OperacaoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }
            Task<List<TipoOperacaoViewModel>> retpr = _tipoAPI.Lista("");
            List<TipoOperacaoViewModel> pr = await retpr;

            ViewBag.tipos = pr.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, OperacaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;

            var response = await _OperacaoAPI.Salvar(id, _sessionManager.contaguid, dados);

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

        [HttpPost]
        public async Task<IActionResult> Adicionar(OperacaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;

            var response = await _OperacaoAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                //  string y = await response.Content.ReadAsStringAsync();
                //  var result = JsonSerializer.Deserialize<FarmPlannerClient.Defarea.OperacaoViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionar", new { acao = 1, id = 0 });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, OperacaoViewModel dados)
        {
            //FarmPlannerClient.DefAreas.DefAreasViewModel dados=new FarmPlannerClient.DefAreas.DefAreasViewModel();
            var response = await _OperacaoAPI.Excluir(id, _sessionManager.contaguid);

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