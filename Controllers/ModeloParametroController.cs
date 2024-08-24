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

using FarmPlannerClient.PrincipioAtivo;

using Microsoft.AspNetCore.Mvc.Rendering;
using FarmPlannerClient.ModeloMaquina;
using FarmPlannerClient.Operacao;
using FarmPlannerClient.Cultura;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,User,AdminC")]
    public class ModeloParametroController : Controller
    {
        private readonly FarmPlannerClient.Controller.ModeloMaquinaControllerClient _ModeloParametroAPI;

        private readonly FarmPlannerClient.Controller.OperacaoControllerClient _operacaoAPI;
        private readonly FarmPlannerClient.Controller.CulturaControllerClient _culturaAPI;

        private readonly SessionManager _sessionManager;

        private const int TAMANHO_PAGINA = 5;

        public ModeloParametroController(ModeloMaquinaControllerClient modeloParametroAPI, OperacaoControllerClient operacaoAPI, CulturaControllerClient culturaAPI, SessionManager sessionManager)
        {
            _ModeloParametroAPI = modeloParametroAPI;
            _operacaoAPI = operacaoAPI;
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(int? idmodelo, int? idcultura, int idoperacao)
        {
            Task<List<ListModeloMaquinaViewModel>> retm = _ModeloParametroAPI.Lista(0, "");
            List<ListModeloMaquinaViewModel> m = await retm;

            ViewBag.modelos = m.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<OperacaoViewModel>> reto = _operacaoAPI.Lista(_sessionManager.contaguid, 0, "");
            List<OperacaoViewModel> o = await reto;

            ViewBag.operacoes = o.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<CulturaViewModel>> retc = _culturaAPI.ListaCultura("");
            List<CulturaViewModel> c = await retc;

            ViewBag.culturas = c.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            ViewBag.SelectedOptionM = idmodelo.ToString();
            ViewBag.SelectedOptionC = idcultura.ToString();
            ViewBag.SelectedOptionO = idoperacao.ToString();

            return View();
        }

        public async Task<JsonResult> GetData(int idmodelo, int idcultura, int idoperacao)
        {
            Task<List<ListModeloParametroViewModel>> ret = _ModeloParametroAPI.ListaParametro(idmodelo, idoperacao, idcultura, _sessionManager.contaguid);
            List<ListModeloParametroViewModel> c = await ret;

            return Json(c);
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(int idmodelo, int idcultura, int idoperacao, int acao = 1, int id = 0)
        {
            ModeloParametroViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new ModeloParametroViewModel();
                c.idCultura = idcultura;
                c.idModeloMaquina = idmodelo;
                c.idOperacao = idoperacao;
                c.idconta = _sessionManager.contaguid;
                ViewBag.Titulo = "Adicionar";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<ModeloParametroViewModel> ret = _ModeloParametroAPI.ListaParametroById(id, _sessionManager.contaguid);
                c = await ret;

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
                    c = JsonConvert.DeserializeObject<ModeloParametroViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            Task<List<ListModeloMaquinaViewModel>> retm = _ModeloParametroAPI.Lista(0, "");
            List<ListModeloMaquinaViewModel> m = await retm;

            ViewBag.modelos = m.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<OperacaoViewModel>> reto = _operacaoAPI.Lista(_sessionManager.contaguid, 0, "");
            List<OperacaoViewModel> o = await reto;

            ViewBag.operacoes = o.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            Task<List<CulturaViewModel>> retc = _culturaAPI.ListaCultura("");
            List<CulturaViewModel> cl = await retc;

            ViewBag.culturas = cl.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, ModeloParametroViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;

            var response = await _ModeloParametroAPI.SalvarParametro(id, _sessionManager.contaguid, dados);

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
        public async Task<IActionResult> Adicionar(ModeloParametroViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;

            var response = await _ModeloParametroAPI.AdicionarParametro(dados);

            if (response.IsSuccessStatusCode)
            {
                //  string y = await response.Content.ReadAsStringAsync();
                //  var result = JsonSerializer.Deserialize<FarmPlannerClient.Defarea.ModeloParametroViewModel>(y);
                return RedirectToAction("adicionar", new { acao = 1, id = 0, idmodelo = dados.idModeloMaquina, idcultura = dados.idCultura, idoperacao = dados.idOperacao });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            TempData["Erro"] = x;
            return RedirectToAction("adicionar", new { acao = 1, id = 0, idmodelo = dados.idModeloMaquina, idcultura = dados.idCultura, idoperacao = dados.idOperacao });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, ModeloParametroViewModel dados)
        {
            //FarmPlannerClient.DefAreas.DefAreasViewModel dados=new FarmPlannerClient.DefAreas.DefAreasViewModel();
            var response = await _ModeloParametroAPI.ExcluirParametro(id, _sessionManager.contaguid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }
    }
}