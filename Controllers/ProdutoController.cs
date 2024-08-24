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
using FarmPlannerClient.GrupoProduto;
using FarmPlannerClient.PrincipioAtivo;
using FarmPlannerClient.Produto;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC")]
    public class ProdutoController : Controller
    {

        private readonly FarmPlannerClient.Controller.ProdutoControllerClient _ProdutoAPI;
        private readonly FarmPlannerClient.Controller.PrincipioAtivoControllerClient _principioAPI;
        private readonly FarmPlannerClient.Controller.GrupoProdutoControllerClient _grupoAPI;
        private readonly FarmPlannerClient.Controller.ParceiroControllerClient _parceiroAPI;

        private readonly SessionManager _sessionManager;

        public ProdutoController(ProdutoControllerClient produtoAPI, PrincipioAtivoControllerClient principioAPI, GrupoProdutoControllerClient grupoAPI, ParceiroControllerClient parceiroAPI, SessionManager sessionManager)
        {
            _ProdutoAPI = produtoAPI;
            _principioAPI = principioAPI;
            _grupoAPI = grupoAPI;
            _parceiroAPI = parceiroAPI;
            _sessionManager = sessionManager;
        }


        private const int TAMANHO_PAGINA = 5;

        public async Task<IActionResult> Index(int? idfab, int? idprincipio, int idgrupo, string? filtro, int pagina = 1)
        {
            Task<List<ListParceiroViewModel>> retp = _parceiroAPI.Lista(_sessionManager.contaguid, "");
            List<ListParceiroViewModel> p = await retp;

            ViewBag.fabricantes = p.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            Task<List<GrupoProdutoViewModel>> retg = _grupoAPI.Lista("");
            List<GrupoProdutoViewModel> g = await retg;

            ViewBag.grupos = g.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<PrincipioAtivoViewModel>> retpr = _principioAPI.Lista(_sessionManager.contaguid, "");
            List<PrincipioAtivoViewModel> pr = await retpr;

            ViewBag.principios = pr.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionP = idfab.ToString();
            ViewBag.SelectedOptionG = idgrupo.ToString();
            ViewBag.SelectedOptionPR = idprincipio.ToString();
            ViewBag.filtro = filtro;

            return View();
        }

        public async Task<JsonResult> GetData(int idgrupo, int idfab, int idprincipio, string? filtro)
        {
            Task<List<ListProdutoViewModel>> ret = _ProdutoAPI.Lista(idgrupo, idprincipio, idfab, _sessionManager.contaguid, filtro);
            List<ListProdutoViewModel> c = await ret;

            return Json(c);
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            ProdutoViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new ProdutoViewModel();
                c.idconta = _sessionManager.contaguid;
                ViewBag.Titulo = "Adicionar um Produto ";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<ProdutoViewModel> ret = _ProdutoAPI.ListaById(id, _sessionManager.contaguid);
                c = await ret;

                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar um Produto";
                    ViewBag.Acao = "editar";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir um Produto";
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
                    c = JsonConvert.DeserializeObject<ProdutoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<ListParceiroViewModel>> retp = _parceiroAPI.Lista(_sessionManager.contaguid, "");
            List<ListParceiroViewModel> p = await retp;

            ViewBag.fabricantes = p.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            Task<List<GrupoProdutoViewModel>> retg = _grupoAPI.Lista("");
            List<GrupoProdutoViewModel> g = await retg;

            ViewBag.grupos = g.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<PrincipioAtivoViewModel>> retpr = _principioAPI.Lista(_sessionManager.contaguid, "");
            List<PrincipioAtivoViewModel> pr = await retpr;
            ViewBag.principios = pr.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.unidades = new[] {
                new SelectListItem { Text = "LT", Value = "0" },
                new SelectListItem { Text = "KG", Value = "1" },
                new SelectListItem { Text = "UN", Value = "2" },
                new SelectListItem { Text = "SEMENTE", Value = "3" }
            };

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, ProdutoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;

            var response = await _ProdutoAPI.Salvar(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(ProdutoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;

            var response = await _ProdutoAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                //  string y = await response.Content.ReadAsStringAsync();
                //  var result = JsonSerializer.Deserialize<FarmPlannerClient.Defarea.ProdutoViewModel>(y);
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
        public async Task<IActionResult> Excluir(int id, ProdutoViewModel dados)
        {
            //FarmPlannerClient.DefAreas.DefAreasViewModel dados=new FarmPlannerClient.DefAreas.DefAreasViewModel();
            var response = await _ProdutoAPI.Excluir(id, _sessionManager.contaguid);

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