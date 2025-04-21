using ADUSClient.Controller;
using ADUSClient.Transacao;
using ADUSAdm.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using ADUSClient.Enum;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super")]
    public class TransacaoController : Controller
    {
        private readonly TransacaoControllerClient _clienteAPI;
        private readonly SessionManager _sessionManager;

        public TransacaoController(TransacaoControllerClient clienteAPI, SessionManager sessionManager)
        {
            _clienteAPI = clienteAPI;
            _sessionManager = sessionManager;
        }

        private const int TAMANHO_PAGINA = 10;

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            var lista = await _clienteAPI.Listar(filtro);
            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)lista.Count / TAMANHO_PAGINA);
            ViewBag.filtro = filtro;
            return View(lista.Skip((pagina - 1) * TAMANHO_PAGINA).Take(TAMANHO_PAGINA).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            TransacaoViewModel model;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                model = new TransacaoViewModel();
                ViewBag.Titulo = "Adicionar Transação";
                ViewBag.Acao = "Adicionar";
            }
            else
            {
                model = await _clienteAPI.ListaById(id);
                ViewBag.Titulo = acao == 2 ? "Editar Transação" :
                                 acao == 3 ? "Excluir Transação" :
                                 "Visualizar Transação";
                ViewBag.Acao = acao == 2 ? "Editar" : acao == 3 ? "Excluir" : "Ver";
            }

            if (TempData["Erro"] != null && TempData["dados"] != null)
            {
                model = JsonConvert.DeserializeObject<TransacaoViewModel>(TempData["dados"]!.ToString()!)!;
                ModelState.AddModelError(string.Empty, TempData["Erro"]!.ToString()!);
            }

            ViewBag.Sinais = new List<SelectListItem>
            {
                new SelectListItem { Text = "Débito", Value = "D" },
                new SelectListItem { Text = "Crédito", Value = "C" }
            };

            ViewBag.Contrapartidas = Enum.GetValues(typeof(TipoContra))
                .Cast<TipoContra>()
                .Select(x => new SelectListItem
                {
                    Text = x.ToString(),
                    Value = ((int)x).ToString()
                }).ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(TransacaoViewModel dados)
        {
            var response = await _clienteAPI.Adicionar(dados);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var registro = JsonConvert.DeserializeObject<TransacaoViewModel>(json);
                return RedirectToAction("Adicionar", new { acao = 2, id = registro!.Id });
            }

            var erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);
            return View(dados);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, TransacaoViewModel dados)
        {
            var response = await _clienteAPI.Editar(id, dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            var erro = await response.Content.ReadAsStringAsync();
            TempData["Erro"] = erro;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            return RedirectToAction("Adicionar", new { acao = 2, id });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, TransacaoViewModel dados)
        {
            var response = await _clienteAPI.Excluir(id);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            var erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, erro);

            return View("Adicionar", dados);
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string? filtro)
        {
            var lista = await _clienteAPI.Listar(filtro);
            return Json(lista);
        }
    }
}