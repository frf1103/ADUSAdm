using ADUSClient.Controller;
using ADUSClient.Parceiro;
using ADUSClient.Parcela;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Text.Json;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super")]
    public class ParcelaController : Controller
    {
        private readonly ParcelaControllerClient _clienteAPI;
        private readonly ParceiroControllerClient _parceiroAPI;

        public ParcelaController(ParcelaControllerClient clienteAPI, ParceiroControllerClient parceiroAPI)
        {
            _clienteAPI = clienteAPI;
            _parceiroAPI = parceiroAPI;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime ini, DateTime fim, int tipodata = 0, int status = 3, string idparceiro = "0", int forma = 3, string idassinatura = "", string? filtro = "")
        {
            ViewBag.Titulo = "Parcelas";
            ViewBag.idBanco = ""; // se for necessário para filtro em select
            ViewBag.Descricao = filtro;

            Task<List<ListParceiroViewModel>> retc = _parceiroAPI.Lista("");
            List<ListParceiroViewModel> t = await retc;

            ViewBag.parceiros = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            /*
            $('#status').val(@ViewBag.Status).trigger('change');
            $('#forma').val(@ViewBag.Forma).trigger('change');
            $('#tipodata').val(@ViewBag.tipodata).trigger('change');

            */

            ViewBag.Status = status.ToString();
            ViewBag.Forma = forma.ToString();
            ViewBag.tipodata = tipodata.ToString();
            ViewBag.SelectedOptionS = idparceiro.ToString();
            ViewBag.idassinatura = idassinatura.ToString();
            ViewBag.idparceiro = idparceiro.ToString();

            if (ini.Year == 1)
            {
                ViewBag.dtinicio = (DateTime.Now.AddDays(-30)).ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtinicio = ini.ToString("yyyy-MM-dd");
            }
            if (fim.Year == 1)
            {
                ViewBag.dtfim = (DateTime.Now.AddDays(30)).ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.dtfim = fim.ToString("yyyy-MM-dd");
            }

            //var lista = await _clienteAPI.ListarParcela(dataIni, dataFim, tipodata, status, idparceiro, forma, idassinatura, filtro);
            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(string id, int acao = 0, string idparceiro = "0")
        {
            ParcelaViewModel c;

            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new ParcelaViewModel
                {
                    id = Guid.NewGuid().ToString("N")
                };
                ViewBag.Titulo = "Adicionar Parcela";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                c = await _clienteAPI.ListaById(id);
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar Parcela";
                    ViewBag.Acao = "editar";
                }
                else if (acao == 3)
                {
                    ViewBag.Titulo = "Excluir Parcela";
                    ViewBag.Acao = "excluir";
                }
                else if (acao == 4)
                {
                    ViewBag.Titulo = "Visualizar Parcela";
                    ViewBag.Acao = "ver";
                }
            }

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<ParcelaViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }

            ViewBag.Formas = new List<SelectListItem>
            {
                new SelectListItem { Text = "Cartão", Value = "0" },
                new SelectListItem { Text = "Boleto", Value = "1" },
                new SelectListItem { Text = "Pix", Value = "2" }
            };
            ViewBag.idparceiro = idparceiro;

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(ParcelaViewModel dados)
        {
            var response = await _clienteAPI.Adicionar(dados);
            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ParcelaViewModel>(y);
                return RedirectToAction("Adicionar", new { id = result.id, acao = 2 });
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);
            return View("Adicionar", dados);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(ParcelaViewModel dados)
        {
            var response = await _clienteAPI.Salvar(dados.id, dados);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            TempData["Erro"] = x;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            return RedirectToAction("Adicionar", new { acao = 2, id = dados.id });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(string id, ParcelaViewModel dados)
        {
            var response = await _clienteAPI.Excluir(id);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("Adicionar");
        }

        [HttpGet]
        public async Task<JsonResult> GetData(DateTime ini, DateTime fim, int tipodata, int status, string idparceiro, int forma, string idassinatura, string? filtro)
        {
            var lista = await _clienteAPI.ListarParcela(ini, fim, tipodata, status, idparceiro ?? "0", forma, idassinatura, filtro ?? " ");
            var x = lista.OrderBy(x => x.datavencimento);
            return Json(x);
        }
    }
}