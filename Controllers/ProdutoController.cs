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
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC")]
    public class ProdutoController : Controller
    {
        private readonly FarmPlannerClient.Controller.ProdutoControllerClient _ProdutoAPI;
        private readonly FarmPlannerClient.Controller.PrincipioAtivoControllerClient _principioAPI;
        private readonly FarmPlannerClient.Controller.GrupoProdutoControllerClient _grupoAPI;
        private readonly FarmPlannerClient.Controller.ParceiroControllerClient _parceiroAPI;
        private readonly FarmPlannerClient.Controller.UnidadeControllerClient _unidadeAPI;

        private readonly SessionManager _sessionManager;

        public ProdutoController(ProdutoControllerClient produtoAPI, PrincipioAtivoControllerClient principioAPI, GrupoProdutoControllerClient grupoAPI, ParceiroControllerClient parceiroAPI, SessionManager sessionManager, UnidadeControllerClient unidadeAPI)
        {
            _ProdutoAPI = produtoAPI;
            _principioAPI = principioAPI;
            _grupoAPI = grupoAPI;
            _parceiroAPI = parceiroAPI;
            _sessionManager = sessionManager;
            _unidadeAPI = unidadeAPI;
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

            Task<List<PrincipioAtivoViewModel>> retpr = _principioAPI.Lista("");
            List<PrincipioAtivoViewModel> pr = await retpr;

            ViewBag.principios = pr.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionP = idfab.ToString();
            ViewBag.SelectedOptionG = idgrupo.ToString();
            ViewBag.SelectedOptionPR = idprincipio.ToString();
            ViewBag.filtro = filtro;

            return View();
        }

        public async Task<JsonResult> GetData(int idgrupo, int idfab, int idprincipio, string? filtro, int tipo = -1)
        {
            Task<List<ListProdutoViewModel>> ret = _ProdutoAPI.Lista(idgrupo, idprincipio, idfab, _sessionManager.contaguid, filtro, tipo);
            List<ListProdutoViewModel> c = await ret;

            return Json(c);
        }

        public async Task<JsonResult> Getprincipioproduto(int idproduto)
        {
            Task<List<ProdutoPrincipioViewModel>> ret = _ProdutoAPI.ListaProdutoPrincipio(0, idproduto, _sessionManager.contaguid);
            List<ProdutoPrincipioViewModel> c = await ret;

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

            //      Task<List<PrincipioAtivoViewModel>> retpr = _principioAPI.Lista("");
            //      List<PrincipioAtivoViewModel> pr = await retpr;
            //      ViewBag.principios = pr.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<UnidadeViewModel>> retun = _unidadeAPI.Lista("");
            List<UnidadeViewModel> un = await retun;

            ViewBag.unidades = un.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, ProdutoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;

            var response = await _ProdutoAPI.Salvar(id, _sessionManager.contaguid, _sessionManager.uid, dados);

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
            dados.uid = _sessionManager.uid;

            var response = await _ProdutoAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                  string y = await response.Content.ReadAsStringAsync();
                  var result = JsonConvert.DeserializeObject<FarmPlannerClient.Produto.ProdutoViewModel>(y);
                return RedirectToAction("adicionarprodutoprincipio",new {idproduto=result.id,acao=1,descproduto=result.descricao});
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
            var response = await _ProdutoAPI.Excluir(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }

        // Tratar principios

        [HttpGet]
        public async Task<IActionResult> AdicionarProdutoPrincipio(int idproduto = 0, int idprincipio = 0, string descproduto = "", int acao = 1)
        {
            ProdutoPrincipioViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new ProdutoPrincipioViewModel();
                c.idconta = _sessionManager.contaguid;
                ViewBag.Titulo = "Adicionar um princípio ativo ao produto " + descproduto;
                ViewBag.Acao = "adicionarprodutoprincipio";
                c.idproduto = idproduto;
                c.descproduto = descproduto;
            }
            else
            {
                Task<ProdutoPrincipioViewModel> ret = _ProdutoAPI.ListaProdutoPrincipioById(idproduto, idprincipio, _sessionManager.contaguid);
                c = await ret;
                ViewBag.idpric = c.idprincipio;

                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar um princípio ativo do produto " + descproduto;
                    ViewBag.Acao = "editarprodutoprincipio";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir um princípio ativo do produto " + descproduto;
                    ViewBag.Acao = "excluirprodutoprincipio";
                }
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<ProdutoPrincipioViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<PrincipioAtivoViewModel>> retp = _principioAPI.Lista("");
            List<PrincipioAtivoViewModel> p = await retp;

            ViewBag.principios = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> EditarProdutoPrincipio(int id, ProdutoPrincipioViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _ProdutoAPI.SalvarPrincipioProduto(dados.idproduto, dados.idprincipio, _sessionManager.contaguid, _sessionManager.uid, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionarprodutoprincipio", new { acao = 1, idproduto = dados.idproduto, idprincipio = dados.idprincipio });
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarProdutoPrincipio(int idproduto, ProdutoPrincipioViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;
            dados.idproduto = idproduto;

            var response = await _ProdutoAPI.AdicionarPrincipioProduto(dados);

            if (response.IsSuccessStatusCode)
            {
                //  string y = await response.Content.ReadAsStringAsync();
                //  var result = JsonSerializer.Deserialize<FarmPlannerClient.Defarea.ProdutoViewModel>(y);
                return RedirectToAction("adicionarprodutoprincipio", new { acao = 1, idproduto = dados.idproduto, idprincipio = 0, descproduto = dados.descproduto });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionarprodutoprincipio", new { acao = 1, idproduto = dados.idproduto, idprincipio = dados.idprincipio, descproduto = dados.descproduto });
        }

        [HttpPost]
        public async Task<IActionResult> ExcluirProdutoPrincipio(ProdutoPrincipioViewModel dados)
        {
            //FarmPlannerClient.DefAreas.DefAreasViewModel dados=new FarmPlannerClient.DefAreas.DefAreasViewModel();
            var response = await _ProdutoAPI.ExcluirPrincipioProduto(dados.idproduto, dados.idprincipio, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionarprodutoprincipio");
        }
    }
}