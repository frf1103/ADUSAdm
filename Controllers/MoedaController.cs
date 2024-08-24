using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using FarmPlannerClient.Moeda;
using FarmPlannerClient.Controller;
using System.Xml.Xsl;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MoedaController : Controller
    {
        private readonly FarmPlannerClient.Controller.MoedaControllerClient _clienteAPI;
        private readonly SessionManager _sessionManager;

        public MoedaController(MoedaControllerClient clienteAPI, SessionManager sessionManager)
        {
            _clienteAPI = clienteAPI;
            _sessionManager = sessionManager;
        }

        private const int TAMANHO_PAGINA = 10;

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<MoedaViewModel>> ret = _clienteAPI.ListaMoeda(filtro);
            List<MoedaViewModel> c = await ret;

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)c.Count() / TAMANHO_PAGINA);
            return View(c.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar()
        {
            MoedaViewModel c = new MoedaViewModel();

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, string? ini, string? fim, int? pag = 1)
        {
            Task<MoedaViewModel> ret = _clienteAPI.ListaMoedaById(id);
            MoedaViewModel c = await ret;
            DateTime xini, xfim;

            if (ini == null)
            {
                xini = System.DateTime.Today.AddDays(-10);
                xfim = System.DateTime.Today;
            }
            else
            {
                xini = Convert.ToDateTime(ini);
                xfim = Convert.ToDateTime(fim);
            }
            Task<List<CotacaoMoedaViewModel>> retc = _clienteAPI.ListaCotacaoByMoeda(id, xini, xfim);
            List<CotacaoMoedaViewModel> cot = null;

            if (retc.Result != null)
            {
                cot = await retc;
            }
            IEnumerable<CotacaoMoedaViewModel> ycot = cot.OrderBy(c => c.cotacaoData); ;

            ViewBag.Id = id;
            c.listcotacoes = ycot;
            ViewBag.Ini = xini.ToString("yyyy-MM-dd");
            ViewBag.Fim = xfim.ToString("yyyy-MM-dd");
            ViewBag.xprincipal = "1";
            if (ini != null)
            {
                ViewBag.xprincipal = "2";
            }

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Excluir(int id)
        {
            Task<MoedaViewModel> ret = _clienteAPI.ListaMoedaById(id);
            MoedaViewModel c = await ret;

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, MoedaViewModel dados)
        {
            var response = await _clienteAPI.Salvar(id, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("editar");
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(MoedaViewModel dados)
        {
            var response = await _clienteAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MoedaViewModel>(y);
                return RedirectToAction(nameof(Editar), new { id = result.id });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, MoedaViewModel dados)
        {
            //MoedaViewModel dados=new MoedaViewModel();
            var response = await _clienteAPI.Excluir(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("excluir");
        }

        public async Task<IActionResult> AdicionarCotacao(int idmoeda, string descmoeda)
        {
            CotacaoMoedaViewModel c = new CotacaoMoedaViewModel();
            ViewBag.idmoeda = idmoeda;
            ViewBag.Descmoeda = descmoeda;

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> EditarCotacao(int id)
        {
            Task<CotacaoMoedaViewModel> ret = _clienteAPI.ListaCotacaoById(id);
            CotacaoMoedaViewModel c = await ret;

            ViewBag.idmoeda = c.idMoeda;
            ViewBag.Descmoeda = c.descmoeda;

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> ExcluirCotacao(int id)
        {
            Task<CotacaoMoedaViewModel> ret = _clienteAPI.ListaCotacaoById(id);
            CotacaoMoedaViewModel c = await ret;
            ViewBag.idmoeda = c.idMoeda;
            ViewBag.Descmoeda = c.descmoeda;

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> EditarCotacao(int id, CotacaoMoedaViewModel dados)
        {
            int idmoeda = dados.idMoeda;
            var response = await _clienteAPI.SalvarCotacao(id, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("editar", new { id = idmoeda });
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("editar");
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarCotacao(CotacaoMoedaViewModel dados)
        {
            var response = await _clienteAPI.AdicionarCotacao(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CotacaoMoedaViewModel>(y);
                return RedirectToAction("AdicionarCotacao", new { idmoeda = result.idMoeda, descmoeda = result.descmoeda });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            // return RedirectToAction("Adicionarvariedade", new { idcultura = dados.idCultura, desccultura = dados.desccultura });
            ViewBag.idmoeda = dados.idMoeda;
            ViewBag.Descmoeda = dados.descmoeda;

            return View("adicionarcotacao");
        }

        [HttpPost]
        public async Task<IActionResult> ExcluirCotacao(int id, CotacaoMoedaViewModel dados)
        {
            //FarmPlannerClient.Variedade.CotacaoMoedaViewModel dados=new FarmPlannerClient.Variedade.CotacaoMoedaViewModel();
            var response = await _clienteAPI.ExcluirCotacao(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("editar", new { id = dados.idMoeda });
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return RedirectToAction("Adicionarcotacao", new { idcultura = dados.idMoeda, descmoeda = dados.descmoeda });
        }
    }
}