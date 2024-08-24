using FarmPlannerAdm.Shared;
using FarmPlannerAdm.ViewModel.Usuario;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace FarmPlannerAdm.Controllers;

[Authorize(Roles = "Admin,AdminC")]
public class UsuariosController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;

    private readonly FarmPlannerClient.Controller.ContaControllerClient _conta;

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly Shared.IEmailSender _emailSender;
    private readonly SessionManager _sessionManager;

    public UsuariosController(
        //     IValidator<AdicionarUsuarioViewModel> adicionarUsuarioValidator,
        UserManager<IdentityUser> userManager,
        //     IValidator<EditarUsuarioViewModel> editarUsuarioValidator,
        RoleManager<IdentityRole> roleManager
,
        FarmPlannerClient.Controller.ContaControllerClient conta,
        Shared.IEmailSender emailSender,
        SessionManager sessionManager)
    {
        //   _adicionarUsuarioValidator = adicionarUsuarioValidator;
        _userManager = userManager;
        //  _editarUsuarioValidator = editarUsuarioValidator;
        _roleManager = roleManager;
        _conta = conta;
        _emailSender = emailSender;
        _sessionManager = sessionManager;
    }

    public async Task<IActionResult> Index(string? r)
    {
        List<string> xusus = new List<string> { };
        string contaguid = _sessionManager.contaguid.ToString();
        Task<List<FarmPlannerClient.Conta.UsuarioContaViewModel>> ret = _conta.ListaUsuariosByConta(contaguid);
        List<FarmPlannerClient.Conta.UsuarioContaViewModel> c = await ret;
        for (var i = 0; i < c.Count; i++)
        {
            xusus.Add(c[i].uid);
        }

        var usuarios = _userManager.Users.Where(u => xusus.Contains(u.Id))
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

        await _conta.AddUsuarioConta(new FarmPlannerClient.Conta.UsuarioContaViewModel
        {
            contaguid = _sessionManager.contaguid,
            idconta = _sessionManager.contaguid,
            uid = user.Id.ToString()
        });

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
            ModelState.AddModelError("uid", "Email não foi encontrado na nossa base");
            return View("index");
        }
        var userid = await _userManager.GetUserIdAsync(user);
        if (userid != null)
        {
            var pwd = "PwdF2024#!";
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, pwd);
            await _emailSender.SendEmailAsync(user.Email, "Acesso à plataforma", "Seja muito bem-vindo(a) a plataforma de planejamento agricola, segue sua senha provisória: " + pwd);
            TempData["msg"] = "Senha provisória enviada para " + user.Email;
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