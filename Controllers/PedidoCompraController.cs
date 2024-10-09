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
using FarmPlannerClient.PedidoCompra;
using FarmPlannerClient.Moeda;
using FarmPlannerClient.Parceiro;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class PedidoCompraController : Controller
    {
        private readonly FarmPlannerClient.Controller.PedidoCompraControllerClient _PedidoCompra;
        private readonly FarmPlannerClient.Controller.CulturaControllerClient _culturaAPI;
        private readonly FarmPlannerClient.Controller.FazendaControllerClient _fazendaAPI;
        private readonly FarmPlannerClient.Controller.AnoAgricolaControllerClient _anoagricolaAPI;

        private readonly FarmPlannerClient.Controller.ProdutoControllerClient _produtoAPI;
        private readonly FarmPlannerClient.Controller.MoedaControllerClient _moedaAPI;

        private readonly FarmPlannerClient.Controller.ParceiroControllerClient _parceiroAPI;
        private readonly SessionManager _sessionManager;

        public PedidoCompraController(PedidoCompraControllerClient PedidoCompra, CulturaControllerClient culturaAPI, FazendaControllerClient fazendaAPI, AnoAgricolaControllerClient anoagricolaAPI, SessionManager sessionManager, ConfigAreaControllerClient configArea, ProdutoControllerClient produtoAPI, MoedaControllerClient moedaAPI, ParceiroControllerClient parceiroAPI)
        {
            _PedidoCompra = PedidoCompra;

            _fazendaAPI = fazendaAPI;
            _anoagricolaAPI = anoagricolaAPI;
            _sessionManager = sessionManager;

            _produtoAPI = produtoAPI;
            _moedaAPI = moedaAPI;
            _parceiroAPI = parceiroAPI;
        }

        private const int TAMANHO_PAGINA = 5;

        public async Task<IActionResult> Index(int idsafra, int idfazenda, int idproduto, int idmoeda, int idfornec, DateTime dtinicio, DateTime dtfim, string? filtro = "")
        {
            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListProdutoViewModel>> retpr = _produtoAPI.Lista(0, 0, 0, _sessionManager.contaguid, "", -1);
            List<ListProdutoViewModel> pr = await retpr;

            ViewBag.produtos = pr.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListParceiroViewModel>> retp = _parceiroAPI.Lista(_sessionManager.contaguid, "");
            List<ListParceiroViewModel> p = await retp;

            ViewBag.fornecedores = p.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            Task<List<MoedaViewModel>> retm = _moedaAPI.ListaMoeda("");
            List<MoedaViewModel> m = await retm;

            ViewBag.moedas = m.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionS = idsafra.ToString();
            ViewBag.SelectedOptionF = idfazenda.ToString();
            ViewBag.SelectedOptionPr = idproduto.ToString();
            ViewBag.SelectedOptionFr = idfornec.ToString();
            ViewBag.SelectedOptionM = idmoeda.ToString();
            ViewBag.idproduto = idproduto;
            ViewBag.filtro = filtro;

            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            if (dtinicio.Year == 1)
            {
                ViewBag.dtinicio = (DateTime.Now.AddDays(-30)).ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtinicio = dtinicio.ToString("yyyy-MM-dd");
            }
            if (dtfim.Year == 1)
            {
                ViewBag.dtfim = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtfim = dtfim.ToString("yyyy-MM-dd");
            }

            return View();
        }

        public async Task<JsonResult> GetData(int idfazenda, int idsafra, int idproduto, int idmoeda, int idfornec, DateTime ini, DateTime fim, string? filtro)
        {
            Task<List<PedidoCompraViewModel>> ret = _PedidoCompra.Lista(_sessionManager.idorganizacao, _sessionManager.idanoagricola, idfazenda, idsafra, _sessionManager.contaguid, idproduto, idmoeda, idfornec, ini, fim, filtro);
            List<PedidoCompraViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> adicionar(int idfaz = 0, int idsafra = 0, int acao = 1, int id = 0, int idproduto = 0)
        {
            PedidoCompraViewModel c;

            ViewBag.idacao = acao;

            if (acao == 1)
            {
                c = new PedidoCompraViewModel();
                //   c.idfazenda = idfaz;
                //   c.idSafra = idsafra;

                ViewBag.Titulo = "Adicionar";
                ViewBag.Acao = "adicionar";
                c.vencimento = DateTime.Now.Date;
                c.datapedido = DateTime.Now.Date;
            }
            else
            {
                Task<PedidoCompraViewModel> ret = _PedidoCompra.ListaById(id, _sessionManager.contaguid);
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
                    c = JsonConvert.DeserializeObject<PedidoCompraViewModel>(dadosJson);
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

            Task<List<ListParceiroViewModel>> retp = _parceiroAPI.Lista(_sessionManager.contaguid, "");
            List<ListParceiroViewModel> p = await retp;

            ViewBag.fornecedores = p.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            Task<List<MoedaViewModel>> retm = _moedaAPI.ListaMoeda("");
            List<MoedaViewModel> m = await retm;

            ViewBag.moedas = m.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.id = id;
            ViewBag.SelectedOptionPr = idproduto.ToString();

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, PedidoCompraViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _PedidoCompra.Salvar(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(PedidoCompraViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _PedidoCompra.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<PedidoCompraViewModel>(y);
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
        public async Task<IActionResult> Excluir(int id, PedidoCompraViewModel dados)
        {
            //FarmPlannerClient.PedidoCompra.PedidoCompraViewModel dados=new FarmPlannerClient.PedidoCompra.PedidoCompraViewModel();
            var response = await _PedidoCompra.Excluir(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionar", new { acao = 3, id = id });
        }

        public async Task<JsonResult> GetProduto(int idpedido, int idproduto = 0)
        {
            Task<List<ItemEntregaViewModel>> ret = _PedidoCompra.ListaProdutoByPedido(idpedido, idproduto, _sessionManager.contaguid);
            List<ItemEntregaViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> adicionarproduto(int idpedido, int acao = 1, int id = 0, int origem = 0)
        {
            ProdutoCompraViewModel c;

            ViewBag.idacao = acao;
            ViewBag.idpedido = idpedido;

            if (acao == 1)
            {
                c = new ProdutoCompraViewModel();
                c.idpedido = idpedido;
                //   c.idfazenda = idfaz;
                //   c.idSafra = idsafra;

                ViewBag.idproduto = 0;

                ViewBag.Titulo = "Adicionar";
                ViewBag.Acao = "adicionarproduto";
            }
            else
            {
                Task<ProdutoCompraViewModel> ret = _PedidoCompra.ListaProdutoById(id, _sessionManager.contaguid);
                c = await ret;
                ViewBag.idpedido = c.idpedido;
                ViewBag.idproduto = c.idproduto;

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
                    c = JsonConvert.DeserializeObject<ProdutoCompraViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<ListProdutoViewModel>> retp = _produtoAPI.Lista(0, 0, 0, _sessionManager.contaguid, "", -1);
            List<ListProdutoViewModel> p = await retp;

            ViewBag.produtos = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> EditarProduto(int id, int origem, ProdutoCompraViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _PedidoCompra.SalvarProduto(id, _sessionManager.contaguid, dados);

            if (response.IsSuccessStatusCode)
            {
                if (origem == 0)
                {
                    return RedirectToAction("adicionar", new { acao = 2, id = dados.idpedido });
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
        public async Task<IActionResult> AdicionarProduto(ProdutoCompraViewModel dados, int origem = 0)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _PedidoCompra.AdicionarProduto(dados);
            ;
            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ProdutoCompraViewModel>(y);
                return RedirectToAction(nameof(AdicionarProduto), new { acao = 1, idpedido = result.idpedido });
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
        public async Task<IActionResult> ExcluirProduto(int id, ProdutoCompraViewModel dados, int origem = 0)
        {
            //FarmPlannerClient.PedidoCompra.PedidoCompraViewModel dados=new FarmPlannerClient.PedidoCompra.PedidoCompraViewModel();
            var response = await _PedidoCompra.ExcluirProduto(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("adicionar", new { acao = 2, id = dados.idpedido });
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionarproduto");
        }

        public async Task<JsonResult> GetEntrega(int idpedido)
        {
            Task<List<ItemEntregaViewModel>> ret = _PedidoCompra.ListaItensEntrega(idpedido, _sessionManager.contaguid);
            List<ItemEntregaViewModel> c = await ret;

            return Json(c);
        }

        public async Task<JsonResult> GetEntregabyProduto(int id)
        {
            Task<List<EntregaCompraViewModel>> ret = _PedidoCompra.ListaEntregaByProduto(id, _sessionManager.contaguid);
            List<EntregaCompraViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<JsonResult> GravaEntrega([FromBody] List<EntregaCompraViewModel> dados)
        {
            if (dados != null)
            {
                foreach (var xd in dados)
                {
                    xd.uid = _sessionManager.uid;
                    xd.idconta = _sessionManager.contaguid;
                    xd.datains = DateTime.Now;
                    var response = await _PedidoCompra.AdicionarEntrega(xd);

                    if (response == null)
                    {
                        string x = await response.Content.ReadAsStringAsync();
                        return Json(new { success = false, message = x });
                    }
                }
                return Json(new { success = true, message = "Dados gravados com sucesso" });
            }
            else
            {
                return Json(new { success = false, message = "Dados inválidos." });
            }
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<JsonResult> CancelarEntrega([FromBody] List<EntregaCompraViewModel> dados)
        {
            if (dados != null)
            {
                foreach (var xd in dados)
                {
                    var response = await _PedidoCompra.ExcluirEntregaById(xd.id, _sessionManager.contaguid, _sessionManager.uid);

                    if (response == null)
                    {
                        string x = await response.Content.ReadAsStringAsync();
                        return Json(new { success = false, message = x });
                    }
                }
                return Json(new { success = true, message = "Dados removidos com sucesso" });
            }
            else
            {
                return Json(new { success = false, message = "Dados inválidos." });
            }
        }
    }
}