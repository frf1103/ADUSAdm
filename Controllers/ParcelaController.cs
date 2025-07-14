using ADUSClient.Controller;
using ADUSClient.Parceiro;
using ADUSClient.Parcela;
using Humanizer;
using MathNet.Numerics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Globalization;
using System.Text.Json;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Super")]
    public class ParcelaController : Controller
    {
        private readonly ParcelaControllerClient _clienteAPI;
        private readonly ParceiroControllerClient _parceiroAPI;
        private readonly AssinaturaControllerClient _assinaturaAPI;

        public ParcelaController(ParcelaControllerClient clienteAPI, ParceiroControllerClient parceiroAPI, AssinaturaControllerClient assinaturaAPI)
        {
            _clienteAPI = clienteAPI;
            _parceiroAPI = parceiroAPI;
            _assinaturaAPI = assinaturaAPI;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime ini, DateTime fim, int tipodata = 0, int status = 3, string idparceiro = "0", int forma = 3, string idassinatura = "", string? filtro = "", int? checkout = 2)
        {
            ViewBag.Titulo = "Parcelas";
            ViewBag.idBanco = ""; // se for necessário para filtro em select
            ViewBag.Descricao = filtro;
            ViewBag.slCheckout = checkout;

            List<ListParceiroViewModel> t = await _parceiroAPI.Lista("", true, false, false, false);

            var a = await _assinaturaAPI.ListaById(idassinatura);

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
            if (a == null)
            {
                if (ini.Year == 1)
                {
                    ViewBag.dtinicio = (DateTime.Now.AddDays(-30)).ToString("yyyy-MM-dd");
                }
                else
                {
                    ViewBag.dtinicio = ini.ToString("yyyy-MM-dd");
                }
            }
            else
            {
                ViewBag.dtinicio = a.datavenda.ToString("yyyy-MM-dd");
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
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Adicionar(string id, DateTime ini, DateTime fim, int acao = 0, string idparceiro = "0", string idassinatura = "0", int forma = 0, int tipodata = 0, int status = 0
            )
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
                    ViewBag.Acao = "visualizar";
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
            ViewBag.idassinatura = idassinatura;
            ViewBag.tipodada = tipodata;
            ViewBag.forma = forma;
            ViewBag.ini = ini.ToString("yyyy-MM-dd");
            ViewBag.fim = fim.ToString("yyyy-MM-dd");
            ViewBag.status = status;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Console.WriteLine($"idassinatura = {c.idassinatura}");

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar(ParcelaViewModel dados)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
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
        public async Task<JsonResult> GetData(DateTime ini, DateTime fim, int tipodata, int status, string idparceiro, int forma, string idassinatura, string? filtro, int? checkout = 2)
        {
            var lista = await _clienteAPI.ListarParcela(ini, fim, tipodata, status, idparceiro ?? "0", forma, idassinatura ?? "0", filtro ?? " ", checkout);
            var x = lista.OrderBy(x => x.datavencimento);
            return Json(x);
        }

        [HttpGet]
        public async Task<IActionResult> visaogeralcarteira(DateTime vencimentoInicio, DateTime vencimentoFim, string idparceiro = "0", int acao = 0)
        {
            List<ListParceiroViewModel> t = await _parceiroAPI.Lista("", true, false, false, false);

            ViewBag.parceiros = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            ViewBag.idParceiro = idparceiro;

            if (vencimentoInicio.Year == 1)
            {
                ViewBag.ini = "2000-01-01";
            }
            else
            {
                ViewBag.ini = vencimentoInicio.ToString("yyyy-MM-dd");
            }
            if (vencimentoFim.Year == 1)
            {
                ViewBag.fim = "2500-01-01";
            }
            else
            {
                ViewBag.fim = vencimentoFim.ToString("yyyy-MM-dd");
            }
            List<visaogeralviewmodel> visao = await _clienteAPI.VisaoGeral(ViewBag.idParceiro, ViewBag.ini, ViewBag.fim);
            ViewBag.TotalComissao = visao.Sum(v => v.comissaoPaga);
            ViewBag.TotalRecebido = visao.Sum(v => v.valorRecebido);
            ViewBag.TotalAntecipacao = visao.Sum(v => v.taxaAntecipacao);
            ViewBag.TotalPlataforma = visao.Sum(v => v.taxaPlataforma);
            ViewBag.TotalPago = visao.Sum(v => v.valorPago);
            ViewBag.TotalAVencer = visao.Sum(v => v.valorAVencer);
            ViewBag.TotalVencido = visao.Sum(v => v.valorVencidas);
            ViewBag.TotalValor = visao.Sum(v => v.valortotal);
            ViewBag.arvores = visao.Sum(v => v.arvores);
            ViewBag.Acompensar = visao.Sum(v => v.acompensar);
            ViewBag.hoje = DateTime.Now.Date.AddDays(-1).ToString("yyyy-MM-dd");
            return View(visao);
        }
    }
}