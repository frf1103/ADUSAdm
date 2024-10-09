using FarmPlannerAdm.Data;
using FarmPlannerAdm.Shared;
using FarmPlannerAdm.ViewModel.Auth;
using FarmPlannerAdm.ViewModel.Usuario;
using FarmPlannerClient.Conta;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;

namespace FarmPlannerAdm.Controllers;

public class AuthController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly FarmPlannercontext _context;
    private readonly FarmPlannerClient.Controller.ContaControllerClient _contaControllerClient;
    private readonly IEmailSender _emailSender;
    private readonly FarmPlannerClient.Controller.PreferUsuControllerClient _preferusuClient;
    private readonly FarmPlannerClient.Controller.OrganizacaoControllerClient _organizacaoClient;
    private readonly SessionManager _sessionManager;
    private readonly IConfiguration _configuration;
    //   private readonly AGMContext _Agmcontext;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager,
        FarmPlannercontext context,
        //AGMContext agmcontext,
        RoleManager<IdentityRole> roleManager, FarmPlannerClient.Controller.ContaControllerClient contaControllerClient, IEmailSender emailSender, FarmPlannerClient.Controller.PreferUsuControllerClient preferusuClient, FarmPlannerClient.Controller.OrganizacaoControllerClient organizacaoClient, SessionManager sessionManager, IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        //   _Agmcontext = agmcontext;
        _roleManager = roleManager;
        _contaControllerClient = contaControllerClient;
        _emailSender = emailSender;
        _preferusuClient = preferusuClient;
        _organizacaoClient = organizacaoClient;
        _sessionManager = sessionManager;
        _configuration = configuration;
    }

    public async Task<IActionResult> Login()
    {
        var usu = _userManager.Users.Where(u => u.UserName == "frf1103@gmail.com").FirstOrDefault();

        if (usu == null)
        {
            var dados = new AdicionarUsuarioViewModel
            {
                Username = "frf1103@gmail.com",
                Senha = "Fbs2023@!",
                Email = "frf1103@gmail.com",
                Telefone = "62992883095",
                Role = "Admin"
            };
            if (!await _roleManager.RoleExistsAsync(dados.Role))
            {
                var role = new IdentityRole(dados.Role);
                await _roleManager.CreateAsync(role);
            }
            var user = new IdentityUser
            {
                UserName = dados.Username,
                Email = dados.Email,
                PhoneNumber = dados.Telefone
            };
            var identityResult = await _userManager.CreateAsync(user, dados.Senha);
            if (!identityResult.Succeeded)
            {
                identityResult.Errors.ToList()
                    .ForEach(e => ModelState.AddModelError(string.Empty, e.Description));
                return View(dados);
            }
            await _userManager.AddToRoleAsync(user, dados.Role);

            var resp = await _contaControllerClient.GetContaByUid(user.Id);
            if (resp == null)
            {
                var response = await _contaControllerClient.Adicionar(new ContaViewModel
                {
                    cpf = "42748135334",
                    email = "frf1103@gmail.com",
                    nome = "Francisco",
                    telefone = "62992883095",
                    uid = user.Id,
                    ativa = true
                });

                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ContaViewModel>(y);
                var responsec = await _contaControllerClient.AddUsuarioConta(new UsuarioContaViewModel
                {
                    contaguid = result.id,
                    idconta = result.id,
                    uid = user.Id
                });

                var responsea = await _contaControllerClient.AddAssinaturaConta(new AssinaturaContaViewModel
                {
                    idconta = result.id,
                    plano = 0,
                    dataexpiracao = DateTime.Now.AddYears(20),
                    dataassinatura = DateTime.Now
                });
            }
            return RedirectToAction(nameof(Login));
        }
        else return View("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel dados, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            return View(dados);
        }
        var result = await _signInManager.PasswordSignInAsync(dados.Username, dados.Senha, dados.Lembrar, false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos");
            return View(dados);
        }
        //        var user = await _signInManager.UserManager.FindByLoginAsync(dados.Username, dados.Senha);

        var user = await _userManager.FindByEmailAsync(dados.Username);
        var userid = await _userManager.GetUserIdAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        string role = "";
        if (roles != null)
        {
            role = roles[0];
        }
        if (role != "Repres")
        {
            var conta = await _contaControllerClient.GetContaByUid(user.Id);

            _sessionManager.contaguid = conta.contaguid;
            _sessionManager.contaguid = conta.contaguid;
        }
        _sessionManager.uid = userid;
        _sessionManager.userrole = role;

        _sessionManager.uid = userid;
        _sessionManager.userrole = role;

        _sessionManager.urlconvite= _configuration.GetValue<string>("AppSettings:urlconvite");

        //_sessionManager.idconta = conta.idconta;

        Task<FarmPlannerClient.PreferUsu.PreferUsuViewModel> ret = _preferusuClient.Lista(_sessionManager.uid);
        FarmPlannerClient.PreferUsu.PreferUsuViewModel c = await ret;
        if (c == null)
        {
            return RedirectToAction("PreferUsu");
        }
        else
        {
            _sessionManager.descanoagricola = c.descano;
            _sessionManager.descorganizacao = c.descorg;
            _sessionManager.idorganizacao = c.idorganizacao;
            _sessionManager.idanoagricola = c.idanoagricola;

            _sessionManager.descanoagricola = c.descano;
            _sessionManager.descorganizacao = c.descorg;
            _sessionManager.idorganizacao = c.idorganizacao;
            _sessionManager.idanoagricola = c.idanoagricola;
        }

        if (returnUrl is not null)
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _sessionManager.preferencias = "";
        _sessionManager.contaguid = "";
        _sessionManager.idorganizacao = 0;
        _sessionManager.idanoagricola = 0;
        _sessionManager.descorganizacao = "";
        _sessionManager.descanoagricola = "";

        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    public IActionResult AlterarSenha()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaViewModel dados)
    {
        if (!ModelState.IsValid)
        {
            return View(dados);
        }
        var user = await _userManager.GetUserAsync(User);
        var result = await _userManager.ChangePasswordAsync(user, dados.SenhaAtual, dados.NovaSenha);
        if (!result.Succeeded)
        {
            result.Errors.ToList().ForEach(x => ModelState.AddModelError(string.Empty, x.Description));
            return View(dados);
        }

        await _signInManager.RefreshSignInAsync(user);
        return RedirectToAction(nameof(AlterarSenha));
    }

    [AllowAnonymous]
    public async Task<IActionResult> AdicionarAdmin()
    {
        var usu = _userManager.Users.Where(u => u.UserName == "admin").FirstOrDefault();

        if (usu == null)
        {
            var dados = new AdicionarUsuarioViewModel
            {
                Username = "admin",
                Senha = "Fr2023@!",
                Email = "frf1103@gmail.com",
                Telefone = "62992883095",
                Role = "Admin"
            };
            if (!await _roleManager.RoleExistsAsync(dados.Role))
            {
                var role = new IdentityRole(dados.Role);
                await _roleManager.CreateAsync(role);
            }
            var user = new IdentityUser
            {
                UserName = dados.Username,
                Email = dados.Email,
                PhoneNumber = dados.Telefone
            };
            var identityResult = await _userManager.CreateAsync(user, dados.Senha);
            if (!identityResult.Succeeded)
            {
                identityResult.Errors.ToList()
                    .ForEach(e => ModelState.AddModelError(string.Empty, e.Description));
                return View(dados);
            }
            await _userManager.AddToRoleAsync(user, dados.Role);
            return RedirectToAction(nameof(Index));
        }
        else return View("Login");
    }

    [AllowAnonymous]
    public ActionResult resetarsenha()
    {
        UsuarioContaViewModel x = new UsuarioContaViewModel();
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> enviarsenha(string uid)
    {
        var user = await _userManager.FindByEmailAsync(uid);
        if (user == null)
        {
            ModelState.AddModelError("uid", "Email não foi encontrado na nossa base");
            return View("resetarsenha");
        }
        var userid = await _userManager.GetUserIdAsync(user);
        if (userid != null)
        {
            var pwd = "PwdF2024#!";
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, pwd);
            await _emailSender.SendEmailAsync(uid.ToString(), "Acesso à plataforma", "Seja muito bem-vindo(a) a plataforma de planejamento agricola, segue sua senha provisória: " + pwd);
            return View("resetarsenha");
        }
        return View("Login");
    }

    public async Task<IActionResult> PreferUsu()
    {
        Task<FarmPlannerClient.PreferUsu.PreferUsuViewModel> ret = _preferusuClient.Lista(_sessionManager.uid);
        FarmPlannerClient.PreferUsu.PreferUsuViewModel c = await ret;
        if (c == null)
        {
            c = new FarmPlannerClient.PreferUsu.PreferUsuViewModel();
        }
        Task<List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel>> retorg = _organizacaoClient.ListaOrganizacaoByUID(_sessionManager.uid);
        List<FarmPlannerClient.Organizacao.OrganizacaoUsuarioViewModel> t = await retorg;

        t = t.OrderBy(t => t.descorg).ToList();
        ViewBag.organizacoes = t.Select(m => new SelectListItem { Text = m.descorg, Value = m.idorganizacao.ToString() });

        return View(c);
    }

    public async Task<IActionResult> GravarPreferUsu(string uid, FarmPlannerClient.PreferUsu.PreferUsuViewModel dados)
    {
        dados.idconta = _sessionManager.contaguid;
        Task<FarmPlannerClient.PreferUsu.PreferUsuViewModel> ret = _preferusuClient.Lista(_sessionManager.uid);
        FarmPlannerClient.PreferUsu.PreferUsuViewModel c = await ret;
        if (c == null)
        {
            dados.uid = _sessionManager.uid;
            var response = await _preferusuClient.Adicionar(dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.PreferUsu.PreferUsuViewModel>(y);
            }
            else
            {
                string x = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(string.Empty, x);
                return View("PreferUsu");
            }
        }
        else
        {
            var response = await _preferusuClient.Salvar(_sessionManager.uid, dados);

            if (response.IsSuccessStatusCode)
            {
                string y = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FarmPlannerClient.PreferUsu.PreferUsuViewModel>(y);
            }
            else
            {
                string x = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(string.Empty, x);
                return View("PreferUsu");
            }
        }
        ret = _preferusuClient.Lista(_sessionManager.uid);
        c = await ret;
        _sessionManager.descanoagricola = c.descano;
        _sessionManager.descorganizacao = c.descorg;
        _sessionManager.idorganizacao = c.idorganizacao;
        _sessionManager.idanoagricola = c.idanoagricola;

        _sessionManager.descanoagricola = c.descano;
        _sessionManager.descorganizacao = c.descorg;
        _sessionManager.idorganizacao = c.idorganizacao;
        _sessionManager.idanoagricola = c.idanoagricola;

        TempData["preferencias"] = "Você está usando: " + c.descorg.Trim() + "/" + c.descano.Trim();
        return RedirectToAction("Index", "Home");

        return View(c);
    }

    public async Task<JsonResult> LoadPrefUsu()
    {
        Task<FarmPlannerClient.PreferUsu.PreferUsuViewModel> ret = _preferusuClient.Lista(_sessionManager.uid);
        FarmPlannerClient.PreferUsu.PreferUsuViewModel c = await ret;
        if (c != null)
        {
            _sessionManager.descanoagricola = c.descano;
            _sessionManager.descorganizacao = c.descorg;
            _sessionManager.idorganizacao = c.idorganizacao;
            _sessionManager.idanoagricola = c.idanoagricola;

            _sessionManager.descanoagricola = c.descano;
            _sessionManager.descorganizacao = c.descorg;
            _sessionManager.idorganizacao = c.idorganizacao;
            _sessionManager.idanoagricola = c.idanoagricola;

            if (!String.IsNullOrWhiteSpace(_sessionManager.descorganizacao) && !String.IsNullOrWhiteSpace(_sessionManager.descanoagricola))
            {
                var x = new { pref = "Preferências: " + _sessionManager.descorganizacao.Trim() + "       -       Ano Agrícola: " + _sessionManager.descanoagricola.Trim() };
                return Json(x);
            }
            else
            {
                var x = new { pref = "DEFINA AS PREFERÊNCIAS" };
                return Json(x);
            }
        }
        else
        {
            return null;
        }
    }
}