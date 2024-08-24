using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using FarmPlannerClient.GrupoProduto;
using FarmPlannerClient.Enum;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GrupoProdutoController : Controller
    {
        private readonly FarmPlannerClient.Controller.GrupoProdutoControllerClient _culturaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public GrupoProdutoController(FarmPlannerClient.Controller.GrupoProdutoControllerClient culturaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel>> ret = _culturaAPI.Lista(filtro);
            List<FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel> c = await ret;

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)c.Count() / TAMANHO_PAGINA);
            return View(c.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar()
        {
            FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel c = new FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel();

            ViewBag.TiposGrupos = new[] {
                new SelectListItem { Text = "Fertilizantes", Value = TipoGrupo.Fertilizantes.ToString() },
                new SelectListItem { Text = "Biologicos", Value =TipoGrupo.Biologicos.ToString() },
                new SelectListItem { Text = "Sementes", Value =TipoGrupo.Sementes.ToString()},
                new SelectListItem { Text = "Corretivos", Value =TipoGrupo.Corretivos.ToString() },
                new SelectListItem { Text = "Defensivos", Value =TipoGrupo.Defensivos.ToString() }
            };

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            Task<FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel c = await ret;

            ViewBag.Id = id;
            ViewBag.TiposGrupos = new[] {
                new SelectListItem { Text = "Fertilizantes", Value = TipoGrupo.Fertilizantes.ToString() },
                new SelectListItem { Text = "Biologicos", Value =TipoGrupo.Biologicos.ToString() },
                new SelectListItem { Text = "Sementes", Value =TipoGrupo.Sementes.ToString()},
                new SelectListItem { Text = "Corretivos", Value =TipoGrupo.Corretivos.ToString() },
                new SelectListItem { Text = "Defensivos", Value =TipoGrupo.Defensivos.ToString() }
            };
            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Excluir(int id)
        {
            Task<FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel c = await ret;

            ViewBag.tipogrupo = c.tipo.ToString();

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel dados)
        {
            var response = await _culturaAPI.Salvar(id, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("editar");
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel dados)
        {
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel dados)
        {
            //FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel dados=new FarmPlannerClient.GrupoProduto.GrupoProdutoViewModel();
            var response = await _culturaAPI.Excluir(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("excluir");
        }
    }
}