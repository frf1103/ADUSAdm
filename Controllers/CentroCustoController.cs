using ADUSClient.CentroCusto;
using ADUSClient.Controller;
using ADUSAdm.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super")]
    public class CentroCustoController : Controller
    {
        private readonly CentroCustoControllerClient _client;
        private readonly SessionManager _session;

        public CentroCustoController(CentroCustoControllerClient client, SessionManager session)
        {
            _client = client;
            _session = session;
        }

        private const int TAMANHO_PAGINA = 10;

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            ViewBag.permissao = (_session.userrole != "UserV");
            var lista = await _client.Listar(filtro);
            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)lista.Count / TAMANHO_PAGINA);
            ViewBag.filtro = filtro;
            return View(lista.Skip((pagina - 1) * TAMANHO_PAGINA).Take(TAMANHO_PAGINA).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            CentroCustoViewModel model;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                model = new CentroCustoViewModel();
                ViewBag.Titulo = "Adicionar Centro de Custo";
                ViewBag.Acao = "Adicionar";
            }
            else
            {
                model = await _client.ObterPorId(id) ?? new CentroCustoViewModel();
                ViewBag.Titulo = acao == 2 ? "Editar Centro de Custo" : acao == 3 ? "Excluir Centro de Custo" : "Visualizar";
                ViewBag.Acao = acao == 2 ? "Editar" : acao == 3 ? "Excluir" : "Ver";
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                    model = JsonConvert.DeserializeObject<CentroCustoViewModel>(TempData["dados"].ToString()!)!;
                ModelState.AddModelError(string.Empty, TempData["Erro"]!.ToString()!);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(CentroCustoViewModel model)
        {
            var response = await _client.Adicionar(model);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<CentroCustoViewModel>(json);
                return RedirectToAction("Adicionar", new { acao = 1});
            }

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, CentroCustoViewModel model)
        {
            var response = await _client.Editar(id, model);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string erro = await response.Content.ReadAsStringAsync();
            TempData["Erro"] = erro;
            TempData["dados"] = JsonConvert.SerializeObject(model);
            return RedirectToAction("Adicionar", new { acao = 2, id = model.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, CentroCustoViewModel model)
        {
            var response = await _client.Excluir(id);
            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            string erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);
            return View("Adicionar");
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string? filtro)
        {
            var lista = await _client.Listar(filtro);
            return Json(lista);
        }
    }
}