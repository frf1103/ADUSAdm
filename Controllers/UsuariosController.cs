using ADUSAdm.Shared;
using ADUSAdm.ViewModel.Usuario;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace ADUSAdm.Controllers;

[Authorize(Roles = "Super,Admin")]
public class UsuariosController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;

    //  private readonly ADUSClient.Controller.ContaControllerClient _conta;

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly Shared.IEmailSender _emailSender;
    private readonly SessionManager _sessionManager;

    public UsuariosController(
        //     IValidator<AdicionarUsuarioViewModel> adicionarUsuarioValidator,
        UserManager<IdentityUser> userManager,
        //     IValidator<EditarUsuarioViewModel> editarUsuarioValidator,
        RoleManager<IdentityRole> roleManager
,
    //    ADUSClient.Controller.ContaControllerClient conta,
        Shared.IEmailSender emailSender,
        SessionManager sessionManager)
    {
        //   _adicionarUsuarioValidator = adicionarUsuarioValidator;
        _userManager = userManager;
        //  _editarUsuarioValidator = editarUsuarioValidator;
        _roleManager = roleManager;
        // _conta = conta;
        _emailSender = emailSender;
        _sessionManager = sessionManager;
    }

    public async Task<IActionResult> Index(string? r)
    {
        List<string> xusus = new List<string> { };
        //   string contaguid = _sessionManager.contaguid.ToString();
        // Task<List<ADUSClient.Conta.UsuarioContaViewModel>> ret = _conta.ListaUsuariosByConta(contaguid);
        // List<ADUSClient.Conta.UsuarioContaViewModel> c = await ret;

        var usuarios = _userManager.Users
            .Select(u => new ListaUsuarioViewModel
            {
                Id = u.Id,
                Username = u.UserName,
                Email = u.Email,
                Telefone = u.PhoneNumber
            });
        if (r != null)
        {
            ViewBag.linkcopia = _sessionManager.urlconvite + "adicionar/?r=" + r;
        }
        else
        {
            ViewBag.linkcopia = "";
        }

        return View(usuarios);
    }

    [AllowAnonymous]
    public IActionResult Adicionar(string? r = "")
    {
        return View(new AdicionarUsuarioViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adicionar(AdicionarUsuarioViewModel dados)
    {
        dados.Telefone = new string(dados.Telefone.Where(char.IsDigit).ToArray());
        //     var validacaoResult = _adicionarUsuarioValidator.Validate(dados);
        /*
                if (!validacaoResult.IsValid)
                {
                    validacaoResult.AddToModelState(ModelState, string.Empty);
                    return View(dados);
                } */
        if (!await _roleManager.RoleExistsAsync(dados.Role))
        {
            var role = new IdentityRole(dados.Role);
            await _roleManager.CreateAsync(role);
        }
        var user = new IdentityUser
        {
            UserName = dados.Username,
            Email = dados.Username,
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

    public async Task<IActionResult> Editar(string id)
    {
        var usuario = _userManager.Users.FirstOrDefault(u => u.Id == id);
        if (usuario is null)
        {
            return NotFound();
        }
        var dados = new EditarUsuarioViewModel
        {
            Username = usuario.UserName,
            Email = usuario.Email,
            Telefone = usuario.PhoneNumber,
            Role = (await _userManager.GetRolesAsync(usuario)).FirstOrDefault() ?? string.Empty
        };
        return View(dados);
    }

    public async Task<IActionResult> EnviarEmail(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            ModelState.AddModelError("uid", "Email n�o foi encontrado na nossa base");
            return View("index");
        }
        var userid = await _userManager.GetUserIdAsync(user);
        if (userid != null)
        {
            var pwd = "PwdF2024#!";
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, pwd);
            await _emailSender.SendEmailAsync(user.Email, "Acesso � plataforma", "Seja muito bem-vindo(a) a plataforma da ADUS, segue sua senha provis�ria: " + pwd);
            TempData["msg"] = "Senha provis�ria enviada para " + user.Email;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(string id, EditarUsuarioViewModel dados)
    {
        //     var validacaoResult = _editarUsuarioValidator.Validate(dados);
        /*        if (!validacaoResult.IsValid)
                {
                    validacaoResult.AddToModelState(ModelState, string.Empty);
                    return View(dados);
                } */

        dados.Telefone = new string(dados.Telefone.Where(char.IsDigit).ToArray());
        var usuario = _userManager.Users.FirstOrDefault(u => u.Id == id);
        if (usuario is null)
        {
            return NotFound();
        }
        if (!await _roleManager.RoleExistsAsync(dados.Role))
        {
            var role = new IdentityRole(dados.Role);
            await _roleManager.CreateAsync(role);
        }
        usuario.UserName = dados.Username;
        usuario.Email = dados.Email;
        usuario.PhoneNumber = dados.Telefone;
        var identityResult = await _userManager.UpdateAsync(usuario);
        if (!identityResult.Succeeded)
        {
            identityResult.Errors.ToList()
                .ForEach(e => ModelState.AddModelError(string.Empty, e.Description));
            return View(dados);
        }
        await _userManager.RemoveFromRolesAsync(usuario, await _userManager.GetRolesAsync(usuario));
        await _userManager.AddToRoleAsync(usuario, dados.Role);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Excluir(string id)
    {
        var usuario = _userManager.Users.FirstOrDefault(u => u.Id == id);
        if (usuario is null)
        {
            return NotFound();
        }
        await _userManager.DeleteAsync(usuario);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> copiarlink(string id)
    {
        return RedirectToAction(nameof(Index), new { r = id });
    }
}