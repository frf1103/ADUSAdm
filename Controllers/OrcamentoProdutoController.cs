using FarmPlannerAdm.Shared;
using FarmPlannerClient.ConfigArea;
using FarmPlannerClient.Controller;
using FarmPlannerClient.DefAreas;
using FarmPlannerClient.Organizacao;

using FarmPlannerClient.Safra;
using FarmPlannerClient.Variedade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using System.Text.Json;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;
using FarmPlannerClient.Operacao;
using FarmPlannerClient.PrincipioAtivo;
using FarmPlannerClient.ModeloMaquina;
using Microsoft.IdentityModel.Abstractions;
using FarmPlannerClient.Produto;
using FarmPlannerClient.OrcamentoProduto;
using FarmPlannerClient.Moeda;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class OrcamentoProdutoController : Controller
    {
        private readonly FarmPlannerClient.Controller.OrcamentoProdutoControllerClient _OrcamentoProduto;
        private readonly FarmPlannerClient.Controller.CulturaControllerClient _culturaAPI;
        private readonly FarmPlannerClient.Controller.FazendaControllerClient _fazendaAPI;
        private readonly FarmPlannerClient.Controller.AnoAgricolaControllerClient _anoagricolaAPI;
        private readonly FarmPlannerClient.Controller.PrincipioAtivoControllerClient _principioAPI;
        private readonly FarmPlannerClient.Controller.ProdutoControllerClient _produtoAPI;
        private readonly FarmPlannerClient.Controller.MoedaControllerClient _moedaAPI;

        private readonly SessionManager _sessionManager;

        public OrcamentoProdutoController(OrcamentoProdutoControllerClient OrcamentoProduto, CulturaControllerClient culturaAPI, FazendaControllerClient fazendaAPI, AnoAgricolaControllerClient anoagricolaAPI, SessionManager sessionManager, ConfigAreaControllerClient configArea, OperacaoControllerClient operacao, PrincipioAtivoControllerClient principioAPI, ProdutoControllerClient produtoAPI, ModeloMaquinaControllerClient modeloAPI, MoedaControllerClient moedaAPI)
        {
            _OrcamentoProduto = OrcamentoProduto;
            _culturaAPI = culturaAPI;
            _fazendaAPI = fazendaAPI;
            _anoagricolaAPI = anoagricolaAPI;
            _sessionManager = sessionManager;

            _principioAPI = principioAPI;
            _produtoAPI = produtoAPI;
            _moedaAPI = moedaAPI;
        }

        private const int TAMANHO_PAGINA = 5;

        public async Task<IActionResult> Index(int idsafra, int idfazenda, int idprincipio, int idproduto, string? filtro = "")
        {
            Task<List<PrincipioAtivoViewModel>> retp = _principioAPI.Lista("");

            List<PrincipioAtivoViewModel> p = await retp;

            ViewBag.principios = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListProdutoViewModel>> retpr = _produtoAPI.Lista(0, 0, 0, _sessionManager.contaguid, "", -1);
            List<ListProdutoViewModel> pr = await retpr;

            ViewBag.produtos = pr.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionS = idsafra.ToString();
            ViewBag.SelectedOptionF = idfazenda.ToString();
            ViewBag.SelectedOptionPr = idproduto.ToString();
            ViewBag.SelectedOptionP = idprincipio.ToString();
            ViewBag.idproduto = idproduto;
            ViewBag.filtro = filtro;

            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            return View();
        }

        public async Task<JsonResult> GetData(int idfazenda, int idsafra, int idprincipio, int idproduto, string? filtro)
        {
            Task<List<ListOrcamentoProdutoViewModel>> ret = _OrcamentoProduto.Lista(idfazenda, idsafra, _sessionManager.contaguid, idprincipio, idproduto, filtro);
            List<ListOrcamentoProdutoViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> adicionar(int idfaz = 0, int idsafra = 0, int acao = 1, int id = 0, int idprincipio = 0, int idproduto = 0)
        {
            OrcamentoProdutoViewModel c;

            ViewBag.idacao = acao;

            ViewBag.SelectedOptionP = idprincipio.ToString();
            ViewBag.SelectedOptionPr = idproduto.ToString();

            if (acao == 1)
            {
                c = new OrcamentoProdutoViewModel();
                //   c.idfazenda = idfaz;
                //   c.idSafra = idsafra;

                ViewBag.Titulo = "Adicionar";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<OrcamentoProdutoViewModel> ret = _OrcamentoProduto.ListaById(id, _sessionManager.contaguid);
                c = await ret;

                //    ViewBag.idvariedade = c.idVariedade;
                //    ViewBag.idtalhao = c.idTalhao;
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar";
                    ViewBag.Acao = "editar";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir";
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
                    c = JsonConvert.DeserializeObject<OrcamentoProdutoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }
            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListProdutoViewModel>> retpr = _produtoAPI.Lista(0, 0, 0, _sessionManager.contaguid, "", -1);
            List<ListProdutoViewModel> pr = await retpr;

            ViewBag.produtos = pr.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });
            Task<List<PrincipioAtivoViewModel>> retp = _principioAPI.Lista("");

            List<PrincipioAtivoViewModel> p = await retp;

            ViewBag.principios = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.id = id;

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, OrcamentoProdutoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _OrcamentoProduto.Salvar(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(OrcamentoProdutoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _OrcamentoProduto.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<OrcamentoProdutoViewModel>(y);
                return RedirectToAction(nameof(Adicionar), new { acao = 2, id = result.id });
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
        public async Task<IActionResult> Excluir(int id, OrcamentoProdutoViewModel dados)
        {
            //FarmPlannerClient.OrcamentoProduto.OrcamentoProdutoViewModel dados=new FarmPlannerClient.OrcamentoProduto.OrcamentoProdutoViewModel();
            var response = await _OrcamentoProduto.Excluir(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }

        public async Task<JsonResult> GetProdOrc(int idorc, int idprincipio = 0, int idproduto = 0)
        {
            Task<List<ListProdutoOrcamentoViewModel>> ret = _OrcamentoProduto.ListaProduto(idorc, idprincipio, idproduto, _sessionManager.contaguid);
            List<ListProdutoOrcamentoViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> adicionarproduto(int idorcamento, int acao = 1, int id = 0,int origem=0)
        {
            ProdutoOrcamentoViewModel c;

            ViewBag.idacao = acao;
            ViewBag.idorc = idorcamento;

            if (acao == 1)
            {
                c = new ProdutoOrcamentoViewModel();
                //   c.idfazenda = idfaz;
                //   c.idSafra = idsafra;

                ViewBag.idproduto = 0;

                ViewBag.Titulo = "Adicionar";
                ViewBag.Acao = "adicionarproduto";
            }
            else
            {
                Task<ProdutoOrcamentoViewModel> ret = _OrcamentoProduto.ListaProdutoById(id, _sessionManager.contaguid);
                c = await ret;
                ViewBag.idorc = c.idOrcamento;
                ViewBag.idproduto = c.idProduto;

                //    ViewBag.idvariedade = c.idVariedade;
                //    ViewBag.idtalhao = c.idTalhao;
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar";
                    ViewBag.Acao = "editarproduto";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir";
                    ViewBag.Acao = "excluirproduto";
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
                    c = JsonConvert.DeserializeObject<ProdutoOrcamentoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<PrincipioAtivoViewModel>> retp = _principioAPI.Lista("");

            List<PrincipioAtivoViewModel> p = await retp;

            ViewBag.principios = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.tipos = new[] {
                new SelectListItem { Text = "Insumo", Value = "0" },
                new SelectListItem { Text = "Combustível", Value ="1" }
            };

            Task<List<MoedaViewModel>> retm = _moedaAPI.ListaMoeda("");
            List<MoedaViewModel> m = await retm;

            ViewBag.moedas = m.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });
            ViewBag.origem = origem;
            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> EditarProduto(int id, int origem,ProdutoOrcamentoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _OrcamentoProduto.SalvarProduto(id, _sessionManager.contaguid, dados);

            if (response.IsSuccessStatusCode)
            {
                if (origem == 0)
                {
                    return RedirectToAction("adicionar", new { acao = 2, id = dados.idOrcamento });
                }
                else
                {
                    return RedirectToAction("index");
                }
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionarproduto", new { acao = 1, id = 0 });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> AdicionarProduto(ProdutoOrcamentoViewModel dados,int origem= 0)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _OrcamentoProduto.AdicionarProduto(dados);
            ;
            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ProdutoOrcamentoViewModel>(y);
                return RedirectToAction(nameof(AdicionarProduto), new { acao = 1, idorcamento = result.idOrcamento });
                
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionarproduto", new { acao = 1, id = 0 });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> ExcluirProduto(int id, ProdutoOrcamentoViewModel dados, int origem = 0)
        {
            //FarmPlannerClient.OrcamentoProduto.OrcamentoProdutoViewModel dados=new FarmPlannerClient.OrcamentoProduto.OrcamentoProdutoViewModel();
            var response = await _OrcamentoProduto.ExcluirProduto(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                if (origem == 1)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("adicionar", new { acao = 2, id = dados.idOrcamento });
                }
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionarproduto");
        }
    }
}