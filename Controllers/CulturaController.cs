using Microsoft.AspNetCore.Mvc;

using FarmPlannerAdm.ViewModel.Cultura;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata;
using System.Text.Json;
using FarmPlannerAdm.ViewModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CulturaController : Controller
    {
        private readonly FarmPlannerClient.Controller.CulturaControllerClient _culturaAPI;

        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public CulturaController(FarmPlannerClient.Controller.CulturaControllerClient culturaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.Cultura.CulturaViewModel>> ret = _culturaAPI.ListaCultura(filtro);
            List<FarmPlannerClient.Cultura.CulturaViewModel> c = await ret;

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)c.Count() / TAMANHO_PAGINA);
            return View(c.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar()
        {
            FarmPlannerClient.Cultura.CulturaViewModel c = new FarmPlannerClient.Cultura.CulturaViewModel();

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            Task<FarmPlannerClient.Cultura.CulturaViewModel> ret = _culturaAPI.ListaCulturaById(id);
            FarmPlannerClient.Cultura.CulturaViewModel c = await ret;

            ViewBag.Id = id;

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Excluir(int id)
        {
            Task<FarmPlannerClient.Cultura.CulturaViewModel> ret = _culturaAPI.ListaCulturaById(id);
            FarmPlannerClient.Cultura.CulturaViewModel c = await ret;

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.Cultura.CulturaViewModel dados)
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
        public async Task<IActionResult> Adicionar(FarmPlannerClient.Cultura.CulturaViewModel dados)
        {
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.Cultura.CulturaViewModel>(y);
                return RedirectToAction(nameof(Editar), new { id = result.id });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.Cultura.CulturaViewModel dados)
        {
            //FarmPlannerClient.Cultura.CulturaViewModel dados=new FarmPlannerClient.Cultura.CulturaViewModel();
            var response = await _culturaAPI.Excluir(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("excluir");
        }

        public async Task<IActionResult> AdicionarVariedade(int idcultura, string desccultura)
        {
            FarmPlannerClient.Variedade.VariedadeViewModel c = new FarmPlannerClient.Variedade.VariedadeViewModel();
            ViewBag.idcultura = idcultura;
            ViewBag.Desccultura = desccultura;

            Task<List<FarmPlannerClient.Variedade.TecnologiaViewModel>> ret = _culturaAPI.ListarTecnologia("");
            List<FarmPlannerClient.Variedade.TecnologiaViewModel> t = await ret;

            ViewBag.Tecnologias = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });
            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> EditarVariedade(int id)
        {
            Task<FarmPlannerClient.Variedade.VariedadeViewModel> ret = _culturaAPI.ListaVariedadeById(id);
            FarmPlannerClient.Variedade.VariedadeViewModel c = await ret;

            ViewBag.idcultura = c.idCultura;
            ViewBag.Desccultura = c.desccultura;

            Task<List<FarmPlannerClient.Variedade.TecnologiaViewModel>> ret1 = _culturaAPI.ListarTecnologia("");
            List<FarmPlannerClient.Variedade.TecnologiaViewModel> t = await ret1;

            ViewBag.Tecnologias = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> ExcluirVariedade(int id)
        {
            Task<FarmPlannerClient.Variedade.VariedadeViewModel> ret = _culturaAPI.ListaVariedadeById(id);
            FarmPlannerClient.Variedade.VariedadeViewModel c = await ret;
            ViewBag.idcultura = c.idCultura;
            ViewBag.Desccultura = c.desccultura;

            Task<List<FarmPlannerClient.Variedade.TecnologiaViewModel>> ret1 = _culturaAPI.ListarTecnologia("");
            List<FarmPlannerClient.Variedade.TecnologiaViewModel> t = await ret1;

            ViewBag.Tecnologias = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> EditarVariedade(int id, FarmPlannerClient.Variedade.VariedadeViewModel dados)
        {
            int idcultura = dados.idCultura;
            var response = await _culturaAPI.SalvarVariedade(id, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("editar", new { id = idcultura });
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("editar");
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarVariedade(FarmPlannerClient.Variedade.VariedadeViewModel dados)
        {
            var response = await _culturaAPI.AdicionarVariedade(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.Variedade.VariedadeViewModel>(y);
                return RedirectToAction("Adicionarvariedade", new { idcultura = result.idCultura, desccultura = result.desccultura });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            // return RedirectToAction("Adicionarvariedade", new { idcultura = dados.idCultura, desccultura = dados.desccultura });
            Task<List<FarmPlannerClient.Variedade.TecnologiaViewModel>> ret1 = _culturaAPI.ListarTecnologia("");
            List<FarmPlannerClient.Variedade.TecnologiaViewModel> t = await ret1;

            ViewBag.Tecnologias = t.Select(m => new SelectListItem { Text = m.descricao, Value = m.id.ToString() });
            ViewBag.idcultura = dados.idCultura;
            ViewBag.Desccultura = dados.desccultura;

            return View("adicionarvariedade");
        }

        [HttpPost]
        public async Task<IActionResult> ExcluirVariedade(int id, FarmPlannerClient.Variedade.VariedadeViewModel dados)
        {
            //FarmPlannerClient.Variedade.VariedadeViewModel dados=new FarmPlannerClient.Variedade.VariedadeViewModel();
            var response = await _culturaAPI.ExcluirVariedade(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("editar", new { id = dados.idCultura });
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return RedirectToAction("Adicionarvariedade", new { idcultura = dados.idCultura, desccultura = dados.desccultura });
        }

        public async Task<JsonResult> GetCulturaById(int id)
        {
            Task<FarmPlannerClient.Cultura.CulturaViewModel> ret = _culturaAPI.ListaCulturaById(id);
            FarmPlannerClient.Cultura.CulturaViewModel c = await ret;
            return Json(c);
        }
    }
}