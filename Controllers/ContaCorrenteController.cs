using ADUSClient.Banco;

using ADUSClient.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Text.Json;
using static RootObject;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super")]
    public class ContaCorrenteController : Controller
    {
        private readonly ContaCorrenteControllerClient _clienteAPI;
        private readonly BancoControllerClient _bancoAPI;

        public ContaCorrenteController(ContaCorrenteControllerClient clienteAPI, BancoControllerClient bancoAPI)
        {
            _clienteAPI = clienteAPI;
            _bancoAPI = bancoAPI;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? idBanco, string? descricao)
        {
            ViewBag.Titulo = "Contas Correntes";
            ViewBag.idBanco = idBanco.ToString();
            ViewBag.Descricao = descricao;

            Task<List<BancoViewModel>> retc = _bancoAPI.ListarBanco("");
            List<BancoViewModel> t = await retc;

            ViewBag.Bancos = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            //var contas = await _clienteAPI.Listar(descricao, idBanco);
            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(string id, int idBanco, string descBanco, int acao = 0)
        {
            ADUSClient.Banco.ContaCorrenteViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new ADUSClient.Banco.ContaCorrenteViewModel();
                c.id = Guid.NewGuid().ToString("N");
                c.bancoId = idBanco;

                ViewBag.Titulo = "Adicionar um Banco";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<ADUSClient.Banco.ContaCorrenteViewModel> ret = _clienteAPI.GetById(id);
                c = await ret;
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar um Banco";
                    ViewBag.Acao = "editar";
                }
                if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir um Banco";
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
                    c = JsonConvert.DeserializeObject<ContaCorrenteViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(ContaCorrenteViewModel dados)
        {
            var response = await _clienteAPI.Adicionar(dados);
            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ContaCorrenteViewModel>(y);
                return RedirectToAction("index", new { idBanco = result.bancoId });
            }
            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);
            return View("Adicionar", dados);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(string id, ContaCorrenteViewModel dados)
        {
            var response = await _clienteAPI.Salvar(id, dados);
            ;
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("index", new { idBanco = dados.bancoId });
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionar", new { acao = 2, id = dados.id });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(string id, ContaCorrenteViewModel dados)
        {
            //FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel dados=new FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel();
            var response = await _clienteAPI.Excluir(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("index", new { idBanco = dados.bancoId });
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string? filtro, int idbanco)
        {
            Task<List<ADUSClient.Banco.ContaCorrenteViewModel>> ret = _clienteAPI.Listar(filtro, idbanco);
            List<ADUSClient.Banco.ContaCorrenteViewModel> c = await ret;
            return Json(c);
        }
    }
}