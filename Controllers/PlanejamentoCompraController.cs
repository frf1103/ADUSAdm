using FarmPlannerAdm.Shared;
using FarmPlannerClient.ConfigArea;
using FarmPlannerClient.Controller;
using FarmPlannerClient.DefAreas;
using FarmPlannerClient.Organizacao;
using FarmPlannerClient.PlanejamentoCompra;
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

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class PlanejamentoCompraController : Controller
    {
        private readonly FarmPlannerClient.Controller.PlanejamentoCompraControllerClient _PlanejamentoCompra;

        private readonly FarmPlannerClient.Controller.FazendaControllerClient _fazendaAPI;
        private readonly FarmPlannerClient.Controller.AnoAgricolaControllerClient _anoagricolaAPI;
        private readonly FarmPlannerClient.Controller.PrincipioAtivoControllerClient _principioAPI;
        private readonly FarmPlannerClient.Controller.ProdutoControllerClient _produtoAPI;
        private readonly SessionManager _sessionManager;

        public PlanejamentoCompraController(PlanejamentoCompraControllerClient PlanejamentoCompra, FazendaControllerClient fazendaAPI, AnoAgricolaControllerClient anoagricolaAPI, SessionManager sessionManager, PrincipioAtivoControllerClient principioAPI, ProdutoControllerClient produtoAPI)
        {
            _PlanejamentoCompra = PlanejamentoCompra;

            _fazendaAPI = fazendaAPI;
            _anoagricolaAPI = anoagricolaAPI;
            _principioAPI = principioAPI;

            _sessionManager = sessionManager;
            _produtoAPI = produtoAPI;
        }

        private const int TAMANHO_PAGINA = 5;

        public async Task<IActionResult> Index(int idsafra, int idfazenda, int idprincipio)
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

            ViewBag.SelectedOptionS = idsafra.ToString();
            ViewBag.SelectedOptionF = idfazenda.ToString();

            ViewBag.SelectedOptionP = idprincipio.ToString();

            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            return View();
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        public async Task<JsonResult> GetPlanejamento(int idfazenda, int idsafra, int idprincipio, int idproduto)
        {
            Task<List<ListPlanejamentoCompraViewModel>> ret = _PlanejamentoCompra.Lista(_sessionManager.idorganizacao, _sessionManager.contaguid, _sessionManager.idanoagricola, idprincipio, idfazenda, idsafra, idproduto);
            List<FarmPlannerClient.PlanejamentoCompra.ListPlanejamentoCompraViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> adicionar(int idfaz = 0, int idsafra = 0, int acao = 1, int id = 0)
        {
            FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel();
                //   c.idfazenda = idfaz;
                //   c.idSafra = idsafra;

                ViewBag.Titulo = "Adicionar";
                ViewBag.Acao = "adicionar";
                ViewBag.idvariedade = "0";
                ViewBag.idtalhao = "0";
                ViewBag.area = 0.00;
            }
            else
            {
                Task<FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel> ret = _PlanejamentoCompra.ListaById(id, _sessionManager.contaguid);
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
                    c = JsonConvert.DeserializeObject<PlanejamentoCompraViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListProdutoViewModel>> retp = _produtoAPI.Lista(0, 0, 0, _sessionManager.contaguid, "", -1);
            List<ListProdutoViewModel> p = await retp;

            ViewBag.produtos = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _PlanejamentoCompra.Salvar(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _PlanejamentoCompra.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel>(y);
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
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel dados)
        {
            //FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel dados=new FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel();
            var response = await _PlanejamentoCompra.Excluir(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        public async Task<JsonResult> GravarLinha([FromBody] LinhaPlanejCompraVideModel dadosP)
        {
            if (dadosP != null)
            {
                Task<FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel> ret = _PlanejamentoCompra.ListaById(dadosP.id, _sessionManager.contaguid);
                FarmPlannerClient.PlanejamentoCompra.PlanejamentoCompraViewModel dados = await ret;

                dados.idconta = _sessionManager.contaguid;
                dados.uid = _sessionManager.uid;
                //                dados.idPrincipio = dados.idPrincipio;
                //                dados.idSafra = dadosP.idSafra;
                //                dados.idFazenda = dadosP.idFazenda;
                dados.id = dadosP.id;
                dados.qtdComprada = dadosP.qtdComprada;
                dados.qtdComprar = dadosP.qtdComprar;
                dados.qtdEstoque = dadosP.qtdEstoque;
                dados.qtdNecessaria = dadosP.qtdNecessaria;
                dados.saldo = dadosP.saldo;
                dados.uid = _sessionManager.uid;

                var response = await _PlanejamentoCompra.Salvar(dadosP.id, _sessionManager.contaguid, dados);

                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        return Json(new { success = true, message = "Dados gravados com sucesso!" });
                    }
                    else
                    {
                        string x = await response.Content.ReadAsStringAsync();
                        return Json(new { success = false, message = x });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Erro na gravaçao" });
                }
            }
            else
            {
                return Json(new { success = false, message = "Dados inválidos." });
            }
        }

        /*
        public async Task<JsonResult> GetArea(int idtalhao, decimal area)
        {
            Task<List<ListPlanejamentoOperacaoViewModel>> ret = _PlanejamentoCompra.Lista(_sessionManager.idanoagricola, 0, idtalhao, 0, 0, _sessionManager.contaguid, _sessionManager.idorganizacao);
            List<FarmPlannerClient.PlanejamentoCompra.ListPlanejamentoOperacaoViewModel> c = await ret;
            decimal areaconf = c.Sum(a => a.area);

            return Json(new { area = area - areaconf });
        }
        */

        //rotinas dos produtos
    }
}