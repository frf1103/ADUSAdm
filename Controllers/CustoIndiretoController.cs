using FarmPlannerAdm.Shared;
using FarmPlannerClient.CustosIndiretos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC,UserV")]
    public class CustoIndiretoController : Controller
    {
        private readonly FarmPlannerClient.Controller.CustoIndiretoControllerClient _CustoIndireto;
        private readonly FarmPlannerClient.Controller.ClasseContaControllerClient _classeAPI;

        private readonly SessionManager _sessionManager;
        private const int TAMANHO_PAGINA = 5;

        public CustoIndiretoController(FarmPlannerClient.Controller.CustoIndiretoControllerClient CustoIndireto, FarmPlannerClient.Controller.CulturaControllerClient culturaAPI, FarmPlannerClient.Controller.OrganizacaoControllerClient orgAPI, SessionManager sessionManager, FarmPlannerClient.Controller.ClasseContaControllerClient classeAPI)
        {
            _CustoIndireto = CustoIndireto;

            _sessionManager = sessionManager;
            _classeAPI = classeAPI;
        }

        [Authorize(Roles = "Admin,User,AdminC,UserV")]
        public async Task<IActionResult> Index(int? idorg, string? filtro, int pagina = 1)
        {
            ViewBag.SelectedOption = idorg.ToString();
            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");

            return View();
        }

        [Authorize(Roles = "Admin,User,AdminC,UserV")]
        public async Task<JsonResult> GetData(string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.ClasseConta.ClasseContaViewModel>> ret = _classeAPI.Lista(filtro);
            List<FarmPlannerClient.ClasseConta.ClasseContaViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> AdicionarGrupoConta(int acao = 1, int id = 0, int idclasse = 0)
        {
            FarmPlannerClient.CustosIndiretos.GrupoContaViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FarmPlannerClient.CustosIndiretos.GrupoContaViewModel();
                c.idOrganizacao = _sessionManager.idorganizacao;
                c.idClasseConta = idclasse;
                ViewBag.idclasse = idclasse.ToString();
                ViewBag.Titulo = "Adicionar um Grupo de Conta";
                ViewBag.Acao = "adicionargrupo";
            }
            else
            {
                Task<FarmPlannerClient.CustosIndiretos.GrupoContaViewModel> ret = _CustoIndireto.ListaGrupoById(id, _sessionManager.contaguid);
                c = await ret;
                ViewBag.idclasse = c.idClasseConta.ToString();
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar um Grupo de conta";
                    ViewBag.Acao = "editargrupo";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir um Grupo de conta";
                    ViewBag.Acao = "excluirgrupo";
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
                    c = Newtonsoft.Json.JsonConvert.DeserializeObject<GrupoContaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editargrupo(int id, FarmPlannerClient.CustosIndiretos.GrupoContaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.idOrganizacao = _sessionManager.idorganizacao;
            dados.uid = _sessionManager.uid;
            var response = await _CustoIndireto.SalvarGrupo(id, _sessionManager.contaguid, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionargrupoconta", new { acao = 2, id = dados.id });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> AdicionarGrupo(FarmPlannerClient.CustosIndiretos.GrupoContaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;

            dados.idOrganizacao = _sessionManager.idorganizacao;
            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _CustoIndireto.AdicionarGrupo(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GrupoContaViewModel>(y);
                return RedirectToAction("AdicionarGrupoconta", new { acao = 1, id = 0, idclasse = dados.idClasseConta });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionargrupoconta", new { acao = 1, id = 0, idclasse = dados.idClasseConta });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Excluirgrupo(int id, FarmPlannerClient.CustosIndiretos.GrupoContaViewModel dados)
        {
            //FarmPlannerClient.CustoIndireto.CustoIndiretoViewModel dados=new FarmPlannerClient.CustoIndireto.CustoIndiretoViewModel();
            var response = await _CustoIndireto.ExcluirGrupo(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionargrupoconta");
        }

        [Authorize(Roles = "Admin,User,AdminC,UserV")]
        public async Task<JsonResult> Getgrupos(int idclasse)
        {
            Task<List<GrupoContaViewModel>> ret = _CustoIndireto.ListaGrupo(idclasse, _sessionManager.idorganizacao, _sessionManager.contaguid, " ");
            List<GrupoContaViewModel> c = await ret;

            return Json(c);
        }

        //contas
        [Authorize(Roles = "Admin,User,AdminC,UserV")]
        public async Task<JsonResult> GetContas(int idgrupo)
        {
            Task<List<CadastroContaViewModel>> ret = _CustoIndireto.ListaConta(idgrupo, _sessionManager.idorganizacao, _sessionManager.contaguid, " ");
            List<CadastroContaViewModel> c = await ret;

            return Json(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpGet]
        public async Task<IActionResult> AdicionarConta(int acao = 1, int id = 0, int idgrupo = 0)
        {
            FarmPlannerClient.CustosIndiretos.CadastroContaViewModel c;


            ViewBag.idacao = acao;
            if (acao == 1)
            {

                Task<GrupoContaViewModel> retg = _CustoIndireto.ListaGrupoById(idgrupo, _sessionManager.contaguid);
                GrupoContaViewModel g = await retg;
                if (g == null || g.idOrganizacao != _sessionManager.idorganizacao)
                {
                    return View("index");
                }

                c = new FarmPlannerClient.CustosIndiretos.CadastroContaViewModel();

                c.idGrupoConta = idgrupo;
                ViewBag.idgrupo = idgrupo.ToString();
                ViewBag.Titulo = "Adicionar uma  Conta";
                ViewBag.Acao = "adicionarconta";
            }
            else
            {
                Task<FarmPlannerClient.CustosIndiretos.CadastroContaViewModel> ret = _CustoIndireto.ListaContaById(id, _sessionManager.contaguid);
                c = await ret;
                ViewBag.idgrupo = c.idGrupoConta.ToString();

                Task<GrupoContaViewModel> retg = _CustoIndireto.ListaGrupoById(c.idGrupoConta, _sessionManager.contaguid);
                GrupoContaViewModel g = await retg;
                if (g == null || g.idOrganizacao != _sessionManager.idorganizacao)
                {
                    return View("index");
                }

                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar uma conta";
                    ViewBag.Acao = "editarconta";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir uma conta";
                    ViewBag.Acao = "excluirconta";
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
                    c = Newtonsoft.Json.JsonConvert.DeserializeObject<CadastroContaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            return View(c);
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> EditarConta(int id, FarmPlannerClient.CustosIndiretos.CadastroContaViewModel dados)
        {
            dados.uid = _sessionManager.uid;
            dados.idconta = _sessionManager.contaguid;
            var response = await _CustoIndireto.SalvarConta(id, _sessionManager.contaguid, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionarconta", new { acao = 2, id = dados.id });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Adicionarconta(FarmPlannerClient.CustosIndiretos.CadastroContaViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            dados.idGrupoConta = dados.idGrupoConta;

            dados.uid = _sessionManager.uid;
            //dados.idOrganizacao =_sessionManager.idorganizacao;
            var response = await _CustoIndireto.AdicionarConta(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GrupoContaViewModel>(y);
                return RedirectToAction("Adicionarconta", new { acao = 1, id = 0, idgrupo = dados.idGrupoConta });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionarconta", new { acao = 1, id = 0, idclasse = dados.idGrupoConta });
        }

        [Authorize(Roles = "Admin,User,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Excluirconta(int id, CadastroContaViewModel dados)
        {
            //FarmPlannerClient.CustoIndireto.CustoIndiretoViewModel dados=new FarmPlannerClient.CustoIndireto.CustoIndiretoViewModel();
            var response = await _CustoIndireto.ExcluirConta(id, _sessionManager.contaguid, _sessionManager.uid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionarconta");
        }
    }
}