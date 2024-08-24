using FarmPlannerAdm.Controllers;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace FarmPlannerAdm.Shared
{
    public class UsuContservice : IUsuarioService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuContservice(UserManager<IdentityUser> userManager, IEmailSender emailSender, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        //private readonly RoleManager<IdentityRole> _roleManager;

        public async Task<bool> AddUsuario(IdentityUser user, string senha)
        {
            var identityResult = await _userManager.CreateAsync(user, senha);
            if (identityResult.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("AdminC"))
                {
                    var role = new IdentityRole("AdminC");
                    await _roleManager.CreateAsync(role);
                }
                await _userManager.AddToRoleAsync(user, "AdminC");
                await _emailSender.SendEmailAsync(user.Email.ToString(), "Acesso as plataforma", "usuario:" + user.Email.ToString() + "   senha: " + senha);
                return true;
            }
            return false;

            //
        }
    }
}