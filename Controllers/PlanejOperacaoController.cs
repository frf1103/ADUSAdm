using FarmPlannerAdm.Shared;
using FarmPlannerClient.ConfigArea;
using FarmPlannerClient.Controller;
using FarmPlannerClient.DefAreas;
using FarmPlannerClient.Organizacao;
using FarmPlannerClient.PlanejOperacao;
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

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class PlanejOperacaoController : Controller
    {
        private readonly FarmPlannerClient.Controller.PlanejOperacaoControllerClient _PlanejOperacao;
        private readonly FarmPlannerClient.Controller.CulturaControllerClient _culturaAPI;
        private readonly FarmPlannerClient.Controller.FazendaControllerClient _fazendaAPI;
        private readonly FarmPlannerClient.Controller.AnoAgricolaControllerClient _anoagricolaAPI;
        private readonly FarmPlannerClient.Controller.ConfigAreaControllerClient _configArea;
        private readonly FarmPlannerClient.Controller.OperacaoControllerClient _operacao;
        private readonly FarmPlannerClient.Controller.PrincipioAtivoControllerClient _principioAPI;
        private readonly FarmPlannerClient.Controller.ProdutoControllerClient _produtoAPI;
        private readonly SessionManager _sessionManager;

        public PlanejOperacaoController(PlanejOperacaoControllerClient PlanejOperacao, CulturaControllerClient culturaAPI, FazendaControllerClient fazendaAPI, AnoAgricolaControllerClient anoagricolaAPI, SessionManager sessionManager, ConfigAreaControllerClient configArea, OperacaoControllerClient operacao, PrincipioAtivoControllerClient principioAPI, ProdutoControllerClient produtoAPI)
        {
            _PlanejOperacao = PlanejOperacao;
            _culturaAPI = culturaAPI;
            _fazendaAPI = fazendaAPI;
            _anoagricolaAPI = anoagricolaAPI;
            _sessionManager = sessionManager;
            _configArea = configArea;
            _operacao = operacao;
            _principioAPI = principioAPI;
            _produtoAPI = produtoAPI;
        }

        private const int TAMANHO_PAGINA = 5;

        public async Task<IActionResult> Index(int idsafra, int idvariedade, int idtalhao, int idfazenda, int idoperacao, DateTime dtinicio, DateTime dtfim)
        {
            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            //            Task<List<EditarTalhaoViewModel>> rett = _fazendaAPI.ListaTalhao(idfazenda, _sessionManager.contaguid, _sessionManager.idanoagricola, "");
            //            List<EditarTalhaoViewModel> t = await rett;

            Task<List<OperacaoViewModel>> reto = _operacao.Lista(_sessionManager.contaguid, 0, "");
            List<OperacaoViewModel> o = await reto;

            ViewBag.operacoes = o.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            //ViewBag.talhoes = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionS = idsafra.ToString();
            ViewBag.SelectedOptionF = idfazenda.ToString();
            ViewBag.SelectedOptionT = idtalhao.ToString();
            ViewBag.SelectedOptionV = idvariedade.ToString();
            ViewBag.SelectedOptionO = idoperacao.ToString();
            if (dtinicio.Year == 1)
            {
                ViewBag.dtinicio = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtinicio = dtinicio.ToString("yyyy-MM-dd");
            }
            if (dtfim.Year == 1)
            {
                ViewBag.dtfim = (DateTime.Now.AddDays(30)).ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtfim = dtfim.ToString("yyyy-MM-dd");
            }
            if (idsafra != 0)
            {
                var safra = s.Find(x => x.id == idsafra);
                ViewBag.idcultura = safra.idCultura.ToString();
            }
            return View();
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        public async Task<IActionResult> Assistente(int idsafra, int idvariedade, int idtalhao, int idfazenda)
        {
            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<PrincipioAtivoViewModel>> retp = _principioAPI.Lista("");

            List<PrincipioAtivoViewModel> p = await retp;

            ViewBag.principios = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<OperacaoViewModel>> reto = _operacao.Lista(_sessionManager.contaguid, 0, "");
            List<OperacaoViewModel> o = await reto;

            ViewBag.operacoes = o.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionO = o[0].id.ToString();
            ViewBag.SelectedOptionS = idsafra.ToString();
            ViewBag.SelectedOptionF = idfazenda.ToString();
            ViewBag.SelectedOptionT = idtalhao.ToString();
            ViewBag.SelectedOptionV = idvariedade.ToString();
            if (idsafra != 0)
            {
                var safra = s.Find(x => x.id == idsafra);
                ViewBag.idcultura = safra.idCultura.ToString();
            }

            return View();
        }

        public async Task<JsonResult> GetTalhoesDisponiveis(int idfazenda)
        {
            List<EditarTalhaoViewModel> t = new List<EditarTalhaoViewModel>();
            if (idfazenda != 0)
            {
                Task<List<EditarTalhaoViewModel>> rett = _fazendaAPI.ListaTalhaoDisponivel(idfazenda, _sessionManager.contaguid, _sessionManager.idanoagricola);
                t = await rett;
            }

            return Json(t);
        }

        public async Task<JsonResult> GetProdPlanej(int id)
        {
            Task<List<ListProdutoPlanejadoViewModel>> ret = _PlanejOperacao.ListaProdPlanej(id, _sessionManager.contaguid);
            List<FarmPlannerClient.PlanejOperacao.ListProdutoPlanejadoViewModel> c = await ret;

            return Json(c);
        }

        public async Task<JsonResult> GetData(int idano, int idfazenda, int idtalhao, int idvariedade, int idsafra, int idoperacao, DateTime ini, DateTime fim)
        {
            Task<List<ListPlanejamentoOperacaoViewModel>> ret = _PlanejOperacao.Lista(_sessionManager.idorganizacao, _sessionManager.idanoagricola, idfazenda, idtalhao, idvariedade, idsafra, _sessionManager.contaguid, idoperacao, ini, fim);
            List<FarmPlannerClient.PlanejOperacao.ListPlanejamentoOperacaoViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> adicionar(int idfaz = 0, int idsafra = 0, int acao = 1, int id = 0)
        {
            FarmPlannerClient.PlanejOperacao.PlanejamentoOperacaoViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FarmPlannerClient.PlanejOperacao.PlanejamentoOperacaoViewModel();
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
                Task<FarmPlannerClient.PlanejOperacao.PlanejamentoOperacaoViewModel> ret = _PlanejOperacao.ListaById(id, _sessionManager.contaguid);
                c = await ret;
                ViewBag.area = c.area;
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
                    c = JsonConvert.DeserializeObject<PlanejamentoOperacaoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<OperacaoViewModel>> reto = _operacao.Lista(_sessionManager.contaguid, 0, "");
            List<OperacaoViewModel> o = await reto;

            ViewBag.operacoes = o.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListConfigAreaViewModel>> retc = _configArea.Lista(_sessionManager.idanoagricola, 0, 0, 0, 0, _sessionManager.contaguid, _sessionManager.idorganizacao);
            List<ListConfigAreaViewModel> ca = await retc;

            ViewBag.configareas = ca.Select(m => new SelectListItem { Text = m.descconfig, Value = m.id.ToString() });

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.PlanejOperacao.PlanejamentoOperacaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _PlanejOperacao.Salvar(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.PlanejOperacao.PlanejamentoOperacaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _PlanejOperacao.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<FarmPlannerClient.PlanejOperacao.PlanejamentoOperacaoViewModel>(y);
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
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.PlanejOperacao.PlanejamentoOperacaoViewModel dados)
        {
            //FarmPlannerClient.PlanejOperacao.PlanejOperacaoViewModel dados=new FarmPlannerClient.PlanejOperacao.PlanejOperacaoViewModel();
            var response = await _PlanejOperacao.Excluir(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }

        /*
        public async Task<JsonResult> GetArea(int idtalhao, decimal area)
        {
            Task<List<ListPlanejamentoOperacaoViewModel>> ret = _PlanejOperacao.Lista(_sessionManager.idanoagricola, 0, idtalhao, 0, 0, _sessionManager.contaguid, _sessionManager.idorganizacao);
            List<FarmPlannerClient.PlanejOperacao.ListPlanejamentoOperacaoViewModel> c = await ret;
            decimal areaconf = c.Sum(a => a.area);

            return Json(new { area = area - areaconf });
        }
        */

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<JsonResult> GravarAssistente([FromBody] List<AssistentePlanejOperViewModel> dados)
        {
            if (dados != null)
            {
                var response = await _PlanejOperacao.GravarAssistente(_sessionManager.contaguid, _sessionManager.uid, dados);

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

        //rotinas dos produtos

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> Adicionarprodplanejado(int idplanejamento, int acao = 0, int id = 0, decimal tam = 0)
        {
            ProdutoPlanejadoViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FarmPlannerClient.PlanejOperacao.ProdutoPlanejadoViewModel();

                ViewBag.Titulo = "Adicionar";
                ViewBag.Acao = "adicionarproduto";
                c.tamanho = (decimal)tam;
            }
            else
            {
                Task<FarmPlannerClient.PlanejOperacao.ProdutoPlanejadoViewModel> ret = _PlanejOperacao.ListaProdPlanejById(id, _sessionManager.contaguid);
                c = await ret;
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
                    ViewBag.Acao = "verproduto";
                }
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<ProdutoPlanejadoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<PrincipioAtivoViewModel>> retp = _principioAPI.Lista("");
            List<PrincipioAtivoViewModel> p = await retp;

            ViewBag.principios = p.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });
            if (acao == 1)
            {
                ViewBag.idplanejamento = idplanejamento;
            }
            else
            {
                ViewBag.idplanejamento = c.idPlanejamento;
            }

            ViewBag.idproduto = c.idProduto;
            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Adicionarproduto(FarmPlannerClient.PlanejOperacao.ProdutoPlanejadoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _PlanejOperacao.AdicionarProdutoPlanejado(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<FarmPlannerClient.PlanejOperacao.ProdutoPlanejadoViewModel>(y);
                return RedirectToAction(nameof(Adicionarprodplanejado), new { acao = 1, idplanejamento = result.idPlanejamento });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionar", new { acao = 1, id = 0, idplanejamento = dados.idPlanejamento });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> EditarProduto(int id, FarmPlannerClient.PlanejOperacao.ProdutoPlanejadoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _PlanejOperacao.SalvarProdutoPlanejado(id, _sessionManager.contaguid, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("adicionar", new { id = dados.idPlanejamento, acao = 2 });
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionarproduto", new { acao = 2, id = dados.id });
        }

        public async Task<IActionResult> Excluirproduto(int id, FarmPlannerClient.PlanejOperacao.ProdutoPlanejadoViewModel dados)
        {
            //FarmPlannerClient.PlanejOperacao.PlanejOperacaoViewModel dados=new FarmPlannerClient.PlanejOperacao.PlanejOperacaoViewModel();
            var response = await _PlanejOperacao.ExcluirProdutoPlanejado(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("adicionar", new { id = dados.idPlanejamento, acao = 2 });
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return RedirectToAction("adicionarproduto", new { acao = 3, id = dados.id });
        }

    }
}