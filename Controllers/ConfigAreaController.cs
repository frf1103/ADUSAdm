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
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;

using FarmPlannerClient.ConfigArea;

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class ConfigAreaController : Controller
    {
        private readonly FarmPlannerClient.Controller.ConfigAreaControllerClient _ConfigArea;
        private readonly FarmPlannerClient.Controller.CulturaControllerClient _culturaAPI;
        private readonly FarmPlannerClient.Controller.FazendaControllerClient _fazendaAPI;
        private readonly FarmPlannerClient.Controller.AnoAgricolaControllerClient _anoagricolaAPI;
        private readonly SessionManager _sessionManager;

        public ConfigAreaController(ConfigAreaControllerClient configArea, CulturaControllerClient culturaAPI, FazendaControllerClient fazendaAPI, AnoAgricolaControllerClient anoagricolaAPI, SessionManager sessionManager)
        {
            _ConfigArea = configArea;
            _culturaAPI = culturaAPI;
            _fazendaAPI = fazendaAPI;
            _anoagricolaAPI = anoagricolaAPI;
            _sessionManager = sessionManager;
        }

        private const int TAMANHO_PAGINA = 5;

        public async Task<IActionResult> Index(int idsafra, int idvariedade, int idtalhao, int idfazenda)
        {
            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<EditarTalhaoViewModel>> rett = _fazendaAPI.ListaTalhao(idfazenda, _sessionManager.contaguid, _sessionManager.idanoagricola, "");
            List<EditarTalhaoViewModel> t = await rett;

            ViewBag.talhoes = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionS = idsafra.ToString();
            ViewBag.SelectedOptionF = idfazenda.ToString();
            ViewBag.SelectedOptionT = idtalhao.ToString();
            ViewBag.SelectedOptionV = idvariedade.ToString();
            if (idsafra != 0)
            {
                var safra = s.Find(x => x.id == idsafra);
                ViewBag.idcultura = safra.idCultura.ToString();
            }
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            return View();
        }


        public async Task<IActionResult> PlanejPlantio(int idsafra, int idvariedade, int idtalhao, int idfazenda)
        {
            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<EditarTalhaoViewModel>> rett = _fazendaAPI.ListaTalhao(idfazenda, _sessionManager.contaguid, _sessionManager.idanoagricola, "");
            List<EditarTalhaoViewModel> t = await rett;

            ViewBag.talhoes = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionS = idsafra.ToString();
            ViewBag.SelectedOptionF = idfazenda.ToString();
            ViewBag.SelectedOptionT = idtalhao.ToString();
            ViewBag.SelectedOptionV = idvariedade.ToString();
            ViewBag.userrole = _sessionManager.userrole;
            if (idsafra != 0)
            {
                var safra = s.Find(x => x.id == idsafra);
                ViewBag.idcultura = safra.idCultura.ToString();
            }
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            return View();
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        public async Task<IActionResult> Assistente(int idfazenda)
        {
            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });
            ViewBag.SelectedOptionS = s[0].id.ToString();
            ViewBag.SelectedOptionF = idfazenda.ToString();
            List<EditarTalhaoViewModel> t = new List<EditarTalhaoViewModel>();
            if (idfazenda != 0)
            {
                Task<List<EditarTalhaoViewModel>> rett = _fazendaAPI.ListaTalhaoDisponivel(idfazenda, _sessionManager.contaguid, _sessionManager.idanoagricola);
                t = await rett;
            }
            ViewBag.contaid = _sessionManager.contaguid;
            ViewBag.uid = _sessionManager.uid;

            return View(t);
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

        public async Task<JsonResult> GetData(int idano, int idfazenda, int idtalhao, int idvariedade, int idsafra)
        {
            Task<List<ListConfigAreaViewModel>> ret = _ConfigArea.Lista(_sessionManager.idanoagricola, idfazenda, idtalhao, idvariedade, idsafra, _sessionManager.contaguid, _sessionManager.idorganizacao);
            List<FarmPlannerClient.ConfigArea.ListConfigAreaViewModel> c = await ret;

            return Json(c);
        }

        public async Task<JsonResult> GetPlanejamento(int idano, int idfazenda, int idtalhao, int idvariedade, int idsafra)
        {
            Task<List<ListConfigAreaViewModel>> ret = _ConfigArea.Lista(_sessionManager.idanoagricola, idfazenda, idtalhao, idvariedade, idsafra, _sessionManager.contaguid, _sessionManager.idorganizacao);
            List<FarmPlannerClient.ConfigArea.ListConfigAreaViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> Adicionar(int idfaz = 0, int idsafra = 0, int acao = 1, int id = 0)
        {
            FarmPlannerClient.ConfigArea.ConfigAreaViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FarmPlannerClient.ConfigArea.ConfigAreaViewModel();
                c.idfazenda = idfaz;
                c.idSafra = idsafra;

                ViewBag.Titulo = "Adicionar";
                ViewBag.Acao = "adicionar";
                ViewBag.idvariedade = "0";
                ViewBag.idtalhao = "0";
            }
            else
            {
                Task<FarmPlannerClient.ConfigArea.ConfigAreaViewModel> ret = _ConfigArea.ListaById(id, _sessionManager.contaguid);
                c = await ret;
                ViewBag.idvariedade = c.idVariedade;
                ViewBag.idtalhao = c.idTalhao;
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
                    c = JsonConvert.DeserializeObject<ConfigAreaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<SafraViewModel>> rets = _anoagricolaAPI.ListaSafra(_sessionManager.idanoagricola, 0, _sessionManager.contaguid, "");
            List<SafraViewModel> s = await rets;

            ViewBag.safras = s.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<ListFazendaViewModel>> retf = _fazendaAPI.Lista(_sessionManager.idorganizacao, 0, _sessionManager.contaguid, "");
            List<ListFazendaViewModel> f = await retf;

            ViewBag.fazendas = f.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.ConfigArea.ConfigAreaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;

            var response = await _ConfigArea.Salvar(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.ConfigArea.ConfigAreaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _ConfigArea.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.ConfigArea.ConfigAreaViewModel>(y);
                return RedirectToAction(nameof(Adicionar), new { acao = 1, id = 0, idfaz = dados.idfazenda, idsafra = dados.idSafra });
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
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.ConfigArea.ConfigAreaViewModel dados)
        {
            //FarmPlannerClient.ConfigArea.ConfigAreaViewModel dados=new FarmPlannerClient.ConfigArea.ConfigAreaViewModel();
            var response = await _ConfigArea.Excluir(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }

        public async Task<JsonResult> GetArea(int idtalhao, decimal area)
        {
            Task<List<ListConfigAreaViewModel>> ret = _ConfigArea.Lista(_sessionManager.idanoagricola, 0, idtalhao, 0, 0, _sessionManager.contaguid, _sessionManager.idorganizacao);
            List<FarmPlannerClient.ConfigArea.ListConfigAreaViewModel> c = await ret;
            decimal areaconf = c.Sum(a => a.area);

            return Json(new { area = area - areaconf });
        }

        public async Task<JsonResult> GetAreaConfById(int id)
        {
            Task<ConfigAreaViewModel> ret = _ConfigArea.ListaById(id, _sessionManager.contaguid);
            FarmPlannerClient.ConfigArea.ConfigAreaViewModel c = await ret;

            return Json(new { area = c.area });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<JsonResult> GravarAssistente([FromBody] List<ConfigAreaViewModel> dados)
        {
            if (dados != null)
            {
                Task<List<string>> k = validarconfigareas(dados);
                List<string> erros = await k;
                if (erros.Count == 0)
                {
                    var response = await _ConfigArea.GravarAssistente(dados);

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
                        return Json(new { success = false, message = erros[0].ToString() });
                    }
                }
                else
                {
                    return Json(new { success = false, message = erros });
                }
            }
            else
            {
                return Json(new { success = false, message = "Dados inválidos." });
            }
        }

        [HttpGet]
        public async Task<List<string>> validarconfigareas(List<ConfigAreaViewModel> dadosP)
        {
            List<string> erros = new List<string>();
            foreach (var p in dadosP)
            {
                Task<List<ListConfigAreaViewModel>> ret = _ConfigArea.Lista(_sessionManager.idanoagricola, p.idfazenda ?? 0, p.idTalhao, p.idVariedade, p.idSafra, _sessionManager.contaguid, _sessionManager.idorganizacao);
                List<FarmPlannerClient.ConfigArea.ListConfigAreaViewModel> c = await ret;
                if (c.Count > 0)
                {
                    erros.Add("Área já configurada: " + c[0].descconfig.ToString());
                }
            }
            return erros;
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        public async Task<JsonResult> GravarPlanejPlantio([FromBody] List<PlanejPlantioViewModel> dadosP)
        {
            if (dadosP != null)
            {
                Task<FarmPlannerClient.ConfigArea.ConfigAreaViewModel> ret = _ConfigArea.ListaById(dadosP[0].id, _sessionManager.contaguid);
                FarmPlannerClient.ConfigArea.ConfigAreaViewModel dados = await ret;

                dados.idconta = _sessionManager.contaguid;
                dados.uid = _sessionManager.uid;
                dados.populacaoRecomendada = dadosP[0].populacaoRecomendada;
                dados.pms = dadosP[0].pms;
                dados.germinacao = dadosP[0].germinacao;
                dados.espacamento = dadosP[0].espacamento;
                dados.margemSeguranca = dadosP[0].margemSeguranca;
                dados.uid = _sessionManager.uid;

                var response = await _ConfigArea.Salvar(dadosP[0].id, _sessionManager.contaguid, dados);

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
    }
}