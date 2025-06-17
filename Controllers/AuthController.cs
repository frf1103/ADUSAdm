using ADUSAdm.Data;
using ADUSAdm.Shared;
using ADUSAdm.ViewModel.Auth;
using ADUSAdm.ViewModel.Usuario;

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

namespace ADUSAdm.Controllers;

public class AuthController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ADUScontext _context;

    private readonly IEmailSender _emailSender;
    private readonly SessionManager _sessionManager;
    private readonly IConfiguration _configuration;

    //   private readonly AGMContext _Agmcontext;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager,
        ADUScontext context,
        //AGMContext agmcontext,
        RoleManager<IdentityRole> roleManager, IEmailSender emailSender, SessionManager sessionManager, IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        //   _Agmcontext = agmcontext;
        _roleManager = roleManager;
        //_contaControllerClient = contaControllerClient;
        _emailSender = emailSender;
        //_preferusuClient = preferusuClient;
        //_organizacaoClient = organizacaoClient;
        _sessionManager = sessionManager;
        _configuration = configuration;
        //_contaAPI = contaAPI;
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
                Role = "Super"
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
        _sessionManager.uid = userid;
        _sessionManager.userrole = role;

        _sessionManager.uid = userid;
        _sessionManager.userrole = role;
        _sessionManager.username = dados.Username;

        _sessionManager.urlconvite = _configuration.GetValue<string>("AppSettings:urlconvite");

        //_sessionManager.idconta = conta.idconta;

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
                Role = "Super"
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
        //UsuarioContaViewModel x = new UsuarioContaViewModel();
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
            await _emailSender.SendEmailAsync(uid.ToString(), "Acesso à plataforma", "Seja muito bem-vindo(a) a plataforma ADUS, segue sua senha provisória: " + pwd);
            return View("resetarsenha");
        }
        return View("Login");
    }
}