using ADUSClient.Banco;
using ADUSClient.Controller;
using ADUSAdm.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Newtonsoft.Json;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super")]
    public class BancoController : Controller
    {
        private readonly BancoControllerClient _clienteAPI;
        private readonly SessionManager _sessionManager;

        public BancoController(BancoControllerClient clienteAPI, SessionManager sessionManager)
        {
            _clienteAPI = clienteAPI;
            _sessionManager = sessionManager;
        }

        private const int TAMANHO_PAGINA = 10;

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            var lista = await _clienteAPI.ListarBanco(filtro);
            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)lista.Count() / TAMANHO_PAGINA);
            return View(lista.Skip((pagina - 1) * TAMANHO_PAGINA).Take(TAMANHO_PAGINA).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            ADUSClient.Banco.BancoViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new ADUSClient.Banco.BancoViewModel();

                ViewBag.Titulo = "Adicionar um Banco";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<ADUSClient.Banco.BancoViewModel> ret = _clienteAPI.ListarBancoById(id);
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
                    c = JsonConvert.DeserializeObject<BancoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(BancoViewModel dados)
        {
            var response = await _clienteAPI.AdicionarBanco(dados);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var banco = JsonConvert.DeserializeObject<BancoViewModel>(json);
                return RedirectToAction("adicionar", new { acao = 2, id = banco.id });
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);
            return View(dados);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, ADUSClient.Banco.BancoViewModel dados)
        {
            var response = await _clienteAPI.EditarBanco(id, dados);
            ;
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionar", new { acao = 2, id = dados.id });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, ADUSClient.Banco.BancoViewModel dados)
        {
            //FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel dados=new FarmPlannerClient.AnoAgricola.AnoAgricolaViewModel();
            var response = await _clienteAPI.ExcluirBanco(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("adicionar");
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string? filtro)
        {
            Task<List<ADUSClient.Banco.BancoViewModel>> ret = _clienteAPI.ListarBanco(filtro);
            List<ADUSClient.Banco.BancoViewModel> c = await ret;

            return Json(c);
        }
    }
}