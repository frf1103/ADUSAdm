using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ADUSClient.Enum;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using ADUSAdm.Shared;

using Newtonsoft.Json;
using ADUSClient.Parceiro;
using ADUSClient.Localidade;
using System.Net.Http;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Admin,Super,User, UserV")]
    public class ParceiroController : Controller
    {
        private readonly ADUSClient.Controller.ParceiroControllerClient _culturaAPI;
        private readonly ADUSClient.Controller.SharedControllerClient _shareAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly SessionManager _sessionManager;

        public ParceiroController(ADUSClient.Controller.ParceiroControllerClient culturaAPI, SessionManager sessionManager, ADUSClient.Controller.SharedControllerClient shareAPI)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
            _shareAPI = shareAPI;
        }

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<ADUSClient.Parceiro.ListParceiroViewModel>> ret = _culturaAPI.Lista(filtro);
            List<ADUSClient.Parceiro.ListParceiroViewModel> c = await ret;

            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Adicionar()
        {
            ADUSClient.Parceiro.ParceiroViewModel c = new ADUSClient.Parceiro.ParceiroViewModel();

            ViewBag.TiposPessoa = new[] {
                new SelectListItem { Text = "Física", Value = TipodePessoa.Física.ToString() },
                new SelectListItem { Text = "Jurídica", Value = TipodePessoa.Jurídica.ToString() }
            };

            ViewBag.TipoSexo = new[] {
                new SelectListItem {Text = "Masculino", Value = TipoSexo.Masculino.ToString()},
                new SelectListItem { Text = "Feminino", Value = TipoSexo.Feminino.ToString() },
                new SelectListItem { Text = "Indiferente", Value = TipoSexo.Indiferente.ToString() }
            };

            ViewBag.TipoEstadoCivil = new[] {
                new SelectListItem {Text = "Solteiro", Value =TipoEstadoCivil.Solteiro.ToString()},
                new SelectListItem {Text = "Casado", Value =TipoEstadoCivil.Casado.ToString()},
                new SelectListItem {Text = "Divorciado", Value =TipoEstadoCivil.Divorciado.ToString()},
                new SelectListItem {Text = "Viuvo", Value =TipoEstadoCivil.Viuvo.ToString()},
                new SelectListItem {Text = "Indiferente", Value =TipoEstadoCivil.Indiferente.ToString()},
            };

            Task<List<ListParceiroViewModel>> retc = _culturaAPI.Lista("");
            List<ListParceiroViewModel> t = await retc;

            ViewBag.Representantes = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            Task<List<UFViewModel>> retu = _shareAPI.ListaUF("");
            List<UFViewModel> uf = await retu;

            ViewBag.Ufs = uf.Select(m => new SelectListItem { Text = m.sigla, Value = m.id.ToString() });

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
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Editar(string id, int acao = 0)
        {
            ViewBag.acao = acao;
            Task<ADUSClient.Parceiro.ParceiroViewModel> ret = _culturaAPI.ListaById(id);
            ADUSClient.Parceiro.ParceiroViewModel c = await ret;

            ViewBag.Id = id;
            ViewBag.TiposPessoa = new[] {
                new SelectListItem { Text = "Física", Value = TipodePessoa.Física.ToString() },
                new SelectListItem { Text = "Jurídica", Value = TipodePessoa.Jurídica.ToString() }
            };

            ViewBag.TipoSexo = new[] {
                new SelectListItem {Text = "Masculino", Value = TipoSexo.Masculino.ToString()},
                new SelectListItem { Text = "Feminino", Value = TipoSexo.Feminino.ToString() },
                new SelectListItem { Text = "Indiferente", Value = TipoSexo.Indiferente.ToString() }
            };

            ViewBag.TipoEstadoCivil = new[] {
                new SelectListItem {Text = "Solteiro", Value =TipoEstadoCivil.Solteiro.ToString()},
                new SelectListItem {Text = "Casado", Value =TipoEstadoCivil.Casado.ToString()},
                new SelectListItem {Text = "Divorciado", Value =TipoEstadoCivil.Divorciado.ToString()},
                new SelectListItem {Text = "Viuvo", Value =TipoEstadoCivil.Viuvo.ToString()},
                new SelectListItem {Text = "Indiferente", Value =TipoEstadoCivil.Indiferente.ToString()},
            };

            Task<List<ListParceiroViewModel>> retc = _culturaAPI.Lista("");
            List<ListParceiroViewModel> t = await retc;

            ViewBag.Representantes = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            Task<List<UFViewModel>> retu = _shareAPI.ListaUF("");
            List<UFViewModel> uf = await retu;

            ViewBag.Ufs = uf.Select(m => new SelectListItem { Text = m.sigla, Value = m.id.ToString() });

            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<ParceiroViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }
            ViewBag.iduf = c.iduf;
            ViewBag.idcid = c.idCidade;
            if (acao == 3)
            {
                ViewBag.acao = "disable";
            }
            else
            {
                ViewBag.acao = "";
            }

            return View(c);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Excluir(string id)
        {
            Task<ADUSClient.Parceiro.ParceiroViewModel> ret = _culturaAPI.ListaById(id);
            ADUSClient.Parceiro.ParceiroViewModel c = await ret;
            ViewBag.TiposPessoa = new[] {
                new SelectListItem { Text = "Física", Value = TipodePessoa.Física.ToString() },
                new SelectListItem { Text = "Jurídica", Value = TipodePessoa.Jurídica.ToString() }
            };
            //ViewBag.tipoclasse = c.tipoParceiro;

            return View(c);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Editar(string id, ADUSClient.Parceiro.ParceiroViewModel dados)
        {
            var response = await _culturaAPI.Salvar(id, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            TempData["Erro"] = x;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            ModelState.AddModelError(string.Empty, x);
            //return View("editar");

            return RedirectToAction("editar", new { id = dados.id, acao = 2 });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Adicionar(ADUSClient.Parceiro.ParceiroViewModel dados)
        {
            dados.id = Guid.NewGuid().ToString("N");
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ADUSClient.Parceiro.ParceiroViewModel>(y);
                return RedirectToAction(nameof(Adicionar));
            }

            var x = await response.Content.ReadAsStringAsync();

            TempData["Erro"] = x;
            TempData["dados"] = JsonConvert.SerializeObject(dados);

            ModelState.AddModelError(string.Empty, x);
            return RedirectToAction("adicionar");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> Excluir(string id, ADUSClient.Parceiro.ParceiroViewModel dados)
        {
            //ADUSClient.Parceiro.ParceiroViewModel dados=new ADUSClient.Parceiro.ParceiroViewModel();
            var response = await _culturaAPI.Excluir(id);

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
            Task<List<ADUSClient.Parceiro.ListParceiroViewModel>> ret = _culturaAPI.Lista(filtro);
            List<ADUSClient.Parceiro.ListParceiroViewModel> c = await ret;

            return Json(c);
        }
    }
}