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
using FarmPlannerClient.Comercializacao;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class ComercializacaoController : Controller
    {
        private readonly FarmPlannerClient.Controller.ComercializacaoControllerClient _Comercializacao;
        private readonly FarmPlannerClient.Controller.CulturaControllerClient _culturaAPI;
        private readonly FarmPlannerClient.Controller.FazendaControllerClient _fazendaAPI;
        private readonly FarmPlannerClient.Controller.AnoAgricolaControllerClient _anoagricolaAPI;

        private readonly FarmPlannerClient.Controller.ProdutoControllerClient _produtoAPI;
        private readonly FarmPlannerClient.Controller.MoedaControllerClient _moedaAPI;

        private readonly FarmPlannerClient.Controller.ParceiroControllerClient _parceiroAPI;
        private readonly SessionManager _sessionManager;

        public ComercializacaoController(ComercializacaoControllerClient Comercializacao, CulturaControllerClient culturaAPI, FazendaControllerClient fazendaAPI, AnoAgricolaControllerClient anoagricolaAPI, SessionManager sessionManager, ConfigAreaControllerClient configArea, ProdutoControllerClient produtoAPI, MoedaControllerClient moedaAPI, ParceiroControllerClient parceiroAPI)
        {
            _Comercializacao = Comercializacao;

            _fazendaAPI = fazendaAPI;
            _anoagricolaAPI = anoagricolaAPI;
            _sessionManager = sessionManager;

            _produtoAPI = produtoAPI;
            _moedaAPI = moedaAPI;
            _parceiroAPI = parceiroAPI;
        }

        private const int TAMANHO_PAGINA = 5;

        public async Task<IActionResult> Index(int idsafra, int idfazenda, int idmoeda, int idfornec, DateTime dtinicio, DateTime dtfim, string? filtro = "")
        {
            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListParceiroViewModel>> retp = _parceiroAPI.Lista(_sessionManager.contaguid, "");
            List<ListParceiroViewModel> p = await retp;

            ViewBag.fornecedores = p.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            Task<List<MoedaViewModel>> retm = _moedaAPI.ListaMoeda("");
            List<MoedaViewModel> m = await retm;

            ViewBag.moedas = m.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionS = idsafra.ToString();
            ViewBag.SelectedOptionF = idfazenda.ToString();

            ViewBag.SelectedOptionFr = idfornec.ToString();
            ViewBag.SelectedOptionM = idmoeda.ToString();

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
                ViewBag.dtfim = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtfim = dtfim.ToString("yyyy-MM-dd");
            }

            return View();
        }

        public async Task<JsonResult> GetData(int idfazenda, int idsafra, int idproduto, int idmoeda, int idfornec, DateTime ini, DateTime fim, string? filtro)
        {
            Task<List<ListComercializacaoViewModel>> ret = _Comercializacao.Lista(_sessionManager.idorganizacao, _sessionManager.idanoagricola, idfazenda, idsafra, _sessionManager.contaguid, idfornec, idmoeda, ini, fim, filtro);
            List<ListComercializacaoViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> adicionar(int idfaz = 0, int idsafra = 0, int acao = 1, int id = 0)
        {
            ComercializacaoViewModel c;

            ViewBag.idacao = acao;

            if (acao == 1)
            {
                c = new ComercializacaoViewModel();
                //   c.idfazenda = idfaz;
                //   c.idSafra = idsafra;

                ViewBag.Titulo = "Adicionar";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<ComercializacaoViewModel> ret = _Comercializacao.ListaById(id, _sessionManager.contaguid);
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
                    c = JsonConvert.DeserializeObject<ComercializacaoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }
            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListParceiroViewModel>> retp = _parceiroAPI.Lista(_sessionManager.contaguid, "");
            List<ListParceiroViewModel> p = await retp;

            ViewBag.fornecedores = p.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            Task<List<MoedaViewModel>> retm = _moedaAPI.ListaMoeda("");
            List<MoedaViewModel> m = await retm;

            ViewBag.moedas = m.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.id = id;

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, ComercializacaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _Comercializacao.Salvar(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(ComercializacaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _Comercializacao.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ComercializacaoViewModel>(y);
                return RedirectToAction(nameof(Adicionar), new { acao = 1, id = 0 });
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
        public async Task<IActionResult> Excluir(int id, ComercializacaoViewModel dados)
        {
            //FarmPlannerClient.Comercializacao.ComercializacaoViewModel dados=new FarmPlannerClient.Comercializacao.ComercializacaoViewModel();
            var response = await _Comercializacao.Excluir(id, _sessionManager.contaguid, _sessionManager.uid);

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

        public async Task<JsonResult> GetEntrega(int id)
        {
            Task<List<ItemEntregaContratoViewModel>> ret = _Comercializacao.ListaItensEntrega(id, _sessionManager.contaguid);
            List<ItemEntregaContratoViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<JsonResult> GravaEntrega([FromBody] List<EntregaContratoViewModel> dados)
        {
            if (dados != null)
            {
                foreach (var xd in dados)
                {
                    xd.uid = _sessionManager.uid;
                    xd.idconta = _sessionManager.contaguid;

                    var response = await _Comercializacao.AdicionarEntrega(xd);

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

        public async Task<JsonResult> GetEntregabyProduto(int id)
        {
            Task<List<EntregaContratoViewModel>> ret = _Comercializacao.ListaEntregaByComercializacao(id, _sessionManager.contaguid);
            List<EntregaContratoViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<JsonResult> CancelarEntrega([FromBody] List<EntregaContratoViewModel> dados)
        {
            if (dados != null)
            {
                foreach (var xd in dados)
                {
                    var response = await _Comercializacao.ExcluirEntregaById(xd.id, _sessionManager.contaguid, _sessionManager.uid);

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