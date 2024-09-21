using FarmPlannerAdm.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;

using System.Data;
using FarmPlannerClient.Localidade;
using FarmPlannerClient.Regiao;
using Newtonsoft.Json;
using FarmPlannerClient.DefAreas;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class DefAreasController : Controller
    {
        private readonly FarmPlannerClient.Controller.FazendaControllerClient _fazendaAPI;
        private readonly FarmPlannerClient.Controller.CulturaControllerClient _culturaAPI;
        private readonly FarmPlannerClient.Controller.OrganizacaoControllerClient _orgAPI;
        private readonly FarmPlannerClient.Controller.RegiaoControllerClient _regiaoAPI;
        private readonly FarmPlannerClient.Controller.SharedControllerClient _shareAPI;
        private readonly SessionManager _sessionManager;

        private const int TAMANHO_PAGINA = 5;

        public DefAreasController(FarmPlannerClient.Controller.CulturaControllerClient culturaAPI, FarmPlannerClient.Controller.OrganizacaoControllerClient orgAPI, FarmPlannerClient.Controller.FazendaControllerClient fazendaAPI, FarmPlannerClient.Controller.RegiaoControllerClient regiaoAPI, FarmPlannerClient.Controller.SharedControllerClient shareAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _orgAPI = orgAPI;
            _fazendaAPI = fazendaAPI;
            _regiaoAPI = regiaoAPI;
            _shareAPI = shareAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(int? idregiao, string? filtro, int pagina = 1)
        {
            Task<List<RegiaoViewModel>> retr = _regiaoAPI.Lista("");
            List<RegiaoViewModel> r = await retr;

            ViewBag.regioes = r.Select(m => new SelectListItem { Text = m.nome, Value = m.id.ToString() });

            ViewBag.SelectedOption = idregiao.ToString();
            ViewBag.filtro = filtro;
            if (_sessionManager.idorganizacao == null || _sessionManager.idorganizacao == 0)
            {
                ModelState.AddModelError(string.Empty, "Necessário definir Organização de preferência");
                return RedirectToAction("index", "home");
            }

            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");


            return View();
        }

        public async Task<JsonResult> GetData(int idregiao, string? filtro, int pagina = 1)
        {
            Task<List<ListFazendaViewModel>> ret = _fazendaAPI.Lista(_sessionManager.idorganizacao, idregiao, _sessionManager.contaguid, filtro);
            List<ListFazendaViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            FazendaViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FazendaViewModel();
                c.idOrganizacao = _sessionManager.idorganizacao;
                ViewBag.Titulo = "Adicionar uma Fazenda ";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<FazendaViewModel> ret = _fazendaAPI.ListaById(id, _sessionManager.contaguid);
                c = await ret;
                ViewBag.idcidade = c.idMunicipio;
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar uma Fazenda";
                    ViewBag.Acao = "editar";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir uma Fazenda";
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
                    c = JsonConvert.DeserializeObject<FazendaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<UFViewModel>> retc = _shareAPI.ListaUF("");
            List<UFViewModel> t = await retc;

            ViewBag.Ufs = t.Select(m => new SelectListItem { Text = m.sigla, Value = m.id.ToString() });

            Task<List<RegiaoViewModel>> retr = _regiaoAPI.Lista("");
            List<RegiaoViewModel> r = await retr;

            ViewBag.Regioes = r.Select(m => new SelectListItem { Text = m.nome, Value = m.id.ToString() });

            ViewBag.tipospropriedade = new[] {
                new SelectListItem { Text = "Própria", Value = "0" },
                new SelectListItem { Text = "Arrendada", Value = "1" }
            };
            ViewBag.tiposarrenda = new[] {
                new SelectListItem { Text = "Reais", Value = "0" },
                new SelectListItem { Text = "SC", Value = "1" }
            };

            Task<List<FarmPlannerClient.Cultura.CulturaViewModel>> retcult = _culturaAPI.ListaCultura("");
            List<FarmPlannerClient.Cultura.CulturaViewModel> tcult = await retcult;

            ViewBag.culturas = tcult.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }
        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, FazendaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.idOrganizacao = _sessionManager.idorganizacao;
            var response = await _fazendaAPI.Salvar(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(FazendaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.idOrganizacao = _sessionManager.idorganizacao;
            var response = await _fazendaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                //  string y = await response.Content.ReadAsStringAsync();
                //  var result = JsonSerializer.Deserialize<FarmPlannerClient.Defarea.FazendaViewModel>(y);
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
        public async Task<IActionResult> Excluir(int id, FazendaViewModel dados)
        {
            //FarmPlannerClient.DefAreas.DefAreasViewModel dados=new FarmPlannerClient.DefAreas.DefAreasViewModel();
            var response = await _fazendaAPI.Excluir(id, _sessionManager.contaguid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }

        public async Task<JsonResult> GetTalhoes(int idfazenda, string? filtro, int pagina = 1)
        {
            Task<List<EditarTalhaoViewModel>> ret = _fazendaAPI.ListaTalhao(idfazenda, _sessionManager.contaguid, _sessionManager.idanoagricola, filtro);
            List<EditarTalhaoViewModel> c = await ret;

            return Json(c);
        }
        [Authorize(Roles = "Admin,User,AdminC")]
        public async Task<IActionResult> AdicionarTalhao(int idfazenda, int acao = 1, int id = 0)
        {
            EditarTalhaoViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FarmPlannerClient.DefAreas.EditarTalhaoViewModel();
                ViewBag.idfazenda = idfazenda;
                ViewBag.Titulo = "Adicionar uma Talhão ";
                ViewBag.Acao = "adicionartalhao";
            }
            else
            {
                Task<EditarTalhaoViewModel> retT = _fazendaAPI.ListaTalhaoById(id, _sessionManager.contaguid);
                c = await retT;
                ViewBag.idfazenda = c.idFazenda;
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar um Talhão";
                    ViewBag.Acao = "editartalhao";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir um Talhão";
                    ViewBag.Acao = "excluirtalhao";
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
                    c = JsonConvert.DeserializeObject<EditarTalhaoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            ViewBag.tiposarea = new[] {
                new SelectListItem { Text = "Sequeiro", Value = "0" },
                new SelectListItem { Text = "Irrigado", Value = "1" }
            };

            return View(c);
        }
        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> EditarTalhao(int id, EditarTalhaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.idAnoAgricola = _sessionManager.idanoagricola;
            dados.uid = _sessionManager.uid;

            var response = await _fazendaAPI.SalvarTalhao(id, _sessionManager.contaguid, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("adicionar", new { acao = 2, id = dados.idFazenda });
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionartalhao", new { acao = 1, idfazenda = dados.idFazenda, id = 0 });
        }
        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> AdicionarTalhao(EditarTalhaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.idAnoAgricola = _sessionManager.idanoagricola;
            dados.uid = _sessionManager.uid;
            var response = await _fazendaAPI.AdicionarTalhao(dados);

            if (response.IsSuccessStatusCode)
            {
                //  string y = await response.Content.ReadAsStringAsync();
                //  var result = JsonSerializer.Deserialize<FarmPlannerClient.Defarea.FazendaViewModel>(y);
                return RedirectToAction("Adicionartalhao", new { acao = 1, idfazenda = dados.idFazenda, id = 0 });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionartalhao", new { acao = 1, idfazenda = dados.idFazenda, id = 0 });
        }
        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> ExcluirTalhao(int id, EditarTalhaoViewModel dados)
        {
            //FarmPlannerClient.DefAreas.DefAreasViewModel dados=new FarmPlannerClient.DefAreas.DefAreasViewModel();
            dados.uid = _sessionManager.uid;
            var response = await _fazendaAPI.ExcluirTalhao(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("adicionar", new { acao = 1, id = dados.idFazenda });
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionartalhao");
        }
    }
}