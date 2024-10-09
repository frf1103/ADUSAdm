using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text.Json;
using FarmPlannerAdm.Shared;
using Microsoft.AspNetCore.Identity;
using FarmPlannerClient.Enum;
using PagSeguro.DotNet.Sdk;
using PagSeguro.DotNet.Sdk.Common.Settings;
using FarmPlannerAdm.ViewModel.Usuario;

namespace FarmPlannerAdm.Controllers
{
    [Authorize(Roles = "Admin,Repres")]
    public class ContaController : Controller
    {
        private readonly FarmPlannerClient.Controller.ContaControllerClient _culturaAPI;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Shared.IEmailSender _emailSender;
        private readonly string emailPagSeguro = "frf1103@gmail.com";
        private readonly string tokenPagSeguro = "90892818-e07b-48d7-9e5c-24dbfa68f528df6258454592b6d7672b2f9d5ca2151ff79f-0836-4a68-ba92-9129eede7910";
        private readonly SessionManager _sessionManager;
        private const int TAMANHO_PAGINA = 5;

        public ContaController(FarmPlannerClient.Controller.ContaControllerClient culturaAPI, Shared.IEmailSender emailSender, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, SessionManager sessionManager)
        {
            _culturaAPI = culturaAPI;

            _roleManager = roleManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Index(string? filtro, int pagina = 1)
        {
            Task<List<FarmPlannerClient.Conta.ContaViewModel>> ret;

            if (_sessionManager.userrole != "Repres")
            {
                ret = _culturaAPI.Lista(filtro);
            }
            else
            {
                ret = _culturaAPI.ListaByRep(_sessionManager.uid, filtro);
            }
            List<FarmPlannerClient.Conta.ContaViewModel> c = await ret;


            ViewBag.permissao = (_sessionManager.userrole != "UserV" && (_sessionManager.userrole != "Repres"));

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)c.Count() / TAMANHO_PAGINA);
            return View(c.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList());
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Adicionar(string? r)
        {
            FarmPlannerClient.Conta.ContaViewModel c = new FarmPlannerClient.Conta.ContaViewModel();
            if (r != null)
            {
                ViewBag.representanteid = r;
                ViewBag.origem = "C";
            }
            else
            {
                ViewBag.representanteid = _sessionManager.uid;
                ViewBag.origem = null;
            }
            ViewBag.Planos = Enum.GetValues(typeof(PlanoAssinatura))
                            .Cast<PlanoAssinatura>()
                            .Select(e => new SelectListItem
                            {
                                Value = e.ToString(),
                                Text = e.ToString()
                            });
            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(string id)
        {
            Task<FarmPlannerClient.Conta.ContaViewModel> ret = _culturaAPI.ListaByContaGUId(id);
            FarmPlannerClient.Conta.ContaViewModel c = await ret;

            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Excluir(string id)
        {
            Task<FarmPlannerClient.Conta.ContaViewModel> ret = _culturaAPI.ListaById(id);
            FarmPlannerClient.Conta.ContaViewModel c = await ret;

            return View(c);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(string id, FarmPlannerClient.Conta.ContaViewModel dados)
        {
            if (dados.uid == null)
            {
                dados.uid = " ";
            }
            dados.contaguid = id;
            var response = await _culturaAPI.Salvar(id, dados);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var x = await response.Content.ReadAsStringAsync();

            ModelState.AddModelError(string.Empty, x);

            return View("editar");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Adicionar(FarmPlannerClient.Conta.ContaViewModel dados, string? origem)
        {
            // dados.representanteid = _sessionManager.uid;
            var u = await _userManager.FindByEmailAsync(dados.email);
            if (u == null)
            {
                var response = await _culturaAPI.Adicionar(dados);

                if (response.IsSuccessStatusCode)
                {
                    string y = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<FarmPlannerClient.Conta.ContaViewModel>(y);
                    var idconta = result.id;
                    string pwd = "Pwd" + dados.cpf.Substring(0, 4) + "@!";
                    AdicionarUsuarioViewModel usu = new AdicionarUsuarioViewModel
                    {
                        Username = dados.email,
                        Senha = pwd,
                        Email = dados.email,
                        Telefone = dados.telefone,
                        Role = "AdminC"
                    };
                    if (!await _roleManager.RoleExistsAsync(usu.Role))
                    {
                        var role = new IdentityRole(usu.Role);
                        await _roleManager.CreateAsync(role);
                    }
                    var user = new IdentityUser
                    {
                        UserName = usu.Username,
                        Email = usu.Email,
                        PhoneNumber = usu.Telefone
                    };
                    var identityResult = await _userManager.CreateAsync(user, usu.Senha);
                    if (!identityResult.Succeeded)
                    {
                        identityResult.Errors.ToList()
                            .ForEach(e => ModelState.AddModelError(string.Empty, e.Description));
                        //return View(usu);
                        return View("adicionar");
                    }
                    await _userManager.AddToRoleAsync(user, usu.Role);

                    result.uid = user.Id;
                    await _culturaAPI.Salvar(idconta, result);
                    await _culturaAPI.AddUsuarioConta(new FarmPlannerClient.Conta.UsuarioContaViewModel
                    {
                        idconta = idconta,
                        contaguid = idconta,
                        uid = result.uid
                    });
                    int qtdper = 0;
                    if (dados.assinatura.plano == PlanoAssinatura.DEGUSTACAO)
                    {
                        qtdper = 30;
                    }
                    else
                    {
                        qtdper = 365;
                    }

                    await _culturaAPI.AddAssinaturaConta(new FarmPlannerClient.Conta.AssinaturaContaViewModel
                    {
                        idconta = idconta,
                        dataassinatura = DateTime.Now,
                        dataexpiracao = DateTime.Now.AddDays(qtdper),
                        plano = dados.assinatura.plano
                    });

                    // pwd = "Pw2024@!";
                    await _emailSender.SendEmailAsync(dados.email.ToString(),
                        "Acesso as plataforma", "Seja muito bem-vindo(a) a plataforma de planejamento agricola, abaixo seguem os dados do plano e de seu acesso:" +
                         "Usuário = " + dados.email.ToString() + Environment.NewLine +
                         "Senha= " + pwd + Environment.NewLine +
                         "Plano: " + dados.assinatura.plano.ToString());

                    if (origem == null)
                    {
                        return RedirectToAction(nameof(Adicionar));
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Conta incluída com sucesso. Favor verificar seu email para ter acesso à plataforma");
                        return View("adicionar");
                    }
                }
                string x = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(string.Empty, x);
                return View("adicionar");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Já existe esse email cadastrado como usuário");
                return View("adicionar");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(string id, FarmPlannerClient.Conta.ContaViewModel dados)
        {
            //FarmPlannerClient.Conta.ContaViewModel dados=new FarmPlannerClient.Conta.ContaViewModel();
            var response = await _culturaAPI.Excluir(id);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            string x = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, x);

            return View("excluir");
        }

        public async Task<string> AddUsuario(AdicionarUsuarioViewModel usu)
        {
            if (!await _roleManager.RoleExistsAsync(usu.Role))
            {
                var role = new IdentityRole(usu.Role);
                await _roleManager.CreateAsync(role);
            }
            var user = new IdentityUser
            {
                UserName = usu.Username,
                Email = usu.Email,
                PhoneNumber = usu.Telefone
            };
            var identityResult = await _userManager.CreateAsync(user, usu.Senha);
            if (!identityResult.Succeeded)
            {
                identityResult.Errors.ToList()
                    .ForEach(e => ModelState.AddModelError(string.Empty, e.Description));
                //return View(usu);
                return null;
            }
            await _userManager.AddToRoleAsync(user, usu.Role);

            return user.Id;
        }

        public ActionResult Assinaturaconfirmada()
        {
            return View();
        }

        [HttpPost]
        public ActionResult RealizarPagamento(FarmPlannerClient.Conta.PagamentoViewModel pagamento)
        {
            /*     try
                 {
                     PagSeguroEnvironment env = new PagSeguroEnvironment();
                     PagSeguroClient pagSeguroClient = new PagSeguroClient();

                     EnvironmentConfiguration.ChangeEnvironment(EnvironmentConfiguration.SandboxEnvironment);
                     AccountCredentials credentials = new AccountCredentials(emailPagSeguro, tokenPagSeguro);

                     PaymentRequest payment = new PaymentRequest();
                     payment.Items.Add(new Item(pagamento.Id, pagamento.Descricao, 1, pagamento.Valor));
                     payment.Sender = new Sender(pagamento.Nome, pagamento.Email, new Phone("11", "999999999"));
                     payment.Currency = Currency.Brl;

                     Uri paymentRedirectUri = payment.Register(credentials);
                     return Redirect(paymentRedirectUri.ToString());
                 }
                 catch (Exception ex)
                 {
                     // Handle errors here
                     return View("Error", new HandleErrorInfo(ex, "Pagamento", "RealizarPagamento"));
                 }*/
            return null;
        }
    }
}