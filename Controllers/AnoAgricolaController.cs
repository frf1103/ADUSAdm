using FarmPlannerAdm.Shared;
using FarmPlannerClient.AnoAgricola;
using FarmPlannerClient.Organizacao;
using FarmPlannerClient.Safra;
using FarmPlannerClient.Variedade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Data;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class AnoAgricolaController : Controller
    {
        private readonly FarmPlannerClient.Controller.AnoAgricolaControllerClient _anoagricola;
        private readonly FarmPlannerClient.Controller.CulturaControllerClient _culturaAPI;
        private readonly FarmPlannerClient.Controller.OrganizacaoControllerClient _orgAPI;
        private readonly SessionManager _sessionManager;
        private const int TAMANHO_PAGINA = 5;

        public AnoAgricolaController(FarmPlannerClient.Controller.AnoAgricolaControllerClient anoAgricola, FarmPlannerClient.Controller.CulturaControllerClient culturaAPI, FarmPlannerClient.Controller.OrganizacaoControllerClient orgAPI, SessionManager sessionManager)
        {
            _anoagricola = anoAgricola;
            _culturaAPI = culturaAPI;
            _orgAPI = orgAPI;
            _sessionManager = sessionManager;
        }

        [Authorize(Roles = "Admin,User,AdminC,UserV")]
        public async Task<IActionResult> Index(int? idorg, string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel>> retc = _orgAPI.ListaOrganizacaoByUID(_sessionManager.uid);
            List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel> t = await retc;

            ViewBag.organizacoes = t.Select(m => new SelectListItem { Text = m.descorg, Value = m.idorganizacao.ToString() });

            ViewBag.SelectedOption = idorg.ToString();
            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");


            return View();
        }

        [Authorize(Roles = "Admin,User,AdminC,UserV")]
        public async Task<JsonResult> GetData(int idorganizacao, string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel>> ret = _anoagricola.Lista(idorganizacao, _sessionManager.contaguid, filtro);
            List<FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel();
                c.idOrganizacao = _sessionManager.idorganizacao;
                ViewBag.Titulo = "Adicionar um Ano Agrícola";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel> ret = _anoagricola.ListaById(id, _sessionManager.contaguid);
                c = await ret;
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar um Ano Agrícola";
                    ViewBag.Acao = "editar";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir um Ano Agrícola";
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
                    c = JsonConvert.DeserializeObject<AnoAgricolaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel>> retc = _orgAPI.ListaOrganizacaoByUID(_sessionManager.uid);
            List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel> t = await retc;

            ViewBag.organizacoes = t.Select(m => new SelectListItem { Text = m.descorg, Value = m.idorganizacao.ToString() });

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel dados)
        {
            var response = await _anoagricola.Salvar(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _anoagricola.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel>(y);
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
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel dados)
        {
            //FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel dados=new FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel();
            var response = await _anoagricola.Excluir(id, _sessionManager.contaguid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }

        [HttpGet]
        public async Task<JsonResult> ListAnoByOrg(int idorg)
        {
            Task<List<FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel>> ret = _anoagricola.Lista(idorg, _sessionManager.contaguid, "");
            List<FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel> c = await ret;
            return Json(c);
        }


        public async Task<IActionResult> Indexsafra(int? idcultura, string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.Cultura.CulturaViewModel>> retc = _culturaAPI.ListaCultura("");
            List<FarmPlannerClient.Cultura.CulturaViewModel> t = await retc;

            ViewBag.culturas = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOption = idcultura.ToString();

            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");


            return View();
        }

        public async Task<JsonResult> GetSafra(int idcultura, string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.Safra.SafraViewModel>> ret = _anoagricola.ListaSafra(_sessionManager.idanoagricola, idcultura, _sessionManager.contaguid, filtro);
            List<FarmPlannerClient.Safra.SafraViewModel> c = await ret;

            return Json(c);
        }
        [Authorize(Roles = "Admin,User,AdminC")]
        public async Task<IActionResult> AdicionarSafra(int acao = 1, int id = 0)
        {
            FarmPlannerClient.Safra.SafraViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FarmPlannerClient.Safra.SafraViewModel();

                ViewBag.Titulo = "Adicionar uma Safra";
                ViewBag.Acao = "adicionarsafra";
            }
            else
            {
                Task<FarmPlannerClient.Safra.SafraViewModel> ret = _anoagricola.ListaSafraById(id, _sessionManager.contaguid);
                c = await ret;
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar uma Safra";
                    ViewBag.Acao = "editarsafra";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir uma Safra";
                    ViewBag.Acao = "excluirsafra";
                }
                if (acao == 4)
                {
                    ViewBag.Titulo = "Visualizar";
                    ViewBag.Acao = "versafra";
                }
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<SafraViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<FarmPlannerClient.Cultura.CulturaViewModel>> retc = _culturaAPI.ListaCultura("");
            List<FarmPlannerClient.Cultura.CulturaViewModel> t = await retc;

            ViewBag.culturas = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> EditarSafra(int id, FarmPlannerClient.Safra.SafraViewModel dados)
        {
            dados.idAnoAgricola = _sessionManager.idanoagricola;
            dados.idconta = _sessionManager.contaguid;
            var response = await _anoagricola.SalvarSafra(id, _sessionManager.contaguid, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Indexsafra");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionarsafra", new { acao = 1, id = 0 });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> AdicionarSafra(FarmPlannerClient.Safra.SafraViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.idAnoAgricola = _sessionManager.idanoagricola;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _anoagricola.AdicionarSafra(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.Safra.SafraViewModel>(y);
                return RedirectToAction("adicionarsafra");
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionarsafra", new { acao = 1, id = 0 });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> ExcluirSafra(int id, FarmPlannerClient.Safra.SafraViewModel dados)
        {
            //FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel dados=new FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel();
            var response = await _anoagricola.ExcluirSafra(id, _sessionManager.contaguid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Indexsafra");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionarsafra");
        }

        public async Task<JsonResult> GetVariedadesBySafra(int idsafra)
        {
            Task<FarmPlannerClient.Safra.SafraViewModel> ret = _anoagricola.ListaSafraById(idsafra, _sessionManager.contaguid);
            FarmPlannerClient.Safra.SafraViewModel c = await ret;

            Task<List<VariedadeViewModel>> retc = _culturaAPI.ListaVariedadeByCultura(c.idCultura.Value, "");
            List<VariedadeViewModel> v = await retc;

            return Json(v);
        }
    }
}