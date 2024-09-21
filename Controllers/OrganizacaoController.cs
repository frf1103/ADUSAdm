using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using FarmPlannerClient.ClasseConta;
using FarmPlannerClient.Enum;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using FarmPlannerAdm.Shared;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using FarmPlannerAdm.Data;
using FarmPlannerAdm.Models;
using FarmPlannerClient.Conta;
using FarmPlannerClient.Organizacao;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,AdminC,User,UserV")]
    public class OrganizacaoController : Controller
    {
        private readonly FarmPlannerClient.Controller.OrganizacaoControllerClient _culturaAPI;
        private readonly FarmPlannerClient.Controller.ContaControllerClient _contaAPI;
        private const int TAMANHO_PAGINA = 5;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly FarmPlannercontext _context;
        private readonly SessionManager _sessionManager;

        public OrganizacaoController(FarmPlannerClient.Controller.OrganizacaoControllerClient culturaAPI, UserManager<IdentityUser> userManager, FarmPlannercontext context, FarmPlannerClient.Controller.ContaControllerClient contaAPI, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;
            _userManager = userManager;
            _context = context;
            _contaAPI = contaAPI;
            _sessionManager = sessionManager;
        }


        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            /*
            Task<List<FarmPlannerClient.Organizacao.OrganizacaoViewModel>> ret = _culturaAPI.ListaByConta(idconta, filtro);
            List<FarmPlannerClient.Organizacao.OrganizacaoViewModel> c = await ret;

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)c.Count() / TAMANHO_PAGINA);
            return View(c.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList()); */
            if (string.IsNullOrWhiteSpace(filtro)) filtro = "";
            ViewBag.filtro = filtro;

            // Obtém as roles do usuário
            var user = await _userManager.FindByIdAsync(_sessionManager.uid);
            var roles = await _userManager.GetRolesAsync(user);

            // Passa as roles para a view usando ViewBag
            ViewBag.UserRoles = roles[0].ToString();
            ViewBag.role = _sessionManager.userrole;
            ViewBag.permissao = (_sessionManager.userrole == "Admin" || _sessionManager.userrole == "AdminC");

            return View();
        }

        public async Task<JsonResult> GetData(string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.Organizacao.OrganizacaoViewModel>> ret = _culturaAPI.ListaByConta(_sessionManager.contaguid, filtro);
            List<FarmPlannerClient.Organizacao.OrganizacaoViewModel> c = await ret;

            return Json(c);
        }

        public async Task<JsonResult> GetUsuarios(int idorg)
        {
            var users = _userManager.Users.ToList();
            Task<List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel>> retc = _culturaAPI.ListaUsuarioByOrg(idorg);
            List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel> usuconta = await retc;
            var usumodels = usuconta.Select(vm => new UsuOrg
            {
                uid = vm.uid,
                idorganizacao = vm.idorganizacao,
                id = vm.id
            }).ToList();
            var selectManyResult = users.SelectMany(User => usumodels
                .Where(UsuOrg => UsuOrg.uid == User.Id)
                .Select(UsuOrg => new
                {
                    id = UsuOrg.id,
                    uid = User.Id,
                    nome = User.UserName
                })
            ).ToList();
            return Json(selectManyResult);
        }

        [Authorize(Roles = "Admin,AdminC")]
        [HttpGet]
        public async Task<IActionResult> Adicionar(int acao = 1, int id = 0)
        {
            FarmPlannerClient.Organizacao.OrganizacaoViewModel c;
            ViewBag.TiposPessoas = new[] {
                new SelectListItem { Text = "Física", Value = TipodePessoa.Física.ToString() },
                new SelectListItem { Text = "Jurídica", Value = TipodePessoa.Jurídica.ToString() }
            };
            ViewBag.idacao = acao;
            if (acao == 1)
            {
                c = new FarmPlannerClient.Organizacao.OrganizacaoViewModel();
                ViewBag.Titulo = "Adicionar uma Organização";
                ViewBag.Acao = "adicionar";
            }
            else
            {
                Task<FarmPlannerClient.Organizacao.OrganizacaoViewModel> ret = _culturaAPI.ListaById(_sessionManager.contaguid, id);
                c = await ret;
                if (acao == 2)
                {
                    ViewBag.Titulo = "Editar uma Organização";
                    ViewBag.Acao = "editar";
                }
                else
                {
                    ViewBag.Titulo = "Excluir uma Organização";
                    ViewBag.Acao = "excluir";
                }
            }
            if (acao == 2)
            {
                foreach (var x in c.usuarios)
                {
                    var user = await _userManager.FindByIdAsync(x.uid);
                    x.nomeusuario = user.UserName;
                }

                ViewData["JsonData"] = Json(c.usuarios);

                var users = _userManager.Users.ToList();
                Task<List<FarmPlannerClient.Conta.UsuarioContaViewModel>> retc = _contaAPI.ListaUsuariosByConta(_sessionManager.contaguid);
                List<FarmPlannerClient.Conta.UsuarioContaViewModel> usuconta = await retc;
                var usumodels = usuconta.Select(vm => new UsuarioConta
                {
                    uid = vm.uid,
                    contaguid = vm.contaguid,
                    idconta = vm.idconta
                }).ToList();
                var selectManyResult = users.SelectMany(User => usumodels
                    .Where(UsuarioConta => UsuarioConta.uid == User.Id)
                    .Select(UsuarioConta => new
                    {
                        uid = User.Id,
                        nome = User.UserName
                    })
                ).ToList();
                ViewData["usuarios"] = new SelectList(selectManyResult, "uid", "nome");
                //  ViewData["usuarios"]=selectManyResult.Select(m => new SelectListItem { Text = m.nomeusu, Value = m.uid.ToString() });
            }
            if (TempData["Erro"] != null)
            {
                if (TempData["dados"] != null)
                {
                    var dadosJson = TempData["dados"].ToString();
                    c = JsonConvert.DeserializeObject<OrganizacaoViewModel>(dadosJson);
                }
                ModelState.AddModelError(string.Empty, TempData["Erro"].ToString());
            }
            return View(c);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,AdminC")]
        public async Task<IActionResult> Editar(string idconta, int id)
        {
            Task<FarmPlannerClient.Organizacao.OrganizacaoViewModel> ret = _culturaAPI.ListaById(idconta, id);
            FarmPlannerClient.Organizacao.OrganizacaoViewModel c = await ret;

            ViewBag.Id = id;
            ViewBag.TiposPessoas = new[] {
                new SelectListItem { Text = "Física", Value = TipodePessoa.Física.ToString() },
                new SelectListItem { Text = "Jurídica", Value = TipodePessoa.Jurídica.ToString() }
            };

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,AdminC")]
        public async Task<IActionResult> Excluir(string idconta, int id)
        {
            Task<FarmPlannerClient.Organizacao.OrganizacaoViewModel> ret = _culturaAPI.ListaById(idconta, id);
            FarmPlannerClient.Organizacao.OrganizacaoViewModel c = await ret;

            ViewBag.TiposPessoas = new[] {
                new SelectListItem { Text = "Física", Value = TipodePessoa.Física.ToString() },
                new SelectListItem { Text = "Jurídica", Value = TipodePessoa.Jurídica.ToString() }
            };

            return View(c);
        }

        [Authorize(Roles = "Admin,AdminC")]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, FarmPlannerClient.Organizacao.OrganizacaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            var response = await _culturaAPI.Salvar(_sessionManager.contaguid, id, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            TempData["dados"] = JsonConvert.SerializeObject(dados);
            return RedirectToAction("adicionar", new { acao = 2, id = id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminC")]
        public async Task<IActionResult> Adicionar(FarmPlannerClient.Organizacao.OrganizacaoViewModel dados)
        {
            dados.idconta = _sessionManager.contaguid;
            var response = await _culturaAPI.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<FarmPlannerClient.Organizacao.OrganizacaoViewModel>(y);
                if (_sessionManager.userrole == "AdminC")
                {
                    addusuario(_sessionManager.uid, result.id);
                }

                return RedirectToAction(nameof(Adicionar), new { acao = 2, id = result.id });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;
            // return View("adicionar");
            return RedirectToAction("adicionar", new { acao = 1, id = dados.id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminC")]
        public async Task<IActionResult> Excluir(int id, FarmPlannerClient.Organizacao.OrganizacaoViewModel dados)
        {
            //FarmPlannerClient.Organizacao.OrganizacaoViewModel dados=new FarmPlannerClient.Organizacao.OrganizacaoViewModel();
            var response = await _culturaAPI.Excluir(_sessionManager.contaguid, id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            TempData["Erro"] = x;
            var dadosStateJson = JsonConvert.SerializeObject(dados);

            TempData["dados"] = dadosStateJson;

            return RedirectToAction("adicionar", new { acao = 3, id = id });
        }

        [Authorize(Roles = "Admin,AdminC")]
        public async Task<ActionResult> addusuario(string uid, int idorg)
        {
            Task<List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel>> retc = _culturaAPI.ListaUsuarioByOrg(idorg);
            List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel> usuconta = await retc;
            if (usuconta == null || (usuconta.Where(x => x.uid == uid).FirstOrDefault() == null))
            {
                OrganizacaoUsuarioViewModel u = new OrganizacaoUsuarioViewModel
                {
                    uid = uid,
                    idorganizacao = idorg
                };
                var response = await _culturaAPI.AdicionarUsuario(u);

                if (response.IsSuccessStatusCode)
                {
                    string y = await response.Content.ReadAsStringAsync();
                    var result = System.Text.Json.JsonSerializer.Deserialize<FarmPlannerClient.Organizacao.OrganizacaoViewModel>(y);
                    return Json(new { success = true, message = "Dados processados com sucesso!" });
                    // return RedirectToAction(nameof(Adicionar), new { acao = 2, id = u.idorganizacao });
                }
                string x = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, x);
                return Json(new { success = false, message = x });
            }
            else
            {
                string x = "Usuário já cadastrado";
                return Json(new { success = false, message = x });
            }

            //return RedirectToAction("adicionar", new { acao = 2, id = idorg });
            return null;
        }

        [Authorize(Roles = "Admin,AdminC")]
        [HttpGet]
        public async Task<ActionResult> delusuario(int id, int idorg)
        {
            var response = await _culturaAPI.ExcluirUsuario(id);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<FarmPlannerClient.Organizacao.OrganizacaoViewModel>(y);
                return RedirectToAction(nameof(Adicionar), new { acao = 2, id = idorg });
            }
            string x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);
            return View("adicionar");
        }
    }
}