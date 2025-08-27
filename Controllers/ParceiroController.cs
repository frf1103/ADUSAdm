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
using Microsoft.EntityFrameworkCore;
using ADUSAdm.Services;

namespace ADUSAdm.Controllers
{
    [Authorize(Roles = "Admin,Super,User, UserV")]
    public class ParceiroController : Controller
    {
        private readonly ADUSClient.Controller.ParceiroControllerClient _culturaAPI;
        private readonly ADUSClient.Controller.SharedControllerClient _shareAPI;
        private readonly CheckoutService _asaasservice;
        private const int TAMANHO_PAGINA = 5;

        private readonly SessionManager _sessionManager;

        public ParceiroController(ADUSClient.Controller.ParceiroControllerClient culturaAPI, SessionManager sessionManager, ADUSClient.Controller.SharedControllerClient shareAPI, CheckoutService asaasservice)
        {
            _culturaAPI = culturaAPI;
            _sessionManager = sessionManager;
            _shareAPI = shareAPI;
            _asaasservice = asaasservice;
        }

        public async Task<IActionResult> Index(string? filtro, List<string> perfis, int pagina = 1)
        {
            ViewBag.filtro = filtro;
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole != "UserV");
            if (perfis.Count == 0)
                ViewBag.PerfisSelecionados = new List<string> { "assinante" };
            else ViewBag.PerfisSelecionados = perfis;
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

            Task<List<ListParceiroViewModel>> retc = _culturaAPI.Lista("", true, false, false, false);
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
            ADUSClient.Parceiro.ParceiroViewModel c;

            ViewBag.urlconvite = _sessionManager.urlconvite;
            if (acao != 1)
            {
                c = await _culturaAPI.ListaById(id);
                ViewBag.acao = (acao == 2) ? "Editar" : (acao == 3) ? "Excluir" : "Visualizar";
                ViewBag.Id = id;
                ViewBag.Titulo = ViewBag.acao + " Parceiro";
            }
            else
            {
                ViewBag.acao = "Adicionar";
                c = new ADUSClient.Parceiro.ParceiroViewModel { id = Guid.NewGuid().ToString() };
                ViewBag.Titulo = "Novo Parceiro";
            }
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

            List<ListParceiroViewModel> t = await _culturaAPI.Lista("", true, false, false, true);

            ViewBag.Representantes = t.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            var coprod = t.Where(x => x.iscoprodutor == true);
            ViewBag.coprodutores = coprod.Select(m => new SelectListItem { Text = m.razaoSocial, Value = m.id.ToString() });

            List<UFViewModel> uf = await _shareAPI.ListaUF("");

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

        [AllowAnonymous]
        [HttpGet]
        public async Task<JsonResult> BuscarPorCpf(string cpf)

        {
            var c = await _culturaAPI.ListaByRegistro(cpf);
            if (c != null)
            {
                return Json(c);
            }

            return Json(new { id = "X" });
        }

        [AllowAnonymous]
        public async Task<JsonResult> GetData(string? filtro, List<string> perfis)
        {
            bool isbanco = (perfis.Contains("banco"));
            bool isassinante = (perfis.Contains("assinante"));
            bool isafiliado = (perfis.Contains("afiliado"));
            bool iscoprodutor = (perfis.Contains("coprodutor"));

            Task<List<ADUSClient.Parceiro.ListParceiroViewModel>> ret = _culturaAPI.Lista(filtro, isassinante, isbanco, isafiliado, iscoprodutor);
            List<ADUSClient.Parceiro.ListParceiroViewModel> c = await ret;

            return Json(c);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AdicionarParceiro(ParceiroViewModel model)
        {
            if (model.tipodePessoa == TipodePessoa.Jurídica)
            {
                if (string.IsNullOrEmpty(model.idRepresentante))
                {
                    var p = await _culturaAPI.ListaByRegistro(model.representante.registro);
                    if (p == null)
                    {
                        var representanteModel = model.representante;
                        var novoRepresentante = new ParceiroViewModel
                        {
                            id = Guid.NewGuid().ToString(),
                            razaoSocial = representanteModel.razaoSocial,
                            fantasia = representanteModel.fantasia,
                            registro = representanteModel.registro,
                            sexo = representanteModel.sexo,
                            estadoCivil = representanteModel.estadoCivil,
                            dtNascimento = representanteModel.dtNascimento,
                            profissao = representanteModel.profissao,
                            email = representanteModel.email,
                            fone1 = representanteModel.fone1,
                            idCidade = representanteModel.idCidade,
                            iduf = representanteModel.iduf,
                            cep = representanteModel.cep,
                            numero = representanteModel.numero,
                            bairro = representanteModel.bairro,
                            logradouro = representanteModel.logradouro
                        };

                        var xa = await _culturaAPI.Adicionar(novoRepresentante);

                        model.idRepresentante = novoRepresentante.id;
                    }
                    else
                    {
                        model.idRepresentante = p.id;
                    }
                    var x = await _culturaAPI.ListaById(model.id);
                    x.idRepresentante = model.idRepresentante;
                    var xp = await _culturaAPI.Salvar(x.id, x);
                }
            }
            else
            {
                var p = await _culturaAPI.ListaById(model.id);
                p.sexo = model.sexo;
                p.profissao = model.profissao;
                p.dtNascimento = model.dtNascimento;
                p.estadoCivil = model.estadoCivil;
                await _culturaAPI.Salvar(model.id, p);
            }
            TempData["Msgsucesso"] = "Dados Gravados com Sucesso";

            return RedirectToAction("sucesso", "checkout", new { registro = model.registro });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super,User")]
        public async Task<IActionResult> CriarSubconta(string id)
        {
            var parceiro = await _culturaAPI.ListaById(id);
            if (parceiro == null)
                return NotFound();

            var resultado = await _asaasservice.CriarSubconta(parceiro); // método que chama a API do Asaas

            if (resultado.Sucesso)
            {
                parceiro.idwallet = resultado.IdWallet;
                _culturaAPI.Salvar(id, parceiro);

                return Json(new { idwallet = resultado.IdWallet });
            }
            else
            {
                return StatusCode(500, "Erro ao criar subconta no Asaas.");
            }
        }
    }
}