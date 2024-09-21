using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using FarmPlannerClient.Enum;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;
using FarmPlannerClient.Organizacao;
using Newtonsoft.Json;
using FarmPlannerClient.Parceiro;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,AdminC,User, UserV")]
    public class ParceiroController : Controller
    {
        private readonly FarmPlannerClient.Controller.ParceiroControllerClient _culturaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public ParceiroController(FarmPlannerClient.Controller.ParceiroControllerClient culturaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.Parceiro.ListParceiroViewModel>> ret = _culturaAPI.Lista(_sessionManager.contaguid, filtro);
            List<FarmPlannerClient.Parceiro.ListParceiroViewModel> c = await ret;

            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,AdminC,User")]
        
        public async Task<IActionResult> Adicionar()
        {
            FarmPlannerClient.Parceiro.ParceiroViewModel c = new FarmPlannerClient.Parceiro.ParceiroViewModel();

            ViewBag.TiposPessoa = new[] {
                new SelectListItem { Text = "Física", Value = TipodePessoa.Física.ToString() },
                new SelectListItem { Text = "Jurídica", Value = TipodePessoa.Jurídica.ToString() }
            };
            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<ParceiroViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }


            return View(c);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,AdminC,User")]

        public async Task<IActionResult> Editar(int id,int acao=0)
        {
            ViewBag.acao = acao;
            Task<FarmPlannerClient.Parceiro.ParceiroViewModel> ret = _culturaAPI.ListaById(id, _sessionManager.contaguid);
            FarmPlannerClient.Parceiro.ParceiroViewModel c = await ret;

            ViewBag.Id = id;
            ViewBag.TiposPessoa = new[] {
                new SelectListItem { Text = "Física", Value = TipodePessoa.Física.ToString() },
                new SelectListItem { Text = "Jurídica", Value = TipodePessoa.Jurídica.ToString() }
            };
            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<ParceiroViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            return View(c);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,AdminC,User")]

        public async Task<IActionResult> Excluir(int id)
        {
            Task<FarmPlannerClient.Parceiro.ParceiroViewModel> ret = _culturaAPI.ListaById(id, _sessionManager.contaguid);
            FarmPlannerClient.Parceiro.ParceiroViewModel c = await ret;
            ViewBag.TiposPessoa = new[] {
                new SelectListItem { Text = "Física", Value = TipodePessoa.Física.ToString() },
                new SelectListItem { Text = "Jurídica", Value = TipodePessoa.Jurídica.ToString() }
            };
            //ViewBag.tipoclasse = c.tipoParceiro;

            return View(c);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminC,User")]

        public async Task<IActionResult> Editar(int id, FarmPlannerClient.Parceiro.ParceiroViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            var response = await _culturaAPI.Salvar(id, _sessionManager.contaguid, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            TempData["Erro"] = x;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            ModelState.AddModelError(string.Empty, x);
            //return View("editar");


            return RedirectToAction("editar",new {id=dados.id,acao=2});
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminC,User")]

        public async Task<IActionResult> Adicionar(FarmPlannerClient.Parceiro.ParceiroViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<FarmPlannerClient.Parceiro.ParceiroViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            
            var x = await response.Content.ReadAsStringAsync();

            TempData["Erro"] = x;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            ModelState.AddModelError(string.Empty, x);
            return RedirectToAction("adicionar");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminC,User")]

        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.Parceiro.ParceiroViewModel dados)
        {
            //FarmPlannerClient.Parceiro.ParceiroViewModel dados=new FarmPlannerClient.Parceiro.ParceiroViewModel();
            var response = await _culturaAPI.Excluir(id, _sessionManager.contaguid);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("excluir");
        }

        public async Task<JsonResult> GetData(string? filtro)
        {
            Task<List<FarmPlannerClient.Parceiro.ListParceiroViewModel>> ret = _culturaAPI.Lista(_sessionManager.contaguid, filtro);
            List<FarmPlannerClient.Parceiro.ListParceiroViewModel> c = await ret;

            return Json(c);
        }

    }
}